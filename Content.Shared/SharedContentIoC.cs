using Content.Shared.Preferences.Appearance;
using Robust.Shared.IoC;

namespace Content.Shared
{
    public static class SharedContentIoC
    {
        public static void Register()
        {
            IoCManager.Register<SpriteAccessoryManager, SpriteAccessoryManager>();
        }
    }
}
