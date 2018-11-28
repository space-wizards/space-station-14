using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.IoC;
using SS14.Shared.Map;

namespace Content.Shared.Interfaces
{
    public interface ISharedNotifyManager
    {
        void PopupMessage(IEntity source, IEntity viewer, string message);
        void PopupMessage(GridLocalCoordinates coordinates, IEntity viewer, string message);
    }

    public static class NotifyManagerExt
    {
        public static void PopupMessage(this IEntity source, IEntity viewer, string message)
        {
            IoCManager.Resolve<ISharedNotifyManager>().PopupMessage(source, viewer, message);
        }
    }
}
