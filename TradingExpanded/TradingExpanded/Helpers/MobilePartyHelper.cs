using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Map;
using TradingExpanded.Components;
using static SandBox.ViewModelCollection.MapSiege.MapSiegePOIVM;
using static TaleWorlds.CampaignSystem.CharacterDevelopment.DefaultPerks;

namespace TradingExpanded.Helpers
{
    /// <summary>
    /// Mobil partilerin oluşturulması ve yönetilmesi için yardımcı sınıf
    /// </summary>
    public static class MobilePartyHelper
    {
        private const float COURIER_BASE_SPEED = 5.0f;
        private const string COURIER_SOUND_TYPE = "caravan";
        
        /// <summary>
        /// Köken yerleşimin kültürüne dayalı uygun bir atlı asker bul
        /// </summary>
        private static CharacterObject FindSuitableMountedTroop(Settlement settlement)
        {
            try
            {
                // Kültür kontrol et
                CultureObject culture = settlement?.Culture;
                if (culture == null) return null;
                
                // Öncelikle karakol veya kervan şablonundan bak
                PartyTemplateObject template = culture.MilitiaPartyTemplate ?? culture.CaravanPartyTemplate;
                
                if (template != null)
                {
                    foreach (var stack in template.Stacks)
                    {
                        CharacterObject soldier = stack.Character;
                        if (soldier != null && soldier.IsMounted)
                        {
                            return soldier;
                        }
                    }
                }
                
                // Eğer bulunamazsa, kültürdeki tüm atlı askerlere bak
                return CharacterObject.All
                    .Where(c => c.Culture == culture && c.IsMounted && c.Tier <= 3)
                    .OrderBy(c => c.Tier)
                    .FirstOrDefault();
            }
            catch (Exception ex)
            {
                CourierLogEntry($"Atlı asker bulma hatası: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Rastgele bir temel asker döndürür
        /// </summary>
        private static CharacterObject GetRandomBasicTroop()
        {
            try
            {
                // Tier 1-2 arası temel bir asker bul
                var basicTroops = CharacterObject.All
                    .Where(c => c.IsBasicTroop && c.Tier <= 2)
                    .ToList();
                    
                if (basicTroops.Count > 0)
                {
                    return basicTroops[MBRandom.RandomInt(0, basicTroops.Count)];
                }
                
                return null;
            }
            catch (Exception ex)
            {
                CourierLogEntry($"Rastgele asker bulma hatası: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Yeni bir kurye partisi oluşturur
        /// </summary>
        /// <param name="originSettlement">Başlangıç yerleşimi</param>
        /// <param name="destinationSettlement">Hedef yerleşim</param>
        /// <param name="owner">Parti sahibi (null ise oyuncu)</param>
        /// <param name="name">Parti adı</param>
        /// <returns>Oluşturulan MobileParty nesnesi</returns>
        public static MobileParty CreateNewCourierParty(
            Settlement originSettlement,
            Settlement destinationSettlement,
            Hero owner = null,
            TextObject name = null)
        {
            if (originSettlement == null)
            {
                LogCritical("Kurye oluşturulamadı: Başlangıç yerleşimi null");
                return null;
            }

            if (destinationSettlement == null)
            {
                LogCritical("Kurye oluşturulamadı: Hedef yerleşimi null");
                return null;
            }

            try
            {
                // Parti adını hazırla
                string partyName = name?.ToString() ?? $"Ulak ({originSettlement.Name} → {destinationSettlement.Name})";
                
                // StringId oluştur - benzersiz olmalı
                string stringId = $"courier_{originSettlement.StringId}_{destinationSettlement.StringId}_{MBRandom.RandomInt(1000, 9999)}";
                
                CourierLogEntry($"Kurye partisi oluşturuluyor: {stringId}, Başlangıç: {originSettlement.Name}, Hedef: {destinationSettlement.Name}");
                
                // Mevcut CourierPartyComponent sınıfını kullan
                MobileParty courierParty = Components.CourierPartyComponent.CreateTravellerParty(
                    "courier", 
                    originSettlement, 
                    destinationSettlement, 
                    partyName, 
                    1, // Kurye sayısı 
                    Components.PopType.None, // Nüfus tipi - kuryeler için None kullanabiliriz
                    FindSuitableCourierCharacter(originSettlement), // Kurye karakteri bul
                    true // Trading true - bu kurye ticaret için
                );
                
                if (courierParty == null)
                {
                    CourierLogEntry("Kurye partisi oluşturulamadı", false);
                    return null;
                }
                
                // Başarılı kurye oluşumu
                CourierLogEntry($"Kurye partisi başarıyla oluşturuldu: {courierParty.Name}, Hedef: {destinationSettlement.Name}");
                
                // İhtiyaç duyulan ayarlamaları yap
                ConfigurePartyForCourierRole(courierParty, destinationSettlement);
                
                return courierParty;
            }
            catch (Exception ex)
            {
                CourierLogEntry($"Kurye oluşturma hatası: {ex.Message}", false);
                return null;
            }
        }

        /// <summary>
        /// Kurye için uygun bir karakter bulur
        /// </summary>
        private static CharacterObject FindSuitableCourierCharacter(Settlement originSettlement)
        {
            try
            {
                // Önce yerleşimdeki kültüre uygun sivil karakterleri dene
                CharacterObject civilian = CharacterObject.All
                    .Where(c => c.Occupation == Occupation.Merchant && c.Culture == originSettlement.Culture)
                    .FirstOrDefault();

                // Bulunamadıysa herhangi bir tüccar veya sivil kullan
                if (civilian == null)
                {
                    civilian = CharacterObject.All
                        .Where(c => c.Occupation == Occupation.Merchant || c.Occupation == Occupation.Townsfolk)
                        .FirstOrDefault();
                }

                // Hala bulunamadıysa looter kullan (son çare)
                if (civilian == null)
                {
                    civilian = CharacterObject.All
                        .Where(c => c.StringId == "looter")
                        .FirstOrDefault();
                }

                return civilian;
            }
            catch (Exception ex)
            {
                CourierLogEntry($"Sivil karakter arama hatası: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Partiyi kurye rolü için yapılandırır
        /// </summary>
        private static void ConfigurePartyForCourierRole(MobileParty party, Settlement destinationSettlement)
        {
            if (party == null || !party.IsActive) return;
            
            try
            {
                // 1. Parti tipini ayarla
                SetPartyTypeAndVisuals(party);
                
                // 2. AI davranışını ayarla
                ConfigurePartyAI(party, destinationSettlement);
                
                // 3. Karşılaşmaları (encounters) devre dışı bırak
                DisableEncounters(party);
                
                // 4. Yiyecek ihtiyacını kaldır
                DisablePartyFoodNeed(party);
                
                // 5. Parti şehirde ise çıkmasını sağla
                if (party.CurrentSettlement != null)
                {
                    ForcePartyToLeaveSettlement(party);
                }
            }
            catch (Exception ex)
            {
                CourierLogEntry($"Kurye parti yapılandırma hatası: {ex.Message}", false);
            }
        }
        
        /// <summary>
        /// Parti tipini ve görsellerini ayarlar
        /// </summary>
        private static void SetPartyTypeAndVisuals(MobileParty party)
        {
            try
            {
                // 1. Parti tipini "Caravan" olarak ayarla
                PropertyInfo partyTypeProperty = party.GetType().GetProperty("PartyType");
                if (partyTypeProperty != null && partyTypeProperty.CanWrite)
                {
                    Type partyTypeEnum = partyTypeProperty.PropertyType;
                    if (partyTypeEnum.IsEnum)
                    {
                        object caravanValue = Enum.Parse(partyTypeEnum, "Caravan");
                        partyTypeProperty.SetValue(party, caravanValue);
                    }
                }
                
                // 2. Hız ayarı
                PropertyInfo baseSpeedProperty = party.GetType().GetProperty("BaseSpeed");
                if (baseSpeedProperty != null && baseSpeedProperty.CanWrite)
                {
                    baseSpeedProperty.SetValue(party, COURIER_BASE_SPEED);
                }
                
                // 3. Görsel ayarları
                PropertyInfo visualsProperty = party.GetType().GetProperty("PartyVisual");
                if (visualsProperty != null)
                {
                    object visuals = visualsProperty.GetValue(party);
                    if (visuals != null)
                    {
                        MethodInfo setMapIconMethod = visuals.GetType().GetMethod("SetMapIconAsCivilian");
                        if (setMapIconMethod != null)
                        {
                            setMapIconMethod.Invoke(visuals, null);
                        }
                    }
                }
                
                // 4. Ses karakteri tipini ayarla
                PropertyInfo soundProperty = party.GetType().GetProperty("SoundCharacterType");
                if (soundProperty != null && soundProperty.CanWrite)
                {
                    soundProperty.SetValue(party, COURIER_SOUND_TYPE);
                }
            }
            catch (Exception) { /* Sessizce devam et */ }
        }
        
        /// <summary>
        /// Parti AI davranışını ayarlar
        /// </summary>
        private static void ConfigurePartyAI(MobileParty party, Settlement destinationSettlement)
        {
            try
            {
                if (party.Ai == null)
                {
                    CourierLogEntry("Parti AI'si null, yapılandırılamıyor", false);
                    return;
                }
                
                // 1. AI'yi etkinleştir
                party.Ai.EnableAi();
                
                // 2. Hedef yerleşimi ayarla - BU ÇOK ÖNEMLİ
                party.Ai.SetMoveGoToSettlement(destinationSettlement);
                
                // 3. Davranış tipini GoToSettlement olarak ayarla
                PropertyInfo defaultBehaviorProperty = party.Ai.GetType().GetProperty("DefaultBehavior");
                if (defaultBehaviorProperty != null && defaultBehaviorProperty.CanWrite)
                {
                    Type aiBehaviorEnum = defaultBehaviorProperty.PropertyType;
                    if (aiBehaviorEnum.IsEnum)
                    {
                        try
                        {
                            object goToSettlementValue = Enum.Parse(aiBehaviorEnum, "GoToSettlement");
                            defaultBehaviorProperty.SetValue(party.Ai, goToSettlementValue);
                        }
                        catch { /* Enum değeri yoksa sessizce devam et */ }
                    }
                }
                
                // 4. TargetSettlement değerini ayarla
                PropertyInfo targetSettlementProperty = party.GetType().GetProperty("TargetSettlement");
                if (targetSettlementProperty != null && targetSettlementProperty.CanWrite)
                {
                    targetSettlementProperty.SetValue(party, destinationSettlement);
                }
                
                // 5. Hareket modunu ayarla
                MethodInfo setMoveGoToPointMethod = party.Ai.GetType().GetMethod("SetMoveGoToPoint");
                if (setMoveGoToPointMethod != null)
                {
                    setMoveGoToPointMethod.Invoke(party.Ai, new object[] { destinationSettlement.Position2D });
                }
            }
            catch (Exception ex)
            {
                CourierLogEntry($"Parti AI davranışını ayarlama hatası: {ex.Message}", false);
            }
        }
        
        /// <summary>
        /// Partinin encounter sistemini devre dışı bırakır
        /// </summary>
        private static void DisableEncounters(MobileParty party)
        {
            try
            {
                if (party == null) return;
                
                // 1. DoNotMakeEncounter özelliğini true yap
                PropertyInfo doNotMakeEncounterProperty = party.GetType().GetProperty("DoNotMakeEncounter");
                if (doNotMakeEncounterProperty != null && doNotMakeEncounterProperty.CanWrite)
                {
                    doNotMakeEncounterProperty.SetValue(party, true);
                }
                
                // 2. IgnoreByOtherPartiesDueToDebug özelliğini ayarla
                PropertyInfo ignoreByOtherPartiesProperty = party.GetType().GetProperty("IgnoreByOtherPartiesDueToDebug");
                if (ignoreByOtherPartiesProperty != null && ignoreByOtherPartiesProperty.CanWrite)
                {
                    ignoreByOtherPartiesProperty.SetValue(party, true);
                }
            }
            catch (Exception ex)
            {
                CourierLogEntry($"Encounter devre dışı bırakma hatası: {ex.Message}", false);
            }
        }
        
        /// <summary>
        /// Partiyi şehirden çıkmaya zorlar (parti şehir içindeyse)
        /// </summary>
        private static void ForcePartyToLeaveSettlement(MobileParty party)
        {
            try
            {
                if (party == null || !party.IsActive || party.CurrentSettlement == null) return;
                
                // 1. Şehirden çıkma metodunu çağır
                MethodInfo leaveSettlementMethod = party.GetType().GetMethod("LeaveSettlement");
                if (leaveSettlementMethod != null)
                {
                    leaveSettlementMethod.Invoke(party, null);
                    return;
                }
                
                // 2. CurrentSettlement değerini null yap ve konumu düzelt
                PropertyInfo currentSettlementProperty = party.GetType().GetProperty("CurrentSettlement");
                if (currentSettlementProperty != null && currentSettlementProperty.CanWrite)
                {
                    Settlement settlement = party.CurrentSettlement;
                    currentSettlementProperty.SetValue(party, null);
                    
                    if (settlement != null)
                    {
                        Vec2 settPos = settlement.Position2D;
                        Vec2 offset = new Vec2(0.5f, 0.5f); // 500 metre offset
                        party.Position2D = settPos + offset;
                    }
                }
            }
            catch (Exception ex)
            {
                CourierLogEntry($"Şehirden çıkarma hatası: {ex.Message}", false);
            }
        }
        
        /// <summary>
        /// Partinin erzak ihtiyacını kaldırır
        /// </summary>
        private static void DisablePartyFoodNeed(MobileParty party)
        {
            try
            {
                if (party == null) return;
                
                // 1. NeedsNoDailyFood özelliğini true yap
                PropertyInfo needsNoDailyFoodProperty = party.GetType().GetProperty("NeedsNoDailyFood");
                if (needsNoDailyFoodProperty != null && needsNoDailyFoodProperty.CanWrite)
                {
                    needsNoDailyFoodProperty.SetValue(party, true);
                }
                
                // 2. DailyFoodConsumptionMultiplier değerini 0 yap
                PropertyInfo foodMultiplierProperty = party.GetType().GetProperty("DailyFoodConsumptionMultiplier");
                if (foodMultiplierProperty != null && foodMultiplierProperty.CanWrite)
                {
                    foodMultiplierProperty.SetValue(party, 0f);
                }
                
                // 3. Yine de temel erzak ver
                AddBasicFoodToParty(party);
            }
            catch (Exception ex)
            {
                CourierLogEntry($"Erzak ihtiyacını kaldırma hatası: {ex.Message}", false);
            }
        }
        
        /// <summary>
        /// Partiye temel erzak ekler
        /// </summary>
        private static void AddBasicFoodToParty(MobileParty party)
        {
            try
            {
                if (party == null) return;
                
                var grainItem = MBObjectManager.Instance.GetObjectTypeList<ItemObject>()
                    .FirstOrDefault(i => i.StringId.Contains("grain"));
                    
                if (grainItem != null)
                {
                    party.ItemRoster.AddToCounts(grainItem, 5);
                }
            }
            catch (Exception ex)
            {
                CourierLogEntry($"Erzak ekleme hatası: {ex.Message}", false);
            }
        }
        
        /// <summary>
        /// Kritik log mesajı - kullanıcıya gösterilir
        /// </summary>
        private static void LogCritical(string message)
        {
            try
            {
                Utils.LogManager.Instance.WriteInfo($"[Courier] {message}");
                InformationManager.DisplayMessage(new InformationMessage(message, Colors.Red));
            }
            catch { /* Sessizce devam et */ }
        }
        
        /// <summary>
        /// Bilgi log mesajı - sadece dosyaya yazılır
        /// </summary>
        private static void LogInfo(string message)
        {
            try
            {
                Utils.LogManager.Instance.WriteInfo($"[Courier] {message}");
            }
            catch { /* Sessizce devam et */ }
        }
        
        /// <summary>
        /// Kurye işlemleri için günlük kaydı - geriye dönük uyumluluk
        /// </summary>
        public static void CourierLogEntry(string message, bool isSuccess = true)
        {
            try
            {
                Utils.LogManager.Instance.WriteInfo($"[Courier] {message}");
                
                if (!isSuccess)
                {
                    InformationManager.DisplayMessage(new InformationMessage(message, Colors.Red));
                }
            }
            catch { /* Sessizce devam et */ }
        }
    }
} 