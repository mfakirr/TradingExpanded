using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.Library;

namespace TradingExpanded.Models
{
    /// <summary>
    /// Harita üzerindeki etkileşimli varlıklar için basit bir uygulama
    /// </summary>
    public class MapEntity : IMapEntity
    {
        public MapEntity(Vec2 position)
        {
            InteractionPosition = position;
        }

        public Vec2 InteractionPosition { get; set; }

        public TextObject Name => new TextObject("Map Entity");

        public bool IsMobileEntity => false;

        public bool ShowCircleAroundEntity => false;

        public void GetMountAndHarnessVisualIdsForPartyIcon(out string mountStringId, out string harnessStringId)
        {
            mountStringId = null;
            harnessStringId = null;
        }

        public bool IsAllyOf(IFaction faction)
        {
            return false;
        }

        public bool IsEnemyOf(IFaction faction)
        {
            return false;
        }

        public void OnHover()
        {
            // Fare üzerine gelindiğinde bir şey yapma
        }

        public bool OnMapClick(bool followModifierUsed)
        {
            // Haritada tıklandığında bir şey yapma
            return false;
        }

        public void OnOpenEncyclopedia()
        {
            // Ansiklopedi açıldığında bir şey yapma
        }

        public void OnPartyInteraction(MobileParty mobileParty)
        {
            // Parti etkileşimi olduğunda bir şey yapma
        }
    }
} 