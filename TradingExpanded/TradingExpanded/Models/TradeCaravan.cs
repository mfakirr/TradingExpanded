using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;

namespace TradingExpanded.Models
{
    /// <summary>
    /// Represents a player-owned trade caravan
    /// </summary>
    [SaveableClass(Constants.SaveBaseId + 3)]
    public class TradeCaravan
    {
        /// <summary>
        /// State of the caravan
        /// </summary>
        public enum CaravanState
        {
            Idle,
            Traveling,
            Trading,
            UnderAttack,
            Returning
        }
        
        [SaveableProperty(1)]
        public string Id { get; set; }
        
        [SaveableProperty(2)]
        public Hero Leader { get; set; }
        
        [SaveableProperty(3)]
        public MobileParty MobileParty { get; set; }
        
        [SaveableProperty(4)]
        public Town CurrentTown { get; set; }
        
        [SaveableProperty(5)]
        public Town DestinationTown { get; set; }
        
        [SaveableProperty(6)]
        public Dictionary<ItemObject, int> Cargo { get; private set; }
        
        [SaveableProperty(7)]
        public int Capital { get; set; }
        
        [SaveableProperty(8)]
        public TradeRoute Route { get; set; }
        
        [SaveableProperty(9)]
        public CaravanState State { get; set; }
        
        [SaveableProperty(10)]
        public int SecurityLevel { get; set; }
        
        [SaveableProperty(11)]
        public float Speed { get; set; }
        
        [SaveableProperty(12)]
        public int CargoCapacity { get; set; }
        
        [SaveableProperty(13)]
        public CampaignTime LastUpdateTime { get; set; }
        
        [SaveableProperty(14)]
        public int DaysInCurrentState { get; set; }
        
        [SaveableProperty(15)]
        public bool IsActive { get; set; }
        
        /// <summary>
        /// Name of the caravan for display
        /// </summary>
        public string Name => $"{Leader.Name}'s Trade Caravan";
        
        /// <summary>
        /// Current cargo value
        /// </summary>
        public int CargoValue => Cargo.Sum(pair => pair.Value * CurrentTown.GetItemPrice(pair.Key));
        
        /// <summary>
        /// Available cargo space
        /// </summary>
        public int AvailableCargoSpace => CargoCapacity - Cargo.Values.Sum();
        
        /// <summary>
        /// Default constructor for saving/loading
        /// </summary>
        public TradeCaravan()
        {
            Cargo = new Dictionary<ItemObject, int>();
        }
        
        /// <summary>
        /// Creates a new trade caravan
        /// </summary>
        public TradeCaravan(Town startingTown, Hero leader, int initialCapital = 5000)
        {
            Id = Constants.GenerateUniqueId();
            CurrentTown = startingTown;
            Leader = leader;
            Capital = initialCapital;
            Cargo = new Dictionary<ItemObject, int>();
            State = CaravanState.Idle;
            SecurityLevel = 1;
            Speed = Constants.BaseCaravanSpeed * (Settings.Instance?.CaravanSpeedMultiplier ?? 1.0f);
            CargoCapacity = Constants.BaseCaravanCapacity;
            LastUpdateTime = CampaignTime.Now;
            DaysInCurrentState = 0;
            IsActive = true;
            
            // TODO: Create actual mobile party in the game when implementing
            // Currently just a placeholder
            // MobileParty = MobileParty.CreateParty("caravan_" + Id, null);
        }
        
        /// <summary>
        /// Begins the journey to the destination town
        /// </summary>
        public void StartJourney(Town destination)
        {
            if (destination == CurrentTown || State != CaravanState.Idle)
                return;
                
            DestinationTown = destination;
            State = CaravanState.Traveling;
            DaysInCurrentState = 0;
            
            // Calculate how long the journey will take
            int travelDays = Constants.CalculateTravelTime(CurrentTown, DestinationTown, Speed);
            
            // TODO: Set the actual path for MobileParty when implementing
        }
        
        /// <summary>
        /// Updates the caravan based on its current state
        /// </summary>
        public void UpdateCaravan()
        {
            if (!IsActive)
                return;
                
            // Calculate elapsed days since last update
            float elapsedDays = (CampaignTime.Now - LastUpdateTime).ToDays;
            
            if (elapsedDays < 0.1f)
                return;
                
            DaysInCurrentState += (int)elapsedDays;
            
            switch (State)
            {
                case CaravanState.Idle:
                    UpdateIdle();
                    break;
                case CaravanState.Traveling:
                    UpdateTraveling();
                    break;
                case CaravanState.Trading:
                    UpdateTrading();
                    break;
                case CaravanState.UnderAttack:
                    UpdateUnderAttack();
                    break;
                case CaravanState.Returning:
                    UpdateReturning();
                    break;
            }
            
            // Apply daily costs
            ApplyDailyCosts(elapsedDays);
            
            // Check if we're out of money
            if (Capital < 0)
            {
                IsActive = false;
                InformationManager.DisplayMessage(new InformationMessage(
                    $"Your trade caravan led by {Leader.Name} has run out of money and has been disbanded.", 
                    Colors.Red));
            }
            
            LastUpdateTime = CampaignTime.Now;
        }
        
        /// <summary>
        /// Updates the caravan in Idle state
        /// </summary>
        private void UpdateIdle()
        {
            // In idle state, we stay in the current town
            // Can be used for maintenance, repairs, etc.
            
            // For now, we'll just apply a small daily income from local trading
            Capital += 50;
        }
        
        /// <summary>
        /// Updates the caravan in Traveling state
        /// </summary>
        private void UpdateTraveling()
        {
            if (DestinationTown == null)
            {
                State = CaravanState.Idle;
                return;
            }
            
            // Calculate travel time
            int travelDays = Constants.CalculateTravelTime(CurrentTown, DestinationTown, Speed);
            
            // Check if we've arrived
            if (DaysInCurrentState >= travelDays)
            {
                // We've arrived at the destination
                CurrentTown = DestinationTown;
                State = CaravanState.Trading;
                DaysInCurrentState = 0;
                
                InformationManager.DisplayMessage(new InformationMessage(
                    $"Your trade caravan has arrived in {CurrentTown.Name}.", Colors.Green));
            }
            else
            {
                // Still traveling, check for random events
                CheckForTravelEvents();
            }
        }
        
        /// <summary>
        /// Updates the caravan in Trading state
        /// </summary>
        private void UpdateTrading()
        {
            // Trading takes 1-3 days
            int tradingDays = 1 + (CurrentTown.Prosperity > 5000 ? 1 : 0) + (CurrentTown.Prosperity > 8000 ? 1 : 0);
            
            if (DaysInCurrentState >= tradingDays)
            {
                // Trading completed, sell goods and buy new ones
                SellCargo();
                BuyNewCargo();
                
                // If we have a route, continue to the next destination
                if (Route != null && Route.Waypoints.Count > 0)
                {
                    // Find the next waypoint in the route
                    Town nextDestination = FindNextDestination();
                    if (nextDestination != null)
                    {
                        StartJourney(nextDestination);
                    }
                    else
                    {
                        State = CaravanState.Idle;
                    }
                }
                else
                {
                    // No route, go idle
                    State = CaravanState.Idle;
                }
            }
        }
        
        /// <summary>
        /// Updates the caravan in UnderAttack state
        /// </summary>
        private void UpdateUnderAttack()
        {
            // Simulate the attack resolution
            Random random = new Random();
            
            // Higher security level = better chance of surviving
            float survivalChance = 0.4f + (SecurityLevel * 0.1f);
            
            if (random.NextDouble() < survivalChance)
            {
                // Survived the attack
                InformationManager.DisplayMessage(new InformationMessage(
                    $"Your trade caravan led by {Leader.Name} has successfully defended against an attack!", 
                    Colors.Green));
                
                // Calculate losses
                int cargoLost = random.Next(10, 30);
                RemoveRandomCargo(cargoLost);
                
                // Continue journey
                State = CaravanState.Traveling;
            }
            else
            {
                // Caravan was defeated
                InformationManager.DisplayMessage(new InformationMessage(
                    $"Your trade caravan led by {Leader.Name} was defeated by attackers and has lost most of its cargo.", 
                    Colors.Red));
                
                // Lose most cargo
                RemoveRandomCargo(random.Next(70, 100));
                
                // Return to the nearest town
                State = CaravanState.Returning;
            }
        }
        
        /// <summary>
        /// Updates the caravan in Returning state
        /// </summary>
        private void UpdateReturning()
        {
            // Returning to the nearest town after an attack
            // Similar to traveling but with reduced speed
            
            // Calculate travel time with reduced speed
            int travelDays = Constants.CalculateTravelTime(CurrentTown, DestinationTown, Speed * 0.7f);
            
            if (DaysInCurrentState >= travelDays)
            {
                // We've arrived back at a town
                CurrentTown = DestinationTown;
                State = CaravanState.Idle;
                DaysInCurrentState = 0;
                
                InformationManager.DisplayMessage(new InformationMessage(
                    $"Your damaged trade caravan has found refuge in {CurrentTown.Name}.", Colors.Green));
            }
        }
        
        /// <summary>
        /// Applies daily costs to the caravan
        /// </summary>
        private void ApplyDailyCosts(float days)
        {
            // Base cost + security cost
            int dailyCost = 50 + (SecurityLevel * Constants.BaseCaravanSecurityCost *
                (int)(Settings.Instance?.CaravanSecurityCostMultiplier ?? 1.0f));
            
            Capital -= (int)(dailyCost * days);
        }
        
        /// <summary>
        /// Checks for random events during travel
        /// </summary>
        private void CheckForTravelEvents()
        {
            // Simple random event system
            Random random = new Random();
            
            // Base chance of an attack
            float attackChance = 0.02f;
            
            // Adjust based on security level
            attackChance -= SecurityLevel * 0.003f;
            
            // Higher chance if carrying valuable cargo
            if (CargoValue > 5000)
                attackChance += 0.01f;
                
            // Check if an attack occurs
            if (random.NextDouble() < attackChance)
            {
                State = CaravanState.UnderAttack;
                DaysInCurrentState = 0;
                
                InformationManager.DisplayMessage(new InformationMessage(
                    $"Your trade caravan led by {Leader.Name} is under attack!", Colors.Red));
            }
        }
        
        /// <summary>
        /// Sells the caravan's cargo at the current town
        /// </summary>
        private void SellCargo()
        {
            int totalEarnings = 0;
            List<ItemObject> soldItems = new List<ItemObject>();
            
            foreach (var pair in Cargo)
            {
                ItemObject item = pair.Key;
                int quantity = pair.Value;
                
                // Get current price in town
                int price = CurrentTown.GetItemPrice(item);
                
                // Sell all
                totalEarnings += price * quantity;
                soldItems.Add(item);
            }
            
            // Update capital
            Capital += totalEarnings;
            
            // Clear sold items
            foreach (var item in soldItems)
            {
                Cargo.Remove(item);
            }
            
            if (totalEarnings > 0)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    $"Your trade caravan sold goods in {CurrentTown.Name} for {totalEarnings} denars.", 
                    Colors.Green));
            }
        }
        
        /// <summary>
        /// Buys new cargo at the current town
        /// </summary>
        private void BuyNewCargo()
        {
            Random random = new Random();
            
            // Reserve some capital for expenses
            int availableCapital = (int)(Capital * 0.8f);
            
            if (availableCapital <= 0)
                return;
                
            // Get items available in the town
            List<ItemObject> availableItems = ItemObject.All
                .Where(item => item.ItemCategory != DefaultItemCategories.Horse && 
                               item.ItemCategory != DefaultItemCategories.WarHorse &&
                               item.ItemCategory != DefaultItemCategories.Armor &&
                               item.ItemCategory != DefaultItemCategories.WeaponMelee &&
                               item.ItemCategory != DefaultItemCategories.WeaponRanged)
                .ToList();
                
            // Buy items until we're out of money or space
            while (availableCapital > 0 && AvailableCargoSpace > 0 && availableItems.Count > 0)
            {
                // Pick a random item
                int itemIndex = random.Next(availableItems.Count);
                ItemObject item = availableItems[itemIndex];
                
                // Get price at current town
                int buyPrice = CurrentTown.GetItemPrice(item);
                
                // Skip expensive items if low on money
                if (buyPrice > availableCapital / 5)
                {
                    availableItems.RemoveAt(itemIndex);
                    continue;
                }
                
                // Check if the destination has a good selling price
                bool isProfitable = false;
                if (DestinationTown != null)
                {
                    int sellPrice = DestinationTown.GetItemPrice(item);
                    isProfitable = sellPrice > buyPrice * 1.2f; // At least 20% profit
                }
                
                // Only buy if profitable or we have no destination
                if (isProfitable || DestinationTown == null)
                {
                    // Calculate quantity to buy
                    int maxQuantity = Math.Min(availableCapital / buyPrice, AvailableCargoSpace);
                    int quantityToBuy = random.Next(1, Math.Max(2, maxQuantity));
                    
                    // Buy the items
                    int totalCost = quantityToBuy * buyPrice;
                    availableCapital -= totalCost;
                    Capital -= totalCost;
                    
                    // Add to cargo
                    if (Cargo.ContainsKey(item))
                        Cargo[item] += quantityToBuy;
                    else
                        Cargo[item] = quantityToBuy;
                }
                
                // Remove item from available list to avoid buying too much of the same thing
                availableItems.RemoveAt(itemIndex);
            }
        }
        
        /// <summary>
        /// Removes a percentage of random cargo
        /// </summary>
        private void RemoveRandomCargo(int percentToRemove)
        {
            if (Cargo.Count == 0)
                return;
                
            Random random = new Random();
            
            List<ItemObject> items = Cargo.Keys.ToList();
            foreach (var item in items)
            {
                int currentQuantity = Cargo[item];
                int amountToRemove = (int)(currentQuantity * (percentToRemove / 100f));
                
                Cargo[item] -= amountToRemove;
                if (Cargo[item] <= 0)
                    Cargo.Remove(item);
            }
        }
        
        /// <summary>
        /// Finds the next destination in the route
        /// </summary>
        private Town FindNextDestination()
        {
            if (Route == null || Route.Waypoints.Count == 0)
                return null;
                
            // Find current position in route
            int currentIndex = Route.Waypoints.IndexOf(CurrentTown);
            
            if (currentIndex < 0)
            {
                // Not on route, find closest waypoint
                return Route.Waypoints[0];
            }
            else if (currentIndex < Route.Waypoints.Count - 1)
            {
                // Next waypoint
                return Route.Waypoints[currentIndex + 1];
            }
            else if (Route.IsCircular)
            {
                // Circle back to beginning
                return Route.Waypoints[0];
            }
            
            return null;
        }
        
        /// <summary>
        /// Increases the security level of the caravan
        /// </summary>
        public bool UpgradeSecurity()
        {
            int upgradeCost = GetSecurityUpgradeCost();
            
            if (Capital < upgradeCost)
                return false;
                
            Capital -= upgradeCost;
            SecurityLevel++;
            
            return true;
        }
        
        /// <summary>
        /// Gets the cost to upgrade security
        /// </summary>
        public int GetSecurityUpgradeCost()
        {
            return 500 * SecurityLevel;
        }
        
        /// <summary>
        /// Increases the cargo capacity of the caravan
        /// </summary>
        public bool UpgradeCargoCapacity()
        {
            int upgradeCost = GetCapacityUpgradeCost();
            
            if (Capital < upgradeCost)
                return false;
                
            Capital -= upgradeCost;
            CargoCapacity += Constants.BaseCaravanCapacity / 2;
            
            return true;
        }
        
        /// <summary>
        /// Gets the cost to upgrade cargo capacity
        /// </summary>
        public int GetCapacityUpgradeCost()
        {
            return 1000 * (CargoCapacity / Constants.BaseCaravanCapacity);
        }
    }
} 