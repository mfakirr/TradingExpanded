using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;
using TaleWorlds.Library;
using TradingExpanded.Helpers;
using TaleWorlds.ObjectSystem;

namespace TradingExpanded.Models
{
    /// <summary>
    /// Uzak şehirlerden fiyat bilgisi toplayan kuryeyi temsil eder
    /// </summary>
    public class Courier
    {
        #region Kurye Durumları
        
        /// <summary>
        /// Kuryenin mevcut durumu
        /// </summary>
        public enum CourierState
        {
            /// <summary>Henüz yola çıkmamış</summary>
            Ready,
            
            /// <summary>Hedef şehre gidiyor</summary>
            Traveling,
            
            /// <summary>Hedef şehirde fiyat bilgisi topluyor</summary>
            Gathering,
            
            /// <summary>Geri dönüyor</summary>
            Returning,
            
            /// <summary>Görevi tamamladı, bilgi getirdi</summary>
            Delivered,
            
            /// <summary>Kuryenin başına bir şey geldi veya kayboldu</summary>
            Lost
        }
        
        #endregion
        
        #region Özellikler
        
        [SaveableProperty(1)]
        public string Id { get; private set; }
        
        [SaveableProperty(2)]
        public Town OriginTown { get; private set; }
        
        [SaveableProperty(3)]
        public Town DestinationTown { get; private set; }
        
        [SaveableProperty(4)]
        public CourierState State { get; private set; }
        
        [SaveableProperty(5)]
        public CampaignTime DepartureTime { get; private set; }
        
        [SaveableProperty(6)]
        public CampaignTime ExpectedReturnTime { get; private set; }
        
        [SaveableProperty(7)]
        public Dictionary<ItemObject, int> PriceInfo { get; private set; }
        
        [SaveableProperty(8)]
        public int CourierSkill { get; private set; }
        
        [SaveableProperty(9)]
        public float JourneyProgress { get; private set; }
        
        [SaveableProperty(10)]
        public bool HasReturnedInfo { get; private set; }
        
        [SaveableProperty(11)]
        public int Cost { get; private set; }
        
        #endregion
        
        #region Hesaplanan Özellikler
        
        /// <summary>
        /// Kuryenin bulunduğu yerleşim (yolculuk durumuna bağlı)
        /// </summary>
        public Settlement CurrentLocation
        {
            get
            {
                switch (State)
                {
                    case CourierState.Ready:
                    case CourierState.Delivered:
                        return OriginTown.Settlement;
                    case CourierState.Gathering:
                        return DestinationTown.Settlement;
                    case CourierState.Traveling:
                        // Yolda, yarı yolda olduğunu varsayalım
                        return null;
                    case CourierState.Returning:
                        // Yolda, yarı yolda olduğunu varsayalım
                        return null;
                    case CourierState.Lost:
                        return null;
                    default:
                        return null;
                }
            }
        }
        
        /// <summary>
        /// Hedef şehre olan mesafe (km)
        /// </summary>
        public float Distance => OriginTown.Settlement.Position2D.Distance(DestinationTown.Settlement.Position2D) / 1000f;
        
        /// <summary>
        /// Yolculuğun yaklaşık süresi (gün)
        /// </summary>
        public float TravelDuration => Distance / 20f; // Ortalama günlük seyahat hızı 20km
        
        /// <summary>
        /// Bir yöne gidiş süresi (saat)
        /// </summary>
        public float OneWayTravelHours => TravelDuration * 24f;
        
        /// <summary>
        /// Yolculuğun tamamlanma yüzdesi
        /// </summary>
        public float CompletionPercentage
        {
            get
            {
                switch (State)
                {
                    case CourierState.Ready:
                        return 0f;
                    case CourierState.Traveling:
                        return JourneyProgress * 0.45f; // %0-%45 arası
                    case CourierState.Gathering:
                        return 45f + (JourneyProgress * 0.1f); // %45-%55 arası
                    case CourierState.Returning:
                        return 55f + (JourneyProgress * 0.45f); // %55-%100 arası
                    case CourierState.Delivered:
                    case CourierState.Lost:
                        return 100f;
                    default:
                        return 0f;
                }
            }
        }
        
        /// <summary>
        /// Kuryenin mevcut durum açıklaması
        /// </summary>
        public string StatusDescription
        {
            get
            {
                switch (State)
                {
                    case CourierState.Ready:
                        return "Hazır (yola çıkmamış)";
                    case CourierState.Traveling:
                        return $"{DestinationTown.Name}'a gidiyor (%{JourneyProgress * 100:F0})";
                    case CourierState.Gathering:
                        return $"{DestinationTown.Name}'da bilgi topluyor";
                    case CourierState.Returning:
                        return $"{OriginTown.Name}'a dönüyor (%{JourneyProgress * 100:F0})";
                    case CourierState.Delivered:
                        return "Görev tamamlandı, bilgiler alındı";
                    case CourierState.Lost:
                        return "Kurye kayboldu";
                    default:
                        return "Bilinmiyor";
                }
            }
        }
        
        /// <summary>
        /// Kalan tahmini dönüş süresi (saat)
        /// </summary>
        public float RemainingHours
        {
            get
            {
                if (State == CourierState.Delivered || State == CourierState.Lost)
                    return 0f;
                
                var hoursLeft = ExpectedReturnTime.ToHours - CampaignTime.Now.ToHours;
                return (float)Math.Max(0.0, hoursLeft);
            }
        }
        
        /// <summary>
        /// Kalan tahmini dönüş süresi (gün)
        /// </summary>
        public float RemainingDays => RemainingHours / 24f;
        
        #endregion
        
        #region Yapılandırıcılar
        
        /// <summary>
        /// Default constructor for saving/loading
        /// </summary>
        public Courier()
        {
            PriceInfo = new Dictionary<ItemObject, int>();
        }
        
        /// <summary>
        /// Creates a new courier to gather information
        /// </summary>
        public Courier(Town originTown, Town destinationTown, int courierSkill = 50)
        {
            if (originTown == null || destinationTown == null)
                throw new ArgumentNullException("Köken ve hedef şehirler belirtilmelidir.");
                
            Id = Guid.NewGuid().ToString();
            OriginTown = originTown;
            DestinationTown = destinationTown;
            CourierSkill = courierSkill.Clamp(10, 100);
            State = CourierState.Ready;
            JourneyProgress = 0f;
            HasReturnedInfo = false;
            PriceInfo = new Dictionary<ItemObject, int>();
            
            // Kurye maliyetini hesapla
            CalculateCost();
        }
        
        #endregion
        
        #region Metotlar
        
        /// <summary>
        /// Kuryenin yolculuğa başlamasını sağlar
        /// </summary>
        public void StartJourney()
        {
            if (State != CourierState.Ready)
                return;
                
            State = CourierState.Traveling;
            DepartureTime = CampaignTime.Now;
            JourneyProgress = 0f;
            
            // Yolculuk süresini hesapla ve geri dönüş zamanını belirle
            // Gidiş + bilgi toplama (½ gün) + dönüş
            float totalHours = (OneWayTravelHours * 2) + 12f;
            ExpectedReturnTime = DepartureTime.AddHours(totalHours);
        }
        
        /// <summary>
        /// Kuryenin günlük durumunu günceller
        /// </summary>
        public void UpdateCourier()
        {
            // Yalnızca aktif kuryeler için güncelleme yap
            if (State == CourierState.Delivered || State == CourierState.Lost || State == CourierState.Ready)
                return;
                
            // Kurye yolculuk süresince ilerleme
            var elapsedHours = (CampaignTime.Now - DepartureTime).ToHours;
            var totalExpectedHours = (ExpectedReturnTime - DepartureTime).ToHours;
            
            // Risk hesaplama
            float baseRiskPerDay = 0.01f; // %1 günlük temel risk
            float riskFactor = Settings.Instance?.CourierRiskFactor ?? 0.1f;
            float skillProtection = CourierSkill / 100f; // Beceri yükseldikçe risk azalır
            
            float dailyRisk = baseRiskPerDay * riskFactor * (1f - (skillProtection * 0.8f));
            
            // Kurye kaybolabilir
            if (MBRandom.RandomFloat <= dailyRisk && State != CourierState.Lost)
            {
                State = CourierState.Lost;
                HasReturnedInfo = false;
                return;
            }
            
            // Kuryenin mevcut yolculuk aşamasını güncelle
            switch (State)
            {
                case CourierState.Traveling:
                    // Gidiş aşaması
                    float travelHours = OneWayTravelHours;
                    JourneyProgress = (float)Math.Min(1.0, elapsedHours / travelHours);
                    
                    // Hedef şehre vardı mı?
                    if (JourneyProgress >= 1.0f)
                    {
                        State = CourierState.Gathering;
                        JourneyProgress = 0f;
                    }
                    break;
                    
                case CourierState.Gathering:
                    // Bilgi toplama aşaması (yaklaşık yarım gün)
                    float gatheringHours = 12f;
                    float gatheringStartHour = OneWayTravelHours;
                    
                    JourneyProgress = (float)Math.Min(1.0, (elapsedHours - gatheringStartHour) / gatheringHours);
                    
                    // Bilgi toplama tamamlandı mı?
                    if (JourneyProgress >= 1.0f)
                    {
                        State = CourierState.Returning;
                        JourneyProgress = 0f;
                        
                        // Fiyat bilgilerini topla
                        GatherPriceInformation();
                    }
                    break;
                    
                case CourierState.Returning:
                    // Dönüş aşaması
                    float returnStartHour = OneWayTravelHours + 12f; // Gidiş + bilgi toplama
                    
                    JourneyProgress = (float)Math.Min(1.0, (elapsedHours - returnStartHour) / OneWayTravelHours);
                    
                    // Dönüş tamamlandı mı?
                    if (JourneyProgress >= 1.0f)
                    {
                        State = CourierState.Delivered;
                        JourneyProgress = 1.0f;
                        HasReturnedInfo = true;
                    }
                    break;
            }
        }
        
        /// <summary>
        /// Hedef şehirde fiyat bilgilerini toplar
        /// </summary>
        private void GatherPriceInformation()
        {
            PriceInfo.Clear();
            
            // Tüm ticari eşyalar için fiyat bilgisi topla
            var items = MBObjectManager.Instance.GetObjectTypeList<ItemObject>()
                .Where(item => 
                    item.ItemCategory != DefaultItemCategories.Horse && 
                    item.ItemCategory != DefaultItemCategories.WarHorse &&
                    item.ItemCategory != DefaultItemCategories.LightArmor &&
                    item.ItemCategory != DefaultItemCategories.MediumArmor &&
                    item.ItemCategory != DefaultItemCategories.HeavyArmor && 
                    item.ItemCategory != DefaultItemCategories.UltraArmor &&
                    item.ItemCategory != DefaultItemCategories.MeleeWeapons1 &&
                    item.ItemCategory != DefaultItemCategories.MeleeWeapons2 &&
                    item.ItemCategory != DefaultItemCategories.MeleeWeapons3 &&
                    item.ItemCategory != DefaultItemCategories.MeleeWeapons4 &&
                    item.ItemCategory != DefaultItemCategories.MeleeWeapons5 &&
                    item.ItemCategory != DefaultItemCategories.RangedWeapons1 &&
                    item.ItemCategory != DefaultItemCategories.RangedWeapons2 &&
                    item.ItemCategory != DefaultItemCategories.RangedWeapons3 &&
                    item.ItemCategory != DefaultItemCategories.RangedWeapons4 &&
                    item.ItemCategory != DefaultItemCategories.RangedWeapons5 &&
                    item.Value > 0);
                    
            foreach (var item in items)
            {
                int price = DestinationTown.GetItemPrice(item);
                
                // Kurye becerisine göre fiyat bilgisi doğruluğu
                float accuracyFactor = CourierSkill / 100f;
                
                // %80-%120 arasında rastgele bir hata payı ekle, beceri arttıkça hata azalır
                float errorRange = 0.4f * (1f - accuracyFactor);
                float errorFactor = 1.0f + (MBRandom.RandomFloat * errorRange) - (errorRange / 2f);
                
                // Gerçek fiyatı beceri seviyesine göre değiştir
                int reportedPrice = (int)(price * errorFactor);
                
                PriceInfo[item] = reportedPrice;
            }
        }
        
        /// <summary>
        /// Kuryenin maliyetini hesaplar
        /// </summary>
        private void CalculateCost()
        {
            float baseCost = Settings.Instance?.CourierBaseCost ?? 100;
            float distanceMultiplier = Settings.Instance?.CourierDistanceMultiplier ?? 0.5f;
            
            // Temel ücret + mesafe başına ek ücret + beceri seviyesi başına ek ücret
            Cost = (int)(baseCost + (Distance * distanceMultiplier) + (CourierSkill * 1.5f));
        }
        
        /// <summary>
        /// Kurye hakkında özet bilgi döndürür
        /// </summary>
        /// <returns>Özet bilgi metni</returns>
        public string GetSummary()
        {
            string summary = $"Kurye: {OriginTown.Name} → {DestinationTown.Name}\n";
            summary += $"Durum: {StatusDescription}\n";
            summary += $"Mesafe: {Distance:F1} km\n";
            summary += $"Beceri: {CourierSkill}/100\n";
            
            if (State == CourierState.Traveling || State == CourierState.Gathering || State == CourierState.Returning)
            {
                summary += $"İlerleme: %{CompletionPercentage:F0}\n";
                summary += $"Tahmini varış: {RemainingDays:F1} gün sonra\n";
            }
            else if (State == CourierState.Delivered)
            {
                summary += $"Getirilen fiyat bilgisi: {PriceInfo.Count} eşya\n";
            }
            
            return summary;
        }
        
        #endregion
    }
} 