using Content.Shared.Salvage.Magnet;

namespace Content.Shared.Salvage;

public abstract partial class SharedSalvageSystem
{
    public ISalvageMagnetOffering GetSalvageOffering(int seed)
    {
        var rand = new System.Random(seed);

        if (seed % 2 == 0)
        {
            return new AsteroidOffering();
        }
        else
        {
            // TODO: Random wreck
            return new SalvageOffering();
        }
    }
}
