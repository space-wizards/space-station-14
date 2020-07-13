using System.Runtime.CompilerServices;
using Content.Shared.Atmos;

namespace Content.Server.Interfaces.Atmos
{
    public interface IAtmosphere
    {
        GasProperty[] Gasses { get; }
        float Moles { get; }
        float Pressure { get; }
        float Temperature { get; set; }
        float Volume { get; }
    }
}
