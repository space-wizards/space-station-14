using Content.Shared.Input;
using Content.Shared.ShipGuns.Systems;
using Robust.Client.Input;

namespace Content.Client.ShipGuns.Systems;

/// <inheritdoc/>
public sealed class TurretConsoleSystem : SharedTurretConsoleSystem
{
    [Dependency] private readonly IInputManager _input = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();
        var turretContext = _input.Contexts.New("turret", "common");
        turretContext.AddFunction(ContentKeyFunctions.TurretSafety);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _input.Contexts.Remove("turret");
    }
}
