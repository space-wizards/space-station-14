#nullable enable
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;

namespace Content.Server.UserInterface
{
    public static class UserInterfaceHelpers
    {
        public static BoundUserInterface? GetUIOrNull(this IEntity entity, object uiKey)
        {
            return entity.GetComponentOrNull<ServerUserInterfaceComponent>()?.GetBoundUserInterfaceOrNull(uiKey);
        }
    }
}
