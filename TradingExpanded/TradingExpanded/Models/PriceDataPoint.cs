using TaleWorlds.CampaignSystem;
using TaleWorlds.SaveSystem;

namespace TradingExpanded.Models
{
    /// <summary>
    /// Represents a single price data point in the price history
    /// </summary>
    public class PriceDataPoint
    {
        [SaveableProperty(1)]
        public CampaignTime Date { get; set; }
        
        [SaveableProperty(2)]
        public int Price { get; set; }
        
        /// <summary>
        /// Default constructor for saving/loading
        /// </summary>
        public PriceDataPoint()
        {
        }
        
        /// <summary>
        /// Creates a new price data point
        /// </summary>
        public PriceDataPoint(CampaignTime date, int price)
        {
            Date = date;
            Price = price;
        }
    }
} 