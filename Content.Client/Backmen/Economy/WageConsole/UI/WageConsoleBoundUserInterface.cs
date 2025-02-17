using Content.Shared.Backmen.Economy.WageConsole;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Client.Backmen.Economy.WageConsole.UI;

[UsedImplicitly]
public sealed class WageConsoleBoundUserInterface : BoundUserInterface
{
    [Dependency] private readonly IPrototypeManager _proto = default!;

    private WageConsoleWindow? _window;
    private EditWageRowWindow? _editWindow;
    private BonusWageWindow? _bonusWageWindow;

    public WageConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        var comp = EntMan.GetComponent<WageConsoleComponent>(Owner);

        var stationName = "";
        if (EntMan.TryGetComponent<TransformComponent>(Owner, out var xform) && xform.GridUid != null &&
            EntMan.TryGetComponent<MetaDataComponent>(xform.GridUid, out var stationData))
        {
            stationName = stationData.EntityName;
        }

        _window = new(Owner, comp, _proto, stationName);
        // _window.OnKeySelected += (key, count) => SendMessage(new ChangeReinforcementMsg(key, count));
        // _window.OnBriefChange += (brief) =>
        //     SendMessage(new WageUpdate(brief[..Math.Min(brief.Length, comp.MaxStringLength)]));
        // _window.OnStartCall += () => SendMessage(new CallReinforcementStart());

        _window.OnWageRowEdit += (u) => { SendMessage(new OpenWageRowMsg(u)); };
        _window.OnBonusWage += (u) => { SendMessage(new OpenBonusWageMsg(u)); };

        _window.OnClose += Close;
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is UpdateWageConsoleUi s)
        {
            if (_editWindow != null || _bonusWageWindow != null)
            {
                _editWindow?.Close();
                _editWindow = null;
                _bonusWageWindow?.Close();
                _bonusWageWindow = null;
            }

            _window?.Update(s);
        }

        if (state is OpenEditWageConsoleUi e)
        {
            if (_editWindow != null)
            {
                _editWindow.Close();
                _bonusWageWindow?.Close();
            }

            _editWindow = new EditWageRowWindow(e);
            _editWindow.OnSaveEditedWageRow += (u, point2) => { SendMessage(new SaveEditedWageRowMsg(u, point2)); };
        }

        if (state is OpenBonusWageConsoleUi b)
        {
            if (_bonusWageWindow != null)
            {
                _editWindow?.Close();
                _bonusWageWindow.Close();
                _bonusWageWindow = null;
            }

            _bonusWageWindow = new BonusWageWindow(b);
            _bonusWageWindow.OnBonusWageRow += (u, point2) =>
            {
                SendMessage(new BonusWageRowMsg(u, point2));
                _bonusWageWindow.Close();
            };
        }
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        _window?.Close();
        _editWindow?.Close();
    }
}
