using System.Linq;
using Content.Client.Gameplay;
using Content.Client.Guidebook;
using Content.Client.Guidebook.Controls;
using Content.Client.Lobby;
using Content.Client.Players.PlayTimeTracking;
using Content.Client.UserInterface.Systems.MenuBar.Widgets;
using Content.Shared.CCVar;
using Content.Shared.Guidebook;
using Content.Shared.Input;
using Robust.Client.State;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Configuration;
using Robust.Shared.Input.Binding;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client.UserInterface.Systems.Guidebook;

public sealed class GuidebookUIController : UIController, IOnStateChanged<LobbyState>, IOnStateChanged<GameplayState>, IOnSystemChanged<GuidebookSystem>
{
    [UISystemDependency] private readonly GuidebookSystem _guidebookSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IConfigurationManager _configuration = default!;
    [Dependency] private readonly JobRequirementsManager _jobRequirements = default!;

    private const int PlaytimeOpenGuidebook = 60;

    private GuidebookWindow? _window;

    private ProtoId<GuideEntryPrototype>? _lastEntry;

    public void OnStateEntered(LobbyState state)
    {
        HandleStateEntered(state);
    }

    public void OnStateEntered(GameplayState state)
    {
        HandleStateEntered(state);
    }

    private void HandleStateEntered(State state)
    {
        DebugTools.Assert(_window == null);

        // setup window
        _window = UIManager.CreateWindow<GuidebookWindow>();

        _window.OnClose += () =>
        {
            UIManager.ClickSound();
            _window.ReturnContainer.Visible = false;
            _lastEntry = _window.LastEntry;
        };

        if (state is LobbyState &&
            _jobRequirements.FetchOverallPlaytime() < TimeSpan.FromMinutes(PlaytimeOpenGuidebook))
        {
            OpenGuidebook();
            _window.RecenterWindow(new(0.5f, 0.5f));
            _window.SetPositionFirst();
        }

        // setup keybinding
        CommandBinds.Builder
            .Bind(ContentKeyFunctions.OpenGuidebook,
                InputCmdHandler.FromDelegate(_ => ToggleWindow()))
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
        _window = null;

        CommandBinds.Unregister<GuidebookUIController>();
    }

    public void OnSystemLoaded(GuidebookSystem system)
    {
        _guidebookSystem.OnGuidebookOpen += OpenGuidebook;
    }

    public void OnSystemUnloaded(GuidebookSystem system)
    {
        _guidebookSystem.OnGuidebookOpen -= OpenGuidebook;
    }

    public void ToggleWindow()
    {
        if (_window == null)
            return;

        if (_window.IsOpen)
            _window.Close();

        else
            OpenGuidebook();
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
    public void OpenGuidebook(
        Dictionary<ProtoId<GuideEntryPrototype>, GuideEntry>? guides = null,
        List<ProtoId<GuideEntryPrototype>>? rootEntries = null,
        ProtoId<GuideEntryPrototype>? forceRoot = null,
        bool includeChildren = true,
        ProtoId<GuideEntryPrototype>? selected = null)
    {
        if (_window == null)
            return;

        if (guides == null)
        {
            guides = _prototypeManager.EnumeratePrototypes<GuideEntryPrototype>()
                .ToDictionary(x => new ProtoId<GuideEntryPrototype>(x.ID), x => (GuideEntry) x);
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

        if (selected == null)
        {
            if (_lastEntry is { } lastEntry && guides.ContainsKey(lastEntry))
            {
                selected = _lastEntry;
            }
            else
            {
                selected = _configuration.GetCVar(CCVars.DefaultGuide);
            }
        }
        _window.UpdateGuides(guides, rootEntries, forceRoot, selected);

        // Expand up to depth-2.
        _window.Tree.SetAllExpanded(false);
        _window.Tree.SetAllExpanded(true, 1);

        _window.OpenCenteredRight();
    }

    public void OpenGuidebook(
        List<ProtoId<GuideEntryPrototype>> guideList,
        List<ProtoId<GuideEntryPrototype>>? rootEntries = null,
        ProtoId<GuideEntryPrototype>? forceRoot = null,
        bool includeChildren = true,
        ProtoId<GuideEntryPrototype>? selected = null)
    {
        Dictionary<ProtoId<GuideEntryPrototype>, GuideEntry> guides = new();
        foreach (var guideId in guideList)
        {
            if (!_prototypeManager.TryIndex(guideId, out var guide))
            {
                Logger.Error($"Encountered unknown guide prototype: {guideId}");
                continue;
            }
            guides.Add(guideId, guide);
        }

        OpenGuidebook(guides, rootEntries, forceRoot, includeChildren, selected);
    }

    private void RecursivelyAddChildren(GuideEntry guide, Dictionary<ProtoId<GuideEntryPrototype>, GuideEntry> guides)
    {
        foreach (var childId in guide.Children)
        {
            if (guides.ContainsKey(childId))
                continue;

            if (!_prototypeManager.TryIndex(childId, out var child))
            {
                Logger.Error($"Encountered unknown guide prototype: {childId} as a child of {guide.Id}. If the child is not a prototype, it must be directly provided.");
                continue;
            }

            guides.Add(childId, child);
            RecursivelyAddChildren(child, guides);
        }
    }
}
