using Content.Shared.Body.Components;
using Content.Shared.Random.Helpers;
using Robust.Shared.Containers;

namespace Content.Server.Body.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedBodyPartComponent))]
    public sealed class BodyPartComponent : SharedBodyPartComponent
    {
        [Dependency] private readonly IEntityManager _entMan = default!;

        private Container _mechanismContainer = default!;

        public override bool CanAddMechanism(MechanismComponent mechanism)
        {
            return base.CanAddMechanism(mechanism) &&
                   _mechanismContainer.CanInsert(mechanism.Owner);
        }

        protected override void OnAddMechanism(MechanismComponent mechanism)
        {
            base.OnAddMechanism(mechanism);

            _mechanismContainer.Insert(mechanism.Owner);
        }

        protected override void OnRemoveMechanism(MechanismComponent mechanism)
        {
            base.OnRemoveMechanism(mechanism);

            _mechanismContainer.Remove(mechanism.Owner);
            mechanism.Owner.RandomOffset(0.25f);
        }

        public void MapInitialize()
        {
            base.Initialize();

            _mechanismContainer = Owner.EnsureContainer<Container>(ContainerId);

            // This is ran in Startup as entities spawned in Initialize
            // are not synced to the client since they are assumed to be
            // identical on it
            foreach (var mechanismId in MechanismIds)
            {
                var entity = _entMan.SpawnEntity(mechanismId, _entMan.GetComponent<TransformComponent>(Owner).MapPosition);

                if (!_entMan.TryGetComponent(entity, out MechanismComponent? mechanism))
                {
                    Logger.Error($"Entity {mechanismId} does not have a {nameof(MechanismComponent)} component.");
                    continue;
                }

                TryAddMechanism(mechanism, true);
            }
        }
    }
}
