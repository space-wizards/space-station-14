using System;
using Content.Client.UserInterface;
using Content.Shared.Input;
using Robust.Client.GameObjects.EntitySystems;
using Robust.Client.Player;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Input;
using Robust.Shared.IoC;

namespace Content.Client.GameObjects.EntitySystems
{
    public class HotbarSystem : EntitySystem
    {
#pragma warning disable 649
        [Dependency] private readonly IGameHud _gameHud;
        [Dependency] private readonly IPlayerManager _playerManager;
#pragma warning restore 649

        public override void Initialize()
        {
            base.Initialize();

            var inputSys = EntitySystemManager.GetEntitySystem<InputSystem>();
            inputSys.BindMap.BindFunction(ContentKeyFunctions.OpenAbilitiesMenu,
                InputCmdHandler.FromDelegate(s => HandleOpenAbilitiesMenu()));
            inputSys.BindMap.BindFunction(ContentKeyFunctions.Hotbar0,
                InputCmdHandler.FromDelegate(s => HandleHotbarKeybindPressed()));
        }

        private void HandleOpenAbilitiesMenu()
        {
            //if (_playerManager.LocalPlayer.ControlledEntity == null
            //    || !_playerManager.LocalPlayer.ControlledEntity.TryGetComponent(out ClientHotbarComponent clientHotbar))
            //{
            //    return;
            //}
        }

        private void HandleHotbarKeybindPressed()
        {
            if (_playerManager.LocalPlayer.ControlledEntity == null
                || !_playerManager.LocalPlayer.ControlledEntity.TryGetComponent(out ClientInventoryComponent clientInventory))
            {
                return;
            }
        }
    }
}
