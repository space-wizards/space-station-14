using Content.Shared.Necroobelisk;
using Content.Shared.Necroobelisk.Components;
using Robust.Client.GameObjects;

namespace Content.Client.Necroobelisk;

public sealed class NecroobeliskSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NecroobeliskComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    private void OnAppearanceChange(EntityUid uid, NecroobeliskComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (_appearance.TryGetData<bool>(uid, NecroobeliskVisuals.Unactive, out var unactive, args.Component))
        {
            if (unactive)
                args.Sprite.LayerSetState(0, component.UnactiveState);
            else
                args.Sprite.LayerSetState(0, component.State);
        }
    }
}

