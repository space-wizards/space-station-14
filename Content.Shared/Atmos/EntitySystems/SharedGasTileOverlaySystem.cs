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
        [ViewVariables] public readonly ThermalByte ByteGasTemperature;


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

[Serializable]
/// <summary>
/// This struct is used to send air temperature on screen to all users.   
/// </summary>
public struct ThermalByte
{
    public const float TempMinimum = 0f;
    public const float TempMaximum = 1000f;
    public const int TempResolution = 250;

    public const byte RESERVED_FUTURE1 = 252;
    public const byte RESERVED_FUTURE2 = 253;
    public const byte STATE_VACUUM = 254;
    public const byte STATE_WALL = 255;

    public static readonly float TempDegreeResolution = (TempMaximum - TempMinimum) / TempResolution;

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

    public void SetWall() => _coreValue = STATE_WALL;
    public void SetVacuum() => _coreValue = STATE_VACUUM;
    public bool IsWall => _coreValue == STATE_WALL;
    public bool IsVacuum => _coreValue == STATE_VACUUM;
    public byte Value => _coreValue;

    /// <summary>
    /// Attempts to get the air temperature in Kelvin. If onVaccumReturnTCMB is true it will return Cosmic Microwave Background Temperature 
    /// </summary>
    /// <param name="temperature">The temperature in Kelvin, if the tile is valid air.</param>
    /// <returns>
    /// True if the tile contains valid temperature; false if it is a wall or vacuum(with onVaccumReturnTCMB set to false).
    /// </returns>
    public readonly bool TryGetTemperature(out float temperature, bool onVaccumReturnTCMB = true)
    {
        if (_coreValue == STATE_WALL)
        {
            temperature = 0f;
            return false;
        }
        else if (_coreValue == STATE_VACUUM)
        {
            temperature = Atmospherics.TCMB;
            return true; ;
        }

        temperature = (_coreValue * TempDegreeResolution) + TempMinimum;
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
