using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Utility;
using static Robust.Client.UserInterface.Controls.BaseButton;

namespace Content.Client.Administration.UI.AuditLogs;

public sealed partial class AdminAuditLogsControl : Control
{
    private static readonly Color RoutineColor = Color.White;
    private static readonly Color NotableColor = new(235, 200, 90);
    private static readonly Color CriticalColor = new(230, 90, 90);

    private int _currentRound;

    public SpinBox RoundSpinBox { get; }
    public LineEdit SearchLineEdit { get; }
    public Button SearchButton { get; }
    public Button RoutineButton { get; }
    public Button NotableButton { get; }
    public Button CriticalButton { get; }
    public BoxContainer LogsContainer { get; }
    public Button NextButton { get; }

    private Label ServerNameLabel { get; }
    private Label CountLabel { get; }

    public AdminAuditLogsControl()
    {
        RobustXamlLoader.Load(this);

        RoundSpinBox = FindControl<SpinBox>("RoundSpinBox");
        SearchLineEdit = FindControl<LineEdit>("SearchLineEdit");
        SearchButton = FindControl<Button>("SearchButton");
        RoutineButton = FindControl<Button>("RoutineButton");
        NotableButton = FindControl<Button>("NotableButton");
        CriticalButton = FindControl<Button>("CriticalButton");
        LogsContainer = FindControl<BoxContainer>("LogsContainer");
        NextButton = FindControl<Button>("NextButton");
        ServerNameLabel = FindControl<Label>("ServerNameLabel");
        CountLabel = FindControl<Label>("CountLabel");

        RoundSpinBox.IsValid = i => i > 0 && i <= _currentRound;
        RoundSpinBox.InitDefaultButtons();

        RoutineButton.OnPressed += SeverityButtonPressed;
        NotableButton.OnPressed += SeverityButtonPressed;
        CriticalButton.OnPressed += SeverityButtonPressed;

        SelectedSeverities.Add(AuditSeverity.Routine);
        SelectedSeverities.Add(AuditSeverity.Notable);
        SelectedSeverities.Add(AuditSeverity.Critical);
    }

    public int SelectedRoundId => RoundSpinBox.Value;
    public string Search => SearchLineEdit.Text.Trim();

    public HashSet<AuditSeverity> SelectedSeverities { get; } = new();

    public void SetCurrentRound(int roundId)
    {
        _currentRound = roundId;

        if (RoundSpinBox.Value <= 0)
            RoundSpinBox.Value = roundId;
    }

    public void SetServerName(string name)
    {
        ServerNameLabel.Text = name;
    }

    public void UpdateCount(int count)
    {
        CountLabel.Text = $"Total: {count}";
    }

    public void SetLogs(List<SharedAdminAuditLog> logs)
    {
        LogsContainer.RemoveAllChildren();
        AddLogs(logs);
    }

    public void AddLogs(List<SharedAdminAuditLog> logs)
    {
        foreach (var log in logs)
        {
            var label = new RichTextLabel
            {
                HorizontalExpand = true,
                Margin = new Thickness(2, 1, 2, 1)
            };

            label.SetMessage(FormatLog(log));
            LogsContainer.AddChild(label);
        }
    }

    private FormattedMessage FormatLog(SharedAdminAuditLog log)
    {
        var msg = new FormattedMessage();

        msg.AddText($"[{log.OccurredAt:yyyy-MM-dd HH:mm:ss}] ");

        msg.PushColor(GetSeverityColor(log.Severity));
        msg.AddText($"[{log.Severity}] ");
        msg.Pop();

        msg.AddText($"[{log.Action}] - ");
        msg.AddText(log.Message);

        return msg;
    }

    private static Color GetSeverityColor(AuditSeverity severity)
    {
        return severity switch
        {
            AuditSeverity.Critical => CriticalColor,
            AuditSeverity.Notable => NotableColor,
            _ => RoutineColor
        };
    }

    private void SeverityButtonPressed(ButtonEventArgs args)
    {
        UpdateSeveritySelection();
    }

    private void UpdateSeveritySelection()
    {
        SelectedSeverities.Clear();

        if (RoutineButton.Pressed)
            SelectedSeverities.Add(AuditSeverity.Routine);
        if (NotableButton.Pressed)
            SelectedSeverities.Add(AuditSeverity.Notable);
        if (CriticalButton.Pressed)
            SelectedSeverities.Add(AuditSeverity.Critical);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        RoutineButton.OnPressed -= SeverityButtonPressed;
        NotableButton.OnPressed -= SeverityButtonPressed;
        CriticalButton.OnPressed -= SeverityButtonPressed;

        RoundSpinBox.IsValid = null;
    }
}
