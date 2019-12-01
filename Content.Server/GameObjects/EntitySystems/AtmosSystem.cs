using Content.Server.Interfaces.Atmos;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.IoC;

namespace Content.Server.GameObjects.EntitySystems
{
    class AtmosSystem : EntitySystem
    {
#pragma warning disable 649
        [Dependency] private readonly IAtmosphereMap _atmosphereMap;
#pragma warning restore 649

        public override void Update(float frameTime)
        {
            _atmosphereMap.Update(frameTime);
        }
    }
}
