using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Actions.Events;
using Content.Shared.EntityTable;
using Content.Shared.Fluids.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Item;
using Content.Shared.Mobs.Components;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Nutrition;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Stacks;
using Content.Shared.Tag;
using Content.Shared.Temperature.Components;
using Content.Shared.Temperature.Systems;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.CF.Spells;

/// <summary>
/// Prestidigitation's four tricks. The click is checked here, not before the menu opens.
/// </summary>
public sealed class CFPrestidigitationSystem : EntitySystem
{
    private const float CookTemperature = 420f;

    private static readonly ProtoId<TagPrototype> RawTag = "Raw";
    private static readonly ProtoId<FlavorPrototype> MagicalFlavor = "cfMagical";
    private static readonly ProtoId<EntityTablePrototype> FunTable = "AllToysTable";
    private static readonly EntProtoId Sparkle = "PuddleSparkle";

    [Dependency] private INetManager _net = default!;
    [Dependency] private IPrototypeManager _prototypes = default!;
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private PullingSystem _pulling = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedTemperatureSystem _temperature = default!;
    [Dependency] private TagSystem _tags = default!;
    [Dependency] private EntityTableSystem _tables = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CFSpellSeasoningComponent, FlavorProfileModificationEvent>(OnFlavor);
        SubscribeAllEvent<CFPrestidigitationMakeEvent>(OnMake);
    }

    public bool TryTrick(EntityUid user, EntityUid target, byte trick)
    {
        switch ((CFPrestidigitationTrick) trick)
        {
            case CFPrestidigitationTrick.Food:
                return TryFood(user, target);
            case CFPrestidigitationTrick.Lift:
                return TryLift(user, target);
            case CFPrestidigitationTrick.Tidy:
                return TryTidy(user, target);
            default:
                return false;
        }
    }

    private bool TryFood(EntityUid user, EntityUid target)
    {
        if (!HasComp<EdibleComponent>(target))
        {
            Fail(user, "cf-spell-prestidigitation-food");
            return false;
        }

        if (!_net.IsServer)
            return true;

        var seasoning = EnsureComp<CFSpellSeasoningComponent>(target);
        Dirty(target, seasoning);

        var cooked = _tags.HasTag(target, RawTag) && HasComp<TemperatureComponent>(target);
        if (cooked)
            _temperature.SetCookedTemperature(target, CookTemperature);

        _popup.PopupEntity(
            Loc.GetString(cooked ? "cf-spell-prestidigitation-cooked" : "cf-spell-prestidigitation-flavoured"),
            user,
            user);
        return true;
    }

    private bool TryLift(EntityUid user, EntityUid target)
    {
        if (target == user || HasComp<MobStateComponent>(target))
        {
            Fail(user, "cf-spell-prestidigitation-mob");
            return false;
        }

        if (HasComp<StackComponent>(target))
        {
            Fail(user, "cf-spell-prestidigitation-stack");
            return false;
        }

        if (HasComp<ItemComponent>(target) && _hands.TryGetEmptyHand(user, out _)
            && _hands.TryPickupAnyHand(user, target, checkActionBlocker: false, animate: false))
        {
            if (_net.IsServer)
                _popup.PopupEntity(Loc.GetString("cf-spell-prestidigitation-hand"), user, user);

            return true;
        }

        if (!_pulling.TryStartPull(user, target))
        {
            Fail(user, "cf-spell-prestidigitation-lift");
            return false;
        }

        if (_net.IsServer)
            _popup.PopupEntity(Loc.GetString("cf-spell-prestidigitation-pull"), user, user);

        return true;
    }

    private bool TryTidy(EntityUid user, EntityUid target)
    {
        if (!HasComp<PuddleComponent>(target))
        {
            Fail(user, "cf-spell-prestidigitation-puddle");
            return false;
        }

        PredictedSpawnAttachedTo(Sparkle, Transform(target).Coordinates);
        QueueDel(target);

        if (_net.IsServer)
            _popup.PopupEntity(Loc.GetString("cf-spell-prestidigitation-tidy"), user, user);

        return true;
    }

    private void OnFlavor(Entity<CFSpellSeasoningComponent> ent, ref FlavorProfileModificationEvent args)
    {
        if (!ent.Comp.Magical)
            return;

        args.Flavors.Add(MagicalFlavor);
    }

    private void OnMake(CFPrestidigitationMakeEvent ev, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { } user)
            return;

        var action = GetEntity(ev.Action);
        if (!TryComp(action, out CFSpellComponent? spell) || !spell.TrickMenu)
            return;

        if (!TryComp(action, out ActionComponent? actionComp) || actionComp.AttachedEntity != user)
            return;

        if (_actions.IsCooldownActive(actionComp))
            return;

        var attempt = new ActionAttemptEvent(user, null);
        RaiseLocalEvent(action, ref attempt);
        if (attempt.Cancelled)
        {
            if (_net.IsServer && attempt.Reason != null)
                _popup.PopupEntity(attempt.Reason, user, user);

            return;
        }

        if (_net.IsServer && !TrySpawnFun(user))
            return;

        _actions.StartUseDelay((action, actionComp));
        var performed = new ActionPerformedEvent(user, null);
        RaiseLocalEvent(action, ref performed);
    }

    private bool TrySpawnFun(EntityUid user)
    {
        if (!_prototypes.TryIndex(FunTable, out EntityTablePrototype? table))
            return false;

        var pick = _tables.GetFirstOrNull(table);
        if (pick == null)
            return false;

        var spawned = Spawn(pick.Value, Transform(user).Coordinates);
        var held = _hands.TryPickupAnyHand(user, spawned, checkActionBlocker: false, animate: false);
        _popup.PopupEntity(
            Loc.GetString(held ? "cf-spell-prestidigitation-made" : "cf-spell-prestidigitation-made-ground"),
            user,
            user);
        return true;
    }

    private void Fail(EntityUid user, string message)
    {
        if (_net.IsServer)
            _popup.PopupEntity(Loc.GetString(message), user, user);
    }
}

[Serializable, NetSerializable]
public sealed class CFPrestidigitationMakeEvent : EntityEventArgs
{
    public NetEntity Action;

    public CFPrestidigitationMakeEvent(NetEntity action)
    {
        Action = action;
    }
}
