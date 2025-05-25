using Content.Shared.Clothing;
using Content.Shared.Implants;
using Content.Shared.Preferences.Loadouts;
using Content.Shared.Roles;
using Content.Shared.Timing;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;

namespace Content.Client.Implants.UI;

[UsedImplicitly]
public sealed class ChameleonControllerBoundUserInterface : BoundUserInterface
{
    private readonly UseDelaySystem _delay;

    [ViewVariables]
    private ChameleonControllerMenu? _menu;

    public ChameleonControllerBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        _delay =  EntMan.System<UseDelaySystem>();
    }

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<ChameleonControllerMenu>();
        _menu.OnJobSelected += OnJobSelected;
    }

    private void OnJobSelected(ProtoId<ChameleonOutfitPrototype> outfit)
    {
        if (!EntMan.TryGetComponent<UseDelayComponent>(Owner, out var useDelayComp))
            return;

        if (!_delay.TryResetDelay((Owner, useDelayComp), true))
            return;

        SendMessage(new ChameleonControllerSelectedOutfitMessage(outfit));

        if (!_delay.TryGetDelayInfo((Owner, useDelayComp), out var delay) || _menu == null)
            return;

        _menu._lockedUntil = DateTime.Now.Add(delay.Length);
        _menu.UpdateGrid(true);
    }
}
