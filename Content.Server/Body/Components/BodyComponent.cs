using Content.Server.Ghost;
using Content.Shared.Audio;
using Content.Shared.Body;
using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.Random.Helpers;
using Content.Shared.Sound;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;
using Robust.Shared.Player;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Body.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedBodyComponent))]
    [ComponentReference(typeof(IGhostOnMove))]
    public class BodyComponent : SharedBodyComponent, IGhostOnMove
    {
        private Container _partContainer = default!;

        [DataField("gibSound")] private SoundSpecifier _gibSound = new SoundCollectionSpecifier("gib");

        protected override bool CanAddPart(string slotId, SharedBodyPartComponent part)
        {
            return base.CanAddPart(slotId, part) &&
                   _partContainer.CanInsert(part.Owner);
        }

        protected override void OnAddPart(BodyPartSlot slot, SharedBodyPartComponent part)
        {
            base.OnAddPart(slot, part);

            _partContainer.Insert(part.Owner);
        }

        protected override void OnRemovePart(BodyPartSlot slot, SharedBodyPartComponent part)
        {
            base.OnRemovePart(slot, part);

            _partContainer.ForceRemove(part.Owner);
            part.Owner.RandomOffset(0.25f);
        }

        protected override void Initialize()
        {
            base.Initialize();

            _partContainer = Owner.EnsureContainer<Container>($"{Name}-{nameof(BodyComponent)}");
            var preset = Preset;

            if (preset != null)
            {
                foreach (var slot in Slots)
                {
                    // Using MapPosition instead of Coordinates here prevents
                    // a crash within the character preview menu in the lobby
                    var entity = Owner.EntityManager.SpawnEntity(preset.PartIDs[slot.Id], Owner.Transform.MapPosition);

                    if (!entity.TryGetComponent(out SharedBodyPartComponent? part))
                    {
                        Logger.Error($"Entity {slot.Id} does not have a {nameof(SharedBodyPartComponent)} component.");
                        continue;
                    }

                    SetPart(slot.Id, part);
                }
            }
        }

        protected override void Startup()
        {
            base.Startup();

            // This is ran in Startup as entities spawned in Initialize
            // are not synced to the client since they are assumed to be
            // identical on it
            foreach (var (part, _) in Parts)
            {
                part.Dirty();
            }
        }

        public override void Gib(bool gibParts = false)
        {
            base.Gib(gibParts);

            SoundSystem.Play(Filter.Pvs(Owner), _gibSound.GetSound(), Owner.Transform.Coordinates, AudioHelpers.WithVariation(0.025f));

            if (Owner.TryGetComponent(out ContainerManagerComponent? container))
            {
                foreach (var cont in container.GetAllContainers())
                {
                    foreach (var ent in cont.ContainedEntities)
                    {
                        cont.ForceRemove(ent);
                        ent.Transform.Coordinates = Owner.Transform.Coordinates;
                        ent.RandomOffset(0.25f);
                    }
                }
            }

            Owner.QueueDelete();
        }
    }
}
