using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Components;
using Content.Shared.FixedPoint;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;

namespace Content.Client.DeviceLinking.UI;

[UsedImplicitly]
public sealed class ThresholdAmountBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private ThresholdAmountWindow? _window;

    public ThresholdAmountBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey) {}

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindow<ThresholdAmountWindow>();

        if (EntMan.TryGetComponent<PowerThresholdComponent>(Owner, out var comp))
            _window.SetBounds(comp.MinimumThresholdAmount, comp.MaximumThresholdAmount);

        _window.ApplyButton.OnPressed += _ =>
        {
            if (int.TryParse(_window.AmountLineEdit.Text, out var i))
            {
                SendMessage(new ThresholdAmountSetValueMessage(i));
                _window.Close();
            }
        };
    }
}
