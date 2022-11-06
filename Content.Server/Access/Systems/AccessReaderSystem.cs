using Content.Server.Doors.Components;
using Content.Server.Examine;
using Content.Server.Popups;
using Content.Server.Tools;
using Content.Shared.Access;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Interaction;
using Content.Shared.Tag;
using Content.Shared.Verbs;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.Access.Systems;

public sealed class AccessReaderSystem : SharedAccessReaderSystem
{
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!;
    [Dependency] private readonly ToolSystem _toolSystem = default!;
    [Dependency] private readonly ExamineSystem _examineSystem = default!;

    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AccessReaderComponent, InteractUsingEvent>(OnInteractUsingEvent);
        SubscribeLocalEvent<AccessReaderComponent, GetVerbsEvent<InteractionVerb>>(OnGetInteractionVerbEvent);
        SubscribeLocalEvent<AccessReaderComponent, GetVerbsEvent<ExamineVerb>>(OnGetExamineVerbsEvent);
    }

    private FormattedMessage CreateMessage(List<HashSet<string>> stringLists)
    {
        var message = new FormattedMessage();
        var hashSet = new HashSet<string>();

        foreach (var entry in stringLists)
        {
            foreach (var value in entry)
            {
                hashSet.Add(value);
            }
        }

        var first = true;

        if (hashSet.Count == 0)
        {
            message.AddMarkup("N/A");
        }

        foreach (var value in hashSet)
        {
            if (!_prototypeManager.TryIndex<AccessLevelPrototype>(value, out var accessPrototype))
            {
                Logger.ErrorS(SharedIdCardConsoleSystem.Sawmill, $"Unable to find accesslevel for {value}");
                continue;
            }

            var accessName = accessPrototype.Name;

            if (!first)
            {
                message.AddMarkup(", " + accessName);
                continue;
            }
            first = false;
            message.AddMarkup(accessName);
        }

        return message;
    }

    private void OnGetExamineVerbsEvent(EntityUid uid, AccessReaderComponent component, GetVerbsEvent<ExamineVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Using == null)
            return;

        if (!_toolSystem.HasQuality(args.Using.Value, "Pulsing"))
            return;

        // Should we be able to check the access levels of other things with an AccessReaderComponent?
         if (!_tagSystem.HasTag(uid, "DoorElectronics"))
            return;

        var message = new FormattedMessage();
        message.AddMarkup("Programmed access levels: ");
        message.AddMessage(CreateMessage(component.AccessLists));

        ExamineVerb verb = new()
        {
            Act = () =>
            {
                _examineSystem.SendExamineTooltip(args.User, uid, message, false, false);
            },
            Text = Loc.GetString("verb-examine-access-text"),
            Message = Loc.GetString("verb-examine-access-message"),
            IconTexture = "/Textures/Interface/VerbIcons/vv.svg.192dpi.png",
            Category = VerbCategory.Examine
        };

        args.Verbs.Add(verb);
    }

    private void OnGetInteractionVerbEvent(EntityUid uid, AccessReaderComponent component, GetVerbsEvent<InteractionVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        if (!HasComp<IdCardComponent>(args.Using) || !TryComp<AccessComponent>(args.Using, out var access))
            return;

        if (!_tagSystem.HasTag(uid, "DoorElectronics"))
            return;

        InteractionVerb verb = new()
        {
            Act = () => UpdateAccess(uid, args.User, component, access.Tags),
            Text = Loc.GetString("verb-update-access"),
            IconTexture = "/Textures/Interface/VerbIcons/lock.svg.192dpi.png"
        };

        args.Verbs.Add(verb);
    }

    private void UpdateAccess(EntityUid targetUid, EntityUid userUid, AccessReaderComponent reader, HashSet<string> accessTags)
    {
        reader.AccessLists.Clear();
        reader.AccessLists.Add(accessTags);
        _popupSystem.PopupEntity(Loc.GetString("access-update-popup"), targetUid, Filter.Entities(userUid));
    }

    private void OnInteractUsingEvent(EntityUid uid, AccessReaderComponent component, InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (!HasComp<IdCardComponent>(args.Used) || !TryComp<AccessComponent>(args.Used, out var access))
            return;

        if (!_tagSystem.HasTag(uid, "DoorElectronics"))
            return;

        UpdateAccess(uid, args.User, component, access.Tags);

        args.Handled = true;
    }
}
