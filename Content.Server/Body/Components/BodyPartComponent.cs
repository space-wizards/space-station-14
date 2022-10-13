using Content.Shared.Body.Components;
using Content.Shared.Random.Helpers;
using Robust.Shared.Containers;

namespace Content.Server.Body.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedBodyPartComponent))]
    public sealed class BodyPartComponent : SharedBodyPartComponent
    {
        private Container _mechanismContainer = default!;

        public override bool CanAddMechanism(OrganComponent organ)
        {
            return base.CanAddMechanism(organ) &&
                   _mechanismContainer.CanInsert(organ.Owner);
        }

        protected override void OnAddMechanism(OrganComponent organ)
        {
            base.OnAddMechanism(organ);

            _mechanismContainer.Insert(organ.Owner);
        }

        protected override void OnRemoveMechanism(OrganComponent organ)
        {
            base.OnRemoveMechanism(organ);

            _mechanismContainer.Remove(organ.Owner);
            organ.Owner.RandomOffset(0.25f);
        }

        public void MapInitialize()
        {
            base.Initialize();

            _mechanismContainer = Owner.EnsureContainer<Container>(ContainerId);

            // TODO BODY SYSTEM BEFORE MERGE
            // // This is ran in Startup as entities spawned in Initialize
            // // are not synced to the client since they are assumed to be
            // // identical on it
            // foreach (var mechanismId in MechanismIds)
            // {
            //     var entity = _entMan.SpawnEntity(mechanismId, _entMan.GetComponent<TransformComponent>(Owner).MapPosition);
            //
            //     if (!_entMan.TryGetComponent(entity, out OrganComponent? mechanism))
            //     {
            //         Logger.Error($"Entity {mechanismId} does not have a {nameof(OrganComponent)} component.");
            //         continue;
            //     }
            //
            //     TryAddMechanism(mechanism, true);
            // }
        }
    }
}
