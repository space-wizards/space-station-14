using System.Linq;
using Content.Server.Interfaces.GameObjects.Components.Interaction;
using Content.Shared.Audio;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.Interfaces.GameObjects.Components;
using Microsoft.EntityFrameworkCore.Internal;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using Robust.Shared.Log;
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

        public override void CloseStorage()
        {
            base.CloseStorage();

            var playSound = false;

            var lockers = _entityManager.GetEntities(new TypeEntityQuery(typeof(EntityStorageComponent))).ToList();

            if (lockers.Contains(Owner))
                lockers.Remove(Owner);

            var lockerEnt = _robustRandom.Pick(lockers);

            if (lockerEnt == null) return; // No valid lockers anywhere.

            var locker = lockerEnt.GetComponent<EntityStorageComponent>();

            if(locker.Open)
                locker.CloseStorage();

            foreach (var entity in Contents.ContainedEntities.ToArray())
            {
                playSound = true;
                Contents.ForceRemove(entity);
                entity.Transform.AttachToGridOrMap();
                locker.Insert(entity);
            }

            if (!playSound) return;
            EntitySystem.Get<AudioSystem>().PlayFromEntity("/Audio/Effects/teleport_departure.ogg", Owner, AudioHelpers.WithVariation(0.125f));
            EntitySystem.Get<AudioSystem>().PlayFromEntity("/Audio/Effects/teleport_arrival.ogg", lockerEnt, AudioHelpers.WithVariation(0.125f));
        }
    }
}
