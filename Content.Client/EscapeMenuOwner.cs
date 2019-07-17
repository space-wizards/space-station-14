using Content.Client.UserInterface;
using Robust.Client.Console;
using Robust.Client.Interfaces.Input;
using Robust.Client.Interfaces.Placement;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.Interfaces.State;
using Robust.Client.State.States;
using Robust.Shared.Input;
using Robust.Shared.Interfaces.Configuration;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Players;
using Robust.Shared.Prototypes;

namespace Content.Client
{
    internal sealed class EscapeMenuOwner : IEscapeMenuOwner
    {
#pragma warning disable 649
        [Dependency] private readonly IClientConsole _clientConsole;
        [Dependency] private readonly IConfigurationManager _configurationManager;
        [Dependency] private readonly IInputManager _inputManager;
        [Dependency] private readonly IPlacementManager _placementManager;
        [Dependency] private readonly IPrototypeManager _prototypeManager;
        [Dependency] private readonly IResourceCache _resourceCache;
        [Dependency] private readonly IStateManager _stateManager;
        [Dependency] private readonly ITileDefinitionManager _tileDefinitionManager;
        [Dependency] private readonly IGameHud _gameHud;
#pragma warning restore 649

        private EscapeMenu _escapeMenu;

        public void Initialize()
        {
            _stateManager.OnStateChanged += StateManagerOnOnStateChanged;

            _gameHud.EscapeButtonToggled += _setOpenValue;
        }

        private void StateManagerOnOnStateChanged(StateChangedEventArgs obj)
        {
            if (obj.NewState is GameScreen)
            {
                // Switched TO GameScreen.
                _escapeMenu = new EscapeMenu(_clientConsole, _tileDefinitionManager, _placementManager,
                    _prototypeManager, _resourceCache, _configurationManager)
                {
                    Visible = false
                };

                _escapeMenu.OnClose += () => _gameHud.EscapeButtonDown = false;

                _escapeMenu.AddToScreen();

                var escapeMenuCommand = InputCmdHandler.FromDelegate(Enabled);

                _inputManager.SetInputCommand(EngineKeyFunctions.EscapeMenu, escapeMenuCommand);
            }
            else if (obj.OldState is GameScreen)
            {
                // Switched FROM GameScreen.
                _escapeMenu.Dispose();
                _escapeMenu = null;

                _inputManager.SetInputCommand(EngineKeyFunctions.EscapeMenu, null);
            }
        }

        private void Enabled(ICommonSession session)
        {
            if (_escapeMenu.Visible)
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
                _escapeMenu.OpenCentered();
            }
            else
            {
                _gameHud.EscapeButtonDown = false;
                _escapeMenu.Close();
            }
        }
    }

    public interface IEscapeMenuOwner
    {
        void Initialize();
    }
}
