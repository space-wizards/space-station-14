using Content.Shared.Access;
using Content.Shared.Doors.Components;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;

namespace Content.Client.Doors.UI;

public sealed class DoorElectronicsBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{

    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    private DoorElectronicsConfigurationMenu? _window;

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

        var castState = (DoorElectronicsConfigurationState)state;

        _window?.UpdateState(castState);
    }

    private void UpdateConfiguration(List<ProtoId<AccessLevelPrototype>> newAccessList)
    {
        SendMessage(new DoorElectronicsUpdateConfigurationMessage(newAccessList));
    }

}
