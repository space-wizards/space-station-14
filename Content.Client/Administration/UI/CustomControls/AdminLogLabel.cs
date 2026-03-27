using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input;
using Robust.Shared.Utility;

namespace Content.Client.Administration.UI.CustomControls;

public sealed class AdminLogLabel : BoxContainer
{
    private static readonly Color TimestampColor = new(120, 120, 130);
    private static readonly Color ServerNameColor = new(90, 125, 160);

    //Impact indicator colors
    private static readonly Color ImpactLowColor = new(90, 90, 100);
    private static readonly Color ImpactMediumColor = new(160, 160, 170);
    private static readonly Color ImpactHighColor = new(168, 139, 94);
    private static readonly Color ImpactExtremeColor = new(187, 50, 50);

    //Metadata
    private static readonly Color MetadataLabelColor = new(90, 90, 100);
    private static readonly Color MetadataValueColor = new(140, 140, 150);

    //Condensed badge
    private static readonly Color CondensedBadgeColor = new(180, 140, 60);
    private static readonly Color SampleMessageColor = new(140, 140, 150);

    //Entity role colors
    private static readonly Color RoleActorColor = new(120, 180, 240);
    private static readonly Color RoleTargetColor = new(230, 190, 70);
    private static readonly Color RoleToolColor = new(100, 195, 195);
    private static readonly Color RoleVictimColor = new(210, 95, 95);
    private static readonly Color RoleContainerColor = new(130, 130, 145);
    private static readonly Color RoleSubjectColor = new(170, 150, 200);
    private static readonly Color RoleOtherColor = new(130, 130, 145);

    /// <summary>
    /// LogTypes that represent condensed/burst events.
    /// </summary>
    private static readonly HashSet<LogType> BurstTypes = new()
    {
        LogType.CombatModeToggleBurst,
        LogType.InteractionRepeatBurst,
        LogType.MeleeMissBurst,
    };

    private readonly RichTextLabel _messageLabel;
    private readonly RichTextLabel? _sampleLabel;
    private bool _showMetadata;
    private bool _isExpanded;

    public AdminLogLabel(ref SharedAdminLog log, HSeparator separator, bool showMetadata = false)
    {
        Log = log;
        Separator = separator;
        _showMetadata = showMetadata;

        Orientation = LayoutOrientation.Vertical;
        HorizontalExpand = true;
        Margin = new Thickness(2, 1, 2, 1);

        // Main log message
        _messageLabel = new RichTextLabel { HorizontalExpand = true };
        _messageLabel.SetMessage(BuildMessage(log, _showMetadata));
        AddChild(_messageLabel);

        // Expandable for condensed entries
        if (log.SampleMessages is { Length: > 0 })
        {
            _sampleLabel = new RichTextLabel { HorizontalExpand = true, Visible = false };
            _sampleLabel.SetMessage(BuildSampleMessages(log.SampleMessages));
            AddChild(_sampleLabel);

            //Make clickable to expand/collapse
            MouseFilter = MouseFilterMode.Stop;
        }

        OnVisibilityChanged += VisibilityChanged;
    }

    public SharedAdminLog Log { get; }

    public HSeparator Separator { get; }

    /// <summary>
    /// Returns true if this log entry represents a condensed/burst event.
    /// </summary>
    public bool IsCondensed => BurstTypes.Contains(Log.Type) || Log.Message.StartsWith("[×");

    protected override void KeyBindDown(GUIBoundKeyEventArgs args)
    {
        base.KeyBindDown(args);

        if (_sampleLabel == null)
            return;

        if (args.Function == EngineKeyFunctions.UIClick)
        {
            _isExpanded = !_isExpanded;
            _sampleLabel.Visible = _isExpanded;
            args.Handle();
        }
    }

    private static Color GetImpactColor(LogImpact impact)
    {
        return impact switch
        {
            LogImpact.Low => ImpactLowColor,
            LogImpact.Medium => ImpactMediumColor,
            LogImpact.High => ImpactHighColor,
            LogImpact.Extreme => ImpactExtremeColor,
            _ => ImpactMediumColor
        };
    }

    // Returns a sort key for entity roles so metadata always displays in
    // narrative order: Actor → Tool → Victim → Target → Container → Subject → Other.
    private static int GetRoleSortKey(AdminLogEntityRole role)
    {
        return role switch
        {
            AdminLogEntityRole.Actor => 0,
            AdminLogEntityRole.Tool => 1,
            AdminLogEntityRole.Victim => 2,
            AdminLogEntityRole.Target => 3,
            AdminLogEntityRole.Container => 4,
            AdminLogEntityRole.Subject => 5,
            _ => 6
        };
    }

    private static Color GetRoleColor(AdminLogEntityRole role)
    {
        return role switch
        {
            AdminLogEntityRole.Actor => RoleActorColor,
            AdminLogEntityRole.Target => RoleTargetColor,
            AdminLogEntityRole.Tool => RoleToolColor,
            AdminLogEntityRole.Victim => RoleVictimColor,
            AdminLogEntityRole.Container => RoleContainerColor,
            AdminLogEntityRole.Subject => RoleSubjectColor,
            _ => RoleOtherColor
        };
    }

    private static FormattedMessage BuildMessage(SharedAdminLog log, bool showMetadata)
    {
        var message = new FormattedMessage();
        var isCondensed = BurstTypes.Contains(log.Type) || log.Message.StartsWith("[×");

        //Impact indicator
        var impactColor = GetImpactColor(log.Impact);
        message.PushColor(impactColor);
        var impactChar = log.Impact switch
        {
            LogImpact.Low => "·",
            LogImpact.Medium => "▪",
            LogImpact.High => "▸",
            LogImpact.Extreme => "▸",
            _ => "·"
        };
        message.AddText($"{impactChar} ");
        message.Pop();

        //Timestamp
        message.PushColor(TimestampColor);
        message.AddText($"{log.Date:HH:mm:ss} ");
        message.Pop();

        //Server name
        if (!string.IsNullOrEmpty(log.ServerName) && log.ServerName != "unknown")
        {
            message.PushColor(ServerNameColor);
            message.AddText($"[{log.ServerName}] ");
            message.Pop();
        }

        //Condensed badge
        if (isCondensed)
        {
            message.PushColor(CondensedBadgeColor);
            message.AddText("● ");
            message.Pop();
        }

        //Main message
        message.AddMarkupPermissive(log.Message);

        if (!showMetadata)
            return message;

        //Entity metadata
        if (log.Entities.Length > 0)
        {
            var sorted = (SharedAdminLogEntity[]) log.Entities.Clone();
            Array.Sort(sorted, static (a, b) => GetRoleSortKey(a.Role).CompareTo(GetRoleSortKey(b.Role)));

            message.AddText("\n");

            for (var i = 0; i < sorted.Length; i++)
            {
                var entity = sorted[i];
                var roleColor = GetRoleColor(entity.Role);
                var name = entity.EntityName ?? entity.PrototypeId ?? "?";

                message.AddText("    ");
                message.PushColor(roleColor);
                message.AddText(entity.Role.ToString());
                message.Pop();

                message.PushColor(MetadataValueColor);
                message.AddText($": {name}");

                var secondary = new List<string>(2);
                secondary.Add($"#{entity.EntityUid}");
                if (entity.PrototypeId != null && entity.EntityName != null)
                    secondary.Add(entity.PrototypeId);

                message.PushColor(MetadataLabelColor);
                message.AddText($" ({string.Join(", ", secondary)})");
                message.Pop();
                message.Pop();

                if (i < sorted.Length - 1)
                    message.AddText("\n");
            }
        }

        //Type / impact / server metadata line
        message.AddText("\n    ");
        message.PushColor(MetadataLabelColor);
        message.AddText("type ");
        message.Pop();
        message.PushColor(MetadataValueColor);
        message.AddText(log.Type.ToString());
        message.Pop();

        message.PushColor(MetadataLabelColor);
        message.AddText("  ·  impact ");
        message.Pop();
        message.PushColor(impactColor);
        message.AddText(log.Impact.ToString());
        message.Pop();

        if (!string.IsNullOrEmpty(log.ServerName) && log.ServerName != "unknown")
        {
            message.PushColor(MetadataLabelColor);
            message.AddText("  ·  server ");
            message.Pop();
            message.PushColor(ServerNameColor);
            message.AddText(log.ServerName);
            message.Pop();
        }

        return message;
    }


    // Builds the expandable section showing original sample messages from a condensed entry.
    private static FormattedMessage BuildSampleMessages(string[] sampleMessages)
    {
        var message = new FormattedMessage();

        for (var i = 0; i < sampleMessages.Length; i++)
        {
            if (i > 0)
                message.AddText("\n");

            message.PushColor(MetadataLabelColor);
            message.AddText("      ");
            message.Pop();
            message.PushColor(SampleMessageColor);
            message.AddText(sampleMessages[i]);
            message.Pop();
        }

        return message;
    }

    public void SetShowMetadata(bool showMetadata)
    {
        if (_showMetadata == showMetadata)
            return;

        _showMetadata = showMetadata;
        _messageLabel.SetMessage(BuildMessage(Log, _showMetadata));
    }

    private void VisibilityChanged(Control control)
    {
        Separator.Visible = Visible;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        OnVisibilityChanged -= VisibilityChanged;
    }
}
