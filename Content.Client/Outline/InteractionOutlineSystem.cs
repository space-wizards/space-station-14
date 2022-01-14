using Content.Client.ContextMenu.UI;
using Content.Client.Interactable;
using Content.Client.Interactable.Components;
using Content.Client.Viewport;
using Content.Shared.CCVar;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Client.State;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Client.Outline;

public sealed class InteractionOutlineSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _configManager = default!;
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly IInputManager _inputManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IStateManager _stateManager = default!;
    [Dependency] private readonly IUserInterfaceManager _uiManager = default!;

    public bool Enabled = true;

    private EntityUid? _lastHoveredEntity;

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        // If there is no local player, there is no session, and therefore nothing to do here.
        var localPlayer = _playerManager.LocalPlayer;
        if (localPlayer == null)
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

        if (currentState is not GameScreen screen) return;

        EntityUid? entityToClick = null;
        var renderScale = 1;
        if (_uiManager.CurrentlyHovered is IViewportControl vp)
        {
            var mousePosWorld = vp.ScreenToMap(_inputManager.MouseScreenPosition.Position);
            entityToClick = screen.GetEntityUnderPosition(mousePosWorld);

            if (vp is ScalingViewport svp)
            {
                renderScale = svp.CurrentRenderScale;
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
        if (localPlayer.ControlledEntity != null && entityToClick != null)
        {
            inRange = localPlayer.InRangeUnobstructed(entityToClick.Value, ignoreInsideBlocker: true);
        }

        InteractionOutlineComponent? outline;

        if (!Enabled || !_configManager.GetCVar(CCVars.OutlineEnabled))
        {
            if (entityToClick != null && TryComp(entityToClick, out outline))
            {
                outline.OnMouseLeave(); //Prevent outline remains from persisting post command.
            }

            return;
        }

        if (entityToClick == _lastHoveredEntity)
        {
            if (entityToClick != null && TryComp(entityToClick, out outline))
            {
                outline.UpdateInRange(inRange, renderScale);
            }

            return;
        }

        if (_lastHoveredEntity != null && !Deleted(_lastHoveredEntity) &&
            TryComp(_lastHoveredEntity, out outline))
        {
            outline.OnMouseLeave();
        }

        _lastHoveredEntity = entityToClick;

        if (_lastHoveredEntity != null && TryComp(_lastHoveredEntity, out outline))
        {
            outline.OnMouseEnter(inRange, renderScale);
        }
    }
}
