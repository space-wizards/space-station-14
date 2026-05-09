using Content.Shared.Silicons.Borgs;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client.Silicons.Borgs;

[UsedImplicitly]
public sealed class BorgBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private BorgMenu? _menu;

    public BorgBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<BorgMenu>();
        _menu.SetEntity(Owner);

        _menu.BrainButtonPressed += () =>
        {
            SendPredictedMessage(new BorgEjectBrainBuiMessage());
        };

        _menu.EjectBatteryButtonPressed += () =>
        {
            SendPredictedMessage(new BorgEjectBatteryBuiMessage());
        };

        _menu.NameChanged += name =>
        {
            SendPredictedMessage(new BorgSetNameBuiMessage(name));
        };

        _menu.RemoveModuleButtonPressed += module =>
        {
            SendPredictedMessage(new BorgRemoveModuleBuiMessage(EntMan.GetNetEntity(module)));
        };
    }

    public override void Update()
    {
        _menu?.UpdateBatteryButton();
        _menu?.UpdateBrainButton();
        _menu?.UpdateModulePanel();
    }
}
