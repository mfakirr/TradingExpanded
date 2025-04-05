using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;

namespace TradingExpanded.TradingExpanded.Components
{
    public abstract class TECompanent : PartyComponent
    {
        protected TECompanent(Settlement target, string stringName)
        {
            Home = target;
            this.stringName = stringName;
        }

        protected TECompanent(Settlement target)
        {
            Home = target;
        }

        [SaveableProperty(1)] protected Settlement Home { get; set; }
        [SaveableProperty(2)] private string stringName { get; set; }
        public override Hero PartyOwner => HomeSettlement.OwnerClan.Leader;

        public override TextObject Name => new TextObject(stringName).SetTextVariable("ORIGIN", Home.Name);

        public override Settlement HomeSettlement => Home;

        protected static void GiveMounts(ref MobileParty party)
        {
            var lacking = party.Party.NumberOfRegularMembers - party.Party.NumberOfMounts;
            var horse = Items.All.FirstOrDefault(x => x.StringId == "sumpter_horse");
            party.ItemRoster.AddToCounts(horse, lacking);
        }

        public static void GiveFood(ref MobileParty party)
        {
            foreach (var itemObject in Items.All)
            {
                if (itemObject.IsFood)
                {
                    var num2 = MBRandom.RoundRandomized(party.Party.NumberOfAllMembers *
                                                        (1f / itemObject.Value) * 16 * MBRandom.RandomFloat *
                                                        MBRandom.RandomFloat * MBRandom.RandomFloat * MBRandom.RandomFloat);
                    if (num2 > 0)
                    {
                        party.ItemRoster.AddToCounts(itemObject, num2);
                    }
                }
            }
        }

        public abstract void TickHourly();
    }
}
