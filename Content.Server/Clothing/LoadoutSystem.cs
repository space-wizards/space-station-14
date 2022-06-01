using Content.Server.Clothing.Components;
using Content.Server.Station.Systems;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Server.Clothing
{
    /// <summary>
    /// Assigns a loadout to an entity based on the startingGear prototype
    /// </summary>
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
            if (component.Prototype == string.Empty)
                return;

            var proto = _protoMan.Index<StartingGearPrototype>(component.Prototype);
            _station.EquipStartingGear(uid, proto, null);
        }
    }
}
