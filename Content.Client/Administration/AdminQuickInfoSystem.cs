using System.Linq;
using Content.Client.Administration.Systems;
using Content.Client.Stylesheets;
using Content.Shared.Administration;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;
using Robust.Shared.RichText;
using Robust.Shared.Utility;

namespace Content.Client.Administration;

// Server-validated, AnyCommand is acceptable here.
[AnyCommand]
internal sealed partial class AdminQuickInfoCommand : LocalizedEntityCommands
{
    [Dependency] private AdminQuickInfoSystem _quickInfo = null!;

    public override string Command => QuickInfoShared.CommandName;

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
            return;

        var idsArg = args[0].Split(',');
        var ids = new NetEntity[idsArg.Length];
        for (var i = 0; i < ids.Length; i++)
        {
            if (!NetEntity.TryParse(idsArg[i], out ids[i]))
                return;
        }

        _quickInfo.OpenPopupFor(ids);
    }
}

internal sealed partial class AdminQuickInfoSystem : EntitySystem
{
    [Dependency] private AdminSystem _adminSystem = null!;

    public event Action<QuickInfoShared.SingleEntityInfo>? EntityResponseReceived;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<QuickInfoShared.Response>(HandleInfoResponse);
    }

    private void HandleInfoResponse(QuickInfoShared.Response ev)
    {
        foreach (var singleEntityInfo in ev.Entities)
        {
            EntityResponseReceived?.Invoke(singleEntityInfo);
        }
    }

    public void OpenPopupFor(NetEntity[] entities)
    {
        var vBox = new VBox();
        var popup = new Popup
        {
            Children =
            {
                new PanelContainer
                {
                    StyleClasses = { StyleClass.TooltipPanel },
                    Children =
                    {
                        vBox,
                    },
                },
            },
        };

        foreach (var entity in entities)
        {
            var playerInfo = _adminSystem.PlayerList.FirstOrDefault(p => p.NetEntity == entity);
            var control = new InfoControl(this, entity, playerInfo);
            popup.OnPopupHide += () => control.Unsubscribe();
            vBox.AddChild(control);
        }

        popup.OpenAtMouse();

        RaiseNetworkEvent(new QuickInfoShared.Request { Entities = entities });
    }

    private sealed class InfoControl : Control
    {
        private readonly AdminQuickInfoSystem _system;
        private readonly NetEntity _entity;
        private readonly PlayerInfo? _playerInfo;
        private QuickInfoShared.SingleEntityInfo? _response;

        private readonly RichTextLabel _contents = new();

        private ILocalizationManager Loc => _system.Loc;

        public InfoControl(AdminQuickInfoSystem system, NetEntity entity, PlayerInfo? playerInfo)
        {
            _system = system;
            _entity = entity;
            _playerInfo = playerInfo;

            AddChild(_contents);

            system.EntityResponseReceived += OnEntityResponseReceived;
            Rebuild();
        }

        private void OnEntityResponseReceived(QuickInfoShared.SingleEntityInfo ev)
        {
            // Yes I know this is O(n^2)
            // Realistically, n is going to be 30 (default cvar limit) or less. Not a big deal.
            if (ev.Entity != _entity)
                return;

            _response = ev;
            Rebuild();
        }

        public void Unsubscribe()
        {
            // This, too, is O(n^2). I think.
            _system.EntityResponseReceived -= OnEntityResponseReceived;
        }

        private void Rebuild()
        {
            var sb = new FormattedStringBuilder();

            if (_playerInfo != null)
            {
                sb.AppendMarkupLine(Loc.GetString("admin-quick-info-username",
                    ("username", _playerInfo.Username),
                    ("playtime", _playerInfo.PlaytimeString)));
                if (_playerInfo.CharacterName != _playerInfo.IdentityName)
                {
                    sb.AppendMarkupLine(Loc.GetString("admin-quick-info-character-identity",
                        ("character", _playerInfo.CharacterName),
                        ("identity", _playerInfo.IdentityName)));
                }
                else
                {
                    sb.AppendMarkupLine(Loc.GetString("admin-quick-info-character",
                        ("character", _playerInfo.CharacterName)));
                }

                sb.AppendMarkupLine(Loc.GetString("admin-quick-info-job", ("job", _playerInfo.StartingJob)));

                if (_playerInfo.RoleProto is { } roleId && _system.ProtoMan.Resolve(roleId, out var role))
                {
                    var roleColor = role.Color.ToHex();
                    var symbol = _playerInfo.Antag ? role.Symbol : "";

                    var roleTypeString = Loc.GetString(role.Name);
                    var roleSubtypeString = _playerInfo.Subtype is { } subtype ? Loc.GetString(subtype) : null;

                    var roleString = roleSubtypeString != null
                        ? Loc.GetString("admin-quick-info-role-type-subtype",
                            ("color", roleColor),
                            ("symbol", symbol),
                            ("type", roleTypeString),
                            ("subtype", roleSubtypeString))
                        : Loc.GetString("admin-quick-info-role-type",
                            ("color", roleColor),
                            ("symbol", symbol),
                            ("type", roleTypeString));
                    sb.AppendMarkupLine(roleString);
                }
                else
                {
                    sb.AppendMarkupLine(Loc.GetString("admin-quick-info-no-role"));
                }
            }

            if (_response == null)
            {
                sb.AppendMarkupLine(Loc.GetString("admin-quick-info-loading", ("entity", _entity)));
            }
            else
            {
                if (!_response.Exists)
                {
                    sb.AppendMarkupLine(Loc.GetString("admin-quick-info-entity-missing", ("entity", _entity)));
                }
                else
                {
                    sb.AppendMarkupLine(Loc.GetString("admin-quick-info-entity",
                        ("entity", _entity),
                        ("name", _response.Name),
                        ("prototype", _response.Prototype ?? Loc.GetString("admin-quick-info-no-prototype"))));
                }
            }

            // Link line at the bottom
            if (_playerInfo != null)
            {
                // Player panel
                sb.MakeCommandLinkTag(
                    Loc.GetString("admin-quick-link-playerpanel"),
                    CommandParsing.EscapeCommand(
                        AdminCommandSyntax.NamePlayerPanel,
                        _playerInfo.SessionId.ToString()),
                    Loc.GetString("admin-quick-link-playerpanel-tooltip"));
                sb.AppendText(" ");

                // Send Message
                sb.MakeCommandLinkTag(
                    Loc.GetString("admin-quick-link-message"),
                    CommandParsing.EscapeCommand(
                        AdminCommandSyntax.NameOpenAdminHelp,
                        _playerInfo.SessionId.ToString()),
                    Loc.GetString("admin-quick-link-message-tooltip"));
                sb.AppendText(" ");
            }

            if (_response is not { Exists: false })
            {
                // Jump/follow
                sb.MakeCommandLinkTag(
                    Loc.GetString("admin-quick-link-follow"),
                    CommandParsing.EscapeCommand(
                        AdminCommandSyntax.NameFollow,
                        _entity.ToString()),
                    Loc.GetString("admin-quick-link-follow-tooltip"));
                sb.AppendText(" ");
            }

            _contents.SetMessage(FormattedMessage.FromMarkupOrThrow(sb.ToString().Trim()), tagsAllowed: null);
        }
    }
}
