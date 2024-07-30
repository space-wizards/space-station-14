using Content.Shared.Chemistry.Components;
using Content.Shared.Timing;
using Content.Shared.Verbs;
using Content.Shared.Popups;
using Robust.Shared.Player;
using Content.Shared.Administration.Logs;

namespace Content.Shared.Chemistry.EntitySystems;

public abstract class SharedHypospraySystem : EntitySystem
{
    [Dependency] protected readonly UseDelaySystem _useDelay = default!;
    [Dependency] protected readonly SharedPopupSystem _popup = default!;
    [Dependency] protected readonly SharedSolutionContainerSystem _solutionContainers = default!;
    [Dependency] protected readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] protected readonly ReactiveSystem _reactiveSystem = default!;
    
    public override void Initialize()
    {
        SubscribeLocalEvent<HyposprayComponent, GetVerbsEvent<AlternativeVerb>>(AddToggleModeVerb);
    }

    // <summary>
    // Uses the OnlyMobs field as a check to implement the ability
    // to draw from jugs and containers with the hypospray
    // Toggleable to allow people to inject containers if they prefer it over drawing
    // </summary>
    private void AddToggleModeVerb(Entity<HyposprayComponent> entity, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Hands == null || entity.Comp.InjectOnly)
            return;

        var (_, component) = entity;
        var user = args.User;
        var verb = new AlternativeVerb
        {
            Text = Loc.GetString("hypospray-verb-mode-label"),
            Act = () =>
            {
                ToggleMode(entity, user);
            }
        };
        args.Verbs.Add(verb);
    }

    private void ToggleMode(Entity<HyposprayComponent> entity, EntityUid user)
    {
        SetMode(entity, !entity.Comp.OnlyAffectsMobs);
        string msg = entity.Comp.OnlyAffectsMobs ? "hypospray-verb-mode-inject-mobs-only" : "hypospray-verb-mode-inject-all";
        _popup.PopupClient(Loc.GetString(msg), entity, user);
    }

    public void SetMode(Entity<HyposprayComponent> entity, bool onlyAffectsMobs)
    {
        if (entity.Comp.OnlyAffectsMobs == onlyAffectsMobs)
            return;

        entity.Comp.OnlyAffectsMobs = onlyAffectsMobs;
        Dirty(entity);
    }
}
