namespace Content.Server.Atmos
{
    public interface ITemperatureExpose
    {
        void TemperatureExpose(GasMixture air, float exposedTemperature, float exposedVolume);
    }
}
