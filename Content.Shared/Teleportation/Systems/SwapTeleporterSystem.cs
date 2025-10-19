using Content.Shared.Examine;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Teleportation.Components;
using Content.Shared.Verbs;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
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
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;

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
        // We can't run the teleport logic on the client due to PVS range issues.
        if (_net.IsClient || comp.LinkedEnt is not { } linkedEnt)
            return;

        var teleEnt = GetTeleportingEntity((uid, xform));
        var otherTeleEnt = GetTeleportingEntity((linkedEnt, Transform(linkedEnt)));
        var teleXform = Transform(teleEnt);
        var otherTeleXform = Transform(otherTeleEnt);

        if (!CanSwapTeleport((teleEnt, teleXform), (otherTeleEnt, otherTeleXform)))
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
        _transform.SwapPositions(teleEnt, otherTeleEnt);
    }

    /// <summary>
    /// Checks if two entities are able to swap positions via the teleporter.
    /// </summary>
    private bool CanSwapTeleport(
        Entity<TransformComponent> entity1,
        Entity<TransformComponent> entity2)
    {
        _container.TryGetOuterContainer(entity1, entity1, out var container1);
        _container.TryGetOuterContainer(entity2, entity2, out var container2);

        if (container2 != null && !_container.CanInsert(entity1, container2) ||
            container1 != null && !_container.CanInsert(entity2, container1))
            return false;

        if (IsPaused(entity1) || IsPaused(entity2))
            return false;

        return true;
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
