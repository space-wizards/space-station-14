using System.Diagnostics.CodeAnalysis;
using Content.Shared.DoAfter;
using JetBrains.Annotations;
using Robust.Shared.Serialization;

namespace Content.Shared.Wires
{
    [Serializable, NetSerializable]
    public sealed partial class WirePanelDoAfterEvent : SimpleDoAfterEvent
    {
    }

    [Serializable, NetSerializable]
    public enum WiresVisuals : byte
    {
        MaintenancePanelState
    }

    [Serializable, NetSerializable]
    public enum WiresUiKey : byte
    {
        Key,
    }

    [Serializable, NetSerializable]
    public enum WiresAction : byte
    {
        Mend,
        Cut,
        Pulse,
    }

    [Serializable, NetSerializable]
    public enum StatusLightState : byte
    {
        Off,
        On,
        BlinkingFast,
        BlinkingSlow
    }

    [Serializable, NetSerializable]
    public sealed class WiresActionMessage : BoundUserInterfaceMessage
    {
        public readonly int Id;
        public readonly WiresAction Action;

        public WiresActionMessage(int id, WiresAction action)
        {
            Id = id;
            Action = action;
        }
    }

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [PublicAPI]
    [Serializable, NetSerializable]
    public enum WireLetter : byte
    {
        α,
        β,
        γ,
        δ,
        ε,
        ζ,
        η,
        θ,
        ι,
        κ,
        λ,
        μ,
        ν,
        ξ,
        ο,
        π,
        ρ,
        σ,
        τ,
        υ,
        φ,
        χ,
        ψ,
        ω
    }

    [PublicAPI]
    [Serializable, NetSerializable]
    public enum WireColor : byte
    {
        Red,
        Blue,
        Green,
        Orange,
        Brown,
        Gold,
        Gray,
        Cyan,
        Navy,
        Purple,
        Pink,
        Fuchsia
    }

    [Serializable, NetSerializable]
    public struct StatusLightData
    {
        public StatusLightData(Color color, StatusLightState state, string text)
        {
            Color = color;
            State = state;
            Text = text;
        }

        public Color Color { get; }
        public StatusLightState State { get; }
        public string Text { get; }

        public override string ToString()
        {
            return $"Color: {Color}, State: {State}, Text: {Text}";
        }
    }

    [Serializable, NetSerializable]
    public sealed class WiresBoundUserInterfaceState : BoundUserInterfaceState
    {
        public string BoardName { get; }
        public string? SerialNumber { get; }
        public ClientWire[] WiresList { get; }
        public StatusEntry[] Statuses { get; }
        public int WireSeed { get; }

        public WiresBoundUserInterfaceState(ClientWire[] wiresList, StatusEntry[] statuses, string boardName, string? serialNumber, int wireSeed)
        {
            BoardName = boardName;
            SerialNumber = serialNumber;
            WireSeed = wireSeed;
            WiresList = wiresList;
            Statuses = statuses;
        }
    }

    [Serializable, NetSerializable]
    public struct StatusEntry
    {
        /// <summary>
        ///     The key of this status, according to the status dictionary
        ///     server side.
        /// </summary>
        public readonly object Key;

        /// <summary>
        ///     The value of this status, according to the status dictionary
        ///     server side..
        /// </summary>
        public readonly object Value;

        public StatusEntry(object key, object value)
        {
            Key = key;
            Value = value;
        }

        public override string ToString()
        {
            return $"{Key}, {Value}";
        }
    }


    /// <summary>
    ///     ClientWire, sent by the server so that the client knows
    ///     what wires there are on an entity.
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class ClientWire
    {
        /// <summary>
        ///     ID of this wire, which corresponds to
        ///     the ID server side.
        /// </summary>
        public int Id;

        /// <summary>
        ///     Whether this wire is cut or not.
        /// </summary>
        public bool IsCut;

        /// <summary>
        ///     Current color of the wire.
        /// </summary>
        public WireColor Color;

        /// <summary>
        ///     Current letter of the wire.
        /// </summary>
        public WireLetter Letter;

        public ClientWire(int id, bool isCut, WireColor color, WireLetter letter)
        {
            Id = id;
            IsCut = isCut;
            Letter = letter;
            Color = color;
        }
    }

    public static class HackingWiresExt
    {
        public static string Name(this WireColor color)
        {
            return Loc.GetString("Red");
        }

        public static Color ColorValue(this WireColor color)
        {
            return Color.Red;
        }

        public static string Name(this WireLetter letter)
        {
            return Loc.GetString("Sigma");
        }

        public static char Letter(this WireLetter letter)
        {
            return 'σ';
        }
    }
}
