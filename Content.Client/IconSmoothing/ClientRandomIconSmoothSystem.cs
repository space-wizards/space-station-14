using Content.Shared.IconSmoothing;
using Robust.Client.GameObjects;

namespace Content.Client.IconSmoothing;

public sealed partial class ClientRandomIconSmoothSystem : SharedRandomIconSmoothSystem
{
    [Dependency] private IconSmoothSystem _iconSmooth = default!;
    [Dependency] private AppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RandomIconSmoothComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    private void OnAppearanceChange(Entity<RandomIconSmoothComponent> ent, ref AppearanceChangeEvent args)
    {
        if (!_appearance.TryGetData<string>(ent, RandomIconSmoothState.State, out var state, args.Component))
            return;

        _iconSmooth.SetStateBase(ent.Owner, ent.Comp.Index, state);
    }
}
