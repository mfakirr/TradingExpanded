using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;

namespace TradingExpanded
{
    /// <summary>
    /// Contains all constant values used throughout the mod
    /// </summary>
    public static class Constants
    {
        // Mod info
        public const string ModName = "TradingExpanded";
        public const string ModVersion = "0.0.1";
        public const string HarmonyDomain = "bannerlord.tradingexpanded";
        
        // Save system
        public const int SaveBaseId = 477281520; // Unique ID for save system
        
        // Shop constants
        public const int BaseShopCost = 10000;
        public const int BaseShopMaintenanceCost = 100;
        public const int BaseShopStorageCapacity = 1000;
        public const int BaseShopLevel = 1;
        public const int MaxShopLevel = 5;
        
        // Caravan constants
        public const int BaseCaravanCost = 5000;
        public const int BaseCaravanCapacity = 500;
        public const int BaseCaravanSecurityCost = 50;
        public const float BaseCaravanSpeed = 4.0f;
        
        // Courier constants
        public const int BaseCourierCost = 500;
        public const int BaseCourierSpeed = 6;
        public const float CourierSuccessChance = 0.95f;
        
        // Trade constants
        public const float WholesaleDiscountMultiplier = 0.9f;
        public const float WholesaleBulkThreshold = 20;
        public const float WholesalePricingInfluence = 0.2f;
        
        // Merchant relation constants
        public const float BaseRelationshipTrustLevel = 0.0f;
        public const float MaxRelationshipTrustLevel = 100.0f;
        public const float MinRelationshipTrustLevel = -100.0f;
        public const float TransactionTrustGain = 0.5f;
        
        // Random event constants
        public const float EventChancePerDay = 0.05f;
        
        // Menu constants
        public const string WholesaleShopMenuId = "town_wholesale_shop";
        
        // Utility methods
        public static string GenerateUniqueId()
        {
            return Guid.NewGuid().ToString();
        }
        
        public static string GetReadableMoney(int denars)
        {
            return denars.ToString("N0") + " " + Game.Current.BasicModels.CurrencyModel.GetCurrencyName();
        }
        
        public static int CalculateShopCostForTown(Town town)
        {
            return (int)(BaseShopCost * (1.0f + town.Prosperity / 10000.0f));
        }
        
        public static int CalculateTravelTime(Town originTown, Town destinationTown, float speedMultiplier)
        {
            // Simple calculation based on distance
            // In a production mod, you'd use actual path finding
            float distance = originTown.Settlement.Position2D.Distance(destinationTown.Settlement.Position2D);
            return Math.Max(1, (int)(distance / (30.0f * speedMultiplier)));
        }
    }
} 