using Content.Shared.IconSmoothing;
using Robust.Client.GameObjects;

namespace Content.Client.IconSmoothing;

public sealed class ClientRandomIconSmoothSystem : SharedRandomIconSmoothSystem
{
    [Dependency] private readonly IconSmoothSystem _iconSmooth = default!;
    [Dependency] private readonly AppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RandomIconSmoothComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    private void OnAppearanceChange(Entity<RandomIconSmoothComponent> ent, ref AppearanceChangeEvent args)
    {
        if (!TryComp<IconSmoothComponent>(ent, out var smooth))
            return;

        if (!_appearance.TryGetData<string>(ent, RandomIconSmoothState.State, out var state, args.Component))
            return;

        smooth.StateBase = state;
        _iconSmooth.SetStateBase(ent, smooth, state);
    }
}
