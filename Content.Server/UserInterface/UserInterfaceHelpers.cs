using Robust.Server.GameObjects;

namespace Content.Server.UserInterface
{
    public static class UserInterfaceHelpers
    {
        [Obsolete("Use UserInterfaceSystem")]
        public static PlayerBoundUserInterface? GetUIOrNull(this EntityUid entity, Enum uiKey)
        {
            return IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<UserInterfaceSystem>().GetUiOrNull(entity, uiKey);
        }
    }
}
