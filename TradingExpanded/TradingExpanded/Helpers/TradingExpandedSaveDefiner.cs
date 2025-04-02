using System.Collections.Generic;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.SaveSystem;
using TradingExpanded.Models;

namespace TradingExpanded.Helpers
{
    /// <summary>
    /// Defines custom objects for serialization with the game's save system
    /// </summary>
    public class TradingExpandedSaveDefiner : SaveableTypeDefiner
    {
        public TradingExpandedSaveDefiner() : base(Constants.SaveBaseId) { }
        
        protected override void DefineClassTypes()
        {
            // Main business objects
            AddClassDefinition(typeof(WholesaleShop), 1);
            AddClassDefinition(typeof(WholesaleBuyOrder), 11); // Inner class
            AddClassDefinition(typeof(WholesaleSellOrder), 12); // Inner class
            
            AddClassDefinition(typeof(WholesaleEmployee), 2);
            AddClassDefinition(typeof(TradeCaravan), 3);
            AddClassDefinition(typeof(TradeRoute), 4);
            AddClassDefinition(typeof(Courier), 5);
            AddClassDefinition(typeof(MerchantRelation), 6);
            AddClassDefinition(typeof(TradeAgreement), 7);
            
            // Analytics and stats objects
            AddClassDefinition(typeof(InventoryTracker), 8);
            AddClassDefinition(typeof(ItemStats), 9);
            AddClassDefinition(typeof(PriceHistory), 10);
            AddClassDefinition(typeof(PriceDataPoint), 13);
            
            // İç sınıflar için
            AddClassDefinition(typeof(PriceTracker.FiyatKaydi), 41);
            AddClassDefinition(typeof(PriceTracker.IslemKaydi), 42);
        }
        
        protected override void DefineContainerDefinitions()
        {
            // Define dictionaries and lists for our custom objects
            ConstructContainerDefinition(typeof(List<WholesaleShop>));
            ConstructContainerDefinition(typeof(Dictionary<string, WholesaleShop>));
            
            ConstructContainerDefinition(typeof(List<WholesaleEmployee>));
            
            ConstructContainerDefinition(typeof(List<TradeCaravan>));
            ConstructContainerDefinition(typeof(Dictionary<string, TradeCaravan>));
            
            ConstructContainerDefinition(typeof(List<Town>));
            
            ConstructContainerDefinition(typeof(List<Courier>));
            ConstructContainerDefinition(typeof(Dictionary<string, Courier>));
            
            ConstructContainerDefinition(typeof(List<MerchantRelation>));
            ConstructContainerDefinition(typeof(Dictionary<string, MerchantRelation>));
            
            ConstructContainerDefinition(typeof(List<TradeAgreement>));
            
            ConstructContainerDefinition(typeof(Dictionary<ItemObject, ItemStats>));
            ConstructContainerDefinition(typeof(Dictionary<Town, Dictionary<ItemObject, PriceHistory>>));
            ConstructContainerDefinition(typeof(Dictionary<ItemObject, PriceHistory>));
            
            ConstructContainerDefinition(typeof(List<PriceDataPoint>));
            
            // Dictionary with ItemObject key
            ConstructContainerDefinition(typeof(Dictionary<ItemObject, int>));
            ConstructContainerDefinition(typeof(Dictionary<ItemObject, WholesaleBuyOrder>));
            ConstructContainerDefinition(typeof(Dictionary<ItemObject, WholesaleSellOrder>));
            
            // PriceTracker iç sınıfları için
            ConstructContainerDefinition(typeof(List<PriceTracker.FiyatKaydi>));
            ConstructContainerDefinition(typeof(List<PriceTracker.IslemKaydi>));
            ConstructContainerDefinition(typeof(Dictionary<string, List<PriceTracker.FiyatKaydi>>));
        }
    }
} 