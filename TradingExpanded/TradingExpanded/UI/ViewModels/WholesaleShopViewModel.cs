using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TradingExpanded.Models;
using TradingExpanded.Behaviors;
using TradingExpanded.Services;

namespace TradingExpanded.UI.ViewModels
{
    /// <summary>
    /// Toptan satış dükkanı ekranında kullanılacak ViewModel
    /// </summary>
    public class WholesaleShopViewModel : ViewModel
    {
        private readonly WholesaleShop _shop;
        private readonly Town _currentTown;
        private readonly TradingExpandedCampaignBehavior _campaignBehavior;
        private readonly Action _closeAction;
        
        /// <summary>
        /// Dükkana sahip olup olmadığımız durumu
        /// </summary>
        public bool HasShop => _shop != null;
        
        /// <summary>
        /// Dükkan başlığı
        /// </summary>
        [DataSourceProperty]
        public string ShopTitle
        {
            get
            {
                if (_shop != null)
                {
                    return $"{_currentTown.Name} Toptan Ticaret Dükkanı";
                }
                else
                {
                    return $"Toptan Ticaret Dükkanı Kur - {_currentTown.Name}";
                }
            }
        }
        
        /// <summary>
        /// Dükkan sermayesi
        /// </summary>
        [DataSourceProperty]
        public string Capital
        {
            get
            {
                if (_shop != null)
                {
                    return $"{_shop.Capital} Dinar";
                }
                else
                {
                    return "0 Dinar";
                }
            }
        }
        
        /// <summary>
        /// Günlük kar
        /// </summary>
        [DataSourceProperty]
        public string DailyProfit
        {
            get
            {
                if (_shop != null)
                {
                    return $"{_shop.DailyProfit} Dinar/gün";
                }
                else
                {
                    return "0 Dinar/gün";
                }
            }
        }
        
        /// <summary>
        /// Toplam kar
        /// </summary>
        [DataSourceProperty]
        public string TotalProfit
        {
            get
            {
                if (_shop != null)
                {
                    return $"{_shop.TotalProfit} Dinar";
                }
                else
                {
                    return "0 Dinar";
                }
            }
        }
        
        /// <summary>
        /// Ana buton metni
        /// </summary>
        [DataSourceProperty]
        public string MainActionButtonText
        {
            get
            {
                if (_shop != null)
                {
                    return "Dükkanı Yönet";
                }
                else
                {
                    return "Dükkan Kur (5000 Dinar)";
                }
            }
        }

        /// <summary>
        /// Dükkan kurulu mu
        /// </summary>
        [DataSourceProperty]
        public bool IsShopEstablished => _shop != null;
        
        /// <summary>
        /// Constructor
        /// </summary>
        public WholesaleShopViewModel(Town town, TradingExpandedCampaignBehavior campaignBehavior, Action closeAction)
        {
            _currentTown = town;
            _campaignBehavior = campaignBehavior;
            _closeAction = closeAction;
            
            // Mevcut şehirde bir dükkanımız var mı?
            _shop = _campaignBehavior.GetShopInTown(town);
        }
        
        /// <summary>
        /// Ana buton tıklama olayı
        /// </summary>
        public void ExecuteMainAction()
        {
            if (_shop != null)
            {
                // Dükkan yönetim ekranını aç
                ManageShop();
            }
            else
            {
                // Yeni dükkan kur
                EstablishNewShop();
            }
        }
        
        /// <summary>
        /// Dükkan kurma işlemi
        /// </summary>
        private void EstablishNewShop()
        {
            if (Hero.MainHero.Gold >= 5000)
            {
                Hero.MainHero.ChangeHeroGold(-5000);
                _campaignBehavior.CreateShop(_currentTown, 5000);
                InformationManager.DisplayMessage(new InformationMessage(
                    $"{_currentTown.Name} şehrinde yeni bir toptan ticaret dükkanı kurdunuz!"));
                _closeAction?.Invoke();
            }
            else
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    "Yeterli altınınız bulunmuyor. Dükkan kurmak için 5000 dinara ihtiyacınız var.",
                    Colors.Red));
            }
        }
        
        /// <summary>
        /// Dükkan yönetimi
        /// </summary>
        private void ManageShop()
        {
            InformationManager.DisplayMessage(new InformationMessage(
                "Dükkan yönetim ekranı henüz uygulanmadı."));
            _closeAction?.Invoke();
        }
    }
} 