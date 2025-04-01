using MCM.Abstractions.Attributes;
using MCM.Abstractions.Attributes.v2;
using MCM.Abstractions.Settings.Base.Global;

namespace TradingExpanded
{
    public class Settings : AttributeGlobalSettings<Settings>
    {
        // MCM required properties
        public override string Id => "TradingExpandedSettings";
        public override string DisplayName => "Trading Expanded";
        public override string FolderName => "TradingExpanded";
        public override string FormatType => "json";
        
        // General settings
        [SettingPropertyFloatingInteger("Price Multiplier", 0.1f, 10.0f, "#0.00", Order = 0, 
            HintText = "Multiplier for all prices in the mod. Higher values make items more expensive.")]
        [SettingPropertyGroup("General Settings")]
        public float PriceMultiplier { get; set; } = 1.0f;
        
        [SettingPropertyBool("Enable Advanced Trading Statistics", Order = 1, 
            HintText = "Enables detailed statistics tracking for your trading empire.")]
        [SettingPropertyGroup("General Settings")]
        public bool EnableAdvancedStats { get; set; } = true;
        
        // Shop settings
        [SettingPropertyInteger("Max Wholesale Shops", 1, 10, "0 Shops", Order = 0, 
            HintText = "Maximum number of wholesale shops you can own simultaneously.")]
        [SettingPropertyGroup("Shop Settings")]
        public int MaxWholesaleShops { get; set; } = 3;
        
        [SettingPropertyFloatingInteger("Shop Maintenance Cost Multiplier", 0.5f, 5.0f, "#0.00", Order = 1, 
            HintText = "Multiplier for shop maintenance costs. Higher values increase daily costs.")]
        [SettingPropertyGroup("Shop Settings")]
        public float ShopMaintenanceCostMultiplier { get; set; } = 1.0f;
        
        [SettingPropertyInteger("Shop Upgrade Cost Multiplier", 50, 300, "%", Order = 2, 
            HintText = "Percentage of base shop cost required for each level upgrade.")]
        [SettingPropertyGroup("Shop Settings")]
        public int ShopUpgradeCostPercent { get; set; } = 100;
        
        // Caravan settings
        [SettingPropertyInteger("Max Caravans", 1, 10, "0 Caravans", Order = 0, 
            HintText = "Maximum number of trade caravans you can own simultaneously.")]
        [SettingPropertyGroup("Caravan Settings")]
        public int MaxCaravans { get; set; } = 3;
        
        [SettingPropertyFloatingInteger("Caravan Speed Multiplier", 0.5f, 2.0f, "#0.00", Order = 1, 
            HintText = "Multiplier for caravan movement speed. Higher values make caravans faster.")]
        [SettingPropertyGroup("Caravan Settings")]
        public float CaravanSpeedMultiplier { get; set; } = 1.0f;
        
        [SettingPropertyFloatingInteger("Caravan Security Cost Multiplier", 0.5f, 3.0f, "#0.00", Order = 2, 
            HintText = "Multiplier for caravan security costs. Higher values increase guard wages.")]
        [SettingPropertyGroup("Caravan Settings")]
        public float CaravanSecurityCostMultiplier { get; set; } = 1.0f;
        
        // Courier settings
        [SettingPropertyInteger("Max Couriers", 1, 10, "0 Couriers", Order = 0, 
            HintText = "Maximum number of couriers you can dispatch simultaneously.")]
        [SettingPropertyGroup("Courier Settings")]
        public int MaxCouriers { get; set; } = 5;
        
        [SettingPropertyFloatingInteger("Courier Speed Multiplier", 0.5f, 2.0f, "#0.00", Order = 1, 
            HintText = "Multiplier for courier movement speed. Higher values make couriers faster.")]
        [SettingPropertyGroup("Courier Settings")]
        public float CourierSpeedMultiplier { get; set; } = 1.0f;
        
        [SettingPropertyFloatingInteger("Courier Success Chance Multiplier", 0.5f, 1.5f, "#0.00", Order = 2, 
            HintText = "Multiplier for courier success chance. Higher values increase success rate.")]
        [SettingPropertyGroup("Courier Settings")]
        public float CourierSuccessChanceMultiplier { get; set; } = 1.0f;
    }
} 