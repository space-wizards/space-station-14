#nullable enable
using System.Threading.Tasks;
using Content.Shared.Construction;
using Robust.Server.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Server.Construction.Completions
{
    public class SpriteChange : IEdgeCompleted, IStepCompleted
    {
        public void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(this, x => x.SpriteSpecifier, "specifier", null);
            serializer.DataField(this, x => x.Layer, "layer", 0);
        }

        public int Layer { get; private set; } = 0;
        public SpriteSpecifier SpriteSpecifier { get; private set; } = SpriteSpecifier.Invalid;

        public async Task StepCompleted(IEntity entity, IEntity user)
        {
            await Completed(entity, user);
        }

        public async Task Completed(IEntity entity, IEntity user)
        {
            if (entity.Deleted) return;

            if (!entity.TryGetComponent(out SpriteComponent? sprite)) return;

            sprite.LayerSetSprite(Layer, SpriteSpecifier);
        }
    }
}
