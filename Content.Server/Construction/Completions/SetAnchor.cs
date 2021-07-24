using System.Threading.Tasks;
using Content.Shared.Construction;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Physics;
using Robust.Shared.Serialization;

namespace Content.Server.Construction.Completions
{
    [UsedImplicitly]
    [DataDefinition]
    public class SetAnchor : IGraphAction
    {
        [DataField("value")] public bool Value { get; private set; } = true;

        public async Task PerformAction(IEntity entity, IEntity? user)
        {
            if (!entity.TryGetComponent(out IPhysBody? physics)) return;

            physics.BodyType = Value ? BodyType.Static : BodyType.Dynamic;
        }
    }
}
