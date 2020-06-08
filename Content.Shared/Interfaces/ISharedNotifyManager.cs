using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Content.Shared.Interfaces
{
    public interface ISharedNotifyManager
    {
        void PopupMessage(IEntity source, IEntity viewer, string message);
        void PopupMessage(GridCoordinates coordinates, IEntity viewer, string message);
        void PopupMessageCursor(IEntity viewer, string message);
    }

    public static class NotifyManagerExt
    {
        public static void PopupMessage(this IEntity source, IEntity viewer, string message)
        {
            IoCManager.Resolve<ISharedNotifyManager>().PopupMessage(source, viewer, message);
        }
    }
}
