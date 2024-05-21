using Content.Shared.IconSmoothing;

namespace Content.Client.IconSmoothing;

public abstract class ClientRandomIconSmoothSystem : SharedRandomIconSmoothSystem
{
    [Dependency] private readonly IconSmoothSystem _iconSmooth = default!;

    protected override void UpdateVisualState(Entity<RandomIconSmoothComponent> ent, string newState)
    {
        if (!TryComp<IconSmoothComponent>(ent, out var smooth))
            return;
        smooth.StateBase = newState;

        _iconSmooth.DirtyNeighbours(ent);
    }
}
