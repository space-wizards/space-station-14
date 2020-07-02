using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Items
{
    public abstract class SharedHandsComponent : Component
    {
        public sealed override string Name => "Hands";
        public sealed override uint? NetID => ContentNetIDs.HANDS;
    }

    [Serializable, NetSerializable]
    public sealed class Hand
    {
        public readonly string Name;
        public readonly EntityUid? EntityUid;
        public readonly HandLocation Location;

        public Hand(string name, EntityUid? entityUid, HandLocation location)
        {
            Name = name;
            EntityUid = entityUid;
            Location = location;
        }

        [CanBeNull] public IEntity Entity { get; private set; }

        public void Initialize(IEntityManager manager)
        {
            if (Entity == null || !EntityUid.HasValue)
            {
                return;
            }

            manager.TryGetEntity(EntityUid.Value, out var entity);
            Entity = entity;
        }
    }

    // The IDs of the items get synced over the network.
    [Serializable, NetSerializable]
    public class HandsComponentState : ComponentState
    {
        public readonly List<Hand> Hands;
        public readonly string ActiveIndex;

        public HandsComponentState(List<Hand> hands, string activeIndex) : base(ContentNetIDs.HANDS)
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
