using Content.Shared.CharacterAppearance;
using Content.Shared.Markings;
using Robust.Shared.IoC;

namespace Content.Shared.IoC
{
    public static class SharedContentIoC
    {
        public static void Register()
        {
            IoCManager.Register<SpriteAccessoryManager, SpriteAccessoryManager>();
            IoCManager.Register<MarkingManager, MarkingManager>();
        }
    }
}
