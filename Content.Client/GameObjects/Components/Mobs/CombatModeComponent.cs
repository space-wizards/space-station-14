using Content.Client.UserInterface;
using Content.Shared.GameObjects.Components.Mobs;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.IoC;
using Robust.Shared.ViewVariables;

namespace Content.Client.GameObjects.Components.Mobs
{
    [RegisterComponent]
    public sealed class CombatModeComponent : SharedCombatModeComponent
    {
        [ViewVariables(VVAccess.ReadWrite)]
        public bool IsInCombatMode { get; private set; }

        [ViewVariables(VVAccess.ReadWrite)]
        public TargetingZone ActiveZone { get; private set; }

#pragma warning disable 649
        [Dependency] private readonly IGameHud _gameHud;
#pragma warning restore 649

        public override void HandleComponentState(ComponentState curState, ComponentState nextState)
        {
            base.HandleComponentState(curState, nextState);

            var state = (CombatModeComponentState) curState;

            IsInCombatMode = state.IsInCombatMode;
            ActiveZone = state.TargetingZone;
            UpdateHud();
        }

        public override void HandleMessage(ComponentMessage message, INetChannel netChannel = null, IComponent component = null)
        {
            base.HandleMessage(message, netChannel, component);

            switch (message)
            {
                case PlayerAttachedMsg _:
                    _gameHud.CombatPanelVisible = true;
                    UpdateHud();
                    break;

                case PlayerDetachedMsg _:
                    _gameHud.CombatPanelVisible = false;
                    break;
            }
        }

        private void UpdateHud()
        {
            _gameHud.CombatModeActive = IsInCombatMode;
            _gameHud.TargetingZone = ActiveZone;
        }
    }
}
