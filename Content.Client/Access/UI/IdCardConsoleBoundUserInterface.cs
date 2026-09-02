using Content.Shared.Access;
using Content.Shared.Access.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.CrewManifest;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;
using static Content.Shared.Access.Components.IdCardConsoleComponent;

namespace Content.Client.Access.UI;

/// <summary>
/// A BUI for the ID card computer, wraps a <see cref="IdCardConsoleWindow"/>.
/// </summary>
/// <seealso cref="IdCardConsoleComponent"/>
public sealed partial class IdCardConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private IdCardConsoleWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<IdCardConsoleWindow>();
        _window.Title = EntMan.GetComponent<MetaDataComponent>(Owner).EntityName;

        List<ProtoId<AccessLevelPrototype>> accessLevels;
        if (EntMan.TryGetComponent<IdCardConsoleComponent>(Owner, out var idCard))
        {
            accessLevels = idCard.AccessLevels;
        }
        else
        {
            accessLevels = new();
            Logger.GetSawmill("id_card_console").Error($"No IdCardConsole component found for {EntMan.ToPrettyString(Owner)}!");
        }
        _window.SetAccessLevels(accessLevels);

        _window.OnDataChanged += SubmitData;
        _window.CrewManifestButton.OnPressed += _ => SendMessage(new CrewManifestOpenUiMessage());
        _window.PrivilegedIdButton.OnPressed += _ => SendMessage(new ItemSlotButtonPressedEvent(PrivilegedIdCardSlotId));
        _window.TargetIdButton.OnPressed += _ => SendMessage(new ItemSlotButtonPressedEvent(TargetIdCardSlotId));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        var castState = (IdCardConsoleBoundUserInterfaceState)state;
        _window?.UpdateState(castState);
    }

    public void SubmitData(IdCardData data)
    {
        SendMessage(new WriteToTargetIdMessage(
            data.FullName,
            data.JobTitle,
            data.Accesses,
            data.JobPrototype));
    }
}

