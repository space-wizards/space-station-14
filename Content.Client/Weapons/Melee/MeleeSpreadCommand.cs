using Content.Shared.CombatMode;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Shared.Console;

namespace Content.Client.Weapons.Melee;

public sealed partial class MeleeSpreadCommand : LocalizedEntityCommands
{
    [Dependency] private IEyeManager _eyeManager = default!;
    [Dependency] private IInputManager _inputManager = default!;
    [Dependency] private IOverlayManager _overlay = default!;
    [Dependency] private IPlayerManager _playerManager = default!;
    [Dependency] private MeleeWeaponSystem _meleeSystem = default!;
    [Dependency] private SharedCombatModeSystem _combatSystem = default!;
    [Dependency] private SharedTransformSystem _transformSystem = default!;

    public override string Command => "showmeleespread";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (_overlay.RemoveOverlay<MeleeArcOverlay>())
            return;

        _overlay.AddOverlay(new MeleeArcOverlay(
            EntityManager,
            _eyeManager,
            _inputManager,
            _playerManager,
            _meleeSystem,
            _combatSystem,
            _transformSystem));
    }
}
