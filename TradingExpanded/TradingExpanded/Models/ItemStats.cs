using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.SaveSystem;

namespace TradingExpanded.Models
{
    /// <summary>
    /// Tracks statistics for an individual item
    /// </summary>
    public class ItemStats
    {
        [SaveableProperty(1)]
        public int TotalPurchased { get; set; }
        
        [SaveableProperty(2)]
        public int TotalSold { get; set; }
        
        [SaveableProperty(3)]
        public int TotalProfit { get; set; }
        
        [SaveableProperty(4)]
        public float AverageBuyPrice { get; set; }
        
        [SaveableProperty(5)]
        public float AverageSellPrice { get; set; }
        
        [SaveableProperty(6)]
        public Town BestBuyLocation { get; set; }
        
        [SaveableProperty(7)]
        public Town BestSellLocation { get; set; }
        
        [SaveableProperty(8)]
        public int TotalPurchaseValue { get; set; }
        
        [SaveableProperty(9)]
        public int TotalSellValue { get; set; }
        
        /// <summary>
        /// Average profit per unit sold
        /// </summary>
        public float AverageProfitPerUnit => TotalSold > 0 ? (float)TotalProfit / TotalSold : 0f;
        
        /// <summary>
        /// Profit margin as a percentage
        /// </summary>
        public float ProfitMarginPercent
        {
            get
            {
                if (AverageBuyPrice <= 0 || TotalSold <= 0)
                    return 0f;
                    
                return (AverageSellPrice - AverageBuyPrice) / AverageBuyPrice * 100f;
            }
        }
        
        /// <summary>
        /// Current inventory (purchased but not sold)
        /// </summary>
        public int CurrentInventory => TotalPurchased - TotalSold;
        
        /// <summary>
        /// Default constructor
        /// </summary>
        public ItemStats()
        {
            TotalPurchased = 0;
            TotalSold = 0;
            TotalProfit = 0;
            AverageBuyPrice = 0f;
            AverageSellPrice = 0f;
            TotalPurchaseValue = 0;
            TotalSellValue = 0;
        }
        
        /// <summary>
        /// Gets an estimated value of the current inventory
        /// </summary>
        public int GetEstimatedInventoryValue()
        {
            if (CurrentInventory <= 0 || AverageBuyPrice <= 0)
                return 0;
                
            return (int)(CurrentInventory * AverageBuyPrice);
        }
        
        /// <summary>
        /// Gets an estimated profit if all current inventory is sold at the average sell price
        /// </summary>
        public int GetEstimatedRemainingProfit()
        {
            if (CurrentInventory <= 0 || AverageSellPrice <= 0)
                return 0;
                
            int estimatedSellValue = (int)(CurrentInventory * AverageSellPrice);
            int costBasis = (int)(CurrentInventory * AverageBuyPrice);
            
            return estimatedSellValue - costBasis;
        }
        
        /// <summary>
        /// Gets the return on investment as a percentage
        /// </summary>
        public float GetReturnOnInvestment()
        {
            if (TotalPurchaseValue <= 0)
                return 0f;
                
            return TotalProfit / (float)TotalPurchaseValue * 100f;
        }
        
        /// <summary>
        /// Gets a string describing whether this item is profitable
        /// </summary>
        public string GetProfitabilityDescription()
        {
            if (TotalSold < 10)
                return "Insufficient data";
                
            float roi = GetReturnOnInvestment();
            
            if (roi < 0)
                return "Unprofitable";
                
            if (roi < 5)
                return "Marginally profitable";
                
            if (roi < 15)
                return "Profitable";
                
            if (roi < 30)
                return "Very profitable";
                
            return "Extremely profitable";
        }
        
        /// <summary>
        /// Resets all statistics for this item
        /// </summary>
        public void Reset()
        {
            TotalPurchased = 0;
            TotalSold = 0;
            TotalProfit = 0;
            AverageBuyPrice = 0f;
            AverageSellPrice = 0f;
            BestBuyLocation = null;
            BestSellLocation = null;
            TotalPurchaseValue = 0;
            TotalSellValue = 0;
        }
    }
} 