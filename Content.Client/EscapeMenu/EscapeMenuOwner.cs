using Content.Client.HUD;
using Content.Client.Viewport;
using Robust.Client.Console;
using Robust.Client.Input;
using Robust.Client.State;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.IoC;

namespace Content.Client.EscapeMenu
{
    internal sealed class EscapeMenuOwner : IEscapeMenuOwner
    {
        [Dependency] private readonly IClientConsoleHost _consoleHost = default!;
        [Dependency] private readonly IInputManager _inputManager = default!;
        [Dependency] private readonly IStateManager _stateManager = default!;
        [Dependency] private readonly IGameHud _gameHud = default!;

        private UI.EscapeMenu? _escapeMenu;

        public void Initialize()
        {
            _stateManager.OnStateChanged += StateManagerOnOnStateChanged;

            _gameHud.EscapeButtonToggled += _setOpenValue;
        }

        private void StateManagerOnOnStateChanged(StateChangedEventArgs obj)
        {
            if (obj.NewState is GameScreenBase)
            {
                // Switched TO GameScreen.
                _escapeMenu = new UI.EscapeMenu(_consoleHost);

                _escapeMenu.OnClose += () => _gameHud.EscapeButtonDown = false;

                _inputManager.SetInputCommand(EngineKeyFunctions.EscapeMenu,
                    InputCmdHandler.FromDelegate(_ => Enabled()));
            }
            else if (obj.OldState is GameScreenBase)
            {
                // Switched FROM GameScreen.
                _escapeMenu?.Dispose();
                _escapeMenu = null;

                _inputManager.SetInputCommand(EngineKeyFunctions.EscapeMenu, null);
            }
        }

        private void Enabled()
        {
            if (_escapeMenu != null && _escapeMenu.IsOpen)
            {
                if (_escapeMenu.IsAtFront())
                {
                    _setOpenValue(false);
                }
                else
                {
                    _escapeMenu.MoveToFront();
                }
            }
            else
            {
                _setOpenValue(true);
            }
        }

        private void _setOpenValue(bool value)
        {
            if (value)
            {
                _gameHud.EscapeButtonDown = true;
                _escapeMenu?.OpenCentered();
            }
            else
            {
                _gameHud.EscapeButtonDown = false;
                _escapeMenu?.Close();
            }
        }
    }

    public interface IEscapeMenuOwner
    {
        void Initialize();
    }
}
