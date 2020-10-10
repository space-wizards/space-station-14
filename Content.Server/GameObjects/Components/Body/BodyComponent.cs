#nullable enable
using Content.Server.Observer;
using Content.Shared.GameObjects.Components.Body;
using Content.Shared.GameObjects.Components.Body.Part;
using Content.Shared.GameObjects.Components.Damage;
using Content.Shared.GameObjects.Components.Movement;
using Robust.Server.GameObjects.Components.Container;
using Robust.Server.Interfaces.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;
using Robust.Shared.Players;

namespace Content.Server.GameObjects.Components.Body
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedBodyComponent))]
    [ComponentReference(typeof(IBody))]
    public class BodyComponent : SharedBodyComponent, IRelayMoveInput
    {
        private Container _container = default!;

        protected override bool CanAddPart(string slot, IBodyPart part)
        {
            return base.CanAddPart(slot, part) && _container.CanInsert(part.Owner);
        }

        protected override void OnAddPart(string slot, IBodyPart part)
        {
            base.OnAddPart(slot, part);

            _container.Insert(part.Owner);
        }

        protected override void OnRemovePart(string slot, IBodyPart part)
        {
            base.OnRemovePart(slot, part);

            _container.ForceRemove(part.Owner);
        }

        public override void Initialize()
        {
            base.Initialize();

            _container = ContainerManagerComponent.Ensure<Container>($"{Name}-{nameof(BodyComponent)}", Owner);

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
            if (Owner.TryGetComponent(out IDamageableComponent? damageable) &&
                damageable.CurrentState == DamageState.Dead)
            {
                new Ghost().Execute(null, (IPlayerSession) session, null);
            }
        }
    }
}
