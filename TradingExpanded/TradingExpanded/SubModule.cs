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
using TradingExpanded.Helpers;
using TaleWorlds.ObjectSystem;
using TaleWorlds.Localization;
using TaleWorlds.CampaignSystem.Party;
using TradingExpanded.Components;

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
        
        // Parti şablonu ID'si
        public const string COURIER_PARTY_TEMPLATE_ID = "trading_expanded_courier_party";
        
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
                
                // Kurye parti şablonlarını kaydet
                RegisterCourierPartyTemplates();
                
                LogManager.Instance.WriteInfo("TradingExpanded kampanya davranışı eklendi");
            }
            catch (Exception ex)
            {
                LogManager.Instance.WriteError("GameStart sırasında hata oluştu", ex);
            }
        }
        
        /// <summary>
        /// Kurye parti şablonlarını kaydet
        /// </summary>
        private void RegisterCourierPartyTemplates()
        {
            try
            {
                // Önce şablonun var olup olmadığını kontrol et
                PartyTemplateObject existingTemplate = MBObjectManager.Instance.GetObject<PartyTemplateObject>(COURIER_PARTY_TEMPLATE_ID);
                
                if (existingTemplate != null)
                {
                    LogManager.Instance.WriteInfo($"Kurye parti şablonu zaten mevcut: {COURIER_PARTY_TEMPLATE_ID}");
                }
                else
                {
                    // Kurye parti şablonu oluştur
                    Campaign campaign = Campaign.Current;
                    if (campaign?.ObjectManager != null)
                    {
                        // Yeni şablon oluştur
                        PartyTemplateObject courierTemplate = campaign.ObjectManager.CreateObject<PartyTemplateObject>();
                        courierTemplate.StringId = COURIER_PARTY_TEMPLATE_ID;
                       // courierTemplate. = new TextObject("{=CourierTemplateParty}Kurye Partisi Şablonu");
                        
                        // Şablonu kaydet
                        MBObjectManager.Instance.RegisterObject(courierTemplate);
                        
                        LogManager.Instance.WriteInfo($"Kurye parti şablonu başarıyla kaydedildi: {COURIER_PARTY_TEMPLATE_ID}");
                    }
                    else
                    {
                        LogManager.Instance.WriteError("Campaign veya ObjectManager null, şablon oluşturulma başarısız.");
                    }
                }
                
                // CourierPartyComponent'i kaydet (önemli)
                RegisterCourierPartyComponent();
            }
            catch (Exception ex)
            {
                LogManager.Instance.WriteError("Kurye parti şablonu kaydedilirken hata oluştu", ex);
            }
        }
        
        /// <summary>
        /// CourierPartyComponent'i kaydeder
        /// </summary>
        private void RegisterCourierPartyComponent()
        {
            try
            {
                // Oyun objelerine component'i kaydet - kurye parti bileşenini ekle
                LogManager.Instance.WriteInfo("CourierPartyComponent kaydediliyor...");

                // Not: PartyComponent sınıfları MBObjectBase'den türemediği için RegisterType kullanılamaz
                // Bu bileşen zaten PartyComponent'ten türediği için özel bir kayıt işlemine genellikle gerek yoktur
                // Bileşen, kullanıldığında otomatik olarak tanınacaktır
                
                LogManager.Instance.WriteInfo("CourierPartyComponent Hazır!");
            }
            catch (Exception ex)
            {
                LogManager.Instance.WriteError("CourierPartyComponent kaydedilirken hata oluştu", ex);
            }
        }

        // ... existing code ...
    }
} 