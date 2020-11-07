using System.Linq;
using Content.Shared.Audio;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using Robust.Shared.Random;

namespace Content.Server.GameObjects.Components.Items.Storage
{
    [ComponentReference(typeof(EntityStorageComponent))]
    [ComponentReference(typeof(IActivate))]
    [ComponentReference(typeof(IStorageComponent))]
    [RegisterComponent]
    public class CursedEntityStorageComponent : EntityStorageComponent
    {
        [Dependency] private IEntityManager _entityManager = default!;
        [Dependency] private IRobustRandom _robustRandom = default!;

        public override string Name => "CursedEntityStorage";

        protected override void CloseStorage()
        {
            base.CloseStorage();

            // No contents, we do nothing
            if (Contents.ContainedEntities.Count == 0) return;

            var lockers = _entityManager.GetEntities(new TypeEntityQuery(typeof(EntityStorageComponent))).ToList();

            if (lockers.Contains(Owner))
                lockers.Remove(Owner);

            var lockerEnt = _robustRandom.Pick(lockers);

            if (lockerEnt == null) return; // No valid lockers anywhere.

            var locker = lockerEnt.GetComponent<EntityStorageComponent>();

            if(locker.Open)
                locker.TryCloseStorage(Owner);

            foreach (var entity in Contents.ContainedEntities.ToArray())
            {
                Contents.ForceRemove(entity);
                locker.Insert(entity);
            }

            EntitySystem.Get<AudioSystem>().PlayFromEntity("/Audio/Effects/teleport_departure.ogg", Owner, AudioHelpers.WithVariation(0.125f));
            EntitySystem.Get<AudioSystem>().PlayFromEntity("/Audio/Effects/teleport_arrival.ogg", lockerEnt, AudioHelpers.WithVariation(0.125f));
        }
    }
}
