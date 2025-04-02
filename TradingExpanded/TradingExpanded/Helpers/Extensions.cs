using System;
using TaleWorlds.CampaignSystem;

namespace TradingExpanded.Helpers
{
    /// <summary>
    /// Extension metotları içeren sınıf
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// CampaignTime'a gün ekler
        /// </summary>
        public static CampaignTime AddDays(this CampaignTime time, float days)
        {
            return time + CampaignTime.Days(days);
        }
        
        /// <summary>
        /// CampaignTime'a saat ekler
        /// </summary>
        public static CampaignTime AddHours(this CampaignTime time, float hours)
        {
            return time + CampaignTime.Hours(hours);
        }
        
        /// <summary>
        /// Bir değerin belirtilen limitler arasında olmasını sağlar
        /// </summary>
        public static T Clamp<T>(this T value, T min, T max) where T : IComparable<T>
        {
            if (value.CompareTo(min) < 0)
                return min;
            else if (value.CompareTo(max) > 0)
                return max;
            else
                return value;
        }
        
        /// <summary>
        /// Town nesnesinin bir köy olup olmadığını kontrol eder
        /// </summary>
        public static bool IsVillage(this TaleWorlds.CampaignSystem.Settlements.Town town)
        {
            return town != null && town.IsCastle == false && town.IsVillage();
        }
    }
} 