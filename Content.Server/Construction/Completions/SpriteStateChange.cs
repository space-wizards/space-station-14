using System.Threading.Tasks;
using Content.Shared.Construction;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Construction.Completions
{
    [UsedImplicitly]
    [DataDefinition]
    public class SpriteStateChange : IGraphAction
    {
        [DataField("layer")] public int Layer { get; private set; } = 0;
        [DataField("state")] public string? State { get; private set; } = string.Empty;

        public async Task PerformAction(IEntity entity, IEntity? user)
        {
            if (entity.Deleted || string.IsNullOrEmpty(State)) return;

            if (!entity.TryGetComponent(out SpriteComponent? sprite)) return;

            // That layer doesn't exist, we do nothing.
            if (sprite.LayerCount <= Layer) return;

            sprite.LayerSetState(Layer, State);
        }
    }
}
