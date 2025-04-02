## Teknik Dokümantasyon - Faz 1

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

#### Veri Yönetimi
- Dictionary tabanlı veri saklama
- Otomatik kayıt/yükleme desteği
- Günlük ve saatlik güncelleme sistemi

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

#### Kervan Yönetimi
```csharp
// Yeni kervan oluşturma
public TradeCaravan CreateCaravan(Town startingTown, Hero leader, int initialCapital)

// Kervan bilgisi alma
public List<TradeCaravan> GetPlayerCaravans()
```

### Hata Yönetimi
- Try-catch blokları ile güvenli operasyonlar
- Detaylı loglama sistemi
- Kullanıcı dostu hata mesajları

### Performans Optimizasyonları
- Lazy loading ile gereksiz yüklemelerden kaçınma
- Dictionary kullanımı ile hızlı erişim
- Periyodik güncelleme sistemi

### Güvenlik
- Null kontrolleri
- Veri doğrulama
- Exception handling

### Gelecek Geliştirmeler
1. UI Extender entegrasyonu
2. Daha detaylı çeviri sistemi
3. Performans optimizasyonları
4. Yeni ticaret özellikleri

### Sürüm Notları
#### v1.1.2
- Menü sistemi yeniden düzenlendi
- Çeviri sistemi eklendi
- Hata düzeltmeleri yapıldı 