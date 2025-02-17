// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Client.Backmen.Economy.Eftpos.UI;
using Content.Shared.Backmen.Economy.Eftpos;

namespace Content.Client.Backmen.Economy.Eftpos;

public sealed class EftposBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private EftposMenu? _menu;

    public EftposBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {

    }

    protected override void Open()
    {
        base.Open();
        _menu = new EftposMenu { Title = IoCManager.Resolve<IEntityManager>().GetComponent<MetaDataComponent>(Owner).EntityName };

        _menu.OnChangeValue += (value) => SendMessage(new EftposChangeValueMessage(value));
        _menu.OnResetValue += () => SendMessage(new EftposChangeValueMessage(null));
        _menu.OnChangeLinkedAccount += (accountNumber) => SendMessage(new EftposChangeLinkedAccountNumberMessage(accountNumber));
        _menu.OnResetLinkedAccount += () => SendMessage(new EftposChangeLinkedAccountNumberMessage(null));

        _menu.OnSwipeCard += () => SendMessage(new EftposSwipeCardMessage());
        _menu.OnLock += () => SendMessage(new EftposLockMessage());

        _menu.OnClose += Close;
        _menu.OpenCentered();
    }
    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        var castState = (SharedEftposComponent.EftposBoundUserInterfaceState) state;
        _menu?.UpdateState(castState);
    }
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing)
            return;

        if (_menu == null)
            return;

        _menu.OnClose -= Close;
        _menu.Dispose();
    }
}
