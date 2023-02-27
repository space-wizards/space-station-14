using Robust.Shared.Network;

namespace Content.Shared.Ninja.Systems;

public sealed class SpaceNinjaSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpaceNinjaComponent, ComponentGetState>(OnNinjaGetState);
        SubscribeLocalEvent<SpaceNinjaComponent, ComponentHandleState>(OnNinjaHandleState);
    }

    /// <summary>
    /// Bind a katana entity to a ninja, letting it be recalled and dash.
    /// </summary>
    public void BindKatana(SpaceNinjaComponent comp, EntityUid katana)
    {
        comp.Katana = katana;
    }

    // TODO: remove when objective stuff moved into objectives somehow
    public void DetonateSpiderCharge(SpaceNinjaComponent comp)
    {
        comp.SpiderChargeDetonated = true;
    }

    private void OnNinjaGetState(EntityUid uid, SpaceNinjaComponent comp, ref ComponentGetState args)
    {
        args.State = new SpaceNinjaComponentState(comp.DoorsJacked, comp.DownloadedNodes, comp.SpiderChargeTarget, comp.SpiderChargeDetonated, comp.CalledInThreat);
    }

    private void OnNinjaHandleState(EntityUid uid, SpaceNinjaComponent comp, ref ComponentHandleState args)
    {
        if (args.Current is not SpaceNinjaComponentState state)
            return;

        comp.DoorsJacked = state.DoorsJacked;
        comp.DownloadedNodes = state.DownloadedNodes;
        comp.SpiderChargeTarget = state.SpiderChargeTarget;
        comp.SpiderChargeDetonated = state.SpiderChargeDetonated;
        comp.CalledInThreat = state.CalledInThreat;
    }
}
