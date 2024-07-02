using Content.Shared.IconSmoothing;

namespace Content.Client.IconSmoothing;

public sealed class ClientRandomIconSmoothSystem : SharedRandomIconSmoothSystem
{
    [Dependency] private readonly IconSmoothSystem _iconSmooth = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RandomIconSmoothComponent, AfterAutoHandleStateEvent>(OnAfterHandleState);
    }

    private void OnAfterHandleState(Entity<RandomIconSmoothComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (!TryComp<IconSmoothComponent>(ent, out var smooth))
            return;
        smooth.StateBase = ent.Comp.SelectedState;
        _iconSmooth.SetStateBase(ent, smooth, ent.Comp.SelectedState);
    }
}
