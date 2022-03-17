using Content.Client.CombatMode.UI;
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
        [Dependency] private readonly IHudManager _hudManager = default!;

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

        public void PlayerDetached() { _hudManager.ShowUIWidget<CombatPanelWidget>(false); }

        public void PlayerAttached()
        {
            _hudManager.ShowUIWidget<CombatPanelWidget>(true); // TODO BOBBY SYSTEM Make the targeting doll actually do something.
            UpdateHud();
        }

        private void UpdateHud()
        {
            if (Owner != _playerManager.LocalPlayer?.ControlledEntity)
            {
                return;
            }
            //Combat panel should never be null in gameplay. If this throws an nullref exception, someone else fucked up
            _hudManager.GetUIWidget<CombatPanelWidget>()!.ActiveTargetZone = ActiveZone;
        }
    }
}
