using System;
using Content.Client.GameObjects.EntitySystems;
using Content.Client.UserInterface;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;

#nullable enable

namespace Content.Client.Construction
{
    /// <summary>
    /// This class presents the Construction/Crafting UI to the client, linking the <see cref="ConstructionSystem"/> with a <see cref="ConstructionMenu"/>.
    /// </summary>
    internal class ConstructionMenuPresenter : IDisposable
    {
        private readonly IGameHud _gameHud;
        private readonly IEntitySystemManager _systemManager;
        private readonly ConstructionMenu _constructionView;
        private ConstructionSystem? _constructionSystem;

        private bool CraftingAvailable
        {
            get => _gameHud.CraftingButtonVisible;
            set
            {
                _gameHud.CraftingButtonVisible = value;
                if(!value)
                    _constructionView.Close();
            }
        }

        private bool WindowOpen
        {
            get => _constructionView.IsOpen;
            set
            {
                if(value && CraftingAvailable)
                {
                    if(_constructionView.IsOpen)
                        _constructionView.MoveToFront();
                    else
                        _constructionView.OpenCentered();
                }
                else
                    _constructionView.Close();
            }
        }

        /// <summary>
        /// Does the window have focus? If the window is closed, this will always return false.
        /// </summary>
        private bool IsAtFront => _constructionView.IsOpen && _constructionView.IsAtFront();

        /// <summary>
        /// Constructs a new instance of <see cref="ConstructionMenuPresenter"/>.
        /// </summary>
        /// <param name="gameHud">GUI that is being presented to.</param>
        /// <param name="systemManager">EntitySystem that contains a ConstructionSystem being presented from.</param>
        public ConstructionMenuPresenter(IGameHud gameHud, IEntitySystemManager systemManager)
        {
            _gameHud = gameHud;
            _systemManager = systemManager;

            // This is required so that if we load after the system is initialized
            if (_systemManager.TryGetEntitySystem<ConstructionSystem>(out var constructionSystem))
                SystemBindingChanged(constructionSystem);

            _systemManager.SystemLoaded += OnSystemLoaded;
            _systemManager.SystemUnloaded += OnSystemUnloaded;

            _constructionView = new ConstructionMenu();
            _constructionView.OnClose += () => _gameHud.CraftingButtonDown = false;

            _gameHud.CraftingButtonToggled += b => WindowOpen = b;
        }

        private void OnSystemLoaded(object? sender, SystemChangedArgs args)
        {
            if (args.System is ConstructionSystem system)
            {
                SystemBindingChanged(system);
            }
        }

        private void OnSystemUnloaded(object? sender, SystemChangedArgs args)
        {
            if (args.System is ConstructionSystem)
            {
                SystemBindingChanged(null);
            }
        }

        private void SystemBindingChanged(ConstructionSystem? newSystem)
        {
            if (newSystem is null)
            {
                if(_constructionSystem is null)
                    return;

                UnbindFromSystem();
            }
            else
            {
                if (_constructionSystem is null)
                {
                    BindToSystem(newSystem);
                    return;
                }

                UnbindFromSystem();
                BindToSystem(newSystem);

                //TODO: update the view
            }
        }

        private void BindToSystem(ConstructionSystem system)
        {
            _constructionSystem = system;
            system.ToggleCraftingWindow += SystemOnToggleMenu;
            system.CraftingAvailabilityChanged += SystemCraftingAvailabilityChanged;
        }

        private void UnbindFromSystem()
        {
            var system = _constructionSystem;

            if(system is null)
                throw new InvalidOperationException();

            system.ToggleCraftingWindow -= SystemOnToggleMenu;
            system.CraftingAvailabilityChanged -= SystemCraftingAvailabilityChanged;
            _constructionSystem = null;
        }

        private void SystemCraftingAvailabilityChanged(object? sender, CraftingAvailabilityChangedArgs e)
        {
            CraftingAvailable = e.Available;
        }

        private void SystemOnToggleMenu(object? sender, EventArgs eventArgs)
        {
            if (!CraftingAvailable)
                return;

            if (WindowOpen)
            {
                if (IsAtFront)
                {
                    WindowOpen = false;
                    _gameHud.CraftingButtonDown = false;
                }
                else
                {
                    _constructionView.MoveToFront();
                }
            }
            else
            {
                WindowOpen = true;
                _gameHud.CraftingButtonDown = true;
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _constructionView.Dispose();

            _systemManager.SystemLoaded -= OnSystemLoaded;
            _systemManager.SystemUnloaded -= OnSystemUnloaded;
        }
    }
}
