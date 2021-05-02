using Content.Server.GameObjects.Components.Buckle;
using Content.Shared.GameObjects.Components;
using Content.Shared.GameObjects.EntitySystems.ActionBlocker;
using Content.Shared.GameObjects.EntitySystems.EffectBlocker;
using Content.Shared.GameObjects.Verbs;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.Placement;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;

namespace Content.Server.GameObjects.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedFoldableComponent))]
    public class FoldableComponent : SharedFoldableComponent, IUse, IEffectBlocker//, IPlacementManager
    {
        public override void Initialize()
        {
            base.Initialize();
        }

        public bool UseEntity(UseEntityEventArgs eventArgs)
        {
            return false;
        }
    }

    /// <summary>
    ///     Allows the unbuckling of the owning entity through a verb if
    ///     anyone right clicks them.
    /// </summary>
    [Verb]
    private sealed class FoldVerb : Verb<FoldableComponent>
    {
        protected override void GetData(IEntity user, FoldableComponent component, VerbData data)
        {
        }

        protected override void Activate(IEntity user, FoldableComponent component)
        {
        }
    }
}
