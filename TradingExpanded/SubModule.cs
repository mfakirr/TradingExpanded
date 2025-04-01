using TaleWorlds.MountAndBlade;

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
    }
}