using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using TaleWorlds.SaveSystem;
using TradingExpanded.Helpers;

namespace TradingExpanded.Models
{
    /// <summary>
    /// Oyuncunun sahip olduğu ticaret kervanını temsil eder
    /// </summary>
    public class TradeCaravan
    {
        #region Kervan Durumları
        
        /// <summary>
        /// Kervanın durum tipleri
        /// </summary>
        public enum CaravanState
        {
            /// <summary>Bir şehirde bekliyor</summary>
            Beklemede,
            
            /// <summary>Yolculuk yapıyor</summary>
            Yolculukta,
            
            /// <summary>Bir şehirde ticaret yapıyor</summary>
            TicaretYapiyor,
            
            /// <summary>Saldırı altında</summary>
            SaldiriAltinda,
            
            /// <summary>Ana şehire dönüyor</summary>
            Donuyor
        }
        
        #endregion
        
        #region Özellikler
        
        [SaveableProperty(1)]
        public string Id { get; private set; }
        
        [SaveableProperty(2)]
        public Hero Lider { get; private set; }
        
        [SaveableProperty(3)]
        public MobileParty HareketliParti { get; private set; }
        
        [SaveableProperty(4)]
        public Town MevcutSehir { get; private set; }
        
        [SaveableProperty(5)]
        public Town HedefSehir { get; private set; }
        
        [SaveableProperty(6)]
        public Dictionary<ItemObject, int> Kargo { get; private set; }
        
        [SaveableProperty(7)]
        public int Sermaye { get; private set; }
        
        [SaveableProperty(8)]
        public List<Town> Rota { get; private set; }
        
        [SaveableProperty(9)]
        public CaravanState Durum { get; private set; }
        
        [SaveableProperty(10)]
        public int GuvenlikSeviyesi { get; private set; }
        
        [SaveableProperty(11)]
        public float Hiz { get; private set; }
        
        [SaveableProperty(12)]
        public int KargoKapasitesi { get; private set; }
        
        [SaveableProperty(13)]
        public CampaignTime SonGuncellemeZamani { get; private set; }
        
        [SaveableProperty(14)]
        public float MevcutDurumIlerleme { get; private set; }
        
        [SaveableProperty(15)]
        public bool AktifMi { get; set; }
        
        [SaveableProperty(16)]
        public int ToplamKar { get; private set; }
        
        [SaveableProperty(17)]
        public Town AnaSehir { get; private set; }
        
        [SaveableProperty(18)]
        public CampaignTime KurulusZamani { get; private set; }
        
        [SaveableProperty(19)]
        public float YolculukSuresi { get; private set; }
        
        [SaveableProperty(20)]
        public float YolculukIlerlemesi { get; private set; }
        
        [SaveableProperty(21)]
        public float BeklemeSuresi { get; private set; }
        
        [SaveableProperty(22)]
        public float GuvenlikRiski { get; private set; }
        
        [SaveableProperty(23)]
        public int ToplamKazanc { get; private set; }
        
        [SaveableProperty(24)]
        public int MaximumKargoKapasitesi { get; private set; }
        
        #endregion
        
        #region Hesaplanan Özellikler
        
        /// <summary>
        /// Kervan adı
        /// </summary>
        public string Ad => $"{Lider.Name} Kervanı";
        
        /// <summary>
        /// Mevcut kargo değeri
        /// </summary>
        public int KargoDegeri
        {
            get
            {
                int deger = 0;
                foreach (var item in Kargo)
                {
                    int birimFiyat = MevcutSehir?.GetItemPrice(item.Key) ?? 100;
                    deger += item.Value * birimFiyat;
                }
                return deger;
            }
        }
        
        /// <summary>
        /// Mevcut kullanılabilir kargo alanı
        /// </summary>
        public int KullanilanAlan => Kargo.Values.Sum();
        
        /// <summary>
        /// Mevcut kargo ağırlığı
        /// </summary>
        public int MevcutKargoAgirlik
        {
            get
            {
                int toplamAgirlik = 0;
                foreach (var item in Kargo)
                {
                    float birimAgirlik = item.Key.Weight > 0 ? item.Key.Weight : 0.1f;
                    toplamAgirlik += (int)(item.Value * birimAgirlik);
                }
                return toplamAgirlik;
            }
        }
        
        /// <summary>
        /// Mevcut boş kargo alanı
        /// </summary>
        public int BosAlan => MaximumKargoKapasitesi - MevcutKargoAgirlik;
        
        /// <summary>
        /// Şu anki durum açıklaması
        /// </summary>
        public string DurumAciklamasi
        {
            get
            {
                switch (Durum)
                {
                    case CaravanState.Beklemede:
                        return $"{MevcutSehir.Name} şehrinde bekliyor";
                    case CaravanState.Yolculukta:
                        return $"{MevcutSehir.Name}'den {HedefSehir.Name}'e yolculuk yapıyor (%{MevcutDurumIlerleme * 100:F0})";
                    case CaravanState.TicaretYapiyor:
                        return $"{MevcutSehir.Name} şehrinde ticaret yapıyor (%{MevcutDurumIlerleme * 100:F0})";
                    case CaravanState.SaldiriAltinda:
                        return "Saldırı altında!";
                    case CaravanState.Donuyor:
                        return $"{AnaSehir.Name} şehrine dönüyor (%{MevcutDurumIlerleme * 100:F0})";
                    default:
                        return "Bilinmiyor";
                }
            }
        }
        
        /// <summary>
        /// Kervanın kurulduğu tarihten bu yana geçen gün sayısı
        /// </summary>
        public int FaaliyetSuresi
        {
            get
            {
                if (KurulusZamani == CampaignTime.Zero)
                    return 0;
                    
                return (int)(CampaignTime.Now - KurulusZamani).ToDays;
            }
        }
        
        /// <summary>
        /// Günlük ortalama kâr
        /// </summary>
        public float GunlukOrtalamaKar
        {
            get
            {
                if (FaaliyetSuresi <= 0)
                    return 0f;
                    
                return (float)ToplamKar / FaaliyetSuresi;
            }
        }
        
        #endregion
        
        #region Yapılandırıcılar
        
        public TradeCaravan() 
        {
            // Boş yapılandırıcı - Serileştirme için gerekli
            Kargo = new Dictionary<ItemObject, int>();
            Rota = new List<Town>();
        }
        
        public TradeCaravan(Town baslangicSehri, Hero lider, int baslangicSermayesi = 5000)
        {
            if (baslangicSehri == null || lider == null)
                throw new ArgumentNullException("Başlangıç şehri ve lider belirtilmelidir.");
                
            Id = Guid.NewGuid().ToString();
            MevcutSehir = baslangicSehri;
            AnaSehir = baslangicSehri;
            Lider = lider;
            Sermaye = baslangicSermayesi;
            KurulusZamani = CampaignTime.Now;
            
            // Varsayılan değerler
            Kargo = new Dictionary<ItemObject, int>();
            Rota = new List<Town>();
            Durum = CaravanState.Beklemede;
            
            // Ayarlardan değerleri al
            GuvenlikSeviyesi = 20; // Settings.Instance?.InitialCaravanSecurityLevel ?? 20; // 1-100 arası
            GuvenlikRiski = (1.0f - (GuvenlikSeviyesi / 100.0f)).Clamp(0.1f, 1.0f); // Güvenlik seviyesine göre risk hesapla
            
            float hizCarpani = 1.0f; // Settings.Instance?.CaravanTravelSpeed ?? 1.0f;
            Hiz = 20f * hizCarpani; // Günlük yolculukta kat edebileceği mesafe (km)
            
            MaximumKargoKapasitesi = 2000; // Settings.Instance?.CaravanCargoCapacity ?? 2000; // Toplam taşıyabileceği birim
            KargoKapasitesi = MaximumKargoKapasitesi;
            
            BeklemeSuresi = 1.0f; // 1 gün bekleme süresi
            YolculukSuresi = 0;
            YolculukIlerlemesi = 0;
            
            SonGuncellemeZamani = CampaignTime.Now;
            MevcutDurumIlerleme = 0f;
            AktifMi = true;
            ToplamKar = 0;
            ToplamKazanc = 0;
            
            // Gerçek MobileParty oluşturma - uygulama sırasında eklenecek
            // HareketliParti = MobileParty.CreateParty("caravan_" + Id, null);
        }
        
        #endregion
        
        #region Temel Metotlar
        
        /// <summary>
        /// Hedef şehre yolculuğa başlar
        /// </summary>
        /// <param name="hedefSehir">Gidilecek şehir</param>
        /// <returns>Başarılı mı?</returns>
        public bool YolculugaBasla(Town hedefSehir)
        {
            if (hedefSehir == null || hedefSehir == MevcutSehir || !AktifMi)
                return false;
                
            if (Durum != CaravanState.Beklemede)
                return false;
                
            HedefSehir = hedefSehir;
            Durum = CaravanState.Yolculukta;
            MevcutDurumIlerleme = 0f;
            
            return true;
        }
        
        /// <summary>
        /// Belirli bir rotayı izlemeye başlar
        /// </summary>
        /// <param name="sehirListesi">Ziyaret edilecek şehirlerin listesi</param>
        /// <returns>Başarılı mı?</returns>
        public bool RotaAyarla(List<Town> sehirListesi)
        {
            if (sehirListesi == null || sehirListesi.Count == 0 || !AktifMi)
                return false;
                
            if (Durum != CaravanState.Beklemede)
                return false;
                
            Rota = new List<Town>(sehirListesi);
            
            // İlk hedefi ayarla
            if (Rota.Count > 0)
            {
                return YolculugaBasla(Rota[0]);
            }
            
            return false;
        }
        
        /// <summary>
        /// Kervanı günlük olarak günceller
        /// </summary>
        public void Guncelle()
        {
            if (HareketliParti == null)
                return;
                
            // Son güncellemeden bu yana geçen süre
            float gecenGunler = (float)(CampaignTime.Now - SonGuncellemeZamani).ToDays;
            if (gecenGunler < 0.01f) // En az 15 dakika geçmesi için kontrol
                return;
                
            SonGuncellemeZamani = CampaignTime.Now;
            
            // Güncellemeyi durum durumuna göre yap
            switch (Durum)
            {
                case CaravanState.Beklemede:
                    BeklemeyiGuncelle(gecenGunler);
                    break;
                case CaravanState.Yolculukta:
                    YolculuguGuncelle(gecenGunler);
                    break;
                case CaravanState.TicaretYapiyor:
                    TicaretiGuncelle(gecenGunler);
                    break;
                case CaravanState.SaldiriAltinda:
                    SaldiriyiGuncelle(gecenGunler);
                    break;
                case CaravanState.Donuyor:
                    DonusuGuncelle(gecenGunler);
                    break;
            }
            
            // Günlük gider uygula
            GunlukGiderUygula(gecenGunler);
        }
        
        #endregion
        
        #region Durum Güncellemeleri
        
        /// <summary>
        /// Şehirde bekleme durumunu günceller
        /// </summary>
        private void BeklemeyiGuncelle(float gecenGunler)
        {
            if (BeklemeSuresi <= 0)
            {
                // Bekleme süresi doldu, ticaret yap
                TicaretYap();
                
                // Yeni hedef belirlenmediyse, bekleme süresini uzat
                if (HedefSehir == null)
                {
                    BeklemeSuresi = 1.0f; // 1 gün daha bekle
                }
                return;
            }
            
            BeklemeSuresi -= gecenGunler;
            
            // Bekleme süresince kalan ürünleri satmaya çalış
            if (Kargo.Count > 0)
            {
                // Gün içinde satılan ürünlerin yüzdesi (%5-%15)
                float satisYuzdesi = 0.05f + (MBRandom.RandomFloat * 0.1f);
                
                // Kargo boşsa veya beklenmedik bir durum olursa döngüyü kır
                if (Kargo.Count == 0)
                    return;
                    
                // Her ürünü satmayı dene
                foreach (var esya in new List<ItemObject>(Kargo.Keys))
                {
                    int miktar = Kargo[esya];
                    int satisMiktari = (int)(miktar * satisYuzdesi);
                    
                    if (satisMiktari > 0)
                    {
                        int birimFiyat = MevcutSehir.GetItemPrice(esya);
                        
                        // Ürünü satmaya çalış
                        if (Kargo.ContainsKey(esya) && Kargo[esya] >= satisMiktari)
                        {
                            // Kargodan çıkar
                            Kargo[esya] -= satisMiktari;
                            if (Kargo[esya] <= 0)
                                Kargo.Remove(esya);
                                
                            // Para kazanç
                            int kazanc = satisMiktari * birimFiyat;
                            Sermaye += kazanc;
                            ToplamKazanc += kazanc;
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Yolculuk durumunu günceller
        /// </summary>
        private void YolculuguGuncelle(float gecenGunler)
        {
            // Yolculuk olaylarını kontrol et
            YolculukOlaylariKontrol(gecenGunler);
            
            if (Durum != CaravanState.Yolculukta) 
                return; // Eğer durum değiştiyse (örn. saldırı) geri dön
                
            // Yolculuk ilerleme durumunu güncelle
            YolculukIlerlemesi += gecenGunler * (Hiz / YolculukSuresi);
            
            // Hedefe ulaşıldı mı?
            if (YolculukIlerlemesi >= 1.0f)
            {
                // Hedefe ulaşıldı
                MevcutSehir = HedefSehir;
                HedefSehir = null;
                YolculukIlerlemesi = 0f;
                
                // Yeni duruma geç
                Durum = CaravanState.TicaretYapiyor;
                BeklemeSuresi = 1.0f; // 1 gün ticaret yap
                
                TicaretYap();
            }
        }
        
        /// <summary>
        /// Ticaret durumunu günceller
        /// </summary>
        private void TicaretiGuncelle(float gecenGunler)
        {
            // Ticaret süresi dolduysa yolculuğa devam et veya geri dön
            BeklemeSuresi -= gecenGunler;
            
            if (BeklemeSuresi <= 0)
            {
                // Ticaret tamamlandı, devam et
                
                // Rotada başka şehir var mı?
                if (Rota != null && Rota.Count > 0)
                {
                    // Rotadaki bir sonraki şehre git
                    int mevcutIndeks = Rota.IndexOf(MevcutSehir);
                    
                    if (mevcutIndeks < 0 || mevcutIndeks >= Rota.Count - 1)
                    {
                        // Rotanın sonuna gelindi, ana şehire dön
                        Durum = CaravanState.Donuyor;
                        HedefSehir = AnaSehir;
                    }
                    else
                    {
                        // Rotadaki bir sonraki şehre git
                        HedefSehir = Rota[mevcutIndeks + 1];
                        Durum = CaravanState.Yolculukta;
                    }
                }
                else
                {
                    // Rota yok, ana şehire dön
                    Durum = CaravanState.Donuyor;
                    HedefSehir = AnaSehir;
                }
                
                if (HedefSehir != null)
                {
                    // Yolculuk süresini hesapla
                    YolculukSuresi = HesaplaYolculukSuresi(MevcutSehir, HedefSehir);
                    YolculukIlerlemesi = 0f;
                }
            }
            // Ticaret devam ediyor
            else
            {
                // Kalan malları sat ve yeni mal al
                TicaretYap();
            }
        }
        
        /// <summary>
        /// Yolculuk sırasında meydana gelebilecek olayları kontrol eder
        /// </summary>
        private void YolculukOlaylariKontrol(float gecenGunler)
        {
            // Güvenlik seviyesine göre saldırı riski hesapla
            float temelSaldiriRiski = 0.05f * gecenGunler; // Her gün için %5 temel risk
            float gercekSaldiriRiski = temelSaldiriRiski * GuvenlikRiski; // Güvenlik seviyesi etkisi
            
            // Rastgele saldırı kontrolü
            if (MBRandom.RandomFloat < gercekSaldiriRiski)
            {
                // Saldırı gerçekleşti
                SaldiriyaDusur();
            }
            
            // Diğer rastgele olaylar (örn. hava koşulları, yol durumu)
            float olumsuzOlayRiski = 0.03f * gecenGunler; // Her gün için %3 temel risk
            
            if (MBRandom.RandomFloat < olumsuzOlayRiski)
            {
                // Hava koşulları veya yol durumu nedeniyle yavaşlama
                float gecikmeOrani = 0.1f + (MBRandom.RandomFloat * 0.2f); // %10-%30 arası gecikme
                YolculukSuresi *= (1 + gecikmeOrani);
            }
        }
        
        /// <summary>
        /// Kervanı saldırı durumuna düşürür
        /// </summary>
        private void SaldiriyaDusur()
        {
            Durum = CaravanState.SaldiriAltinda;
            
            // Saldırı hasarını hesapla (güvenlik seviyesine göre)
            float hasarOrani = 0.5f * (1 - (GuvenlikSeviyesi / 100.0f)).Clamp(0.1f, 0.9f);
            
            // Sermayeye zarar ver
            int kayip = (int)(Sermaye * hasarOrani);
            Sermaye -= kayip;
            
            // Kargonun bir kısmını kaybet (daha az güvenlik = daha çok kayıp)
            foreach (var esya in new List<ItemObject>(Kargo.Keys))
            {
                int kayipMiktar = (int)(Kargo[esya] * hasarOrani);
                if (kayipMiktar > 0)
                {
                    Kargo[esya] -= kayipMiktar;
                    if (Kargo[esya] <= 0)
                        Kargo.Remove(esya);
                }
            }
            
            // Toparlanma süresi (1-3 gün)
            BeklemeSuresi = 1.0f + (MBRandom.RandomFloat * 2.0f);
        }
        
        /// <summary>
        /// Saldırı durumunu günceller
        /// </summary>
        private void SaldiriyiGuncelle(float gecenGunler)
        {
            // Bekleme süresini azalt
            BeklemeSuresi -= gecenGunler;
            
            // Bekleme süresi bittiyse normal duruma dön
            if (BeklemeSuresi <= 0)
            {
                // Eğer sermaye sıfırın altına düştüyse kervanı devreden çıkar
                if (Sermaye <= 0)
                {
                    AktifMi = false;
                    return;
                }
                
                // Normal duruma dön
                Durum = CaravanState.Beklemede;
                BeklemeSuresi = 1.0f; // 1 gün dinlen
            }
        }
        
        /// <summary>
        /// Dönüş durumunu günceller
        /// </summary>
        private void DonusuGuncelle(float gecenGunler)
        {
            if (HedefSehir == null || HedefSehir != AnaSehir)
            {
                HedefSehir = AnaSehir;
            }
            
            // Yolculuk ilerlemesini hesapla
            float mesafe = MevcutSehir.Settlement.Position2D.Distance(HedefSehir.Settlement.Position2D) / 1000f; // km
            float toplamSure = mesafe / Hiz; // günler
            
            // İlerlemeyi güncelle
            float gunlukIlerleme = gecenGunler / toplamSure;
            MevcutDurumIlerleme += gunlukIlerleme;
            
            // Hedefe ulaşıldı mı?
            if (MevcutDurumIlerleme >= 1.0f)
            {
                // Ana şehre vardık
                MevcutSehir = AnaSehir;
                HedefSehir = null;
                Durum = CaravanState.Beklemede;
                MevcutDurumIlerleme = 0f;
                
                // Kargo satışı
                TicaretYap();
            }
            else
            {
                // Yolculukta rasgele olaylar
                YolculukOlaylariKontrol(gecenGunler);
            }
        }
        
        #endregion
        
        #region Yardımcı Metotlar
        
        /// <summary>
        /// Mevcut şehirde ticaret yapar (kargo satışı ve alımı)
        /// </summary>
        private void TicaretYap()
        {
            int baslangicSermaye = Sermaye;
            
            // Kargo satışı
            foreach (var item in Kargo.ToList())
            {
                int miktar = item.Value;
                int birimFiyat = MevcutSehir.GetItemPrice(item.Key);
                
                // Satış fiyatını hesapla (ticaret becerisi etkisi)
                float becerifaktoru = 1.0f + (Lider.GetSkillValue(DefaultSkills.Trade) / 300f); // +%0-33 arası bonus
                int satisFiyati = (int)(birimFiyat * becerifaktoru);
                
                // Satış geliri
                int satisTutari = miktar * satisFiyati;
                Sermaye += satisTutari;
                
                // Kargodan çıkar
                Kargo.Remove(item.Key);
            }
            
            // Yeni kargo alımı
            KargoAl();
            
            // Kârı hesapla ve ekle
            int kar = Sermaye - baslangicSermaye;
            ToplamKar += kar;
        }
        
        /// <summary>
        /// İki şehir arasındaki yolculuk süresini hesaplar
        /// </summary>
        /// <returns>Günlük yolculuk süresi</returns>
        private float HesaplaYolculukSuresi(Town baslangic, Town hedef)
        {
            if (baslangic == null || hedef == null || baslangic.Settlement == null || hedef.Settlement == null)
                return 5.0f; // Varsayılan 5 gün
            
            // Mesafeyi kilometre cinsinden hesapla
            float mesafe = baslangic.Settlement.Position2D.Distance(hedef.Settlement.Position2D) / 1000f;
            
            // Günlük yolculuk süresi = Mesafe / Hız
            return mesafe / Hiz;
        }
        
        /// <summary>
        /// Kârı en yüksek eşyaları tespit ederek kargo alır
        /// </summary>
        public void KargoAl()
        {
            if (MevcutSehir == null || Sermaye <= 0)
                return;

            // Boş kargo kapasitesi 
            int bosKapasite = MaximumKargoKapasitesi - MevcutKargoAgirlik;
            if (bosKapasite <= 0)
                return;

            // Kârlı ürünleri belirle
            var satirKarlıÜrünler = new List<(ItemObject item, int alisFiyati, int satisFiyati, float karOrani)>();
            
            // Satın alma için kullanılacak sermaye (toplam sermayenin %70'i)
            int alisSermayesi = (int)(Sermaye * 0.7f);
            
            // MBObjectManager kullanarak tüm ticaret edilebilir ürünleri al
            var tumUrunler = MBObjectManager.Instance.GetObjectTypeList<ItemObject>()
                .Where(item => item.Value > 0 && 
                    item.ItemCategory != DefaultItemCategories.Horse && 
                    item.ItemCategory != DefaultItemCategories.WarHorse &&
                    item.ItemCategory != DefaultItemCategories.LightArmor &&
                    item.ItemCategory != DefaultItemCategories.MediumArmor &&
                    item.ItemCategory != DefaultItemCategories.HeavyArmor && 
                    item.ItemCategory != DefaultItemCategories.UltraArmor &&
                    !item.ItemCategory.StringId.Contains("weapon"));
            
            foreach (var esya in tumUrunler)
            {
                int alisFiyati = MevcutSehir.GetItemPrice(esya);
                if (alisFiyati <= 0 || alisFiyati > alisSermayesi)
                    continue;
                    
                // En yakın 5 şehirde satış fiyatları kontrol ediliyor
                var enYakinSehirler = Campaign.Current.Settlements
                    .Where(s => s.IsTown || s.IsVillage)
                    .OrderBy(s => MevcutSehir.Settlement.Position2D.Distance(s.Position2D))
                    .Take(5)
                    .ToList();
                    
                int toplamSatisFiyati = 0;
                int sehirSayisi = 0;
                
                foreach (var sehir in enYakinSehirler)
                {
                    if (sehir == MevcutSehir.Settlement)
                        continue;
                        
                    if (sehir.IsTown || sehir.IsVillage)
                    {
                        Town town = sehir.IsTown ? sehir.Town : sehir.Village.Bound?.Town;
                        if (town != null)
                        {
                            int satisFiyati = town.GetItemPrice(esya);
                            if (satisFiyati > 0)
                            {
                                toplamSatisFiyati += satisFiyati;
                                sehirSayisi++;
                            }
                        }
                    }
                }
                
                if (sehirSayisi == 0)
                    continue;
                    
                // Ortalama satış fiyatını hesapla
                int ortSatisFiyati = toplamSatisFiyati / sehirSayisi;
                
                // Kâr oranını hesapla
                float karOrani = (float)(ortSatisFiyati - alisFiyati) / alisFiyati;
                
                // Kâr oranı %10'dan büyükse listeye ekle
                if (karOrani > 0.1f)
                {
                    satirKarlıÜrünler.Add((esya, alisFiyati, ortSatisFiyati, karOrani));
                }
            }
            
            // Kâr oranına göre sırala (en karlıdan en az karlıya)
            satirKarlıÜrünler = satirKarlıÜrünler.OrderByDescending(u => u.karOrani).ToList();
            
            // Her bir karlı üründen satın alınacak miktarı belirle
            foreach (var urun in satirKarlıÜrünler)
            {
                var esya = urun.item;
                var alisFiyati = urun.alisFiyati;
                var satisFiyati = urun.satisFiyati;
                var karOrani = urun.karOrani;
                
                // Ürün başına ağırlık
                float birimAgirlik = esya.Weight;
                if (birimAgirlik <= 0)
                    birimAgirlik = 0.1f;
                    
                // Satın alınabilecek maksimum miktar (sermaye ve boş kapasiteye göre)
                int maxMiktar = Math.Min(
                    alisSermayesi / alisFiyati,
                    (int)(bosKapasite / birimAgirlik)
                );
                
                // Satın alma miktarını kar oranına göre ayarla (daha karlı ürünler için daha fazla miktar)
                int alinacakMiktar = (int)(maxMiktar * Math.Min(karOrani * 2, 1.0f));
                
                if (alinacakMiktar <= 0)
                    continue;
                    
                // Miktarı sınırla
                alinacakMiktar = Math.Min(alinacakMiktar, 50); // Maksimum 50 adet
                
                // Ürünü satın al
                int toplamMaliyet = alinacakMiktar * alisFiyati;
                
                // Kargoyu güncelle
                if (Kargo.ContainsKey(esya))
                    Kargo[esya] += alinacakMiktar;
                else
                    Kargo[esya] = alinacakMiktar;
                    
                // Sermayeyi azalt
                Sermaye -= toplamMaliyet;
                alisSermayesi -= toplamMaliyet;
                
                // Kargo kapasitesini güncelle
                bosKapasite -= (int)(alinacakMiktar * birimAgirlik);
                
                // Eğer kapasitemiz veya sermayemiz kalmadıysa döngüden çık
                if (bosKapasite <= 0 || alisSermayesi <= 0)
                    break;
            }
        }
        
        /// <summary>
        /// Günlük giderleri uygular
        /// </summary>
        private void GunlukGiderUygula(float gecenGunler)
        {
            if (gecenGunler <= 0)
                return;
                
            // Temel günlük giderler
            int gunlukBakimMaliyeti = 10 + (GuvenlikSeviyesi * 15); // Güvenlik seviyesi başına +15
            
            // Toplam gider
            int toplamGider = (int)(gunlukBakimMaliyeti * gecenGunler);
            
            // Sermayeden düş
            Sermaye -= toplamGider;
        }
        
        /// <summary>
        /// Kervanın güvenlik seviyesini artırır
        /// </summary>
        /// <param name="artis">Artırılacak miktar</param>
        /// <returns>Başarılı olup olmadığı</returns>
        public bool GuvenligiArtir(int artis)
        {
            if (artis <= 0 || Sermaye <= 0)
                return false;
                
            // Mevcut güvenlik seviyesiyle orantılı maliyet
            int maliyet = 100 * artis * (1 + (GuvenlikSeviyesi / 20));
            
            if (maliyet > Sermaye)
                return false;
                
            // Güvenlik seviyesini artır
            GuvenlikSeviyesi = (GuvenlikSeviyesi + artis).Clamp(0, 100);
            
            // Mevcut güvenlik seviyesine göre risk faktörünü ayarla
            GuvenlikRiski = (1.0f - (GuvenlikSeviyesi / 100.0f)).Clamp(0.1f, 1.0f);
            
            // Sermayeyi azalt
            Sermaye -= maliyet;
            
            return true;
        }
        
        /// <summary>
        /// Kargo kapasitesini artırır
        /// </summary>
        /// <returns>Başarılı mı?</returns>
        public bool KapasiteyiArtir()
        {
            // Kapasite artırma maliyeti
            int maliyet = 500 + (KargoKapasitesi / 10);
            
            if (Sermaye < maliyet)
                return false;
                
            Sermaye -= maliyet;
            KargoKapasitesi += 200;
            
            return true;
        }
        
        /// <summary>
        /// Kervan hakkında özet bilgi verir
        /// </summary>
        /// <returns>Özet bilgi metni</returns>
        public string OzetBilgiAl()
        {
            string ozet = $"Kervan: {Ad}\n";
            ozet += $"Durum: {DurumAciklamasi}\n";
            ozet += $"Sermaye: {Sermaye} dinar\n";
            ozet += $"Toplam Kâr: {ToplamKar} dinar\n";
            ozet += $"Kargo: {KullanilanAlan}/{KargoKapasitesi} ({KargoDegeri} dinar değerinde)\n";
            ozet += $"Güvenlik: Seviye {GuvenlikSeviyesi}/3\n";
            ozet += $"Hız: {Hiz:F1} km/gün\n";
            
            if (Rota.Count > 0)
            {
                ozet += "Rota: ";
                foreach (var sehir in Rota)
                {
                    ozet += sehir.Name + " → ";
                }
                ozet += AnaSehir.Name + "\n";
            }
            
            return ozet;
        }
        
        #endregion
    }
} 