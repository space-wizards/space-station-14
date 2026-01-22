using Content.Server.Administration.Managers;
using Content.Server.EUI;
using Content.Shared.Administration;
using Content.Shared.Eui;
using Content.Shared.Follower;
using Content.Shared.Coordinates;
using Robust.Server.GameStates;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using JetBrains.Annotations;

namespace Content.Server.Administration.UI;

/// <summary>
/// Admin Eui for opening a viewport window to observe entities.
/// Use the "Open Camera" admin verb or the "camera" command to open.
/// </summary>
[UsedImplicitly]
public sealed partial class AdminCameraEui : BaseEui
{
    [Dependency] private readonly IAdminManager _admin = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private readonly FollowerSystem _follower = default!;
    private readonly PvsOverrideSystem _pvs = default!;
    private readonly SharedViewSubscriberSystem _viewSubscriber = default!;

    private static readonly EntProtoId CameraProtoId = "AdminCamera";

    private readonly EntityUid _target;
    private EntityUid? _camera;


    public AdminCameraEui(EntityUid target)
    {
        IoCManager.InjectDependencies(this);
        _follower = _entityManager.System<FollowerSystem>();
        _pvs = _entityManager.System<PvsOverrideSystem>();
        _viewSubscriber = _entityManager.System<SharedViewSubscriberSystem>();

        _target = target;
    }

    public override void Opened()
    {
        base.Opened();

        _camera = CreateCamera(_target, Player);
        StateDirty();
    }

    public override void Closed()
    {
        base.Closed();

        _entityManager.DeleteEntity(_camera);
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        switch (msg)
        {
            case AdminCameraFollowMessage:
                if (!_admin.HasAdminFlag(Player, AdminFlags.Admin) || Player.AttachedEntity == null)
                    return;
                _follower.StartFollowingEntity(Player.AttachedEntity.Value, _target);
                break;
            default:
                break;
        }
    }

    public override EuiStateBase GetNewState()
    {
        var name = _entityManager.GetComponent<MetaDataComponent>(_target).EntityName;
        var netEnt = _entityManager.GetNetEntity(_camera);
        return new AdminCameraEuiState(netEnt, name, _timing.CurTick);
    }

    private EntityUid CreateCamera(EntityUid target, ICommonSession observer)
    {
        // Spawn a camera entity attached to the target.
        var coords = target.ToCoordinates();
        var camera = _entityManager.SpawnAttachedTo(CameraProtoId, coords);

        // Allow the user to see the entities near the camera.
        // This also force sends the camera entity to the user, overriding the visibility flags.
        // (The camera entity has its visibility flags set to VisibilityFlags.Admin so that cheat clients can't see it)
        _viewSubscriber.AddViewSubscriber(camera, observer);

        return camera;
    }
}
