using Content.Client.ContextMenu.UI;
using Content.Client.Gameplay;
using Content.Client.Interactable.Components;
using Content.Client.Viewport;
using Content.Shared.CCVar;
using Content.Shared.Interaction;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Client.State;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Configuration;

namespace Content.Client.Outline;

public sealed class InteractionOutlineSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _configManager = default!;
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly IInputManager _inputManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IStateManager _stateManager = default!;
    [Dependency] private readonly IUserInterfaceManager _uiManager = default!;
    [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;

    /// <summary>
    ///     Whether to currently draw the outline. The outline may be temporarily disabled by other systems
    /// </summary>
    private bool _enabled = true;

    /// <summary>
    ///     Whether to draw the outline at all. Overrides <see cref="_enabled"/>.
    /// </summary>
    private bool _cvarEnabled = true;

    private EntityUid? _lastHoveredEntity;

    public override void Initialize()
    {
        base.Initialize();

        Subs.CVar(_configManager, CCVars.OutlineEnabled, SetCvarEnabled);
        UpdatesAfter.Add(typeof(SharedEyeSystem));
    }

    public void SetCvarEnabled(bool cvarEnabled)
    {
        _cvarEnabled = cvarEnabled;

        // clear last hover if required:

        if (_cvarEnabled)
            return;

        if (_lastHoveredEntity == null || Deleted(_lastHoveredEntity))
            return;

        if (TryComp(_lastHoveredEntity, out InteractionOutlineComponent? outline))
            outline.OnMouseLeave(_lastHoveredEntity.Value);
    }

    public void SetEnabled(bool enabled)
    {
        if (enabled == _enabled)
            return;

        _enabled = enabled;

        // clear last hover if required:

        if (enabled)
            return;

        if (_lastHoveredEntity == null || Deleted(_lastHoveredEntity))
            return;

        if (TryComp(_lastHoveredEntity, out InteractionOutlineComponent? outline))
            outline.OnMouseLeave(_lastHoveredEntity.Value);
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        if (!_enabled || !_cvarEnabled)
            return;

        // If there is no local player, there is no session, and therefore nothing to do here.
        var localSession = _playerManager.LocalSession;
        if (localSession == null)
            return;

        // TODO InteractionOutlineComponent
        // BUG: The logic that gets the renderScale here assumes that the entity is only visible in a single
        // viewport. The entity will be highlighted in ALL viewport where it is visible, regardless of which
        // viewport is being used to hover over it. If these Viewports have very different render scales, this may
        // lead to extremely thick outlines in the other viewports. Fixing this probably requires changing how the
        // hover outline works, so that it only highlights the entity in a single viewport.

        // GameScreen is still in charge of what entities are visible under a specific cursor position.
        // Potentially change someday? who knows.
        var currentState = _stateManager.CurrentState;

        if (currentState is not GameplayStateBase screen) return;

        EntityUid? entityToClick = null;
        var renderScale = 1;
        if (_uiManager.CurrentlyHovered is IViewportControl vp
            && _inputManager.MouseScreenPosition.IsValid)
        {
            var mousePosWorld = vp.PixelToMap(_inputManager.MouseScreenPosition.Position);

            if (vp is ScalingViewport svp)
            {
                renderScale = svp.CurrentRenderScale;
                entityToClick = screen.GetClickedEntity(mousePosWorld, svp.Eye);
            }
            else
            {
                entityToClick = screen.GetClickedEntity(mousePosWorld);
            }
        }
        else if (_uiManager.CurrentlyHovered is EntityMenuElement element)
        {
            entityToClick = element.Entity;
            // TODO InteractionOutlineComponent
            // Currently we just take the renderscale from the main viewport. In the future, when the bug mentioned
            // above is fixed, the viewport should probably be the one that was clicked on to open the entity menu
            // in the first place.
            renderScale = _eyeManager.MainViewport.GetRenderScale();
        }

        var inRange = false;
        if (localSession.AttachedEntity != null && !Deleted(entityToClick))
        {
            inRange = _interactionSystem.InRangeUnobstructed(localSession.AttachedEntity.Value, entityToClick.Value);
        }

        InteractionOutlineComponent? outline;

        if (entityToClick == _lastHoveredEntity)
        {
            if (entityToClick != null && TryComp(entityToClick, out outline))
            {
                outline.UpdateInRange(entityToClick.Value, inRange, renderScale);
            }

            return;
        }

        if (_lastHoveredEntity != null && !Deleted(_lastHoveredEntity) &&
            TryComp(_lastHoveredEntity, out outline))
        {
            outline.OnMouseLeave(_lastHoveredEntity.Value);
        }

        _lastHoveredEntity = entityToClick;

        if (_lastHoveredEntity != null && TryComp(_lastHoveredEntity, out outline))
        {
            outline.OnMouseEnter(_lastHoveredEntity.Value, inRange, renderScale);
        }
    }
}
