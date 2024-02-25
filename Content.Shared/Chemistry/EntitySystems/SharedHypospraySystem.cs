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
    [Dependency] protected readonly IEntityManager _entMan = default!;
    [Dependency] protected readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] protected readonly ReactiveSystem _reactiveSystem = default!;
    public override void Initialize()
    {
        SubscribeLocalEvent<HyposprayComponent, GetVerbsEvent<AlternativeVerb>>(AddToggleModeVerb);
    }

    //
    // Uses the OnlyMobs field as a check to implement the ability
    // to draw from jugs and containers with the hypospray
    // Toggleable to allow people to inject containers if they prefer it over drawing
    //
    private void AddToggleModeVerb(Entity<HyposprayComponent> entity, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Hands == null)
            return;

        if (!HasComp<ActorComponent>(args.User))
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
        string msg;
        switch (entity.Comp.ToggleMode)
        {
            case HyposprayToggleMode.All:
                SetMode(entity, HyposprayToggleMode.OnlyMobs);
                msg = "hypospray-verb-mode-inject-mobs-only";
                break;
            case HyposprayToggleMode.OnlyMobs:
                SetMode(entity, HyposprayToggleMode.All);
                msg = "hypospray-verb-mode-inject-all";
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        _popup.PopupEntity(Loc.GetString(msg), entity, user);
    }

    public void SetMode(Entity<HyposprayComponent> entity, HyposprayToggleMode mode)
    {
        entity.Comp.ToggleMode = mode;
        Dirty(entity);
    }
}
