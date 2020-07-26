using System;
using Content.Client.GameObjects;
using Content.Client.GameObjects.Components.Storage;
using Content.Client.GameObjects.EntitySystems;
using Content.Client.Utility;
using Content.Shared.GameObjects.Components.Items;
using Content.Shared.Input;
using Robust.Client.GameObjects;
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
using Robust.Shared.Interfaces.Map;
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
        [Dependency] private readonly IEntityManager _entityManager;
        [Dependency] private readonly IEyeManager _eyeManager;
        [Dependency] private readonly IMapManager _mapManager;
#pragma warning restore 0649

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
                button.EntityHover = false;
                button.SpriteView.Sprite = sprite;
                button.StorageButton.Visible = entity.HasComponent<ClientStorageComponent>();
            }
            return true;
        }

        public bool OnButtonPressed(GUIBoundKeyEventArgs args, IEntity item)
        {
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

                var mousePosWorld = _eyeManager.ScreenToMap(args.PointerLocation);
                if (!_mapManager.TryFindGridAt(mousePosWorld, out var grid))
                    grid = _mapManager.GetDefaultGrid(mousePosWorld.MapId);

                var message = new FullInputCmdMessage(_gameTiming.CurTick, _gameTiming.TickFraction, funcId, BoundKeyState.Down,
                    grid.MapToGrid(mousePosWorld), args.PointerLocation, item.Uid);

                // client side command handlers will always be sent the local player session.
                var session = _playerManager.LocalPlayer.Session;
                inputSys.HandleInputCommand(session, func, message);
            }
            else
            {
                return false;
            }
            args.Handle();
            return true;
        }

        public void UpdateCooldown(ItemSlotButton button, IEntity entity)
        {
            var cooldownDisplay = button.CooldownDisplay;

            if (entity != null
                && entity.TryGetComponent(out ItemCooldownComponent cooldown)
                && cooldown.CooldownStart.HasValue
                && cooldown.CooldownEnd.HasValue)
            {
                var start = cooldown.CooldownStart.Value;
                var end = cooldown.CooldownEnd.Value;

                var length = (end - start).TotalSeconds;
                var progress = (_gameTiming.CurTime - start).TotalSeconds / length;
                var ratio = (progress <= 1 ? (1 - progress) : (_gameTiming.CurTime - end).TotalSeconds * -5);

                cooldownDisplay.Progress = (float)ratio.Clamp(-1, 1);

                if (ratio > -1f)
                {
                    cooldownDisplay.Visible = true;
                }
                else
                {
                    cooldownDisplay.Visible = false;
                }
            }
            else
            {
                cooldownDisplay.Visible = false;
            }
        }

        public void HoverInSlot(ItemSlotButton button, IEntity entity, bool fits)
        {
            if (entity == null || !button.MouseIsHovering)
            {
                button.SpriteView.Sprite?.Owner.Delete();
                button.SpriteView.Sprite = null;
                button.StorageButton.Visible = false;
                return;
            }

            if (!entity.HasComponent<SpriteComponent>())
            {
                return;
            }

            // Set green / red overlay at 50% transparency
            var hoverEntity = _entityManager.SpawnEntity("hoverentity", MapCoordinates.Nullspace);
            var hoverSprite = hoverEntity.GetComponent<SpriteComponent>();
            hoverSprite.CopyFrom(entity.GetComponent<SpriteComponent>());
            hoverSprite.Color = fits ? new Color(0, 255, 0, 127) : new Color(255, 0, 0, 127);

            button.EntityHover = true;
            button.SpriteView.Sprite = hoverSprite;
            button.StorageButton.Visible = entity.HasComponent<ClientStorageComponent>();
        }
    }
}
