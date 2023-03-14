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
    [Dependency] private readonly ExamineSystem _examine = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly ToolSystem _tool = default!;

    public override void Initialize()
    {
        base.Initialize();

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
            message.AddMarkup(Loc.GetString("generic-not-available-shorthand"));
        }

        foreach (var value in hashSet)
        {
            if (!_proto.TryIndex<AccessLevelPrototype>(value, out var accessPrototype))
            {
                Logger.ErrorS(SharedIdCardConsoleSystem.Sawmill, $"Unable to find accesslevel for {value}");
                continue;
            }

            var accessName = Loc.GetString(accessPrototype.Name ?? "");

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

        if (!_tool.HasQuality(args.Using.Value, "Pulsing"))
            return;

        // Should we be able to check the access levels of other things with an AccessReaderComponent?
         if (!_tag.HasTag(uid, "DoorElectronics"))
            return;

        var message = new FormattedMessage();
        message.AddMarkup("Programmed access levels: ");
        message.AddMessage(CreateMessage(component.AccessLists));

        ExamineVerb verb = new()
        {
            Act = () =>
            {
                _examine.SendExamineTooltip(args.User, uid, message, false, false);
            },
            Text = Loc.GetString("verb-examine-access-text"),
            Message = Loc.GetString("verb-examine-access-message"),
            Icon = new SpriteSpecifier.Texture(new ResourcePath("/Textures/Interface/VerbIcons/vv.svg.192dpi.png")),
            Category = VerbCategory.Examine
        };

        args.Verbs.Add(verb);
    }
}
