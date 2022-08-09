using Robust.Server.GameObjects;

namespace Content.Server.UserInterface
{
    public static class UserInterfaceHelpers
    {
        public static BoundUserInterface? GetUIOrNull(this EntityUid entity, object uiKey)
        {
            return IoCManager.Resolve<IEntityManager>().GetComponentOrNull<ServerUserInterfaceComponent>(entity)?.GetBoundUserInterfaceOrNull(uiKey);
        }
    }
}
