using System.Collections.Immutable;
using System.Linq;
using System.Numerics;
using Content.Client.Stylesheets;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;

namespace Content.Client.Lobby.UI;

public sealed class DraggableJobTarget : Control
{
    private static readonly List<JobPrototype> OrderedJobsInternal = new ();
    public static ImmutableList<JobPrototype> OrderedJobs => OrderedJobsInternal.ToImmutableList();

    private readonly BoxContainer _mainBox;
    private readonly PanelContainer? _backgroundPanel;
    private Container? _jobIconContainer;
    private DraggableJobTarget? _fallbackTarget;

    public JobPriority Priority { get; set; }

    private bool IsHighPriority => Priority == JobPriority.High;

    public DraggableJobTarget()
    {
        HorizontalExpand = true;

        _backgroundPanel = new PanelContainer()
        {
            Visible = false,
        };

        var back = new StyleBoxFlat
        {
            BackgroundColor = StyleNano.NanoGold,
        };

        _backgroundPanel.PanelOverride = back;

        AddChild(_backgroundPanel);

        _mainBox = new BoxContainer()
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            HorizontalExpand = true,
        };

        AddChild(_mainBox);
    }

    public void SetFallbackTarget(DraggableJobTarget target)
    {
        if (!IsHighPriority)
            throw new InvalidOperationException("Only the high priority job target can have a fallback set");

        if (target.IsHighPriority)
            throw new InvalidOperationException("The fallback target shouldn't also be high priority. this sus");

        _fallbackTarget = target;
    }

    protected override void EnteredTree()
    {
        base.EnteredTree();

        _mainBox.RemoveAllChildren();
        _jobIconContainer = null;

        var header = new Label
        {
            Text = Loc.GetString($"humanoid-profile-editor-job-priority-{Priority.ToString().ToLower()}-button"),
            HorizontalAlignment = HAlignment.Center,
            StyleClasses = { "LabelBig" },
            Margin = new Thickness(0, 6),
        };

        _mainBox.AddChild(header);

        if (Priority != JobPriority.High)
        {
            _jobIconContainer = new GridContainer()
            {
                MaxGridWidth = 140,
                HorizontalAlignment = HAlignment.Center,
            };
        }
        else
        {
            _jobIconContainer = new BoxContainer()
            {
                Name = "HighBox",
                HorizontalAlignment = HAlignment.Center,
                VerticalAlignment = VAlignment.Center,
            };
        }

        _mainBox.AddChild(_jobIconContainer);
    }

    protected override void ExitedTree()
    {
        base.ExitedTree();

        RemoveAllChildren();
        _jobIconContainer = null;
    }

    public void ClearIcons()
    {
        _jobIconContainer?.DisposeAllChildren();
    }

    public void RegisterJobIcon(DraggableJobIcon icon)
    {
        icon.OnMouseMove += pos => HandleMouseMove(pos, icon);
        icon.OnMouseUp += args => HandleMouseUp(args, ref icon);
    }

    public void AddJobIcon(DraggableJobIcon icon, bool preOrdered = false)
    {
        if (IsHighPriority && _jobIconContainer?.ChildCount > 0)
        {
            if (_fallbackTarget is null)
                return;
            if (_jobIconContainer.Children.First() is not DraggableJobIcon toBump)
                return;
            _fallbackTarget.AddJobIcon(toBump);
        }

        icon.SetScale(Priority);
        var insertIndex = preOrdered ? -1 : FindInsertLocation(icon);
        icon.Orphan();
        _jobIconContainer?.AddChild(icon);
        if(insertIndex >= 0)
            icon.SetPositionInParent(insertIndex);
    }

    private int FindInsertLocation(DraggableJobIcon icon)
    {
        if (IsHighPriority)
            return -1;

        var thisIndex = OrderedJobs.IndexOf(icon.JobProto);

        var insertAt = _jobIconContainer?.Children.Cast<DraggableJobIcon>()
            .ToImmutableList()
            .FindIndex(curIcon => OrderedJobs.IndexOf(curIcon.JobProto) > thisIndex);

        return insertAt ?? -1;
    }

    private void HandleMouseUp(Vector2 pos, ref DraggableJobIcon icon)
    {
        if (!icon.Dragging || !GlobalRect.Contains(pos))
            return;

        AddJobIcon(icon);
        if (_backgroundPanel is not null)
            _backgroundPanel.Visible = false;
        // Modulate = Color.White;
    }

    private void HandleMouseMove(Vector2 pos, DraggableJobIcon icon)
    {
        var contained = GlobalRect.Contains(pos);
        // Modulate = contained ? Color.Blue : Color.White;
        if (_backgroundPanel is not null)
            _backgroundPanel.Visible = contained;
        if(contained)
            icon.SetScale(Priority);
    }

    public static void UpdatedOrderedJobs(IPrototypeManager protoMan)
    {
        OrderedJobsInternal.Clear();

        // Get and sort departments
        var departments = protoMan.EnumeratePrototypes<DepartmentPrototype>().ToList();
        departments.Sort(DepartmentUIComparer.Instance);
        foreach (var department in departments)
        {
            // Get and sort jobs in department
            var jobs = department.Roles.Select(jobId => protoMan.Index(jobId)).ToList();
            jobs.Sort(JobUIComparer.Instance);
            foreach (var job in jobs)
            {
                if (!OrderedJobsInternal.Contains(job))
                    OrderedJobsInternal.Add(job);
            }
        }
    }
}
