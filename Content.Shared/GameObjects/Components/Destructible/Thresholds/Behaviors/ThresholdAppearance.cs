#nullable enable
using System;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Shared.GameObjects.Components.Destructible.Thresholds.Behaviors
{
    [Serializable, NetSerializable]
    public struct ThresholdAppearance : IExposeData
    {
        /// <summary>
        ///     The RSI to use when this threshold is reached.
        ///     If null, it will not be changed.
        /// </summary>
        [ViewVariables] public string? Sprite;

        /// <summary>
        ///     The sprite state to use when this threshold is reached.
        ///     If null, it will not be changed.
        /// </summary>
        [ViewVariables] public string? State;

        /// <summary>
        ///     The sprite layer to modify with <see cref="Sprite"/> and
        ///     <see cref="State"/> when this threshold is reached.
        ///     If null, it will modify the base sprite instead.
        /// </summary>
        [ViewVariables] public int? Layer;

        void IExposeData.ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(ref Sprite, "sprite", null);
            serializer.DataField(ref State, "state", null);
            serializer.DataField(ref Layer, "layer", null);
        }
    }
}
