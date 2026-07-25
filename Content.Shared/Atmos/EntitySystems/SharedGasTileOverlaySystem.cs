using Robust.Shared.Configuration;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Atmos.EntitySystems;

public abstract partial class SharedGasTileOverlaySystem : EntitySystem
{
    public const int ChunkSize = ChunkEntitySystem.ChunkSize;
    public const int MaxPackedOpacityGases = 8;
    protected float AccumulatedFrameTime;

    [Dependency] protected IConfigurationManager ConfMan = default!;
    [Dependency] private SharedAtmosphereSystem _atmosphere = default!;

    /// <summary>
    ///     Bitmask of all gases with visible overlays. Bit N corresponds to gas ID N.
    /// </summary>
    public uint VisibleGasId { get; private set; }

    /// <summary>
    ///     Number of gas IDs present in <see cref="VisibleGasId"/>.
    /// </summary>
    public int VisibleGasCount { get; private set; }

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GasOverlayChunkComponent, ComponentGetState>(OnGetState);
        SubscribeLocalEvent<GasOverlayChunkComponent, ComponentHandleState>(OnHandleState);

        if (Atmospherics.TotalNumberOfGases > sizeof(uint) * 8)
            throw new InvalidOperationException("Gas overlay visibility mask supports at most 32 gases.");

        VisibleGasId = 0;
        VisibleGasCount = 0;

        for (var i = 0; i < Atmospherics.TotalNumberOfGases; i++)
        {
            var gasPrototype = _atmosphere.GetGas(i);
            if (gasPrototype.GasOverlaySprite != null)
            {
                VisibleGasId |= GetGasIdMask(i);
                VisibleGasCount++;
            }
        }

        if (VisibleGasCount > MaxPackedOpacityGases)
            throw new InvalidOperationException($"Gas overlay opacity supports at most {MaxPackedOpacityGases} visible gases.");
    }

    public bool IsGasVisible(int gasId)
    {
        DebugTools.Assert(gasId >= 0 && gasId < Atmospherics.TotalNumberOfGases);
        return (VisibleGasId & GetGasIdMask(gasId)) != 0;
    }

    private static uint GetGasIdMask(int gasId)
    {
        return 1u << gasId;
    }

    private void OnGetState(EntityUid uid, GasOverlayChunkComponent component, ref ComponentGetState args)
    {
        if (args.FromTick <= component.CreationTick)
        {
            var fireData = new GasOverlayFireData[component.FireData.Length];
            var opacityData = new GasOverlayOpacityData[component.OpacityData.Length];
            var temperatureData = new GasOverlayTemperatureData[component.TemperatureData.Length];

            Array.Copy(component.FireData, fireData, component.FireData.Length);
            Array.Copy(component.OpacityData, opacityData, component.OpacityData.Length);
            Array.Copy(component.TemperatureData, temperatureData, component.TemperatureData.Length);

            args.State = new GasOverlayChunkState(fireData, opacityData, temperatureData);
            return;
        }

        var fireCount = 0;
        var opacityCount = 0;
        var temperatureCount = 0;

        for (var i = 0; i < component.FireData.Length; i++)
        {
            if (component.FireLastModified[i] > args.FromTick)
                fireCount++;

            if (component.OpacityLastModified[i] > args.FromTick)
                opacityCount++;

            if (component.TemperatureLastModified[i] > args.FromTick)
                temperatureCount++;
        }

        var fire = fireCount == 0 ? Array.Empty<GasOverlayFireDelta>() : new GasOverlayFireDelta[fireCount];
        var opacity = opacityCount == 0 ? Array.Empty<GasOverlayOpacityDelta>() : new GasOverlayOpacityDelta[opacityCount];
        var temperature = temperatureCount == 0 ? Array.Empty<GasOverlayTemperatureDelta>() : new GasOverlayTemperatureDelta[temperatureCount];

        fireCount = 0;
        opacityCount = 0;
        temperatureCount = 0;

        for (var i = 0; i < component.FireData.Length; i++)
        {
            var index = (byte) i;

            if (component.FireLastModified[i] > args.FromTick)
                fire[fireCount++] = new GasOverlayFireDelta(index, component.FireData[i]);

            if (component.OpacityLastModified[i] > args.FromTick)
                opacity[opacityCount++] = new GasOverlayOpacityDelta(index, component.OpacityData[i]);

            if (component.TemperatureLastModified[i] > args.FromTick)
                temperature[temperatureCount++] = new GasOverlayTemperatureDelta(index, component.TemperatureData[i]);
        }

        args.State = new GasOverlayChunkDeltaState(fire, opacity, temperature);
    }

    private void OnHandleState(EntityUid uid, GasOverlayChunkComponent component, ref ComponentHandleState args)
    {
        switch (args.Current)
        {
            case GasOverlayChunkState state:
                if (component.FireData.Length != state.FireData.Length)
                    component.FireData = new GasOverlayFireData[state.FireData.Length];
                if (component.OpacityData.Length != state.OpacityData.Length)
                    component.OpacityData = new GasOverlayOpacityData[state.OpacityData.Length];
                if (component.TemperatureData.Length != state.TemperatureData.Length)
                    component.TemperatureData = new GasOverlayTemperatureData[state.TemperatureData.Length];

                Array.Copy(state.FireData, component.FireData, state.FireData.Length);
                Array.Copy(state.OpacityData, component.OpacityData, state.OpacityData.Length);
                Array.Copy(state.TemperatureData, component.TemperatureData, state.TemperatureData.Length);
                break;
            case GasOverlayChunkDeltaState delta:
                foreach (var modified in delta.ModifiedFire)
                {
                    component.FireData[modified.Index] = modified.Data;
                }

                foreach (var modified in delta.ModifiedOpacity)
                {
                    component.OpacityData[modified.Index] = modified.Data;
                }

                foreach (var modified in delta.ModifiedTemperature)
                {
                    component.TemperatureData[modified.Index] = modified.Data;
                }
                break;
        }
    }

    public static Vector2i GetGasChunkIndices(Vector2i indices)
    {
        return new Vector2i((int)MathF.Floor((float)indices.X / ChunkSize), (int)MathF.Floor((float)indices.Y / ChunkSize));
    }

    [Serializable, NetSerializable]
    public readonly struct GasOverlayFireData(byte fireState) : IEquatable<GasOverlayFireData>
    {
        [ViewVariables] public readonly byte FireState = fireState;

        public bool Equals(GasOverlayFireData other)
        {
            return FireState == other.FireState;
        }
    }

    [Serializable, NetSerializable]
    public readonly struct GasOverlayOpacityData(ulong packedOpacity) : IEquatable<GasOverlayOpacityData>
    {
        [ViewVariables] public readonly ulong PackedOpacity = packedOpacity;

        public byte GetOpacity(int index)
        {
            DebugTools.Assert(index >= 0 && index < MaxPackedOpacityGases);
            return (byte) (PackedOpacity >> (index * 8));
        }

        public static ulong SetOpacity(ulong packedOpacity, int index, byte opacity)
        {
            DebugTools.Assert(index >= 0 && index < MaxPackedOpacityGases);
            var shift = index * 8;
            var mask = 0xFFUL << shift;
            return (packedOpacity & ~mask) | ((ulong) opacity << shift);
        }

        public bool Equals(GasOverlayOpacityData other)
        {
            return PackedOpacity == other.PackedOpacity;
        }
    }

    [Serializable, NetSerializable]
    public readonly struct GasOverlayTemperatureData(ThermalByte byteTemp, bool active = true)
        : IEquatable<GasOverlayTemperatureData>
    {
        /// <summary>
        /// Whether this tile has authoritative temperature overlay data. This distinguishes absent chunk data from
        /// a real tile at <see cref="ThermalByte.TempMinimum"/>.
        /// </summary>
        [ViewVariables] public readonly bool Active = active;

        [ViewVariables] public readonly ThermalByte ByteGasTemperature = byteTemp;

        public bool Equals(GasOverlayTemperatureData other)
        {
            return Active == other.Active && ByteGasTemperature == other.ByteGasTemperature;
        }
    }

    [Serializable, NetSerializable]
    public readonly struct GasOverlayData : IEquatable<GasOverlayData>
    {
        [ViewVariables] public readonly byte FireState;
        [ViewVariables] public readonly ulong PackedOpacity;
        // TODO change fire color based on ByteTemp

        /// <summary>
        /// Network-synced air temperature, compressed to a single byte per tile for bandwidth optimization.
        /// Note: Values are approximate and may deviate even ~10°C from the precise server side only temperature.
        /// </summary>
        [ViewVariables]
        public readonly ThermalByte ByteGasTemperature;


        public GasOverlayData(byte fireState, ulong packedOpacity, ThermalByte byteTemp)
        {
            FireState = fireState;
            PackedOpacity = packedOpacity;
            ByteGasTemperature = byteTemp;
        }

        public byte GetOpacity(int index)
        {
            DebugTools.Assert(index >= 0 && index < MaxPackedOpacityGases);
            return (byte) (PackedOpacity >> (index * 8));
        }

        public bool Equals(GasOverlayData other)
        {
            if (FireState != other.FireState)
                return false;

            if (PackedOpacity != other.PackedOpacity)
                return false;

            if (ByteGasTemperature != other.ByteGasTemperature)
                return false;

            return true;
        }
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
