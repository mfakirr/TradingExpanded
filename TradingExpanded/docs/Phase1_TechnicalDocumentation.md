## Teknik Dokümantasyon - Faz 3

### Genel Bakış
Bu dokümantasyon, Trading Expanded modunun teknik detaylarını içerir.

### Ana Bileşenler

#### 1. TradingExpandedCampaignBehavior
- Modun ana davranış sınıfı
- Oyun olaylarını yönetir
- Menü sistemini kontrol eder
- Veri yönetimini sağlar

#### 2. Menü Sistemi
- Şehir menüsüne entegre edilmiş
- Çoklu dil desteği
- Dinamik menü seçenekleri
- Kullanıcı dostu arayüz

#### 3. Veri Modelleri
- WholesaleShop: Toptan satış dükkanı veri modeli
- TradeCaravan: Kervan veri modeli
- Courier: Kurye veri modeli
- MerchantRelation: Tüccar ilişkileri modeli

### Teknik Detaylar

#### Menü Sistemi İmplementasyonu
```csharp
// Ana menü kaydı
campaignGameStarter.AddGameMenu(
    "wholesale_shop_menu",
    "{=WholesaleShopMenuTitle}Toptan Satış Dükkanı",
    MenuCallbackArgs args => { ... }
);

// Menü seçenekleri
campaignGameStarter.AddGameMenuOption(
    "town",
    "wholesale_shop_option",
    "{=WholesaleShopMenuText}Toptan Satış Dükkanı",
    args => { ... },
    args => { ... }
);
```

#### Çeviri Sistemi
- ModuleData/Languages altında XML tabanlı çeviri dosyaları
- Her dil için ayrı klasör
- String ID sistemi ile kolay yönetim
- TextObject kullanımı ile dinamik içerik

```csharp
// Çeviri örneği
new TextObject("{=WholesaleShopNewOption}Yeni Dükkan Kur ({GOLD} Dinar)")
    .SetTextVariable("GOLD", initialCapital)
    .ToString()
```

#### Veri Yönetimi
- Dictionary tabanlı veri saklama
- Otomatik kayıt/yükleme desteği
- Günlük ve saatlik güncelleme sistemi

#### API Uyumluluk Katmanı

```csharp
// Town.Prosperity özelliğine güvenli erişim
public static float GetTownProsperityValue(Town town, float defaultValue = 300f) 
{
    // Çeşitli erişim yöntemlerini dene
    // 1. Doğrudan property erişimi
    // 2. Üst sınıf property erişimi
    // 3. Fief sınıfı aracılığıyla erişim
    // 4. Method çağrısı ile erişim
    // 5. Settlement üzerinden erişim
}
```

### API Kullanımı

#### Dükkan Yönetimi
```csharp
// Yeni dükkan oluşturma
public WholesaleShop CreateShop(Town town, int initialCapital = 5000)

// Dükkan bilgisi alma
public WholesaleShop GetShopInTown(Town town)

// Dükkan kapatma
public void CloseShop(string shopId)
```

#### Ayarlar Sistemi
```csharp
// Merkezi ayarlar
int initialCapital = Settings.Instance?.WholesaleMinimumCapital ?? 5000;

// XML tabanlı ayar dosyası
public class Settings 
{
    [XmlElement("WholesaleMinimumCapital")]
    public int WholesaleMinimumCapital { get; set; } = 5000;
    
    [XmlElement("WholesaleProfitMargin")]
    public float WholesaleProfitMargin { get; set; } = 0.15f;
}
```

### Hata Yönetimi
- Try-catch blokları ile güvenli operasyonlar
- Merkezi hata loglama ve gösterme
- Açıklayıcı kullanıcı hata mesajları
- Otomatik para iadesi mekanizması

```csharp
// Merkezi hata loglama 
private static void LogError(string message, Exception ex)
{
    string errorMessage = $"{message}: {ex.Message}";
    
    // Konsola log
    if (Settings.Instance?.DebugMode ?? false)
    {
        Debug.Print($"TradingExpanded Error: {errorMessage}");
        Debug.Print($"Stack Trace: {ex.StackTrace}");
    }
    
    // Kullanıcıya göster
    InformationManager.DisplayMessage(new InformationMessage(errorMessage, Colors.Red));
}
```

### Performans Optimizasyonları
- Lazy loading ile gereksiz yüklemelerden kaçınma
- Dictionary kullanımı ile hızlı erişim
- Periyodik güncelleme sistemi
- Exception handling optimizasyonu

### Güvenlik
- Null kontrolleri
- Veri doğrulama
- Exception handling
- Bannerlord API değişikliklerine karşı koruma

### Gelecek Geliştirmeler
1. UI Extender entegrasyonu
2. Daha detaylı çeviri sistemi
3. Performans optimizasyonları
4. Yeni ticaret özellikleri

### Sürüm Notları
#### v1.1.3
- Menü sistemi iyileştirildi
- Çeviri sistemi geliştirildi
- Bannerlord farklı sürümleri ile uyumluluk sağlandı
- Hata yönetimi merkezi hale getirildi
- Merkezi ayarlar sistemi geliştirildi 