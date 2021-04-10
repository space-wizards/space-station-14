using Content.Server.Atmos;

namespace Content.Server.Interfaces.GameObjects
{
    public interface IAtmosProcess
    {
        void ProcessAtmos(float time, IGridAtmosphereComponent atmosphere);
    }
}
