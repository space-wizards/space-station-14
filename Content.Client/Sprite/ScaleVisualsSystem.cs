using System.Numerics;
using Content.Shared.Sprite;
using Robust.Client.GameObjects;

namespace Content.Client.Sprite;

public sealed partial class ScaleVisualsSystem : SharedScaleVisualsSystem
{
    [Dependency] private SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ScaleVisualsComponent, AppearanceChangeEvent>(OnChangeData);
    }

    private void OnChangeData(Entity<ScaleVisualsComponent> ent, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null
            || !args.TryGetData<Vector2>(ScaleVisuals.Scale, out var scale))
            return;

        // save the original scale
        ent.Comp.OriginalScale ??= args.Sprite.Scale;

        _sprite.SetScale((ent.Owner, args.Sprite), scale);
    }

    // revert to the original scale
    protected override void ResetScale(Entity<ScaleVisualsComponent> ent)
    {
        base.ResetScale(ent);

        if (ent.Comp.OriginalScale != null)
            _sprite.SetScale(ent.Owner, ent.Comp.OriginalScale.Value);
    }
}
