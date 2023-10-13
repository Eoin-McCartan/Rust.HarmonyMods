using Harmony;

namespace Rust.Harmony.StackSizeManager
{
    internal class HarmonyHooks : IHarmonyModHooks
    {
        public void OnLoaded(OnHarmonyModLoadedArgs args) => HarmonyConfig.LoadConfig();

        public void OnUnloaded(OnHarmonyModUnloadedArgs args)
        {
        }
    }
}