# Trading Expanded - Oyun Dokümantasyonu

## Genel Bakış
Trading Expanded, Mount & Blade II: Bannerlord oyununa gelişmiş ticaret özellikleri ekleyen bir moddur.

## Özellikler

### Toptan Satış Dükkanı
- Her şehirde bir toptan satış dükkanı açabilirsiniz
- Başlangıç sermayesi: 5000 Dinar (ayarlardan değiştirilebilir)
- Şehrin refahına göre günlük kazanç
- Sermaye yatırımı ve çekme imkanı
- Farklı Bannerlord sürümleriyle uyumlu

### Menü Sistemi
- Kolay erişilebilir şehir menüsü entegrasyonu
- Sezgisel kullanıcı arayüzü
- Türkçe dil desteği
- Detaylı bilgi gösterimi
- Çevrilebilir metinler

### Güvenlik Sistemleri
- Hata durumunda otomatik para iadesi
- Bannerlord API değişikliklerine karşı koruma
- Kullanıcı dostu hata mesajları
- API uyumluluk katmanı

## Kullanım Kılavuzu

### Dükkan Açma
1. Herhangi bir şehre girin
2. Şehir menüsünden "Toptan Satış Dükkanı" seçeneğini seçin
3. "Yeni Dükkan Kur" seçeneğini seçin
4. Başlangıç sermayesi ile dükkanınız açılır
5. Açılışta hata olursa paranız otomatik olarak iade edilir

### Dükkan Yönetimi
1. Şehir menüsünden "Toptan Satış Dükkanı" seçeneğini seçin
2. "Dükkanı Yönet" seçeneğini seçin
3. Aşağıdaki işlemleri yapabilirsiniz:
   - Dükkan bilgilerini görüntüleme
   - Sermaye yatırımı
   - Sermaye çekme
   - Dükkan kapatma

### Hatalar ve Çözümleri

#### "Metot bulunamadı: Prosperity" Hatası
Bu hata, Bannerlord'un yeni sürümlerinde API değişikliği nedeniyle oluşabilir. Mod bunu otomatik olarak algılayıp güvenli bir şekilde çalışacaktır. Herhangi bir işlem yapmanız gerekmez.

#### Dükkan Kurma Hatası
Dükkan kurarken hata oluşursa, yatırılan sermaye otomatik olarak iade edilir ve size bilgi mesajı gösterilir.

### İpuçları
- Şehrin refahı yüksek olan yerlerde dükkan açmak daha karlıdır
- Düzenli olarak sermaye yatırımı yaparak kazancınızı artırabilirsiniz
- Birden fazla şehirde dükkan açarak riski dağıtabilirsiniz
- Debug modunu açarak hata ayıklama yapabilirsiniz (geliştiriciler için)

## Ayarlar

Mod ayarlarını `Modules/TradingExpanded/Config/TradingExpandedSettings.xml` dosyasından değiştirebilirsiniz:

```xml
<Settings>
  <IsEnabled>true</IsEnabled>
  <DebugMode>false</DebugMode>
  <WholesaleMinimumCapital>5000</WholesaleMinimumCapital>
  <WholesaleProfitMargin>0.15</WholesaleProfitMargin>
  <WholesaleMaxCapital>100000</WholesaleMaxCapital>
  <MaxWholesaleShops>3</MaxWholesaleShops>
</Settings>
```

### Ayar Parametreleri

- **WholesaleMinimumCapital**: Dükkan kurma maliyeti ve başlangıç sermayesi
- **WholesaleProfitMargin**: Kâr marjı
- **WholesaleMaxCapital**: Maksimum sermaye limiti
- **MaxWholesaleShops**: Açılabilecek maksimum dükkan sayısı
- **DebugMode**: Hata ayıklama modu (geliştiriciler için)

## Sıkça Sorulan Sorular

### Kaç dükkan açabilirim?
- Varsayılan olarak maksimum 3 dükkan açabilirsiniz. Bu değer ayar dosyasından değiştirilebilir.

### Dükkanımı kapatırsam sermayem ne olur?
- Dükkanı kapattığınızda tüm sermayeniz size iade edilir

### Dükkanım zarar edebilir mi?
- Hayır, dükkanlar zarar etmez ancak şehrin refahına göre kazancınız değişir

### Mod güncellendiğinde verilerim kaybolur mu?
- Hayır, mod güncellense bile dükkan verileriniz korunacaktır

### Bannerlord'un farklı sürümleri ile uyumlu mu?
- Evet, mod Bannerlord'un farklı sürümleri ile çalışacak şekilde tasarlanmıştır

## Çeviri Desteği

Mod için kendi dilinizde çeviri eklemek için:

1. `ModuleData/Languages` klasörü altında kendi diliniz için bir klasör oluşturun (örn. `DE` - Almanca için)
2. `std_TradingExpanded_xx.xml` dosyasını oluşturun (xx: dil kodu)
3. Aşağıdaki formatta çevirileri ekleyin:

```xml
<?xml version="1.0" encoding="utf-8"?>
<base xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema" type="string">
  <strings>
    <string id="WholesaleShopMenuTitle" text="Toptan Satış Dükkanı" />
    <string id="WholesaleShopNewOption" text="Yeni Dükkan Kur ({GOLD} Dinar)" />
    <!-- Diğer çeviriler -->
  </strings>
</base>
```

## Hata Bildirimi
Herhangi bir hata ile karşılaşırsanız, lütfen mod sayfasından bildirimde bulunun.

## Sürüm Notları

### v1.1.3
- Menü sistemi iyileştirildi
- Çeviri sistemi geliştirildi 
- Bannerlord farklı sürümleri ile uyumluluk sağlandı
- Hata yönetimi merkezi hale getirildi
- Merkezi ayarlar sistemi geliştirildi

## 1. Genel Bakış
TradingExpanded, Mount & Blade II: Bannerlord için ticaret sistemini geliştiren ve oyunculara kendi ticaret imparatorluklarını kurma imkanı sağlayan bir modifikasyondur. Oyuncular toptan ticaret dükkanları açabilir, kervanlar organize edebilir ve özel ticaret ağları oluşturabilir.

## 2. Temel Özellikler

### 2.1. Toptan Ticaret Dükkanları
- **Konsept**: Oyuncular şehirlerde toptan ticaret dükkanları satın alabilir ve yönetebilir
- **İşlevsellik**: Toplu alım/satım, mal depolama, müzakereler
- **Benzersiz Avantajlar**: Toptan alımlarda indirimler, büyük siparişler verebilme imkanı
- **Personel Yönetimi**: Dükkanlar için çalışanlar tutabilir ve onların becerilerini geliştirebilirsiniz

### 2.2. Kervan Yönetimi
- **Konsept**: Oyuncular kendi özel kervanlarını oluşturabilir ve yönetebilir
- **İşlevsellik**: Malları bir yerden başka bir yere taşıma, ticaret rotaları belirleme
- **Güvenlik**: Kervan muhafızları kiralama, haydut saldırılarına karşı önlem alma
- **Ticaret Rotaları**: Önceden belirlenmiş rotalar oluşturarak kervanların otomatik hareket etmesini sağlama

### 2.3. Kurye Sistemi
- **Konsept**: Diğer şehirlerden fiyat bilgisi toplamak için kuryeler gönderme
- **İşlevsellik**: Pazar araştırması, fiyat dalgalanmalarını takip etme
- **Avantajlar**: Karlı ticaret fırsatlarını önceden tespit etme

### 2.4. NPC Tüccarlarla Etkileşim
- **Konsept**: Diğer tüccarlarla ilişkiler geliştirme
- **İşlevsellik**: Ortaklıklar kurma, ticaret anlaşmaları yapma, rekabet etme
- **İlişki Sistemi**: İtibar ve güven kazanarak daha avantajlı anlaşmalar yapabilme

### 2.5. Envanter ve Personel Yönetimi
- **Konsept**: Dükkan ve depo envanterlerini yönetme, personel kiralama
- **İşlevsellik**: Personel becerileri, maaşlar, sadakat
- **Gelişim**: Çalışanların yeteneklerini zaman içinde geliştirebilme

### 2.6. Otomatik Ticaret Rotaları
- **Konsept**: Karlı ticaret rotalarını otomatikleştirme
- **İşlevsellik**: Rotalar oluşturma, performans analizi
- **Optimizasyon**: En karlı rotalarda ticareti maksimize etme

### 2.7. Ekonomik Olaylar
- **Konsept**: Oyun dünyasını etkileyen dinamik ekonomik olaylar
- **İşlevsellik**: Kıtlık, bolluk, savaş ekonomisi, vergi düzenlemeleri
- **Fiyat Takibi**: Ekonomik olayların fiyatlar üzerindeki etkisini izleyebilme

## 3. Kullanıcı Arayüzü
- **Toptan Satış Menüsü**: Ana şehir ekranında yeni bir seçenek
- **Kervan Yönetim Ekranı**: Kervanları oluşturma ve takip etme
- **Ticaret Haritası**: Fiyat bilgilerini gösteren özel bir harita
- **Tüccar İlişkileri Paneli**: NPC tüccarlarla ilişkileri gösteren panel
- **Fiyat Analiz Grafikleri**: Şehirler arası fiyat farklarını görsel olarak izleme

## 4. Oyun Dengesi
- Dükkan satın alma ve bakım maliyetleri, orijinal oyun ekonomisi ile dengelidir
- Aynı pazarda çok fazla oyuncu faaliyet gösterdiğinde kar marjları düşer
- Ekonomik olaylar zorluk ve çeşitlilik sunar
- Otomatik ticaret için risk ve ödül dengesi optimize edilmiştir

## 5. Başlangıç Rehberi

### 5.1. İlk Adımlar
1. Orta düzeyde sermaye (en az 10.000 dinar) ile başlayın
2. İlk toptan ticaret dükkanınızı işlek bir şehirde açın
3. Başlangıçta birkaç çalışan kiralayın
4. Yerel ürünleri düşük fiyattan alıp, ihtiyaç duyulan şehirlerde satın

### 5.2. Kervan Kurma
1. Güvenilir bir lider seçin
2. Güvenli bir rota belirleyin
3. Başlangıçta güvenlik için yeterli muhafız kiralayın
4. Karlı malları taşımaya öncelik verin

### 5.3. Ticaret İmparatorluğunu Genişletme
1. Farklı bölgelerde dükkanlar açın
2. Kuryeler göndererek fiyat bilgilerini güncel tutun
3. Tüccarlarla ilişkilerinizi geliştirin
4. Daha karlı ticaret anlaşmaları için pazarlık yapın

## 6. İleri Düzey Stratejiler

### 6.1. Pazar Hakimiyeti
- Belirli bir malın ticaretinde uzmanlaşarak o pazarda hakimiyet kurun
- Tekelci davranarak fiyatları kendi lehinize manipüle edin
- Rekabet eden tüccarları satın alın veya ortaklık kurun

### 6.2. Kriz Yönetimi
- Savaş zamanlarında alternatif ticaret rotaları oluşturun
- Ekonomik krizlerde ucuza mal alın, bolluk zamanlarında satın
- Politik ilişkilerinizi kullanarak ticaret avantajları elde edin

### 6.3. Gelişmiş Lojistik
- Kurye ağınızı genişleterek bilgi akışını hızlandırın
- Birden fazla kervandan oluşan konvoylar oluşturun
- Depolama kapasitesini artırarak büyük miktarlarda mal stoklayın 