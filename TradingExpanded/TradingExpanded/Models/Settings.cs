using System;
using System.Xml.Serialization;
using System.IO;
using TaleWorlds.Library;
using TaleWorlds.Core;

namespace TradingExpanded.Models
{
    /// <summary>
    /// Modu konfigüre etmek için ayarlar sınıfı
    /// </summary>
    public class Settings
    {
        private static Settings _instance;
        
        /// <summary>
        /// Settings sınıfının Singleton örneğini döndürür
        /// </summary>
        public static Settings Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = LoadSettings();
                }
                return _instance;
            }
        }
        
        #region Mod Ayarları
        
        // Genel Ayarlar
        [XmlElement("IsEnabled")]
        public bool IsEnabled { get; set; } = true;
        
        [XmlElement("DebugMode")]
        public bool DebugMode { get; set; } = false;
        
        // Toptan Satış Mağazası Ayarları
        [XmlElement("MaxWholesaleShops")]
        public int MaxWholesaleShops { get; set; } = 3;
        
        [XmlElement("WholesaleMinimumCapital")]
        public int WholesaleMinimumCapital { get; set; } = 5000;
        
        [XmlElement("WholesaleProfitMargin")]
        public float WholesaleProfitMargin { get; set; } = 0.15f;
        
        [XmlElement("WholesaleMaxCapital")]
        public int WholesaleMaxCapital { get; set; } = 100000;
        
        // Kervan Ayarları
        [XmlElement("MaxCaravans")]
        public int MaxCaravans { get; set; } = 3;
        
        [XmlElement("CaravanInitialCapital")]
        public int CaravanInitialCapital { get; set; } = 5000;
        
        [XmlElement("CaravanTravelSpeed")]
        public float CaravanTravelSpeed { get; set; } = 1.0f;
        
        [XmlElement("CaravanMaxCapital")]
        public int CaravanMaxCapital { get; set; } = 50000;
        
        // Kurye Ayarları
        [XmlElement("MaxCouriers")]
        public int MaxCouriers { get; set; } = 5;
        
        [XmlElement("CourierBaseCost")]
        public int CourierBaseCost { get; set; } = 100;
        
        [XmlElement("CourierDistanceMultiplier")]
        public float CourierDistanceMultiplier { get; set; } = 0.5f;
        
        [XmlElement("CourierRiskFactor")]
        public float CourierRiskFactor { get; set; } = 0.1f;
        
        // Tüccar İlişkileri Ayarları
        [XmlElement("MaxAgreementsPerMerchant")]
        public int MaxAgreementsPerMerchant { get; set; } = 3;
        
        [XmlElement("RelationshipDecayRate")]
        public float RelationshipDecayRate { get; set; } = 0.1f;
        
        [XmlElement("RelationshipGainFromTrade")]
        public float RelationshipGainFromTrade { get; set; } = 0.5f;
        
        // Fiyat ve Envanter Takip Ayarları
        [XmlElement("MaxPriceHistoryDays")]
        public int MaxPriceHistoryDays { get; set; } = 60;
        
        [XmlElement("PriceHistoryEnabled")]
        public bool PriceHistoryEnabled { get; set; } = true;
        
        [XmlElement("PriceVolatility")]
        public float PriceVolatility { get; set; } = 1.0f;
        
        #endregion
        
        /// <summary>
        /// Ayarları varsayılan değerlere sıfırlar
        /// </summary>
        public void ResetToDefaults()
        {
            IsEnabled = true;
            DebugMode = false;
            
            MaxWholesaleShops = 3;
            WholesaleMinimumCapital = 5000;
            WholesaleProfitMargin = 0.15f;
            WholesaleMaxCapital = 100000;
            
            MaxCaravans = 3;
            CaravanInitialCapital = 5000;
            CaravanTravelSpeed = 1.0f;
            CaravanMaxCapital = 50000;
            
            MaxCouriers = 5;
            CourierBaseCost = 100;
            CourierDistanceMultiplier = 0.5f;
            CourierRiskFactor = 0.1f;
            
            MaxAgreementsPerMerchant = 3;
            RelationshipDecayRate = 0.1f;
            RelationshipGainFromTrade = 0.5f;
            
            MaxPriceHistoryDays = 60;
            PriceHistoryEnabled = true;
            PriceVolatility = 1.0f;
            
            SaveSettings();
        }
        
        /// <summary>
        /// Ayarları XML dosyasından yükler
        /// </summary>
        private static Settings LoadSettings()
        {
            string settingsPath = GetSettingsFilePath();
            
            if (File.Exists(settingsPath))
            {
                try
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(Settings));
                    using (FileStream fileStream = new FileStream(settingsPath, FileMode.Open))
                    {
                        return (Settings)serializer.Deserialize(fileStream);
                    }
                }
                catch (Exception ex)
                {
                    Debug.Print("TradingExpanded: Ayarlar yüklenirken hata oluştu: " + ex.Message);
                    return new Settings();
                }
            }
            else
            {
                // Ayar dosyası bulunamadı, yeni bir tane oluştur
                Settings settings = new Settings();
                settings.SaveSettings();
                return settings;
            }
        }
        
        /// <summary>
        /// Ayarları XML dosyasına kaydeder
        /// </summary>
        public void SaveSettings()
        {
            string settingsPath = GetSettingsFilePath();
            
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(Settings));
                using (FileStream fileStream = new FileStream(settingsPath, FileMode.Create))
                {
                    serializer.Serialize(fileStream, this);
                }
            }
            catch (Exception ex)
            {
                Debug.Print("TradingExpanded: Ayarlar kaydedilirken hata oluştu: " + ex.Message);
            }
        }
        
        /// <summary>
        /// Ayar dosyasının yolunu döndürür
        /// </summary>
        private static string GetSettingsFilePath()
        {
            string configDir = Path.Combine(BasePath.Name, "Modules", "TradingExpanded", "Config");
            
            // Dizin yoksa oluştur
            if (!Directory.Exists(configDir))
            {
                Directory.CreateDirectory(configDir);
            }
            
            return Path.Combine(configDir, "TradingExpandedSettings.xml");
        }
    }
} 