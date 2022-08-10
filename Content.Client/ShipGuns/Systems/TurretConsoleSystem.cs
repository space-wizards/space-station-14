using Content.Client.ShipGuns.Components;
using Content.Shared.Input;
using Content.Shared.ShipGuns.Components;
using Content.Shared.ShipGuns.Systems;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Shared.GameStates;

namespace Content.Client.ShipGuns.Systems;

/// <inheritdoc/>
public sealed class TurretConsoleSystem : SharedTurretConsoleSystem
{
    [Dependency] private readonly IInputManager _input = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GunnerComponent, ComponentHandleState>(OnHandleState);
        var turretContext = _input.Contexts.New("turret", "common");
        turretContext.AddFunction(ContentKeyFunctions.TurretSafety);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _input.Contexts.Remove("turret");
    }

    protected override void HandleGunnerShutdown(EntityUid uid, GunnerComponent component, ComponentShutdown args)
    {
        base.HandleGunnerShutdown(uid, component, args);
        if (_playerManager.LocalPlayer?.ControlledEntity != uid)
            return;

        _input.Contexts.SetActiveContext("human");
    }

    private void OnHandleState(EntityUid uid, GunnerComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not GunnerComponentState state)
            return;

        var console = state.Console.GetValueOrDefault();
        if (!console.IsValid())
        {
            component.Console = null;
            _input.Contexts.SetActiveContext("human");
            return;
        }

        if (!TryComp<TurretConsoleComponent>(console, out var turretConsoleComponent))
        {
            Logger.Warning($"Unable to set Gunner console to {console}");
            return;
        }

        component.Console = turretConsoleComponent;
        _actionBlockerSystem.UpdateCanMove(uid);
        _input.Contexts.SetActiveContext("turret");
    }
}
