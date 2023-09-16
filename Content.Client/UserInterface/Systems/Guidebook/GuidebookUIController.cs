using System.Linq;
using Content.Client.Gameplay;
using Content.Client.Guidebook;
using Content.Client.Guidebook.Controls;
using Content.Client.Lobby;
using Content.Client.UserInterface.Controls;
using Content.Shared.Input;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using static Robust.Client.UserInterface.Controls.BaseButton;
using Robust.Shared.Input.Binding;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client.UserInterface.Systems.Guidebook;

public sealed class GuidebookUIController : UIController, IOnStateEntered<LobbyState>, IOnStateEntered<GameplayState>, IOnStateExited<LobbyState>, IOnStateExited<GameplayState>, IOnSystemChanged<GuidebookSystem>
{
    [UISystemDependency] private readonly GuidebookSystem _guidebookSystem = default!;
    [Dependency] private readonly IClyde _clyde = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IUserInterfaceManager _uiManager = default!;

    private IClydeWindow? ClydeWindow { get; set; }

    private GuidebookWindow? _guideWindow;
    private MenuButton? GuidebookButton => UIManager.GetActiveUIWidgetOrNull<MenuBar.Widgets.GameTopMenuBar>()?.GuidebookButton;

    public void OnStateEntered(LobbyState state)
    {
        HandleStateEntered();
    }

    public void OnStateEntered(GameplayState state)
    {
        HandleStateEntered();
    }

    private void HandleStateEntered()
    {
        DebugTools.Assert(_guideWindow == null);

        // setup window
        _guideWindow = UIManager.CreateWindow<GuidebookWindow>();
        _guideWindow.OnClose += OnWindowClosed;
        _guideWindow.OnOpen += OnWindowOpen;
        _guideWindow.Guidebook.PopOutButton.OnPressed += _ => PopOut();

        // setup keybinding
        CommandBinds.Builder
            .Bind(ContentKeyFunctions.OpenGuidebook,
                InputCmdHandler.FromDelegate(_ => ToggleGuidebook()))
            .Register<GuidebookUIController>();
    }

    public void OnStateExited(LobbyState state)
    {
        HandleStateExited();
    }

    public void OnStateExited(GameplayState state)
    {
        HandleStateExited();
    }

    private void HandleStateExited()
    {
        if (_guideWindow == null)
            return;

        _guideWindow.OnClose -= OnWindowClosed;
        _guideWindow.OnOpen -= OnWindowOpen;

        // shutdown
        _guideWindow.Dispose();
        _guideWindow = null;
        CommandBinds.Unregister<GuidebookUIController>();
    }

    public void OnSystemLoaded(GuidebookSystem system)
    {
        _guidebookSystem.OnGuidebookOpen += ToggleGuidebook;
    }

    public void OnSystemUnloaded(GuidebookSystem system)
    {
        _guidebookSystem.OnGuidebookOpen -= ToggleGuidebook;
    }

    internal void UnloadButton()
    {
        if (GuidebookButton == null)
            return;

        GuidebookButton.OnPressed -= GuidebookButtonOnPressed;
    }

    internal void LoadButton()
    {
        if (GuidebookButton == null)
            return;

        GuidebookButton.OnPressed += GuidebookButtonOnPressed;
    }

    private void GuidebookButtonOnPressed(ButtonEventArgs obj)
    {
        ToggleGuidebook();
    }

    private void OnWindowClosed()
    {
        if (GuidebookButton != null)
            GuidebookButton.Pressed = false;
    }

    private void OnWindowOpen()
    {
        if (GuidebookButton != null)
            GuidebookButton.Pressed = true;
    }

    /// <summary>
    ///     Opens or closes the guidebook.
    /// </summary>
    /// <param name="guides">What guides should be shown. If not specified, this will instead list all the entries</param>
    /// <param name="rootEntries">A list of guides that should form the base of the table of contents. If not specified,
    /// this will automatically simply be a list of all guides that have no parent.</param>
    /// <param name="forceRoot">This forces a singular guide to contain all other guides. This guide will
    /// contain its own children, in addition to what would normally be the root guides if this were not
    /// specified.</param>
    /// <param name="includeChildren">Whether or not to automatically include child entries. If false, this will ONLY
    /// show the specified entries</param>
    /// <param name="selected">The guide whose contents should be displayed when the guidebook is opened</param>
    public void ToggleGuidebook(
        Dictionary<string, GuideEntry>? guides = null,
        List<string>? rootEntries = null,
        string? forceRoot = null,
        bool includeChildren = true,
        string? selected = null)
    {
        if (_guideWindow == null)
        {
            _guideWindow = UIManager.CreateWindow<GuidebookWindow>();
            _guideWindow.OnClose += OnWindowClosed;
            _guideWindow.OnOpen += OnWindowOpen;
            _guideWindow.Guidebook.PopOutButton.OnPressed += _ => PopOut();
        }

        if (_guideWindow.IsOpen)
        {
            _guideWindow.Close();
            return;
        }

        if (GuidebookButton != null)
            GuidebookButton.Pressed = !_guideWindow.IsOpen;

        if (guides == null)
        {
            guides = _prototypeManager.EnumeratePrototypes<GuideEntryPrototype>()
                .ToDictionary(x => x.ID, x => (GuideEntry) x);
        }
        else if (includeChildren)
        {
            var oldGuides = guides;
            guides = new(oldGuides);
            foreach (var guide in oldGuides.Values)
            {
                RecursivelyAddChildren(guide, guides);
            }
        }

        _guideWindow.Guidebook.UpdateGuides(guides, rootEntries, forceRoot, selected);

        // Expand up to depth-2.
        _guideWindow.Guidebook.Tree.SetAllExpanded(false);
        _guideWindow.Guidebook.Tree.SetAllExpanded(true, 1);

        _guideWindow.OpenCenteredRight();
    }

    public void ToggleGuidebook(
        List<string> guideList,
        List<string>? rootEntries = null,
        string? forceRoot = null,
        bool includeChildren = true,
        string? selected = null)
    {
        Dictionary<string, GuideEntry> guides = new();
        foreach (var guideId in guideList)
        {
            if (!_prototypeManager.TryIndex<GuideEntryPrototype>(guideId, out var guide))
            {
                Logger.Error($"Encountered unknown guide prototype: {guideId}");
                continue;
            }
            guides.Add(guideId, guide);
        }

        ToggleGuidebook(guides, rootEntries, forceRoot, includeChildren, selected);
    }

    private void RecursivelyAddChildren(GuideEntry guide, Dictionary<string, GuideEntry> guides)
    {
        foreach (var childId in guide.Children)
        {
            if (guides.ContainsKey(childId))
                continue;

            if (!_prototypeManager.TryIndex<GuideEntryPrototype>(childId, out var child))
            {
                Logger.Error($"Encountered unknown guide prototype: {childId} as a child of {guide.Id}. If the child is not a prototype, it must be directly provided.");
                continue;
            }

            guides.Add(childId, child);
            RecursivelyAddChildren(child, guides);
        }
    }
    private void PopOut()
    {
        if (_guideWindow == null)
        {
            return;
        }

        var monitor = _clyde.EnumerateMonitors().First();

        ClydeWindow = _clyde.CreateWindow(new WindowCreateParameters
        {
            Maximized = false,
            Title = "Guidebook",
            Monitor = monitor,
            Width = 750,
            Height = 700
        });
        var control = _guideWindow.Guidebook;
        control.Orphan();
        _guideWindow.Dispose();
        _guideWindow = null;

        ClydeWindow.RequestClosed += OnRequestClosed;

        ClydeWindow.DisposeOnClose = true;
        var Root = _uiManager.CreateWindowRoot(ClydeWindow);
        Root.AddChild(control);

        control.PopOutButton.Disabled = true;
        control.PopOutButton.Visible = false;
    }

    private void OnRequestClosed(WindowRequestClosedEventArgs obj)
    {
        ClydeWindow = null;
        _guideWindow = null;
        if (GuidebookButton != null)
            GuidebookButton.Pressed = false;
    }
}
