using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TradingExpanded.Behaviors;
using TradingExpanded.Repositories;
using TradingExpanded.Models;
using TradingExpanded.Services;
using TradingExpanded.Utils;
using TradingExpanded.UI.Patches;
using HarmonyLib;

namespace TradingExpanded
{
    // BLSE loader attribute
    [AttributeUsage(AttributeTargets.Class)]
    public class BLSEInterceptorAttribute : Attribute { }

    // BLSE loading interceptor
    public static class BLSELoadingInterceptor
    {
        public static void OnInitializing()
        {
            LogManager.Instance.WriteInfo("TradingExpanded modu başlatılıyor...");
        }
    }

    public class SubModule : MBSubModuleBase
    {
        // Dependency Injection container
        private static ITradingExpandedServiceProvider _serviceProvider;
        
        // Harmony instance
        private Harmony _harmony;
        
        protected override void OnSubModuleLoad()
        {
            try
            {
                LogManager.Instance.WriteInfo("TradingExpanded SubModule yükleniyor...");
                
                // Harmony patch'lerini başlat
                _harmony = new Harmony("com.tradingexpanded.patches");
                _harmony.PatchAll();
                
                LogManager.Instance.WriteInfo("Harmony patch'leri uygulandı");
            }
            catch (Exception ex)
            {
                LogManager.Instance.WriteError("ModuleLoad sırasında hata oluştu", ex);
            }
        }
        
        protected override void OnSubModuleUnloaded()
        {
            try
            {
                LogManager.Instance.WriteInfo("TradingExpanded SubModule kapatılıyor...");
                
                // Harmony patch'lerini kaldır
                _harmony?.UnpatchAll("com.tradingexpanded.patches");
                
                // Mod kapanırken son kayıt ve temizlik işlemleri
                LogManager.Instance.Flush();
            }
            catch (Exception ex)
            {
                LogManager.Instance.WriteError("ModuleUnload sırasında hata oluştu", ex);
            }
        }

        protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
        {
            try
            {
                if (!(game.GameType is Campaign))
                    return;

                // Servis sağlayıcıyı oluştur
                _serviceProvider = new ServiceProvider();
                
                // Repository'leri kaydet
                _serviceProvider.AddService<IRepository<WholesaleShop>>(new WholesaleShopRepository());
                _serviceProvider.AddService<IRepository<TradeCaravan>>(new TradeCaravanRepository());
                _serviceProvider.AddService<IRepository<Courier>>(new CourierRepository());
                
                // Behavior'ları ekle
                var campaignStarter = (CampaignGameStarter)gameStarterObject;
                campaignStarter.AddBehavior(new TradingExpandedCampaignBehavior(_serviceProvider));
                
                LogManager.Instance.WriteInfo("TradingExpanded kampanya davranışı eklendi");
            }
            catch (Exception ex)
            {
                LogManager.Instance.WriteError("GameStart sırasında hata oluştu", ex);
            }
        }

        // ... existing code ...
    }
} 