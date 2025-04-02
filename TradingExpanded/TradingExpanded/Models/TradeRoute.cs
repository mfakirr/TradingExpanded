using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.SaveSystem;
using TradingExpanded.Helpers;

namespace TradingExpanded.Models
{
    /// <summary>
    /// Defines a trade route between settlements
    /// </summary>
    public class TradeRoute
    {
        [SaveableProperty(1)]
        public string Id { get; set; }
        
        [SaveableProperty(2)]
        public string Name { get; set; }
        
        [SaveableProperty(3)]
        public List<Town> Waypoints { get; private set; }
        
        [SaveableProperty(4)]
        public bool IsCircular { get; set; }
        
        [SaveableProperty(5)]
        public bool IsActive { get; set; }
        
        [SaveableProperty(6)]
        public int ExpectedProfit { get; set; }
        
        [SaveableProperty(7)]
        public int EstimatedDuration { get; set; }
        
        /// <summary>
        /// The total length of the route in days
        /// </summary>
        public int TotalLength
        {
            get
            {
                if (Waypoints.Count <= 1)
                    return 0;
                    
                int length = 0;
                
                for (int i = 0; i < Waypoints.Count - 1; i++)
                {
                    length += CalculateTravelTime(Waypoints[i], Waypoints[i + 1]);
                }
                
                // Add return journey for circular routes
                if (IsCircular && Waypoints.Count > 1)
                {
                    length += CalculateTravelTime(Waypoints.Last(), Waypoints.First());
                }
                
                return length;
            }
        }
        
        /// <summary>
        /// İki şehir arasındaki yolculuk süresini hesaplar
        /// </summary>
        private int CalculateTravelTime(Town origin, Town destination, float speedMultiplier = 1.0f)
        {
            if (origin == null || destination == null)
                return 0;
                
            // Basit mesafe hesabı (gerçekte daha karmaşık olabilir)
            float distance = origin.Settlement.Position2D.Distance(destination.Settlement.Position2D);
            
            // Gün cinsinden yaklaşık yolculuk süresi
            int travelDays = (int)(distance / (speedMultiplier * 10));
            
            // En az 1 gün
            return System.Math.Max(1, travelDays);
        }
        
        /// <summary>
        /// Default constructor for saving/loading
        /// </summary>
        public TradeRoute()
        {
            Waypoints = new List<Town>();
        }
        
        /// <summary>
        /// Creates a new trade route with the given name
        /// </summary>
        public TradeRoute(string name, bool isCircular = true)
        {
            Id = Constants.GenerateUniqueId();
            Name = name;
            Waypoints = new List<Town>();
            IsCircular = isCircular;
            IsActive = true;
            ExpectedProfit = 0;
            EstimatedDuration = 0;
        }
        
        /// <summary>
        /// Adds a waypoint to the route
        /// </summary>
        public void AddWaypoint(Town town)
        {
            if (town == null || Waypoints.Contains(town))
                return;
                
            Waypoints.Add(town);
            UpdateEstimates();
        }
        
        /// <summary>
        /// Inserts a waypoint at the specified index
        /// </summary>
        public void InsertWaypoint(Town town, int index)
        {
            if (town == null || Waypoints.Contains(town) || index < 0 || index > Waypoints.Count)
                return;
                
            Waypoints.Insert(index, town);
            UpdateEstimates();
        }
        
        /// <summary>
        /// Removes a waypoint from the route
        /// </summary>
        public void RemoveWaypoint(Town town)
        {
            if (town == null || !Waypoints.Contains(town))
                return;
                
            Waypoints.Remove(town);
            UpdateEstimates();
        }
        
        /// <summary>
        /// Moves a waypoint up in the route
        /// </summary>
        public void MoveWaypointUp(Town town)
        {
            int index = Waypoints.IndexOf(town);
            
            if (index <= 0)
                return;
                
            Waypoints.RemoveAt(index);
            Waypoints.Insert(index - 1, town);
            UpdateEstimates();
        }
        
        /// <summary>
        /// Moves a waypoint down in the route
        /// </summary>
        public void MoveWaypointDown(Town town)
        {
            int index = Waypoints.IndexOf(town);
            
            if (index < 0 || index >= Waypoints.Count - 1)
                return;
                
            Waypoints.RemoveAt(index);
            Waypoints.Insert(index + 1, town);
            UpdateEstimates();
        }
        
        /// <summary>
        /// Clears all waypoints from the route
        /// </summary>
        public void ClearWaypoints()
        {
            Waypoints.Clear();
            UpdateEstimates();
        }
        
        /// <summary>
        /// Updates the estimated duration and profit for the route
        /// </summary>
        public void UpdateEstimates()
        {
            if (Waypoints.Count <= 1)
            {
                EstimatedDuration = 0;
                ExpectedProfit = 0;
                return;
            }
            
            // Calculate duration based on distances
            EstimatedDuration = TotalLength;
            
            // Add trading time (roughly 1-2 days per town)
            EstimatedDuration += Waypoints.Count * 2;
            
            // Estimate profits (this is a very rough estimate)
            // In a real implementation, you'd analyze price differentials between towns
            ExpectedProfit = EstimateProfits();
        }
        
        /// <summary>
        /// Estimates the potential profits for this route
        /// </summary>
        private int EstimateProfits()
        {
            if (Waypoints.Count <= 1)
                return 0;
                
            // This is a simplified profit estimation
            // In a real implementation, you would check actual item prices in each town
            
            int baseProfit = Waypoints.Count * 500; // Base profit per town
            
            // Add bonus for prosperity
            int prosperityBonus = Waypoints.Sum(town => (int)(town.Prosperity / 20));
            
            // Longer routes have diminishing returns due to maintenance costs
            float lengthPenalty = 1.0f - (0.05f * (Waypoints.Count - 2));
            lengthPenalty = System.Math.Max(0.5f, lengthPenalty);
            
            return (int)((baseProfit + prosperityBonus) * lengthPenalty);
        }
        
        /// <summary>
        /// Gets a description of the route
        /// </summary>
        public string GetRouteDescription()
        {
            if (Waypoints.Count == 0)
                return "No waypoints defined.";
                
            string description = string.Join(" → ", Waypoints.Select(town => town.Name.ToString()));
            
            if (IsCircular && Waypoints.Count > 1)
            {
                description += " → " + Waypoints[0].Name.ToString();
            }
            
            return description;
        }
        
        /// <summary>
        /// Creates a route that visits towns with good price differentials for the given item
        /// </summary>
        public static TradeRoute CreateOptimalRouteForItem(ItemObject item, Town startingTown, int maxWaypoints = 5)
        {
            // This would involve complex price analysis and route optimization
            // For now, we'll create a simple route with nearby towns
            
            var route = new TradeRoute($"Route for {item.Name}");
            
            if (startingTown != null)
            {
                route.AddWaypoint(startingTown);
                
                // Get nearby towns, sorted by distance
                var nearbyTowns = Settlement.All
                    .Where(s => s.IsTown && s.Town != startingTown)
                    .OrderBy(s => s.Position2D.Distance(startingTown.Settlement.Position2D))
                    .Take(maxWaypoints - 1)
                    .Select(s => s.Town);
                    
                foreach (var town in nearbyTowns)
                {
                    route.AddWaypoint(town);
                }
            }
            
            return route;
        }
    }
} 