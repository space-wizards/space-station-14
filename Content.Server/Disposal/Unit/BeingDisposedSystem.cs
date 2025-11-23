using Content.Server.Atmos.EntitySystems;
using Content.Server.Body.Systems;
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
    }

    private void OnGetAir(Entity<BeingDisposedComponent> ent, ref AtmosExposedGetAirEvent args)
    {
        if (TryComp<DisposalHolderComponent>(ent.Comp.Holder, out var holder))
        {
            args.Gas = holder.Air;
            args.Handled = true;
        }
    }

    private void OnInhaleLocation(Entity<BeingDisposedComponent> ent, ref InhaleLocationEvent args)
    {
        if (TryComp<DisposalHolderComponent>(ent.Comp.Holder, out var holder))
        {
            args.Gas = holder.Air;
        }
    }

    private void OnExhaleLocation(Entity<BeingDisposedComponent> ent, ref ExhaleLocationEvent args)
    {
        if (TryComp<DisposalHolderComponent>(ent.Comp.Holder, out var holder))
        {
            args.Gas = holder.Air;
        }
    }
}
