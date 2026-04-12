using Robust.Client.Graphics;
using Robust.Client.State;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.ContentPack;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace MapEditor.RTBridge;

/// <summary>
///     Minimal Robust.Client state for the embedded editor viewport.
///     Attaches an <see cref="EditorViewportControl"/> that fills the state
///     root and renders whatever the current eye is looking at, plus
///     handles camera pan and zoom. Has no other UI. The editor's panels,
///     menus, and toolbars all live in the WPF host layer around the RT
///     viewport.
/// </summary>
[ContentAccessAllowed]
public sealed class EditorViewportState : State
{
    [Dependency] private readonly IUserInterfaceManager _ui = default!;
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;

    private EditorViewportControl? _viewport;

    protected override void Startup()
    {
        // EditorBootstrap publishes EditorContext.Current before switching
        // to this state, so .Current and its EditorEye are guaranteed to
        // be non-null here. If that ever breaks, we want a loud crash, not
        // a silent fallback eye that is not the one being rendered.
        var context = EditorContext.Current
            ?? throw new System.InvalidOperationException(
                "EditorViewportState.Startup ran before EditorContext was published. " +
                "Check ordering in EditorBootstrap.OnRtInitialized.");

        _viewport = new EditorViewportControl(_eyeManager, _entityManager, context.EditorEye)
        {
            HorizontalExpand = true,
            VerticalExpand = true,
        };

        LayoutContainer.SetAnchorPreset(_viewport, LayoutContainer.LayoutPreset.Wide);
        _ui.StateRoot.AddChild(_viewport);
    }

    protected override void Shutdown()
    {
        _viewport?.Orphan();
        _viewport = null;
    }
}
