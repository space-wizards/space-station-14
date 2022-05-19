using Content.Shared.CharacterAppearance;
using Content.Shared.Markings;

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
