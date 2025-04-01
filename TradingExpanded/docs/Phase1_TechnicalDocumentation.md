# TradingExpanded: Phase 1 Technical Documentation

## Overview
This document outlines the technical implementation details for Phase 1 of the TradingExpanded mod, focusing on establishing the project structure and creating the core data models. The architecture is designed with extensibility, maintainability, and performance in mind.

## Project Structure

### Namespace Organization
The mod will use a hierarchical namespace structure:
- `TradingExpanded` - Root namespace
  - `TradingExpanded.Models` - Data models and entities
  - `TradingExpanded.Services` - Business logic services
  - `TradingExpanded.UI` - User interface components
  - `TradingExpanded.Utils` - Utility classes and helpers
  - `TradingExpanded.Patches` - Harmony patches

### Core Files Organization
- `SubModule.cs` - Main entry point for the mod
- `Settings.cs` - MCM settings class
- `Configuration.cs` - Configuration management
- `Constants.cs` - Constant values used throughout the mod

## Core Systems Implementation

### 1. Module Initialization

The `SubModule.cs` file will handle initialization of the mod, including:
- Setting up Harmony patching
- Registering services with ButterLib
- Initializing UI extensions with UIExtenderEx
- Loading settings through MCM

```csharp
protected override void OnSubModuleLoad()
{
    base.OnSubModuleLoad();
    _harmony = new HarmonyLib.Harmony("TradingExpanded");
    _harmony.PatchAll(Assembly.GetExecutingAssembly());
    LoadModServices();
}

protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
{
    base.OnGameStart(game, gameStarterObject);
    if (gameStarterObject is CampaignGameStarter campaignStarter)
    {
        RegisterBehaviors(campaignStarter);
        InitializeUIExtensions();
    }
}
```

### 2. Data Storage and State Management

Data for the mod will be persisted through:
- CampaignBehavior classes for game-state data
- Custom serialization for complex objects
- MCM settings for user preferences

### 3. Data Models

#### 3.1 Wholesale Shop Model
The `WholesaleShop` class will represent player-owned wholesale shops in cities:

```csharp
public class WholesaleShop
{
    public string Id { get; set; }               // Unique identifier
    public Town Town { get; set; }               // Associated town
    public int Capital { get; set; }             // Available money
    public Dictionary<ItemObject, int> Inventory { get; set; } // Stock
    public List<WholesaleEmployee> Employees { get; set; }     // Staff
    public float ProfitMargin { get; set; }      // Profit percentage
    public int Level { get; set; }               // Shop level
    public int DailyWages { get; set; }          // Daily costs
    public int StorageCapacity { get; set; }     // Max inventory capacity
    
    // Methods for shop operations
    public int BuyItem(ItemObject item, int quantity, int price) { ... }
    public int SellItem(ItemObject item, int quantity, int price) { ... }
    public int CalculateDailyIncome() { ... }
    public void UpdateShop() { ... }
}
```

#### 3.2 Caravan Model
The `TradeCaravan` class will represent player's customized trade caravans:

```csharp
public class TradeCaravan
{
    public string Id { get; set; }               // Unique identifier
    public Hero Leader { get; set; }             // Caravan leader
    public MobileParty MobileParty { get; set; } // Mobile party reference
    public Town CurrentTown { get; set; }        // Current location
    public Town DestinationTown { get; set; }    // Destination
    public Dictionary<ItemObject, int> Cargo { get; set; } // Goods carried
    public int Capital { get; set; }             // Available money
    public TradeRoute Route { get; set; }        // Assigned route
    public CaravanState State { get; set; }      // Current state
    public int SecurityLevel { get; set; }       // Guards strength
    public float Speed { get; set; }             // Movement speed
    public int CargoCapacity { get; set; }       // Max cargo capacity
    
    // Methods for caravan operations
    public void StartJourney() { ... }
    public void ArrivedAtDestination() { ... }
    public void PerformTrade() { ... }
    public void UpdatePosition() { ... }
}

public enum CaravanState
{
    Idle,
    Traveling,
    Trading,
    UnderAttack,
    Returning
}
```

#### 3.3 Courier Model
The `Courier` class will represent messengers sent to gather price information:

```csharp
public class Courier
{
    public string Id { get; set; }               // Unique identifier
    public Town OriginTown { get; set; }         // Starting location
    public Town DestinationTown { get; set; }    // Target location
    public int DaysToArrive { get; set; }        // Travel time remaining
    public int DaysToReturn { get; set; }        // Return time
    public bool HasReturnedInfo { get; set; }    // Info delivery status
    public Dictionary<ItemObject, int> PriceInfo { get; set; } // Collected prices
    public CourierState State { get; set; }      // Current state
    public int Skill { get; set; }               // Courier skill level
    
    // Methods for courier operations
    public void StartJourney() { ... }
    public void GatherInformation() { ... }
    public void ReturnJourney() { ... }
    public void DeliverInformation() { ... }
}

public enum CourierState
{
    Preparing,
    Traveling,
    GatheringInfo,
    Returning,
    Delivered,
    Lost
}
```

#### 3.4 Merchant Relations Model
The `MerchantRelation` class will track relationships with NPC merchants:

```csharp
public class MerchantRelation
{
    public string Id { get; set; }               // Unique identifier
    public Hero Merchant { get; set; }           // NPC merchant
    public float RelationLevel { get; set; }     // Relationship score
    public List<TradeAgreement> Agreements { get; set; } // Active deals
    public int TradeVolume { get; set; }         // Total trade amount
    public DateTime LastInteraction { get; set; } // Last trade date
    
    // Methods for relationship management
    public void ImproveTrust(float amount) { ... }
    public void ReduceTrust(float amount) { ... }
    public bool CanMakeAgreement() { ... }
    public void RecordTransaction(int amount) { ... }
}

public class TradeAgreement
{
    public string Id { get; set; }               // Unique identifier
    public ItemObject Item { get; set; }         // Traded item
    public int Quantity { get; set; }            // Agreed quantity
    public int Price { get; set; }               // Agreed price
    public int Duration { get; set; }            // Duration in days
    public int RemainingDays { get; set; }       // Time left
    public bool IsActive { get; set; }           // Agreement status
    
    // Methods for agreement management
    public void Activate() { ... }
    public void Fulfill() { ... }
    public void Terminate() { ... }
    public void UpdateStatus() { ... }
}
```

#### 3.5 Inventory Management Model
The `InventoryTracker` class will manage inventory statistics and analytics:

```csharp
public class InventoryTracker
{
    public string Id { get; set; }               // Unique identifier
    public Dictionary<ItemObject, ItemStats> ItemStatistics { get; set; } // Per-item stats
    public Dictionary<Town, Dictionary<ItemObject, PriceHistory>> PriceHistory { get; set; } // Price trends
    
    // Methods for inventory analytics
    public ItemObject GetMostProfitableItem() { ... }
    public Town GetBestBuyLocation(ItemObject item) { ... }
    public Town GetBestSellLocation(ItemObject item) { ... }
    public void RecordTransaction(ItemObject item, int quantity, int price, Town town, bool isPurchase) { ... }
    public float GetProfitMargin(ItemObject item, Town buyTown, Town sellTown) { ... }
}

public class ItemStats
{
    public int TotalPurchased { get; set; }
    public int TotalSold { get; set; }
    public int TotalProfit { get; set; }
    public float AverageBuyPrice { get; set; }
    public float AverageSellPrice { get; set; }
    public Town BestBuyLocation { get; set; }
    public Town BestSellLocation { get; set; }
}

public class PriceHistory
{
    public ItemObject Item { get; set; }
    public Town Town { get; set; }
    public List<PriceDataPoint> DataPoints { get; set; }
    
    public float GetAveragePrice(int lastNDays) { ... }
    public float GetTrend() { ... }
}

public class PriceDataPoint
{
    public CampaignTime Date { get; set; }
    public int Price { get; set; }
}
```

## Data Persistence Strategy

### Save/Load System
We will implement a `SaveableTypeDefiner` class to make our custom objects properly serialize with the game's save system:

```csharp
public class TradingExpandedSaveDefiner : SaveableTypeDefiner
{
    public TradingExpandedSaveDefiner() : base(xxxyyy) { } // Unique save ID
    
    protected override void DefineClassTypes()
    {
        AddClassDefinition(typeof(WholesaleShop), 1);
        AddClassDefinition(typeof(TradeCaravan), 2);
        AddClassDefinition(typeof(Courier), 3);
        AddClassDefinition(typeof(MerchantRelation), 4);
        AddClassDefinition(typeof(TradeAgreement), 5);
        AddClassDefinition(typeof(InventoryTracker), 6);
        AddClassDefinition(typeof(ItemStats), 7);
        AddClassDefinition(typeof(PriceHistory), 8);
        AddClassDefinition(typeof(PriceDataPoint), 9);
    }
    
    protected override void DefineContainerDefinitions()
    {
        // Define dictionaries and lists used in our classes
        ConstructContainerDefinition(typeof(List<WholesaleShop>));
        ConstructContainerDefinition(typeof(Dictionary<string, WholesaleShop>));
        // Add more container definitions as needed
    }
}
```

### Campaign Behavior
A campaign behavior will be used to manage the mod's data in the campaign:

```csharp
public class TradingExpandedCampaignBehavior : CampaignBehaviorBase
{
    private Dictionary<string, WholesaleShop> _wholesaleShops = new Dictionary<string, WholesaleShop>();
    private Dictionary<string, TradeCaravan> _caravans = new Dictionary<string, TradeCaravan>();
    private Dictionary<string, Courier> _couriers = new Dictionary<string, Courier>();
    private Dictionary<string, MerchantRelation> _merchantRelations = new Dictionary<string, MerchantRelation>();
    private InventoryTracker _inventoryTracker = new InventoryTracker();
    
    public override void RegisterEvents()
    {
        CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
        CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, OnHourlyTick);
        // Register additional events
    }
    
    public override void SyncData(IDataStore dataStore)
    {
        dataStore.SyncData("TradingExpanded.WholesaleShops", ref _wholesaleShops);
        dataStore.SyncData("TradingExpanded.Caravans", ref _caravans);
        dataStore.SyncData("TradingExpanded.Couriers", ref _couriers);
        dataStore.SyncData("TradingExpanded.MerchantRelations", ref _merchantRelations);
        dataStore.SyncData("TradingExpanded.InventoryTracker", ref _inventoryTracker);
    }
    
    private void OnDailyTick()
    {
        UpdateCaravans();
        UpdateCouriers();
        UpdateShops();
        UpdateRelations();
    }
    
    private void OnHourlyTick()
    {
        // More frequent updates
    }
    
    // Additional methods for managing shops, caravans, etc.
}
```

## MCM Settings Integration

Settings for the mod will be managed through MCM:

```csharp
public class Settings : AttributeGlobalSettings<Settings>
{
    public override string Id => "TradingExpandedSettings";
    public override string DisplayName => "Trading Expanded";
    public override string FolderName => "TradingExpanded";
    public override string FormatType => "json";
    
    [SettingPropertyFloatingInteger("Price Multiplier", 0.1f, 10.0f, "#0.00", Order = 0)]
    [SettingPropertyGroup("General Settings")]
    public float PriceMultiplier { get; set; } = 1.0f;
    
    [SettingPropertyBool("Enable Advanced Trading Statistics", Order = 1)]
    [SettingPropertyGroup("General Settings")]
    public bool EnableAdvancedStats { get; set; } = true;
    
    [SettingPropertyInteger("Max Wholesale Shops", 1, 10, "0 Shops", Order = 2)]
    [SettingPropertyGroup("Shop Settings")]
    public int MaxWholesaleShops { get; set; } = 3;
    
    [SettingPropertyFloatingInteger("Shop Maintenance Cost Multiplier", 0.5f, 5.0f, "#0.00", Order = 3)]
    [SettingPropertyGroup("Shop Settings")]
    public float ShopMaintenanceCostMultiplier { get; set; } = 1.0f;
    
    [SettingPropertyInteger("Max Caravans", 1, 10, "0 Caravans", Order = 4)]
    [SettingPropertyGroup("Caravan Settings")]
    public int MaxCaravans { get; set; } = 3;
    
    [SettingPropertyFloatingInteger("Caravan Speed Multiplier", 0.5f, 2.0f, "#0.00", Order = 5)]
    [SettingPropertyGroup("Caravan Settings")]
    public float CaravanSpeedMultiplier { get; set; } = 1.0f;
    
    [SettingPropertyInteger("Max Couriers", 1, 10, "0 Couriers", Order = 6)]
    [SettingPropertyGroup("Courier Settings")]
    public int MaxCouriers { get; set; } = 5;
    
    [SettingPropertyFloatingInteger("Courier Speed Multiplier", 0.5f, 2.0f, "#0.00", Order = 7)]
    [SettingPropertyGroup("Courier Settings")]
    public float CourierSpeedMultiplier { get; set; } = 1.0f;
}
```

## Implementation Plan for Phase 1

1. Create the base project structure and namespaces
2. Implement the core data models
3. Set up the campaign behavior for data management
4. Implement the save/load system
5. Create the MCM settings class
6. Initialize Harmony patching in SubModule.cs
7. Add basic service interfaces
8. Create utility helper classes
9. Set up basic UI scaffolding for Phase 2

## Dependencies and Requirements

### Game Components
- TaleWorlds.CampaignSystem
- TaleWorlds.Core
- TaleWorlds.Library
- TaleWorlds.Localization

### External Dependencies
- Harmony (v2.2.2)
- ButterLib (v2.8.11)
- UIExtenderEx (v2.8.0)
- MCM (v5.9.1) 