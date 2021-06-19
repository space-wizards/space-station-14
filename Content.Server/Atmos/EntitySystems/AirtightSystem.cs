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
            SubscribeLocalEvent<AirtightComponent, SnapGridPositionChangedEvent>(OnAirtightPositionChanged);
            SubscribeLocalEvent<AirtightComponent, RotateEvent>(OnAirtightRotated);
        }

        private void OnAirtightPositionChanged(EntityUid uid, AirtightComponent component, SnapGridPositionChangedEvent args)
        {
            component.OnSnapGridMove(args);
        }

        private void OnAirtightRotated(EntityUid uid, AirtightComponent airtight, RotateEvent ev)
        {
            airtight.RotateEvent(ev);
        }
    }
}
