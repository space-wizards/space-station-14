using Content.Shared.Access;
using Content.Shared.Doors.Electronics;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;

namespace Content.Client.Doors.Electronics;

public sealed class DoorElectronicsBoundUserInterface : BoundUserInterface
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    private DoorElectronicsConfigurationMenu? _window;

    public DoorElectronicsBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindow<DoorElectronicsConfigurationMenu>();
        _window.OnAccessChanged += UpdateConfiguration;
        Reset();
    }

    public override void OnProtoReload(PrototypesReloadedEventArgs args)
    {
        base.OnProtoReload(args);

        if (!args.WasModified<AccessLevelPrototype>())
            return;

        Reset();
    }

    private void Reset()
    {
        List<ProtoId<AccessLevelPrototype>> accessLevels = new();

        foreach (var accessLevel in _prototypeManager.EnumeratePrototypes<AccessLevelPrototype>())
        {
            if (accessLevel.Name != null)
            {
                accessLevels.Add(accessLevel.ID);
            }
        }

        accessLevels.Sort();
        _window?.Reset(_prototypeManager, accessLevels);
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        var castState = (DoorElectronicsConfigurationState) state;

        _window?.UpdateState(castState);
    }

    public void UpdateConfiguration(List<ProtoId<AccessLevelPrototype>> newAccessList)
    {
        SendMessage(new DoorElectronicsUpdateConfigurationMessage(newAccessList));
    }
}
