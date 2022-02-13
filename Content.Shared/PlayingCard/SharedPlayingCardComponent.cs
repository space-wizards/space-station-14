using Robust.Shared.Serialization;
using System;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Players;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.ViewVariables;


namespace Content.Shared.PlayingCard
{
    [NetworkedComponent, Friend(typeof(SharedPlayingCardSystem))]
    public abstract class SharedPlayingCardComponent : Component, ISerializationHooks
    {
        [ViewVariables(VVAccess.ReadWrite)]
        // [DataField("stackType", required:true, customTypeSerializer:typeof(PrototypeIdSerializer<StackPrototype>))]
        public string StackTypeId { get; private set; } = string.Empty;

        // ID OF CARD SET

        // LIST OF CARDS IN ORDER

        // CAN GRAB FROM ABOVE
        // HOW MAY COUNTS IN PILE

        // IF DECK?

        public bool IsUpright = false;

        public bool IsDeck = false;

        public List<String> CardList = new List<String>();

        /// <summary>
        ///     Current stack count.
        ///     Do NOT set this directly, use the <see cref="SharedCardsSystem.SetCount"/> method instead.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("count")]
        public int Count { get; set; } = 30;


        [ViewVariables(VVAccess.ReadOnly)]
        [DataField("max")]
        public int MaxCount  { get; set; } = 30;
    }


    [Serializable, NetSerializable]
    public sealed class PlayingCardComponentState : ComponentState
    {
        public int Count { get; }
        public int ID { get; }
        public int MaxCount { get; }

        public PlayingCardComponentState(int id, int count, int maxCount)
        {
            ID = id;
            Count = count;
            MaxCount = maxCount;
        }
    }
}
