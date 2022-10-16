using Content.Shared.Humanoid.Markings;

namespace Content.Shared.IoC
{
    public static class SharedContentIoC
    {
        public static void Register()
        {
            IoCManager.Register<MarkingManager, MarkingManager>();
        }
    }
}
