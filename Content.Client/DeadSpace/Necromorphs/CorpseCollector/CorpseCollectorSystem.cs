// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Robust.Client.GameObjects;
using Content.Shared.DeadSpace.Necromorphs.CorpseCollector;
using Content.Shared.DeadSpace.Necromorphs.CorpseCollector.Components;
using Content.Client.Necromorphs.InfectionDead;

namespace Content.Client.DeadSpace.Necromorphs.CorpseCollector;

public sealed class CorpseCollectorSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CorpseCollectorComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }
    private void OnAppearanceChange(EntityUid uid, CorpseCollectorComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!args.Sprite.LayerMapTryGet(NecromorfLayers.Necromorf, out var index))
            return;

        if (_appearance.TryGetData<bool>(uid, CorpseCollectorVisuals.lvl3, out var lvl3, args.Component) && lvl3)
        {
            args.Sprite.LayerSetState(index, component.Lvl3State);
        }
        else if (_appearance.TryGetData<bool>(uid, CorpseCollectorVisuals.lvl2, out var lvl2, args.Component))
        {
            if (lvl2)
                args.Sprite.LayerSetState(index, component.Lvl2State);
            else
                args.Sprite.LayerSetState(index, component.State);
        }
    }
}
