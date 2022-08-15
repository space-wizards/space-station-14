using Content.Server.Radiation.Systems;
using JetBrains.Annotations;

namespace Content.Server.Radiation
{
    [UsedImplicitly]
    public sealed class RadiationPulseSystem : EntitySystem
    {
        [Dependency] private readonly RadiationSystem _radiation = default!;


    }
}
