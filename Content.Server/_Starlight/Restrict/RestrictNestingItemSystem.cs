using Content.Shared.Disposal.Components;
using Content.Shared.Disposal.Mailing;
using Content.Shared.Popups;
using Content.Shared.Starlight.Restrict;

namespace Content.Server.Starlight.Restrict;
public sealed partial class RestrictNestingItemSystem : SharedRestrictNestingItemSystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    public override void Initialize()
    {
        base.Initialize();

        //fun, so for SOME reason mailing system is server only. fml
        SubscribeLocalEvent<MailingUnitComponent, BeforeMailFlushEvent>(OnMailingUnitFlush);
    }

    private void OnMailingUnitFlush(Entity<MailingUnitComponent> ent, ref BeforeMailFlushEvent args)
    {
        //get the storage of the mailing unit
        if (RecursivelyCheckForNesting(ent, skipInitialItem: false))
        {
            _popup.PopupEntity(Loc.GetString("restrict-nesting-item-failed-to-flush-mailing"), ent);
            args.Cancel();
        }
    }
}
