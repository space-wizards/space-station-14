#nullable enable
using System.Threading.Tasks;
using Content.Shared.Construction;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Construction.Completions
{
    [UsedImplicitly]
    [DataDefinition]
    public class SetAnchor : IGraphAction
    {
        [DataField("value")] public bool Value { get; private set; } = true;

        public async Task PerformAction(IEntity entity, IEntity? user)
        {
            if (!entity.TryGetComponent(out IPhysicsComponent? physics)) return;

            physics.Anchored = Value;
        }
    }
}
