#nullable enable
using System;
using Content.Shared.GameObjects.Components.Pulling;
using Content.Shared.Physics.Pull;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Shared.GameObjects.Components.Items
{
    public abstract class SharedHandsComponent : Component, ISharedHandsComponent
    {
        public sealed override string Name => "Hands";
        public sealed override uint? NetID => ContentNetIDs.HANDS;

        [ViewVariables]
        protected ICollidableComponent? PulledObject;

        [ViewVariables]
        protected bool IsPulling => PulledObject != null;

        public bool StartPull(SharedPullableComponent pullable)
        {
            if (Owner == pullable.Owner)
            {
                return false;
            }

            if (!Owner.IsInSameOrNoContainer(pullable.Owner))
            {
                return false;
            }

            if (IsPulling)
            {
                StopPull();
            }

            PulledObject = pullable.Owner.GetComponent<ICollidableComponent>();
            var controller = PulledObject.EnsureController<PullController>();
            return controller.StartPull(Owner.GetComponent<ICollidableComponent>());
        }

        public virtual bool StopPull()
        {
            if (PulledObject != null &&
                PulledObject.TryGetController(out PullController controller))
            {
                return controller.StopPull();
            }

            return false;
        }

        public bool TogglePull(SharedPullableComponent pullable)
        {
            if (PulledObject == null)
            {
                return StartPull(pullable);
            } else if (pullable.Owner.TryGetComponent(out ICollidableComponent? collidable) &&
                       PulledObject == collidable)
            {
                return StopPull();
            }

            return false;
        }

        public override void HandleMessage(ComponentMessage message, IComponent? component)
        {
            base.HandleMessage(message, component);

            if (!(message is PullMessage pullMessage) ||
                pullMessage.Puller.Owner != Owner)
            {
                return;
            }

            switch (message)
            {
                case PullStartedMessage msg:
                    PulledObject = msg.Pulled;
                    break;
                case PullStoppedMessage _:
                    PulledObject = null;
                    break;
            }
        }
    }

    [Serializable, NetSerializable]
    public sealed class SharedHand
    {
        public readonly int Index;
        public readonly string Name;
        public readonly EntityUid? EntityUid;
        public readonly HandLocation Location;

        public SharedHand(int index, string name, EntityUid? entityUid, HandLocation location)
        {
            Index = index;
            Name = name;
            EntityUid = entityUid;
            Location = location;
        }
    }

    // The IDs of the items get synced over the network.
    [Serializable, NetSerializable]
    public class HandsComponentState : ComponentState
    {
        public readonly SharedHand[] Hands;
        public readonly string? ActiveIndex;

        public HandsComponentState(SharedHand[] hands, string? activeIndex) : base(ContentNetIDs.HANDS)
        {
            Hands = hands;
            ActiveIndex = activeIndex;
        }
    }

    /// <summary>
    /// A message that calls the use interaction on an item in hand, presumed for now the interaction will occur only on the active hand.
    /// </summary>
    [Serializable, NetSerializable]
    public class UseInHandMsg : ComponentMessage
    {
        public UseInHandMsg()
        {
            Directed = true;
        }
    }

    /// <summary>
    /// A message that calls the activate interaction on the item in Index.
    /// </summary>
    [Serializable, NetSerializable]
    public class ActivateInHandMsg : ComponentMessage
    {
        public string Index { get; }

        public ActivateInHandMsg(string index)
        {
            Directed = true;
            Index = index;
        }
    }

    [Serializable, NetSerializable]
    public class ClientAttackByInHandMsg : ComponentMessage
    {
        public string Index { get; }

        public ClientAttackByInHandMsg(string index)
        {
            Directed = true;
            Index = index;
        }
    }

    [Serializable, NetSerializable]
    public class ClientChangedHandMsg : ComponentMessage
    {
        public string Index { get; }

        public ClientChangedHandMsg(string index)
        {
            Directed = true;
            Index = index;
        }
    }

    public enum HandLocation : byte
    {
        Left,
        Middle,
        Right
    }
}
