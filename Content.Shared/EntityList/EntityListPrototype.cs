using System.Collections.Immutable;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityList
{
    [Prototype]
    public sealed partial class EntityListPrototype : IPrototype
    {
        [ViewVariables]
        [IdDataField]
        public string ID { get; private set; } = default!;

        [DataField]
        public ImmutableList<EntProtoId> Entities { get; private set; } = ImmutableList<EntProtoId>.Empty;

        public IEnumerable<EntityPrototype> GetEntities(IPrototypeManager? prototypeManager = null)
        {
            prototypeManager ??= IoCManager.Resolve<IPrototypeManager>();

            foreach (var entityId in Entities)
            {
                yield return prototypeManager.Index<EntityPrototype>(entityId);
            }
        }
    }
}
