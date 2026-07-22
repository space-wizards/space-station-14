// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.Damage.Systems;
using Content.Shared.DeadSpace.Traps;
using Content.Shared.DoAfter;
using Content.Shared.Ensnaring.Components;
using Content.Shared.Ensnaring;
using Content.Shared.NPC.Components;
using Content.Shared.NPC.Systems;
using Content.Shared.Stunnable;
using Content.Shared.Verbs;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;
using Robust.Shared.Physics.Events;
using Robust.Shared.Timing;
using System.Numerics;

namespace Content.Server.DeadSpace.Traps;

public sealed class BearTrapSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedContainerSystem _containers = default!;
    [Dependency] private readonly NpcFactionSystem _factions = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly SharedEnsnareableSystem _ensnare = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<BearTrapComponent, GetVerbsEvent<AlternativeVerb>>(OnGetVerbs);
        SubscribeLocalEvent<BearTrapComponent, StartCollideEvent>(OnCollide);
        SubscribeLocalEvent<BearTrapComponent, BearTrapDisarmDoAfterEvent>(OnDisarmed);
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<BearTrapComponent>();
        while (query.MoveNext(out var uid, out var trap))
        {
            if (!trap.Arming && !trap.Armed &&
                TryComp<EnsnaringComponent>(uid, out var inactive) && inactive.Ensnared == null)
            {
                RemComp<EnsnaringComponent>(uid);
                continue;
            }

            if (trap.Armed && CheckCurrentOverlaps((uid, trap)))
                continue;

            if (!trap.Arming || trap.ArmsAt == null)
                continue;

            var left = trap.ArmsAt.Value - _timing.CurTime;
            var progress = 1f - Math.Clamp((float) (left.TotalSeconds / trap.ArmingTime.TotalSeconds), 0f, 1f);
            var opacity = MathHelper.Lerp(1f, trap.MinimumOpacity, progress);
            if (Math.Abs(trap.Opacity - opacity) >= 0.05f)
            {
                trap.Opacity = opacity;
                Dirty(uid, trap);
            }
            if (left > TimeSpan.Zero)
                continue;

            trap.Arming = false;
            trap.Opacity = trap.MinimumOpacity;
            trap.ArmsAt = null;
            _appearance.SetData(uid, BearTrapVisuals.Armed, true);
            Dirty(uid, trap);
        }
    }

    private void OnGetVerbs(Entity<BearTrapComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || ent.Comp.Used || _containers.IsEntityInContainer(ent))
            return;

        var user = args.User;
        if (ent.Comp.Armed)
        {
            args.Verbs.Add(new AlternativeVerb
            {
                Text = Loc.GetString("bear-trap-disarm-verb"),
                Act = () => StartDisarm(ent, user),
            });
            return;
        }

        if (ent.Comp.Arming)
            return;

        args.Verbs.Add(new AlternativeVerb
        {
            Text = Loc.GetString("bear-trap-arm-verb"),
            Act = () => Arm(ent, user),
        });
    }

    private void StartDisarm(Entity<BearTrapComponent> ent, EntityUid user)
    {
        var args = new DoAfterArgs(EntityManager, user, ent.Comp.DisarmTime,
            new BearTrapDisarmDoAfterEvent(), ent, target: ent)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
            DistanceThreshold = 2f,
        };
        _doAfter.TryStartDoAfter(args);
    }

    private void OnDisarmed(Entity<BearTrapComponent> ent, ref BearTrapDisarmDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || !ent.Comp.Armed)
            return;

        args.Handled = true;
        ent.Comp.Used = true;
        ent.Comp.Armed = false;
        ent.Comp.Arming = false;
        ent.Comp.ArmsAt = null;
        ent.Comp.Installer = null;
        ent.Comp.Opacity = 1f;
        _appearance.SetData(ent, BearTrapVisuals.Armed, false);
        if (TryComp<EnsnaringComponent>(ent, out var ensnaring) && ensnaring.Ensnared == null)
            RemComp<EnsnaringComponent>(ent);
        _transform.Unanchor(ent);
        _transform.SetLocalRotation(ent, Angle.Zero);
        Dirty(ent);
    }

    private void Arm(Entity<BearTrapComponent> ent, EntityUid user)
    {
        ent.Comp.Arming = true;
        ent.Comp.Armed = true;
        ent.Comp.ArmsAt = _timing.CurTime + ent.Comp.ArmingTime;
        ent.Comp.Installer = user;
        _appearance.SetData(ent, BearTrapVisuals.Armed, true);
        _transform.AnchorEntity(ent);
        _transform.SetLocalRotation(ent, Angle.Zero);
        ConfigureEnsnaring(ent);
        ent.Comp.Opacity = 1f;
        Dirty(ent);
    }

    private void OnCollide(Entity<BearTrapComponent> ent, ref StartCollideEvent args)
    {
        var target = args.OtherEntity;
        if (!args.OtherFixture.Hard || target == ent.Comp.Installer)
            return;

        TryCatch(ent, target);
    }

    private bool CheckCurrentOverlaps(Entity<BearTrapComponent> ent)
    {
        var xform = Transform(ent);
        var box = Box2.CenteredAround(_transform.GetWorldPosition(ent), new Vector2(0.8f, 0.8f));
        var installerPresent = false;

        foreach (var target in _lookup.GetEntitiesIntersecting(xform.MapID, box,
                     LookupFlags.Dynamic | LookupFlags.Sundries))
        {
            if (target == ent.Owner)
                continue;

            if (target == ent.Comp.Installer)
            {
                installerPresent = true;
                continue;
            }

            if (TryCatch(ent, target))
                return true;
        }

        if (!installerPresent)
            ent.Comp.Installer = null;

        return false;
    }

    private bool TryCatch(Entity<BearTrapComponent> ent, EntityUid target)
    {
        if (!ent.Comp.Armed || IsIgnored(ent, target) ||
            !TryComp<EnsnaringComponent>(ent, out var ensnaring) ||
            !_ensnare.TryEnsnare(target, ent, ensnaring))
            return false;

        ent.Comp.Installer = null;
        ent.Comp.Used = true;
        ent.Comp.Arming = false;
        ent.Comp.Armed = false;
        ent.Comp.ArmsAt = null;
        ent.Comp.Opacity = 1f;
        _appearance.SetData(ent, BearTrapVisuals.Armed, false);
        _transform.SetLocalRotation(ent, Angle.Zero);
        Dirty(ent);
        _damage.TryChangeDamage(target, ent.Comp.Damage, origin: ent);
        _stun.TryUpdateParalyzeDuration(target, ent.Comp.StunTime);
        return true;
    }

    private bool IsIgnored(EntityUid trap, EntityUid target)
    {
        if (!TryComp<TrapIgnoreComponent>(trap, out var ignore))
            return false;

        if (ignore.Whitelist != null && _whitelist.IsValid(ignore.Whitelist, target))
            return true;

        return ignore.Factions.Count > 0 &&
               TryComp<NpcFactionMemberComponent>(target, out var member) &&
               _factions.IsMemberOfAny((target, member), ignore.Factions);
    }

    private EnsnaringComponent ConfigureEnsnaring(EntityUid uid)
    {
        var ensnaring = EnsureComp<EnsnaringComponent>(uid);
        ensnaring.FreeTime = 10f;
        ensnaring.BreakoutTime = 10f;
        ensnaring.WalkSpeed = 0.35f;
        ensnaring.SprintSpeed = 0.35f;
        ensnaring.StaminaDamage = 0f;
        ensnaring.MaxEnsnares = 1;
        ensnaring.CanMoveBreakout = false;
        return ensnaring;
    }
}
