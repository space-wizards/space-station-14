using System.Collections.Immutable;
using System.Linq;
using System.Numerics;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;

namespace Content.Client.Lobby.UI;

/// <summary>
/// A control that is to be used in conjunction with <see cref="DraggableJobIcon"/>. These serve as drop targets for the
/// <see cref="DraggableJobIcon"/>. This class handles hover and drop behavior, as well as ensuring that job icons
/// that are added remain sorted.
/// </summary>
public sealed class DraggableJobTarget : Control
{
    /// <summary>
    /// Style pseudoclass for when something droppable is being held over the control.
    /// </summary>
    public const string StylePseudoClassActive = "active";

    /// <summary>
    /// This will be the main "layout" box of the control, which contains the job container and label header
    /// </summary>
    private readonly BoxContainer _mainBox;

    /// <summary>
    /// This panel is what becomes visible when you are dragging an icon over the target
    /// </summary>
    private readonly PanelContainer _backgroundPanel;

    /// <summary>
    /// This is the main container that holds the job icons. This is a <see cref="GridContainer"/> unless
    /// <see cref="Priority"/> is "High", then it is a <see cref="BoxContainer"/>
    /// </summary>
    private Container? _jobIconContainer;

    /// <summary>
    /// This is used if <see cref="Priority"/> is "High". If this is not null and an icon is dropped into this target
    /// while there is already a icon here, it will first kick the occupying icon to the fallback target before
    /// placing the dropped icon in.
    /// If it is null, it will simply not handle the icon (and <see cref="DraggableJobIcon"/> will probably kick it back
    /// to where it was before the drag)
    /// </summary>
    private DraggableJobTarget? _fallbackTarget;

    /// <summary>
    /// The job priority that this drop target represents.
    /// </summary>
    public JobPriority Priority { get; set; }

    /// <summary>
    /// Since this has different behavior with high priority, this is a simple helper property to ask that question.
    /// </summary>
    private bool IsHighPriority => Priority == JobPriority.High;

    public DraggableJobTarget()
    {
        // Add the panel used to highlight the target when hovered
        _backgroundPanel = new PanelContainer();
        AddChild(_backgroundPanel);

        // Add the main content box
        _mainBox = new BoxContainer()
        {
            Margin = new Thickness(10, 0),
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            HorizontalExpand = true,
        };
        AddChild(_mainBox);
    }

    /// <summary>
    /// Set the fallback target for a high priority drop target.
    /// Generally this should be the medium priority drop target.
    /// </summary>
    /// <param name="target">fallback target to be set, should NOT be high priority</param>
    /// <exception cref="InvalidOperationException">
    /// Throws if <see cref="Priority"/> is not "High", as it wouldn't do anything.
    /// Throws if <paramref name="target"/> <see cref="Priority"/> is "High", as this is sussy baka and you could make
    /// some weird infinite loop that I'm too lazy to check for.
    /// </exception>
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

        // When this enters the tree, let's remake everything.
        // I wanted to put most of this in the constructor, but other xaml elements weren't initialized at that stage

        // Nuke everything in the main content box
        _mainBox.RemoveAllChildren();
        _jobIconContainer = null;

        // Make and add the text header
        var header = new Label
        {
            Text = Loc.GetString($"humanoid-profile-editor-job-priority-{Priority.ToString().ToLower()}-button"),
            HorizontalAlignment = HAlignment.Center,
            StyleClasses = { "LabelBig" },
            Margin = new Thickness(0, 6),
        };

        _mainBox.AddChild(header);

        // Make and add the control that will hold the job icons
        if (Priority != JobPriority.High)
        {
            _jobIconContainer = new GridContainer()
            {
                // Columns will be adjusted later anyway
                Columns = 5,
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
                // This is the size of one high priority icon
                // Just makes sure it doesn't change size when you take it out
                MinWidth = 64,
                MinHeight = 64,
            };
        }

        _mainBox.AddChild(_jobIconContainer);
    }

    /// <summary>
    /// Just to be safe and nuke everything in the main content box when it exits the tree
    /// </summary>
    protected override void ExitedTree()
    {
        base.ExitedTree();

        _mainBox.RemoveAllChildren();
        _jobIconContainer = null;
    }

    /// <summary>
    /// Nuke all the jobs in the job container, just like a collection clear
    /// </summary>
    public void ClearIcons()
    {
        _jobIconContainer?.DisposeAllChildren();
    }

    /// <summary>
    /// Register a <see cref="DraggableJobIcon"/>
    /// </summary>
    /// <param name="icon"></param>
    public void RegisterJobIcon(DraggableJobIcon icon)
    {
        icon.OnMouseMove += pos => HandleMouseMove(pos, icon);
        icon.OnMouseUp += args => HandleMouseUp(args, ref icon);
    }

    /// <summary>
    /// Add a job icon to this control. The icon will be reparented if it is already parented.
    /// </summary>
    /// <param name="icon">Job icon to be added and parented</param>
    /// We don't have to care about order here.
    /// * If the icons are being created by LobbyCharacterPreviewPanel.Refresh(), they are being created in order
    /// * If the icon has just been dragged here, then LobbyCharacterPreviewPanel.Refresh() is about to run and re-create
    ///   all the icons in order
    /// * If the icon is returning after being dragged to nowhere, it will remember its old position and move itself back
    public void AddJobIcon(DraggableJobIcon icon)
    {
        if (_jobIconContainer is null)
            return;

        if (IsHighPriority && _jobIconContainer?.ChildCount > 0)
        {
            if (_fallbackTarget is null)
                return;
            if (_jobIconContainer.Children.First() is not DraggableJobIcon toBump)
                return;
            _fallbackTarget.AddJobIcon(toBump);
        }

        icon.SetScale(Priority);
        icon.Orphan();
        _jobIconContainer?.AddChild(icon);
    }

    /// <summary>
    /// Helper function with a workaround for child control styles not updating. Once RT
    /// has https://github.com/space-wizards/RobustToolbox/pull/6264 or similar, this can
    /// be removed, and calls to it replaced with calls to AddStylePseudoClass or
    /// RemoveStylePseudoClass
    /// </summary>
    /// <param name="active"></param>
    private void SetActive(bool active)
    {
        if (HasStylePseudoClass(StylePseudoClassActive) == active)
            return;

        if (active)
            AddStylePseudoClass(StylePseudoClassActive);
        else
            RemoveStylePseudoClass(StylePseudoClassActive);

        _backgroundPanel.RemoveStyleClass("dummy");
    }

    /// <summary>
    /// Check if an icon is hovering above the target on a drag end and handle it if it is.
    /// </summary>
    private void HandleMouseUp(Vector2 pos, ref DraggableJobIcon icon)
    {
        if (!icon.Dragging || !GlobalRect.Contains(pos))
            return;

        AddJobIcon(icon);
        SetActive(false);
    }

    /// <summary>
    /// Check if an icon is hovering above the target and handle the feedback effects
    /// </summary>
    private void HandleMouseMove(Vector2 pos, DraggableJobIcon icon)
    {
        var contained = GlobalRect.Contains(pos);
        SetActive(contained);
        if(contained)
            icon.SetScale(Priority);
    }

    /// <summary>
    /// Get the jobs that are contained in this control.
    /// </summary>
    public IEnumerable<JobPrototype> GetContainedJobs()
    {
        if (_jobIconContainer is null)
            return [];

        return _jobIconContainer.Children.Cast<DraggableJobIcon>().Select(icon => icon.JobProto);
    }

    /// <summary>
    /// Get the number of jobs contained in this control.
    /// </summary>
    public int ContainedJobCount()
    {
        return GetContainedJobs().Count();
    }

    /// <summary>
    /// Set the column count of the GridContainer in this control
    /// </summary>
    public void SetColumns(int columns)
    {
        // If child count is less than requested columns, just set that instead or else there will be
        // little separators that make the icon not centered.
        // Also GridContainer will throw if you try to set 0 columns.
        if (_jobIconContainer is GridContainer grid)
            grid.Columns = grid.ChildCount == 0 ? 1 : Math.Min(columns, grid.ChildCount);
    }
}
