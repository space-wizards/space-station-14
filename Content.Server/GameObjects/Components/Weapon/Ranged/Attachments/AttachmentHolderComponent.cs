using System;
using System.Collections.Generic;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.Interfaces;
using Robust.Server.GameObjects.Components.Container;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Weapon.Ranged.Attachments
{
    [RegisterComponent]
    public sealed class AttachmentHolderComponent : Component, IAttackBy
    {
        // Currently this is the big poopoo and should be done by a UI but I was too lazy to do it that way for now
        public override string Name => "AttachmentHolder";
        private Dictionary<AttachmentSlot, IEntity> _equippedSlots = new Dictionary<AttachmentSlot, IEntity>
        {
            {AttachmentSlot.Muzzle, null},
        };
        public AttachmentSlot AllSlots => _allSlots;
        private AttachmentSlot _allSlots;
        private Container _container;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _allSlots, "slots", AttachmentSlot.None);
        }

        public override void Initialize()
        {
            base.Initialize();
            _container = ContainerManagerComponent.Ensure<Container>($"{Name}-attachments", Owner);
        }

        public bool TryInsertAttachment(IEntity user, IEntity entity)
        {
            if (!entity.TryGetComponent(out AttachmentMuzzleComponent attachmentMuzzle))
            {
                return false;
            }

            if (_equippedSlots[attachmentMuzzle.Slot] != null)
            {
                Owner.PopupMessage(user, Loc.GetString("Slot full"));
                return false;
            }

            attachmentMuzzle.Attached();
            _equippedSlots[attachmentMuzzle.Slot] = entity;
            _container.Insert(entity);
            return true;
        }

        public IEntity RemoveAttachment(AttachmentSlot slot)
        {
            if ((_allSlots & slot) == 0)
            {
                return null;
            }

            if (!_equippedSlots.TryGetValue(slot, out var entity))
            {
                return null;
            }

            var attachment = entity.GetComponent<AttachmentMuzzleComponent>();
            attachment.Detached();
            _equippedSlots[slot] = null;
            _container.Remove(entity);
            return entity;
        }

        bool IAttackBy.AttackBy(AttackByEventArgs eventArgs)
        {
            return TryInsertAttachment(eventArgs.User, eventArgs.AttackWith);
        }
    }

    [Flags]
    public enum AttachmentSlot
    {
        None = 0,
        Muzzle = 1 << 0,
    }
}