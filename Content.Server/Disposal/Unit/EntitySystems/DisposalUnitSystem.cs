using Content.Server.Disposal.Unit.Components;
using Content.Server.Construction.Components;
using Robust.Shared.GameObjects;

namespace Content.Server.Disposal.Unit.EntitySystems
{
    public sealed class DisposalUnitSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<DisposalUnitComponent, AnchoredEvent>(OnAnchored);
            SubscribeLocalEvent<DisposalUnitComponent, UnanchoredEvent>(OnUnanchored);
        }

        private static void OnAnchored(EntityUid uid, DisposalUnitComponent component, AnchoredEvent args)
        {
            component.UpdateVisualState();
        }

        private static void OnUnanchored(EntityUid uid, DisposalUnitComponent component, UnanchoredEvent args)
        {
            component.UpdateVisualState();
            component.TryEjectContents();
        }
    }
}
