using Content.Client.GameObjects.Components.Actor;
using Content.Client.UserInterface;
using Content.Shared.Input;
using Robust.Client.GameObjects.EntitySystems;
using Robust.Client.Player;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Input;
using Robust.Shared.IoC;

namespace Content.Client.GameObjects.EntitySystems
{
    public sealed class CharacterInterfaceSystem : EntitySystem
    {
#pragma warning disable 649
        [Dependency] private readonly IGameHud _gameHud;
        [Dependency] private readonly IPlayerManager _playerManager;
#pragma warning restore 649

        public override void Initialize()
        {
            base.Initialize();

            var inputSys = EntitySystemManager.GetEntitySystem<InputSystem>();
            inputSys.BindMap.BindFunction(ContentKeyFunctions.OpenCharacterMenu,
                new PointerInputCmdHandler(HandleOpenCharacterMenu));
        }

        private void HandleOpenCharacterMenu(in PointerInputCmdHandler.PointerInputCmdArgs args)
        {
            if (_playerManager.LocalPlayer.ControlledEntity == null
                || !_playerManager.LocalPlayer.ControlledEntity.TryGetComponent(out CharacterInterface characterInterface))
            {
                return;
            }

            var menu = characterInterface.Window;

            if (menu.Visible)
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
                _gameHud.CharacterButtonDown = true;
                menu.OpenCentered();
            }
            else
            {
                _gameHud.CharacterButtonDown = false;
                menu.Close();
            }
        }
    }
}
