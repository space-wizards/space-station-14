using Content.Client.Implants.UI;
using Content.Client.Items;
using Content.Shared.Implants;
using Content.Shared.Implants.Components;
using Robust.Shared.Prototypes;

namespace Content.Client.Implants;

public sealed class ImplanterSystem : SharedImplanterSystem
{
    [Dependency] private readonly SharedUserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ImplanterComponent, AfterAutoHandleStateEvent>(OnHandleImplanterState);
        Subs.ItemStatus<ImplanterComponent>(ent => new ImplanterStatusControl(ent));
    }

    private void OnHandleImplanterState(EntityUid uid, ImplanterComponent component, ref AfterAutoHandleStateEvent args)
    {
        if (_uiSystem.TryGetOpenUi<DeimplantBoundUserInterface>(uid, DeimplantUiKey.Key, out var bui))
        {
            Dictionary<string, string> implants = new();
            foreach (var implant in component.DeimplantWhitelist)
            {
                if (_proto.TryIndex(implant, out var proto))
                    implants.Add(proto.ID, proto.Name);
            }

            bui.UpdateState(implants, component.DeimplantChosen);
        }

        component.UiUpdateNeeded = true;
    }
}
