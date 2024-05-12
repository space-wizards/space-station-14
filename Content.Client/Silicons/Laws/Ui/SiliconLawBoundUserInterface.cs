using System.Linq;
using Content.Shared.Silicons.Laws;
using Content.Shared.Silicons.Laws.Components;
using JetBrains.Annotations;

namespace Content.Client.Silicons.Laws.Ui;

[UsedImplicitly]
public sealed class SiliconLawBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private SiliconLawMenu? _menu;
    private EntityUid _owner;
    private List<SiliconLaw>? _laws;

    public SiliconLawBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        _owner = owner;
    }

    protected override void Open()
    {
        base.Open();

        _menu = new();

        _menu.OnClose += Close;
        _menu.OpenCentered();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing)
            return;
        _menu?.Close();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not SiliconLawBuiState msg)
            return;

        if (_laws != null && _laws.Count == msg.Laws.Count)
        {
            var isSame = true;
            msg.Laws.Sort();
            for (var i = 0; i < _laws.Count; i++)
            {
                if (_laws[i].LawString == msg.Laws[i].LawString)
                    continue;

                isSame = false;
                break;
            }

            if (isSame)
                return;
        }

        _laws = msg.Laws.ToList();
        _laws.Sort();

        _menu?.Update(_owner, msg);
    }
}
