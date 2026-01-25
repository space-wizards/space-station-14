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
        return new((int)MathF.Floor((float)indices.X / ChunkSize), (int)MathF.Floor((float)indices.Y / ChunkSize));
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
/// This struct is used to send air temperature on screen to all users.   
/// </summary>
[Serializable]
public struct ThermalByte
{
    public const float TempMinimum = 0f;
    public const float TempMaximum = 1000f;
    public const int TempResolution = 250;

    public const byte ReservedFuture1 = 252;
    public const byte ReservedFuture2 = 253;
    public const byte StateVaccum = 254;
    public const byte StateWall = 255;

    public const float TempDegreeResolution = (TempMaximum - TempMinimum) / TempResolution;
    public const float TempToByteFactor = TempResolution / (TempMaximum - TempMinimum);

    [DataField("value")]
    private byte _coreValue;

    public ThermalByte(float temperatureKelvin)
    {
        SetTemperature(temperatureKelvin);
    }

    //TODO Converstion between Kelvins and Thermal Byte is linear right now. This means resolution at 250K and 1000K is the same 4 degrees.
    //This propably in the futre should be quadratic(or just linear but with changes to resolution at pre defined points), with higher resolution at normal ranges and lower at extreems
    // This would allow to still transfer temperature info about 1000K but with lower resolution while increasing resolution at room temperature for better atmospheric prediction.
    public void SetTemperature(float temperatureKelvin)
    {
        var clampedTemp = Math.Clamp(temperatureKelvin, TempMinimum, TempMaximum);
        _coreValue = (byte)((clampedTemp - TempMinimum) * TempResolution / (TempMaximum - TempMinimum));
    }

    public void SetWall() => _coreValue = StateWall;
    public void SetVacuum() => _coreValue = StateVaccum;
    public bool IsWall => _coreValue == StateWall;
    public bool IsVacuum => _coreValue == StateVaccum;
    public byte Value => _coreValue;

    /// <summary>
    /// Attempts to get the air temperature in Kelvin. 
    /// </summary>
    /// <param name="temperature">The temperature in Kelvin, if the tile has a valid temperature.</param>
    /// <param name="onVacuumReturnTCMB">
    /// If true and the tile is a vacuum, <paramref name="temperature"/> will be set to <see cref="Atmospherics.TCMB"/> 
    /// and the method will return <see langword="true"/>.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the tile contains a valid temperature (including vacuum if <paramref name="onVacuumReturnTCMB"/> is set); 
    /// otherwise <see langword="false"/> (e.g., walls).
    /// </returns>
    public readonly bool TryGetTemperature(out float temperature, bool onVacuumReturnTCMB = true)
    {
        if (_coreValue == StateWall)
        {
            temperature = 0f;
            return false;
        }
        else if (_coreValue == StateVaccum)
        {
            if (onVacuumReturnTCMB)
            {
                temperature = Atmospherics.TCMB;
                return true;
            }

            temperature = 0f;
            return false;
        }

        temperature = (_coreValue * TempToByteFactor) + TempMinimum;
        return true;
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
