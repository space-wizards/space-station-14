// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.DeadSpace.Sandevistan;
using Robust.Client.GameObjects;
using Robust.Shared.Utility;

namespace Content.Client.DeadSpace.Sandevistan;

public sealed class SandevistanBodyVisualSystem : EntitySystem
{
    private static readonly ResPath SpritePath =
        new("/Textures/_DeadSpace/Mobs/Effects/sandevistan.rsi");

    private const string SpriteState = "body";
    private const string OuterClothingSlot = "outerClothing";

    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SandevistanBodyVisualComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<SandevistanBodyVisualComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnStartup(Entity<SandevistanBodyVisualComponent> ent, ref ComponentStartup args)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite) ||
            _sprite.LayerMapTryGet((ent.Owner, sprite), SandevistanBodyVisualLayers.Body, out _, false) ||
            !_sprite.LayerMapTryGet((ent.Owner, sprite), OuterClothingSlot, out var outerClothingLayer, false))
        {
            return;
        }

        var layer = _sprite.AddLayer(
            (ent.Owner, sprite),
            new SpriteSpecifier.Rsi(SpritePath, SpriteState),
            outerClothingLayer);

        _sprite.LayerMapSet((ent.Owner, sprite), SandevistanBodyVisualLayers.Body, layer);
        sprite.LayerSetShader(layer, "unshaded");
    }

    private void OnShutdown(Entity<SandevistanBodyVisualComponent> ent, ref ComponentShutdown args)
    {
        if (TryComp<SpriteComponent>(ent, out var sprite) &&
            _sprite.LayerMapTryGet((ent.Owner, sprite), SandevistanBodyVisualLayers.Body, out _, false))
        {
            _sprite.RemoveLayer((ent.Owner, sprite), SandevistanBodyVisualLayers.Body);
        }
    }

    private enum SandevistanBodyVisualLayers : byte
    {
        Body,
    }
}
