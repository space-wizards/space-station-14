using Content.Client.UserInterface;
using Content.Shared.GameObjects.Components.Mobs;
using Content.Shared.GameObjects.EntitySystemMessages;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.IoC;

namespace Content.Client.GameObjects.EntitySystems
{
    public sealed class CombatModeSystem : EntitySystem
    {
#pragma warning disable 649
        [Dependency] private readonly IGameHud _gameHud;
#pragma warning restore 649

        public override void Initialize()
        {
            base.Initialize();

            _gameHud.OnCombatModeChanged = OnCombatModeChanged;
            _gameHud.OnTargetingZoneChanged = OnTargetingZoneChanged;
        }

        private void OnTargetingZoneChanged(TargetingZone obj)
        {
            RaiseNetworkEvent(new CombatModeSystemMessages.SetTargetZoneMessage(obj));
        }

        private void OnCombatModeChanged(bool obj)
        {
            RaiseNetworkEvent(new CombatModeSystemMessages.SetCombatModeActiveMessage(obj));
        }
    }
}
