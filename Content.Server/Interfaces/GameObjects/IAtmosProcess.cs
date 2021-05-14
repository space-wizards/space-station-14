using Content.Server.Atmos;

namespace Content.Server.Interfaces.GameObjects
{
    public interface IAtmosProcess
    {
        void ProcessAtmos(IGridAtmosphereComponent atmosphere);
    }
}
