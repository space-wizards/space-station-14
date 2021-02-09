#nullable enable
using System.Threading.Tasks;
using Content.Shared.Construction;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Server.Construction.Completions
{
    [UsedImplicitly]
    public class SpriteStateChange : IGraphAction
    {
        void IExposeData.ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(this, x => x.State, "state", string.Empty);
            serializer.DataField(this, x => x.Layer, "layer", 0);
        }

        public int Layer { get; private set; } = 0;
        public string? State { get; private set; } = string.Empty;

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
