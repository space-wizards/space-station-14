#nullable enable
using System;
using Content.Server.Commands.Observer;
using Content.Server.GameObjects.Components.Observer;
using Content.Shared.Audio;
using Content.Shared.GameObjects.Components.Body;
using Content.Shared.GameObjects.Components.Body.Part;
using Content.Shared.GameObjects.Components.Body.Slot;
using Content.Shared.GameObjects.Components.Mobs.State;
using Content.Shared.GameObjects.Components.Movement;
using Content.Shared.Utility;
using Robust.Server.Console;
using Robust.Shared.Audio;
using Robust.Shared.Console;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Player;
using Robust.Shared.Players;

namespace Content.Server.GameObjects.Components.Body
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedBodyComponent))]
    [ComponentReference(typeof(IBody))]
    [ComponentReference(typeof(IGhostOnMove))]
    public class BodyComponent : SharedBodyComponent, IRelayMoveInput, IGhostOnMove
    {
        private Container _partContainer = default!;

        protected override bool CanAddPart(string slotId, IBodyPart part)
        {
            return base.CanAddPart(slotId, part) &&
                   _partContainer.CanInsert(part.Owner);
        }

        protected override void OnAddPart(BodyPartSlot slot, IBodyPart part)
        {
            base.OnAddPart(slot, part);

            _partContainer.Insert(part.Owner);
        }

        protected override void OnRemovePart(BodyPartSlot slot, IBodyPart part)
        {
            base.OnRemovePart(slot, part);

            _partContainer.ForceRemove(part.Owner);
            part.Owner.RandomOffset(0.25f);
        }

        public override void Initialize()
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

                    if (!entity.TryGetComponent(out IBodyPart? part))
                    {
                        Logger.Error($"Entity {slot.Id} does not have a {nameof(IBodyPart)} component.");
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

        void IRelayMoveInput.MoveInputPressed(ICommonSession session)
        {
            if (Owner.TryGetComponent(out IMobStateComponent? mobState) &&
                mobState.IsDead())
            {
                var host = IoCManager.Resolve<IServerConsoleHost>();

                new Ghost().Execute(new ConsoleShell(host, session), string.Empty, Array.Empty<string>());
            }
        }

        public override void Gib(bool gibParts = false)
        {
            base.Gib(gibParts);

            SoundSystem.Play(Filter.Pvs(Owner), AudioHelpers.GetRandomFileFromSoundCollection("gib"), Owner.Transform.Coordinates,
                    AudioHelpers.WithVariation(0.025f));

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

            Owner.Delete();
        }
    }
}
