using Content.Shared.Atmos.Components;
using Robust.Shared.Configuration;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Atmos.EntitySystems;

public abstract class SharedGasTileOverlaySystem : EntitySystem
{
    public const byte ChunkSize = 8;
    protected float AccumulatedFrameTime;
    protected bool PvsEnabled;

    [Dependency] protected readonly IPrototypeManager ProtoMan = default!;
    [Dependency] protected readonly IConfigurationManager ConfMan = default!;
    [Dependency] private readonly SharedAtmosphereSystem _atmosphere = default!;

    /// <summary>
    ///     array of the ids of all visible gases.
    /// </summary>
    public int[] VisibleGasId = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GasTileOverlayComponent, ComponentGetState>(OnGetState);

        List<int> visibleGases = new();

        for (var i = 0; i < Atmospherics.TotalNumberOfGases; i++)
        {
            var gasPrototype = _atmosphere.GetGas(i);
            if (!string.IsNullOrEmpty(gasPrototype.GasOverlayTexture) ||
                (!string.IsNullOrEmpty(gasPrototype.GasOverlaySprite) && !string.IsNullOrEmpty(gasPrototype.GasOverlayState)))
                visibleGases.Add(i);
        }
        VisibleGasId = visibleGases.ToArray();
    }

    private void OnGetState(EntityUid uid, GasTileOverlayComponent component, ref ComponentGetState args)
    {
        if (PvsEnabled && !args.ReplayState)
            return;

        // Should this be a full component state or a delta-state?
        if (args.FromTick <= component.CreationTick || args.FromTick <= component.ForceTick)
        {
            args.State = new GasTileOverlayState(component.Chunks);
            return;
        }

        var data = new Dictionary<Vector2i, GasOverlayChunk>();
        foreach (var (index, chunk) in component.Chunks)
        {
            if (chunk.LastUpdate >= args.FromTick)
                data[index] = chunk;
        }

        args.State = new GasTileOverlayDeltaState(data, new(component.Chunks.Keys));
    }

    public static Vector2i GetGasChunkIndices(Vector2i indices)
    {
        return new Vector2i((int)MathF.Floor((float)indices.X / ChunkSize), (int)MathF.Floor((float)indices.Y / ChunkSize));
    }

    [Serializable, NetSerializable]
    public readonly struct GasOverlayData : IEquatable<GasOverlayData>
    {
        [ViewVariables] public readonly byte FireState;
        [ViewVariables] public readonly byte[] Opacity;
        // TODO change fire color based on ByteTemp

        /// <summary>
        /// Network-synced air temperature, compressed to a single byte per tile for bandwidth optimization.
        /// Note: Values are approximate and may deviate even ~10°C from the precise server side only temperature.
        /// </summary>
        [ViewVariables]
        public readonly ThermalByte ByteGasTemperature;


        public GasOverlayData(byte fireState, byte[] opacity, ThermalByte byteTemp)
        {
            FireState = fireState;
            Opacity = opacity;
            ByteGasTemperature = byteTemp;
        }

        public bool Equals(GasOverlayData other)
        {
            if (FireState != other.FireState)
                return false;

            if (Opacity?.Length != other.Opacity?.Length)
                return false;

            if (Opacity != null && other.Opacity != null)
            {
                for (var i = 0; i < Opacity.Length; i++)
                {
                    if (Opacity[i] != other.Opacity[i])
                        return false;
                }
            }

            if (ByteGasTemperature != other.ByteGasTemperature)
                return false;

            return true;
        }
    }

    [Serializable, NetSerializable]
    public sealed class GasOverlayUpdateEvent : EntityEventArgs
    {
        public Dictionary<NetEntity, List<GasOverlayChunk>> UpdatedChunks = new();
        public Dictionary<NetEntity, HashSet<Vector2i>> RemovedChunks = new();
    }
}

/// <summary>
///     Struct for networking gas temperatures to all clients using a single struct(byte) per tile.
/// </summary>
/// <remarks>
///     <para>
///         This struct compresses the gas temperature into a 1-byte value (0-255).
///         It clamps the temperature to a maximum of 1000K and divides it by 4, creating a range of 0-250.
///         This provides a resolution of 4 degrees Kelvin.
///     </para>
///     <para>
///         The remaining bytes are used as special flags:
///         <list type="bullet">
///             <item><description><b>255</b>: Represents a Wall (block cannot hold atmosphere).</description></item>
///             <item><description><b>254</b>: Represents a Vacuum.</description></item>
///             <item><description><b>251-253</b>: Reserved for future use.</description></item>
///         </list>
///     </para>
///     <para>
///         <b>Dirtying Logic:</b> The value is only dirtied and networked if the difference between the
///         networked byte and the real atmosphere byte is greater than 1. This prevents network spam
///         from minor temperature fluctuations (e.g., heating from 1K to 8K will not trigger an update,
///         but hitting 9K moves the byte index enough to sync).
///     </para>
///     <para>
///         Currently, the conversion is linear. Future improvements might involve a quadratic scale
///         or pre-defined resolution points to offer higher precision at room temperatures
///         and lower precision at extreme temperatures (1000K).
///     </para>
/// </remarks>
[Serializable, NetSerializable]
public struct ThermalByte : IEquatable<ThermalByte>
{
    public const float TempMinimum = 0f;
    public const float TempMaximum = 1000f;
    public const int TempResolution = 250;

    public const byte ReservedFuture0 = 251;
    public const byte ReservedFuture1 = 252;
    public const byte ReservedFuture2 = 253;
    public const byte StateVacuum = 254;
    public const byte AtmosImpossible = 255;

    public const float TempDegreeResolution = (TempMaximum - TempMinimum) / TempResolution;
    public const float TempToByteFactor = TempResolution / (TempMaximum - TempMinimum);

    private byte _coreValue;

    public ThermalByte(float temperatureKelvin)
    {
        SetTemperature(temperatureKelvin);
    }

    public ThermalByte()
    {
        _coreValue = AtmosImpossible;
    }

    /// <summary>
    /// Set temperature of air in this in Kelvin.
    /// </summary>
    public void SetTemperature(float temperatureKelvin)
    {
        var clampedTemp = Math.Clamp(temperatureKelvin, TempMinimum, TempMaximum);
        _coreValue = (byte)((clampedTemp - TempMinimum) * TempResolution / (TempMaximum - TempMinimum));
    }

    public void SetAtmosIsImpossible()
    {
        _coreValue = AtmosImpossible;
    }

    public void SetVacuum()
    {
        _coreValue = StateVacuum;
    }

    public bool IsAtmosImpossible => _coreValue == AtmosImpossible; // Cold space, solid walls
    public bool IsVacuum => _coreValue == StateVacuum;
    public byte Value => _coreValue;

    /// <summary>
    /// Attempts to get the air temperature in Kelvin.
    /// </summary>
    /// <param name="temperature">The temperature in Kelvin, if the tile has a valid temperature.</param>
    /// <param name="onVacuumReturnTcmb">
    /// If true and the tile is a vacuum, <paramref name="temperature"/> will be set to <see cref="Atmospherics.TCMB"/>
    /// and the method will return <see langword="true"/>.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the tile contains a valid temperature (including vacuum if <paramref name="onVacuumReturnTcmb"/> is set);
    /// otherwise <see langword="false"/> (e.g., walls).
    /// </returns>
    public readonly bool TryGetTemperature(out float temperature, bool onVacuumReturnTcmb = true)
    {
        switch (_coreValue)
        {
            case AtmosImpossible:
                temperature = 0f;
                return false;
            case StateVacuum when onVacuumReturnTcmb:
                temperature = Atmospherics.TCMB;
                return true;
            case StateVacuum:
                temperature = 0f;
                return false;
            default:
                temperature = (_coreValue * TempDegreeResolution) + TempMinimum;
                return true;
        }
    }

    public bool Equals(ThermalByte other)
    {
        return _coreValue == other._coreValue;
    }

    public static bool operator ==(ThermalByte left, ThermalByte right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ThermalByte left, ThermalByte right)
    {
        return !left.Equals(right);
    }

    public override bool Equals(object? obj)
    {
        return obj is ThermalByte other && Equals(other);
    }

    public override int GetHashCode()
    {
        return _coreValue.GetHashCode();
    }
}
