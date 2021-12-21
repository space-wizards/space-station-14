using System.Linq;
using Content.Client.CharacterInfo.Components;
using Content.Client.HUD;
using Content.Shared.Input;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Input;
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
        [Dependency] private readonly IInputManager _inputManager = default!;

        public override void Initialize()
        {
            base.Initialize();

            CommandBinds.Builder
                .Bind(ContentKeyFunctions.OpenCharacterMenu,
                InputCmdHandler.FromDelegate(_ => HandleOpenCharacterMenu()))
                .Register<CharacterInterfaceSystem>();

            SubscribeLocalEvent<CharacterInterfaceComponent, ComponentInit>(OnComponentInit);
            SubscribeLocalEvent<CharacterInterfaceComponent, ComponentRemove>(OnComponentRemove);
            SubscribeLocalEvent<CharacterInterfaceComponent, PlayerAttachedEvent>(OnPlayerAttached);
            SubscribeLocalEvent<CharacterInterfaceComponent, PlayerDetachedEvent>(OnPlayerDetached);
        }

        public override void Shutdown()
        {
            CommandBinds.Unregister<CharacterInterfaceSystem>();
            base.Shutdown();
        }

        private void OnComponentInit(EntityUid uid, CharacterInterfaceComponent comp, ComponentInit args)
        {
            //Use all the character ui interfaced components to create the character window
            comp.UIComponents = EntityManager.GetComponents<ICharacterUI>(uid).ToList();
            if (comp.UIComponents.Count == 0)
                return;

            comp.Window = new CharacterInterfaceComponent.CharacterWindow(comp.UIComponents)
            {
                SetSize = (545, 400)
            };
            
            comp.Window.OnClose += () => _gameHud.CharacterButtonDown = false;
        }

        private void OnComponentRemove(EntityUid uid, CharacterInterfaceComponent comp, ComponentRemove args)
        {
            if (comp.UIComponents != null)
            {
                foreach (var component in comp.UIComponents)
                {
                    // Make sure these don't get deleted when the window is disposed.
                    component.Scene.Orphan();
                }
            }

            comp.UIComponents = null;

            comp.Window?.Close();
            comp.Window = null;

            _inputManager.SetInputCommand(ContentKeyFunctions.OpenCharacterMenu, null);
        }

        private void OnPlayerAttached(EntityUid uid, CharacterInterfaceComponent comp, PlayerAttachedEvent args)
        {
            if (comp.Window == null)
                return;

            _gameHud.CharacterButtonVisible = true;
            _gameHud.CharacterButtonToggled = b =>
            {
                if (b)
                    comp.Window.OpenCentered();
                else
                    comp.Window.Close();
            };
        }

        private void OnPlayerDetached(EntityUid uid, CharacterInterfaceComponent comp, PlayerDetachedEvent args)
        {
            if (comp.Window == null)
                return;

            _gameHud.CharacterButtonVisible = false;
            comp.Window.Close();
        }

        private void HandleOpenCharacterMenu()
        {
            if (_playerManager.LocalPlayer?.ControlledEntity == null
                || !EntityManager.TryGetComponent(_playerManager.LocalPlayer.ControlledEntity, out CharacterInterfaceComponent? characterInterface))
                return;

            var menu = characterInterface.Window;
            if (menu == null)
                return;

            if (menu.IsOpen)
            {
                if (menu.IsAtFront())
                    _setOpenValue(menu, false);
                else
                    menu.MoveToFront();
            }
            else
            {
                _setOpenValue(menu, true);
            }
        }

        private void _setOpenValue(SS14Window menu, bool value)
        {
            _gameHud.CharacterButtonDown = value;
            if (value)
                menu.OpenCentered();
            else
                menu.Close();
        }
    }
}
