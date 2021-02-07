using Robust.Shared.GameObjects;
using Robust.Shared.Map;

namespace Content.Server.Atmos
{
    public interface ITemperatureExpose
    {
        void TemperatureExpose(GasMixture air, float exposedTemperature, float exposedVolume);
    }
}
