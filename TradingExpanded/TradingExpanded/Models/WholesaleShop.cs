using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;

namespace TradingExpanded.Models
{
    /// <summary>
    /// Represents a player-owned wholesale shop in a town
    /// </summary>
    [SaveableClass(Constants.SaveBaseId + 1)]
    public class WholesaleShop
    {
        [SaveableProperty(1)]
        public string Id { get; set; }
        
        [SaveableProperty(2)]
        public Town Town { get; set; }
        
        [SaveableProperty(3)]
        public int Capital { get; set; }
        
        [SaveableProperty(4)]
        public Dictionary<ItemObject, int> Inventory { get; private set; }
        
        [SaveableProperty(5)]
        public List<WholesaleEmployee> Employees { get; private set; }
        
        [SaveableProperty(6)]
        public float ProfitMargin { get; set; }
        
        [SaveableProperty(7)]
        public int Level { get; set; }
        
        [SaveableProperty(8)]
        public int DailyWages { get; set; }
        
        [SaveableProperty(9)]
        public int StorageCapacity { get; set; }
        
        [SaveableProperty(10)]
        public bool IsActive { get; set; }
        
        [SaveableProperty(11)]
        public CampaignTime LastUpdateTime { get; set; }
        
        /// <summary>
        /// Name of the shop for display
        /// </summary>
        public string Name => $"{Hero.MainHero.Name}'s Wholesale Shop in {Town.Name}";
        
        /// <summary>
        /// Current total inventory value
        /// </summary>
        public int InventoryValue => Inventory.Sum(pair => pair.Value * Town.GetItemPrice(pair.Key));
        
        /// <summary>
        /// Current available inventory space
        /// </summary>
        public int AvailableStorage => StorageCapacity - Inventory.Values.Sum();
        
        /// <summary>
        /// Default constructor for saving/loading
        /// </summary>
        public WholesaleShop() 
        {
            Inventory = new Dictionary<ItemObject, int>();
            Employees = new List<WholesaleEmployee>();
        }
        
        /// <summary>
        /// Creates a new wholesale shop in the specified town
        /// </summary>
        public WholesaleShop(Town town, int initialCapital = 5000)
        {
            Id = Constants.GenerateUniqueId();
            Town = town;
            Capital = initialCapital;
            Inventory = new Dictionary<ItemObject, int>();
            Employees = new List<WholesaleEmployee>();
            ProfitMargin = 0.1f; // 10% markup
            Level = Constants.BaseShopLevel;
            DailyWages = CalculateDailyWages();
            StorageCapacity = Constants.BaseShopStorageCapacity;
            IsActive = true;
            LastUpdateTime = CampaignTime.Now;
        }
        
        /// <summary>
        /// Buys an item for the shop inventory
        /// </summary>
        /// <returns>The actual amount bought (may be less if not enough money or space)</returns>
        public int BuyItem(ItemObject item, int quantity, int pricePerItem)
        {
            if (item == null || quantity <= 0)
                return 0;
                
            // Calculate how many we can afford to buy
            int maxAffordable = Capital / pricePerItem;
            
            // Calculate how many we can store
            int maxStorable = AvailableStorage;
            
            // Buy as much as we can
            int actualQuantity = Math.Min(quantity, Math.Min(maxAffordable, maxStorable));
            
            if (actualQuantity <= 0)
                return 0;
                
            // Update capital
            int totalCost = actualQuantity * pricePerItem;
            Capital -= totalCost;
            
            // Update inventory
            if (Inventory.ContainsKey(item))
                Inventory[item] += actualQuantity;
            else
                Inventory[item] = actualQuantity;
                
            return actualQuantity;
        }
        
        /// <summary>
        /// Sells an item from the shop inventory
        /// </summary>
        /// <returns>The actual amount sold (may be less if not enough in inventory)</returns>
        public int SellItem(ItemObject item, int quantity, int pricePerItem)
        {
            if (item == null || quantity <= 0 || !Inventory.ContainsKey(item))
                return 0;
                
            // Calculate how many we can actually sell
            int maxSellable = Inventory[item];
            int actualQuantity = Math.Min(quantity, maxSellable);
            
            if (actualQuantity <= 0)
                return 0;
                
            // Update capital
            int totalEarned = actualQuantity * pricePerItem;
            Capital += totalEarned;
            
            // Update inventory
            Inventory[item] -= actualQuantity;
            if (Inventory[item] <= 0)
                Inventory.Remove(item);
                
            return actualQuantity;
        }
        
        /// <summary>
        /// Calculates the daily income from sold goods
        /// </summary>
        public int CalculateDailyIncome()
        {
            // This is a simplified model. In a full mod, this would be more complex.
            int baseIncome = Level * 200;
            float townProsperityFactor = Town.Prosperity / 5000f;
            
            return (int)(baseIncome * (1 + townProsperityFactor));
        }
        
        /// <summary>
        /// Calculates the daily wages for shop employees
        /// </summary>
        public int CalculateDailyWages()
        {
            int baseCost = Constants.BaseShopMaintenanceCost * Level;
            int employeeCost = Employees.Sum(e => e.DailyWage);
            
            return baseCost + employeeCost;
        }
        
        /// <summary>
        /// Updates the shop for daily tick
        /// </summary>
        public void UpdateShop()
        {
            if (!IsActive)
                return;
                
            // Calculate elapsed days since last update
            float elapsedDays = (CampaignTime.Now - LastUpdateTime).ToDays;
            
            if (elapsedDays < 1.0f)
                return;
                
            // Update shop finances
            int incomePerDay = CalculateDailyIncome();
            int wagesPerDay = CalculateDailyWages();
            int profitPerDay = incomePerDay - wagesPerDay;
            
            Capital += (int)(profitPerDay * elapsedDays);
            
            // If we're out of money, deactivate the shop
            if (Capital < 0)
            {
                IsActive = false;
                InformationManager.DisplayMessage(new InformationMessage(
                    $"Your wholesale shop in {Town.Name} has run out of money and has been closed.", 
                    Colors.Red));
            }
            
            // Update last update time
            LastUpdateTime = CampaignTime.Now;
        }
        
        /// <summary>
        /// Upgrades the shop to the next level if possible
        /// </summary>
        /// <returns>Whether the upgrade was successful</returns>
        public bool Upgrade()
        {
            if (Level >= Constants.MaxShopLevel)
                return false;
                
            int upgradeCost = GetUpgradeCost();
            
            if (Capital < upgradeCost)
                return false;
                
            // Apply the upgrade
            Capital -= upgradeCost;
            Level++;
            StorageCapacity += Constants.BaseShopStorageCapacity / 2;
            
            return true;
        }
        
        /// <summary>
        /// Gets the cost to upgrade the shop to the next level
        /// </summary>
        public int GetUpgradeCost()
        {
            if (Level >= Constants.MaxShopLevel)
                return int.MaxValue;
                
            float settingsMultiplier = Settings.Instance?.ShopUpgradeCostPercent / 100f ?? 1.0f;
            return (int)(Constants.BaseShopCost * Level * settingsMultiplier);
        }
        
        /// <summary>
        /// Hires an employee to work at the shop
        /// </summary>
        public bool HireEmployee(WholesaleEmployee employee)
        {
            if (employee == null || Employees.Count >= Level * 2)
                return false;
                
            Employees.Add(employee);
            DailyWages = CalculateDailyWages();
            
            return true;
        }
        
        /// <summary>
        /// Fires an employee from the shop
        /// </summary>
        public bool FireEmployee(WholesaleEmployee employee)
        {
            if (employee == null || !Employees.Contains(employee))
                return false;
                
            Employees.Remove(employee);
            DailyWages = CalculateDailyWages();
            
            return true;
        }
        
        /// <summary>
        /// Gets the current wholesale price for an item
        /// </summary>
        public int GetWholesaleBuyPrice(ItemObject item, int quantity)
        {
            if (item == null || quantity <= 0)
                return 0;
                
            // Get base item price from town market
            int basePrice = Town.GetItemPrice(item);
            
            // Apply wholesale discount for bulk purchases
            float discount = quantity >= Constants.WholesaleBulkThreshold ? 
                Constants.WholesaleDiscountMultiplier : 1.0f;
                
            // Apply settings multiplier
            float settingsMultiplier = Settings.Instance?.PriceMultiplier ?? 1.0f;
            
            return (int)(basePrice * discount * settingsMultiplier);
        }
        
        /// <summary>
        /// Gets the current selling price for an item
        /// </summary>
        public int GetWholesaleSellPrice(ItemObject item, int quantity)
        {
            if (item == null || quantity <= 0)
                return 0;
                
            // Start with the buy price
            int buyPrice = GetWholesaleBuyPrice(item, quantity);
            
            // Apply the profit margin
            return (int)(buyPrice * (1.0f + ProfitMargin));
        }
    }
} 