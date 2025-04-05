using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using TaleWorlds.SaveSystem;
using TradingExpanded.Models;
using TradingExpanded.Helpers;
using TradingExpanded.Repositories;
using TradingExpanded.Services;
using TradingExpanded.UI.ViewModels;
using TradingExpanded.Utils;

namespace TradingExpanded.Behaviors
{
    /// <summary>
    /// Manages the mod's data and game state in the campaign
    /// </summary>
    public class TradingExpandedCampaignBehavior : CampaignBehaviorBase
    {
        // Repositories
        private IRepository<WholesaleShop> _shopRepository;
        private IRepository<TradeCaravan> _caravanRepository;
        private IRepository<Courier> _courierRepository;
        
        // Services
        private IInventoryTrackerService _inventoryTrackerService;
        private readonly ITradingExpandedServiceProvider _serviceProvider;
        
        // Legacy collections - bunları daha sonra tamamen repository'lere taşımak gerekecek
        private Dictionary<string, WholesaleShop> _wholesaleShops;
        private Dictionary<string, TradeCaravan> _caravans;
        private Dictionary<string, Courier> _couriers;
        private Dictionary<string, MerchantRelation> _merchantRelations;
        private InventoryTracker _inventoryTracker;
        
        // Flags
        private bool _isInitialized;
        
        /// <summary>
        /// Constructor - dependency injection ile
        /// </summary>
        public TradingExpandedCampaignBehavior(ITradingExpandedServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            
            // Servisleri al
            if (_serviceProvider.HasService<IRepository<WholesaleShop>>())
                _shopRepository = _serviceProvider.GetService<IRepository<WholesaleShop>>();
            
            if (_serviceProvider.HasService<IRepository<TradeCaravan>>())
                _caravanRepository = _serviceProvider.GetService<IRepository<TradeCaravan>>();
            
            if (_serviceProvider.HasService<IRepository<Courier>>())
                _courierRepository = _serviceProvider.GetService<IRepository<Courier>>();
            
            if (_serviceProvider.HasService<IInventoryTrackerService>())
                _inventoryTrackerService = _serviceProvider.GetService<IInventoryTrackerService>();
            
            // Eski koleksiyonları oluştur (geçiş süreci için)
            _wholesaleShops = new Dictionary<string, WholesaleShop>();
            _caravans = new Dictionary<string, TradeCaravan>();
            _couriers = new Dictionary<string, Courier>();
            _merchantRelations = new Dictionary<string, MerchantRelation>();
            _inventoryTracker = new InventoryTracker();
            _isInitialized = false;
            
            LogManager.Instance.WriteDebug("TradingExpandedCampaignBehavior oluşturuldu");
        }
        
        /// <summary>
        /// Legacy constructor (save/load uyumluluğu için)
        /// </summary>
        public TradingExpandedCampaignBehavior()
        {
            _wholesaleShops = new Dictionary<string, WholesaleShop>();
            _caravans = new Dictionary<string, TradeCaravan>();
            _couriers = new Dictionary<string, Courier>();
            _merchantRelations = new Dictionary<string, MerchantRelation>();
            _inventoryTracker = new InventoryTracker();
            _isInitialized = false;
            
            LogManager.Instance.WriteDebug("TradingExpandedCampaignBehavior oluşturuldu (legacy constructor)");
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
            try
            {
                dataStore.SyncData("_wholesaleShops", ref _wholesaleShops);
                dataStore.SyncData("_caravans", ref _caravans);
                dataStore.SyncData("_couriers", ref _couriers);
                dataStore.SyncData("_merchantRelations", ref _merchantRelations);
                dataStore.SyncData("_inventoryTracker", ref _inventoryTracker);
            }
            catch (Exception ex)
            {
                LogManager.Instance.WriteError("SyncData sırasında hata oluştu", ex);
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

            RegisterWholesaleShopMenus(campaignGameStarter);
        }
        
        /// <summary>
        /// Toptan Satış Dükkanı ile ilgili tüm menüleri kaydeder
        /// </summary>
        private void RegisterWholesaleShopMenus(CampaignGameStarter campaignGameStarter)
        {
            // Ana menüyü oluştur
            campaignGameStarter.AddGameMenu(
                "wholesale_shop_menu", 
                "{=WholesaleShopMenuTitle}Toptan Satış Dükkanı", 
                (MenuCallbackArgs args) => 
                {
                    var shop = GetShopInTown(Settlement.CurrentSettlement.Town);
                    if (shop != null)
                    {
                        args.MenuTitle = new TextObject("{=WholesaleShopMenuTitleWithInfo}Toptan Satış Dükkanı - Sermaye: {CAPITAL}{GOLD_ICON}")
                            .SetTextVariable("CAPITAL", shop.Capital);
                    }
                }
            );

            // Şehir menüsüne Toptan Satış Dükkanı seçeneğini ekle
            AddMainMenuOption(campaignGameStarter);

            // Alt menü seçeneklerini ekle
            AddManageShopOption(campaignGameStarter);
            AddEstablishShopOption(campaignGameStarter);
            AddLeaveMenuOption(campaignGameStarter);
        }
        
        /// <summary>
        /// Ana menüye Toptan Satış Dükkanı seçeneğini ekler
        /// </summary>
        private void AddMainMenuOption(CampaignGameStarter campaignGameStarter)
        {
            campaignGameStarter.AddGameMenuOption(
                "town",
                "wholesale_shop_option",
                "{=WholesaleShopMenuText}Toptan Satış Dükkanı",
                (MenuCallbackArgs args) =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
                    return Settlement.CurrentSettlement?.IsTown ?? false;
                },
                (MenuCallbackArgs args) =>
                {
                    GameMenu.SwitchToMenu("wholesale_shop_menu");
                },
                false,
                -1,
                false,
                "{=WholesaleShopMenuTooltip}Şehirdeki toptan satış dükkanınızı yönetin veya yeni bir dükkan açın."
            );
        }
        
        /// <summary>
        /// Dükkan yönetim seçeneğini ekler
        /// </summary>
        private void AddManageShopOption(CampaignGameStarter campaignGameStarter)
        {
            campaignGameStarter.AddGameMenuOption(
                "wholesale_shop_menu",
                "wholesale_shop_manage",
                "{=WholesaleShopManageText}Dükkanı Yönet",
                (MenuCallbackArgs args) =>
                {
                    var shop = GetShopInTown(Settlement.CurrentSettlement.Town);
                    args.IsEnabled = shop != null;
                    if (!args.IsEnabled)
                        args.Tooltip = new TextObject("{=WholesaleShopNoShop}Bu şehirde henüz bir dükkanınız yok.");
                    return true;
                },
                (MenuCallbackArgs args) =>
                {
                    var shop = GetShopInTown(Settlement.CurrentSettlement.Town);
                    var viewModel = new WholesaleShopViewModel(Settlement.CurrentSettlement.Town, this, () => 
                    {
                        GameMenu.SwitchToMenu("wholesale_shop_menu");
                    });
                    viewModel.ExecuteMainAction();
                },
                false,
                -1,
                false
            );
        }
        
        /// <summary>
        /// Yeni dükkan kurma seçeneğini ekler
        /// </summary>
        private void AddEstablishShopOption(CampaignGameStarter campaignGameStarter)
        {
            campaignGameStarter.AddGameMenuOption(
                "wholesale_shop_menu",
                "wholesale_shop_establish",
                "{=WholesaleShopEstablishText}Yeni Dükkan Kur (5000 Dinar)",
                (MenuCallbackArgs args) =>
                {
                    var shop = GetShopInTown(Settlement.CurrentSettlement.Town);
                    args.IsEnabled = shop == null && Hero.MainHero.Gold >= 5000;
                    if (!args.IsEnabled)
                    {
                        if (shop != null)
                            args.Tooltip = new TextObject("{=WholesaleShopAlreadyExists}Bu şehirde zaten bir dükkanınız var.");
                        else
                            args.Tooltip = new TextObject("{=WholesaleShopNotEnoughGold}Yeni bir dükkan kurmak için 5000 dinara ihtiyacınız var.");
                    }
                    return true;
                },
                (MenuCallbackArgs args) =>
                {
                    var viewModel = new WholesaleShopViewModel(Settlement.CurrentSettlement.Town, this, () => 
                    {
                        GameMenu.SwitchToMenu("wholesale_shop_menu");
                    });
                    viewModel.ExecuteMainAction();
                },
                false,
                -1,
                false
            );
        }
        
        /// <summary>
        /// Geri dönüş seçeneğini ekler
        /// </summary>
        private void AddLeaveMenuOption(CampaignGameStarter campaignGameStarter)
        {
            campaignGameStarter.AddGameMenuOption(
                "wholesale_shop_menu",
                "wholesale_shop_leave",
                "{=WholesaleShopLeaveText}Geri Dön",
                (MenuCallbackArgs args) => { return true; },
                (MenuCallbackArgs args) => { GameMenu.SwitchToMenu("town"); },
                true,
                -1,
                false
            );
        }
        
        /// <summary>
        /// Oyun yüklendiğinde çalışır
        /// </summary>
        private void OnGameLoaded(CampaignGameStarter campaignGameStarter)
        {
            try
            {
                LogManager.Instance.WriteInfo("TradingExpanded: Oyun yüklendi");
                
                // Dictionary koleksiyonları oluştur
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
                
                // PriceHistory null kontrolü
                if (_inventoryTracker.PriceHistory == null)
                    _inventoryTracker.PriceHistory = new Dictionary<Town, Dictionary<ItemObject, PriceHistory>>();
                
                // Başlatılmadıysa başlat
                if (!_isInitialized)
                {
                    Initialize();
                }
            }
            catch (Exception ex)
            {
                LogManager.Instance.WriteError("OnGameLoaded sırasında hata oluştu", ex);
            }
        }
        
        private void OnDailyTick()
        {
            try
        {
            // Update all objects daily
            UpdateWholesaleShops();
            UpdateCaravans();
            UpdateCouriers();
            UpdateMerchantRelations();
            
            // Update analytics less frequently (every 3 days)
            if ((int)CampaignTime.Now.ToDays % 3 == 0)
                {
                    if (_inventoryTrackerService != null)
                    {
                        _inventoryTrackerService.Update();
                    }
                    else
            {
                _inventoryTracker.Update();
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.Instance.WriteError("OnDailyTick sırasında hata oluştu", ex);
            }
        }
        
        private void OnHourlyTick()
        {
            // For more frequent updates if needed
        }
        
        private void OnSettlementEntered(MobileParty mobileParty, Settlement settlement, Hero hero)
        {
            try
        {
            // Handle player entering a settlement
            if (mobileParty == MobileParty.MainParty && settlement.IsTown)
            {
                // Record current prices for analytics
                    if (_inventoryTrackerService != null)
                    {
                        _inventoryTrackerService.RecordCurrentPrices(settlement.Town);
                    }
                    else
                    {
                RecordCurrentPrices(settlement.Town);
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.Instance.WriteError("OnSettlementEntered sırasında hata oluştu", ex);
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
                .Where(courier => !courier.IsDelivered)
                .ToList();
        }
        
        /// <summary>
        /// Kurye sisteminin hazır olup olmadığını kontrol eder
        /// </summary>
        public bool IsCourierSystemReady()
        {
            // Kurye sistemi her zaman hazır
            return true;
        }
        
        /// <summary>
        /// Şu an aktif olan kurye sayısını döndürür
        /// </summary>
        public int GetActiveCourierCount()
        {
            try
            {
                // Repository kullanılıyorsa
                if (_courierRepository != null)
                {
                    return _courierRepository.GetAll().Count(c => !c.IsDelivered);
                }
                
                // Eski koleksiyon kullanılıyorsa
                return _couriers.Values.Count(c => !c.IsDelivered);
            }
            catch (Exception ex)
            {
                LogManager.Instance.WriteError("Aktif kurye sayısı alınırken hata", ex);
                return 0;
            }
        }
        
        /// <summary>
        /// Yeni bir kurye oluşturur ve gönderir
        /// </summary>
        /// <param name="originTown">Başlangıç şehri</param>
        /// <param name="destinationTown">Hedef şehri</param>
        /// <param name="cost">Kurye maliyeti</param>
        /// <returns>İşlem başarılı olursa true, aksi halde false</returns>
        public bool CreateAndSendCourier(Town originTown, Town destinationTown, int cost)
        {
            try
            {
                if (originTown == null || destinationTown == null)
                {
                    LogManager.Instance.WriteError("Kurye oluşturma: Başlangıç veya hedef şehir null.");
                    return false;
                }
                
                LogManager.Instance.WriteDebug($"Kurye gönderilecek: {originTown.Name} -> {destinationTown.Name}, maliyet: {cost}");
                
                // Varış zamanını hesapla
                CampaignTime arrivalTime = CalculateCourierArrivalTime(originTown, destinationTown);
                
                // Kargo oluştur (sadece göstermelik)
                CargoItem cargo = CargoItem.CreateMoneyCargo(0);
                
                // Kurye oluştur - yeni constructor'ı kullan
                Courier courier = new Courier(originTown.Settlement, destinationTown.Settlement, cargo, cost, arrivalTime);
                
                // Kurye partisini oluştur
                courier.CreatePartyOnMap();
                
                // Repository güncelle
                if (_courierRepository != null)
                {
                    _courierRepository.AddOrUpdate(courier);
                }
                else
                {
                    // Eski koleksiyona ekle
                    _couriers[courier.Id] = courier;
                }
                
                return true;
            }
            catch (Exception ex)
            {
                LogManager.Instance.WriteError("Kurye oluşturma ve gönderme sırasında hata", ex);
                return false;
            }
        }
        
        /// <summary>
        /// İki şehir arasındaki kurye seyahat süresini hesaplar
        /// </summary>
        public CampaignTime CalculateCourierArrivalTime(Town originTown, Town destinationTown)
        {
            try
            {
                if (originTown == null || destinationTown == null)
                    return CampaignTime.DaysFromNow(3); // Varsayılan
                
                // Mesafeyi hesapla
                float distance = Campaign.Current.Models.MapDistanceModel.GetDistance(originTown.Settlement, destinationTown.Settlement);
                
                // Hız çarpanı ayarlardan al
                float speedMultiplier = Settings.Instance?.CourierTravelSpeedMultiplier ?? 1.0f;
                
                // 100 birim mesafeyi geçmek için yaklaşık 1 gün (24 saat) 
                // ve varış için ekstra 12 saat veri toplama süresi
                float travelHours = (distance / 100f) * 24f / speedMultiplier;
                float gatheringHours = 12f;
                float totalHours = travelHours + gatheringHours + travelHours; // Gidiş + Veri Toplama + Dönüş
                
                // Risk faktörü ile ekstra zaman
                float riskFactor = Settings.Instance?.CourierRiskFactor ?? 0.1f;
                totalHours += totalHours * riskFactor; // Risk için ekstra süre
                
                return CampaignTime.HoursFromNow((int)totalHours);
            }
            catch (Exception ex)
            {
                LogManager.Instance.WriteError("Kurye varış zamanı hesaplanırken hata", ex);
                return CampaignTime.DaysFromNow(3); // Hata durumunda varsayılan
            }
        }
        
        /// <summary>
        /// Kuryeleri günceller
        /// </summary>
        private void UpdateCouriers()
        {
            try
            {
                List<Courier> couriers;
                
                // Repository kullanılıyorsa
                if (_courierRepository != null)
                {
                    couriers = _courierRepository.GetAll().ToList();
                }
                else
                {
                    // Eski koleksiyon
                    couriers = _couriers.Values.ToList();
                }
                
                foreach (var courier in couriers)
                {
                    // Sadece aktif kuryeleri güncelle
                    if (!courier.IsDelivered)
                    {
                        // Kurye varış durumunu kontrol et
                        bool hasArrived = courier.CheckArrival();
                        
                        // Eğer kurye yeni varmışsa
                        if (hasArrived && courier.IsDelivered)
                        {
                            // Risk faktörü hesapla - bir şansla kurye kaybolabilir
                            float riskFactor = Settings.Instance?.CourierRiskFactor ?? 0.1f;
                            float randomValue = MBRandom.RandomFloat;
                            
                            // Kurye kayboldu mu?
                            if (randomValue < riskFactor)
                            {
                                LogManager.Instance.WriteDebug($"Kurye kayboldu: {courier.Origin.Name} -> {courier.Destination.Name}");
                                
                                // Kayıp bildirimi
                                InformationManager.DisplayMessage(new InformationMessage(
                                    new TextObject("{=CourierLostMessage}{DESTINATION_TOWN} şehrine gönderdiğiniz ulak kayboldu. Bilgiler alınamadı.")
                                        .SetTextVariable("DESTINATION_TOWN", courier.Destination.Name)
                                        .ToString(),
                                    Colors.Red));
                            }
                            else
                            {
                                // Varış bildirimi
                                InformationManager.DisplayMessage(new InformationMessage(
                                    new TextObject("{=CourierReturnedMessage}{DESTINATION_TOWN} şehrine gönderdiğiniz ulak görevini tamamladı ve bilgileri getirdi.")
                                        .SetTextVariable("DESTINATION_TOWN", courier.Destination.Name)
                                        .ToString(),
                                    Colors.Green));
                                
                                // Bilgileri kaydet
                                CollectPriceData(courier);
                            }
                            
                            // Repository güncelle
                            if (_courierRepository != null)
                            {
                                _courierRepository.AddOrUpdate(courier);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.Instance.WriteError("Kuryeler güncellenirken hata", ex);
            }
        }
        
        /// <summary>
        /// Kurye tarafından toplanan fiyat verilerini işler
        /// </summary>
        private void CollectPriceData(Courier courier)
        {
            try
            {
                if (courier == null || courier.Destination == null)
                    return;
                    
                LogManager.Instance.WriteDebug($"Kurye fiyat verisi topluyor: {courier.Destination.Name}");
                
                // Hedef şehire ait Town nesnesini bul
                Town town = null;
                if (courier.Destination.IsTown)
                {
                    town = courier.Destination.Town;
                }
                
                if (town == null)
                {
                    LogManager.Instance.WriteError("Hedef yerleşim geçerli bir şehir değil.");
                    return;
                }
                
                // Kuryenin topladığı bilgileri tutacak dictionary
                var priceInfo = new Dictionary<ItemObject, int>();
                
                // Sadece ticaret mallarını ele al
                foreach (var item in MBObjectManager.Instance.GetObjectTypeList<ItemObject>())
                {
                    if (item.IsTradeGood)
                    {
                        // Şehirdeki şu anki fiyatı al
                        int price = town.GetItemPrice(item);
                        if (price > 0)
                        {
                            // Fiyatı kaydet
                            priceInfo[item] = price;
                            
                            // InventoryTracker'a da kaydet
                            _inventoryTracker.RecordPriceHistory(item, town, price);
                            
                            // Repository kullanılıyorsa servis üzerinden kaydet
                            if (_inventoryTrackerService != null)
                            {
                                _inventoryTrackerService.RecordCurrentPrices(town);
                            }
                        }
                    }
                }
                
                // Artık Courier sınıfında PriceInfo yok, sadece bildirim yapalım
                if (courier.IsDelivered && courier.Cargo != null)
                {
                    // Fiyat bilgilerini toplama işlemi tamamlandı bildirimi
                    InformationManager.DisplayMessage(new InformationMessage(
                        new TextObject("{=CourierPriceCollectedMessage}{TOWN_NAME} şehrine ait fiyat bilgileri kaydedildi. Toplam {COUNT} ticaret malı fiyatı güncellendi.")
                            .SetTextVariable("TOWN_NAME", town.Name)
                            .SetTextVariable("COUNT", priceInfo.Count)
                            .ToString(),
                        Colors.Green));
                    
                    // İlginç fiyat farkları varsa bildir
                    FindAndReportInterestingPrices(town, priceInfo);
                }
            }
            catch (Exception ex)
            {
                LogManager.Instance.WriteError("Fiyat verileri toplanırken hata", ex);
            }
        }
        
        /// <summary>
        /// İlginç fiyat farklarını bulur ve rapor eder
        /// </summary>
        private void FindAndReportInterestingPrices(Town town, Dictionary<ItemObject, int> priceInfo)
        {
            try
            {
                // Bulunduğumuz şehir
                Town currentTown = Settlement.CurrentSettlement?.Town;
                if (currentTown == null || priceInfo == null || priceInfo.Count == 0)
                    return;
                    
                // Kârlı olabilecek alım/satım fırsatlarını bul
                List<string> profitableItems = new List<string>();
                
                foreach (var itemPrice in priceInfo)
                {
                    ItemObject item = itemPrice.Key;
                    int remotePrice = itemPrice.Value;
                    int localPrice = currentTown.GetItemPrice(item);
                    
                    // Fiyat farkı yüzde 20'den fazla mı?
                    if (localPrice > 0 && remotePrice > 0)
                    {
                        float priceDiff = Math.Abs(localPrice - remotePrice) / (float)Math.Min(localPrice, remotePrice);
                        
                        if (priceDiff >= 0.2f) // %20 veya daha fazla fark
                        {
                            if (localPrice < remotePrice) // Yerel fiyat daha ucuz, satmak kârlı
                            {
                                int profit = remotePrice - localPrice;
                                profitableItems.Add(new TextObject("{=ProfitableSellItem}{ITEM_NAME}: Burada {LOCAL_PRICE}{GOLD_ICON}, {TOWN_NAME}'da {REMOTE_PRICE}{GOLD_ICON} → Kâr: {PROFIT}{GOLD_ICON}")
                                    .SetTextVariable("ITEM_NAME", item.Name)
                                    .SetTextVariable("LOCAL_PRICE", localPrice)
                                    .SetTextVariable("REMOTE_PRICE", remotePrice)
                                    .SetTextVariable("TOWN_NAME", town.Name)
                                    .SetTextVariable("PROFIT", profit)
                                    .ToString());
                            }
                            else // Uzak şehirde daha ucuz, oradan alıp burada satmak kârlı
                            {
                                int profit = localPrice - remotePrice;
                                profitableItems.Add(new TextObject("{=ProfitableBuyItem}{ITEM_NAME}: {TOWN_NAME}'da {REMOTE_PRICE}{GOLD_ICON}, burada {LOCAL_PRICE}{GOLD_ICON} → Kâr: {PROFIT}{GOLD_ICON}")
                                    .SetTextVariable("ITEM_NAME", item.Name)
                                    .SetTextVariable("LOCAL_PRICE", localPrice)
                                    .SetTextVariable("REMOTE_PRICE", remotePrice)
                                    .SetTextVariable("TOWN_NAME", town.Name)
                                    .SetTextVariable("PROFIT", profit)
                                    .ToString());
                            }
                        }
                    }
                }
                
                // Kârlı fırsatları rapor et
                if (profitableItems.Count > 0)
                {
                    // En fazla 5 tane göster
                    int count = Math.Min(profitableItems.Count, 5);
                    
                    InformationManager.DisplayMessage(new InformationMessage(
                        new TextObject("{=ProfitableItemsFound}Kârlı ticaret fırsatları bulundu:").ToString(),
                        Colors.Green));
                        
                    for (int i = 0; i < count; i++)
                    {
                        InformationManager.DisplayMessage(new InformationMessage(profitableItems[i], Colors.Green));
                    }
                    
                    if (profitableItems.Count > count)
                    {
                        InformationManager.DisplayMessage(new InformationMessage(
                            new TextObject("{=MoreProfitableItems}...ve {COUNT} ticaret fırsatı daha.")
                                .SetTextVariable("COUNT", profitableItems.Count - count)
                                .ToString(),
                            Colors.Green));
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.Instance.WriteError("İlginç fiyatlar raporlanırken hata", ex);
            }
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
        /// Şehirdeki mevcut fiyatları kaydeder
        /// </summary>
        public void RecordCurrentPrices(Town town)
        {
            if (town == null)
                return;
                
            foreach (var item in MBObjectManager.Instance.GetObjectTypeList<ItemObject>())
            {
                if (item.IsTradeGood)
                {
                    // Şu anki fiyatı al
                int price = town.GetItemPrice(item);
                    if (price > 0)
                    {
                        // Record price history
                        _inventoryTracker.RecordPriceHistory(item, town, price);
                    }
                }
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