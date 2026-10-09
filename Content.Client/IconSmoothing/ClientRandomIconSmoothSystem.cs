using Content.Shared.IconSmoothing;
using Robust.Client.GameObjects;

namespace Content.Client.IconSmoothing;

public sealed partial class ClientRandomIconSmoothSystem : SharedRandomIconSmoothSystem
{
    [Dependency] private IconSmoothSystem _iconSmooth = default!;

    [SubscribeLocalEvent]
    private void OnAppearanceChange(Entity<RandomIconSmoothComponent> ent, ref AppearanceChangeEvent args)
    {
        if (!args.TryGetData<string>(RandomIconSmoothState.State, out var state))
            return;

        _iconSmooth.SetStateBase(ent.Owner, ent.Comp.Index, state);
    }
}
