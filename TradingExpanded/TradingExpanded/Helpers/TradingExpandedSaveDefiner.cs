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
            AddClassDefinition(typeof(WholesaleShop), TradingExpandedTypeIds.WholesaleShop);
            AddClassDefinition(typeof(WholesaleBuyOrder), TradingExpandedTypeIds.WholesaleBuyOrder);
            AddClassDefinition(typeof(WholesaleSellOrder), TradingExpandedTypeIds.WholesaleSellOrder);
            
            AddClassDefinition(typeof(WholesaleEmployee), TradingExpandedTypeIds.WholesaleEmployee);
            AddClassDefinition(typeof(TradeCaravan), TradingExpandedTypeIds.TradeCaravan);
            AddClassDefinition(typeof(TradeRoute), TradingExpandedTypeIds.TradeRoute);
            AddClassDefinition(typeof(Courier), TradingExpandedTypeIds.Courier);
            AddClassDefinition(typeof(MerchantRelation), TradingExpandedTypeIds.MerchantRelation);
            AddClassDefinition(typeof(TradeAgreement), TradingExpandedTypeIds.TradeAgreement);
            
            // Analytics and stats objects
            AddClassDefinition(typeof(InventoryTracker), TradingExpandedTypeIds.InventoryTracker);
            AddClassDefinition(typeof(ItemStats), TradingExpandedTypeIds.ItemStats);
            AddClassDefinition(typeof(PriceHistory), TradingExpandedTypeIds.PriceHistory);
            AddClassDefinition(typeof(PriceDataPoint), TradingExpandedTypeIds.PriceDataPoint);
            
            // İç sınıflar için
            AddClassDefinition(typeof(PriceTracker.FiyatKaydi), TradingExpandedTypeIds.PriceTrackerFiyatKaydi);
            AddClassDefinition(typeof(PriceTracker.IslemKaydi), TradingExpandedTypeIds.PriceTrackerIslemKaydi);
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
//            ConstructContainerDefinition(typeof(Dictionary<Town, Dictionary<ItemObject, PriceHistory>>));
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