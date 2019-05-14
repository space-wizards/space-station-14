using Content.Client.UserInterface;
using Robust.Client.Console;
using Robust.Client.Interfaces.Graphics;
using Robust.Client.Interfaces.Input;
using Robust.Client.Interfaces.Placement;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.Interfaces.State;
using Robust.Client.State.States;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Input;
using Robust.Shared.Interfaces.Configuration;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;

namespace Content.Client
{
    internal sealed class EscapeMenuOwner : IEscapeMenuOwner
    {
#pragma warning disable 649
        [Dependency] private readonly IStateManager _stateManager;
        [Dependency] private readonly IDisplayManager _displayManager;
        [Dependency] private readonly IClientConsole _clientConsole;
        [Dependency] private readonly ITileDefinitionManager _tileDefinitionManager;
        [Dependency] private readonly IPlacementManager _placementManager;
        [Dependency] private readonly IPrototypeManager _prototypeManager;
        [Dependency] private readonly IResourceCache _resourceCache;
        [Dependency] private readonly IConfigurationManager _configurationManager;
        [Dependency] private readonly IInputManager _inputManager;
#pragma warning restore 649

        private EscapeMenu _escapeMenu;

        public void Initialize()
        {
            _stateManager.OnStateChanged += StateManagerOnOnStateChanged;
        }

        private void StateManagerOnOnStateChanged(StateChangedEventArgs obj)
        {
            if (obj.NewState is GameScreen)
            {
                // Switched TO GameScreen.
                _escapeMenu = new EscapeMenu(_displayManager, _clientConsole, _tileDefinitionManager, _placementManager,
                    _prototypeManager, _resourceCache, _configurationManager)
                {
                    Visible = false
                };

                _escapeMenu.AddToScreen();

                var escapeMenuCommand = InputCmdHandler.FromDelegate(session =>
                {
                    if (_escapeMenu.Visible)
                    {
                        if (_escapeMenu.IsAtFront())
                        {
                            _escapeMenu.Visible = false;
                        }
                        else
                        {
                            _escapeMenu.MoveToFront();
                        }
                    }
                    else
                    {
                        _escapeMenu.OpenCentered();
                    }
                });

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
    }

    public interface IEscapeMenuOwner
    {
        void Initialize();
    }
}
