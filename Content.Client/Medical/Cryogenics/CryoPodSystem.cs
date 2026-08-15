using System.Numerics;
using Content.Shared.Medical.Cryogenics;
using Robust.Client.GameObjects;

namespace Content.Client.Medical.Cryogenics;

public sealed partial class CryoPodSystem : SharedCryoPodSystem
{
    [Dependency] private SpriteSystem _sprite = default!;

    [SubscribeLocalEvent]
    private void OnCryoPodInsertion(EntityUid uid, InsideCryoPodComponent component, ComponentStartup args)
    {
        if (!TryComp<SpriteComponent>(uid, out var spriteComponent))
        {
            return;
        }

        component.PreviousOffset = spriteComponent.Offset;
        _sprite.SetOffset((uid, spriteComponent), new Vector2(0, 1));
    }

    [SubscribeLocalEvent]
    private void OnCryoPodRemoval(EntityUid uid, InsideCryoPodComponent component, ComponentRemove args)
    {
        if (!TryComp<SpriteComponent>(uid, out var spriteComponent))
        {
            return;
        }

        _sprite.SetOffset((uid, spriteComponent), component.PreviousOffset);
    }

    [SubscribeLocalEvent]
    private void OnAppearanceChange(EntityUid uid, CryoPodComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!args.TryGetData<bool>(CryoPodVisuals.ContainsEntity, out var isOpen)
            || !args.TryGetData<bool>(CryoPodVisuals.IsOn, out var isOn))
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

    protected override void UpdateUi(Entity<CryoPodComponent> cryoPod)
    {
        // Atmos and health scanner aren't predicted currently...
    }
}

public enum CryoPodVisualLayers : byte
{
    Base,
    Cover,
}
