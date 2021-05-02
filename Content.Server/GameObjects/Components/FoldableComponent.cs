using Content.Shared.GameObjects.Components;
using Content.Shared.GameObjects.EntitySystems.ActionBlocker;
using Content.Shared.GameObjects.EntitySystems.EffectBlocker;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.Placement;
using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedFoldableComponent))]
    public class FoldableComponent : SharedFoldableComponent, IUse, IEffectBlocker//, IPlacementManager
    {
        public bool CanBePickedUp() => false;

        public override void Initialize()
        {
            base.Initialize();
        }

        public bool UseEntity(UseEntityEventArgs eventArgs)
        {
            eventArgs.User.CanPickup();
            return false;
        }
    }
}
