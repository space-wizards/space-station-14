using System.Threading.Tasks;
using Content.Shared.Construction;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;

namespace Content.Server.Construction.Completions
{
    [DataDefinition]
    public class SetLayerVisibility : IGraphAction
    {
        [DataField("layer")] public int Layer { get; private set; } = 0;

        [DataField("value")] public bool Value { get; private set; } = true;

        public async Task PerformAction(IEntity entity, IEntity? user)
        {
            if (entity.Deleted) return;

            if (!entity.TryGetComponent(out SpriteComponent? sprite)) return;

            // That layer doesn't exist, we do nothing.
            if (sprite.LayerCount <= Layer) return;

            sprite.LayerSetVisible(Layer, Value);
        }
    }
}
