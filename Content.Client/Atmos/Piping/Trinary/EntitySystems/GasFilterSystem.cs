using Content.Shared.Atmos.Piping.Trinary.Components;
using Content.Shared.Atmos.Piping.Trinary.EntitySystems;
using Robust.Client.GameObjects;

namespace Content.Client.Atmos.Piping.Trinary.EntitySystems;

public sealed partial class GasFilterSystem : SharedGasFilterSystem
{
    [Dependency] private UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GasFilterComponent, AfterAutoHandleStateEvent>(OnFilterState);
    }

    protected override void UpdateUi(Entity<GasFilterComponent> ent)
    {
        if (_ui.TryGetOpenUi(ent.Owner, GasFilterUiKey.Key, out var bui))
            bui.Update();
    }

    private void OnFilterState(Entity<GasFilterComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        UpdateUi(ent);
    }
}
