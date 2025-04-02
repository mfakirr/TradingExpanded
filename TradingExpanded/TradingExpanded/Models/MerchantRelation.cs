using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.SaveSystem;
using TradingExpanded.Helpers;

namespace TradingExpanded.Models
{
    /// <summary>
    /// Tüccarlarla olan ilişkileri ve yapılan anlaşmaları temsil eden sınıf
    /// </summary>
    public class MerchantRelation
    {
        #region Sabitler
        
        private const float MAX_TRUST = 100f;
        private const float MIN_TRUST = 0f;
        
        #endregion
        
        #region Özellikler
        
        [SaveableProperty(1)]
        public string Id { get; private set; }
        
        [SaveableProperty(2)]
        public Hero Merchant { get; private set; }
        
        [SaveableProperty(3)]
        public float Trust { get; private set; }
        
        [SaveableProperty(4)]
        public bool IsActive { get; set; }
        
        [SaveableProperty(5)]
        public int SuccessfulTradeCount { get; private set; }
        
        [SaveableProperty(6)]
        public int FailedTradeCount { get; private set; }
        
        [SaveableProperty(7)]
        public CampaignTime LastTradeDate { get; private set; }
        
        [SaveableProperty(8)]
        public CampaignTime LastVisitDate { get; private set; }
        
        [SaveableProperty(9)]
        public List<TradeAgreement> Agreements { get; private set; }
        
        [SaveableProperty(10)]
        public int TotalTradeVolume { get; private set; }
        
        #endregion
        
        #region Hesaplanan Özellikler
        
        /// <summary>
        /// Aktif anlaşmaların sayısı
        /// </summary>
        public int ActiveAgreementCount => Agreements.Count(a => a.IsActive);
        
        /// <summary>
        /// Tüccarın aktif alım anlaşmaları
        /// </summary>
        public List<TradeAgreement> ActiveBuyAgreements => 
            Agreements.Where(a => a.IsActive && a.Type == TradeAgreement.AgreementType.Buy).ToList();
            
        /// <summary>
        /// Tüccarın aktif satış anlaşmaları
        /// </summary>
        public List<TradeAgreement> ActiveSellAgreements => 
            Agreements.Where(a => a.IsActive && a.Type == TradeAgreement.AgreementType.Sell).ToList();
            
        /// <summary>
        /// Tüccar ile son ticaretin üzerinden geçen gün sayısı
        /// </summary>
        public int DaysSinceLastTrade => 
            LastTradeDate == CampaignTime.Zero ? 
            int.MaxValue : 
            (int)(CampaignTime.Now - LastTradeDate).ToDays;
            
        /// <summary>
        /// Tüccarı son ziyaretin üzerinden geçen gün sayısı
        /// </summary>
        public int DaysSinceLastVisit => 
            LastVisitDate == CampaignTime.Zero ? 
            int.MaxValue : 
            (int)(CampaignTime.Now - LastVisitDate).ToDays;
            
        /// <summary>
        /// Tüccarla ilişki genel durumu
        /// </summary>
        public string TrustStatus
        {
            get
            {
                if (Trust >= 80f) return "Çok İyi";
                if (Trust >= 60f) return "İyi";
                if (Trust >= 40f) return "Normal";
                if (Trust >= 20f) return "Düşük";
                return "Çok Düşük";
            }
        }
        
        /// <summary>
        /// Tüccarla yeni anlaşma yapılabilir mi?
        /// </summary>
        public bool CanMakeAgreement()
        {
            int maxAgreements = Settings.Instance?.MaxAgreementsPerMerchant ?? 3;
            
            // Maksimum anlaşma sayısını ve minimum güven düzeyini kontrol et
            return IsActive && 
                   ActiveAgreementCount < maxAgreements && 
                   Trust >= 20f;
        }
        
        #endregion
        
        #region Yapılandırıcılar
        
        /// <summary>
        /// Default constructor for saving/loading
        /// </summary>
        public MerchantRelation()
        {
            Agreements = new List<TradeAgreement>();
        }
        
        /// <summary>
        /// Creates a new merchant relationship
        /// </summary>
        public MerchantRelation(Hero merchant)
        {
            if (merchant == null)
                throw new ArgumentNullException(nameof(merchant));
                
            Merchant = merchant;
            Id = merchant.StringId;
            Trust = 30f; // Başlangıç güven değeri
            IsActive = true;
            SuccessfulTradeCount = 0;
            FailedTradeCount = 0;
            LastTradeDate = CampaignTime.Zero;
            LastVisitDate = CampaignTime.Zero;
            Agreements = new List<TradeAgreement>();
            TotalTradeVolume = 0;
        }
        
        #endregion
        
        #region Metotlar
        
        /// <summary>
        /// Tüccarla olan güven ilişkisini belirtilen miktar kadar artırır
        /// </summary>
        /// <param name="amount">Artırılacak miktar</param>
        public void ImproveTrust(float amount)
        {
            if (amount <= 0f)
                return;
                
            Trust = Math.Min(MAX_TRUST, Trust + amount);
        }
        
        /// <summary>
        /// Tüccarla olan güven ilişkisini belirtilen miktar kadar azaltır
        /// </summary>
        /// <param name="amount">Azaltılacak miktar</param>
        public void ReduceTrust(float amount)
        {
            if (amount <= 0f)
                return;
                
            Trust = Math.Max(MIN_TRUST, Trust - amount);
        }
        
        /// <summary>
        /// Başarılı bir ticaret işlemini kaydeder ve ilişkiyi iyileştirir
        /// </summary>
        /// <param name="value">Ticaretin değeri</param>
        public void RecordSuccessfulTrade(int value)
        {
            if (value <= 0)
                return;
                
            SuccessfulTradeCount++;
            LastTradeDate = CampaignTime.Now;
            TotalTradeVolume += value;
            
            // Ticaret değerine göre güveni artır
            float trustGain = Math.Min(10f, value / 1000f);
            trustGain *= Settings.Instance?.RelationshipGainFromTrade ?? 0.5f;
            
            ImproveTrust(trustGain);
        }
        
        /// <summary>
        /// Başarısız bir ticaret işlemini kaydeder ve ilişkiyi kötüleştirir
        /// </summary>
        /// <param name="value">Başarısız ticaretin değeri</param>
        public void RecordFailedTrade(int value)
        {
            if (value <= 0)
                return;
                
            FailedTradeCount++;
            LastTradeDate = CampaignTime.Now;
            
            // Ticaret değerine göre güveni azalt
            float trustLoss = Math.Min(15f, value / 800f);
            
            ReduceTrust(trustLoss);
        }
        
        /// <summary>
        /// Tüccarın ziyaret edildiğini kaydeder
        /// </summary>
        public void RecordVisit()
        {
            LastVisitDate = CampaignTime.Now;
            
            // Uzun süreli ziyaret etmeme durumunda güven kaybını telafi et
            if (DaysSinceLastVisit > 30)
            {
                ImproveTrust(5f);
            }
        }
        
        /// <summary>
        /// Aktif anlaşmaları günceller ve zamanı geçmiş olanları kapatır
        /// </summary>
        public void UpdateAgreements()
        {
            foreach (var agreement in Agreements)
            {
                agreement.Update();
            }
        }
        
        /// <summary>
        /// İlişkiyi ve anlaşmaları günceller
        /// </summary>
        public void Update()
        {
            if (!IsActive)
                return;
                
            // Tüccarla olan anlaşmaları güncelle
            UpdateAgreements();
            
            // Uzun süre ticaret yapılmadığında güven azalması
            if (DaysSinceLastTrade > 14 && Trust > MIN_TRUST)
            {
                float decayRate = Settings.Instance?.RelationshipDecayRate ?? 0.1f;
                ReduceTrust(decayRate);
            }
            
            // Tüccar artık mevcut değilse ilişkiyi pasif yap
            if (Merchant == null || !Merchant.IsAlive)
            {
                IsActive = false;
                
                // Aktif anlaşmaları sonlandır
                foreach (var agreement in Agreements.Where(a => a.IsActive))
                {
                    agreement.Terminate();
                }
            }
        }
        
        /// <summary>
        /// Tüccarla yapılabilecek anlaşma fiyat teklifini hesaplar
        /// </summary>
        /// <param name="basePrice">Eşyanın temel fiyatı</param>
        /// <param name="isBuyAgreement">Alım anlaşması mı?</param>
        /// <returns>Teklif edilecek fiyat</returns>
        public int CalculateOfferPrice(int basePrice, bool isBuyAgreement)
        {
            if (basePrice <= 0)
                return 0;
                
            float trustFactor = Trust / 100f; // 0.0 - 1.0 arası değer
            
            // Alım anlaşmasında fiyat düşer, satım anlaşmasında artar
            if (isBuyAgreement)
            {
                // Güven yükseldikçe daha uygun fiyat
                float discount = 0.05f + (trustFactor * 0.1f); // %5 - %15 arası indirim
                return (int)(basePrice * (1.0f - discount));
            }
            else
            {
                // Güven yükseldikçe daha iyi satış fiyatı
                float premium = 0.05f + (trustFactor * 0.1f); // %5 - %15 arası fiyat artışı
                return (int)(basePrice * (1.0f + premium));
            }
        }
        
        /// <summary>
        /// Tüccarla yeni anlaşma yapılabilir mi kontrol eder ve güvenin yeterli olup olmadığını bildirir
        /// </summary>
        /// <param name="requiredTrust">Gereken minimum güven seviyesi</param>
        /// <returns>Anlaşma yapılabilir mi durumu</returns>
        public bool CanMakeAgreementWithTrust(float requiredTrust)
        {
            return CanMakeAgreement() && Trust >= requiredTrust;
        }
        
        /// <summary>
        /// Tüccarla ilişki hakkında detaylı bilgi sağlar
        /// </summary>
        /// <returns>İlişki hakkında bilgi metni</returns>
        public string GetRelationshipSummary()
        {
            string summary = $"{Merchant.Name} ile İlişki\n";
            summary += $"Güven Düzeyi: {Trust:F1} ({TrustStatus})\n";
            summary += $"Anlaşmalar: {ActiveAgreementCount} aktif / {Agreements.Count} toplam\n";
            summary += $"Başarılı İşlemler: {SuccessfulTradeCount}, Başarısız İşlemler: {FailedTradeCount}\n";
            summary += $"Toplam Ticaret Hacmi: {TotalTradeVolume} dinar\n";
            
            if (LastTradeDate != CampaignTime.Zero)
                summary += $"Son Ticaret: {DaysSinceLastTrade} gün önce\n";
                
            if (LastVisitDate != CampaignTime.Zero)
                summary += $"Son Ziyaret: {DaysSinceLastVisit} gün önce\n";
                
            return summary;
        }
        
        #endregion
    }
} 