using Content.Client.GameObjects.Components.Actor;
using Content.Client.UserInterface;
using Content.Shared.Input;
using JetBrains.Annotations;
using Robust.Client.Player;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.GameObjects;
using Robust.Shared.Input.Binding;
using Robust.Shared.IoC;

namespace Content.Client.GameObjects.EntitySystems
{
    [UsedImplicitly]
    public sealed class CharacterInterfaceSystem : EntitySystem
    {
        [Dependency] private readonly IGameHud _gameHud = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;

        public override void Initialize()
        {
            base.Initialize();

            CommandBinds.Builder
                .Bind(ContentKeyFunctions.OpenCharacterMenu,
                InputCmdHandler.FromDelegate(s => HandleOpenCharacterMenu()))
                .Register<CharacterInterfaceSystem>();
        }

        public override void Shutdown()
        {
            CommandBinds.Unregister<CharacterInterfaceSystem>();
            base.Shutdown();
        }

        private void HandleOpenCharacterMenu()
        {
            if (_playerManager.LocalPlayer?.ControlledEntity == null
                || !_playerManager.LocalPlayer.ControlledEntity.TryGetComponent(out CharacterInterface? characterInterface))
            {
                return;
            }

            var menu = characterInterface.Window;

            if (menu == null)
            {
                return;
            }

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
