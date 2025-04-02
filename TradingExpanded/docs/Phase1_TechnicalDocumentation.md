# TradingExpanded - Teknik Dokümantasyon

## Genel Bakış
TradingExpanded modülü, Mount & Blade II: Bannerlord oyununa gelişmiş ticaret sistemi ekler. Bu modül, oyuncuların toptan ticaret dükkanları açmasına, ticaret kervanları oluşturmasına ve detaylı ticaret analizleri yapmasına olanak tanır.

## Proje Yapısı
Proje aşağıdaki temel bileşenlerden oluşur:

### Klasör Yapısı
- **Models/**: Veri modelleri
- **Behaviors/**: Oyun davranışları ve kampanya mantığı
- **Helpers/**: Yardımcı metotlar ve araçlar
- **UI/**: Kullanıcı arayüzü bileşenleri
- **Patches/**: Harmony yamaları
- **Utils/**: Genel yardımcı işlevler
- **Services/**: Servis katmanı işlevleri

### Temel Dosyalar
- `SubModule.cs` - Mod için ana giriş noktası
- `Behaviors/TradingExpandedCampaignBehavior.cs` - Mod verileri ve kampanya davranışları
- `Helpers/Constants.cs` - Sabitler ve yardımcı fonksiyonlar
- `Helpers/TradingExpandedSaveDefiner.cs` - Oyun kayıt sistemi için tanımlamaları içerir

### Veri Modelleri
- `WholesaleShop` - Toptan ticaret dükkanı
- `TradeCaravan` - Ticaret kervanı
- `Courier` - Kurye
- `TradeRoute` - Ticaret rotası
- `TradeAgreement` - Ticaret anlaşması
- `PriceTracker` - Fiyat takibi
- `InventoryTracker` - Envanter istatistikleri
- `MerchantRelation` - Tüccar ilişkileri

## İşleyiş

### Başlatma
`SubModule.cs` dosyası, mod başlatma işlemini yönetir:

```csharp
protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
{
    base.OnGameStart(game, gameStarterObject);
    
    if (game.GameType is Campaign)
    {
        if (gameStarterObject is CampaignGameStarter campaignStarter)
        {
            campaignStarter.AddBehavior(new TradingExpandedCampaignBehavior());
        }
    }
}
```

### Veri Yönetimi
`TradingExpandedCampaignBehavior` sınıfı, mod verilerini yönetir ve günlük güncelleme döngüsünü gerçekleştirir.

```csharp
private void OnDailyTick()
{
    // Update all objects daily
    UpdateWholesaleShops();
    UpdateCaravans();
    UpdateCouriers();
    UpdateMerchantRelations();
    
    // Update analytics less frequently (every 3 days)
    if ((int)CampaignTime.Now.ToDays % 3 == 0)
    {
        _inventoryTracker.Update();
    }
}
```

### Toptan Ticaret Dükkanları
Toptan ticaret dükkanları, şehirlerde kurulabilir ve alım-satım emirleri yönetilebilir. Bu dükkanlarda ticaret yapacak çalışanlar tutulabilir ve mevcut stok yönetilebilir.

```csharp
public WholesaleShop CreateShop(Town town, int initialCapital = 5000)
{
    if (town == null)
        return null;
        
    // Check if we already have a shop in this town
    if (GetShopInTown(town) != null)
        return null;
        
    // Check if we've reached the maximum number of shops
    int maxShops = Settings.Instance?.MaxWholesaleShops ?? 3;
    if (GetPlayerShops().Count >= maxShops)
        return null;
        
    // Create the shop
    var shop = new WholesaleShop(town, initialCapital);
    _wholesaleShops[shop.Id] = shop;
    
    return shop;
}
```

### Kervanlar
Kervanlar, oyuncu tarafından şehirler arasında ticaret yapmak için oluşturulabilir. Kervanlar yolculuk sırasında otomatik olarak güncellenir ve yönetilir.

```csharp
public TradeCaravan CreateCaravan(Town startingTown, Hero leader, int initialCapital = 5000)
{
    if (startingTown == null || leader == null)
        return null;
        
    // Check if we've reached the maximum number of caravans
    int maxCaravans = Settings.Instance?.MaxCaravans ?? 3;
    if (GetPlayerCaravans().Count >= maxCaravans)
        return null;
        
    // Create the caravan
    var caravan = new TradeCaravan(startingTown, leader, initialCapital);
    _caravans[caravan.Id] = caravan;
    
    return caravan;
}
```

### Fiyat Takibi
`PriceTracker` sınıfı, şehirler arası fiyat farklarını takip eder ve karlı ticaret fırsatlarını belirlemeye yardımcı olur.

```csharp
public void FiyatKaydet(ItemObject esya, Town sehir, int fiyat)
{
    if (esya == null || sehir == null || fiyat <= 0)
        return;
        
    string esyaId = esya.StringId;
    
    if (!_fiyatKayitlari.ContainsKey(esyaId))
    {
        _fiyatKayitlari[esyaId] = new List<FiyatKaydi>();
    }
    
    // Bugün için zaten kayıt var mı kontrol et
    int gunNo = (int)CampaignTime.Now.ToDays;
    bool ayniGunKayitVar = _fiyatKayitlari[esyaId].Any(
        k => k.Sehir == sehir && (int)k.Tarih.ToDays == gunNo);
        
    if (!ayniGunKayitVar)
    {
        _fiyatKayitlari[esyaId].Add(new FiyatKaydi
        {
            Tarih = CampaignTime.Now,
            Fiyat = fiyat,
            Sehir = sehir,
            Miktar = sehir.GetItemPrice(esya)
        });
    }
    
    // Eski kayıtları temizle
    TemizleFiyatKayitlari(esyaId);
}
```

## Kayıt Sistemi
Mod, Bannerlord'un kayıt sistemini kullanarak veri durumunu korur. Bu, `TradingExpandedSaveDefiner` sınıfı tarafından yapılandırılır.

```csharp
protected override void DefineClassTypes()
{
    // Main business objects
    AddClassDefinition(typeof(WholesaleShop), 1);
    AddClassDefinition(typeof(WholesaleBuyOrder), 11);
    AddClassDefinition(typeof(WholesaleSellOrder), 12);
    
    AddClassDefinition(typeof(WholesaleEmployee), 2);
    AddClassDefinition(typeof(TradeCaravan), 3);
    AddClassDefinition(typeof(TradeRoute), 4);
    AddClassDefinition(typeof(Courier), 5);
    AddClassDefinition(typeof(MerchantRelation), 6);
    AddClassDefinition(typeof(TradeAgreement), 7);
    
    // Analytics and stats objects
    AddClassDefinition(typeof(InventoryTracker), 8);
    AddClassDefinition(typeof(ItemStats), 9);
    AddClassDefinition(typeof(PriceHistory), 10);
    AddClassDefinition(typeof(PriceDataPoint), 13);
    
    // İç sınıflar için
    AddClassDefinition(typeof(PriceTracker.FiyatKaydi), 41);
    AddClassDefinition(typeof(PriceTracker.IslemKaydi), 42);
}
```

## Konfigürasyon
Mod ayarları `Settings` sınıfı aracılığıyla yönetilir. Oyun içi değerleri değiştirmek için bu sınıf kullanılır.

```csharp
public class Settings
{
    private static Settings _instance;
    
    public static Settings Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new Settings();
            }
            return _instance;
        }
    }
    
    // Wholesale shop settings
    public int MaxWholesaleShops { get; set; } = 3;
    public float WholesaleProfitMargin { get; set; } = 0.15f;
    
    // Caravan settings
    public int MaxCaravans { get; set; } = 3;
    public float CaravanTaxRate { get; set; } = 0.05f;
    
    // Courier settings
    public int MaxCouriers { get; set; } = 5;
    public float CourierSpeedMultiplier { get; set; } = 1.2f;
}
```

## Güvenlik ve Performans Güvenceleri

TradingExpanded modunda güvenlik ve performans için aşağıdaki önlemler alınmıştır:

1. Dictionary kayıtlarına erişimde null kontrolü yapılarak referans hataları önlenmiştir.
2. Sık güncellemeler gerektiren operasyonlar için optimizasyonlar yapılmıştır.
3. Büyük veri koleksiyonları için boyut sınırlamaları getirilmiştir.
4. GetProfitMargin gibi yoğun hesaplama gerektiren operasyonlarda önbellek mekanizmaları kullanılmıştır.

## Gelecek Geliştirmeler

1. Daha detaylı ekonomi simülasyonu
2. Ticaret rotaları için otomatik optimizasyon
3. Bölgesel ekonomik olaylar
4. İyileştirilmiş UI
5. Daha fazla istatistik ve grafik
6. Oyuncu olmayan karakterlerin yönettiği toptan ticaret dükkanları

## Kurulum ve Katkı
Modül geliştirmeye katkıda bulunmak için:

1. Proje klonlandıktan sonra `dotnet build` komutu ile derlenebilir
2. Release derlemesi için `dotnet publish -c Release -f net472` kullanılmalıdır
3. Yeni özellikler eklemek için uygun klasör yapısına dikkat edilmelidir
4. Tüm değişiklikler için birim testler yazılmalıdır
5. PR göndermeden önce kod incelemesi yapılmalıdır
6. Belgelendirme güncel tutulmalıdır 