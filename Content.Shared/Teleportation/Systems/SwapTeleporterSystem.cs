using Content.Shared.Examine;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Popups;
using Content.Shared.Teleportation.Components;
using Content.Shared.Verbs;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Timing;

namespace Content.Shared.Teleportation.Systems;

/// <summary>
/// This handles <see cref="SwapTeleporterComponent"/>
/// </summary>
public sealed class SwapTeleporterSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;
    [Dependency] private readonly INetManager _netMan = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly PullingSystem _pulling = default!;

    private EntityQuery<TransformComponent> _xformQuery;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<SwapTeleporterComponent, AfterInteractEvent>(OnInteract);
        SubscribeLocalEvent<SwapTeleporterComponent, GetVerbsEvent<AlternativeVerb>>(OnGetAltVerb);
        SubscribeLocalEvent<SwapTeleporterComponent, ActivateInWorldEvent>(OnActivateInWorld);
        SubscribeLocalEvent<SwapTeleporterComponent, ExaminedEvent>(OnExamined);

        SubscribeLocalEvent<SwapTeleporterComponent, ComponentShutdown>(OnShutdown);

        _xformQuery = GetEntityQuery<TransformComponent>();
    }

    private void OnInteract(Entity<SwapTeleporterComponent> ent, ref AfterInteractEvent args)
    {
        var (uid, comp) = ent;
        if (args.Target == null || !args.CanReach)
            return;

        var target = args.Target.Value;

        if (!TryComp<SwapTeleporterComponent>(target, out var targetComp))
            return;

        if (_whitelistSystem.IsWhitelistFail(comp.TeleporterWhitelist, target) ||
            _whitelistSystem.IsWhitelistFail(targetComp.TeleporterWhitelist, uid))
        {
            return;
        }

        if (comp.LinkedEnt != null)
        {
            _popup.PopupClient(Loc.GetString("swap-teleporter-popup-link-fail-already"), uid, args.User);
            return;
        }

        if (targetComp.LinkedEnt != null)
        {
            _popup.PopupClient(Loc.GetString("swap-teleporter-popup-link-fail-already-other"), uid, args.User);
            return;
        }

        comp.LinkedEnt = target;
        targetComp.LinkedEnt = uid;
        Dirty(uid, comp);
        Dirty(target, targetComp);
        _appearance.SetData(uid, SwapTeleporterVisuals.Linked, true);
        _appearance.SetData(target, SwapTeleporterVisuals.Linked, true);
        _popup.PopupClient(Loc.GetString("swap-teleporter-popup-link-create"), uid, args.User);
    }

    private void OnGetAltVerb(Entity<SwapTeleporterComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        var (uid, comp) = ent;
        if (!args.CanAccess || !args.CanInteract || args.Hands == null || comp.TeleportTime != null)
            return;

        if (!TryComp<SwapTeleporterComponent>(comp.LinkedEnt, out var otherComp) || otherComp.TeleportTime != null)
            return;

        var user = args.User;
        args.Verbs.Add(new AlternativeVerb
        {
            Text = Loc.GetString("swap-teleporter-verb-destroy-link"),
            Priority = 1,
            Act = () =>
            {
                DestroyLink((uid, comp), user);
            }
        });
    }

    private void OnActivateInWorld(Entity<SwapTeleporterComponent> ent, ref ActivateInWorldEvent args)
    {
        if (args.Handled || !args.Complex)
            return;

        var (uid, comp) = ent;
        var user = args.User;
        if (comp.TeleportTime != null)
            return;

        if (comp.LinkedEnt == null)
        {
            _popup.PopupClient(Loc.GetString("swap-teleporter-popup-teleport-cancel-link"), ent, user);
            return;
        }

        // don't allow teleporting to happen if the linked one is already teleporting
        if (!TryComp<SwapTeleporterComponent>(comp.LinkedEnt, out var otherComp)
            || otherComp.TeleportTime != null)
        {
            return;
        }

        if (_timing.CurTime < comp.NextTeleportUse)
        {
            _popup.PopupClient(Loc.GetString("swap-teleporter-popup-teleport-cancel-time"), ent, user);
            return;
        }

        _audio.PlayPredicted(comp.TeleportSound, uid, user);
        _audio.PlayPredicted(otherComp.TeleportSound, comp.LinkedEnt.Value, user);
        comp.NextTeleportUse = _timing.CurTime + comp.Cooldown;
        comp.TeleportTime = _timing.CurTime + comp.TeleportDelay;
        Dirty(uid, comp);
        args.Handled = true;
    }

    public void DoTeleport(Entity<SwapTeleporterComponent, TransformComponent> ent)
    {
        var (uid, comp, xform) = ent;

        comp.TeleportTime = null;

        Dirty(uid, comp);
        if (comp.LinkedEnt is not { } linkedEnt)
        {
            return;
        }

        // can't predict if either entity doesn't exist on the client / is outside of PVS
        if (_netMan.IsClient)
        {
            if (!Exists(uid) || Transform(uid).MapID == MapId.Nullspace || !Exists(linkedEnt) || Transform(linkedEnt).MapID == MapId.Nullspace)
                return;
        }

        var teleEnt = GetTeleportingEntity((uid, xform));
        var otherTeleEnt = GetTeleportingEntity((linkedEnt, Transform(linkedEnt)));

        _container.TryGetOuterContainer(teleEnt, Transform(teleEnt), out var cont);
        _container.TryGetOuterContainer(otherTeleEnt, Transform(otherTeleEnt), out var otherCont);

        if (!CanTeleport(teleEnt,otherTeleEnt)) // Logic moved upon request
        {
            _popup.PopupEntity(Loc.GetString("swap-teleporter-popup-teleport-fail",
                ("entity", Identity.Entity(linkedEnt, EntityManager))),
                teleEnt,
                teleEnt,
                PopupType.MediumCaution);
            return;
        }

        _popup.PopupClient(Loc.GetString("swap-teleporter-popup-teleport-other",
            ("entity", Identity.Entity(linkedEnt, EntityManager))),
            teleEnt,
            otherTeleEnt,
            PopupType.MediumCaution);

        // break pulls before teleport so we dont break shit
        // Ideally this situation would be well-handled by the physics engine, but until it is this needs to handle it
        // https://github.com/space-wizards/space-station-14/issues/31214
        if (TryComp<PullableComponent>(teleEnt, out var pullable) && pullable.BeingPulled)
        {
            _pulling.TryStopPull(teleEnt, pullable);
        }

        if (TryComp<PullerComponent>(teleEnt, out var pullerComp)
            && TryComp<PullableComponent>(pullerComp.Pulling, out var subjectPulling))
        {
            _pulling.TryStopPull(pullerComp.Pulling.Value, subjectPulling);
        }
        // both sides
        if (TryComp<PullableComponent>(otherTeleEnt, out var otherPullable) && otherPullable.BeingPulled)
        {
            _pulling.TryStopPull(otherTeleEnt, otherPullable);
        }

        if (TryComp<PullerComponent>(otherTeleEnt, out var otherPullerComp)
            && TryComp<PullableComponent>(otherPullerComp.Pulling, out var otherSubjectPulling))
        {
            _pulling.TryStopPull(otherPullerComp.Pulling.Value, otherSubjectPulling);
        }

        _transform.SwapPositions(teleEnt, otherTeleEnt);
    }

    public bool CanTeleport(EntityUid teleEnt, EntityUid otherTeleEnt)
    {
        _container.TryGetOuterContainer(teleEnt, Transform(teleEnt), out var cont);
        _container.TryGetOuterContainer(otherTeleEnt, Transform(otherTeleEnt), out var otherCont);

        // Checks if the objects can actually be swapped with respect to containers

        bool containerBlocked = otherCont != null && !_container.CanInsert(teleEnt, otherCont) ||
            cont != null && !_container.CanInsert(otherTeleEnt, cont);

        // Prevents teleporting to the polymorph zone or the cryosleep zone.

        bool pausedMap = _map.IsPaused(Transform(teleEnt).MapID) || _map.IsPaused(Transform(otherTeleEnt).MapID);

        // Room for more logic in case more situations come up in the future

        // Bring it all together

        return !(containerBlocked || pausedMap);
    }

    /// <remarks>
    /// HYAH -link
    /// </remarks>
    public void DestroyLink(Entity<SwapTeleporterComponent?> ent, EntityUid? user)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;
        var linkedNullable = ent.Comp.LinkedEnt;

        ent.Comp.LinkedEnt = null;
        ent.Comp.TeleportTime = null;
        _appearance.SetData(ent, SwapTeleporterVisuals.Linked, false);
        Dirty(ent, ent.Comp);

        if (user != null)
            _popup.PopupClient(Loc.GetString("swap-teleporter-popup-link-destroyed"), ent, user.Value);
        else
            _popup.PopupEntity(Loc.GetString("swap-teleporter-popup-link-destroyed"), ent);

        if (linkedNullable is {} linked)
            DestroyLink(linked, user); // the linked one is shown globally
    }

    private EntityUid GetTeleportingEntity(Entity<TransformComponent> ent)
    {
        var parent = ent.Comp.ParentUid;

        if (HasComp<MapGridComponent>(parent) || HasComp<MapComponent>(parent))
            return ent;

        if (!_xformQuery.TryGetComponent(parent, out var parentXform) || parentXform.Anchored)
            return ent;

        if (!TryComp<PhysicsComponent>(parent, out var body) || body.BodyType == BodyType.Static)
            return ent;

        return GetTeleportingEntity((parent, parentXform));
    }

    private void OnExamined(Entity<SwapTeleporterComponent> ent, ref ExaminedEvent args)
    {
        var (_, comp) = ent;
        using (args.PushGroup(nameof(SwapTeleporterComponent)))
        {
            var locale = comp.LinkedEnt == null
                ? "swap-teleporter-examine-link-absent"
                : "swap-teleporter-examine-link-present";
            args.PushMarkup(Loc.GetString(locale));

            if (_timing.CurTime < comp.NextTeleportUse)
            {
                args.PushMarkup(Loc.GetString("swap-teleporter-examine-time-remaining",
                    ("second", (int) ((comp.NextTeleportUse - _timing.CurTime).TotalSeconds + 0.5f))));
            }
        }
    }

    private void OnShutdown(Entity<SwapTeleporterComponent> ent, ref ComponentShutdown args)
    {
        DestroyLink((ent, ent), null);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<SwapTeleporterComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var comp, out var xform))
        {
            if (comp.TeleportTime == null)
                continue;

            if (_timing.CurTime < comp.TeleportTime)
                continue;

            DoTeleport((uid, comp, xform));
        }
    }
}
