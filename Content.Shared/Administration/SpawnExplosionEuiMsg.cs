using Content.Shared.Eui;
using Robust.Shared.Serialization;
using Robust.Shared.Map;
using Content.Shared.Explosion;
using Content.Shared.Explosion.Components;

namespace Content.Shared.Administration;

public static class SpawnExplosionEuiMsg
{
    /// <summary>
    ///     This message is sent to the server to request explosion preview data.
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class PreviewRequest : EuiMessageBase
    {
        public readonly MapCoordinates Epicenter;
        public readonly string TypeId;
        public readonly float TotalIntensity;
        public readonly float IntensitySlope;
        public readonly float MaxIntensity;

        public PreviewRequest(MapCoordinates epicenter, string typeId, float totalIntensity, float intensitySlope, float maxIntensity)
        {
            Epicenter = epicenter;
            TypeId = typeId;
            TotalIntensity = totalIntensity;
            IntensitySlope = intensitySlope;
            MaxIntensity = maxIntensity;
        }
    }

    /// <summary>
    ///     This message is used to send explosion-preview data to the client.
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class PreviewData : EuiMessageBase
    {
        public readonly float Slope;
        public readonly float TotalIntensity;
        public readonly ExplosionVisualsState Explosion;

        public PreviewData(ExplosionVisualsState explosion, float slope, float totalIntensity)
        {
            Slope = slope;
            TotalIntensity = totalIntensity;
            Explosion = explosion;
        }
    }
}
