using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.SaveSystem;
using TradingExpanded.Helpers;

namespace TradingExpanded.Models
{
    /// <summary>
    /// Tüccarlar ve oyuncu arasındaki ticaret anlaşmalarını temsil eder
    /// </summary>
    public class TradeAgreement
    {
        #region Anlaşma Türleri
        
        public enum AgreementType
        {
            Buy,    // Tüccardan satın alma anlaşması
            Sell    // Tüccara satma anlaşması
        }
        
        #endregion
        
        #region Özellikler
        
        [SaveableProperty(1)]
        public string Id { get; private set; }
        
        [SaveableProperty(2)]
        public AgreementType Type { get; private set; }
        
        [SaveableProperty(3)]
        public ItemObject Item { get; private set; }
        
        [SaveableProperty(4)]
        public int Quantity { get; private set; }
        
        [SaveableProperty(5)]
        public int Price { get; private set; }
        
        [SaveableProperty(6)]
        public CampaignTime StartDate { get; private set; }
        
        [SaveableProperty(7)]
        public CampaignTime EndDate { get; private set; }
        
        [SaveableProperty(8)]
        public int RemainingQuantity { get; private set; }
        
        [SaveableProperty(9)]
        public bool IsActive { get; private set; }
        
        [SaveableProperty(10)]
        public bool IsFulfilled { get; private set; }
        
        #endregion
        
        #region Hesaplanan Özellikler
        
        /// <summary>
        /// Anlaşmanın günlerle ifade edilen toplam süresi
        /// </summary>
        public int DurationInDays => (int)(EndDate.ToHours - StartDate.ToHours) / 24;
        
        /// <summary>
        /// Anlaşmanın tamamlanma yüzdesi
        /// </summary>
        public float CompletionPercentage => RemainingQuantity <= 0 ? 100f : (1f - (float)RemainingQuantity / Quantity) * 100f;
        
        /// <summary>
        /// Anlaşmanın kalan süresi (gün)
        /// </summary>
        public int RemainingDays
        {
            get
            {
                if (!IsActive || IsFulfilled)
                    return 0;
                    
                var hoursLeft = EndDate.ToHours - CampaignTime.Now.ToHours;
                return hoursLeft <= 0 ? 0 : (int)(float)Math.Ceiling(hoursLeft / 24);
            }
        }
        
        /// <summary>
        /// Anlaşmanın süresi dolmuş mu?
        /// </summary>
        public bool IsExpired => CampaignTime.Now >= EndDate;
        
        /// <summary>
        /// Anlaşmanın toplam değeri
        /// </summary>
        public int TotalValue => Quantity * Price;
        
        /// <summary>
        /// Kalan işlemlerin toplam değeri
        /// </summary>
        public int RemainingValue => RemainingQuantity * Price;
        
        #endregion
        
        #region Yapılandırıcılar
        
        private TradeAgreement() 
        {
            // Boş yapılandırıcı - Serileştirme için gerekli
        }
        
        private TradeAgreement(ItemObject item, int quantity, int price, int durationInDays, AgreementType type)
        {
            Id = Guid.NewGuid().ToString();
            Item = item;
            Quantity = quantity;
            Price = price;
            Type = type;
            
            RemainingQuantity = quantity;
            IsActive = true;
            IsFulfilled = false;
            
            StartDate = CampaignTime.Now;
            EndDate = CampaignTime.Now.AddDays(durationInDays);
        }
        
        /// <summary>
        /// Satın alma anlaşması oluşturur (tüccardan satın alınacak)
        /// </summary>
        /// <param name="item">Anlaşmanın yapıldığı eşya</param>
        /// <param name="quantity">Anlaşılan miktar</param>
        /// <param name="price">Birim fiyat</param>
        /// <param name="durationInDays">Anlaşma süresi (gün)</param>
        /// <returns>Oluşturulan anlaşma nesnesi</returns>
        public static TradeAgreement CreateBuyAgreement(ItemObject item, int quantity, int price, int durationInDays)
        {
            return new TradeAgreement(item, quantity, price, durationInDays, AgreementType.Buy);
        }
        
        /// <summary>
        /// Satış anlaşması oluşturur (tüccara satılacak)
        /// </summary>
        /// <param name="item">Anlaşmanın yapıldığı eşya</param>
        /// <param name="quantity">Anlaşılan miktar</param>
        /// <param name="price">Birim fiyat</param>
        /// <param name="durationInDays">Anlaşma süresi (gün)</param>
        /// <returns>Oluşturulan anlaşma nesnesi</returns>
        public static TradeAgreement CreateSellAgreement(ItemObject item, int quantity, int price, int durationInDays)
        {
            return new TradeAgreement(item, quantity, price, durationInDays, AgreementType.Sell);
        }
        
        #endregion
        
        #region Metotlar
        
        /// <summary>
        /// Anlaşma kapsamında bir işlem kaydeder
        /// </summary>
        /// <param name="amount">İşlem miktarı</param>
        /// <returns>İşlem başarılı oldu mu</returns>
        public bool RecordTransaction(int amount)
        {
            if (!IsActive || IsFulfilled || IsExpired || amount <= 0 || amount > RemainingQuantity)
                return false;
                
            RemainingQuantity -= amount;
            
            // Tüm miktar tamamlandı mı kontrol et
            if (RemainingQuantity <= 0)
            {
                RemainingQuantity = 0;
                IsFulfilled = true;
            }
            
            return true;
        }
        
        /// <summary>
        /// Anlaşmayı sonlandırır
        /// </summary>
        /// <param name="fulfillRemainingQuantity">Kalan miktarın tamamlandığı varsayılsın mı?</param>
        public void Terminate(bool fulfillRemainingQuantity = false)
        {
            IsActive = false;
            
            if (fulfillRemainingQuantity)
            {
                RemainingQuantity = 0;
                IsFulfilled = true;
            }
        }
        
        /// <summary>
        /// Anlaşmayı güncelleyerek süre dolmuşsa durumunu değiştirir
        /// </summary>
        public void Update()
        {
            if (!IsActive || IsFulfilled)
                return;
                
            if (IsExpired)
            {
                IsActive = false;
            }
        }
        
        /// <summary>
        /// Anlaşmanın süresini uzatır
        /// </summary>
        /// <param name="additionalDays">Eklenecek gün sayısı</param>
        /// <returns>Uzatma başarılı oldu mu</returns>
        public bool ExtendDuration(int additionalDays)
        {
            if (!IsActive || additionalDays <= 0)
                return false;
                
            EndDate = EndDate.AddDays(additionalDays);
            
            // Eğer anlaşma süresi dolmuşsa tekrar aktif hale getir
            if (IsExpired && CampaignTime.Now < EndDate)
            {
                IsActive = true;
            }
            
            return true;
        }
        
        /// <summary>
        /// Anlaşmanın miktarını artırır
        /// </summary>
        /// <param name="additionalQuantity">Eklenecek miktar</param>
        /// <returns>Artırma başarılı oldu mu</returns>
        public bool IncreaseQuantity(int additionalQuantity)
        {
            if (!IsActive || additionalQuantity <= 0)
                return false;
                
            Quantity += additionalQuantity;
            RemainingQuantity += additionalQuantity;
            
            // Anlaşma tamamlanmış olarak işaretlendiyse, artık tamamlanmamış
            if (IsFulfilled && RemainingQuantity > 0)
            {
                IsFulfilled = false;
            }
            
            return true;
        }
        
        /// <summary>
        /// Anlaşmanın içeriğini açıklayan metin
        /// </summary>
        /// <returns>Anlaşma açıklaması</returns>
        public string GetAgreementDescription()
        {
            string typeStr = Type == AgreementType.Buy ? "Satın Alma" : "Satış";
            string statusStr = IsExpired ? "Süresi Dolmuş" : (IsFulfilled ? "Tamamlandı" : $"Aktif ({RemainingDays} gün kaldı)");
            
            return $"{typeStr} Anlaşması: {Item.Name} - {Quantity} adet ({RemainingQuantity} kaldı) - Birim fiyat: {Price} - Durum: {statusStr}";
        }
        
        #endregion
    }
} 