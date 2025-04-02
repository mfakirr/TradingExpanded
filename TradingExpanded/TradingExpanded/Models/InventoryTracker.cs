using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.SaveSystem;
using TradingExpanded.Helpers;

namespace TradingExpanded.Models
{
    /// <summary>
    /// Tracks inventory statistics and price history for trade items
    /// </summary>
    public class InventoryTracker
    {
        [SaveableProperty(1)]
        public string Id { get; set; }
        
        [SaveableProperty(2)]
        public Dictionary<ItemObject, ItemStats> ItemStatistics { get; private set; }
        
        [SaveableProperty(3)]
        public Dictionary<Town, Dictionary<ItemObject, PriceHistory>> PriceHistory { get; set; }
        
        [SaveableProperty(4)]
        public bool IsEnabled { get; set; }
        
        [SaveableProperty(5)]
        public int MaxPriceHistoryDays { get; set; }
        
        /// <summary>
        /// Default constructor for saving/loading
        /// </summary>
        public InventoryTracker()
        {
            Id = IdGenerator.GenerateUniqueId();
            ItemStatistics = new Dictionary<ItemObject, ItemStats>();
            PriceHistory = new Dictionary<Town, Dictionary<ItemObject, PriceHistory>>();
            IsEnabled = true;
            MaxPriceHistoryDays = 60; // 60 days of price history
        }
        
        /// <summary>
        /// Gets the most profitable item based on tracked statistics
        /// </summary>
        public ItemObject GetMostProfitableItem()
        {
            if (ItemStatistics.Count == 0)
                return null;
                
            return ItemStatistics
                .OrderByDescending(pair => pair.Value.TotalProfit)
                .FirstOrDefault().Key;
        }
        
        /// <summary>
        /// Gets the best town to buy a specific item
        /// </summary>
        public Town GetBestBuyLocation(ItemObject item)
        {
            if (item == null || PriceHistory == null || PriceHistory.Count == 0)
                return null;
                
            // Find towns with price history for this item
            var townsWithHistory = PriceHistory
                .Where(pair => pair.Value != null && pair.Value.ContainsKey(item) && pair.Value[item] != null && pair.Value[item].DataPoints != null && pair.Value[item].DataPoints.Count > 0)
                .ToList();
                
            if (townsWithHistory.Count == 0)
                return null;
                
            // Return the town with the lowest average price
            return townsWithHistory
                .OrderBy(pair => pair.Value[item].GetAveragePrice(7)) // Last 7 days
                .FirstOrDefault().Key;
        }
        
        /// <summary>
        /// Gets the best town to sell a specific item
        /// </summary>
        public Town GetBestSellLocation(ItemObject item)
        {
            if (item == null || PriceHistory == null || PriceHistory.Count == 0)
                return null;
                
            // Find towns with price history for this item
            var townsWithHistory = PriceHistory
                .Where(pair => pair.Value != null && pair.Value.ContainsKey(item) && pair.Value[item] != null && pair.Value[item].DataPoints != null && pair.Value[item].DataPoints.Count > 0)
                .ToList();
                
            if (townsWithHistory.Count == 0)
                return null;
                
            // Return the town with the highest average price
            return townsWithHistory
                .OrderByDescending(pair => pair.Value[item].GetAveragePrice(7)) // Last 7 days
                .FirstOrDefault().Key;
        }
        
        /// <summary>
        /// Records a transaction for statistical tracking
        /// </summary>
        public void RecordTransaction(ItemObject item, int quantity, int price, Town town, bool isPurchase)
        {
            if (!IsEnabled || item == null || quantity <= 0 || town == null)
                return;
                
            // Update item statistics
            if (!ItemStatistics.ContainsKey(item))
            {
                ItemStatistics[item] = new ItemStats();
            }
            
            var stats = ItemStatistics[item];
            
            if (isPurchase)
            {
                stats.TotalPurchased += quantity;
                stats.TotalPurchaseValue += price * quantity;
                
                // Update average buy price
                float totalBuyValue = stats.AverageBuyPrice * (stats.TotalPurchased - quantity);
                totalBuyValue += price * quantity;
                stats.AverageBuyPrice = totalBuyValue / stats.TotalPurchased;
                
                // Update best buy location if this price is better
                if (stats.BestBuyLocation == null || price < town.GetItemPrice(item))
                {
                    stats.BestBuyLocation = town;
                }
            }
            else
            {
                stats.TotalSold += quantity;
                stats.TotalSellValue += price * quantity;
                
                // Update average sell price
                float totalSellValue = stats.AverageSellPrice * (stats.TotalSold - quantity);
                totalSellValue += price * quantity;
                stats.AverageSellPrice = totalSellValue / stats.TotalSold;
                
                // Update best sell location if this price is better
                if (stats.BestSellLocation == null || price > town.GetItemPrice(item))
                {
                    stats.BestSellLocation = town;
                }
                
                // Update total profit
                int costBasis = (int)(stats.AverageBuyPrice * quantity);
                int revenue = price * quantity;
                stats.TotalProfit += revenue - costBasis;
            }
            
            // Record price history
            RecordPriceHistory(item, town, price);
        }
        
        /// <summary>
        /// Records a price history data point for an item in a town
        /// </summary>
        public void RecordPriceHistory(ItemObject item, Town town, int price)
        {
            if (PriceHistory == null)
            {
                PriceHistory = new Dictionary<Town, Dictionary<ItemObject, PriceHistory>>();
            }
            
            if (!PriceHistory.ContainsKey(town))
            {
                PriceHistory[town] = new Dictionary<ItemObject, PriceHistory>();
            }
            
            if (!PriceHistory[town].ContainsKey(item))
            {
                PriceHistory[town][item] = new PriceHistory
                {
                    Item = item,
                    Town = town,
                    DataPoints = new List<PriceDataPoint>()
                };
            }
            
            PriceHistory[town][item].DataPoints.Add(new PriceDataPoint
            {
                Date = CampaignTime.Now,
                Price = price
            });
        }
        
        /// <summary>
        /// Gets the profit margin between buying in one town and selling in another
        /// </summary>
        public float GetProfitMargin(ItemObject item, Town buyTown, Town sellTown)
        {
            if (item == null || buyTown == null || sellTown == null || PriceHistory == null)
                return 0f;
                
            if (!PriceHistory.ContainsKey(buyTown) || !PriceHistory.ContainsKey(sellTown))
                return 0f;
                
            var buyHistory = PriceHistory[buyTown];
            var sellHistory = PriceHistory[sellTown];
            
            if (!buyHistory.ContainsKey(item) || !sellHistory.ContainsKey(item))
                return 0f;
                
            // Get average buy and sell prices over the last 7 days
            float buyPrice = buyHistory[item].GetAveragePrice(7);
            float sellPrice = sellHistory[item].GetAveragePrice(7);
            
            if (buyPrice <= 0)
                return 0f;
                
            // Calculate profit margin as a percentage
            return (sellPrice - buyPrice) / buyPrice * 100f;
        }
        
        /// <summary>
        /// Gets items with the highest profit margins between towns
        /// </summary>
        public List<Tuple<ItemObject, Town, Town, float>> GetTopProfitableRoutes(int count = 5)
        {
            var profitableRoutes = new List<Tuple<ItemObject, Town, Town, float>>();
            
            if (PriceHistory.Count < 2)
                return profitableRoutes;
                
            // Get all items that have price history in at least two towns
            var items = PriceHistory
                .SelectMany(townPair => townPair.Value.Keys)
                .Distinct()
                .ToList();
                
            foreach (var item in items)
            {
                // Get all towns with price history for this item
                var towns = PriceHistory
                    .Where(townPair => townPair.Value.ContainsKey(item))
                    .Select(townPair => townPair.Key)
                    .ToList();
                    
                // Compare all town pairs
                for (int i = 0; i < towns.Count; i++)
                {
                    for (int j = 0; j < towns.Count; j++)
                    {
                        if (i == j)
                            continue;
                            
                        Town buyTown = towns[i];
                        Town sellTown = towns[j];
                        
                        float profitMargin = GetProfitMargin(item, buyTown, sellTown);
                        
                        if (profitMargin > 10f) // Only consider routes with at least 10% profit
                        {
                            profitableRoutes.Add(new Tuple<ItemObject, Town, Town, float>(
                                item, buyTown, sellTown, profitMargin));
                        }
                    }
                }
            }
            
            // Sort by profit margin and take the top results
            return profitableRoutes
                .OrderByDescending(tuple => tuple.Item4)
                .Take(count)
                .ToList();
        }
        
        /// <summary>
        /// Updates the inventory tracker
        /// </summary>
        public void Update()
        {
            if (!IsEnabled || PriceHistory == null)
                return;
                
            // Trim old data points from price history
            foreach (var townEntry in PriceHistory)
            {
                foreach (var itemEntry in townEntry.Value)
                {
                    var dataPoints = itemEntry.Value.DataPoints;
                    if (dataPoints == null || dataPoints.Count == 0)
                        continue;
                        
                    // Keep only the last 30 days of price data
                    var cutoff = CampaignTime.Now.AddDays(-30);
                    var newDataPoints = dataPoints.Where(dp => dp.Date >= cutoff).ToList();
                    
                    itemEntry.Value.DataPoints = newDataPoints;
                }
            }
        }
    }
} 