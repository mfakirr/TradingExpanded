using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;
using TaleWorlds.Library;
using TradingExpanded.Helpers;
using TradingExpanded.Utils;
using TradingExpanded.Components;
using TaleWorlds.ObjectSystem;

namespace TradingExpanded.Models
{
    /// <summary>
    /// Ticaret bilgisi toplamak için yerleşimler arasında seyahat eden kurye
    /// </summary>
    public class Courier
    {
        #region Temel Özellikler
        [SaveableField(1)]
        private Settlement _origin;

        [SaveableField(2)]
        private Settlement _destination;

        [SaveableField(3)]
        private MobileParty _mobileParty;

        [SaveableField(4)]
        private CampaignTime _creationTime;

        [SaveableField(5)]
        private bool _delivered = false;

        [SaveableField(6)]
        private bool _failed = false;

        [SaveableField(7)]
        private float _quantity;
        
        [SaveableField(8)]
        private string _id;
        
        [SaveableField(9)]
        private bool _isReturning = false;
        
        [SaveableField(10)]
        private Dictionary<string, float> _collectedPrices = new Dictionary<string, float>();

        public Settlement Origin => _origin;
        public Settlement Destination => _destination;
        public MobileParty MobileParty => _mobileParty;
        public bool IsDelivered => _delivered;
        public bool HasFailed => _failed;
        public float Quantity => _quantity;
        public bool IsReturning => _isReturning;
        public IReadOnlyDictionary<string, float> CollectedPrices => _collectedPrices;
        public string Id => _id;
        #endregion
        
        /// <summary>
        /// Boş yapıcı - deserialization için
        /// </summary>
        public Courier()
        {
            _id = Guid.NewGuid().ToString();
            _creationTime = CampaignTime.Now;
            _collectedPrices = new Dictionary<string, float>();
        }
        
        /// <summary>
        /// Yeni bir kurye oluşturur
        /// </summary>
        public Courier(Settlement origin, Settlement destination, float quantity)
        {
            _origin = origin ?? throw new ArgumentNullException(nameof(origin));
            _destination = destination ?? throw new ArgumentNullException(nameof(destination));
            _quantity = quantity;
            _creationTime = CampaignTime.Now;
            _id = Guid.NewGuid().ToString();
            _collectedPrices = new Dictionary<string, float>();
        }
        
        /// <summary>
        /// Haritada kurye partisini oluşturur
        /// </summary>
        public void CreatePartyOnMap()
        {
            if (ShouldSkipPartyCreation())
                return;

            try
            {
                var (from, to, partyName) = PreparePartyInfo();
                
                if (from == null || to == null)
                {
                    LogError("Parti oluşturulamadı: Kalkış veya hedef yerleşimi boş");
                    _failed = true;
                    return;
                }
                
                LogInfo($"Kurye partisi oluşturuluyor: {from.Name} => {to.Name}");

                _mobileParty = MobilePartyHelper.CreateNewCourierParty(from, to, Hero.MainHero, partyName);
                
                if (_mobileParty?.IsActive == true)
                {
                    ValidatePartyComponent();
                    ShowPartyCreatedMessage(from, to);
                }
                else
                {
                    LogError("Kurye partisi oluşturulamadı veya oluşturulduktan sonra aktif değil.");
                    _failed = true;
                }
            }
            catch (Exception ex)
            {
                LogError($"Kurye partisi oluşturma hatası: {ex.Message}", ex);
                _failed = true;
            }
        }
        
        /// <summary>
        /// Kuryenin teslim durumunu kontrol eder
        /// </summary>
        public void CheckDelivery()
        {
            if (_delivered || _failed) 
                return;

            try
            {
                if (!IsPartyActive())
                {
                    LogInfo("Kurye partisi artık aktif değil, başarısız olarak işaretleniyor.");
                    MarkAsFailed();
                    return;
                }
                
                var currentTarget = _isReturning ? _origin : _destination;
                
                // Yerleşime varış kontrolü
                if (HasReachedSettlement(currentTarget))
                    return;
                
                // Yakınlık kontrolü
                if (IsCloseToTarget(currentTarget))
                    return;
                
                // Hedef kontrolü ve düzeltme
                FixTargetIfNeeded(currentTarget);
            }
            catch (Exception ex)
            {
                LogError($"Teslim kontrolü sırasında hata: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// Teslim durumunu günceller ve başarılı olarak işaretler
        /// </summary>
        public void MarkAsDelivered()
        {
            _delivered = true;
            RemoveParty();
            
            if (_collectedPrices.Count > 0)
            {
                UpdateTradeRumors();
            }
        }
        
        /// <summary>
        /// Kurye görevini başarısız olarak işaretler
        /// </summary>
        public void MarkAsFailed()
        {
            _failed = true;
            RemoveParty();
        }
        
        /// <summary>
        /// Kurye hakkında özet bilgi döndürür
        /// </summary>
        public string GetSummary()
        {
            string status = _delivered ? "Teslim Edildi" : 
                           _failed ? "Başarısız" : 
                           _isReturning ? "Dönüş Yolunda" : "Gidiş Yolunda";
            
            string summary = $"Kurye: {Origin.Name} → {Destination.Name}\n";
            summary += $"Durum: {status}\n";
            summary += $"Miktar: {_quantity}\n";
            
            if (_collectedPrices.Count > 0)
            {
                summary += $"Toplanan Fiyat Bilgisi: {_collectedPrices.Count} ürün\n";
            }
            
            return summary;
        }
        
        #region Yardımcı Metodlar
        private bool ShouldSkipPartyCreation()
        {
            bool shouldSkip = _delivered || _failed || (_mobileParty != null && _mobileParty.IsActive);
            
            if (shouldSkip)
            {
                LogInfo($"Kurye partisi oluşturulmadı. Sebep: Teslim={_delivered}, Başarısız={_failed}, Aktif={_mobileParty != null && _mobileParty.IsActive}");
            }
            
            return shouldSkip;
        }
        
        private (Settlement from, Settlement to, TextObject partyName) PreparePartyInfo()
        {
            Settlement from = _isReturning ? _destination : _origin;
            Settlement to = _isReturning ? _origin : _destination;
            
            string nameText = _isReturning 
                ? $"Ulak - Dönüş ({from.Name} → {to.Name})"
                : $"Ulak ({from.Name} → {to.Name})";
                
            TextObject partyName = new TextObject(nameText);
            
            return (from, to, partyName);
        }
        
        private void ValidatePartyComponent()
        {
            if (_mobileParty.PartyComponent is CourierPartyComponent)
            {
                LogInfo("Parti başarıyla CourierPartyComponent kullanılarak oluşturuldu");
            }
            else
            {
                LogInfo("Parti oluşturuldu ancak CourierPartyComponent türünde değil");
            }
        }
        
        private void ShowPartyCreatedMessage(Settlement from, Settlement to)
        {
            string message = _isReturning 
                ? $"{from.Name}'dan {to.Name}'a dönmekte olan ulak haritada görünür durumda."
                : $"{from.Name}'dan {to.Name}'a giden ulak haritada görünür durumda.";
            
            ShowMessage(message, Colors.Green);
        }
        
        private bool IsPartyActive()
        {
            return _mobileParty != null && _mobileParty.IsActive;
        }
        
        private bool HasReachedSettlement(Settlement target)
        {
            if (_mobileParty.CurrentSettlement == target)
            {
                if (!_isReturning)
                {
                    LogInfo($"Kurye {_destination.Name} hedefine ulaştı, fiyat bilgileri toplanıyor ve geri dönüş başlatılıyor.");
                    CollectPricesFromTown(_destination);
                    StartReturnJourney();
                }
                else
                {
                    LogInfo($"Kurye {_origin.Name} kökenine geri döndü, teslim edildi olarak işaretleniyor.");
                    MarkAsDelivered();
                    
                    ShowMessage($"{_destination.Name}'dan {_origin.Name}'a dönen ulak fiyat bilgileriyle başarıyla ulaştı.", Colors.Green);
                }
                
                return true;
            }
            
            return false;
        }
        
        private bool IsCloseToTarget(Settlement target)
        {
            float distanceToTarget = Campaign.Current.Models.MapDistanceModel.GetDistance(_mobileParty, target);
            
            if (distanceToTarget <= 1.0f)
            {
                if (!_isReturning)
                {
                    LogInfo($"Kurye {_destination.Name} hedefine yakın (mesafe: {distanceToTarget}), fiyat bilgileri toplanıyor ve geri dönüş başlatılıyor.");
                    CollectPricesFromTown(_destination);
                    StartReturnJourney();
                }
                else
                {
                    LogInfo($"Kurye {_origin.Name} kökenine yakın (mesafe: {distanceToTarget}), teslim edildi olarak işaretleniyor.");
                    MarkAsDelivered();
                    
                    ShowMessage($"{_destination.Name}'dan {_origin.Name}'a dönen ulak fiyat bilgileriyle başarıyla ulaştı.", Colors.Green);
                }
                
                return true;
            }
            
            return false;
        }
        
        private void FixTargetIfNeeded(Settlement target)
        {
            if (_mobileParty.TargetSettlement != target && _mobileParty.Ai != null)
            {
                LogInfo("Parti hedefi kaybolmuş, yeniden ayarlanıyor...");
                _mobileParty.Ai.SetMoveGoToSettlement(target);
            }
        }
        
        private void RemoveParty()
        {
            if (IsPartyActive())
            {
                try
                {
                    LogInfo("Kurye partisi kaldırılıyor...");
                    _mobileParty.RemoveParty();
                    LogInfo("Kurye partisi haritadan kaldırıldı.");
                }
                catch (Exception ex)
                {
                    LogError($"Parti kaldırılırken hata oluştu: {ex.Message}", ex);
                }
            }
        }
        
        private void CollectPricesFromTown(Settlement town)
        {
            try
            {
                _collectedPrices.Clear();
                
                if (town?.IsTown != true || town.Town == null)
                {
                    LogError($"{town?.Name} yerleşimi fiyat bilgilerini toplamak için uygun değil!");
                    return;
                }
                
                LogInfo($"{town.Name} şehrinden fiyat bilgileri toplanıyor...");
                
                // Sadece ticari malları ve gıda ürünlerini filtrele, sonra limit uygula
                var itemTypes = MBObjectManager.Instance.GetObjectTypeList<ItemObject>()
                    .Where(IsTradeGood)
                    .Take(50);
                
                foreach (var item in itemTypes)
                {
                    float price = town.Town.GetItemPrice(item, null, false);
                    _collectedPrices[item.StringId] = price;
                    
                    LogInfo($"{item.Name}: {price} denar");
                }
                
                LogInfo($"Toplam {_collectedPrices.Count} ürün fiyatı toplandı.");
            }
            catch (Exception ex)
            {
                LogError($"Fiyat toplama hatası: {ex.Message}", ex);
            }
        }
        
        private bool IsTradeGood(ItemObject item)
        {
            if (item == null)
                return false;
                
            return item.IsTradeGood;
        }
        
        private void StartReturnJourney()
        {
            try
            {
                LogInfo("Geri dönüş yolculuğu başlatılıyor...");
                
                RemoveParty();
                _isReturning = true;
                CreatePartyOnMap();
                
                LogInfo($"Geri dönüş yolculuğu başladı: {_destination.Name} → {_origin.Name}");
                ShowMessage($"{_destination.Name}'dan {_origin.Name}'a dönen ulak yola çıktı.", Colors.Green);
            }
            catch (Exception ex)
            {
                LogError($"Geri dönüş başlatma hatası: {ex.Message}", ex);
                MarkAsFailed();
            }
        }
        
        private void UpdateTradeRumors()
        {
            try
            {
                LogInfo($"{_destination.Name} şehrinden toplanan fiyat bilgileri işleniyor...");
                
                var tradeRumors = CreateTradeRumors();
                
                if (tradeRumors.Count > 0)
                {
                    var success = AddTradeRumorsToGame(tradeRumors);
                    
                    if (!success)
                    {
                        ShowFallbackPriceInfo(tradeRumors);
                    }
                }
                else
                {
                    LogInfo("Hiç ticari mal bulunamadı.");
                    ShowMessage($"{_destination.Name} şehrinden ticari mal fiyatı bulunamadı.", Colors.Red);
                }
            }
            catch (Exception ex)
            {
                LogError($"Fiyat bilgilerini işleme hatası: {ex.Message}", ex);
            }
        }
        
        private List<TradeRumor> CreateTradeRumors()
        {
            var rumors = new List<TradeRumor>();
            int count = 0;
            
            foreach (var priceInfo in _collectedPrices)
            {
                var item = MBObjectManager.Instance.GetObject<ItemObject>(priceInfo.Key);
                
                if (IsTradeGood(item))
                {
                    float buyPrice = priceInfo.Value;
                    float sellPrice = _destination.Town.GetItemPrice(item, null, true);
                    
                    var rumor = new TradeRumor(
                        _destination, 
                        item, 
                        (int)buyPrice, 
                        (int)sellPrice, 
                        10);
                    
                    rumors.Add(rumor);
                    count++;
                    
                    LogInfo($"Ticaret duyumu oluşturuldu: {item.Name} @ {_destination.Name} - Alış: {buyPrice}, Satış: {sellPrice}");
                }
            }
            
            return rumors;
        }
        
        private bool AddTradeRumorsToGame(List<TradeRumor> rumors)
        {
            try
            {
                var behavior = Campaign.Current.GetCampaignBehavior<TaleWorlds.CampaignSystem.CampaignBehaviors.TradeRumorsCampaignBehavior>();
                
                if (behavior != null)
                {
                    behavior.AddTradeRumors(rumors, _destination);
                    
                    ShowMessage($"{_destination.Name} şehrinden {rumors.Count} ticaret duyumu toplanıp güncellendi.", Colors.Green);
                    LogInfo($"{rumors.Count} ticaret duyumu başarıyla eklendi.");
                    return true;
                }
                
                LogError("TradeRumorsCampaignBehavior bulunamadı.");
                return false;
            }
            catch (Exception ex)
            {
                LogError($"Ticaret duyumları eklenirken hata: {ex.Message}", ex);
                return false;
            }
        }
        
        private void ShowFallbackPriceInfo(List<TradeRumor> rumors)
        {
            string priceReport = $"{_destination.Name} şehrinden toplanan fiyat bilgileri:\n";
            foreach (var rumor in rumors.Take(5))
            {
                priceReport += $"{rumor.ItemCategory.Name}: {GetRumorPrice(rumor)} denar\n";
            }
            
            if (rumors.Count > 5)
            {
                priceReport += $"...ve {rumors.Count - 5} diğer ürün.";
            }
            
            ShowMessage(priceReport, Colors.Green);
        }
        
        private int GetRumorPrice(TradeRumor rumor)
        {
            try
            {
                var priceField = rumor.GetType().GetProperty("BuyPrice") ?? 
                                 rumor.GetType().GetProperty("PriceWithoutTaxes");
                
                if (priceField != null)
                {
                    return (int)priceField.GetValue(rumor);
                }
            }
            catch {}
            
            return 0;
        }
        
        // Log yardımcıları
        private void LogInfo(string message) => LogManager.Instance.WriteInfo($"[Courier] {message}");
        private void LogError(string message, Exception ex = null) => LogManager.Instance.WriteError($"[Courier] {message}", ex);
        private void ShowMessage(string message, Color color) => InformationManager.DisplayMessage(new InformationMessage(message, color));
        #endregion
    }
} 
