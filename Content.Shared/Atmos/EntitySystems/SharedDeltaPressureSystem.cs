using Content.Shared.Atmos.Components;
using Content.Shared.Examine;

namespace Content.Shared.Atmos.EntitySystems;

public abstract partial class SharedDeltaPressureSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DeltaPressureComponent, ExaminedEvent>(OnExaminedEvent);
    }

    protected virtual void OnExaminedEvent(Entity<DeltaPressureComponent> ent, ref ExaminedEvent args) { }
}
