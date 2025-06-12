using Content.Shared.Atmos.Components;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;

namespace Content.Client.Body.Systems;

public sealed class InternalsSystem : SharedInternalsSystem
{
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<InternalsComponent, AfterAutoHandleStateEvent>(OnInternalsAfterState);
    }

    private void OnInternalsAfterState(Entity<InternalsComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (ent.Comp.GasTankEntity != null && _ui.TryGetOpenUi(ent.Comp.GasTankEntity.Value, SharedGasTankUiKey.Key, out var bui))
        {
            bui.Update();
        }
    }
}
