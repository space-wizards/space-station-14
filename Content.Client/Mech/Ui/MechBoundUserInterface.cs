using JetBrains.Annotations;
using Content.Client.UserInterface;
using Content.Shared.Mech;
using Content.Shared.Mech.Systems;
using Robust.Client.Timing;
using Robust.Client.UserInterface;

namespace Content.Client.Mech.Ui;

[UsedImplicitly]
public sealed class MechBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey), IBuiPreTickUpdate
{
    [Dependency] private readonly IClientGameTiming _gameTiming = null!;

    [ViewVariables]
    private MechMenu? _menu;

    private BuiPredictionState? _pred;

    // Input coalescers for performance optimization
    private InputCoalescer<bool> _airtightCoalescer;
    private InputCoalescer<bool> _fanCoalescer;
    private InputCoalescer<bool> _filterCoalescer;

    protected override void Open()
    {
        base.Open();

        _pred = new BuiPredictionState(this, _gameTiming);

        _menu = this.CreateWindowCenteredLeft<MechMenu>();
        _menu.SetEntity(Owner);
        _menu.SetParentBui(this);

        // Equipment and module removal
        _menu.OnRemoveButtonPressed += uid =>
        {
            _pred.SendMessage(new MechEquipmentRemoveMessage(EntMan.GetNetEntity(uid)));
        };
        _menu.OnRemoveModuleButtonPressed += uid =>
        {
            _pred.SendMessage(new MechModuleRemoveMessage(EntMan.GetNetEntity(uid)));
        };

        // Cabin control
        _menu.OnAirtightChanged += isAirtight => _airtightCoalescer.Set(isAirtight);
        _menu.OnFanToggle += isActive => _fanCoalescer.Set(isActive);
        _menu.OnFilterToggle += enabled => _filterCoalescer.Set(enabled);

        // Direct action
        _menu.OnCabinPurge += () => _pred.SendMessage(new MechCabinAirMessage());

        // DNA lock
        _menu.OnDnaLockRegister += () => _pred.SendMessage(new MechDnaLockRegisterMessage());
        _menu.OnDnaLockToggle += () => _pred.SendMessage(new MechDnaLockToggleMessage());
        _menu.OnDnaLockReset += () => _pred.SendMessage(new MechDnaLockResetMessage());

        // Card lock
        _menu.OnCardLockRegister += () => _pred.SendMessage(new MechCardLockRegisterMessage());
        _menu.OnCardLockToggle += () => _pred.SendMessage(new MechCardLockToggleMessage());
        _menu.OnCardLockReset += () => _pred.SendMessage(new MechCardLockResetMessage());
    }

    void IBuiPreTickUpdate.PreTickUpdate()
    {
        // Send coalesced input events
        if (_airtightCoalescer.CheckIsModified(out var airtightValue))
            _pred!.SendMessage(new MechAirtightMessage(airtightValue));

        if (_fanCoalescer.CheckIsModified(out var fanValue))
            _pred!.SendMessage(new MechFanToggleMessage(fanValue));

        if (_filterCoalescer.CheckIsModified(out var filterValue))
            _pred!.SendMessage(new MechFilterToggleMessage(filterValue));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is not MechBoundUiState mechState)
            return;

        // Apply any pending predicted messages to state
        foreach (var replayMsg in _pred!.MessagesToReplay())
        {
            switch (replayMsg)
            {
                case MechAirtightMessage airtight:
                    mechState.IsAirtight = airtight.IsAirtight;
                    break;

                case MechFanToggleMessage fanToggle:
                    mechState.FanActive = fanToggle.IsActive;
                    mechState.FanState = fanToggle.IsActive ? MechFanState.On : MechFanState.Off;
                    break;

                case MechFilterToggleMessage filterToggle:
                    mechState.FilterEnabled = filterToggle.Enabled;
                    break;
            }
        }

        _menu?.UpdateState(mechState);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing)
            return;

        _menu?.Close();
        _menu = null;
    }
}
