using System.Linq;
using Content.Shared.Audio;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server.GameObjects.Components.Items.Storage
{
    [ComponentReference(typeof(EntityStorageComponent))]
    [ComponentReference(typeof(IActivate))]
    [ComponentReference(typeof(IStorageComponent))]
    [RegisterComponent]
    public class CursedEntityStorageComponent : EntityStorageComponent
    {
         [Dependency] private readonly IRobustRandom _robustRandom = default!;

        public override string Name => "CursedEntityStorage";

        protected override void CloseStorage()
        {
            base.CloseStorage();

            // No contents, we do nothing
            if (Contents.ContainedEntities.Count == 0) return;

            var lockers = Owner.EntityManager.GetEntities(new TypeEntityQuery(typeof(EntityStorageComponent))).ToList();

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

            SoundSystem.Play(Filter.Pvs(Owner), "/Audio/Effects/teleport_departure.ogg", Owner, AudioHelpers.WithVariation(0.125f));
            SoundSystem.Play(Filter.Pvs(lockerEnt), "/Audio/Effects/teleport_arrival.ogg", lockerEnt, AudioHelpers.WithVariation(0.125f));
        }
    }
}
