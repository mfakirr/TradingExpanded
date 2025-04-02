using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using TaleWorlds.SaveSystem;
using TradingExpanded.Helpers;
using TradingExpanded.UI.Patches;

namespace TradingExpanded.Models
{
    /// <summary>
    /// Oyuncunun şehirlerde kurabileceği toptan ticaret dükkanlarını temsil eder
    /// </summary>
    public class WholesaleShop
    {
        #region Özellikler
        
        [SaveableProperty(1)]
        public string Id { get; private set; }
        
        [SaveableProperty(2)]
        public Town Town { get; private set; }
        
        [SaveableProperty(3)]
        public int Capital { get; private set; }
        
        [SaveableProperty(4)]
        public Dictionary<ItemObject, int> Inventory { get; private set; }
        
        [SaveableProperty(5)]
        public bool IsActive { get; set; }
        
        [SaveableProperty(6)]
        public CampaignTime LastUpdateTime { get; private set; }
        
        [SaveableProperty(7)]
        public int DailyExpenses { get; private set; }
        
        [SaveableProperty(8)]
        public int DailyProfit { get; private set; }
        
        [SaveableProperty(9)]
        public int TotalProfit { get; private set; }
        
        [SaveableProperty(10)]
        public CampaignTime EstablishmentDate { get; private set; }
        
        [SaveableProperty(11)]
        public Dictionary<ItemObject, WholesaleBuyOrder> BuyOrders { get; private set; }
        
        [SaveableProperty(12)]
        public Dictionary<ItemObject, WholesaleSellOrder> SellOrders { get; private set; }
        
        [SaveableProperty(13)]
        public float ProfitMargin { get; set; }
        
        [SaveableProperty(14)]
        public List<WholesaleEmployee> Employees { get; private set; }
        
        #endregion
        
        #region Hesaplanan Özellikler
        
        /// <summary>
        /// Dükkanın envanter değeri
        /// </summary>
        public int InventoryValue
        {
            get
            {
                int value = 0;
                
                foreach (var item in Inventory.Keys)
                {
                    int quantity = Inventory[item];
                    int price = Town.GetItemPrice(item);
                    
                    value += quantity * price;
                }
                
                return value;
            }
        }
        
        /// <summary>
        /// Dükkanın toplam varlık değeri (sermaye + envanter)
        /// </summary>
        public int TotalValue => Capital + InventoryValue;
        
        /// <summary>
        /// Dükkanın günlük net kârı
        /// </summary>
        public int DailyNetProfit => DailyProfit - DailyExpenses;
        
        /// <summary>
        /// Dükkanın kârlılık oranı
        /// </summary>
        public float ProfitabilityRate
        {
            get
            {
                if (TotalValue <= 0)
                    return 0f;
                    
                return (float)TotalProfit / TotalValue;
            }
        }
        
        /// <summary>
        /// Dükkanın kuruluşundan bu yana geçen gün sayısı
        /// </summary>
        public int DaysSinceEstablishment
        {
            get
            {
                if (EstablishmentDate == CampaignTime.Zero)
                    return 0;
                    
                return (int)(CampaignTime.Now - EstablishmentDate).ToDays;
            }
        }
        
        /// <summary>
        /// Günlük ortalama kâr
        /// </summary>
        public float AverageDailyProfit
        {
            get
            {
                if (DaysSinceEstablishment <= 0)
                    return 0f;
                    
                return (float)TotalProfit / DaysSinceEstablishment;
            }
        }
        
        /// <summary>
        /// Aktif alım siparişlerinin sayısı
        /// </summary>
        public int ActiveBuyOrderCount => BuyOrders.Count(o => o.Value.IsActive);
        
        /// <summary>
        /// Aktif satış siparişlerinin sayısı
        /// </summary>
        public int ActiveSellOrderCount => SellOrders.Count(o => o.Value.IsActive);
        
        #endregion
        
        #region Yapılandırıcılar
        
        public WholesaleShop()
        {
            Inventory = new Dictionary<ItemObject, int>();
            BuyOrders = new Dictionary<ItemObject, WholesaleBuyOrder>();
            SellOrders = new Dictionary<ItemObject, WholesaleSellOrder>();
            Employees = new List<WholesaleEmployee>();
        }
        
        public WholesaleShop(Town town, int initialCapital = 5000)
        {
            if (town == null)
                throw new ArgumentNullException(nameof(town));
                
            Id = IdGenerator.GenerateUniqueId();
            Town = town;
            Capital = initialCapital;
            IsActive = true;
            LastUpdateTime = CampaignTime.Now;
            EstablishmentDate = CampaignTime.Now;
            
            // Varsayılan kâr marjı
            ProfitMargin = Settings.Instance?.WholesaleProfitMargin ?? 0.15f;
            
            // Boş envanter başlat
            Inventory = new Dictionary<ItemObject, int>();
            BuyOrders = new Dictionary<ItemObject, WholesaleBuyOrder>();
            SellOrders = new Dictionary<ItemObject, WholesaleSellOrder>();
            Employees = new List<WholesaleEmployee>();
            
            DailyExpenses = CalculateDailyExpenses();
            DailyProfit = 0;
            TotalProfit = 0;
        }
        
        #endregion
        
        #region Metotlar
        
        /// <summary>
        /// Dükkanı günlük olarak günceller
        /// </summary>
        public void UpdateShop()
        {
            if (!IsActive)
                return;
                
            // Son güncellemeden bu yana geçen gün sayısını hesapla
            var elapsedDays = (CampaignTime.Now - LastUpdateTime).ToDays;
            
            if (elapsedDays < 0.5f)
                return; // Günün yarısından az geçmişse güncelleme yapma
                
            // Günlük giderleri düş
            int expenses = (int)(DailyExpenses * elapsedDays);
            Capital -= expenses;
            
            // Otomatik alım-satım işlemleri
            int profit = ProcessAutomaticTrading((int)elapsedDays);
            Capital += profit;
            
            // Aktif siparişleri güncelle
            UpdateBuyOrders();
            UpdateSellOrders();
            
            // Günlük istatistikleri güncelle
            TotalProfit += profit;
            DailyProfit = (int)(profit / elapsedDays);
            
            // Eğer sermaye yetersizse dükkan kapanabilir
            if (Capital <= 0)
            {
                // Sermayenin %10'u kadar borç oluştur ve devam et
                int debt = Math.Min(1000, (int)(TotalValue * 0.1f));
                
                if (debt > 0)
                {
                    Capital += debt;
                }
                else
                {
                    // Sermaye tamamen tükendi, dükkanı kapat
                    IsActive = false;
                }
            }
            
            LastUpdateTime = CampaignTime.Now;
        }
        
        /// <summary>
        /// Otomatik alım-satım işlemlerini gerçekleştirir
        /// </summary>
        /// <param name="days">Geçen gün sayısı</param>
        /// <returns>Elde edilen kâr</returns>
        private int ProcessAutomaticTrading(int days)
        {
            int profit = 0;
            
            // Günlük alım-satım potansiyeli hesapla
            int tradingVolume = CalculateTradingVolume(days);
            
            if (tradingVolume <= 0)
                return 0;
                
            // Kârlı eşyaları tespit et
            var profitableItems = IdentifyProfitableItems();
            
            // En kârlı eşyalardan başlayarak ticaret yap
            int remainingVolume = tradingVolume;
            
            foreach (var itemData in profitableItems)
            {
                if (remainingVolume <= 0)
                    break;
                    
                ItemObject item = itemData.Item;
                float potentialProfit = itemData.PotentialProfit;
                
                // Eşyanın mevcut fiyatı
                int currentPrice = Town.GetItemPrice(item);
                
                // Alım miktarını belirle
                int maxQuantity = Math.Min(remainingVolume / currentPrice, 50);
                
                if (maxQuantity <= 0)
                    continue;
                    
                // Eşyadan ne kadar alınabilir hesapla
                int availableQuantity = CalculateItemAvailability(item, maxQuantity);
                
                if (availableQuantity <= 0)
                    continue;
                    
                // Alım-satım maliyeti ve kârı hesapla
                int purchaseCost = availableQuantity * currentPrice;
                int expectedSaleValue = (int)(purchaseCost * (1f + ProfitMargin));
                int itemProfit = expectedSaleValue - purchaseCost;
                
                // İşlemi gerçekleştir
                if (purchaseCost <= Capital && itemProfit > 0)
                {
                    // Sermayeden düş
                    Capital -= purchaseCost;
                    
                    // Envantere ekle
                    AddToInventory(item, availableQuantity);
                    
                    // Otomatik satış simülasyonu (yarısını hemen sat)
                    int saleQuantity = availableQuantity / 2;
                    
                    if (saleQuantity > 0)
                    {
                        int saleProfit = (int)(saleQuantity * currentPrice * ProfitMargin);
                        profit += saleProfit;
                        
                        // Envanterden düş
                        RemoveFromInventory(item, saleQuantity);
                    }
                    
                    // Kalan işlem hacmini güncelle
                    remainingVolume -= purchaseCost;
                }
            }
            
            return profit;
        }
        
        /// <summary>
        /// Eşya için alım siparişi oluşturur
        /// </summary>
        /// <param name="item">Alınacak eşya</param>
        /// <param name="targetPrice">Hedef fiyat</param>
        /// <param name="quantity">Miktar</param>
        /// <param name="expiryDays">Siparişin geçerlilik süresi (gün)</param>
        /// <returns>Oluşturulan sipariş nesnesi</returns>
        public WholesaleBuyOrder CreateBuyOrder(ItemObject item, int targetPrice, int quantity, int expiryDays)
        {
            if (item == null || targetPrice <= 0 || quantity <= 0 || expiryDays <= 0)
                return null;
                
            // Mevcut siparişi kontrol et
            if (BuyOrders.TryGetValue(item, out var existingOrder) && existingOrder.IsActive)
            {
                // Mevcut siparişi güncelle
                existingOrder.UpdateOrder(targetPrice, quantity, expiryDays);
                return existingOrder;
            }
            
            // Yeni sipariş oluştur
            var order = new WholesaleBuyOrder(item, targetPrice, quantity, expiryDays);
            BuyOrders[item] = order;
            
            return order;
        }
        
        /// <summary>
        /// Eşya için satış siparişi oluşturur
        /// </summary>
        /// <param name="item">Satılacak eşya</param>
        /// <param name="targetPrice">Hedef fiyat</param>
        /// <param name="quantity">Miktar</param>
        /// <param name="expiryDays">Siparişin geçerlilik süresi (gün)</param>
        /// <returns>Oluşturulan sipariş nesnesi</returns>
        public WholesaleSellOrder CreateSellOrder(ItemObject item, int targetPrice, int quantity, int expiryDays)
        {
            if (item == null || targetPrice <= 0 || quantity <= 0 || expiryDays <= 0)
                return null;
                
            // Envanterde yeterli eşya var mı kontrol et
            int availableQuantity = 0;
            if (Inventory.TryGetValue(item, out availableQuantity) && availableQuantity < quantity)
                return null;
                
            // Mevcut siparişi kontrol et
            if (SellOrders.TryGetValue(item, out var existingOrder) && existingOrder.IsActive)
            {
                // Mevcut siparişi güncelle
                existingOrder.UpdateOrder(targetPrice, quantity, expiryDays);
                return existingOrder;
            }
            
            // Yeni sipariş oluştur
            var order = new WholesaleSellOrder(item, targetPrice, quantity, expiryDays);
            SellOrders[item] = order;
            
            // Envanterden düş
            RemoveFromInventory(item, quantity);
            
            return order;
        }
        
        /// <summary>
        /// Alım siparişlerini günceller
        /// </summary>
        private void UpdateBuyOrders()
        {
            foreach (var order in BuyOrders.Values.ToList())
            {
                if (!order.IsActive)
                    continue;
                    
                // Siparişi güncelle
                order.Update();
                
                // Sipariş süresi dolmuşsa devam et
                if (!order.IsActive)
                    continue;
                    
                // Piyasa fiyatı hedef fiyatın altına düştü mü kontrol et
                int currentPrice = Town.GetItemPrice(order.Item);
                
                if (currentPrice <= order.TargetPrice)
                {
                    // Eşya alınabilir
                    int maxQuantity = Math.Min(order.RemainingQuantity, Capital / currentPrice);
                    
                    if (maxQuantity > 0)
                    {
                        // Alım işlemini gerçekleştir
                        int totalCost = maxQuantity * currentPrice;
                        
                        // Sermayeden düş
                        Capital -= totalCost;
                        
                        // Envantere ekle
                        AddToInventory(order.Item, maxQuantity);
                        
                        // Siparişi güncelle
                        order.FulfillPartial(maxQuantity);
                    }
                }
            }
        }
        
        /// <summary>
        /// Satış siparişlerini günceller
        /// </summary>
        private void UpdateSellOrders()
        {
            foreach (var order in SellOrders.Values.ToList())
            {
                if (!order.IsActive)
                    continue;
                    
                // Siparişi güncelle
                order.Update();
                
                // Sipariş süresi dolmuşsa ve satılmamışsa envantere geri ekle
                if (!order.IsActive && order.RemainingQuantity > 0)
                {
                    AddToInventory(order.Item, order.RemainingQuantity);
                    continue;
                }
                
                // Piyasa fiyatı hedef fiyatın üzerine çıktı mı kontrol et
                int currentPrice = Town.GetItemPrice(order.Item);
                
                if (currentPrice >= order.TargetPrice)
                {
                    // Eşya satılabilir
                    int quantity = order.RemainingQuantity;
                    
                    if (quantity > 0)
                    {
                        // Satış işlemini gerçekleştir
                        int totalValue = quantity * currentPrice;
                        
                        // Sermayeye ekle
                        Capital += totalValue;
                        
                        // Siparişi güncelle
                        order.FulfillPartial(quantity);
                        
                        // Kâr kaydet
                        int purchaseCost = quantity * order.PurchasePrice;
                        int profit = totalValue - purchaseCost;
                        
                        TotalProfit += profit;
                    }
                }
            }
        }
        
        /// <summary>
        /// Envantere eşya ekler
        /// </summary>
        /// <param name="item">Eklenecek eşya</param>
        /// <param name="quantity">Miktar</param>
        public void AddToInventory(ItemObject item, int quantity)
        {
            if (item == null || quantity <= 0)
                return;
                
            if (Inventory.ContainsKey(item))
            {
                Inventory[item] += quantity;
            }
            else
            {
                Inventory[item] = quantity;
            }
        }
        
        /// <summary>
        /// Envanterden eşya çıkarır
        /// </summary>
        /// <param name="item">Çıkarılacak eşya</param>
        /// <param name="quantity">Miktar</param>
        /// <returns>Gerçekten çıkarılan miktar</returns>
        public int RemoveFromInventory(ItemObject item, int quantity)
        {
            if (item == null || quantity <= 0)
                return 0;
                
            if (!Inventory.ContainsKey(item))
                return 0;
                
            int availableQuantity = Inventory[item];
            int amountToRemove = Math.Min(availableQuantity, quantity);
            
            if (amountToRemove <= 0)
                return 0;
                
            Inventory[item] -= amountToRemove;
            
            // Miktar sıfırsa envanterden tamamen kaldır
            if (Inventory[item] <= 0)
            {
                Inventory.Remove(item);
            }
            
            return amountToRemove;
        }
        
        /// <summary>
        /// Günlük giderleri hesaplar
        /// </summary>
        /// <returns>Günlük gider miktarı</returns>
        private int CalculateDailyExpenses()
        {
            int baseExpenses = Constants.BaseShopMaintenanceCost;
            
            // Köylerde bakım maliyeti daha düşük
            if (Town.IsVillage())
            {
                baseExpenses = (int)(baseExpenses * 0.7f);
            }
            
            // Şehir refahı bakım maliyetini etkiler - Town.Prosperity yerine güvenli metodu kullan
            float prosperity = SettlementMenuPatch.GetTownProsperityValue(Town);
            float prosperityFactor = 1f + (prosperity / 10000f);
            int expenses = (int)(baseExpenses * prosperityFactor);
            
            // Her çalışan ek bakım maliyeti ekler
            if (Employees != null && Employees.Any())
            {
                expenses += Employees.Sum(e => e.DailyWage);
            }
            
            return expenses;
        }
        
        /// <summary>
        /// Şehirde kârlı olabilecek eşyaları belirler
        /// </summary>
        /// <returns>Kâr potansiyeline göre sıralanmış eşya listesi</returns>
        private List<(ItemObject Item, float PotentialProfit)> IdentifyProfitableItems()
        {
            // Satışa uygun ticari malların listesini oluştur
            var tradableItems = MBObjectManager.Instance.GetObjectTypeList<ItemObject>()
                .Where(item => 
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
                    item.Value > 0)
                .ToList();
                
            // Her eşya için potansiyel kâr hesapla
            var profitableItems = new List<(ItemObject Item, float PotentialProfit)>();
            
            foreach (var item in tradableItems)
            {
                // Şehrin alış fiyatı
                int buyPrice = Town.GetItemPrice(item);
                
                // Civar şehirlerdeki ortalama satış fiyatı
                float averageSellPrice = EstimateAverageSellPrice(item);
                
                if (averageSellPrice > buyPrice)
                {
                    float profit = (averageSellPrice - buyPrice) / buyPrice * 100f;
                    profitableItems.Add((item, profit));
                }
            }
            
            // Kâr potansiyeline göre sırala
            return profitableItems
                .OrderByDescending(pair => pair.PotentialProfit)
                .ToList();
        }
        
        /// <summary>
        /// Civar şehirlerdeki ortalama satış fiyatını hesaplar
        /// </summary>
        /// <param name="item">Fiyatı hesaplanacak eşya</param>
        /// <returns>Tahmini ortalama satış fiyatı</returns>
        private float EstimateAverageSellPrice(ItemObject item)
        {
            if (item == null)
                return 0f;
                
            // Yakındaki şehirleri bul
            var nearbyTowns = Settlement.All
                .Where(s => s.IsTown && s != Town.Settlement)
                .OrderBy(s => Town.Settlement.Position2D.Distance(s.Position2D))
                .Take(5)
                .Select(s => s.Town)
                .ToList();
                
            if (nearbyTowns.Count == 0)
                return Town.GetItemPrice(item) * 1.2f; // Yakın şehir yoksa %20 ekle
                
            // Yakındaki şehirlerin ortalama fiyatını hesapla
            float totalPrice = 0f;
            int townCount = 0;
            
            foreach (var town in nearbyTowns)
            {
                int price = town.GetItemPrice(item);
                if (price > 0)
                {
                    totalPrice += price;
                    townCount++;
                }
            }
            
            // Ortalamayı hesapla
            if (townCount > 0)
            {
                return totalPrice / townCount;
            }
            
            // Yakın şehirlerde satılmıyorsa mevcut şehir fiyatından hesapla
            return Town.GetItemPrice(item) * 1.2f;
        }
        
        /// <summary>
        /// Şehirde bir eşyadan ne kadar bulunabileceğini hesaplar
        /// </summary>
        /// <param name="item">Kontrol edilecek eşya</param>
        /// <param name="desiredQuantity">İstenen miktar</param>
        /// <returns>Bulunabilecek miktar</returns>
        private int CalculateItemAvailability(ItemObject item, int desiredQuantity)
        {
            if (item == null || desiredQuantity <= 0)
                return 0;
                
            // Şehir pazarında bulunabilecek miktar
            float prosperity = SettlementMenuPatch.GetTownProsperityValue(Town);
            int baseAvailability = (int)(prosperity / 1000);
            
            // Şehrin üretim ürünleri için daha fazla bulunabilirlik
            bool isLocalProduct = Town.Settlement.Village?.VillageType?.PrimaryProduction?.Equals(item) ?? false;
            
            if (isLocalProduct)
            {
                baseAvailability *= 3;
            }
            
            // Minimum 1, maksimum istenen miktar
            return Math.Min(desiredQuantity, Math.Max(1, baseAvailability));
        }
        
        /// <summary>
        /// Günlük ticaret hacmini hesaplar
        /// </summary>
        /// <param name="days">Geçen gün sayısı</param>
        /// <returns>Ticaret hacmi</returns>
        private int CalculateTradingVolume(int days)
        {
            if (days <= 0)
                return 0;
                
            // Temel ticaret hacmi (sermayenin %20'si)
            int baseVolume = (int)(Capital * 0.2f);
            
            // Şehir refahına göre ayarlama
            float prosperityFactor = SettlementMenuPatch.GetTownProsperityValue(Town) / 5000f;
            baseVolume = (int)(baseVolume * prosperityFactor);
            
            // Günlük hacim (minimum 100)
            int dailyVolume = Math.Max(100, baseVolume);
            
            return dailyVolume * days;
        }
        
        /// <summary>
        /// Dükkan hakkında özet bilgi verir
        /// </summary>
        /// <returns>Özet bilgi metni</returns>
        public string GetSummary()
        {
            string summary = $"Toptan Ticaret Dükkanı - {Town.Name}\n";
            summary += $"Sermaye: {Capital} dinar\n";
            summary += $"Envanter Değeri: {InventoryValue} dinar\n";
            summary += $"Envanterdeki Eşya: {Inventory.Count} çeşit\n";
            summary += $"Günlük Giderler: {DailyExpenses} dinar\n";
            summary += $"Günlük Kâr: {DailyProfit} dinar\n";
            summary += $"Toplam Kâr: {TotalProfit} dinar\n";
            summary += $"Kâr Marjı: %{ProfitMargin * 100:F1}\n";
            summary += $"Aktif Emirler: {ActiveBuyOrderCount} alım, {ActiveSellOrderCount} satış\n";
            
            return summary;
        }
        
        #endregion
    }
    
    /// <summary>
    /// Toptan alım siparişini temsil eder
    /// </summary>
    public class WholesaleBuyOrder
    {
        [SaveableProperty(1)]
        public ItemObject Item { get; private set; }
        
        [SaveableProperty(2)]
        public int TargetPrice { get; private set; }
        
        [SaveableProperty(3)]
        public int Quantity { get; private set; }
        
        [SaveableProperty(4)]
        public int RemainingQuantity { get; private set; }
        
        [SaveableProperty(5)]
        public CampaignTime ExpiryDate { get; private set; }
        
        [SaveableProperty(6)]
        public bool IsActive { get; private set; }
        
        public WholesaleBuyOrder() { }
        
        public WholesaleBuyOrder(ItemObject item, int targetPrice, int quantity, int expiryDays)
        {
            Item = item;
            TargetPrice = targetPrice;
            Quantity = quantity;
            RemainingQuantity = quantity;
            ExpiryDate = CampaignTime.Now.AddDays(expiryDays);
            IsActive = true;
        }
        
        public void Update()
        {
            // Süre dolmuş veya miktar tamamlanmışsa deaktif et
            if (CampaignTime.Now >= ExpiryDate || RemainingQuantity <= 0)
            {
                IsActive = false;
            }
        }
        
        public void FulfillPartial(int amount)
        {
            if (amount <= 0 || amount > RemainingQuantity)
                return;
                
            RemainingQuantity -= amount;
            
            if (RemainingQuantity <= 0)
            {
                IsActive = false;
            }
        }
        
        public void UpdateOrder(int newTargetPrice, int additionalQuantity, int newExpiryDays)
        {
            TargetPrice = newTargetPrice;
            Quantity += additionalQuantity;
            RemainingQuantity += additionalQuantity;
            ExpiryDate = CampaignTime.Now.AddDays(newExpiryDays);
            IsActive = true;
        }
    }
    
    /// <summary>
    /// Toptan satış siparişini temsil eder
    /// </summary>
    public class WholesaleSellOrder
    {
        [SaveableProperty(1)]
        public ItemObject Item { get; private set; }
        
        [SaveableProperty(2)]
        public int TargetPrice { get; private set; }
        
        [SaveableProperty(3)]
        public int Quantity { get; private set; }
        
        [SaveableProperty(4)]
        public int RemainingQuantity { get; private set; }
        
        [SaveableProperty(5)]
        public CampaignTime ExpiryDate { get; private set; }
        
        [SaveableProperty(6)]
        public bool IsActive { get; private set; }
        
        [SaveableProperty(7)]
        public int PurchasePrice { get; private set; }
        
        public WholesaleSellOrder() { }
        
        public WholesaleSellOrder(ItemObject item, int targetPrice, int quantity, int expiryDays)
        {
            Item = item;
            TargetPrice = targetPrice;
            Quantity = quantity;
            RemainingQuantity = quantity;
            ExpiryDate = CampaignTime.Now.AddDays(expiryDays);
            IsActive = true;
            
            // Satış için ortalama alım fiyatını kaydet
            PurchasePrice = (int)(targetPrice * 0.8f); // Varsayılan olarak hedef fiyatın %80'i
        }
        
        public void Update()
        {
            // Süre dolmuş veya miktar tamamlanmışsa deaktif et
            if (CampaignTime.Now >= ExpiryDate || RemainingQuantity <= 0)
            {
                IsActive = false;
            }
        }
        
        public void FulfillPartial(int amount)
        {
            if (amount <= 0 || amount > RemainingQuantity)
                return;
                
            RemainingQuantity -= amount;
            
            if (RemainingQuantity <= 0)
            {
                IsActive = false;
            }
        }
        
        public void UpdateOrder(int newTargetPrice, int additionalQuantity, int newExpiryDays)
        {
            TargetPrice = newTargetPrice;
            Quantity += additionalQuantity;
            RemainingQuantity += additionalQuantity;
            ExpiryDate = CampaignTime.Now.AddDays(newExpiryDays);
            IsActive = true;
        }
    }
} 