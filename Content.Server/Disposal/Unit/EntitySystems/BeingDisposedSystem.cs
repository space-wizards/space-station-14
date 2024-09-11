using Content.Server.Atmos.EntitySystems;
using Content.Server.Body.Systems;
using Content.Server.Disposal.Unit.Components;
using Content.Server.Storage.Components;

namespace Content.Server.Disposal.Unit.EntitySystems;

public sealed class BeingDisposedSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BeingDisposedComponent, InhaleLocationEvent>(OnInhaleLocation);
        SubscribeLocalEvent<BeingDisposedComponent, ExhaleLocationEvent>(OnExhaleLocation);
        SubscribeLocalEvent<BeingDisposedComponent, AtmosExposedGetAirEvent>(OnGetAir);
    }

    private void OnGetAir(EntityUid uid, BeingDisposedComponent component, ref AtmosExposedGetAirEvent args)
    {
        if (TryComp<InternalAirComponent>(component.Holder, out var internalAir))
        {
            args.Gas = internalAir.Air;
            args.Handled = true;
        }
    }

    private void OnInhaleLocation(EntityUid uid, BeingDisposedComponent component, InhaleLocationEvent args)
    {
        if (TryComp<InternalAirComponent>(component.Holder, out var internalAir))
        {
            args.Gas = internalAir.Air;
        }
    }

    private void OnExhaleLocation(EntityUid uid, BeingDisposedComponent component, ExhaleLocationEvent args)
    {
        if (TryComp<InternalAirComponent>(component.Holder, out var internalAir))
        {
            args.Gas = internalAir.Air;
        }
    }
}
