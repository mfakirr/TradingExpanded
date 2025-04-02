using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;
using TradingExpanded.Models;

namespace TradingExpanded.Services
{
    /// <summary>
    /// IInventoryTrackerService arayüzünün uygulaması
    /// </summary>
    public class InventoryTrackerService : IInventoryTrackerService
    {
        private readonly InventoryTracker _inventoryTracker;
        
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="inventoryTracker">Var olan InventoryTracker nesnesi</param>
        public InventoryTrackerService(InventoryTracker inventoryTracker)
        {
            _inventoryTracker = inventoryTracker ?? new InventoryTracker();
        }
        
        /// <summary>
        /// Tüm ürün istatistiklerini getirir
        /// </summary>
        public Dictionary<ItemObject, ItemStats> GetAllItemStats()
        {
            return _inventoryTracker.ItemStatistics;
        }
        
        /// <summary>
        /// Bir ürünün istatistiklerini getirir
        /// </summary>
        public ItemStats GetItemStats(ItemObject item)
        {
            if (item == null)
                return null;
                
            if (!_inventoryTracker.ItemStatistics.ContainsKey(item))
                _inventoryTracker.ItemStatistics[item] = new ItemStats();
                
            return _inventoryTracker.ItemStatistics[item];
        }
        
        /// <summary>
        /// Şehirdeki tüm ürünlerin fiyat geçmişini getirir
        /// </summary>
        public Dictionary<ItemObject, PriceHistory> GetPriceHistoryInTown(Town town)
        {
            if (town == null)
                return new Dictionary<ItemObject, PriceHistory>();
                
            if (!_inventoryTracker.PriceHistory.ContainsKey(town))
                _inventoryTracker.PriceHistory[town] = new Dictionary<ItemObject, PriceHistory>();
                
            return _inventoryTracker.PriceHistory[town];
        }
        
        /// <summary>
        /// Bir ürünün belirli bir şehirdeki fiyat geçmişini getirir
        /// </summary>
        public PriceHistory GetPriceHistory(ItemObject item, Town town)
        {
            if (item == null || town == null)
                return null;
                
            if (!_inventoryTracker.PriceHistory.ContainsKey(town))
                _inventoryTracker.PriceHistory[town] = new Dictionary<ItemObject, PriceHistory>();
                
            if (!_inventoryTracker.PriceHistory[town].ContainsKey(item))
                _inventoryTracker.PriceHistory[town][item] = new PriceHistory
                {
                    Item = item,
                    Town = town,
                    DataPoints = new List<PriceDataPoint>()
                };
                
            return _inventoryTracker.PriceHistory[town][item];
        }
        
        /// <summary>
        /// Bir ürünün en karlı alım yeri
        /// </summary>
        public Town GetBestBuyLocation(ItemObject item)
        {
            if (item == null)
                return null;
                
            if (!_inventoryTracker.ItemStatistics.ContainsKey(item))
                return null;
                
            return _inventoryTracker.ItemStatistics[item].BestBuyLocation;
        }
        
        /// <summary>
        /// Bir ürünün en karlı satış yeri
        /// </summary>
        public Town GetBestSellLocation(ItemObject item)
        {
            if (item == null)
                return null;
                
            if (!_inventoryTracker.ItemStatistics.ContainsKey(item))
                return null;
                
            return _inventoryTracker.ItemStatistics[item].BestSellLocation;
        }
        
        /// <summary>
        /// En karlı ürünü getirir
        /// </summary>
        public ItemObject GetMostProfitableItem()
        {
            if (_inventoryTracker.ItemStatistics.Count == 0)
                return null;
                
            return _inventoryTracker.ItemStatistics
                .OrderByDescending(kvp => kvp.Value.TotalProfit)
                .FirstOrDefault().Key;
        }
        
        /// <summary>
        /// Alım-satım işlemi kaydeder
        /// </summary>
        public void RecordTransaction(ItemObject item, int quantity, int price, Town town, bool isPurchase)
        {
            if (item == null || town == null || quantity <= 0 || price <= 0)
                return;
                
            _inventoryTracker.RecordTransaction(item, quantity, price, town, isPurchase);
        }
        
        /// <summary>
        /// Şehirdeki mevcut fiyatları kaydeder
        /// </summary>
        public void RecordCurrentPrices(Town town)
        {
            if (town == null)
                return;
                
            foreach (var item in MBObjectManager.Instance.GetObjectTypeList<ItemObject>())
            {
                if (item.IsTradeGood)
                {
                    int price = town.GetItemPrice(item);
                    if (price > 0)
                    {
                        var priceHistory = GetPriceHistory(item, town);
                        if (priceHistory != null && priceHistory.DataPoints != null)
                        {
                            priceHistory.DataPoints.Add(new PriceDataPoint
                            {
                                Date = CampaignTime.Now,
                                Price = price
                            });
                        }
                    }
                }
            }
            
            UpdateBestLocations();
        }
        
        /// <summary>
        /// Genel güncelleme işlemi
        /// </summary>
        public void Update()
        {
            _inventoryTracker.Update();
        }
        
        /// <summary>
        /// En iyi alım/satım lokasyonlarını günceller
        /// </summary>
        private void UpdateBestLocations()
        {
            // Fiyat geçmişlerine göre en iyi alım/satım lokasyonlarını güncelle
            foreach (var item in MBObjectManager.Instance.GetObjectTypeList<ItemObject>())
            {
                if (item.IsTradeGood)
                {
                    Town bestBuyTown = null;
                    Town bestSellTown = null;
                    int lowestPrice = int.MaxValue;
                    int highestPrice = 0;
                    
                    foreach (var townEntry in _inventoryTracker.PriceHistory)
                    {
                        if (townEntry.Value.ContainsKey(item))
                        {
                            int avgPrice = (int)townEntry.Value[item].GetAveragePrice(7);
                            if (avgPrice > 0)
                            {
                                if (avgPrice < lowestPrice)
                                {
                                    lowestPrice = avgPrice;
                                    bestBuyTown = townEntry.Key;
                                }
                                
                                if (avgPrice > highestPrice)
                                {
                                    highestPrice = avgPrice;
                                    bestSellTown = townEntry.Key;
                                }
                            }
                        }
                    }
                    
                    if (!_inventoryTracker.ItemStatistics.ContainsKey(item))
                        _inventoryTracker.ItemStatistics[item] = new ItemStats();
                        
                    if (bestBuyTown != null)
                        _inventoryTracker.ItemStatistics[item].BestBuyLocation = bestBuyTown;
                        
                    if (bestSellTown != null)
                        _inventoryTracker.ItemStatistics[item].BestSellLocation = bestSellTown;
                }
            }
        }
    }
} 