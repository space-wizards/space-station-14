// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.DeadSpace.Necromorphs.NecroWall;
using Content.Shared.DeadSpace.Necromorphs.NecroWall.Components;
using Robust.Client.GameObjects;

namespace Content.Client.Revenant;

public sealed class NecroWallSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NecroWallComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    private void OnAppearanceChange(EntityUid uid, NecroWallComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (_appearance.TryGetData<bool>(uid, NecroWallVisuals.Stage4, out var stage4, args.Component) && stage4)
        {
            args.Sprite.LayerSetState(0, component.Stage4);
        }
        else if (_appearance.TryGetData<bool>(uid, NecroWallVisuals.Stage3, out var stage3, args.Component) && stage3)
        {
            args.Sprite.LayerSetState(0, component.Stage3);
        }
        else if (_appearance.TryGetData<bool>(uid, NecroWallVisuals.Stage2, out var stage2, args.Component))
        {
            if (stage2)
                args.Sprite.LayerSetState(0, component.Stage2);
            else
                args.Sprite.LayerSetState(0, component.Stage1);
        }
    }
}
