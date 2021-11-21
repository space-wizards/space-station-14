using JetBrains.Annotations;
using Content.Shared.Chemistry.EntitySystems;

namespace Content.Client.Chemistry.EntitySystems
{
    [UsedImplicitly]
    public sealed class ChemMasterSystem : SharedChemMasterSystem
    {
        // gotta love empty client side systems that exist purely because theres one specific thing that can only be
        // done server-side which prevents the whole system from being in shared.
    }
}
