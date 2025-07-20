// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Robust.Client.GameObjects;
using Content.Shared.DeadSpace.Necromorphs.InfectionDead;
using Content.Shared.DeadSpace.Necromorphs.InfectionDead.Components;

namespace Content.Client.Necromorphs.InfectionDead;

public sealed class InfectionDeadMutationAnalyzerSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InfectionDeadMutationAnalyzerComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    private void OnAppearanceChange(EntityUid uid, InfectionDeadMutationAnalyzerComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (_appearance.TryGetData<bool>(uid, InfectionDeadMutationAnalyzerVisuals.Icon, out var icon, args.Component))
        {
            if (icon)
                args.Sprite.LayerSetState(0, component.State);
            else
                args.Sprite.LayerSetState(0, component.WorkingState);
        }
    }
}

