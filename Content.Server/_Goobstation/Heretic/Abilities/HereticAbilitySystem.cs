using Content.Server.Atmos.EntitySystems;
using Content.Server.Chat.Systems;
using Content.Server.DoAfter;
using Content.Server.Flash;
using Content.Server.Hands.Systems;
using Content.Server.Magic;
using Content.Server.Polymorph.Systems;
using Content.Server.Popups;
using Content.Server.Radio.Components;
using Content.Server.Store.Systems;
using Content.Shared.Actions;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Heretic;
using Content.Shared.Inventory;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Store.Components;
using Robust.Shared.Audio.Systems;
using Content.Shared.Popups;
using Robust.Shared.Random;
using Content.Shared.Body.Systems;
using Content.Server.Medical;
using Robust.Server.GameObjects;
using Content.Shared.Stunnable;
using Robust.Shared.Map;
using Content.Shared.StatusEffect;
using Content.Shared.Throwing;
using Content.Server.Station.Systems;
using Content.Shared.Localizations;
using Robust.Shared.Audio;
using Content.Shared.Mobs.Components;
using Robust.Shared.Prototypes;
using Content.Shared.Eye.Blinding.Systems;

namespace Content.Server.Heretic.Abilities;

public sealed partial class HereticAbilitySystem : EntitySystem
{
    // keeping track of all systems in a single file
    [Dependency] private readonly StoreSystem _store = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly HandsSystem _hands = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly PolymorphSystem _poly = default!;
    [Dependency] private readonly ChainFireballSystem _splitball = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly MobStateSystem _mobstate = default!;
    [Dependency] private readonly FlammableSystem _flammable = default!;
    [Dependency] private readonly DamageableSystem _dmg = default!;
    [Dependency] private readonly StaminaSystem _stam = default!;
    [Dependency] private readonly AtmosphereSystem _atmos = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    [Dependency] private readonly SharedAudioSystem _aud = default!;
    [Dependency] private readonly DoAfterSystem _doafter = default!;
    [Dependency] private readonly FlashSystem _flash = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly BlindableSystem _blindable = default!;
    [Dependency] private readonly VomitSystem _vomit = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffect = default!;
    [Dependency] private readonly PhysicsSystem _phys = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly ThrowingSystem _throw = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly IMapManager _mapMan = default!;
    [Dependency] private readonly IPrototypeManager _prot = default!;

    private List<EntityUid> GetNearbyPeople(Entity<HereticComponent> ent, float range)
    {
        var list = new List<EntityUid>();
        var lookup = _lookup.GetEntitiesInRange(Transform(ent).Coordinates, range);

        foreach (var look in lookup)
        {
            // ignore heretics with the same path*, affect everyone else
            if ((TryComp<HereticComponent>(look, out var th) && th.CurrentPath == ent.Comp.CurrentPath)
            || HasComp<GhoulComponent>(look))
                continue;

            if (!HasComp<StatusEffectsComponent>(look))
                continue;

            list.Add(look);
        }
        return list;
    }

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HereticComponent, EventHereticOpenStore>(OnStore);
        SubscribeLocalEvent<HereticComponent, EventHereticMansusGrasp>(OnMansusGrasp);

        SubscribeLocalEvent<GhoulComponent, EventHereticMansusLink>(OnMansusLink);
        SubscribeLocalEvent<GhoulComponent, HereticMansusLinkDoAfter>(OnMansusLinkDoafter);

        SubscribeAsh();
        SubscribeFlesh();
        SubscribeVoid();
    }

    private bool TryUseAbility(EntityUid ent, BaseActionEvent args)
    {
        if (args.Handled)
            return false;

        if (!TryComp<HereticActionComponent>(args.Action, out var actionComp))
            return false;

        // check if any magic items are worn
        if (TryComp<HereticComponent>(ent, out var hereticComp) && actionComp.RequireMagicItem && !hereticComp.Ascended)
        {
            var ev = new CheckMagicItemEvent();
            RaiseLocalEvent(ent, ev);

            if (!ev.Handled)
            {
                _popup.PopupEntity(Loc.GetString("heretic-ability-fail-magicitem"), ent, ent);
                return false;
            }
        }

        // shout the spell out
        if (!string.IsNullOrWhiteSpace(actionComp.MessageLoc))
            _chat.TrySendInGameICMessage(ent, Loc.GetString(actionComp.MessageLoc!), InGameICChatType.Speak, false);

        return true;
    }
    private void OnStore(Entity<HereticComponent> ent, ref EventHereticOpenStore args)
    {
        if (!TryComp<StoreComponent>(ent, out var store))
            return;

        _store.ToggleUi(ent, ent, store);
    }
    private void OnMansusGrasp(Entity<HereticComponent> ent, ref EventHereticMansusGrasp args)
    {
        if (!TryUseAbility(ent, args))
            return;

        if (ent.Comp.MansusGraspActive)
        {
            _popup.PopupEntity(Loc.GetString("heretic-ability-fail"), ent, ent);
            return;
        }

        var st = Spawn("TouchSpellMansus", Transform(ent).Coordinates);

        if (!_hands.TryForcePickupAnyHand(ent, st))
        {
            _popup.PopupEntity(Loc.GetString("heretic-ability-fail"), ent, ent);
            QueueDel(st);
            return;
        }

        ent.Comp.MansusGraspActive = true;
        args.Handled = true;
    }

    private void OnMansusLink(Entity<GhoulComponent> ent, ref EventHereticMansusLink args)
    {
        if (!TryUseAbility(ent, args))
            return;

        if (!HasComp<MindContainerComponent>(args.Target))
        {
            _popup.PopupEntity(Loc.GetString("heretic-manselink-fail-nomind"), ent, ent);
            return;
        }

        if (TryComp<ActiveRadioComponent>(args.Target, out var radio)
        && radio.Channels.Contains("Mansus"))
        {
            _popup.PopupEntity(Loc.GetString("heretic-manselink-fail-exists"), ent, ent);
            return;
        }

        var dargs = new DoAfterArgs(EntityManager, ent, 5f, new HereticMansusLinkDoAfter(args.Target), ent, args.Target)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            BreakOnWeightlessMove = true
        };
        _popup.PopupEntity(Loc.GetString("heretic-manselink-start"), ent, ent);
        _popup.PopupEntity(Loc.GetString("heretic-manselink-start-target"), args.Target, args.Target, PopupType.MediumCaution);
        _doafter.TryStartDoAfter(dargs);
    }
    private void OnMansusLinkDoafter(Entity<GhoulComponent> ent, ref HereticMansusLinkDoAfter args)
    {
        if (args.Cancelled)
            return;

        var reciever = EnsureComp<IntrinsicRadioReceiverComponent>(args.Target);
        var transmitter = EnsureComp<IntrinsicRadioTransmitterComponent>(args.Target);
        var radio = EnsureComp<ActiveRadioComponent>(args.Target);
        radio.Channels = new() { "Mansus" };
        transmitter.Channels = new() { "Mansus" };

        // this "* 1000f" (divided by 1000 in FlashSystem) is gonna age like fine wine :clueless:
        _flash.Flash(args.Target, null, null, 2f * 1000f, 0f, false, true, stunDuration: TimeSpan.FromSeconds(1f));
    }
}
