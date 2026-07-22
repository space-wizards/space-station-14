// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.DeadSpace.Sticky;
using Content.Shared.Sticky.Components;
using Robust.Client.GameObjects;

namespace Content.Client.DeadSpace.Sticky;

public sealed class ScaleWhenStuckSystem : EntitySystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ScaleWhenStuckComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<ScaleWhenStuckComponent, AppearanceChangeEvent>(OnAppearanceChanged);
    }

    private void OnInit(Entity<ScaleWhenStuckComponent> ent, ref ComponentInit args)
    {
        if (TryComp<SpriteComponent>(ent, out var sprite))
            ent.Comp.OriginalScale = sprite.Scale;
    }

    private void OnAppearanceChanged(Entity<ScaleWhenStuckComponent> ent, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null || ent.Comp.OriginalScale is not { } originalScale)
            return;

        var scale = originalScale;
        if (TryComp<StickyComponent>(ent, out var sticky) &&
            sticky.StuckTo is { } target &&
            HasComp<StickySurfaceOverrideComponent>(target))
        {
            scale *= ent.Comp.Scale;
        }

        _sprite.SetScale((ent.Owner, args.Sprite), scale);
    }
}
