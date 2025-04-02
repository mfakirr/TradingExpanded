# Trading Expanded Mod

Mount & Blade II: Bannerlord için geliştirilmiş bir ticaret genişletme modu.

## Özellikler

### Toptan Satış Dükkanı
- Her şehirde bir toptan satış dükkanı açabilirsiniz
- Başlangıç sermayesi: 5000 Dinar
- Şehrin refahına göre günlük kazanç
- Sermaye yatırımı ve çekme imkanı

## Kurulum

1. Modu `Modules` klasörüne çıkarın
2. Launcher'dan modu aktifleştirin
3. Oyunu başlatın

## Kullanım

### Toptan Satış Dükkanı Açma
1. Herhangi bir şehre girin
2. Şehir menüsünden "Toptan Satış Dükkanı" seçeneğini seçin
3. "Yeni Dükkan Kur" seçeneğini seçin (5000 Dinar gerekir)

### Dükkan Yönetimi
1. Şehir menüsünden "Toptan Satış Dükkanı" seçeneğini seçin
2. "Dükkanı Yönet" seçeneğini seçin
3. Sermaye yatırımı veya çekme işlemlerini yapın

## Çeviri Desteği

Mod, çoklu dil desteğine sahiptir. Yeni dil eklemek için:

1. `ModuleData/Languages` klasörü altında yeni bir dil klasörü oluşturun
2. `std_TradingExpanded_xx.xml` dosyası oluşturun (xx: dil kodu)
3. Mevcut çeviri dosyasını örnek alarak çevirileri ekleyin

## Geliştirici Notları

### Menü Sistemi
- Şehir menüsüne yeni seçenekler `CampaignGameStarter.AddGameMenuOption` ile eklenir
- Her menü seçeneği için benzersiz bir ID kullanılır
- Çeviri ID'leri `{=ID}Text` formatında tanımlanır

### Kod Yapısı
- `TradingExpandedCampaignBehavior`: Ana davranış sınıfı
- `WholesaleShop`: Dükkan veri modeli
- `WholesaleShopViewModel`: Dükkan yönetim arayüzü

## Sürüm Geçmişi

### v1.1.2
- Menü sistemi yeniden düzenlendi
- Çeviri sistemi eklendi
- Hata düzeltmeleri yapıldı

## Katkıda Bulunma

1. Bu depoyu fork edin
2. Yeni bir branch oluşturun (`git checkout -b feature/AmazingFeature`)
3. Değişikliklerinizi commit edin (`git commit -m 'Add some AmazingFeature'`)
4. Branch'inizi push edin (`git push origin feature/AmazingFeature`)
5. Bir Pull Request oluşturun

## Lisans

Bu proje MIT lisansı altında lisanslanmıştır. Detaylar için `LICENSE` dosyasına bakın. 