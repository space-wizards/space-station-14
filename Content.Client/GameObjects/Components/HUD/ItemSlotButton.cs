using System;
using Content.Client.GameObjects.EntitySystems;
using Content.Shared.Input;
using Robust.Client.GameObjects.EntitySystems;
using Robust.Client.Graphics;
using Robust.Client.Interfaces.Graphics.ClientEye;
using Robust.Client.Interfaces.Input;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.Player;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects;
using Robust.Shared.Input;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Content.Client.GameObjects.Components.HUD
{
    class ItemSlotButton : MarginContainer
    {
#pragma warning disable 0649
        [Dependency] private readonly IPlayerManager _playerManager;
        [Dependency] private readonly IGameTiming _gameTiming;
        [Dependency] private readonly IInputManager _inputManager;
        [Dependency] private readonly IEntitySystemManager _entitySystemManager;
        [Dependency] private readonly IEyeManager _eyeManager;
#pragma warning restore 0649

        public IEntity Item { get; set; }

        public BaseButton Button { get; }
        public SpriteView SpriteView { get; }
        public BaseButton StorageButton { get; }

        public Action<BaseButton.ButtonEventArgs> OnPressed { get; set; }
        public Action<BaseButton.ButtonEventArgs> OnStoragePressed { get; set; }

        public ItemSlotButton(Texture texture, Texture storageTexture)
        {
            CustomMinimumSize = (64, 64);

            AddChild(Button = new TextureButton
            {
                TextureNormal = texture,
                Scale = (2, 2),
                EnableAllKeybinds = true
            });

            Button.OnPressed += OnButtonPressed;

            AddChild(SpriteView = new SpriteView
            {
                MouseFilter = MouseFilterMode.Ignore,
                Scale = (2, 2)
            });

            AddChild(StorageButton = new TextureButton
            {
                TextureNormal = storageTexture,
                Scale = (0.75f, 0.75f),
                SizeFlagsHorizontal = SizeFlags.ShrinkEnd,
                SizeFlagsVertical = SizeFlags.ShrinkEnd,
                Visible = false,
                EnableAllKeybinds = true
            });

            StorageButton.OnPressed += OnStorageButtonPressed;
        }

        private void OnStorageButtonPressed(BaseButton.ButtonEventArgs args)
        {
            if (args.Event.Function == EngineKeyFunctions.Use)
            {
                OnStoragePressed?.Invoke(args);
            }
            else
            {
                OnButtonPressed(args);
            }
            if (Item == null)
                return;
        }

        private void OnButtonPressed(BaseButton.ButtonEventArgs args)
        {
            args.Event.Handle();
            if (Item == null)
                return;
            if (args.Event.Function == ContentKeyFunctions.ExamineEntity)
            {
                _entitySystemManager.GetEntitySystem<ExamineSystem>()
                    .DoExamine(Item);
            }
            else if (args.Event.Function == ContentKeyFunctions.OpenContextMenu)
            {
                _entitySystemManager.GetEntitySystem<VerbSystem>()
                    .OpenContextMenu(Item, new ScreenCoordinates(args.Event.PointerLocation.Position));
            }
            else if (args.Event.Function == ContentKeyFunctions.ActivateItemInWorld)
            {
                var inputSys = _entitySystemManager.GetEntitySystem<InputSystem>();

                var func = args.Event.Function;
                var funcId = _inputManager.NetworkBindMap.KeyFunctionID(func);

                var mousePosWorld = _eyeManager.ScreenToWorld(args.Event.PointerLocation);
                var message = new FullInputCmdMessage(_gameTiming.CurTick, funcId, args.Event.State, mousePosWorld,
                    args.Event.PointerLocation, Item.Uid);

                // client side command handlers will always be sent the local player session.
                var session = _playerManager.LocalPlayer.Session;
                inputSys.HandleInputCommand(session, func, message);
            }
            else
            {
                OnPressed?.Invoke(args);
            }
        }
    }
}
