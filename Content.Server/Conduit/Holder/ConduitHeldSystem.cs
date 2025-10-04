using Content.Server.Atmos.EntitySystems;
using Content.Server.Body.Systems;
using Content.Shared.Conduit.Holder;

namespace Content.Server.Conduit.Holder;

/// <inheritdoc/>
public sealed class ConduitHeldSystem : SharedConduitHeldSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ConduitHeldComponent, InhaleLocationEvent>(OnInhaleLocation);
        SubscribeLocalEvent<ConduitHeldComponent, ExhaleLocationEvent>(OnExhaleLocation);
        SubscribeLocalEvent<ConduitHeldComponent, AtmosExposedGetAirEvent>(OnGetAir);
    }

    private void OnGetAir(Entity<ConduitHeldComponent> ent, ref AtmosExposedGetAirEvent args)
    {
        if (TryComp<ConduitHolderComponent>(ent.Comp.Holder, out var holder))
        {
            args.Gas = holder.Air;
            args.Handled = true;
        }
    }

    private void OnInhaleLocation(Entity<ConduitHeldComponent> ent, ref InhaleLocationEvent args)
    {
        if (TryComp<ConduitHolderComponent>(ent.Comp.Holder, out var holder))
        {
            args.Gas = holder.Air;
        }
    }

    private void OnExhaleLocation(Entity<ConduitHeldComponent> ent, ref ExhaleLocationEvent args)
    {
        if (TryComp<ConduitHolderComponent>(ent.Comp.Holder, out var holder))
        {
            args.Gas = holder.Air;
        }
    }
}
