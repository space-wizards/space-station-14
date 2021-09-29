using Content.Shared.Audio;
using Content.Shared.Interaction;
using Content.Shared.Sound;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Serialization.Manager.Attributes;
using System.Linq;

namespace Content.Server.Storage.Components
{
    [ComponentReference(typeof(EntityStorageComponent))]
    [ComponentReference(typeof(IActivate))]
    [ComponentReference(typeof(IStorageComponent))]
    [RegisterComponent]
    public class CursedEntityStorageComponent : EntityStorageComponent
    {
        [Dependency] private readonly IRobustRandom _robustRandom = default!;

        public override string Name => "CursedEntityStorage";

        [DataField("cursedSound")] private SoundSpecifier _cursedSound = new SoundPathSpecifier("/Audio/Effects/teleport_departure.ogg");
        [DataField("cursedLockerSound")] private SoundSpecifier _cursedLockerSound = new SoundPathSpecifier("/Audio/Effects/teleport_arrival.ogg");

        protected override void CloseStorage()
        {
            base.CloseStorage();

            // No contents, we do nothing
            if (Contents.ContainedEntities.Count == 0) return;

            var lockers = Owner.EntityManager.EntityQuery<EntityStorageComponent>().Select(c => c.Owner).ToList();

            if (lockers.Contains(Owner))
                lockers.Remove(Owner);

            var lockerEnt = _robustRandom.Pick(lockers);

            if (lockerEnt == null) return; // No valid lockers anywhere.

            var locker = lockerEnt.GetComponent<EntityStorageComponent>();

            if (locker.Open)
                locker.TryCloseStorage(Owner);

            foreach (var entity in Contents.ContainedEntities.ToArray())
            {
                Contents.ForceRemove(entity);
                locker.Insert(entity);
            }

            SoundSystem.Play(Filter.Pvs(Owner), _cursedSound.GetSound(), Owner, AudioHelpers.WithVariation(0.125f));
            SoundSystem.Play(Filter.Pvs(lockerEnt), _cursedLockerSound.GetSound(), lockerEnt, AudioHelpers.WithVariation(0.125f));
        }
    }
}
