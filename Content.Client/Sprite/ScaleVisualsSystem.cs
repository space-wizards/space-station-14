using System.Numerics;
using Content.Shared.Sprite;
using Robust.Client.GameObjects;

namespace Content.Client.Sprite;

public sealed class ScaleVisualsSystem : SharedScaleVisualsSystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ScaleVisualsComponent, AppearanceChangeEvent>(OnChangeData);
    }

    private void OnChangeData(Entity<ScaleVisualsComponent> ent, ref AppearanceChangeEvent args)
    {
        if (!args.AppearanceData.TryGetValue(ScaleVisuals.Scale, out var scale) ||
            args.Sprite == null) return;

        // save the original scale
        ent.Comp.OriginalScale ??= args.Sprite.Scale;

        var vecScale = (Vector2)scale;
        _sprite.SetScale((ent.Owner, args.Sprite), vecScale);
    }

    // revert to the original scale
    protected override void ResetScale(Entity<ScaleVisualsComponent> ent)
    {
        base.ResetScale(ent);

        if (ent.Comp.OriginalScale != null)
            _sprite.SetScale(ent.Owner, ent.Comp.OriginalScale.Value);
    }
}
