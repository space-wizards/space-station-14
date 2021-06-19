using Content.Client.HUD;
using Content.Shared.Input;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.GameObjects;
using Robust.Shared.Input.Binding;
using Robust.Shared.IoC;

namespace Content.Client.CharacterInterface
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

            SubscribeLocalEvent<CharacterInterfaceComponent, PlayerAttachedEvent>((_, component, _) => component.PlayerAttached());
            SubscribeLocalEvent<CharacterInterfaceComponent, PlayerDetachedEvent>((_, component, _) => component.PlayerDetached());
        }

        public override void Shutdown()
        {
            CommandBinds.Unregister<CharacterInterfaceSystem>();
            base.Shutdown();
        }

        private void HandleOpenCharacterMenu()
        {
            if (_playerManager.LocalPlayer?.ControlledEntity == null
                || !_playerManager.LocalPlayer.ControlledEntity.TryGetComponent(out CharacterInterfaceComponent? characterInterface))
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
