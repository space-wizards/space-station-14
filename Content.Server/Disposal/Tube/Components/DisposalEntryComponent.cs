using System.Linq;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Disposal.Unit.Components;
using Content.Server.Disposal.Unit.EntitySystems;

namespace Content.Server.Disposal.Tube.Components
{
    [RegisterComponent]
    [Access(typeof(DisposalTubeSystem), typeof(DisposalUnitSystem))]
    public sealed class DisposalEntryComponent : Component
    {
        [Dependency] private readonly IEntityManager _entMan = default!;

        private const string HolderPrototypeId = "DisposalHolder";

        public bool TryInsert(DisposalUnitComponent from, IEnumerable<string>? tags = default)
        {
            var holder = _entMan.SpawnEntity(HolderPrototypeId, _entMan.GetComponent<TransformComponent>(Owner).MapPosition);
            var holderComponent = _entMan.GetComponent<DisposalHolderComponent>(holder);

            foreach (var entity in from.Container.ContainedEntities.ToArray())
            {
                holderComponent.TryInsert(entity);
            }

            EntitySystem.Get<AtmosphereSystem>().Merge(holderComponent.Air, from.Air);
            from.Air.Clear();

            if (tags != default)
                holderComponent.Tags.UnionWith(tags);

            return EntitySystem.Get<DisposableSystem>().EnterTube((holderComponent).Owner, Owner, holderComponent);
        }
    }
}
