using Content.Shared.Medical.Surgery;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;

namespace Content.Client.UserInterface.Systems.Surgery;

public sealed class SurgeryRealmUIController : UIController
{
    [Dependency] private readonly IEntityManager _entities = default!;
    [Dependency] private readonly IPlayerManager _players = default!;

    [UISystemDependency] private readonly TransformSystem _transform = default!;

    private SurgeryRealmWindow? _window;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<SurgeryRealmStartEvent>(OnSurgeryRealmStart);
    }

    private void OnSurgeryRealmStart(SurgeryRealmStartEvent msg, EntitySessionEventArgs args)
    {
        _window?.Close();

        if (!_entities.TryGetComponent(msg.Camera, out EyeComponent? eye) || eye.Eye == null)
        {
            Logger.Error("Camera entity does not have an eye!");
            return;
        }

        if (_players.LocalPlayer?.ControlledEntity is not { } playerEntity)
        {
            return;
        }

        // eye.Rotation = _transform.GetWorldRotation(playerEntity);

        _window = new SurgeryRealmWindow(eye.Eye);
        _window.OnClose += OnWindowClose;
        _window.OpenCentered();
    }

    private void OnWindowClose()
    {
        _window = null;
    }
}
