using Content.Shared.Atmos.EntitySystems;
using Content.Shared.Atmos.Reactions;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Serialization;
using System.Linq;
using System.Xml.Linq;
using YamlDotNet.Core.Tokens;

namespace Content.Shared.Atmos.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class GasAnalyzerComponent : Component
{
    [ViewVariables]
    public EntityUid? Target;

    [ViewVariables]
    public EntityUid User;

    [DataField("enabled"), ViewVariables(VVAccess.ReadWrite)]
    public bool Enabled;

    [Serializable, NetSerializable]
    public enum GasAnalyzerUiKey
    {
        Key,
    }

    /// <summary>
    /// Atmospheric data is gathered in the system and sent to the user
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class GasAnalyzerUserMessage : BoundUserInterfaceMessage
    {
        public string DeviceName;
        public NetEntity DeviceUid;
        public bool DeviceFlipped;
        public string? Error;
        public GasMixEntry[] NodeGasMixes;
        public GasAnalyzerUserMessage(GasMixEntry[] nodeGasMixes, string deviceName, NetEntity deviceUid, bool deviceFlipped, string? error = null)
        {
            NodeGasMixes = nodeGasMixes;
            DeviceName = deviceName;
            DeviceUid = deviceUid;
            DeviceFlipped = deviceFlipped;
            Error = error;
        }
    }

    /// <summary>
    /// Contains information on a gas mix entry, turns into a tab in the UI
    /// </summary>
    [Serializable, NetSerializable]
    public struct GasMixEntry
    {
        /// <summary>
        /// Name of the tab in the UI
        /// </summary>
        public readonly string Name;
        public readonly float Volume;
        public readonly float Pressure;
        public readonly float Temperature;
        public readonly GasPacket? Gases;

        public GasMixEntry(string name, float volume, float pressure, float temperature, GasPacket? gases = null)
        {
            Name = name;
            Volume = volume;
            Pressure = pressure;
            Temperature = temperature;
            Gases = gases;
        }
    }

    /// <summary>
    /// Packed bitfield Dto of gases, as well as their corresponding amounts for efficient network transmission.
    /// </summary>
    [Serializable, NetSerializable]
    readonly public struct GasPacket
    {
        // Bitfield of which gases are present in the mixture, LSB->MSB corresponds with Gas enum order.
        // 1 bit per gas, so max 16 gases with a ushort. Gases with amounts below UIMinMoles are not included.
        public readonly ushort GasBitfield;

        // List of gas amounts in the mixture, in the same order as the bitfield
        public readonly float[]? Moles;


        // Improve maintainability by throwing a compiler error if the bitfield size is too small to represent all gases.
        private const byte BitsInGasBitfield = 16;
        private const byte ERROR_BitsIn_gasBitfieldTooSmall__IncreaseBitfieldSize =
            Atmospherics.TotalNumberOfGases <= BitsInGasBitfield ? 42 : -42; // Intentional compiler error if bitfield size is too small

        public GasPacket(ushort bitfield, float[]? moles)
        {
            GasBitfield = bitfield;
            Moles = moles;
        }

        /// <summary>
        /// Determines whether the current object has a nonzero gas bitfield and a non-empty, non-null moles array.
        /// </summary>
        /// <returns>true if the gas bitfield is not zero and the moles array is not null or empty; otherwise, false.</returns>
        public bool IsValid() => GasBitfield != 0 && (Moles?.Length ?? 0) > 0;

        /// <summary>
        /// Unpacks the gas packet into an array of gas names, amounts, and colors.
        /// </summary>
        /// <returns>An array of tuples containing the name, amount, and color of each gas present in the packet. Returns an
        /// empty array if the packet is invalid.</returns>
        public (string Name, float Amount, string Color)[] UnpackGasPacket()
        {

            if (IsValid() == false)
                return Array.Empty<(string Name, float Amount, string Color)>();

            // Utilize Gas prototype methods to fetch gas names and colors as they are unpacked
            var atmo = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<SharedAtmosphereSystem>();
            var results = new List<(string Name, float Amount, string Color)>();
            var bits = this.GasBitfield;
            var amountIndex = 0;

            for (var i = 0; i < Atmospherics.TotalNumberOfGases; i++)
            {
                var mask = (1 << i);
                if ((bits & mask) != 0)
                {
                    var gasProto = atmo.GetGas(i);
                    var name = gasProto.Name;
                    var color = gasProto.Color;
                    var amount = Moles![amountIndex];
                    results.Add((name, amount, color));
                    amountIndex++;
                }
            }

            return results.ToArray();
        }

    }
}

[Serializable, NetSerializable]
public enum GasAnalyzerVisuals : byte
{
    Enabled,
}

