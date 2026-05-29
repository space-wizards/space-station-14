using Content.Shared.Disposal.Components;
using Content.Shared.Disposal.Mailing;

namespace Content.Client.Disposal.Mailing;

public sealed partial class MailingUnitSystem : SharedMailingUnitSystem
{
    [Dependency] private SharedUserInterfaceSystem _userInterface = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MailingUnitComponent, AfterAutoHandleStateEvent>(OnMailingState);
    }

    private void OnMailingState(Entity<MailingUnitComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (_userInterface.TryGetOpenUi<MailingUnitBoundUserInterface>(ent.Owner, MailingUnitUiKey.Key, out var bui))
        {
            bui.Refresh(ent);
        }
    }
}
