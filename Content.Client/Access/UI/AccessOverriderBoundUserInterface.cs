using System.Linq;
using Content.Shared.Access;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Containers.ItemSlots;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;
using static Content.Shared.Access.Components.AccessOverriderComponent;

namespace Content.Client.Access.UI;

public sealed class AccessOverriderBoundUserInterface : BoundUserInterface
{
    private readonly AccessOverriderSystem _overrider;
    private readonly AccessReaderSystem _accessReader;

    private AccessOverriderWindow? _window;

    public AccessOverriderBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        _overrider = EntMan.System<AccessOverriderSystem>();
        _accessReader = EntMan.System<AccessReaderSystem>();
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<AccessOverriderWindow>();
        _window.Title = EntMan.GetComponent<MetaDataComponent>(Owner).EntityName;

        _window.OnSubmit += newAccessList => SendPredictedMessage(new SetAccessesMessage(newAccessList));

        _window.OnItemSlotButtonPressed +=
            () => SendPredictedMessage(new ItemSlotButtonPressedEvent(PrivilegedIdCardSlotId));

        RefreshAccess();
        Update();
    }

    public override void OnProtoReload(PrototypesReloadedEventArgs args)
    {
        if (!args.WasModified<EntityPrototype>() && !args.WasModified<AccessLevelPrototype>())
            return;

        // The system will handle updating the component if its prototype was
        // modified.
        RefreshAccess();
    }

    private void RefreshAccess()
    {
        // Weird but okay!
        if (!EntMan.TryGetComponent<AccessOverriderComponent>(Owner, out var accessOverrider))
            return;

        var accessLevels = accessOverrider.AccessLevels.ToList();

        // StringComparer is not sandboxed so we gotta do this silly thing
        // (https://github.com/space-wizards/RobustToolbox/issues/6081)
        accessLevels.Sort((a, b) => string.Compare(a.Id, b.Id, StringComparison.CurrentCulture));

        // This doesn't seem to really update the UI if it's currently being
        // looked at (even with a call to Update) but that's such a rare
        // circumstance for reload support that I don't think it matters.
        _window?.SetAccessLevels(accessLevels);
    }

    public override void Update()
    {
        if (_window == null
            || !EntMan.TryGetComponent<AccessOverriderComponent>(Owner, out var comp))
            return;

        var isAuthed = _overrider.PrivilegedIdIsAuthorized((Owner, comp));
        string? targetLabel = null;
        string? privilegedIdName = null;

        // IEnumerable for this one since we only need to enumerate it once
        // (Yes this is a fairly pointless micro-optimization)
        IEnumerable<ProtoId<AccessLevelPrototype>> missingAccess = [];
        List<ProtoId<AccessLevelPrototype>> possibleAccess = [];
        List<ProtoId<AccessLevelPrototype>> currentAccess = [];

        if (comp.TargetAccessReaderId is { } accessReader)
        {
            if (!_accessReader.GetMainAccessReader(accessReader, out var reader))
                return;

            targetLabel = Loc.GetString("access-overrider-window-target-label", ("device", accessReader));
            currentAccess = reader.Value.Comp.AccessLists.SelectMany(x => x).ToList();
        }

        if (comp.PrivilegedIdSlot.Item is { } idCard)
        {
            if (comp.TargetAccessReaderId is not null)
                possibleAccess = _accessReader.FindAccessTags(idCard).ToList();

            privilegedIdName = EntMan.GetComponent<MetaDataComponent>(idCard).EntityName;
            missingAccess = currentAccess.Except(possibleAccess);
        }

        _window.UpdateState(privilegedIdName, targetLabel, isAuthed, comp.ShowPrivilegedId, missingAccess, currentAccess, possibleAccess);
    }
}
