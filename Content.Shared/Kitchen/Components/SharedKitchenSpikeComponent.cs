using System;
using Content.Shared.DragDrop;
using Content.Shared.Nutrition.Components;
using Content.Shared.Sound;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Kitchen.Components
{
    public abstract class SharedKitchenSpikeComponent : Component, IDragDropOn
    {
        [ViewVariables]
        [DataField("delay")]
        public float SpikeDelay = 12.0f;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("sound")]
        public SoundSpecifier SpikeSound = new SoundPathSpecifier("/Audio/Effects/Fluids/splat.ogg");

        bool IDragDropOn.CanDragDropOn(DragDropEvent eventArgs)
        {
            if (!IoCManager.Resolve<IEntityManager>().HasComponent<SharedButcherableComponent>(eventArgs.Dragged))
            {
                return false;
            }

            // TODO: Once we get silicons need to check organic
            return true;
        }

        public abstract bool DragDropOn(DragDropEvent eventArgs);

        [Serializable, NetSerializable]
        public enum KitchenSpikeVisuals : byte
        {
            Status
        }

        [Serializable, NetSerializable]
        public enum KitchenSpikeStatus : byte
        {
            Empty,
            Bloody
        }
    }
}
