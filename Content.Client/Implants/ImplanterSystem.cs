using Content.Client.Implants.UI;
using Content.Client.Items;
using Content.Shared.Implants;
using Content.Shared.Implants.Components;

namespace Content.Client.Implants;

public sealed partial class ImplanterSystem : SharedImplanterSystem
{
    [Dependency] private SharedUserInterfaceSystem _uiSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ImplanterComponent, AfterAutoHandleStateEvent>(OnHandleImplanterState);
        Subs.ItemStatus<ImplanterComponent>(ent => new ImplanterStatusControl(ent));
    }

    private void OnHandleImplanterState(Entity<ImplanterComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        UpdateUi(ent);
    }

    protected override void UpdateUi(Entity<ImplanterComponent> ent)
    {
        if (_uiSystem.TryGetOpenUi(ent.Owner, DeimplantUiKey.Key, out var bui))
        {
            bui.Update();
        }

        ent.Comp.UiUpdateNeeded = true;
    }
}
