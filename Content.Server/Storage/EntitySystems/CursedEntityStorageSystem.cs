using Content.Server.Storage.Components;
using Content.Shared.Audio;
using Content.Shared.Interaction;
using JetBrains.Annotations;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Player;
using Robust.Shared.Random;
using System.Collections.Generic;
using System.Linq;

namespace Content.Server.Storage.EntitySystems
{
    [UsedImplicitly]
    public class CursedEntityStorageSystem : EntitySystem
    {
        [Dependency] private readonly IRobustRandom _robustRandom = default!;

        /// <inheritdoc />
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<CursedEntityStorageECSComponent, EntityStorageClosedEvent>(OnCloseStorage);
        }

        protected void OnCloseStorage(EntityUid eUI, CursedEntityStorageECSComponent comp, EntityStorageClosedEvent args)
        {
            if (!comp.Owner.TryGetComponent<EntityStorageECSComponent>(out var entityStorageComp) ||
               entityStorageComp.Contents.ContainedEntities.Count == 0) // No contents, we do nothing
            {
                return;
            }

            var lockers = comp.Owner.EntityManager.EntityQuery<EntityStorageComponent>().Select(c => c.Owner).ToList();

            if (lockers.Contains(comp.Owner))
                lockers.Remove(comp.Owner);

            var lockerEnt = _robustRandom.Pick(lockers);

            if (lockerEnt == null) return; // No valid lockers anywhere.

            var locker = lockerEnt.GetComponent<EntityStorageComponent>();

            if (locker.Open)
                locker.TryCloseStorage(comp.Owner);

            foreach (var entity in entityStorageComp.Contents.ContainedEntities.ToArray())
            {
                entityStorageComp.Contents.ForceRemove(entity);
                locker.Insert(entity);
            }

            SoundSystem.Play(Filter.Pvs(comp.Owner), comp.CursedSound.GetSound(), comp.Owner, AudioHelpers.WithVariation(0.125f));
            SoundSystem.Play(Filter.Pvs(lockerEnt), comp.CursedLockerSound.GetSound(), lockerEnt, AudioHelpers.WithVariation(0.125f));
        }
    }
}
