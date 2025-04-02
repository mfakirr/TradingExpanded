using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.ObjectSystem;
using TaleWorlds.SaveSystem;
using TradingExpanded.Models;
using TradingExpanded.Helpers;

namespace TradingExpanded.Behaviors
{
    /// <summary>
    /// Manages the mod's data and game state in the campaign
    /// </summary>
    public class TradingExpandedCampaignBehavior : CampaignBehaviorBase
    {
        // Main business objects
        private Dictionary<string, WholesaleShop> _wholesaleShops;
        private Dictionary<string, TradeCaravan> _caravans;
        private Dictionary<string, Courier> _couriers;
        private Dictionary<string, MerchantRelation> _merchantRelations;
        private InventoryTracker _inventoryTracker;
        
        // Flags
        private bool _isInitialized;
        
        public TradingExpandedCampaignBehavior()
        {
            _wholesaleShops = new Dictionary<string, WholesaleShop>();
            _caravans = new Dictionary<string, TradeCaravan>();
            _couriers = new Dictionary<string, Courier>();
            _merchantRelations = new Dictionary<string, MerchantRelation>();
            _inventoryTracker = new InventoryTracker();
            _isInitialized = false;
        }
        
        #region CampaignBehaviorBase Implementation
        
        public override void RegisterEvents()
        {
            // Register for campaign events
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
            CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, OnHourlyTick);
            CampaignEvents.SettlementEntered.AddNonSerializedListener(this, OnSettlementEntered);
            CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, OnGameLoaded);
        }
        
        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("TradingExpanded.WholesaleShops", ref _wholesaleShops);
            dataStore.SyncData("TradingExpanded.Caravans", ref _caravans);
            dataStore.SyncData("TradingExpanded.Couriers", ref _couriers);
            dataStore.SyncData("TradingExpanded.MerchantRelations", ref _merchantRelations);
            dataStore.SyncData("TradingExpanded.InventoryTracker", ref _inventoryTracker);
            dataStore.SyncData("TradingExpanded.IsInitialized", ref _isInitialized);
            
            // Initialize if needed (first time loading the mod)
            if (!_isInitialized && dataStore.IsLoading)
            {
                Initialize();
            }
        }
        
        #endregion
        
        #region Event Handlers
        
        private void OnSessionLaunched(CampaignGameStarter campaignGameStarter)
        {
            if (!_isInitialized)
            {
                Initialize();
            }
        }
        
        private void OnGameLoaded(CampaignGameStarter campaignGameStarter)
        {
            // Ensure proper initialization after loading a save
            if (_wholesaleShops == null)
                _wholesaleShops = new Dictionary<string, WholesaleShop>();
                
            if (_caravans == null)
                _caravans = new Dictionary<string, TradeCaravan>();
                
            if (_couriers == null)
                _couriers = new Dictionary<string, Courier>();
                
            if (_merchantRelations == null)
                _merchantRelations = new Dictionary<string, MerchantRelation>();
                
            if (_inventoryTracker == null)
                _inventoryTracker = new InventoryTracker();
        }
        
        private void OnDailyTick()
        {
            // Update all objects daily
            UpdateWholesaleShops();
            UpdateCaravans();
            UpdateCouriers();
            UpdateMerchantRelations();
            
            // Update analytics less frequently (every 3 days)
            if ((int)CampaignTime.Now.ToDays % 3 == 0)
            {
                _inventoryTracker.Update();
            }
        }
        
        private void OnHourlyTick()
        {
            // For more frequent updates if needed
        }
        
        private void OnSettlementEntered(MobileParty mobileParty, Settlement settlement, Hero hero)
        {
            // Handle player entering a settlement
            if (mobileParty == MobileParty.MainParty && settlement.IsTown)
            {
                // Record current prices for analytics
                RecordCurrentPrices(settlement.Town);
            }
        }
        
        #endregion
        
        #region Initialization
        
        private void Initialize()
        {
            // Initialize merchant relations with notable traders
            InitializeMerchantRelations();
            
            _isInitialized = true;
        }
        
        private void InitializeMerchantRelations()
        {
            // Find all notable merchant heroes in the game
            foreach (var hero in Hero.AllAliveHeroes)
            {
                if (hero.IsNotable && hero.Occupation == Occupation.Merchant)
                {
                    if (!_merchantRelations.ContainsKey(hero.StringId))
                    {
                        _merchantRelations[hero.StringId] = new MerchantRelation(hero);
                    }
                }
            }
        }
        
        #endregion
        
        #region Wholesale Shop Management
        
        /// <summary>
        /// Gets a wholesale shop by its ID
        /// </summary>
        public WholesaleShop GetShop(string shopId)
        {
            if (string.IsNullOrEmpty(shopId) || !_wholesaleShops.ContainsKey(shopId))
                return null;
                
            return _wholesaleShops[shopId];
        }
        
        /// <summary>
        /// Gets all wholesale shops owned by the player
        /// </summary>
        public List<WholesaleShop> GetPlayerShops()
        {
            return _wholesaleShops.Values
                .Where(shop => shop.IsActive)
                .ToList();
        }
        
        /// <summary>
        /// Gets a wholesale shop in a specific town, if any
        /// </summary>
        public WholesaleShop GetShopInTown(Town town)
        {
            if (town == null)
                return null;
                
            return _wholesaleShops.Values
                .FirstOrDefault(shop => shop.Town == town && shop.IsActive);
        }
        
        /// <summary>
        /// Creates a new wholesale shop in the specified town
        /// </summary>
        public WholesaleShop CreateShop(Town town, int initialCapital = 5000)
        {
            if (town == null)
                return null;
                
            // Check if we already have a shop in this town
            if (GetShopInTown(town) != null)
                return null;
                
            // Check if we've reached the maximum number of shops
            int maxShops = Settings.Instance?.MaxWholesaleShops ?? 3;
            if (GetPlayerShops().Count >= maxShops)
                return null;
                
            // Create the shop
            var shop = new WholesaleShop(town, initialCapital);
            _wholesaleShops[shop.Id] = shop;
            
            return shop;
        }
        
        /// <summary>
        /// Closes a wholesale shop permanently
        /// </summary>
        public void CloseShop(string shopId)
        {
            var shop = GetShop(shopId);
            if (shop != null)
            {
                shop.IsActive = false;
            }
        }
        
        /// <summary>
        /// Updates all wholesale shops
        /// </summary>
        private void UpdateWholesaleShops()
        {
            foreach (var shop in _wholesaleShops.Values)
            {
                shop.UpdateShop();
            }
        }
        
        #endregion
        
        #region Caravan Management
        
        /// <summary>
        /// Gets a trade caravan by its ID
        /// </summary>
        public TradeCaravan GetCaravan(string caravanId)
        {
            if (string.IsNullOrEmpty(caravanId) || !_caravans.ContainsKey(caravanId))
                return null;
                
            return _caravans[caravanId];
        }
        
        /// <summary>
        /// Gets all trade caravans owned by the player
        /// </summary>
        public List<TradeCaravan> GetPlayerCaravans()
        {
            return _caravans.Values
                .Where(c => c.AktifMi)
                .ToList();
        }
        
        /// <summary>
        /// Creates a new trade caravan starting in the specified town
        /// </summary>
        public TradeCaravan CreateCaravan(Town startingTown, Hero leader, int initialCapital = 5000)
        {
            if (startingTown == null || leader == null)
                return null;
                
            // Check if we've reached the maximum number of caravans
            int maxCaravans = Settings.Instance?.MaxCaravans ?? 3;
            if (GetPlayerCaravans().Count >= maxCaravans)
                return null;
                
            // Create the caravan
            var caravan = new TradeCaravan(startingTown, leader, initialCapital);
            _caravans[caravan.Id] = caravan;
            
            return caravan;
        }
        
        /// <summary>
        /// Disbands a trade caravan
        /// </summary>
        public void DisbandCaravan(string caravanId)
        {
            var caravan = GetCaravan(caravanId);
            if (caravan != null)
            {
                caravan.AktifMi = false;
                
                // Remove from active caravans if completely removed
                // _caravans.Remove(caravanId);
            }
        }
        
        /// <summary>
        /// Updates all trade caravans
        /// </summary>
        private void UpdateCaravans()
        {
            foreach (var caravan in _caravans.Values)
            {
                if (caravan.AktifMi)
                {
                    caravan.Guncelle();
                }
            }
        }
        
        #endregion
        
        #region Courier Management
        
        /// <summary>
        /// Gets a courier by its ID
        /// </summary>
        public Courier GetCourier(string courierId)
        {
            if (string.IsNullOrEmpty(courierId) || !_couriers.ContainsKey(courierId))
                return null;
                
            return _couriers[courierId];
        }
        
        /// <summary>
        /// Gets all couriers dispatched by the player
        /// </summary>
        public List<Courier> GetPlayerCouriers()
        {
            return _couriers.Values
                .Where(courier => courier.State != Courier.CourierState.Delivered && 
                                 courier.State != Courier.CourierState.Lost)
                .ToList();
        }
        
        /// <summary>
        /// Creates and dispatches a new courier to gather price information
        /// </summary>
        public Courier DispatchCourier(Town originTown, Town destinationTown, int skill = 50)
        {
            if (originTown == null || destinationTown == null || originTown == destinationTown)
                return null;
                
            // Check if we've reached the maximum number of couriers
            int maxCouriers = Settings.Instance?.MaxCouriers ?? 5;
            if (GetPlayerCouriers().Count >= maxCouriers)
                return null;
                
            // Create the courier
            var courier = new Courier(originTown, destinationTown, skill);
            _couriers[courier.Id] = courier;
            
            // Start the journey
            courier.StartJourney();
            
            return courier;
        }
        
        /// <summary>
        /// Updates all couriers
        /// </summary>
        private void UpdateCouriers()
        {
            foreach (var courier in _couriers.Values)
            {
                courier.UpdateCourier();
            }
        }
        
        /// <summary>
        /// Gets price information gathered by couriers
        /// </summary>
        public Dictionary<ItemObject, int> GetCourierPriceInfo(Town town)
        {
            if (town == null)
                return new Dictionary<ItemObject, int>();
                
            // Find the most recent courier info for this town
            var courier = _couriers.Values
                .Where(c => c.DestinationTown == town && 
                           c.HasReturnedInfo &&
                           c.State == Courier.CourierState.Delivered)
                .OrderByDescending(c => c.DepartureTime)
                .FirstOrDefault();
                
            if (courier == null)
                return new Dictionary<ItemObject, int>();
                
            return courier.PriceInfo;
        }
        
        #endregion
        
        #region Merchant Relation Management
        
        /// <summary>
        /// Gets a merchant relation by merchant hero
        /// </summary>
        public MerchantRelation GetMerchantRelation(Hero merchant)
        {
            if (merchant == null)
                return null;
                
            if (!_merchantRelations.ContainsKey(merchant.StringId))
            {
                _merchantRelations[merchant.StringId] = new MerchantRelation(merchant);
            }
            
            return _merchantRelations[merchant.StringId];
        }
        
        /// <summary>
        /// Gets all active merchant relations
        /// </summary>
        public List<MerchantRelation> GetAllMerchantRelations()
        {
            return _merchantRelations.Values
                .Where(relation => relation.IsActive)
                .ToList();
        }
        
        /// <summary>
        /// Updates all merchant relations
        /// </summary>
        private void UpdateMerchantRelations()
        {
            foreach (var relation in _merchantRelations.Values)
            {
                relation.Update();
            }
        }
        
        /// <summary>
        /// Creates a trade agreement with a merchant
        /// </summary>
        public TradeAgreement CreateTradeAgreement(Hero merchant, ItemObject item, int quantity, int price, int duration, bool isBuyAgreement)
        {
            if (merchant == null || item == null || quantity <= 0 || price <= 0 || duration <= 0)
                return null;
                
            var relation = GetMerchantRelation(merchant);
            if (relation == null || !relation.CanMakeAgreement())
                return null;
                
            // Create the agreement
            TradeAgreement agreement = isBuyAgreement ? 
                TradeAgreement.CreateBuyAgreement(item, quantity, price, duration) : 
                TradeAgreement.CreateSellAgreement(item, quantity, price, duration);
                
            relation.Agreements.Add(agreement);
            
            // Improve the relationship for making an agreement
            relation.ImproveTrust(5.0f);
            
            return agreement;
        }
        
        #endregion
        
        #region Inventory and Price Tracking
        
        /// <summary>
        /// Gets the inventory tracker
        /// </summary>
        public InventoryTracker GetInventoryTracker()
        {
            return _inventoryTracker;
        }
        
        /// <summary>
        /// Records current prices in a town for analytics
        /// </summary>
        public void RecordCurrentPrices(Town town)
        {
            if (town == null || !_inventoryTracker.IsEnabled)
                return;
                
            // Get all tradeable items
            var items = MBObjectManager.Instance.GetObjectTypeList<ItemObject>()
                .Where(item => 
                    item.ItemCategory != DefaultItemCategories.Horse && 
                    item.ItemCategory != DefaultItemCategories.WarHorse &&
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
                    item.ItemCategory != DefaultItemCategories.RangedWeapons5);
                    
            foreach (var item in items)
            {
                // Get current price in town
                int price = town.GetItemPrice(item);
                
                // Record the price
                _inventoryTracker.RecordPriceHistory(item, price, town);
            }
        }
        
        /// <summary>
        /// Records a transaction for statistical tracking
        /// </summary>
        public void RecordTransaction(ItemObject item, int quantity, int price, Town town, bool isPurchase)
        {
            if (item == null || quantity <= 0 || town == null)
                return;
                
            _inventoryTracker.RecordTransaction(item, quantity, price, town, isPurchase);
        }
        
        #endregion
    }
} 