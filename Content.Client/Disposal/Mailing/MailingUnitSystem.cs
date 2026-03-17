using Content.Shared.Disposal;
using Content.Shared.Disposal.Components;
using Content.Shared.Disposal.Mailing;

namespace Content.Client.Disposal.Mailing;

public sealed class MailingUnitSystem : SharedMailingUnitSystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MailingUnitComponent, AfterAutoHandleStateEvent>(OnMailingState);
    }

    private void OnMailingState(Entity<MailingUnitComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (UserInterfaceSystem.TryGetOpenUi<MailingUnitBoundUserInterface>(ent.Owner, MailingUnitUiKey.Key, out var bui))
        {
            bui.Refresh(ent);
        }
    }
}
