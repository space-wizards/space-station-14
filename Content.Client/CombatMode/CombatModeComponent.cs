using Content.Client.HUD;
using Content.Shared.CombatMode;
using Content.Shared.Targeting;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Client.CombatMode
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedCombatModeComponent))]
    public sealed class CombatModeComponent : SharedCombatModeComponent
    {
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IGameHud _gameHud = default!;

        public override bool IsInCombatMode
        {
            get => base.IsInCombatMode;
            set
            {
                base.IsInCombatMode = value;
                UpdateHud();
            }
        }

        public override TargetingZone ActiveZone
        {
            get => base.ActiveZone;
            set
            {
                base.ActiveZone = value;
                UpdateHud();
            }
        }

        public void PlayerDetached() { _gameHud.CombatPanelVisible = false; }

        public void PlayerAttached()
        {
            _gameHud.CombatPanelVisible = false; // TODO BOBBY SYSTEM Make the targeting doll actually do something.
            UpdateHud();
        }

        private void UpdateHud()
        {
            if (Owner != _playerManager.LocalPlayer?.ControlledEntity)
            {
                return;
            }

            _gameHud.TargetingZone = ActiveZone;
        }
    }
}
