using Content.Server.GameTicking.Rules;
using Content.Shared.CosmicCult;
using Content.Shared.CosmicCult.Components;
using Content.Shared.CosmicCult.Components.Actions;
using Content.Shared.DoAfter;
using Content.Shared.IdentityManagement;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Robust.Shared.Player;

namespace Content.Server.CosmicCult.Abilities;

public sealed partial class CosmicSiphonSystem : EntitySystem
{
    [Dependency] private CosmicCultRuleSystem _cultRule = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    [SubscribeLocalEvent]
    private void OnCosmicSiphon(Entity<CosmicActionSiphonComponent> ent, ref EventCosmicSiphon args)
    {
        if (!TryComp<CosmicCultActionComponent>(ent, out var action))
            return;

        if (TryComp<MobStateComponent>(args.Target, out var state) && state.CurrentState != MobState.Alive)
        {
            _popup.PopupEntity(Loc.GetString("cosmicability-siphon-fail", ("target", Identity.Entity(args.Target, EntityManager))), ent, ent);
            return;
        }

        if (TryComp<ActorComponent>(ent, out var actor))
            RaiseNetworkEvent(new SiphonVisualsEvent(GetNetEntity(args.Target)), actor.PlayerSession);

        Dirty(ent);
        _popup.PopupEntity(Loc.GetString("cosmicability-siphon-success", ("target", Identity.Entity(args.Target, EntityManager))), ent, ent);
        // _cultRule.IncrementCultObjectiveEntropy(args.Performer); TODO: COSMIC CULT OBJECTIVES

        var evt = new CosmicCultistProgressEvent(action.Empowered ? ent.Comp.QuantityEmpowered : ent.Comp.QuantityDefault);
        RaiseLocalEvent(args.Target, ref evt);
        args.Handled = true;
    }
}
