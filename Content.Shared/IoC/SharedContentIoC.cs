using Content.Shared.Humanoid.Markings;
using Content.Shared.Localizations;
using Content.Shared.Ganimed.SponsorManager;

namespace Content.Shared.IoC
{
    public static class SharedContentIoC
    {
        public static void Register()
        {
            IoCManager.Register<MarkingManager, MarkingManager>();
			IoCManager.Register<SponsorManager, SponsorManager>();
            IoCManager.Register<ContentLocalizationManager, ContentLocalizationManager>();
        }
    }
}
