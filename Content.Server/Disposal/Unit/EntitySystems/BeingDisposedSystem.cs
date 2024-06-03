using Content.Server.Atmos.EntitySystems;
using Content.Server.Body.Systems;
using Content.Server.Disposal.Unit.Components;

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
        if (TryComp<DisposalHolderComponent>(component.Holder, out var holder))
        {
            args.Gas = holder.Air;
            args.Handled = true;
        }
    }

    private void OnInhaleLocation(EntityUid uid, BeingDisposedComponent component, InhaleLocationEvent args)
    {
        if (TryComp<DisposalHolderComponent>(component.Holder, out var holder))
        {
            args.Gas = holder.Air;
        }
    }

    private void OnExhaleLocation(EntityUid uid, BeingDisposedComponent component, ExhaleLocationEvent args)
    {
        if (TryComp<DisposalHolderComponent>(component.Holder, out var holder))
        {
            args.Gas = holder.Air;
        }
    }
}
