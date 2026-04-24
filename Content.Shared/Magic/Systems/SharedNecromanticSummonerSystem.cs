using Content.Shared.Charges.Components;
using Content.Shared.Charges.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Gibbing;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Magic.Components;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared.Magic.Systems;

public abstract class SharedNecromanticSummonerSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly GibbingSystem _gibbing = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedChargesSystem _charges = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NecromanticSummonerComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<NecromanticSummonerComponent, NecromanticSummonerDoAfterEvent>(OnDoAfter);
    }

    private void OnAfterInteract(Entity<NecromanticSummonerComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target is not { } target)
            return;

        args.Handled = true;

        if (!CanSummon(ent, target, args.User))
            return;

        var doAfterEventArgs = new DoAfterArgs(EntityManager, args.User, ent.Comp.DoAfterTime, new NecromanticSummonerDoAfterEvent(), ent.Owner, target: args.Target, used: ent.Owner)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
        };

        if (!_doAfter.TryStartDoAfter(doAfterEventArgs))
            return;

        var doAfterMessage = Loc.GetString(ent.Comp.DoAfterPopup, ("target", Identity.Entity(target, EntityManager)));
        _popup.PopupClient(doAfterMessage, target, args.User);
    }

    private void OnDoAfter(Entity<NecromanticSummonerComponent> ent, ref NecromanticSummonerDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Target is not { } target)
            return;

        args.Handled = true;

        // Check again to make sure they were not revived in the meantime.
        if (!CanSummon(ent, target, args.User))
            return;

        // Klatuu! Barada! Necktie!
        Summon(ent, target, args.User);
    }

    /// <summary>
    /// Check if the target mob can be used for summoning.
    /// </summary>
    public bool CanSummon(Entity<NecromanticSummonerComponent> ent, EntityUid target, EntityUid user)
    {
        if (!_whitelist.CheckBoth(target, ent.Comp.Blacklist, ent.Comp.Whitelist))
            return false;

        if (_charges.IsEmpty(ent.Owner))
        {
            var emptyMessage = Loc.GetString(
                ent.Comp.NoChargesPopup,
                ("target", Identity.Entity(target, EntityManager)),
                ("used", ent.Owner));
            _popup.PopupClient(emptyMessage, target, user);
            return false;
        }

        if (!_mobState.IsDead(target)) // We are doing necromancy, duh!
        {
            var notDeadMessage = Loc.GetString(
                ent.Comp.NotDeadPopup,
                ("target", Identity.Entity(target, EntityManager)),
                ("used", ent.Owner));
            _popup.PopupClient(notDeadMessage, target, user);
            return false;
        }

        if (!TryComp<MindContainerComponent>(target, out var mindContainer) || !mindContainer.HasMind)
        {
            var noSoulMessage = Loc.GetString(
                ent.Comp.NoSoulPopup,
                ("target", Identity.Entity(target, EntityManager)),
                ("used", ent.Owner));
            _popup.PopupClient(noSoulMessage, target, user);
            return false; // We want our minion to be controlled by a player.
        }

        return true;
    }

    /// <summary>
    /// Summon a minion by spawning a new mob and transferring the target player to it.
    /// </summary>
    public void Summon(Entity<NecromanticSummonerComponent> ent, EntityUid target, EntityUid user)
    {
        if (TryComp<LimitedChargesComponent>(ent, out var chargesComp) && !_charges.TryUseCharge((ent.Owner, chargesComp)))
            return;

        var coords = Transform(target).Coordinates;
        // Use coords because the entity will be deleted.
        _audio.PlayPredicted(ent.Comp.SummonSound, coords, user);
        var summonUserMessage = Loc.GetString(
            ent.Comp.SummonUserPopup,
            ("target", Identity.Entity(target, EntityManager)),
            ("user", Identity.Entity(user, EntityManager)),
            ("used", ent.Owner));
        var summonTargetMessage = Loc.GetString(ent.Comp.SummonTargetPopup,
            ("target", Identity.Entity(target, EntityManager)),
            ("user", Identity.Entity(user, EntityManager)),
            ("used", ent.Owner));
        var summonOthersMessage = Loc.GetString(ent.Comp.SummonOthersPopup,
            ("target", Identity.Entity(target, EntityManager)),
            ("user", Identity.Entity(user, EntityManager)),
            ("used", ent.Owner));

        // TODO: Make the popup API sane.
        if (_net.IsClient && _timing.IsFirstTimePredicted)
            _popup.PopupCoordinates(summonUserMessage, coords, user, PopupType.Large);
        _popup.PopupCoordinates(summonTargetMessage, coords, target, PopupType.LargeCaution);
        var othersFilter = Filter.Empty().AddPlayersByPvs(target).RemovePlayersByAttachedEntity([user, target]);
        _popup.PopupCoordinates(summonOthersMessage, coords, othersFilter, true, PopupType.LargeCaution);

        SpawnSummonAndTransferPlayer(ent.Comp.Prototype, coords, target);

        // Gib the old body.
        if (ent.Comp.GibBody)
            _gibbing.Gib(target, user: user);
    }

    /// <summary>
    /// Spawns the summoned mob and transfers the player used to spawn it over.
    /// If the spawned mob has GhostRoleComponent, that ghost role is applied,
    /// otherwise we simply transfer the existing mind.
    /// </summary>
    public virtual void SpawnSummonAndTransferPlayer(EntProtoId summonPrototype, EntityCoordinates coords, EntityUid target) { }
}


/// <summary>
/// DoAfter event raised when an item with <see cref="NecromanticSummonerComponent"/> is used on a dead mob.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class NecromanticSummonerDoAfterEvent() : SimpleDoAfterEvent;
