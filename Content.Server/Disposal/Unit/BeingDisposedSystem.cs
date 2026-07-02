using Content.Server.Body.Systems;
using Content.Shared.Atmos;
using Content.Shared.Disposal.Unit;

namespace Content.Server.Disposal.Unit;

/// <inheritdoc/>
public sealed class BeingDisposedSystem : SharedBeingDisposedSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BeingDisposedComponent, InhaleLocationEvent>(OnInhaleLocation);
        SubscribeLocalEvent<BeingDisposedComponent, ExhaleLocationEvent>(OnExhaleLocation);
        SubscribeLocalEvent<BeingDisposedComponent, AtmosExposedGetAirEvent>(OnGetAir);
        SubscribeLocalEvent<DisposalHolderComponent, GetBeingDisposedGasEvent>(OnGetBeingDisposedGas);
    }

    private void OnGetAir(Entity<BeingDisposedComponent> ent, ref AtmosExposedGetAirEvent args)
    {
        var ev = new GetBeingDisposedGasEvent();
        RaiseLocalEvent(ent.Comp.Holder, ref ev);

        if (ev.Gas == null)
            return;

        args.Gas = ev.Gas;
        args.Handled = true;
    }

    private void OnInhaleLocation(Entity<BeingDisposedComponent> ent, ref InhaleLocationEvent args)
    {
        var ev = new GetBeingDisposedGasEvent();
        RaiseLocalEvent(ent.Comp.Holder, ref ev);

        if (ev.Gas != null)
            args.Gas = ev.Gas;
    }

    private void OnExhaleLocation(Entity<BeingDisposedComponent> ent, ref ExhaleLocationEvent args)
    {
        var ev = new GetBeingDisposedGasEvent();
        RaiseLocalEvent(ent.Comp.Holder, ref ev);

        if (ev.Gas != null)
            args.Gas = ev.Gas;
    }

    private void OnGetBeingDisposedGas(Entity<DisposalHolderComponent> ent, ref GetBeingDisposedGasEvent args)
    {
        args.Gas = ent.Comp.Air;
    }
}
