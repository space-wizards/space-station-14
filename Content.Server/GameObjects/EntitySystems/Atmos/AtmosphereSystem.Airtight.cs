using Content.Server.GameObjects.Components.Atmos;
using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.EntitySystems.Atmos
{
    public partial class AtmosphereSystem
    {
        private void InitializeAirtight()
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
