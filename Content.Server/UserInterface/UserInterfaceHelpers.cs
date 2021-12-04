using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

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
