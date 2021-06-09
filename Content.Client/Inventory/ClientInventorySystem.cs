using Content.Client.GameObjects.Components.HUD.Inventory;
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
    public sealed class ClientInventorySystem : EntitySystem
    {
        [Dependency] private readonly IGameHud _gameHud = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;

        public override void Initialize()
        {
            base.Initialize();

            CommandBinds.Builder
                .Bind(ContentKeyFunctions.OpenInventoryMenu,
                    InputCmdHandler.FromDelegate(_ => HandleOpenInventoryMenu()))
                .Register<ClientInventorySystem>();
        }

        public override void Shutdown()
        {
            CommandBinds.Unregister<ClientInventorySystem>();
            base.Shutdown();
        }

        private void HandleOpenInventoryMenu()
        {
            _gameHud.InventoryButtonDown = !_gameHud.InventoryButtonDown;
        }
    }
}
