using Content.Shared.Actions;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Inventory;
using Content.Shared.Medical.Stethoscope.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Containers;

namespace Content.Shared.Medical.Stethoscope;

public sealed class StethoscopeSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;

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

        if (!HasComp<MobStateComponent>(args.Args.Target))
            return;

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
        // TODO: Add check for respirator component when it gets moved to shared.
        // If the mob is dead or cannot asphyxiation damage, the popup shows nothing.
        if (!TryComp<MobStateComponent>(target, out var mobState)                        ||
            !TryComp<DamageableComponent>(target, out var damageComp) ||
            _mobState.IsDead(target, mobState)                                           ||
            !damageComp.Damage.DamageDict.TryGetValue(DamageToListenFor, out var asphyxDmg))
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
