#nullable enable
using System.Threading.Tasks;
using Content.Shared.Construction;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;

namespace Content.Server.Construction.Completions
{
    [UsedImplicitly]
    [DataDefinition]
    public class SpriteChange : IGraphAction
    {
        [DataField("layer")] public int Layer { get; private set; } = 0;
        [DataField("specifier")]  public SpriteSpecifier? SpriteSpecifier { get; private set; } = SpriteSpecifier.Invalid;

        public async Task PerformAction(IEntity entity, IEntity? user)
        {
            if (entity.Deleted || SpriteSpecifier == null || SpriteSpecifier == SpriteSpecifier.Invalid) return;

            if (!entity.TryGetComponent(out SpriteComponent? sprite)) return;

            // That layer doesn't exist, we do nothing.
            if (sprite.LayerCount <= Layer) return;

            sprite.LayerSetSprite(Layer, SpriteSpecifier);
        }
    }
}
