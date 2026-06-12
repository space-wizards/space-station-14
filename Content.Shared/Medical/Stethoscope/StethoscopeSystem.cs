using Content.Shared.Actions;
// Begin Offbrand - part-aware stethoscope
using Content.Shared._Offbrand.Medical;
using Content.Shared._Offbrand.Skeletons;
using Content.Shared.Body;
using Content.Shared.Examine;
// End Offbrand - part-aware stethoscope
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Inventory;
using Content.Shared.Localizations;
using Content.Shared.Medical.Stethoscope.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Containers;
// Begin Offbrand - part-aware stethoscope
using Robust.Shared.Utility;
// End Offbrand - part-aware stethoscope

namespace Content.Shared.Medical.Stethoscope;

public sealed partial class StethoscopeSystem : EntitySystem
{
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    // Begin Offbrand Changes - part-aware stethoscope
    [Dependency] private ExamineSystemShared _examine = default!;
    [Dependency] private EntityQuery<ParentOrganComponent> _parentOrganQuery = default!;
    // End Offbrand Changes - part-aware stethoscope

    // The damage type to "listen" for with the stethoscope.
    private const string DamageToListenFor = "Asphyxiation";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StethoscopeComponent, InventoryRelayedEvent<GetVerbsEvent<InnateVerb>>>(AddStethoscopeVerb);
        SubscribeLocalEvent<StethoscopeComponent, GetItemActionsEvent>(OnGetActions);
        SubscribeLocalEvent<StethoscopeComponent, StethoscopeActionEvent>(OnStethoscopeAction);
        SubscribeLocalEvent<StethoscopeComponent, StethoscopeDoAfterEvent>(OnDoAfter);
    }

    private void OnGetActions(Entity<StethoscopeComponent> ent, ref GetItemActionsEvent args)
    {
        args.AddAction(ref ent.Comp.ActionEntity, ent.Comp.Action);
    }

    private void OnStethoscopeAction(Entity<StethoscopeComponent> ent, ref StethoscopeActionEvent args)
    {
        StartListening(ent, args.Target);
    }

    private void AddStethoscopeVerb(Entity<StethoscopeComponent> ent, ref InventoryRelayedEvent<GetVerbsEvent<InnateVerb>> args)
    {
        if (!args.Args.CanInteract || !args.Args.CanAccess)
            return;

        // Begin Offbrand - part-aware stethoscope
        if (!HasComp<BodyComponent>(args.Args.Target) &&
            !HasComp<OrganComponent>(args.Args.Target) &&
            !HasComp<MobStateComponent>(args.Args.Target))
        {
            return;
        }
        // End Offbrand - part-aware stethoscope

        var target = args.Args.Target;

        InnateVerb verb = new()
        {
            Act = () => StartListening(ent, target),
            Text = Loc.GetString("stethoscope-verb"),
            IconEntity = GetNetEntity(ent),
            Priority = 2,
        };
        args.Args.Verbs.Add(verb);
    }

    private void StartListening(Entity<StethoscopeComponent> ent, EntityUid target)
    {
        if (!_container.TryGetContainingContainer((ent, null, null), out var container))
            return;

        // Begin Offbrand - part-aware stethoscope
        if (HasComp<BodyComponent>(target))
        {
            var markup = new FormattedMessage();
            _examine.SendExamineTooltip(container.Owner, target, markup, false, true, true);
            return;
        }
        // End Offbrand - part-aware stethoscope

        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, container.Owner, ent.Comp.Delay, new StethoscopeDoAfterEvent(), ent, target: target, used: ent)
        {
            DuplicateCondition = DuplicateConditions.SameEvent,
            BreakOnMove = true,
            Hidden = true,
            BreakOnHandChange = false,
        });
    }

    private void OnDoAfter(Entity<StethoscopeComponent> ent, ref StethoscopeDoAfterEvent args)
    {
        var target = args.Target;

        if (args.Handled || target == null || args.Cancelled)
        {
            ent.Comp.LastMeasuredDamage = null;
            return;
        }

        ExamineWithStethoscope(ent, args.Args.User, target.Value);

        args.Repeat = true;
    }

    private void ExamineWithStethoscope(Entity<StethoscopeComponent> stethoscope, EntityUid user, EntityUid target)
    {
        // Begin Offbrand Changes - part-aware stethoscope
        if (HasComp<OrganComponent>(target))
        {
            var ev = new StethoscopeExamineEvent(new());

            RaiseLocalEvent(target, ref ev);

            if (_parentOrganQuery.TryGetComponent(target, out var parent))
            {
                foreach (var child in parent.Children)
                {
                    if (Exists(child))
                        RaiseLocalEvent(child, ref ev);
                }
            }

            if (ev.Messages.Count == 0)
                _popup.PopupPredicted(Loc.GetString("stethoscope-nothing"), target, user);
            else
                _popup.PopupPredicted(Loc.GetString("stethoscope-sounds", ("sounds", ContentLocalizationManager.FormatList(ev.Messages))), target, user);

            stethoscope.Comp.LastMeasuredDamage = null;
            return;
        }
        // End Offbrand Changes - part-aware stethoscope

        // TODO: Add check for respirator component when it gets moved to shared.
        // If the mob is dead or cannot asphyxiation damage, the popup shows nothing.
        if (!TryComp<MobStateComponent>(target, out var mobState)                        ||
            _mobState.IsDead(target, mobState)                                           ||
            !_damageable.GetAllDamage(target).DamageDict.TryGetValue(DamageToListenFor, out var asphyxDmg))
        {
            _popup.PopupPredicted(Loc.GetString("stethoscope-nothing"), target, user);
            stethoscope.Comp.LastMeasuredDamage = null;
            return;
        }

        var absString = GetAbsoluteDamageString(asphyxDmg);

        // Don't show the change if this is the first time listening.
        if (stethoscope.Comp.LastMeasuredDamage == null)
        {
            _popup.PopupPredicted(absString, target, user);
        }
        else
        {
            var deltaString = GetDeltaDamageString(stethoscope.Comp.LastMeasuredDamage.Value, asphyxDmg);
            _popup.PopupPredicted(Loc.GetString("stethoscope-combined-status", ("absolute", absString), ("delta", deltaString)), target, user);
        }

        stethoscope.Comp.LastMeasuredDamage = asphyxDmg;
    }

    private string GetAbsoluteDamageString(FixedPoint2 asphyxDmg)
    {
        var msg = (int) asphyxDmg switch
        {
            < 10 => "stethoscope-normal",
            < 30 => "stethoscope-raggedy",
            < 60 => "stethoscope-hyper",
            < 80 => "stethoscope-irregular",
            _    => "stethoscope-fucked",
        };
        return Loc.GetString(msg);
    }

    private string GetDeltaDamageString(FixedPoint2 lastDamage, FixedPoint2 currentDamage)
    {
        if (lastDamage > currentDamage)
            return Loc.GetString("stethoscope-delta-improving");
        if (lastDamage < currentDamage)
            return Loc.GetString("stethoscope-delta-worsening");
        return Loc.GetString("stethoscope-delta-steady");
    }

}

public sealed partial class StethoscopeActionEvent : EntityTargetActionEvent;
