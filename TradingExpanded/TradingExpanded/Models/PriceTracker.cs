using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;
using TaleWorlds.SaveSystem;
using TradingExpanded.Helpers;

namespace TradingExpanded.Models
{
    /// <summary>
    /// Eşyaların fiyat geçmişini ve alım-satım işlemlerini takip eder
    /// </summary>
    public class PriceTracker
    {
        #region İç Sınıflar
        
        /// <summary>
        /// Belirli bir gündeki eşya fiyatını temsil eder
        /// </summary>
        public class FiyatKaydi
        {
            [SaveableProperty(101)]
            public CampaignTime Tarih { get; set; }
            
            [SaveableProperty(102)]
            public int Fiyat { get; set; }
            
            [SaveableProperty(103)]
            public Town Sehir { get; set; }
            
            public FiyatKaydi() { }
            
            public FiyatKaydi(int fiyat, Town sehir)
            {
                Tarih = CampaignTime.Now;
                Fiyat = fiyat;
                Sehir = sehir;
            }
        }
        
        /// <summary>
        /// Yapılan bir alım veya satım işlemini temsil eder
        /// </summary>
        public class IslemKaydi
        {
            /// <summary>
            /// İşlem tipleri
            /// </summary>
            public enum IslemTipi
            {
                Alim,
                Satim
            }
            
            [SaveableProperty(201)]
            public CampaignTime Tarih { get; set; }
            
            [SaveableProperty(202)]
            public ItemObject Esya { get; set; }
            
            [SaveableProperty(203)]
            public int Miktar { get; set; }
            
            [SaveableProperty(204)]
            public int BirimFiyat { get; set; }
            
            [SaveableProperty(205)]
            public Town Sehir { get; set; }
            
            [SaveableProperty(206)]
            public IslemTipi Tip { get; set; }
            
            public IslemKaydi() { }
            
            public IslemKaydi(ItemObject esya, int miktar, int birimFiyat, Town sehir, IslemTipi tip)
            {
                Tarih = CampaignTime.Now;
                Esya = esya;
                Miktar = miktar;
                BirimFiyat = birimFiyat;
                Sehir = sehir;
                Tip = tip;
            }
            
            /// <summary>
            /// İşlemin toplam değeri
            /// </summary>
            public int ToplamDeger => Miktar * BirimFiyat;
            
            /// <summary>
            /// İşlemin kısa açıklaması
            /// </summary>
            public string Aciklama
            {
                get
                {
                    string tipStr = Tip == IslemTipi.Alim ? "Alım" : "Satım";
                    return $"{tipStr}: {Miktar}x {Esya.Name} ({BirimFiyat} dinar/adet) - {Sehir.Name}";
                }
            }
        }
        
        #endregion
        
        #region Özellikler
        
        [SaveableProperty(301)]
        private Dictionary<string, List<FiyatKaydi>> FiyatGecmisi { get; set; }
        
        [SaveableProperty(302)]
        private List<IslemKaydi> IslemGecmisi { get; set; }
        
        [SaveableProperty(303)]
        private Dictionary<string, int> EnDusukFiyatlar { get; set; }
        
        [SaveableProperty(304)]
        private Dictionary<string, int> EnYuksekFiyatlar { get; set; }
        
        [SaveableProperty(305)]
        private Dictionary<string, int> OrtalamaSatisFiyatlari { get; set; }
        
        [SaveableProperty(306)]
        private Dictionary<string, int> OrtalalamaAlimFiyatlari { get; set; }
        
        [SaveableProperty(307)]
        private CampaignTime SonGuncellemeZamani { get; set; }
        
        [SaveableProperty(308)]
        private float GuvenlikRiski { get; set; }
        
        [SaveableProperty(309)]
        private float GuvenlikSeviyesi { get; set; }
        
        #endregion
        
        #region Yapılandırıcılar
        
        public PriceTracker()
        {
            FiyatGecmisi = new Dictionary<string, List<FiyatKaydi>>();
            IslemGecmisi = new List<IslemKaydi>();
            EnDusukFiyatlar = new Dictionary<string, int>();
            EnYuksekFiyatlar = new Dictionary<string, int>();
            OrtalamaSatisFiyatlari = new Dictionary<string, int>();
            OrtalalamaAlimFiyatlari = new Dictionary<string, int>();
            SonGuncellemeZamani = CampaignTime.Now;
            GuvenlikRiski = 1.0f;
            GuvenlikSeviyesi = 100.0f;
        }
        
        #endregion
        
        #region Public Metotlar
        
        /// <summary>
        /// Bir eşya için yeni fiyat kaydeder
        /// </summary>
        /// <param name="esya">Fiyatı kaydedilecek eşya</param>
        /// <param name="fiyat">Güncel fiyat</param>
        /// <param name="sehir">Fiyatın geçerli olduğu şehir</param>
        public void FiyatKaydet(ItemObject esya, int fiyat, Town sehir)
        {
            if (esya == null || fiyat <= 0 || sehir == null)
                return;
                
            string esyaId = esya.StringId;
            
            // Fiyat geçmişi yoksa oluştur
            if (!FiyatGecmisi.ContainsKey(esyaId))
            {
                FiyatGecmisi[esyaId] = new List<FiyatKaydi>();
            }
            
            // Bugün için zaten kayıt var mı kontrol et
            bool bugununKaydiVar = false;
            CampaignTime bugun = CampaignTime.Now;
            
            foreach (var kayit in FiyatGecmisi[esyaId])
            {
                // Aynı gün ve aynı şehir için kayıt varsa güncelleme yapma
                if (kayit.Sehir == sehir && 
                    kayit.Tarih.GetDayOfYear == bugun.GetDayOfYear && 
                    kayit.Tarih.GetYear == bugun.GetYear)
                {
                    bugununKaydiVar = true;
                    break;
                }
            }
            
            // Bugün için kayıt yoksa ekle
            if (!bugununKaydiVar)
            {
                var yeniFiyat = new FiyatKaydi(fiyat, sehir);
                FiyatGecmisi[esyaId].Add(yeniFiyat);
            }
            
            // Eski kayıtları temizle
            FiyatKayitlariniTemizle(esyaId);
            
            // En düşük/yüksek fiyatları güncelle
            if (!EnDusukFiyatlar.ContainsKey(esyaId) || fiyat < EnDusukFiyatlar[esyaId])
            {
                EnDusukFiyatlar[esyaId] = fiyat;
            }
            
            if (!EnYuksekFiyatlar.ContainsKey(esyaId) || fiyat > EnYuksekFiyatlar[esyaId])
            {
                EnYuksekFiyatlar[esyaId] = fiyat;
            }
        }
        
        /// <summary>
        /// İşlem kaydı ekler (alım veya satım)
        /// </summary>
        /// <param name="esya">İşleme konu olan eşya</param>
        /// <param name="miktar">İşlem miktarı</param>
        /// <param name="birimFiyat">Birim başına fiyat</param>
        /// <param name="sehir">İşlemin gerçekleştiği şehir</param>
        /// <param name="tip">İşlem tipi (alım/satım)</param>
        public void IslemKaydet(ItemObject esya, int miktar, int birimFiyat, Town sehir, IslemKaydi.IslemTipi tip)
        {
            if (esya == null || miktar <= 0 || birimFiyat <= 0 || sehir == null)
                return;
                
            // Bugün için zaten aynı işlem var mı kontrol et
            bool benzerIslemVar = false;
            CampaignTime bugun = CampaignTime.Now;
            
            foreach (var islem in IslemGecmisi)
            {
                // Aynı gün, aynı eşya, aynı şehir ve aynı tip için işlem varsa tekrarlamayı önle
                if (islem.Esya == esya && 
                    islem.Sehir == sehir && 
                    islem.Tip == tip &&
                    islem.Tarih.GetDayOfYear == bugun.GetDayOfYear && 
                    islem.Tarih.GetYear == bugun.GetYear)
                {
                    // Önceki işlemi güncelle
                    islem.Miktar += miktar;
                    benzerIslemVar = true;
                    break;
                }
            }
            
            // Benzer işlem yoksa yeni işlem oluştur
            if (!benzerIslemVar)
            {
                var islem = new IslemKaydi(esya, miktar, birimFiyat, sehir, tip);
                IslemGecmisi.Add(islem);
            }
            
            // İşlemin tipine göre ortalama fiyatı güncelle
            string esyaId = esya.StringId;
            
            if (tip == IslemKaydi.IslemTipi.Alim)
            {
                if (!OrtalalamaAlimFiyatlari.ContainsKey(esyaId))
                {
                    OrtalalamaAlimFiyatlari[esyaId] = birimFiyat;
                }
                else
                {
                    GuncelleOrtalama(OrtalalamaAlimFiyatlari, esyaId, birimFiyat);
                }
            }
            else // Satim
            {
                if (!OrtalamaSatisFiyatlari.ContainsKey(esyaId))
                {
                    OrtalamaSatisFiyatlari[esyaId] = birimFiyat;
                }
                else
                {
                    GuncelleOrtalama(OrtalamaSatisFiyatlari, esyaId, birimFiyat);
                }
            }
            
            // Maksimum 100 işlem sakla
            if (IslemGecmisi.Count > 100)
            {
                IslemGecmisi = IslemGecmisi.OrderByDescending(i => i.Tarih).Take(100).ToList();
            }
        }
        
        /// <summary>
        /// Belirli bir eşyanın fiyat geçmişini döndürür
        /// </summary>
        /// <param name="esya">Geçmişi istenilen eşya</param>
        /// <returns>Fiyat kayıtları</returns>
        public List<FiyatKaydi> FiyatGecmisiniGetir(ItemObject esya)
        {
            if (esya == null)
                return new List<FiyatKaydi>();
                
            string esyaId = esya.StringId;
            
            if (!FiyatGecmisi.ContainsKey(esyaId))
                return new List<FiyatKaydi>();
                
            return FiyatGecmisi[esyaId].OrderByDescending(f => f.Tarih).ToList();
        }
        
        /// <summary>
        /// Belirli bir şehir için eşyanın fiyat geçmişini döndürür
        /// </summary>
        /// <param name="esya">Geçmişi istenilen eşya</param>
        /// <param name="sehir">Şehir</param>
        /// <returns>Fiyat kayıtları</returns>
        public List<FiyatKaydi> FiyatGecmisiniGetir(ItemObject esya, Town sehir)
        {
            if (esya == null || sehir == null)
                return new List<FiyatKaydi>();
                
            var tumGecmis = FiyatGecmisiniGetir(esya);
            
            return tumGecmis.Where(f => f.Sehir == sehir).OrderByDescending(f => f.Tarih).ToList();
        }
        
        /// <summary>
        /// Belirli bir eşyanın en düşük fiyatını döndürür
        /// </summary>
        /// <param name="esya">Eşya</param>
        /// <returns>En düşük fiyat (kayıt yoksa 0)</returns>
        public int EnDusukFiyatiGetir(ItemObject esya)
        {
            if (esya == null)
                return 0;
                
            string esyaId = esya.StringId;
            
            if (!EnDusukFiyatlar.ContainsKey(esyaId))
                return 0;
                
            return EnDusukFiyatlar[esyaId];
        }
        
        /// <summary>
        /// Belirli bir eşyanın en yüksek fiyatını döndürür
        /// </summary>
        /// <param name="esya">Eşya</param>
        /// <returns>En yüksek fiyat (kayıt yoksa 0)</returns>
        public int EnYuksekFiyatiGetir(ItemObject esya)
        {
            if (esya == null)
                return 0;
                
            string esyaId = esya.StringId;
            
            if (!EnYuksekFiyatlar.ContainsKey(esyaId))
                return 0;
                
            return EnYuksekFiyatlar[esyaId];
        }
        
        /// <summary>
        /// Belirli bir eşyanın ortalama alım fiyatını döndürür
        /// </summary>
        /// <param name="esya">Eşya</param>
        /// <returns>Ortalama alım fiyatı (kayıt yoksa 0)</returns>
        public int OrtalamaAlimFiyatiGetir(ItemObject esya)
        {
            if (esya == null)
                return 0;
                
            string esyaId = esya.StringId;
            
            if (!OrtalalamaAlimFiyatlari.ContainsKey(esyaId))
                return 0;
                
            return OrtalalamaAlimFiyatlari[esyaId];
        }
        
        /// <summary>
        /// Belirli bir eşyanın ortalama satış fiyatını döndürür
        /// </summary>
        /// <param name="esya">Eşya</param>
        /// <returns>Ortalama satış fiyatı (kayıt yoksa 0)</returns>
        public int OrtalamaSatisFiyatiGetir(ItemObject esya)
        {
            if (esya == null)
                return 0;
                
            string esyaId = esya.StringId;
            
            if (!OrtalamaSatisFiyatlari.ContainsKey(esyaId))
                return 0;
                
            return OrtalamaSatisFiyatlari[esyaId];
        }
        
        /// <summary>
        /// Son işlemleri döndürür
        /// </summary>
        /// <param name="adet">İstenen işlem sayısı</param>
        /// <returns>Son işlemler</returns>
        public List<IslemKaydi> SonIslemleriGetir(int adet = 10)
        {
            return IslemGecmisi.OrderByDescending(i => i.Tarih).Take(adet).ToList();
        }
        
        /// <summary>
        /// Belirli bir eşya için en karlı şehri bulur
        /// </summary>
        /// <param name="esya">Eşya</param>
        /// <returns>En karlı şehir ve fiyat farkı</returns>
        public (Town Sehir, int FiyatFarki) EnKarliSehriGetir(ItemObject esya)
        {
            if (esya == null)
                return (null, 0);
                
            var sehirler = Town.AllTowns;
            Town enUcuzSehir = null;
            Town enPahaliSehir = null;
            int enDusukFiyat = int.MaxValue;
            int enYuksekFiyat = 0;
            
            foreach (var sehir in sehirler)
            {
                int fiyat = sehir.GetItemPrice(esya);
                
                if (fiyat <= 0)
                    continue;
                    
                if (fiyat < enDusukFiyat)
                {
                    enDusukFiyat = fiyat;
                    enUcuzSehir = sehir;
                }
                
                if (fiyat > enYuksekFiyat)
                {
                    enYuksekFiyat = fiyat;
                    enPahaliSehir = sehir;
                }
            }
            
            int fiyatFarki = enYuksekFiyat - enDusukFiyat;
            
            return (enPahaliSehir, fiyatFarki);
        }
        
        /// <summary>
        /// Eşyanın fiyat dalgalanması hakkında bilgi verir
        /// </summary>
        /// <param name="esya">Eşya</param>
        /// <returns>Dalgalanma oranı (0-1 arası, yüksek değer daha değişken)</returns>
        public float FiyatDalgalanmaOrani(ItemObject esya)
        {
            if (esya == null)
                return 0f;
                
            string esyaId = esya.StringId;
            
            if (!FiyatGecmisi.ContainsKey(esyaId) || FiyatGecmisi[esyaId].Count < 2)
                return 0f;
                
            var fiyatlar = FiyatGecmisi[esyaId].Select(f => f.Fiyat).ToList();
            int minFiyat = fiyatlar.Min();
            int maxFiyat = fiyatlar.Max();
            float ortalamaFiyat = (float)fiyatlar.Average();
            
            if (ortalamaFiyat <= 0)
                return 0f;
                
            // Dalgalanma oranı = (Max - Min) / Ortalama
            return (maxFiyat - minFiyat) / ortalamaFiyat;
        }
        
        /// <summary>
        /// Tüm eşyaların fiyatlarını güncelleyerek yeni bir gün için hazırlık yapar
        /// </summary>
        public void GunlukGuncelle()
        {
            // Tüm eşyaların günlük fiyat dalgalanmasını hesapla
            var items = MBObjectManager.Instance.GetObjectTypeList<ItemObject>()
                .Where(item => item.Value > 0)
                .ToList();
                
            foreach (var item in items)
            {
                // Eşya için tüm şehirlerdeki fiyatları karşılaştır
                var sehirler = Settlement.All
                    .Where(s => s.IsTown)
                    .Select(s => s.Town)
                    .ToList();
                    
                foreach (var sehir in sehirler)
                {
                    // Şehirdeki mevcut fiyatı kaydet
                    int fiyat = sehir.GetItemPrice(item);
                    if (fiyat > 0)
                    {
                        FiyatKaydet(item, fiyat, sehir);
                    }
                }
            }
        }
        
        #endregion
        
        #region Yardımcı Metotlar
        
        /// <summary>
        /// Eski fiyat kayıtlarını temizler
        /// </summary>
        private void FiyatKayitlariniTemizle(string esyaId)
        {
            if (!FiyatGecmisi.ContainsKey(esyaId))
                return;
                
            // Maksimum kayıt süresi
            int maxGun = Settings.Instance?.MaxPriceHistoryDays ?? 60;
            
            // Şu anki zaman
            var simdikiZaman = CampaignTime.Now;
            
            // Maksimum süreden eski kayıtları temizle
            FiyatGecmisi[esyaId] = FiyatGecmisi[esyaId]
                .Where(kayit => (simdikiZaman - kayit.Tarih).ToDays <= maxGun)
                .ToList();
        }
        
        /// <summary>
        /// Ortalama fiyatı günceller
        /// </summary>
        private void GuncelleOrtalama(Dictionary<string, int> sozluk, string esyaId, int yeniFiyat)
        {
            if (!sozluk.ContainsKey(esyaId))
            {
                sozluk[esyaId] = yeniFiyat;
            }
            else
            {
                // Yeni fiyatı %20 ağırlıkla hesaba kat
                int eskiOrtalama = sozluk[esyaId];
                int yeniOrtalama = (int)(eskiOrtalama * 0.8f + yeniFiyat * 0.2f);
                sozluk[esyaId] = yeniOrtalama;
            }
        }
        
        #endregion
    }
} 