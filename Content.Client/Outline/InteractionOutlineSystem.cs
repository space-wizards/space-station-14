using Content.Client.ContextMenu.UI;
using Content.Client.Gameplay;
using Content.Client.Interactable.Components;
using Content.Client.Viewport;
using Content.Shared.CCVar;
using Content.Shared.Interaction;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Client.State;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;

namespace Content.Client.Outline;

public sealed partial class InteractionOutlineSystem : EntitySystem
{
    [Dependency] private IConfigurationManager _configManager = default!;
    [Dependency] private IEyeManager _eyeManager = default!;
    [Dependency] private IInputManager _inputManager = default!;
    [Dependency] private IPlayerManager _playerManager = default!;
    [Dependency] private IStateManager _stateManager = default!;
    [Dependency] private IUserInterfaceManager _uiManager = default!;
    [Dependency] private IPrototypeManager _prototypeManager = default!;
    [Dependency] private SharedInteractionSystem _interactionSystem = default!;

    [Dependency] private EntityQuery<InteractionOutlineComponent> _outlineQuery = default!;
    [Dependency] private EntityQuery<SpriteComponent> _spriteQuery = default!;

    /// <summary>
    ///     Whether to currently draw the outline. The outline may be temporarily disabled by other systems
    /// </summary>
    private bool _enabled = true;

    /// <summary>
    ///     Whether to draw the outline at all. Overrides <see cref="_enabled"/>.
    /// </summary>
    private bool _cvarEnabled = true;

    private const float OutlineWidth = 1;

    private static readonly ProtoId<ShaderPrototype> ShaderInRange = "SelectionOutlineInrange";
    private static readonly ProtoId<ShaderPrototype> ShaderOutOfRange = "SelectionOutline";

    private ShaderInstance? _shaderInRange;
    private ShaderInstance? _shaderOutOfRange;

    private EntityUid? _lastHoveredEntity;

    public override void Initialize()
    {
        base.Initialize();

        _shaderInRange = _prototypeManager.Index(ShaderInRange).InstanceUnique();
        _shaderOutOfRange = _prototypeManager.Index(ShaderOutOfRange).InstanceUnique();

        Subs.CVar(_configManager, CCVars.OutlineEnabled, SetCvarEnabled);
        UpdatesAfter.Add(typeof(SharedEyeSystem));
    }

    public void SetCvarEnabled(bool cvarEnabled)
    {
        _cvarEnabled = cvarEnabled;

        // clear last hover if required:

        if (_cvarEnabled)
            return;

        if (!_outlineQuery.HasComp(_lastHoveredEntity))
            return;

        if (!_spriteQuery.TryComp(_lastHoveredEntity, out var sprite))
            return;

        sprite.PostShader = null;
    }

    public void SetEnabled(bool enabled)
    {
        if (enabled == _enabled)
            return;

        _enabled = enabled;

        // clear last hover if required:

        if (enabled)
            return;

        if (!_outlineQuery.HasComp(_lastHoveredEntity))
            return;

        if (!_spriteQuery.TryComp(_lastHoveredEntity, out var sprite))
            return;

        sprite.PostShader = null;
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

        if (_lastHoveredEntity != entityToClick && _outlineQuery.HasComp(_lastHoveredEntity) && _spriteQuery.TryComp(_lastHoveredEntity, out var lastSprite))
            lastSprite.PostShader = null;

        _lastHoveredEntity = entityToClick;

        if (!_outlineQuery.HasComp(entityToClick))
            return;

        if (!_spriteQuery.TryComp(entityToClick, out var sprite))
            return;

        var inRange = false;
        if (localSession.AttachedEntity != null && !Deleted(entityToClick))
            inRange = _interactionSystem.InRangeUnobstructed(localSession.AttachedEntity.Value, entityToClick.Value);

        var shader = sprite.PostShader = inRange ? _shaderInRange : _shaderOutOfRange;
        shader?.SetParameter("outline_width", OutlineWidth * renderScale);
    }
}
