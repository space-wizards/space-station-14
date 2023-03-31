using Content.Server.Clothing.Components;
using Content.Server.Station.Systems;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Clothing
{
    /// <summary>
    /// Assigns a loadout to an entity based on the startingGear prototype
    /// </summary>
    public sealed class LoadoutSystem : EntitySystem
    {
        [Dependency] private readonly StationSpawningSystem _station = default!;
        [Dependency] private readonly IPrototypeManager _protoMan = default!;
        [Dependency] private readonly IRobustRandom _random = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<LoadoutComponent, ComponentStartup>(OnStartup);
        }

        private void OnStartup(EntityUid uid, LoadoutComponent component, ComponentStartup args)
        {
            if (component.Prototypes == null)
                return;

            var proto = _protoMan.Index<StartingGearPrototype>(_random.Pick(component.Prototypes));
            _station.EquipStartingGear(uid, proto, null);
        }
    }
}
