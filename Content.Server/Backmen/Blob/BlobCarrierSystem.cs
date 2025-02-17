using Content.Server.Actions;
using Content.Server.Backmen.Blob.Components;
using Content.Server.Body.Systems;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Mind;
using Content.Shared.Backmen.Blob;
using Content.Shared.Backmen.Blob.Components;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Popups;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.Backmen.Blob;

public sealed class BlobCarrierSystem : SharedBlobCarrierSystem
{
    [Dependency] private readonly BlobCoreSystem _blobCoreSystem = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly BodySystem _bodySystem = default!;
    [Dependency] private readonly ActionsSystem _action = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BlobCarrierComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<BlobCarrierComponent, TransformToBlobActionEvent>(OnTransformToBlobChanged);

        SubscribeLocalEvent<BlobCarrierComponent, MapInitEvent>(OnStartup);

        SubscribeLocalEvent<BlobCarrierComponent, MindAddedMessage>(OnMindAdded);
        SubscribeLocalEvent<BlobCarrierComponent, MindRemovedMessage>(OnMindRemove);
    }


    [ValidatePrototypeId<EntityPrototype>] private const string ActionTransformToBlob = "ActionTransformToBlob";

    private void OnMindAdded(EntityUid uid, BlobCarrierComponent component, MindAddedMessage args)
    {
        component.HasMind = true;
    }

    private void OnMindRemove(EntityUid uid, BlobCarrierComponent component, MindRemovedMessage args)
    {
        component.HasMind = false;
    }

    private void OnTransformToBlobChanged(Entity<BlobCarrierComponent> uid, ref TransformToBlobActionEvent args)
    {
        TransformToBlob(uid);
    }

    private void OnStartup(EntityUid uid, BlobCarrierComponent component, MapInitEvent args)
    {
        _action.AddAction(uid, ref component.TransformToBlob, ActionTransformToBlob);

        var ghostRole = EnsureComp<GhostRoleComponent>(uid);
        EnsureComp<GhostTakeoverAvailableComponent>(uid);
        ghostRole.RoleName = Loc.GetString("blob-carrier-role-name");
        ghostRole.RoleDescription = Loc.GetString("blob-carrier-role-desc");
        ghostRole.RoleRules = Loc.GetString("blob-carrier-role-rules");

        EnsureComp<BlobSpeakComponent>(uid).OverrideName = false;
    }

    private void OnMobStateChanged(Entity<BlobCarrierComponent> uid, ref MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Dead)
        {
            TransformToBlob(uid);
        }
    }

    protected override void TransformToBlob(Entity<BlobCarrierComponent> ent)
    {
        var xform = Transform(ent);
        if (!HasComp<MapGridComponent>(xform.GridUid))
            return;

        if (_mind.TryGetMind(ent, out _, out var mind) && mind.UserId != null)
        {
            var core = Spawn(ent.Comp.CoreBlobPrototype, xform.Coordinates);

            if (!TryComp<BlobCoreComponent>(core, out var blobCoreComponent))
                return;

            _blobCoreSystem.CreateBlobObserver(core, mind.UserId.Value, blobCoreComponent);
        }
        else
        {
            Spawn(ent.Comp.CoreBlobGhostRolePrototype, xform.Coordinates);
        }

        _bodySystem.GibBody(ent);
    }
}
