using Content.Server.Actions;
using Content.Server.Body.Systems;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Mind;
using Content.Server.Mind.Components;
using Content.Shared.Actions.ActionTypes;
using Content.Shared.Blob;
using Content.Shared.Mobs;
using Content.Shared.Popups;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.Blob
{
    public sealed class BlobCarrierSystem : EntitySystem
    {
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly BlobCoreSystem _blobCoreSystem = default!;
        [Dependency] private readonly MindSystem _mind = default!;
        [Dependency] private readonly BodySystem _bodySystem = default!;
        [Dependency] private readonly IPrototypeManager _proto = default!;
        [Dependency] private readonly ActionsSystem _action = default!;
        [Dependency] private readonly SharedPopupSystem _popup = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;


        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<BlobCarrierComponent, MobStateChangedEvent>(OnMobStateChanged);
            SubscribeLocalEvent<BlobCarrierComponent, TransformToBlobActionEvent>(OnTransformToBlobChanged);

            SubscribeLocalEvent<BlobCarrierComponent, ComponentStartup>(OnStartup);
            SubscribeLocalEvent<BlobCarrierComponent, ComponentShutdown>(OnShutdown);

            SubscribeLocalEvent<BlobCarrierComponent, MindAddedMessage>(OnMindAdded);
            SubscribeLocalEvent<BlobCarrierComponent, MindRemovedMessage>(OnMindRemove);
        }

        private void OnMindAdded(EntityUid uid, BlobCarrierComponent component, MindAddedMessage args)
        {
            component.HasMind = true;
        }

        private void OnMindRemove(EntityUid uid, BlobCarrierComponent component, MindRemovedMessage args)
        {
            component.HasMind = false;
        }

        private void OnTransformToBlobChanged(EntityUid uid, BlobCarrierComponent component, TransformToBlobActionEvent args)
        {
            TransformToBlob(uid, component);
        }

        private void OnStartup(EntityUid uid, BlobCarrierComponent component, ComponentStartup args)
        {
            var transformToBlob = new InstantAction(
                _proto.Index<InstantActionPrototype>("TransformToBlob"));
            _action.AddAction(uid, transformToBlob, null);
            var ghostRole = EnsureComp<GhostRoleComponent>(uid);
            EnsureComp<GhostTakeoverAvailableComponent>(uid);
            ghostRole.RoleName = Loc.GetString("blob-carrier-role-name");
            ghostRole.RoleDescription = Loc.GetString("blob-carrier-role-desc");
            ghostRole.RoleRules = Loc.GetString("blob-carrier-role-rules");
        }

        private void OnShutdown(EntityUid uid, BlobCarrierComponent component, ComponentShutdown args)
        {

        }

        private void OnMobStateChanged(EntityUid uid, BlobCarrierComponent component, MobStateChangedEvent args)
        {
            if (args.NewMobState == MobState.Dead)
            {
                TransformToBlob(uid, component);
            }
        }

        private void TransformToBlob(EntityUid uid, BlobCarrierComponent carrier)
        {
            var xform = Transform(uid);
            if (!_mapManager.TryGetGrid(xform.GridUid, out var map))
                return;

            if (_mind.TryGetMind(uid, out var mind) && mind.UserId != null)
            {
                var core = Spawn(carrier.CoreBlobPrototype, xform.Coordinates);

                if (!TryComp<BlobCoreComponent>(core, out var blobCoreComponent))
                    return;

                _blobCoreSystem.CreateBlobObserver(core, mind.UserId.Value, blobCoreComponent);
            }
            else
            {
                Spawn(carrier.CoreBlobGhostRolePrototype, xform.Coordinates);
            }

            _bodySystem.GibBody(uid);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            var blobFactoryQuery = EntityQueryEnumerator<BlobCarrierComponent>();
            while (blobFactoryQuery.MoveNext(out var ent, out var comp))
            {
                if (!comp.HasMind)
                    return;

                comp.TransformationTimer += frameTime;

                if (_gameTiming.CurTime < comp.NextAlert)
                    continue;

                var remainingTime = Math.Round(comp.TransformationDelay - comp.TransformationTimer, 0);
                _popup.PopupEntity(Loc.GetString("carrier-blob-alert", ("second", remainingTime)), ent, ent, PopupType.LargeCaution);

                comp.NextAlert = _gameTiming.CurTime + TimeSpan.FromSeconds(comp.AlertInterval);

                if (!(comp.TransformationTimer >= comp.TransformationDelay))
                    continue;

                TransformToBlob(ent, comp);
            }
        }
    }
}
