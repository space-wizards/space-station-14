using System;
using Content.Client.GameObjects;
using Content.Client.GameObjects.Components.Storage;
using Content.Client.GameObjects.EntitySystems;
using Content.Client.Utility;
using Content.Shared.GameObjects.Components.Items;
using Content.Shared.Input;
using Robust.Client.GameObjects.EntitySystems;
using Robust.Client.Graphics;
using Robust.Client.Interfaces.GameObjects.Components;
using Robust.Client.Interfaces.Graphics.ClientEye;
using Robust.Client.Interfaces.Input;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Shared.Input;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.Client.UserInterface
{
    public class ItemSlotManager : IItemSlotManager
    {
#pragma warning disable 0649
        [Dependency] private readonly IPlayerManager _playerManager;
        [Dependency] private readonly IGameTiming _gameTiming;
        [Dependency] private readonly IInputManager _inputManager;
        [Dependency] private readonly IEntitySystemManager _entitySystemManager;
        [Dependency] private readonly IEyeManager _eyeManager;
        [Dependency] private readonly IResourceCache _resourceCache;
#pragma warning restore 0649

        private const int CooldownLevels = 8;

        private readonly Texture[] _texturesCooldownOverlay = new Texture[CooldownLevels];

        public void Initialize()
        {
            for (var i = 0; i < CooldownLevels; i++)
            {
                _texturesCooldownOverlay[i] =
                    _resourceCache.GetTexture($"/Textures/UserInterface/Inventory/cooldown-{i}.png");
            }
        }

        public bool SetItemSlot(ItemSlotButton button, IEntity entity)
        {
            if (entity == null)
            {
                button.SpriteView.Sprite = null;
                button.StorageButton.Visible = false;
            }
            else
            {
                if (!entity.TryGetComponent(out ISpriteComponent sprite))
                    return false;
                button.SpriteView.Sprite = sprite;
                button.StorageButton.Visible = entity.HasComponent<ClientStorageComponent>();
            }
            return true;
        }

        public bool OnButtonPressed(GUIBoundKeyEventArgs args, IEntity item)
        {
            args.Handle();

            if (item == null)
                return false;

            if (args.Function == ContentKeyFunctions.ExamineEntity)
            {
                _entitySystemManager.GetEntitySystem<ExamineSystem>()
                    .DoExamine(item);
            }
            else if (args.Function == ContentKeyFunctions.OpenContextMenu)
            {
                _entitySystemManager.GetEntitySystem<VerbSystem>()
                                    .OpenContextMenu(item, new ScreenCoordinates(args.PointerLocation.Position));
            }
            else if (args.Function == ContentKeyFunctions.ActivateItemInWorld)
            {
                var inputSys = _entitySystemManager.GetEntitySystem<InputSystem>();

                var func = args.Function;
                var funcId = _inputManager.NetworkBindMap.KeyFunctionID(args.Function);

                var mousePosWorld = _eyeManager.ScreenToWorld(args.PointerLocation);
                var message = new FullInputCmdMessage(_gameTiming.CurTick, funcId, BoundKeyState.Down, mousePosWorld,
                    args.PointerLocation, item.Uid);

                // client side command handlers will always be sent the local player session.
                var session = _playerManager.LocalPlayer.Session;
                inputSys.HandleInputCommand(session, func, message);
            }
            else
            {
                return false;
            }
            return true;
        }

        public void UpdateCooldown(ItemSlotButton button, IEntity entity)
        {
            var cooldownTexture = button.CooldownCircle;

            if (entity != null
                && entity.TryGetComponent(out ItemCooldownComponent cooldown)
                && cooldown.CooldownStart.HasValue
                && cooldown.CooldownEnd.HasValue)
            {
                var start = cooldown.CooldownStart.Value;
                var end = cooldown.CooldownEnd.Value;

                var length = (end - start).TotalSeconds;
                var progress = (_gameTiming.CurTime - start).TotalSeconds;
                var ratio = (float)(progress / length);

                var textureIndex = CalculateCooldownLevel(ratio);
                if (textureIndex == CooldownLevels)
                {
                    cooldownTexture.Visible = false;
                }
                else
                {
                    cooldownTexture.Visible = true;
                    cooldownTexture.Texture = _texturesCooldownOverlay[textureIndex];
                }
            }
            else
            {
                cooldownTexture.Visible = false;
            }
        }

        internal static int CalculateCooldownLevel(float cooldownValue)
        {
            var val = cooldownValue.Clamp(0, 1);
            val *= CooldownLevels;
            return (int)Math.Floor(val);
        }
    }
}
