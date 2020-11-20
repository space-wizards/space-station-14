using Content.Client.GameObjects.Components.HUD.Inventory;
using Content.Client.UserInterface;
using Content.Shared.Input;
using JetBrains.Annotations;
using Robust.Client.Player;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Input.Binding;
using Robust.Shared.IoC;

namespace Content.Client.GameObjects.EntitySystems
{
    [UsedImplicitly]
    public sealed class ClientInventorySystem : EntitySystem
    {
        [Dependency] private readonly IGameHud _gameHud = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;

        public override void Initialize()
        {
            base.Initialize();

            CommandBinds.Builder
                .Bind(ContentKeyFunctions.OpenInventoryMenu,
                    InputCmdHandler.FromDelegate(s => HandleOpenInventoryMenu()))
                .Register<ClientInventorySystem>();
        }

        public override void Shutdown()
        {
            CommandBinds.Unregister<ClientInventorySystem>();
            base.Shutdown();
        }

        private void HandleOpenInventoryMenu()
        {
            if (_playerManager.LocalPlayer.ControlledEntity == null
                || !_playerManager.LocalPlayer.ControlledEntity.TryGetComponent(out ClientInventoryComponent clientInventory))
            {
                return;
            }

            var menu = clientInventory.InterfaceController.Window;

            if (menu.IsOpen)
            {
                if (menu.IsAtFront())
                {
                    _setOpenValue(menu, false);
                }
                else
                {
                    menu.MoveToFront();
                }
            }
            else
            {
                _setOpenValue(menu, true);
            }
        }

        private void _setOpenValue(SS14Window menu, bool value)
        {
            if (value)
            {
                _gameHud.InventoryButtonDown = true;
                menu.OpenCentered();
            }
            else
            {
                _gameHud.InventoryButtonDown = false;
                menu.Close();
            }
        }
    }
}
