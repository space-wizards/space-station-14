using Content.Shared.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.Components.Items.Storage
{
    [RegisterComponent]
    public class InEntityStorageComponent : Component, IActionBlocker
    {
        public override string Name => "InEntityStorage";

        public bool CanInteract()
        {
            return false;
        }
    }
}
