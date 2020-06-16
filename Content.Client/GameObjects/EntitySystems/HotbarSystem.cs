using System;
using Content.Client.GameObjects.Components.HUD.Hotbar;
using Content.Client.Utility;
using Content.Shared.GameObjects.Components.HUD.Hotbar;
using Content.Shared.Input;
using Robust.Client.GameObjects.EntitySystems;
using Robust.Client.Graphics;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Players;
using static Robust.Shared.Input.PointerInputCmdHandler;

namespace Content.Client.GameObjects.EntitySystems
{
    public class HotbarSystem : EntitySystem
    {
#pragma warning disable 649
        [Dependency] private readonly IPlayerManager _playerManager;
        [Dependency] private readonly IGameTiming _gameTiming;
#pragma warning restore 649

        public override void Initialize()
        {
            base.Initialize();

            CommandBinds.Builder
                .Bind(ContentKeyFunctions.OpenActionsMenu,
                InputCmdHandler.FromDelegate(s => HandleOpenActionsMenu()))
                .Bind(ContentKeyFunctions.Hotbar0,
                new PointerInputCmdHandler((in PointerInputCmdArgs args) => { return HandleHotbarKeybindPressed(0, args); }))
                .Bind(ContentKeyFunctions.Hotbar1,
                new PointerInputCmdHandler((in PointerInputCmdArgs args) => { return HandleHotbarKeybindPressed(1, args); }))
                .Bind(ContentKeyFunctions.Hotbar2,
                new PointerInputCmdHandler((in PointerInputCmdArgs args) => { return HandleHotbarKeybindPressed(2, args); }))
                .Bind(ContentKeyFunctions.Hotbar3,
                new PointerInputCmdHandler((in PointerInputCmdArgs args) => { return HandleHotbarKeybindPressed(3, args); }))
                .Bind(ContentKeyFunctions.Hotbar4,
                new PointerInputCmdHandler((in PointerInputCmdArgs args) => { return HandleHotbarKeybindPressed(4, args); }))
                .Bind(ContentKeyFunctions.Hotbar5,
                new PointerInputCmdHandler((in PointerInputCmdArgs args) => { return HandleHotbarKeybindPressed(5, args); }))
                .Bind(ContentKeyFunctions.Hotbar6,
                new PointerInputCmdHandler((in PointerInputCmdArgs args) => { return HandleHotbarKeybindPressed(6, args); }))
                .Bind(ContentKeyFunctions.Hotbar7,
                new PointerInputCmdHandler((in PointerInputCmdArgs args) => { return HandleHotbarKeybindPressed(7, args); }))
                .Bind(ContentKeyFunctions.Hotbar8,
                new PointerInputCmdHandler((in PointerInputCmdArgs args) => { return HandleHotbarKeybindPressed(8, args); }))
                .Bind(ContentKeyFunctions.Hotbar9,
                new PointerInputCmdHandler((in PointerInputCmdArgs args) => { return HandleHotbarKeybindPressed(9, args); }))
                .Register<HotbarSystem>();
        }

        public override void Shutdown()
        {
            base.Shutdown();

            CommandBinds.Unregister<HotbarSystem>();
        }

        public override void FrameUpdate(float frameTime)
        {
            base.FrameUpdate(frameTime);

            if (!_gameTiming.IsFirstTimePredicted)
            {
                return;
            }

            var playerEnt = _playerManager.LocalPlayer.ControlledEntity;
            if (playerEnt == null
                || !playerEnt.TryGetComponent(out HotbarComponent clientHotbar))
            {
                return;
            }

            clientHotbar.UpdateCooldown(_gameTiming.CurTime);
        }

        private void HandleOpenActionsMenu()
        {
            var playerEnt = _playerManager.LocalPlayer.ControlledEntity;
            if (playerEnt == null
                || !playerEnt.TryGetComponent(out HotbarComponent clientHotbar))
            {
                return;
            }

            clientHotbar.OpenActionMenu();
        }

        private bool HandleHotbarKeybindPressed(int index, in PointerInputCmdArgs args)
        {
            if (_playerManager.LocalPlayer.ControlledEntity == null
                || !_playerManager.LocalPlayer.ControlledEntity.TryGetComponent(out HotbarComponent hotbarComponent))
            {
                return false;
            }

            hotbarComponent.TriggerAction(index, args);
            return true;
        }
    }

    public class HotbarAction
    {
        public string Name;
        public HotbarActionId Id;
        public Texture Texture;
        public bool Active;
        public Action<HotbarAction, ICommonSession, GridCoordinates, EntityUid> ActivateAction;
        public Action<HotbarAction, bool> SelectAction;
        public Action<HotbarAction> OnRemoveFromSlot;
        public TimeSpan? Start;
        public TimeSpan? End;

        public HotbarAction(string name, string texturePath, Action<HotbarAction, ICommonSession, GridCoordinates, EntityUid> activateAction, Action<HotbarAction, bool> selectAction)
        {
            Name = name;
            if (texturePath != null)
            {
                var resCache = IoCManager.Resolve<IResourceCache>();
                Texture = resCache.GetTexture(texturePath);
            }
            else
            {
                Texture = null;
            }
            Active = false;
            ActivateAction = activateAction;
            SelectAction = selectAction;
            Start = null;
            End = null;
        }

        public void Activate(PointerInputCmdArgs args)
        {
            ActivateAction?.Invoke(this, args.Session, args.Coordinates, args.EntityUid);
        }

        public void Toggle(bool pressed)
        {
            Active = pressed;
            SelectAction?.Invoke(this, pressed);
        }

        public void RemovedFromHotbar()
        {
            OnRemoveFromSlot?.Invoke(this);
        }
    }

    public class GetActionsMessage : ComponentMessage
    {
        public HotbarComponent Hotbar;

        public GetActionsMessage(HotbarComponent hotbar)
        {
            Hotbar = hotbar;
        }
    }
}
