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
    /// Represents the price history of an item in a town
    /// </summary>
    public class PriceHistory
    {
        [SaveableProperty(1)]
        public ItemObject Item { get; set; }
        
        [SaveableProperty(2)]
        public Town Town { get; set; }
        
        [SaveableProperty(3)]
        public List<PriceDataPoint> DataPoints { get; set; }
        
        /// <summary>
        /// Default constructor for saving/loading
        /// </summary>
        public PriceHistory()
        {
            DataPoints = new List<PriceDataPoint>();
        }
        
        /// <summary>
        /// Gets the average price over the last n days
        /// </summary>
        public float GetAveragePrice(int lastNDays)
        {
            if (DataPoints == null)
            {
                DataPoints = new List<PriceDataPoint>();
                return 0f;
            }
            
            if (DataPoints.Count == 0)
                return 0f;
                
            // Convert days to game time
            CampaignTime cutoff = CampaignTime.Now.AddDays(-lastNDays);
            
            // Get data points within the timeframe
            var recentPoints = DataPoints
                .Where(dp => dp.Date >= cutoff)
                .ToList();
                
            if (recentPoints.Count == 0)
                return DataPoints.Last().Price; // Return most recent if no points in timeframe
                
            return (float)recentPoints.Average(dp => dp.Price);
        }
        
        /// <summary>
        /// Gets the price trend as a percentage change over the specified days
        /// </summary>
        public float GetTrend(int days = 7)
        {
            if (DataPoints.Count < 2)
                return 0f;
                
            // Get earliest and latest prices within the timeframe
            CampaignTime cutoff = CampaignTime.Now.AddDays(-days);
            
            var recentPoints = DataPoints
                .Where(dp => dp.Date >= cutoff)
                .OrderBy(dp => dp.Date)
                .ToList();
                
            if (recentPoints.Count < 2)
                return 0f;
                
            float oldestPrice = recentPoints.First().Price;
            float newestPrice = recentPoints.Last().Price;
            
            if (oldestPrice <= 0)
                return 0f;
                
            return (newestPrice - oldestPrice) / oldestPrice * 100f;
        }
        
        /// <summary>
        /// Gets the minimum price in the history
        /// </summary>
        public int GetMinPrice()
        {
            if (DataPoints.Count == 0)
                return 0;
                
            return DataPoints.Min(dp => dp.Price);
        }
        
        /// <summary>
        /// Gets the maximum price in the history
        /// </summary>
        public int GetMaxPrice()
        {
            if (DataPoints.Count == 0)
                return 0;
                
            return DataPoints.Max(dp => dp.Price);
        }
        
        /// <summary>
        /// Gets the current price (most recent data point)
        /// </summary>
        public int GetCurrentPrice()
        {
            if (DataPoints.Count == 0)
                return Town?.GetItemPrice(Item) ?? 0;
                
            return DataPoints
                .OrderByDescending(dp => dp.Date)
                .First()
                .Price;
        }
        
        /// <summary>
        /// Gets a description of the price trend
        /// </summary>
        public string GetTrendDescription()
        {
            if (DataPoints.Count < 2)
                return "Stable";
                
            float trend = GetTrend();
            
            if (trend < -10)
                return "Falling rapidly";
                
            if (trend < -5)
                return "Falling";
                
            if (trend < -2)
                return "Slightly falling";
                
            if (trend <= 2)
                return "Stable";
                
            if (trend <= 5)
                return "Slightly rising";
                
            if (trend <= 10)
                return "Rising";
                
            return "Rising rapidly";
        }
        
        /// <summary>
        /// Gets a price forecast for the next few days
        /// </summary>
        public string GetPriceForecast()
        {
            if (DataPoints.Count < 5)
                return "Insufficient data for forecast";
                
            // Calculate short term trend (3 days)
            float shortTrend = GetTrend(3);
            
            // Calculate medium term trend (7 days)
            float mediumTrend = GetTrend(7);
            
            // Calculate long term trend (14 days)
            float longTrend = GetTrend(14);
            
            // Weight the trends (more weight to recent trends)
            float weightedTrend = (shortTrend * 0.5f) + (mediumTrend * 0.3f) + (longTrend * 0.2f);
            
            if (weightedTrend < -10)
                return "Prices expected to drop significantly";
                
            if (weightedTrend < -5)
                return "Prices likely to fall";
                
            if (weightedTrend < -2)
                return "Prices may decrease slightly";
                
            if (weightedTrend <= 2)
                return "Prices expected to remain stable";
                
            if (weightedTrend <= 5)
                return "Prices may increase slightly";
                
            if (weightedTrend <= 10)
                return "Prices likely to rise";
                
            return "Prices expected to rise significantly";
        }
        
        /// <summary>
        /// Predicts the price in a number of days in the future
        /// </summary>
        public int PredictPrice(int daysInFuture)
        {
            if (DataPoints.Count < 3 || daysInFuture <= 0)
                return GetCurrentPrice();
                
            // Calculate the average daily change over the past 7 days
            float trendPercent = GetTrend(7) / 7f; // Average daily percentage change
            
            // Get current price
            int currentPrice = GetCurrentPrice();
            
            // Apply the trend for the number of days
            float predictedPrice = currentPrice * (1 + (trendPercent / 100f * daysInFuture));
            
            // Add some randomness (market fluctuations)
            Random random = new Random();
            float randomFactor = 1.0f + ((float)random.NextDouble() * 0.1f - 0.05f); // ±5%
            
            return Math.Max(1, (int)(predictedPrice * randomFactor));
        }
    }
} 