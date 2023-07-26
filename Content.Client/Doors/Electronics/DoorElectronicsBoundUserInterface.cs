using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;
using Content.Shared.Doors.Electronics;

namespace Content.Client.Doors.Electronics;

public sealed class DoorElectronicsBoundUserInterface : BoundUserInterface
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;

    private DoorElectronicsConfigurationMenu? _window;

    public DoorElectronicsBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();
        List<string> accessLevels;

        if (_entityManager.TryGetComponent<DoorElectronicsComponent>(Owner, out var doorElectronics))
        {
            accessLevels = doorElectronics.AccessLevels;
            accessLevels.Sort();
        }
        else
        {
            accessLevels = new List<string>();
            Logger.ErrorS(SharedDoorElectronicsSystem.Sawmill, $"No DoorElectronicsComponent component found for {_entityManager.ToPrettyString(Owner)}!");
        }

        _window = new DoorElectronicsConfigurationMenu(this, accessLevels, _prototypeManager);
        _window.OnClose += Close;
        _window.OpenCentered();

        SendMessage(new DoorElectronicsRefreshUiMessage());
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        var castState = (DoorElectronicsConfigurationState) state;

        _window?.UpdateState(castState);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing) return;

        _window?.Dispose();
    }

    public void UpdateConfiguration(List<string> newAccessList)
    {
        SendMessage(new DoorElectronicsUpdateConfigurationMessage(newAccessList));
    }
}
