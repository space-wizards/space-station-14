using Content.Client.Construction;
using Content.Client.DragDrop;
using Content.Shared.Input;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.IoC;

namespace Content.Client.Actions
{
    [UsedImplicitly]
    public class ActionsSystem : EntitySystem
    {
        [Dependency] private readonly IPlayerManager _playerManager = default!;

        public override void Initialize()
        {
            base.Initialize();

            // set up hotkeys for hotbar
            CommandBinds.Builder
                .Bind(ContentKeyFunctions.OpenActionsMenu,
                    InputCmdHandler.FromDelegate(_ => ToggleActionsMenu()))
                .Bind(ContentKeyFunctions.Hotbar1,
                    HandleHotbarKeybind(0))
                .Bind(ContentKeyFunctions.Hotbar2,
                    HandleHotbarKeybind(1))
                .Bind(ContentKeyFunctions.Hotbar3,
                    HandleHotbarKeybind(2))
                .Bind(ContentKeyFunctions.Hotbar4,
                    HandleHotbarKeybind(3))
                .Bind(ContentKeyFunctions.Hotbar5,
                    HandleHotbarKeybind(4))
                .Bind(ContentKeyFunctions.Hotbar6,
                    HandleHotbarKeybind(5))
                .Bind(ContentKeyFunctions.Hotbar7,
                    HandleHotbarKeybind(6))
                .Bind(ContentKeyFunctions.Hotbar8,
                    HandleHotbarKeybind(7))
                .Bind(ContentKeyFunctions.Hotbar9,
                    HandleHotbarKeybind(8))
                .Bind(ContentKeyFunctions.Hotbar0,
                    HandleHotbarKeybind(9))
                .Bind(ContentKeyFunctions.Loadout1,
                    HandleChangeHotbarKeybind(0))
                .Bind(ContentKeyFunctions.Loadout2,
                    HandleChangeHotbarKeybind(1))
                .Bind(ContentKeyFunctions.Loadout3,
                    HandleChangeHotbarKeybind(2))
                .Bind(ContentKeyFunctions.Loadout4,
                    HandleChangeHotbarKeybind(3))
                .Bind(ContentKeyFunctions.Loadout5,
                    HandleChangeHotbarKeybind(4))
                .Bind(ContentKeyFunctions.Loadout6,
                    HandleChangeHotbarKeybind(5))
                .Bind(ContentKeyFunctions.Loadout7,
                    HandleChangeHotbarKeybind(6))
                .Bind(ContentKeyFunctions.Loadout8,
                    HandleChangeHotbarKeybind(7))
                .Bind(ContentKeyFunctions.Loadout9,
                    HandleChangeHotbarKeybind(8))
                // when selecting a target, we intercept clicks in the game world, treating them as our target selection. We want to
                // take priority before any other systems handle the click.
                .BindBefore(EngineKeyFunctions.Use, new PointerInputCmdHandler(TargetingOnUse),
                    typeof(ConstructionSystem), typeof(DragDropSystem))
                .Register<ActionsSystem>();

            SubscribeLocalEvent<ClientActionsComponent, PlayerAttachedEvent>((_, component, _) => component.PlayerAttached());
            SubscribeLocalEvent<ClientActionsComponent, PlayerDetachedEvent>((_, component, _) => component.PlayerDetached());
        }

        public override void Shutdown()
        {
            base.Shutdown();
            CommandBinds.Unregister<ActionsSystem>();
        }

        private PointerInputCmdHandler HandleHotbarKeybind(byte slot)
        {
            // delegate to the ActionsUI, simulating a click on it
            return new((in PointerInputCmdHandler.PointerInputCmdArgs args) =>
                {
                    var playerEntity = _playerManager.LocalPlayer?.ControlledEntity;
                    if (playerEntity == null ||
                        !EntityManager.TryGetComponent<ClientActionsComponent?>(playerEntity.Value, out var actionsComponent)) return false;

                    actionsComponent.HandleHotbarKeybind(slot, args);
                    return true;
                }, false);
        }

        private PointerInputCmdHandler HandleChangeHotbarKeybind(byte hotbar)
        {
            // delegate to the ActionsUI, simulating a click on it
            return new((in PointerInputCmdHandler.PointerInputCmdArgs args) =>
                {
                    var playerEntity = _playerManager.LocalPlayer?.ControlledEntity;
                    if (!EntityManager.TryGetComponent<ClientActionsComponent?>(playerEntity, out var actionsComponent)) return false;

                    actionsComponent.HandleChangeHotbarKeybind(hotbar, args);
                    return true;
                },
                false);
        }

        private bool TargetingOnUse(in PointerInputCmdHandler.PointerInputCmdArgs args)
        {
            var playerEntity = _playerManager.LocalPlayer?.ControlledEntity;
            if (!EntityManager.TryGetComponent<ClientActionsComponent?>(playerEntity, out var actionsComponent)) return false;

            return actionsComponent.TargetingOnUse(args);
        }

        private void ToggleActionsMenu()
        {
            var playerEntity = _playerManager.LocalPlayer?.ControlledEntity;
            if (!EntityManager.TryGetComponent<ClientActionsComponent?>(playerEntity, out var actionsComponent)) return;

            actionsComponent.ToggleActionsMenu();
        }
    }
}
