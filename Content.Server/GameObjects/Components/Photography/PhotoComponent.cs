using Content.Server.GameObjects.EntitySystems;
using Content.Shared.GameObjects.Components.Photography;
using Content.Shared.Interfaces;
using Robust.Shared.GameObjects;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Photography
{
    [RegisterComponent]
    public class PhotoComponent : SharedPhotoComponent, IUse
    {
        [ViewVariables] public string PhotoId;

        public bool UseEntity(UseEntityEventArgs eventArgs)
        {
            if(PhotoId == null)
            {
                eventArgs.User.PopupMessage(eventArgs.User, "It's not dry yet!");
                return false;
            }
            SendNetworkMessage(new OpenPhotoUiMessage(PhotoId));
            return true;
        }
    }
}
