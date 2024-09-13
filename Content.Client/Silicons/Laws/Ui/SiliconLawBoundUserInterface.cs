using System.Linq;
using Content.Shared.Silicons.Laws;
using Content.Shared.Silicons.Laws.Components;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

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

        _menu = this.CreateWindow<SiliconLawMenu>();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not SiliconLawBuiState msg)
            return;

        if (_laws != null && _laws.Count == msg.Laws.Count)
        {
            var isSame = true;
            foreach (var law in msg.Laws)
            {
                if (_laws.Contains(law))
                    continue;
                isSame = false;
                break;
            }

            if (isSame)
                return;
        }

        _laws = msg.Laws.ToList();

        _menu?.Update(_owner, msg);
    }
}
