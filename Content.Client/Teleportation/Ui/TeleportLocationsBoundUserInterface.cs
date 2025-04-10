using Content.Shared.Teleportation;
using Content.Shared.Teleportation.Components;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client.Teleportation.Ui;

[UsedImplicitly]
public sealed class TeleportLocationsBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private TeleportMenu? _menu;

    public TeleportLocationsBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<TeleportMenu>();
        SendMessage(new TeleportLocationRequestPointsMessage());

        if (!EntMan.TryGetComponent<TeleportLocationsComponent>(Owner, out var teleComp))
            return;

        _menu._warps = teleComp.AvailableWarps;
        _menu.AddTeleportButtons();

        _menu.TeleportClicked += (netEnt, pointName) =>
        {
            SendPredictedMessage(new TeleportLocationDestinationMessage(netEnt, pointName));
        };
    }
}
