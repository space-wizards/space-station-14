namespace Content.Shared.Localizations;

public static partial class ContextedLoc
{
    private static ILocalizationManager _loc => IoCManager.Resolve<ILocalizationManager>();
    private static EntityManager _entity => IoCManager.Resolve<EntityManager>();
}
