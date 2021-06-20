using Content.Server.Atmos.Components;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Server.Atmos.EntitySystems
{
    [UsedImplicitly]
    public class AirtightSystem : EntitySystem
    {
        public override void Initialize()
        {
            SubscribeLocalEvent<AirtightComponent, AnchorStateChangedEvent>(OnAirtightPositionChanged);
            SubscribeLocalEvent<AirtightComponent, RotateEvent>(OnAirtightRotated);
        }

        private void OnAirtightPositionChanged(EntityUid uid, AirtightComponent component, AnchorStateChangedEvent args)
        {
            component.AnchorStateChanged();
        }

        private void OnAirtightRotated(EntityUid uid, AirtightComponent airtight, RotateEvent ev)
        {
            airtight.RotateEvent(ev);
        }
    }
}
