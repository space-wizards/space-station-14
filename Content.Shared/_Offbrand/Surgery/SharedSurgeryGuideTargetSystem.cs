using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.GameObjects;

namespace Content.Shared._Offbrand.Surgery;

public abstract class SharedSurgeryGuideTargetSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _userInterface = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SurgeryToolComponent, GetVerbsEvent<UtilityVerb>>(OnGetVerbs);

        Subs.BuiEvents<SurgeryGuideTargetComponent>(SurgeryGuideUiKey.Key,
                sub =>
                {
                    sub.Event<SurgeryGuideStartSurgeryMessage>(OnStartSurgery);
                    sub.Event<SurgeryGuideStartCleanupMessage>(OnStartCleanup);
                });
    }

    private void OnGetVerbs(Entity<SurgeryToolComponent> ent, ref GetVerbsEvent<UtilityVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || !HasComp<SurgeryGuideTargetComponent>(args.Target))
            return;

        var @event = args;
        args.Verbs.Add(new UtilityVerb()
        {
            Act = () =>
            {
                _userInterface.OpenUi(@event.Target, SurgeryGuideUiKey.Key, @event.User);
            },
            Text = Loc.GetString("verb-perform-surgery"),
        });
    }

    protected virtual void OnStartSurgery(Entity<SurgeryGuideTargetComponent> ent, ref SurgeryGuideStartSurgeryMessage args)
    {
        _userInterface.CloseUi(ent.Owner, SurgeryGuideUiKey.Key, args.Actor);
        _popup.PopupPredictedCursor(Loc.GetString("surgery-examine-for-instructions"), args.Actor);
    }

    protected virtual void OnStartCleanup(Entity<SurgeryGuideTargetComponent> ent, ref SurgeryGuideStartCleanupMessage args)
    {
        _userInterface.CloseUi(ent.Owner, SurgeryGuideUiKey.Key, args.Actor);
        _popup.PopupPredictedCursor(Loc.GetString("surgery-examine-for-instructions"), args.Actor);
    }
}
