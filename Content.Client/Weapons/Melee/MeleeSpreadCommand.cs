using Content.Shared.CombatMode;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Shared.Console;
using Robust.Shared.Map;

namespace Content.Client.Weapons.Melee;


public sealed class MeleeSpreadCommand : LocalizedEntityCommands
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly IInputManager _inputManager = default!;
    [Dependency] private readonly IOverlayManager _overlay = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly MeleeWeaponSystem _meleeSystem = default!;
    [Dependency] private readonly SharedCombatModeSystem _combatSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;

    public override string Command => "showmeleespread";
    public override string Description => "Shows the current weapon's range and arc for debugging";
    public override string Help => $"{Command}";
    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (_overlay.RemoveOverlay<MeleeArcOverlay>())
            return;

        _overlay.AddOverlay(new MeleeArcOverlay(
            _entManager,
            _eyeManager,
            _inputManager,
            _playerManager,
            _meleeSystem,
            _combatSystem,
            _transformSystem));
    }
}
