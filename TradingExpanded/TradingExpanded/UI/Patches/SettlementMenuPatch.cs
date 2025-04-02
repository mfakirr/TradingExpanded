/*
 * NOT: Bannerlord 1.2.9 veya daha yeni sürümlerde bu patch güncellenmiştir.
 * 
 * Orijinal sorun: Mount & Blade II: Bannerlord'un son sürümünde SettlementMenuVM sınıfının
 * namespace'i veya adı değiştirilmişti.
 * 
 * Çözüm: 
 * Bannerlord 1.2.9+ sürümleri için doğru sınıf adı ve namespace:
 * - Sınıf: SettlementMenuOverlayVM
 * - Namespace: TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.Overlay
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.Overlay;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Core.ViewModelCollection.Generic;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TradingExpanded.Models;
using TradingExpanded.Behaviors;
using TradingExpanded.UI.ViewModels;
using HarmonyLib;
using Debug = TaleWorlds.Library.Debug;

namespace TradingExpanded.UI.Patches
{
    /// <summary>
    /// Şehir menüsüne Toptan Satış Dükkanı butonu eklemek için patch
    /// </summary>
    [HarmonyPatch]
    public class SettlementMenuPatch
    {
        private static TradingExpandedCampaignBehavior _campaignBehavior;
        private static bool _buttonAdded = false;
        
        /// <summary>
        /// Session başlatıldığında çalışacak metot
        /// </summary>
        [HarmonyPatch(typeof(CampaignGameStarter), "OnGameStart")]
        public static void Prefix_OnGameStart()
        {
            try
            {
                _buttonAdded = false;
                InformationManager.DisplayMessage(new InformationMessage(
                    "Toptan Satış Dükkanı menüsü hazırlanıyor...", Colors.Green));
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    $"Menü hazırlanırken hata: {ex.Message}", Colors.Red));
            }
        }
        
        /// <summary>
        /// Session sonlandığında çalışacak metot
        /// </summary>
        [HarmonyPatch(typeof(CampaignGameStarter), "OnGameEnd")]
        public static void Prefix_OnGameEnd()
        {
            _buttonAdded = false;
            _campaignBehavior = null;
        }

        /// <summary>
        /// RefreshValues metodu çalıştıktan sonra çalışacak postfix metodu
        /// </summary>
        [HarmonyPatch(typeof(SettlementMenuOverlayVM), "RefreshValues")]
        public static void Postfix(SettlementMenuOverlayVM __instance)
        {
            try
            {
                // Sadece oyuncu bir şehirdeyse işlem yap
                if (Settlement.CurrentSettlement == null || !Settlement.CurrentSettlement.IsTown)
                    return;
                
                // Campaign behavior'ı al
                if (_campaignBehavior == null)
                {
                    _campaignBehavior = Campaign.Current?.GetCampaignBehavior<TradingExpandedCampaignBehavior>();
                    if (_campaignBehavior == null)
                    {
                        InformationManager.DisplayMessage(new InformationMessage(
                            "TradingExpandedCampaignBehavior bulunamadı!", Colors.Red));
                        return;
                    }
                }
                
                // Buton zaten eklenmişse ve hala görünüyorsa tekrar eklemeye çalışma
                if (_buttonAdded)
                {
                    // Butonun hala mevcut olup olmadığını kontrol et
                    bool buttonExists = CheckIfButtonExists(__instance);
                    if (buttonExists)
                        return;
                    else
                        _buttonAdded = false; // Buton kaybolmuşsa yeniden ekle
                }
                
                // Şehir menüsüne Toptan Satış Dükkanı butonu ekle
                try
                {
                    InformationManager.DisplayMessage(new InformationMessage(
                        $"Şehir menüsüne Toptan Satış Dükkanı butonu ekleniyor...", Colors.Green));
                    
                    _buttonAdded = AddWholesaleShopButton(__instance);
                    
                    if (_buttonAdded)
                    {
                        InformationManager.DisplayMessage(new InformationMessage(
                            "Toptan Satış Dükkanı butonu başarıyla eklendi!", Colors.Green));
                    }
                    else
                    {
                        InformationManager.DisplayMessage(new InformationMessage(
                            "Buton eklenemedi. Butonu eklemek için uygun yer bulunamadı.", Colors.Red));
                    }
                }
                catch (Exception ex)
                {
                    InformationManager.DisplayMessage(new InformationMessage(
                        $"Buton eklenirken hata: {ex.Message}", Colors.Red));
                }
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage($"Patch hatası: {ex.Message}"));
            }
        }
        
        /// <summary>
        /// Butonun hala menüde var olup olmadığını kontrol eder
        /// </summary>
        private static bool CheckIfButtonExists(SettlementMenuOverlayVM menuVM)
        {
            try
            {
                // MainMenu, MenuItems ve IssueList'i kontrol et
                var properties = new[] { "MainMenu", "MenuItems", "IssueList" };
                
                foreach (var propName in properties)
                {
                    var property = menuVM.GetType().GetProperty(propName);
                    if (property != null)
                    {
                        object value = property.GetValue(menuVM);
                        if (value != null)
                        {
                            if (ButtonAlreadyExists(value, "WholesaleShop"))
                                return true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    $"Buton kontrolü sırasında hata: {ex.Message}", Colors.Red));
            }
            
            return false;
        }
        
        /// <summary>
        /// Şehir menüsüne Toptan Satış Dükkanı butonu ekler
        /// </summary>
        private static bool AddWholesaleShopButton(SettlementMenuOverlayVM menuVM)
        {
            try
            {
                // 1. MainMenu alanını arıyoruz (Bannerlord son sürümleri için)
                var mainMenuProperty = menuVM.GetType().GetProperty("MainMenu");
                if (mainMenuProperty != null)
                {
                    object mainMenu = mainMenuProperty.GetValue(menuVM);
                    if (mainMenu != null)
                    {
                        InformationManager.DisplayMessage(new InformationMessage(
                            $"MainMenu alanı bulundu, türü: {mainMenu.GetType().Name}", Colors.Green));
                        
                        // MainMenu'deki SubMenus veya Children özellikleri
                        var subMenusOrItems = TryGetItems(mainMenu);
                        if (subMenusOrItems != null)
                        {
                            InformationManager.DisplayMessage(new InformationMessage(
                                $"MainMenu'de {subMenusOrItems.GetType().Name} türünde liste bulundu", Colors.Green));
                            
                            // Buton ekle
                            if (TryAddButtonToList(subMenusOrItems, menuVM, "MainMenu"))
                                return true;
                        }
                    }
                }
                
                // 2. Settlement Menu Items
                var menuItemsProperty = menuVM.GetType().GetProperty("MenuItems");
                if (menuItemsProperty != null)
                {
                    object menuItems = menuItemsProperty.GetValue(menuVM);
                    if (menuItems != null)
                    {
                        InformationManager.DisplayMessage(new InformationMessage(
                            $"MenuItems alanı bulundu, türü: {menuItems.GetType().Name}", Colors.Green));
                        
                        // Buton ekle
                        if (TryAddButtonToList(menuItems, menuVM, "MenuItems"))
                            return true;
                    }
                }
                
                // 3. IssueList (daha yaygın kullanılan)
                var issueListProperty = menuVM.GetType().GetProperty("IssueList");
                if (issueListProperty != null)
                {
                    object issueList = issueListProperty.GetValue(menuVM);
                    if (issueList != null)
                    {
                        InformationManager.DisplayMessage(new InformationMessage(
                            $"IssueList alanı bulundu, türü: {issueList.GetType().Name}", Colors.Green));
                        
                        // Mevcut butonları listele
                        ListExistingButtons(issueList);
                        
                        // Buton ekle
                        if (TryAddButtonToList(issueList, menuVM, "IssueList"))
                            return true;
                    }
                }
                
                // 4. CategoryList (daha eski sürümlerde)
                var categoryListProperty = menuVM.GetType().GetProperty("CategoryList");
                if (categoryListProperty != null)
                {
                    object categoryList = categoryListProperty.GetValue(menuVM);
                    if (categoryList != null)
                    {
                        InformationManager.DisplayMessage(new InformationMessage(
                            $"CategoryList alanı bulundu, türü: {categoryList.GetType().Name}", Colors.Green));
                        
                        // Category ile buton ekleme
                        if (TryAddButtonToCategoryList(categoryList, menuVM))
                            return true;
                    }
                }
                
                // 5. Son çare: Tüm özellikleri tarayarak bir liste bulmaya çalış
                var properties = menuVM.GetType().GetProperties();
                foreach (var prop in properties)
                {
                    if (prop.Name != "IssueList" && prop.Name != "MenuItems" && prop.Name != "MainMenu" && prop.Name != "CategoryList")
                    {
                        try
                        {
                            object propValue = prop.GetValue(menuVM);
                            if (propValue != null && 
                                (propValue.GetType().IsGenericType || 
                                 propValue.GetType().GetMethod("Add") != null))
                            {
                                InformationManager.DisplayMessage(new InformationMessage(
                                    $"Potansiyel liste alanı bulundu: {prop.Name}, türü: {propValue.GetType().Name}", Colors.Yellow));
                                
                                if (TryAddButtonToList(propValue, menuVM, prop.Name))
                                    return true;
                            }
                        }
                        catch { /* Ignore errors in reflection */ }
                    }
                }
                
                return false;
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    $"Buton eklenirken genel hata: {ex.Message}", Colors.Red));
                return false;
            }
        }
        
        /// <summary>
        /// Menü öğesinden alt öğeleri almaya çalışır
        /// </summary>
        private static object TryGetItems(object menu)
        {
            try
            {
                // Olası özellik adları
                string[] propertyNames = { "Items", "Children", "SubMenus", "MenuItems", "Elements" };
                
                foreach (var propName in propertyNames)
                {
                    var property = menu.GetType().GetProperty(propName);
                    if (property != null)
                    {
                        var value = property.GetValue(menu);
                        if (value != null)
                        {
                            InformationManager.DisplayMessage(new InformationMessage(
                                $"{propName} özelliği bulundu: {value.GetType().Name}", Colors.Green));
                            return value;
                        }
                    }
                }
                
                // Method ile erişmeyi dene
                string[] methodNames = { "GetItems", "GetChildren", "GetSubMenus", "GetMenuItems" };
                
                foreach (var methodName in methodNames)
                {
                    var method = menu.GetType().GetMethod(methodName, Type.EmptyTypes);
                    if (method != null)
                    {
                        var value = method.Invoke(menu, null);
                        if (value != null)
                        {
                            InformationManager.DisplayMessage(new InformationMessage(
                                $"{methodName} metodu ile liste alındı: {value.GetType().Name}", Colors.Green));
                            return value;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    $"Alt öğeler alınırken hata: {ex.Message}", Colors.Red));
            }
            
            return null;
        }
        
        /// <summary>
        /// Kategori listesine buton eklemeyi dener
        /// </summary>
        private static bool TryAddButtonToCategoryList(object categoryList, SettlementMenuOverlayVM menuVM)
        {
            try
            {
                // Kategori listesindeki ilk kategoriyi bulma
                var categories = categoryList as System.Collections.IEnumerable;
                if (categories != null)
                {
                    foreach (var category in categories)
                    {
                        string categoryName = "Unknown";
                        try
                        {
                            var nameProperty = category.GetType().GetProperty("Name");
                            if (nameProperty != null)
                                categoryName = nameProperty.GetValue(category)?.ToString() ?? "null";
                        }
                        catch { /* Ignore name errors */ }
                        
                        InformationManager.DisplayMessage(new InformationMessage(
                            $"Kategori bulundu: {categoryName}", Colors.Green));
                        
                        // Kategorideki alt öğeleri bul
                        var categoryItems = TryGetItems(category);
                        if (categoryItems != null)
                        {
                            // Buton ekle
                            if (TryAddButtonToList(categoryItems, menuVM, $"Category[{categoryName}]"))
                                return true;
                        }
                    }
                }
                
                return false;
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    $"Kategori listesine buton eklenirken hata: {ex.Message}", Colors.Red));
                return false;
            }
        }
        
        /// <summary>
        /// Bir listeye buton eklemeyi dener
        /// </summary>
        private static bool TryAddButtonToList(object list, SettlementMenuOverlayVM menuVM, string listName)
        {
            try
            {
                // Add metodu var mı kontrol et
                var addMethod = list.GetType().GetMethod("Add", new Type[] { typeof(object) })
                    ?? list.GetType().GetMethod("Add");
                
                if (addMethod == null)
                {
                    InformationManager.DisplayMessage(new InformationMessage(
                        $"{listName} listesinde Add metodu bulunamadı", Colors.Red));
                    return false;
                }
                
                // Bu listedeki eleman tipini belirle
                Type itemType = null;
                
                // Eğer MBBindingList ise, generic tipini bul
                if (list.GetType().IsGenericType && list.GetType().GetGenericTypeDefinition() == typeof(MBBindingList<>))
                {
                    itemType = list.GetType().GetGenericArguments()[0];
                }
                else
                {
                    // Eğer MBBindingList değilse, Add metodunun parametresinden tipini belirle
                    var parameters = addMethod.GetParameters();
                    if (parameters.Length > 0)
                    {
                        itemType = parameters[0].ParameterType;
                    }
                }
                
                if (itemType == null)
                {
                    InformationManager.DisplayMessage(new InformationMessage(
                        $"{listName} listesinin eleman tipi belirlenemedi", Colors.Red));
                    return false;
                }
                
                InformationManager.DisplayMessage(new InformationMessage(
                    $"{listName} listesi için eleman tipi: {itemType.Name}", Colors.Green));
                
                // İlk önce bu listedeki butonlardan birinin id'sini kontrol edelim  
                // Eğer zaten bu id'de bir buton varsa, eklemeyelim
                if (ButtonAlreadyExists(list, "WholesaleShop"))
                {
                    InformationManager.DisplayMessage(new InformationMessage(
                        "Toptan Satış Dükkanı butonu zaten eklenmiş.", Colors.Yellow));
                    return true;
                }
                
                // Buton oluştur
                object button = CreateButton(itemType, menuVM);
                if (button == null)
                {
                    InformationManager.DisplayMessage(new InformationMessage(
                        $"{itemType.Name} tipinde buton oluşturulamadı", Colors.Red));
                    return false;
                }
                
                // Buton oluşturuldu, listeye ekle
                addMethod.Invoke(list, new object[] { button });
                
                InformationManager.DisplayMessage(new InformationMessage(
                    $"Toptan Satış Dükkanı butonu {listName} listesine eklendi!", Colors.Green));
                return true;
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    $"{listName} listesine buton eklenirken hata: {ex.Message}", Colors.Red));
                return false;
            }
        }
        
        /// <summary>
        /// Verilen ID'de bir butonun zaten eklenmiş olup olmadığını kontrol eder
        /// </summary>
        private static bool ButtonAlreadyExists(object list, string buttonId)
        {
            try
            {
                // Liste içindeki her öğeyi denetle
                var enumerable = list as System.Collections.IEnumerable;
                if (enumerable != null)
                {
                    foreach (var item in enumerable)
                    {
                        try
                        {
                            // ID özelliğini kontrol et
                            var idProperty = item.GetType().GetProperty("StringId") 
                                ?? item.GetType().GetProperty("Id") 
                                ?? item.GetType().GetProperty("Name");
                                
                            if (idProperty != null)
                            {
                                string id = idProperty.GetValue(item)?.ToString();
                                if (buttonId.Equals(id, StringComparison.OrdinalIgnoreCase))
                                {
                                    return true;
                                }
                            }
                        }
                        catch { /* Ignore reflection errors for individual items */ }
                    }
                }
            }
            catch { /* Ignore enumeration errors */ }
            
            return false;
        }
        
        /// <summary>
        /// Mevcut butonları listeler
        /// </summary>
        private static void ListExistingButtons(object list)
        {
            try
            {
                var enumerable = list as System.Collections.IEnumerable;
                if (enumerable != null)
                {
                    InformationManager.DisplayMessage(new InformationMessage(
                        "Mevcut menü öğeleri:", Colors.Yellow));
                    
                    int count = 0;
                    foreach (var item in enumerable)
                    {
                        try
                        {
                            // ID ve Text özelliklerini kontrol et
                            var idProperty = item.GetType().GetProperty("StringId") 
                                ?? item.GetType().GetProperty("Id") 
                                ?? item.GetType().GetProperty("Name");
                                
                            var textProperty = item.GetType().GetProperty("Text") 
                                ?? item.GetType().GetProperty("Caption") 
                                ?? item.GetType().GetProperty("Label");
                            
                            string id = idProperty?.GetValue(item)?.ToString() ?? "?";
                            string text = textProperty?.GetValue(item)?.ToString() ?? "?";
                            
                            InformationManager.DisplayMessage(new InformationMessage(
                                $"Öğe {++count}: ID={id}, Text={text}", Colors.Yellow));
                        }
                        catch { /* Ignore reflection errors for individual items */ }
                    }
                    
                    if (count == 0)
                    {
                        InformationManager.DisplayMessage(new InformationMessage(
                            "Liste boş", Colors.Yellow));
                    }
                }
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    $"Mevcut butonlar listelenirken hata: {ex.Message}", Colors.Red));
            }
        }
        
        /// <summary>
        /// Toptan Satış Dükkanı butonu oluşturur
        /// </summary>
        private static object CreateButton(Type buttonType, SettlementMenuOverlayVM menuVM)
        {
            try
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    $"{buttonType.Name} tipinde buton oluşturuluyor...", Colors.Green));
                
                // Butonu oluşturmak için reflection kullan
                var constructors = buttonType.GetConstructors();
                if (constructors.Length == 0)
                {
                    InformationManager.DisplayMessage(new InformationMessage(
                        $"{buttonType.Name} için constructor bulunamadı.", Colors.Red));
                    return null;
                }
                
                // Constructor'ların parametrelerini loglayalım
                int idx = 0;
                foreach (var ctor in constructors.OrderByDescending(c => c.GetParameters().Length))
                {
                    var ctorParams = ctor.GetParameters();
                    InformationManager.DisplayMessage(new InformationMessage(
                        $"Constructor {++idx}: Parametre sayısı={ctorParams.Length}", Colors.Yellow));
                    
                    for (int i = 0; i < ctorParams.Length; i++)
                    {
                        InformationManager.DisplayMessage(new InformationMessage(
                            $"   Parametre {i+1}: {ctorParams[i].Name} ({ctorParams[i].ParameterType.Name})", Colors.Yellow));
                    }
                }
                
                // En çok parametreli constructor'ı bul (genellikle en kapsamlı olanıdır)
                var constructor = constructors.OrderByDescending(c => c.GetParameters().Length).First();
                var parameters = constructor.GetParameters();
                var paramValues = new object[parameters.Length];
                
                // Her parametre için değer ata
                for (int i = 0; i < parameters.Length; i++)
                {
                    var param = parameters[i];
                    string paramName = param.Name.ToLower();
                    Type paramType = param.ParameterType;
                    
                    // Parametrenin tipine ve ismine göre değer ata
                    if (paramType == typeof(string) && (paramName.Contains("id") || paramName.Contains("stringid")))
                    {
                        paramValues[i] = "WholesaleShop";
                    }
                    else if (paramType == typeof(string) && paramName.Contains("text"))
                    {
                        paramValues[i] = new TextObject("Toptan Satış Dükkanı").ToString();
                    }
                    else if (paramType == typeof(bool) && paramName.Contains("enabled"))
                    {
                        paramValues[i] = true;
                    }
                    else if (paramType == typeof(Action))
                    {
                        paramValues[i] = new Action(OnWholesaleShopButtonClicked);
                    }
                    else if (paramType == typeof(int) && (paramName.Contains("order") || paramName.Contains("visual")))
                    {
                        paramValues[i] = 99;
                    }
                    else if (paramType == typeof(string) && paramName.Contains("hint"))
                    {
                        paramValues[i] = new TextObject("Şehirdeki toptan satış dükkanınızı yönetin veya yeni bir dükkan açın").ToString();
                    }
                    else if (paramType == typeof(string) && (paramName.Contains("image") || paramName.Contains("icon")))
                    {
                        paramValues[i] = "";
                    }
                    else if (paramType == typeof(TextObject))
                    {
                        // String parametre gibi, ismine göre değer ata
                        if (paramName.Contains("text"))
                        {
                            paramValues[i] = new TextObject("Toptan Satış Dükkanı");
                        }
                        else if (paramName.Contains("hint"))
                        {
                            paramValues[i] = new TextObject("Şehirdeki toptan satış dükkanınızı yönetin veya yeni bir dükkan açın");
                        }
                        else
                        {
                            paramValues[i] = new TextObject("");
                        }
                    }
                    // MBBindingList türünde parametre - boş liste ver
                    else if (paramType.IsGenericType && paramType.GetGenericTypeDefinition() == typeof(MBBindingList<>))
                    {
                        Type elementType = paramType.GetGenericArguments()[0];
                        var listType = typeof(MBBindingList<>).MakeGenericType(elementType);
                        paramValues[i] = Activator.CreateInstance(listType);
                    }
                    else if (paramType.IsValueType)
                    {
                        paramValues[i] = Activator.CreateInstance(paramType);
                    }
                    else
                    {
                        paramValues[i] = null;
                    }
                }
                
                // Constructor'ı çağır
                object button = constructor.Invoke(paramValues);
                return button;
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    $"Buton oluşturulurken hata: {ex.Message}\n{ex.StackTrace}", Colors.Red));
                return null;
            }
        }
        
        /// <summary>
        /// Toptan Satış Dükkanı butonu tıklandığında
        /// </summary>
        private static void OnWholesaleShopButtonClicked()
        {
            try
            {
                // Şehir kontrolü
                if (Settlement.CurrentSettlement == null || !Settlement.CurrentSettlement.IsTown)
                {
                    InformationManager.DisplayMessage(new InformationMessage(
                        "Bu özellik sadece şehirlerde kullanılabilir.", Colors.Red));
                    return;
                }
                
                InformationManager.DisplayMessage(new InformationMessage(
                    "Toptan Satış Dükkanı butonu tıklandı!", Colors.Green));
                    
                // Şehir bilgilerini al
                Town currentTown = Settlement.CurrentSettlement.Town;
                
                // Dükkan durumunu kontrol et
                bool hasShop = _campaignBehavior.GetShopInTown(currentTown) != null;
                
                // Duruma göre menü göster
                ShowWholesaleShopMenu(currentTown, hasShop);
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    $"Toptan Satış Dükkanı butonu tıklandığında hata: {ex.Message}", Colors.Red));
            }
        }
        
        /// <summary>
        /// Toptan Satış Dükkanı menüsünü göster
        /// </summary>
        private static void ShowWholesaleShopMenu(Town town, bool hasShop)
        {
            try
            {
                if (hasShop)
                {
                    // Mevcut dükkanı yönetmek için seçenekler
                    List<InquiryElement> options = new List<InquiryElement>();
                    
                    // Dükkan bilgilerini göster seçeneği
                    options.Add(new InquiryElement(
                        1,
                        "{=WholesaleShopInfoOption}Dükkan Bilgileri",
                        null,
                        true,
                        "{=WholesaleShopInfoDesc}Toptan satış dükkanınızın bilgilerini görüntüler."
                    ));
                    
                    // Sermaye yatır seçeneği
                    options.Add(new InquiryElement(
                        2,
                        "{=WholesaleShopInvestOption}Sermaye Yatır",
                        null,
                        true,
                        "{=WholesaleShopInvestDesc}Dükkanınıza sermaye yatırarak daha fazla kazanç elde edin."
                    ));
                    
                    // Sermaye çek seçeneği
                    options.Add(new InquiryElement(
                        3,
                        "{=WholesaleShopWithdrawOption}Sermaye Çek",
                        null,
                        true,
                        "{=WholesaleShopWithdrawDesc}Dükkanınızın sermayesinden para çekin."
                    ));
                    
                    // Dükkanı kapat seçeneği
                    options.Add(new InquiryElement(
                        4,
                        "{=WholesaleShopCloseOption}Dükkanı Kapat",
                        null,
                        true,
                        "{=WholesaleShopCloseDesc}Dükkanınızı kapatıp sermayenizi geri alın."
                    ));
                    
                    // Menüyü göster
                    MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(
                        "{=WholesaleShopManageTitle}Toptan Satış Dükkanı Yönetimi",
                        new TextObject("{=WholesaleShopManageDesc}{TOWN} şehrindeki dükkanınızı yönetin.")
                            .SetTextVariable("TOWN", town.Name)
                            .ToString(),
                        options,
                        true,
                        1,  // Maksimum seçilebilecek seçenek sayısı
                        "{=WholesaleShopSelect}Seç",
                        "{=WholesaleShopCancel}İptal",
                        (List<InquiryElement> selectedOptions) =>
                        {
                            if (selectedOptions.Count > 0)
                            {
                                InquiryElement selectedOption = selectedOptions[0];
                                HandleShopManagementOption(town, (int)selectedOption.Identifier);
                            }
                        },
                        null  // cancel action
                    ));
                }
                else
                {
                    // Ayarlardan başlangıç sermayesini al
                    int initialCapital = Settings.Instance?.WholesaleMinimumCapital ?? 5000;
                    
                    // Yeni dükkan açma seçeneği
                    List<InquiryElement> options = new List<InquiryElement>();
                    
                    options.Add(new InquiryElement(
                        1,
                        new TextObject("{=WholesaleShopNewOption}Yeni Dükkan Kur ({GOLD} Dinar)")
                            .SetTextVariable("GOLD", initialCapital)
                            .ToString(),
                        null,
                        true,
                        new TextObject("{=WholesaleShopNewDesc}Bu şehirde yeni bir toptan satış dükkanı kurun ({GOLD} Dinar gerekir).")
                            .SetTextVariable("GOLD", initialCapital)
                            .ToString()
                    ));
                    
                    MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(
                        "{=WholesaleShopCreateTitle}Toptan Satış Dükkanı Kur",
                        new TextObject("{=WholesaleShopCreateDesc}{TOWN} şehrinde yeni bir toptan satış dükkanı kurmak ister misiniz?")
                            .SetTextVariable("TOWN", town.Name)
                            .ToString(),
                        options,
                        true,
                        1,
                        "{=WholesaleShopSelect}Seç",
                        "{=WholesaleShopCancel}İptal",
                        (List<InquiryElement> selectedOptions) =>
                        {
                            if (selectedOptions.Count > 0)
                            {
                                OpenNewShop(town);
                            }
                        },
                        null
                    ));
                }
            }
            catch (Exception ex)
            {
                LogError("Menü gösterilirken hata", ex);
            }
        }
        
        /// <summary>
        /// Dükkan yönetim seçeneğini işle
        /// </summary>
        private static void HandleShopManagementOption(Town town, int option)
        {
            try
            {
                switch (option)
                {
                    case 1: // Dükkan Bilgileri
                        ShowShopInfo(town);
                        break;
                        
                    case 2: // Sermaye Yatır
                        InformationManager.DisplayMessage(new InformationMessage(
                            "Sermaye yatırma işlemi için WholesaleShopViewModel kullanılmalıdır.", Colors.Yellow));
                        break;
                        
                    case 3: // Sermaye Çek
                        InformationManager.DisplayMessage(new InformationMessage(
                            "Sermaye çekme işlemi için WholesaleShopViewModel kullanılmalıdır.", Colors.Yellow));
                        break;
                        
                    case 4: // Dükkanı Kapat
                        InformationManager.DisplayMessage(new InformationMessage(
                            "Dükkan kapatma işlemi için WholesaleShopViewModel kullanılmalıdır.", Colors.Yellow));
                        break;
                        
                    default:
                        InformationManager.DisplayMessage(new InformationMessage(
                            "Geçersiz seçim.", Colors.Red));
                        break;
                }
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage($"Seçim işlemi sırasında hata: {ex.Message}"));
            }
        }
        
        /// <summary>
        /// Dükkan bilgilerini göster
        /// </summary>
        private static void ShowShopInfo(Town town)
        {
            try
            {
                var shop = _campaignBehavior.GetShopInTown(town);
                if (shop != null)
                {
                    string infoText = 
                        $"Şehir: {town.Name}\n" +
                        $"Sermaye: {shop.Capital} Dinar\n" +
                        $"Günlük Kazanç: {shop.DailyProfit} Dinar\n";
                    
                    InformationManager.DisplayMessage(new InformationMessage(infoText, Colors.Green));
                }
                else
                {
                    InformationManager.DisplayMessage(new InformationMessage(
                        "Bu şehirde bir dükkanınız yok.", Colors.Red));
                }
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage($"Dükkan bilgileri gösterilirken hata: {ex.Message}"));
            }
        }
        
        /// <summary>
        /// Yeni dükkan açma işlevi
        /// </summary>
        private static void OpenNewShop(Town town)
        {
            try
            {
                // Şehirde zaten dükkan var mı kontrol et
                if (_campaignBehavior.GetShopInTown(town) != null)
                {
                    InformationManager.DisplayMessage(new InformationMessage(
                        "{=WholesaleShopAlreadyExists}Bu şehirde zaten bir dükkanınız var.", Colors.Red));
                    return;
                }
                
                // Ayarlardan başlangıç sermayesini al
                int initialCapital = Settings.Instance?.WholesaleMinimumCapital ?? 5000;
                
                // Oyuncunun parasını kontrol et
                int playerGold = Hero.MainHero.Gold;
                
                if (playerGold < initialCapital)
                {
                    InformationManager.DisplayMessage(new InformationMessage(
                        new TextObject("{=WholesaleShopNotEnoughGold}Yeni bir dükkan açmak için yeterli paranız yok. En az {GOLD} Dinar gerekiyor.")
                            .SetTextVariable("GOLD", initialCapital)
                            .ToString(), 
                        Colors.Red));
                    return;
                }
                
                // Onay sor
                InformationManager.ShowInquiry(
                    new InquiryData(
                        "{=WholesaleShopNewShopTitle}Yeni Dükkan Aç",
                        new TextObject("{=WholesaleShopNewShopQuestion}{TOWN} şehrinde {GOLD} Dinar sermaye ile yeni bir toptan satış dükkanı açmak istiyor musunuz?")
                            .SetTextVariable("TOWN", town.Name)
                            .SetTextVariable("GOLD", initialCapital)
                            .ToString(),
                        true,
                        true,
                        "{=WholesaleShopNewShopConfirm}Evet, Aç",
                        "{=WholesaleShopNewShopCancel}Hayır, Vazgeç",
                        () => ConfirmOpenNewShop(town),
                        null
                    )
                );
            }
            catch (Exception ex)
            {
                LogError("Dükkan açma sırasında hata", ex);
            }
        }
        
        /// <summary>
        /// Yeni dükkan açma onayı
        /// </summary>
        private static void ConfirmOpenNewShop(Town town)
        {
            try
            {
                // Ayarlardan başlangıç sermayesini al
                int initialCapital = Settings.Instance?.WholesaleMinimumCapital ?? 5000;
                
                // Oyuncunun parasını kontrol et
                int playerGold = Hero.MainHero.Gold;
                
                if (playerGold < initialCapital)
                {
                    InformationManager.DisplayMessage(new InformationMessage(
                        new TextObject("{=WholesaleShopNotEnoughGold}Yeni bir dükkan açmak için yeterli paranız yok. En az {GOLD} Dinar gerekiyor.")
                            .SetTextVariable("GOLD", initialCapital)
                            .ToString(), 
                        Colors.Red));
                    return;
                }
                
                // Oyuncudan parayı al
                Hero.MainHero.ChangeHeroGold(-initialCapital);
                
                // Dükkanı oluştur
                WholesaleShop shop = _campaignBehavior.CreateShop(town, initialCapital);
                
                if (shop != null)
                {
                    // Başarılı bildirim
                    InformationManager.DisplayMessage(new InformationMessage(
                        "{=WholesaleShopCreated}Toptancı dükkanınız kurulmuştur.", Colors.Green));
                        
                    // Menü butonunu yeniden çizdirmek için _buttonAdded değişkenini sıfırla
                    // böylece RefreshValues çağrıldığında buton güncellenecek
                    _buttonAdded = false;
                }
                else
                {
                    // Başarısız ise parayı geri ver
                    Hero.MainHero.ChangeHeroGold(initialCapital);
                    
                    InformationManager.DisplayMessage(new InformationMessage(
                        "{=WholesaleShopFailed}Dükkan oluşturulamadı.", Colors.Red));
                }
            }
            catch (MissingMethodException ex)
            {
                // Prosperity metodu bulunamadı hatası için özel mesaj
                if (ex.Message.Contains("Prosperity"))
                {
                    InformationManager.DisplayMessage(new InformationMessage(
                        "{=WholesaleShopProsperityError}Oyunun bu sürümünde Prosperity özelliğine erişilemedi. Mod güncel Bannerlord sürümüyle uyumlu değil.", 
                        Colors.Red));
                }
                else
                {
                    // Diğer metot hatalarında
                    LogError("Metot bulunamadı hatası", ex);
                }
                
                // Parayı geri ver
                RefundShopCapital();
            }
            catch (Exception ex)
            {
                LogError("Dükkan açma sırasında hata", ex);
                
                // Parayı geri ver
                RefundShopCapital();
            }
        }
        
        /// <summary>
        /// Dükkan sermayesini iade eder (hata durumunda)
        /// </summary>
        private static void RefundShopCapital()
        {
            int refundAmount = Settings.Instance?.WholesaleMinimumCapital ?? 5000;
            Hero.MainHero.ChangeHeroGold(refundAmount);
            
            InformationManager.DisplayMessage(new InformationMessage(
                new TextObject("{=WholesaleShopRefund}Dükkan kurma başarısız oldu. {GOLD} Dinar iade edildi.")
                    .SetTextVariable("GOLD", refundAmount)
                    .ToString(), 
                Colors.Green));
        }
        
        /// <summary>
        /// Hata log mesajı gösterir ve kullanıcıya bildirim yapar
        /// </summary>
        private static void LogError(string message, Exception ex)
        {
            string errorMessage = $"{message}: {ex.Message}";
            
            // Konsola log
            if (Settings.Instance?.DebugMode ?? false)
            {
                Debug.Print($"TradingExpanded Error: {errorMessage}");
                Debug.Print($"Stack Trace: {ex.StackTrace}");
            }
            
            // Kullanıcıya göster
            InformationManager.DisplayMessage(new InformationMessage(errorMessage, Colors.Red));
        }
        
        /// <summary>
        /// Town sınıfından Prosperity değerini güvenli bir şekilde alır.
        /// API değişikliklerinde de çalışması için birden fazla yöntem dener.
        /// </summary>
        /// <param name="town">Prosperity değeri alınacak şehir</param>
        /// <param name="defaultValue">Alınamazsa kullanılacak varsayılan değer</param>
        /// <returns>Şehrin refah seviyesi veya hata durumunda varsayılan değer</returns>
        public static float GetTownProsperityValue(Town town, float defaultValue = 300f)
        {
            if (town == null)
                return defaultValue;
                
            try
            {
                // Sırasıyla erişim yöntemlerini dene
                
                // 1. Reflection ile özelliğe erişmeyi dene
                PropertyInfo propertyInfo = town.GetType().GetProperty("Prosperity");
                if (propertyInfo != null)
                {
                    object value = propertyInfo.GetValue(town);
                    if (value != null && (value is float || value is int))
                    {
                        return Convert.ToSingle(value);
                    }
                }
                
                // 2. Fief sınıfındaki özelliğe erişmeyi dene (Inheritance yapısında değişiklik olabilir)
                propertyInfo = town.GetType().BaseType?.GetProperty("Prosperity");
                if (propertyInfo != null)
                {
                    object value = propertyInfo.GetValue(town);
                    if (value != null && (value is float || value is int))
                    {
                        return Convert.ToSingle(value);
                    }
                }
                
                // 3. Fief sınıfını doğrudan bulmayı dene
                Type fiefType = typeof(Town).Assembly.GetType("TaleWorlds.CampaignSystem.Settlements.Fief");
                if (fiefType != null)
                {
                    // Town nesnesi Fief'e dönüştürülebilir mi kontrol et
                    if (fiefType.IsAssignableFrom(town.GetType()))
                    {
                        propertyInfo = fiefType.GetProperty("Prosperity");
                        if (propertyInfo != null)
                        {
                            object value = propertyInfo.GetValue(town);
                            if (value != null && (value is float || value is int))
                            {
                                return Convert.ToSingle(value);
                            }
                        }
                    }
                }
                
                // 4. Başka bir metot üzerinden değeri almayı dene
                var method = town.GetType().GetMethod("GetProsperity") ??
                             town.GetType().GetMethod("CalculateProsperity") ??
                             town.GetType().BaseType?.GetMethod("GetProsperity");
                             
                if (method != null && (method.ReturnType == typeof(float) || method.ReturnType == typeof(int)))
                {
                    object result = method.Invoke(town, null);
                    if (result != null)
                    {
                        return Convert.ToSingle(result);
                    }
                }
                
                // 5. Settlement.Prosperity özelliğini dene (API değişmiş olabilir)
                propertyInfo = town.Settlement?.GetType().GetProperty("Prosperity");
                if (propertyInfo != null)
                {
                    object value = propertyInfo.GetValue(town.Settlement);
                    if (value != null && (value is float || value is int))
                    {
                        return Convert.ToSingle(value);
                    }
                }
                
                // Hiçbir yöntem çalışmadı, sessizce varsayılan değeri döndür
                if (Settings.Instance?.DebugMode ?? false)
                {
                    InformationManager.DisplayMessage(new InformationMessage(
                        "{=WholesaleShopProsperityDefault}Şehrin refah seviyesine erişilemedi, varsayılan değer kullanılıyor.", 
                        Colors.Yellow));
                }
                return defaultValue;
            }
            catch (Exception ex)
            {
                if (Settings.Instance?.DebugMode ?? false)
                {
                    LogError("Prosperity değeri alınırken hata", ex);
                }
                return defaultValue;
            }
        }
    }
} 