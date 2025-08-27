using System.Numerics;
using Content.Shared.Medical.Cryogenics;
using Robust.Client.GameObjects;

namespace Content.Client.Medical.Cryogenics;

public sealed class CryoPodSystem : SharedCryoPodSystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CryoPodComponent, AppearanceChangeEvent>(OnAppearanceChange);
        SubscribeLocalEvent<InsideCryoPodComponent, ComponentStartup>(OnCryoPodInsertion);
        SubscribeLocalEvent<InsideCryoPodComponent, ComponentRemove>(OnCryoPodRemoval);
    }

    private void OnCryoPodInsertion(EntityUid uid, InsideCryoPodComponent component, ComponentStartup args)
    {
        if (!TryComp<SpriteComponent>(uid, out var spriteComponent))
        {
            return;
        }

        component.PreviousOffset = spriteComponent.Offset;
        _sprite.SetOffset((uid, spriteComponent), new Vector2(0, 1));
    }

    private void OnCryoPodRemoval(EntityUid uid, InsideCryoPodComponent component, ComponentRemove args)
    {
        if (!TryComp<SpriteComponent>(uid, out var spriteComponent))
        {
            return;
        }

        _sprite.SetOffset((uid, spriteComponent), component.PreviousOffset);
    }

    private void OnAppearanceChange(EntityUid uid, CryoPodComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
        {
            return;
        }

        if (!_appearance.TryGetData<bool>(uid, CryoPodVisuals.ContainsEntity, out var isOpen, args.Component)
            || !_appearance.TryGetData<bool>(uid, CryoPodVisuals.IsOn, out var isOn, args.Component))
        {
            return;
        }

        if (isOpen)
        {
            _sprite.LayerSetRsiState((uid, args.Sprite), CryoPodVisualLayers.Base, "pod-open");
            _sprite.LayerSetVisible((uid, args.Sprite), CryoPodVisualLayers.Cover, false);
        }
        else
        {
            _sprite.LayerSetRsiState((uid, args.Sprite), CryoPodVisualLayers.Base, isOn ? "pod-on" : "pod-off");
            _sprite.LayerSetRsiState((uid, args.Sprite), CryoPodVisualLayers.Cover, isOn ? "cover-on" : "cover-off");
            _sprite.LayerSetVisible((uid, args.Sprite), CryoPodVisualLayers.Cover, true);
        }
    }
}

public enum CryoPodVisualLayers : byte
{
    Base,
    Cover,
}
