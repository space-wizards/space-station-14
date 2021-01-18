#nullable enable
using System;
using Content.Server.Commands.Observer;
using Content.Shared.Audio;
using Content.Shared.GameObjects.Components.Body;
using Content.Shared.GameObjects.Components.Body.Part;
using Content.Shared.GameObjects.Components.Damage;
using Content.Shared.GameObjects.Components.Mobs.State;
using Content.Shared.GameObjects.Components.Movement;
using Content.Shared.Utility;
using Robust.Server.GameObjects.Components.Container;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Server.Interfaces.Console;
using Robust.Server.Interfaces.Player;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Players;

namespace Content.Server.GameObjects.Components.Body
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedBodyComponent))]
    [ComponentReference(typeof(IBody))]
    public class BodyComponent : SharedBodyComponent, IRelayMoveInput
    {
        private Container _partContainer = default!;

        protected override bool CanAddPart(string slot, IBodyPart part)
        {
            return base.CanAddPart(slot, part) &&
                   _partContainer.CanInsert(part.Owner);
        }

        protected override void OnAddPart(string slot, IBodyPart part)
        {
            base.OnAddPart(slot, part);

            _partContainer.Insert(part.Owner);
        }

        protected override void OnRemovePart(string slot, IBodyPart part)
        {
            base.OnRemovePart(slot, part);

            _partContainer.ForceRemove(part.Owner);
            part.Owner.RandomOffset(0.25f);
        }

        public override void Initialize()
        {
            base.Initialize();

            _partContainer = ContainerManagerComponent.Ensure<Container>($"{Name}-{nameof(BodyComponent)}", Owner);

            foreach (var (slot, partId) in PartIds)
            {
                // Using MapPosition instead of Coordinates here prevents
                // a crash within the character preview menu in the lobby
                var entity = Owner.EntityManager.SpawnEntity(partId, Owner.Transform.MapPosition);

                if (!entity.TryGetComponent(out IBodyPart? part))
                {
                    Logger.Error($"Entity {partId} does not have a {nameof(IBodyPart)} component.");
                    continue;
                }

                TryAddPart(slot, part, true);
            }
        }

        protected override void Startup()
        {
            base.Startup();

            // This is ran in Startup as entities spawned in Initialize
            // are not synced to the client since they are assumed to be
            // identical on it
            foreach (var part in Parts.Values)
            {
                part.Dirty();
            }
        }

        void IRelayMoveInput.MoveInputPressed(ICommonSession session)
        {
            if (Owner.TryGetComponent(out IMobStateComponent? mobState) &&
                mobState.IsDead())
            {
                var shell = IoCManager.Resolve<IConsoleShell>();

                new Ghost().Execute(shell, (IPlayerSession) session, Array.Empty<string>());
            }
        }

        public override void Gib(bool gibParts = false)
        {
            base.Gib(gibParts);

            EntitySystem.Get<AudioSystem>()
                .PlayAtCoords(AudioHelpers.GetRandomFileFromSoundCollection("gib"), Owner.Transform.Coordinates,
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
