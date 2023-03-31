using Content.Shared.Construction;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Utility;

namespace Content.Server.Construction.Completions
{
    [UsedImplicitly]
    [DataDefinition]
    public sealed class SpriteChange : IGraphAction
    {
        [DataField("layer")] public int Layer { get; private set; } = 0;
        [DataField("specifier")] public SpriteSpecifier? SpriteSpecifier { get; private set; } = SpriteSpecifier.Invalid;

        public void PerformAction(EntityUid uid, EntityUid? userUid, IEntityManager entityManager)
        {
            if (SpriteSpecifier == null || SpriteSpecifier == SpriteSpecifier.Invalid)
                return;

            if (!entityManager.TryGetComponent(uid, out SpriteComponent? sprite))
                return;

            // That layer doesn't exist, we do nothing.
            if (sprite.LayerCount <= Layer)
                return;

            sprite.LayerSetSprite(Layer, SpriteSpecifier);
        }
    }
}
