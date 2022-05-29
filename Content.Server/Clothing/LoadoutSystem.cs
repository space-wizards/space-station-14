using Content.Server.Clothing.Components;
using Content.Server.Station.Systems;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Server.Clothing
{
    public sealed class LoadoutSystem : EntitySystem
    {
        [Dependency] private readonly StationSpawningSystem _station = default!;
        [Dependency] private readonly IPrototypeManager _protoMan = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<LoadoutComponent, ComponentStartup>(OnStartup);
        }

        private void OnStartup(EntityUid uid, LoadoutComponent component, ComponentStartup args)
        {
            if (!_protoMan.TryIndex<StartingGearPrototype>(component.Prototype, out var proto))
                return;

            _station.EquipStartingGear(uid, proto, null);
        }
    }
}
