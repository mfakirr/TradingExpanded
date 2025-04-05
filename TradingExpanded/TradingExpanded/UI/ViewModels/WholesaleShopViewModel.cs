using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TradingExpanded.Models;
using TradingExpanded.Behaviors;
using TradingExpanded.Services;
using TradingExpanded.UI.Patches;

namespace TradingExpanded.UI.ViewModels
{
    /// <summary>
    /// Toptan satış dükkanı ekranında kullanılacak ViewModel
    /// </summary>
    public class WholesaleShopViewModel : ViewModel
    {
        private readonly WholesaleShop _shop;
        private readonly Town _currentTown;
        private readonly TradingExpandedCampaignBehavior _campaignBehavior;
        private readonly Action _closeAction;
        
        /// <summary>
        /// Dükkana sahip olup olmadığımız durumu
        /// </summary>
        public bool HasShop => _shop != null;
        
        /// <summary>
        /// Dükkan başlığı
        /// </summary>
        [DataSourceProperty]
        public string ShopTitle
        {
            get
            {
                if (_shop != null)
                {
                    return $"{_currentTown.Name} Toptan Ticaret Dükkanı";
                }
                else
                {
                    return $"Toptan Ticaret Dükkanı Kur - {_currentTown.Name}";
                }
            }
        }
        
        /// <summary>
        /// Dükkan sermayesi
        /// </summary>
        [DataSourceProperty]
        public string Capital
        {
            get
            {
                if (_shop != null)
                {
                    return $"{_shop.Capital} Dinar";
                }
                else
                {
                    return "0 Dinar";
                }
            }
        }
        
        /// <summary>
        /// Günlük kar
        /// </summary>
        [DataSourceProperty]
        public string DailyProfit
        {
            get
            {
                if (_shop != null)
                {
                    return $"{_shop.DailyProfit} Dinar/gün";
                }
                else
                {
                    return "0 Dinar/gün";
                }
            }
        }
        
        /// <summary>
        /// Toplam kar
        /// </summary>
        [DataSourceProperty]
        public string TotalProfit
        {
            get
            {
                if (_shop != null)
                {
                    return $"{_shop.TotalProfit} Dinar";
                }
                else
                {
                    return "0 Dinar";
                }
            }
        }
        
        /// <summary>
        /// Ana buton metni
        /// </summary>
        [DataSourceProperty]
        public string MainActionButtonText
        {
            get
            {
                if (_shop != null)
                {
                    return "Dükkanı Yönet";
                }
                else
                {
                    return "Dükkan Kur (5000 Dinar)";
                }
            }
        }

        /// <summary>
        /// Dükkan kurulu mu
        /// </summary>
        [DataSourceProperty]
        public bool IsShopEstablished => _shop != null;
        
        /// <summary>
        /// Constructor
        /// </summary>
        public WholesaleShopViewModel(Town town, TradingExpandedCampaignBehavior campaignBehavior, Action closeAction)
        {
            _currentTown = town;
            _campaignBehavior = campaignBehavior;
            _closeAction = closeAction;
            
            // Mevcut şehirde bir dükkanımız var mı?
            _shop = _campaignBehavior.GetShopInTown(town);
        }
        
        /// <summary>
        /// Ana buton tıklama olayı
        /// </summary>
        public void ExecuteMainAction()
        {
            if (_shop != null)
            {
                // Dükkan yönetim ekranını aç
                ManageShop();
            }
            else
            {
                // Yeni dükkan kur
                EstablishNewShop();
            }
        }
        
        /// <summary>
        /// Dükkan kurma işlemi
        /// </summary>
        private void EstablishNewShop()
        {
            if (Hero.MainHero.Gold >= 5000)
            {
                Hero.MainHero.ChangeHeroGold(-5000);
                _campaignBehavior.CreateShop(_currentTown, 5000);
                InformationManager.DisplayMessage(new InformationMessage(
                    $"{_currentTown.Name} şehrinde yeni bir toptan ticaret dükkanı kurdunuz!"));
                _closeAction?.Invoke();
            }
            else
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    "Yeterli altınınız bulunmuyor. Dükkan kurmak için 5000 dinara ihtiyacınız var.",
                    Colors.Red));
            }
        }
        
        /// <summary>
        /// Dükkan yönetimi
        /// </summary>
        private void ManageShop()
        {
            if (_shop == null)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    "Bu şehirde bir dükkanınız bulunmuyor.", Colors.Red));
                _closeAction?.Invoke();
                return;
            }
            
            // Dükkan yönetim seçenekleri
            var titleText = new TextObject("{=WholesaleShopManageText}Dükkanı Yönet").ToString();
            var inquiryElements = new List<InquiryElement>();
            
            // Dükkan bilgilerini görüntüle seçeneği
            inquiryElements.Add(new InquiryElement(
                "view_details",
                new TextObject("Dükkan Bilgilerini Görüntüle").ToString(),
                null,  // ImageIdentifier
                true,  // isSelectable
                "Dükkan bilgilerini görüntüler"  // hint
            ));
            
            // Sermaye yatırma seçeneği
            inquiryElements.Add(new InquiryElement(
                "invest_capital",
                new TextObject("Sermaye Yatır (1000 Dinar)").ToString(),
                null,  // ImageIdentifier
                true,  // isSelectable
                "Dükkanınıza sermaye yatırır"  // hint
            ));
            
            // Sermaye çekme seçeneği
            inquiryElements.Add(new InquiryElement(
                "withdraw_capital",
                new TextObject("Sermaye Çek (1000 Dinar)").ToString(),
                null,  // ImageIdentifier
                _shop.Capital >= 1000,  // isSelectable
                "Dükkanınızdan sermaye çeker"  // hint
            ));
            
            // Dükkanı kapat seçeneği
            inquiryElements.Add(new InquiryElement(
                "close_shop",
                new TextObject("Dükkanı Kapat").ToString(),
                null,  // ImageIdentifier
                true,  // isSelectable
                "Dükkanınızı kapatır"  // hint
            ));
            
            // Kurye gönderme seçeneği
            inquiryElements.Add(new InquiryElement(
                "send_courier",
                new TextObject("{=CourierSendOption}Ulak Gönder").ToString(),
                null,  // ImageIdentifier
                true,  // isSelectable
                "Diğer şehirlere kurye gönderir"  // hint
            ));
            
            // Geri dönüş seçeneği
            inquiryElements.Add(new InquiryElement(
                "back",
                new TextObject("Geri Dön").ToString(),
                null,  // ImageIdentifier
                true,  // isSelectable
                "Ana menüye döner"  // hint
            ));
            
            // Dükkan detayları
            var shopDetails = new TextObject("Toptan Satış Dükkanı - {TOWN}\nSermaye: {CAPITAL} Dinar\nGünlük Kâr: {PROFIT} Dinar/gün\nToplam Kâr: {TOTAL_PROFIT} Dinar")
                .SetTextVariable("TOWN", _currentTown.Name)
                .SetTextVariable("CAPITAL", _shop.Capital)
                .SetTextVariable("PROFIT", _shop.DailyProfit)
                .SetTextVariable("TOTAL_PROFIT", _shop.TotalProfit)
                .ToString();
            
            // Menüyü göster
            MBInformationManager.ShowMultiSelectionInquiry(
                new MultiSelectionInquiryData(
                    titleText,                                      // titleText
                    shopDetails,                                    // descriptionText
                    inquiryElements,                                // inquiryElements
                    true,                                           // isExitShown
                    0,                                              // minSelectableOptionCount
                    1,                                              // maxSelectableOptionCount
                    GameTexts.FindText("str_done").ToString(),      // affirmativeText
                    GameTexts.FindText("str_cancel").ToString(),    // negativeText
                    OnManageShopOptionSelected,                     // affirmativeAction
                    null,                                           // negativeAction
                    "",                                             // soundEventPath
                    false                                           // isSearchAvailable
                )
            );
        }
        
        /// <summary>
        /// Dükkan yönetim seçeneği seçildiğinde çağrılır
        /// </summary>
        private void OnManageShopOptionSelected(List<InquiryElement> selectedOptions)
        {
            if (selectedOptions == null || selectedOptions.Count == 0)
            {
                _closeAction?.Invoke();
                return;
            }
            
            var selectedOption = selectedOptions[0];
            string identifier = selectedOption.Identifier.ToString();
            
            switch (identifier)
            {
                case "view_details":
                    ShowShopDetails();
                    break;
                    
                case "invest_capital":
                    InvestCapitalToShop();
                    break;
                    
                case "withdraw_capital":
                    WithdrawCapitalFromShop();
                    break;
                    
                case "close_shop":
                    CloseShop();
                    break;
                    
                case "send_courier":
                    SendCourier();
                    break;
                    
                case "back":
                default:
                    _closeAction?.Invoke();
                    break;
            }
        }
        
        /// <summary>
        /// Dükkana sermaye yatırma
        /// </summary>
        private void InvestCapitalToShop()
        {
            if (Hero.MainHero.Gold >= 1000)
            {
                if (_shop.AddCapital(1000))
                {
                    Hero.MainHero.ChangeHeroGold(-1000);
                    
                    InformationManager.DisplayMessage(new InformationMessage(
                        $"Dükkana 1000 dinar sermaye yatırdınız. Yeni sermaye: {_shop.Capital} dinar", Colors.Green));
                }
                else
                {
                    InformationManager.DisplayMessage(new InformationMessage(
                        "Sermaye yatırılırken bir hata oluştu.", Colors.Red));
                }
            }
            else
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    "Yeterli altınınız bulunmuyor. Sermaye yatırmak için 1000 dinara ihtiyacınız var.", Colors.Red));
            }
            
            // Tekrar yönetim menüsünü göster
            ManageShop();
        }
        
        /// <summary>
        /// Dükkandan sermaye çekme
        /// </summary>
        private void WithdrawCapitalFromShop()
        {
            if (_shop.Capital >= 1000)
            {
                if (_shop.WithdrawCapital(1000))
                {
                    Hero.MainHero.ChangeHeroGold(1000);
                    
                    InformationManager.DisplayMessage(new InformationMessage(
                        $"Dükkandan 1000 dinar sermaye çektiniz. Kalan sermaye: {_shop.Capital} dinar", Colors.Green));
                }
                else
                {
                    InformationManager.DisplayMessage(new InformationMessage(
                        "Sermaye çekilirken bir hata oluştu.", Colors.Red));
                }
            }
            else
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    "Dükkanda yeterli sermaye bulunmuyor. En az 1000 dinar sermaye olmalı.", Colors.Red));
            }
            
            // Tekrar yönetim menüsünü göster
            ManageShop();
        }
        
        /// <summary>
        /// Dükkanı kapatma
        /// </summary>
        private void CloseShop()
        {
            // Dükkanı kapatmak istediğinize emin misiniz?
            InformationManager.ShowInquiry(
                new InquiryData(
                    "Dükkanı Kapat",                                                                    // titleText
                    $"{_currentTown.Name} şehrindeki dükkanınızı kapatmak istediğinize emin misiniz? " +  // descriptionText
                    $"Kalan sermayeniz olan {_shop.Capital} dinar size geri ödenecek.",
                    true,                                                                              // isAffirmativeOptionShown
                    true,                                                                              // isNegativeOptionShown
                    "Evet",                                                                            // affirmativeText
                    "Hayır",                                                                           // negativeText
                    () =>                                                                              // onAffirmativeClicked
                    {
                        // Evet seçildi
                        Hero.MainHero.ChangeHeroGold(_shop.Capital);
                        _campaignBehavior.CloseShop(_shop.Id);
                        
                        InformationManager.DisplayMessage(new InformationMessage(
                            $"{_currentTown.Name} şehrindeki dükkanınızı kapattınız. {_shop.Capital} dinar geri ödendi.", Colors.Green));
                            
                        _closeAction?.Invoke();
                    }, 
                    () =>                                                                              // onNegativeClicked
                    {
                        // Hayır seçildi, tekrar yönetim menüsü göster
                        ManageShop();
                    },
                    "",                                                                                // soundEventPath
                    0f                                                                                 // waitTime
                )
            );
        }
        
        /// <summary>
        /// Kurye gönderme menüsünü aç
        /// </summary>
        private void SendCourier()
        {
            try 
            {
                // Ulak limiti kontrolü
                int maxCouriers = Settings.Instance?.MaxCouriers ?? 5;
                int currentCouriers = _campaignBehavior.GetActiveCourierCount();
                
                if (currentCouriers >= maxCouriers)
                {
                    InformationManager.DisplayMessage(new InformationMessage(
                        new TextObject("{=CourierLimitReached}Maksimum ulak sayısına ({MAX_COURIERS}) ulaştınız. Yeni ulak göndermek için mevcut ulaklardan birinin görevini tamamlaması gerekiyor.")
                            .SetTextVariable("MAX_COURIERS", maxCouriers)
                            .ToString(),
                        Colors.Red));
                    return;
                }
                
                // Gönderilecek şehir listesini hazırla
                List<InquiryElement> townOptions = new List<InquiryElement>();
                
                // Şehirleri mesafelerine göre sırala
                var towns = Town.AllTowns
                    .Where(t => t != _currentTown) // Şu anki şehri hariç tut
                    .OrderBy(t => Campaign.Current.Models.MapDistanceModel.GetDistance(_currentTown.Settlement, t.Settlement))
                    .ToList();
                    
                foreach (Town town in towns)
                {
                    // Şehirler arası mesafeyi hesapla
                    float distance = Campaign.Current.Models.MapDistanceModel.GetDistance(_currentTown.Settlement, town.Settlement);
                    
                    // Ulak maliyetini hesapla (basit formül)
                    int baseCost = Settings.Instance?.CourierBaseCost ?? 100;
                    float distanceMultiplier = Settings.Instance?.CourierDistanceMultiplier ?? 0.5f;
                    int cost = baseCost + (int)(distance * distanceMultiplier);
                    
                    // Maliyet için yeterli altın var mı kontrol et
                    bool canAfford = Hero.MainHero.Gold >= cost;
                    
                    // Seçenek metin
                    string optionText = new TextObject("{=CourierDestinationOption}{TOWN_NAME} - {DISTANCE}km ({COST} Dinar)")
                        .SetTextVariable("TOWN_NAME", town.Name)
                        .SetTextVariable("DISTANCE", ((int)distance).ToString())
                        .SetTextVariable("COST", cost.ToString())
                        .ToString();
                    
                    // İpucu metin
                    string hintText = new TextObject("{=CourierDestinationDesc}{TOWN_NAME} şehrine ulak göndermek {COST} Dinar. Bu şehirdeki ticaret malı fiyatlarını öğrenebilirsiniz.")
                        .SetTextVariable("TOWN_NAME", town.Name)
                        .SetTextVariable("COST", cost.ToString())
                        .ToString();
                    
                    // Seçeneği ekle
                    townOptions.Add(new InquiryElement(
                        town.StringId, // Town ID'sini tanımlayıcı olarak kullan
                        optionText,
                        null,
                        canAfford, // Eğer yeterli altın yoksa seçilemez
                        hintText
                    ));
                }
                
                // Önce mevcut menüyü kapat
                _closeAction?.Invoke();
                
                // Hedef şehri seçme ekranı
                MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(
                    new TextObject("{=CourierSendTitle}Ulak Gönder").ToString(),                // Başlık
                    new TextObject("{=CourierSendDesc}{ORIGIN_TOWN} şehrinden hangi şehire ulak göndermek istiyorsunuz?")
                        .SetTextVariable("ORIGIN_TOWN", _currentTown.Name)
                        .ToString(),                                                             // Açıklama
                    townOptions,                                                                 // Şehir seçenekleri
                    true,                                                                        // Çıkış butonu göster
                    0,                                                                           // Min seçim
                    1,                                                                           // Max seçim
                    new TextObject("{=CourierSendConfirm}Gönder").ToString(),                   // Onay butonu
                    new TextObject("{=CourierSendCancel}İptal").ToString(),                     // İptal butonu
                    (List<InquiryElement> selectedOptions) =>                                    // Seçim yapıldığında
                    {
                        if (selectedOptions.Count > 0)
                        {
                            string selectedTownId = selectedOptions[0].Identifier as string;
                            if (!string.IsNullOrEmpty(selectedTownId))
                            {
                                Town destinationTown = Town.AllTowns.FirstOrDefault(t => t.StringId == selectedTownId);
                                if (destinationTown != null)
                                {
                                    ShowCourierConfirmation(destinationTown);
                                }
                            }
                        }
                    },
                    null,                                                                        // İptal edildiğinde
                    "",                                                                          // Ses
                    false                                                                        // Arama özelliği
                ));
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    $"Ulak gönderme işlemi sırasında hata: {ex.Message}", Colors.Red));
            }
        }
        
        /// <summary>
        /// Ulak gönderme onayı sorar
        /// </summary>
        private void ShowCourierConfirmation(Town destinationTown)
        {
            try
            {
                // Şehirler arası mesafeyi hesapla
                float distance = Campaign.Current.Models.MapDistanceModel.GetDistance(_currentTown.Settlement, destinationTown.Settlement);
                
                // Ulak maliyetini hesapla
                int baseCost = Settings.Instance?.CourierBaseCost ?? 100;
                float distanceMultiplier = Settings.Instance?.CourierDistanceMultiplier ?? 0.5f;
                int cost = baseCost + (int)(distance * distanceMultiplier);
                
                // Onay sor
                InformationManager.ShowInquiry(
                    new InquiryData(
                        new TextObject("{=CourierConfirmTitle}Ulak Gönder").ToString(),                              // Başlık
                        new TextObject("{=CourierConfirmQuestion}{DESTINATION_TOWN} şehrine {COST} Dinar karşılığında ulak göndermek istiyor musunuz?")
                            .SetTextVariable("DESTINATION_TOWN", destinationTown.Name)
                            .SetTextVariable("COST", cost)
                            .ToString(),                                                                             // Açıklama
                        true,                                                                                        // Onay butonu göster
                        true,                                                                                        // İptal butonu göster
                        new TextObject("{=CourierConfirmYes}Evet, Gönder").ToString(),                              // Onay butonu metni
                        new TextObject("{=CourierConfirmNo}Hayır, Vazgeç").ToString(),                              // İptal butonu metni
                        () => SendCourierToTown(destinationTown, cost),                                              // Onaylandığında
                        null,                                                                                        // İptal edildiğinde
                        "",                                                                                          // Ses
                        0f                                                                                           // Bekleme süresi
                    )
                );
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    $"Ulak gönderme onayı sırasında hata: {ex.Message}", Colors.Red));
            }
        }
        
        /// <summary>
        /// Ulak gönderme işlemini gerçekleştirir
        /// </summary>
        private void SendCourierToTown(Town destinationTown, int cost)
        {
            try
            {
                // Oyuncunun parası yeterli mi kontrol et
                if (Hero.MainHero.Gold < cost)
                {
                    InformationManager.DisplayMessage(new InformationMessage(
                        new TextObject("{=CourierNotEnoughGold}Ulak göndermek için yeterli altınınız yok. {COST} Dinar gerekiyor.")
                            .SetTextVariable("COST", cost)
                            .ToString(),
                        Colors.Red));
                    return;
                }
                
                // Parayı oyuncudan al
                Hero.MainHero.ChangeHeroGold(-cost);
                
                // Ulak gönder
                bool success = _campaignBehavior.CreateAndSendCourier(_currentTown, destinationTown, cost);
                
                if (success)
                {
                    // Hesaplanan varış süresi
                    CampaignTime arrivalTime = _campaignBehavior.CalculateCourierArrivalTime(_currentTown, destinationTown);
                    int days = (int)arrivalTime.ToHours / 24;
                    
                    InformationManager.DisplayMessage(new InformationMessage(
                        new TextObject("{=CourierSentSuccess}{DESTINATION_TOWN} şehrine ulaşması yaklaşık {DAYS} gün sürecek bir ulak gönderildi.")
                            .SetTextVariable("DESTINATION_TOWN", destinationTown.Name)
                            .SetTextVariable("DAYS", days)
                            .ToString(),
                        Colors.Green));
                }
                else
                {
                    // Hata durumunda para iadesi
                    Hero.MainHero.ChangeHeroGold(cost);
                    
                    InformationManager.DisplayMessage(new InformationMessage(
                        new TextObject("{=CourierSendFailed}Ulak gönderme işlemi başarısız oldu. Paranız iade edildi.").ToString(),
                        Colors.Red));
                }
            }
            catch (Exception ex)
            {
                // Hata durumunda para iadesi
                Hero.MainHero.ChangeHeroGold(cost);
                
                InformationManager.DisplayMessage(new InformationMessage(
                    $"Ulak gönderme işlemi sırasında hata: {ex.Message}", Colors.Red));
            }
        }
        
        /// <summary>
        /// Dükkan bilgilerini gösterir
        /// </summary>
        private void ShowShopDetails()
        {
            if (_shop == null)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    "Bu şehirde bir dükkanınız bulunmuyor.", Colors.Red));
                return;
            }
            
            // Dükkanın durumunu metni
            string detailText = _shop.GetStatusText();
            
            // Bilgi penceresi göster
            InformationManager.ShowInquiry(
                new InquiryData(
                    $"{_currentTown.Name} Dükkan Bilgileri",     // titleText
                    detailText,                                   // descriptionText
                    true,                                         // isAffirmativeOptionShown
                    false,                                        // isNegativeOptionShown
                    "Tamam",                                      // affirmativeText
                    null,                                         // negativeText
                    null,                                         // onAffirmativeClicked
                    null,                                         // onNegativeClicked
                    "",                                           // soundEventPath
                    0f                                            // waitTime
                )
            );
            
            ManageShop();
        }
    }
} 