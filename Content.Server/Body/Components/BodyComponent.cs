using Content.Shared.Audio;
using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.Random.Helpers;
using Content.Shared.Sound;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Player;

namespace Content.Server.Body.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedBodyComponent))]
    public sealed class BodyComponent : SharedBodyComponent
    {
        [Dependency] private readonly IEntityManager _entMan = default!;

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
                    var entity = _entMan.SpawnEntity(preset.PartIDs[slot.Id], _entMan.GetComponent<TransformComponent>(Owner).MapPosition);

                    if (!_entMan.TryGetComponent(entity, out SharedBodyPartComponent? part))
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

        public override HashSet<EntityUid> Gib(bool gibParts = false)
        {
            var gibs = base.Gib(gibParts);

            var xform = _entMan.GetComponent<TransformComponent>(Owner);
            var coordinates = xform.Coordinates;

            // These have already been forcefully removed from containers so run it here.
            foreach (var part in gibs)
            {
                _entMan.EventBus.RaiseLocalEvent(part, new PartGibbedEvent(Owner, gibs), true);
            }

            SoundSystem.Play(_gibSound.GetSound(), Filter.Pvs(Owner, entityManager: _entMan), coordinates, AudioHelpers.WithVariation(0.025f));

            if (_entMan.TryGetComponent(Owner, out ContainerManagerComponent? container))
            {
                foreach (var cont in container.GetAllContainers())
                {
                    foreach (var ent in cont.ContainedEntities)
                    {
                        cont.ForceRemove(ent);
                        _entMan.GetComponent<TransformComponent>(ent).Coordinates = coordinates;
                        ent.RandomOffset(0.25f);
                    }
                }
            }

            _entMan.EventBus.RaiseLocalEvent(Owner, new BeingGibbedEvent(gibs), false);
            _entMan.QueueDeleteEntity(Owner);

            return gibs;
        }
    }

    public sealed class BeingGibbedEvent : EntityEventArgs
    {
        public readonly HashSet<EntityUid> GibbedParts;

        public BeingGibbedEvent(HashSet<EntityUid> gibbedParts)
        {
            GibbedParts = gibbedParts;
        }
    }

    /// <summary>
    /// An event raised on all the parts of an entity when it's gibbed
    /// </summary>
    public sealed class PartGibbedEvent : EntityEventArgs
    {
        public EntityUid EntityToGib;
        public readonly HashSet<EntityUid> GibbedParts;

        public PartGibbedEvent(EntityUid entityToGib, HashSet<EntityUid> gibbedParts)
        {
            EntityToGib = entityToGib;
            GibbedParts = gibbedParts;
        }
    }
}
