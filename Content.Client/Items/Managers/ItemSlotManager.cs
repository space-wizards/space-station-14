using System;
using System.Collections.Generic;
using Content.Client.Examine;
using Content.Client.Items.UI;
using Content.Client.Storage;
using Content.Client.Verbs;
using Content.Shared.Cooldown;
using Content.Shared.Hands.Components;
using Content.Shared.Input;
using Content.Shared.Interaction;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Shared.GameObjects;
using Robust.Shared.Input;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Timing;

namespace Content.Client.Items.Managers
{
    public class ItemSlotManager : IItemSlotManager
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;
        [Dependency] private readonly IUserInterfaceManager _uiMgr = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;

        private readonly HashSet<EntityUid> _highlightEntities = new();

        public event Action<EntitySlotHighlightedEventArgs>? EntityHighlightedUpdated;

        public bool SetItemSlot(ItemSlotButton button, IEntity? entity)
        {
            if (entity == null)
            {
                button.SpriteView.Sprite = null;
                button.StorageButton.Visible = false;
            }
            else
            {
                ISpriteComponent? sprite;
                if (entity.TryGetComponent(out HandVirtualItemComponent? virtPull)
                    && _entityManager.TryGetComponent(virtPull.BlockingEntity, out ISpriteComponent pulledSprite))
                {
                    sprite = pulledSprite;
                }
                else if (!entity.TryGetComponent(out sprite))
                {
                    return false;
                }

                button.ClearHover();
                button.SpriteView.Sprite = sprite;
                button.StorageButton.Visible = entity.HasComponent<ClientStorageComponent>();
            }

            button.Entity = entity?.Uid ?? default;

            // im lazy
            button.UpdateSlotHighlighted();
            return true;
        }

        public bool OnButtonPressed(GUIBoundKeyEventArgs args, IEntity? item)
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
                _entitySystemManager.GetEntitySystem<VerbSystem>().VerbMenu.OpenVerbMenu(item);
            }
            else if (args.Function == ContentKeyFunctions.ActivateItemInWorld)
            {
                _entityManager.EntityNetManager?.SendSystemNetworkMessage(new InteractInventorySlotEvent(item.Uid, altInteract: false));
            }
            else if (args.Function == ContentKeyFunctions.AltActivateItemInWorld)
            {
                _entityManager.EntityNetManager?.SendSystemNetworkMessage(new InteractInventorySlotEvent(item.Uid, altInteract: true));
            }
            else
            {
                return false;
            }
            args.Handle();
            return true;
        }

        public void UpdateCooldown(ItemSlotButton? button, IEntity? entity)
        {
            var cooldownDisplay = button?.CooldownDisplay;

            if (cooldownDisplay == null)
            {
                return;
            }

            if (entity == null ||
                entity.Deleted ||
                !entity.TryGetComponent(out ItemCooldownComponent? cooldown) ||
                !cooldown.CooldownStart.HasValue ||
                !cooldown.CooldownEnd.HasValue)
            {
                cooldownDisplay.Visible = false;
                return;
            }

            var start = cooldown.CooldownStart.Value;
            var end = cooldown.CooldownEnd.Value;

            var length = (end - start).TotalSeconds;
            var progress = (_gameTiming.CurTime - start).TotalSeconds / length;
            var ratio = (progress <= 1 ? (1 - progress) : (_gameTiming.CurTime - end).TotalSeconds * -5);

            cooldownDisplay.Progress = MathHelper.Clamp((float) ratio, -1, 1);
            cooldownDisplay.Visible = ratio > -1f;
        }

        public void HoverInSlot(ItemSlotButton button, IEntity? entity, bool fits)
        {
            if (entity == null || !button.MouseIsHovering)
            {
                button.ClearHover();
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

            button.HoverSpriteView.Sprite = hoverSprite;
        }

        public bool IsHighlighted(EntityUid uid)
        {
            return _highlightEntities.Contains(uid);
        }

        public void HighlightEntity(EntityUid uid)
        {
            if (!_highlightEntities.Add(uid))
                return;

            EntityHighlightedUpdated?.Invoke(new EntitySlotHighlightedEventArgs(uid, true));
        }

        public void UnHighlightEntity(EntityUid uid)
        {
            if (!_highlightEntities.Remove(uid))
                return;

            EntityHighlightedUpdated?.Invoke(new EntitySlotHighlightedEventArgs(uid, false));
        }
    }

    public readonly struct EntitySlotHighlightedEventArgs
    {
        public EntitySlotHighlightedEventArgs(EntityUid entity, bool newHighlighted)
        {
            Entity = entity;
            NewHighlighted = newHighlighted;
        }

        public EntityUid Entity { get; }
        public bool NewHighlighted { get; }
    }
}
