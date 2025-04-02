using TaleWorlds.MountAndBlade;
using TaleWorlds.CampaignSystem;
using TradingExpanded.Behaviors;
using TaleWorlds.Core;

namespace Bannerlord.BUTRLoader
{
    [System.AttributeUsage(System.AttributeTargets.Class)]
    public sealed class BLSEInterceptorAttribute : System.Attribute { }
}

namespace TradingExpanded
{
    [Bannerlord.BUTRLoader.BLSEInterceptor]
    public static class BLSELoadingInterceptor
    {
        public static void OnInitializeSubModulesPrefix()
        {

        }
        public static void OnLoadSubModulesPostfix()
        {

        }
    }

    public class SubModule : MBSubModuleBase
    {
        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();

        }

        protected override void OnSubModuleUnloaded()
        {
            base.OnSubModuleUnloaded();

        }

        protected override void OnBeforeInitialModuleScreenSetAsRoot()
        {
            base.OnBeforeInitialModuleScreenSetAsRoot();

        }
        
        protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
        {
            base.OnGameStart(game, gameStarterObject);
            
            if (game.GameType is Campaign)
            {
                if (gameStarterObject is CampaignGameStarter campaignStarter)
                {
                    campaignStarter.AddBehavior(new TradingExpandedCampaignBehavior());
                }
            }
        }
    }
}