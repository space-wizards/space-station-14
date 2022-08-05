using Content.Shared.OuterRim.Worldgen.Components;
using Robust.Shared.GameStates;

namespace Content.Shared.OuterRim.Worldgen.Systems;

/// <summary>
/// This handles the networking for GridIdentityComponent.
/// </summary>
public sealed class GridIdentitySystem : EntitySystem
{
    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<GridIdentityComponent, ComponentGetState>(GetGridIdentityState);
        SubscribeLocalEvent<GridIdentityComponent, ComponentHandleState>(HandleGridIdentityState);
    }

    private void HandleGridIdentityState(EntityUid uid, GridIdentityComponent component, ref ComponentHandleState args)
    {
        if (args.Next is null)
            return;

        component.GridColor = ((GridIdentityComponentState) args.Next).GridColor;

        component.ShowIff = ((GridIdentityComponentState) args.Next).ShowIff;
    }

    private void GetGridIdentityState(EntityUid uid, GridIdentityComponent component, ref ComponentGetState args)
    {
        args.State = new GridIdentityComponentState(component);
    }
}
