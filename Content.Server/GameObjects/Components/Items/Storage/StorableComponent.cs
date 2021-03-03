using Content.Shared.GameObjects.Components.Storage;
using Robust.Shared.GameObjects;
using Robust.Shared.Players;

namespace Content.Server.GameObjects.Components.Items.Storage
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedStorableComponent))]
    public class StorableComponent : SharedStorableComponent
    {
        public override ComponentState GetComponentState(ICommonSession player)
        {
            return new StorableComponentState(Size);
        }
    }


}
