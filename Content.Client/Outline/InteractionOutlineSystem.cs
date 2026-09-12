using System.Numerics;
using Content.Client.ContextMenu.UI;
using Content.Client.Gameplay;
using Content.Client.Graphics;
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
using Robust.Shared.Graphics;
using Robust.Shared.Prototypes;

namespace Content.Client.Outline;

/// <summary>
/// Gives moused-over entities an interaction outline.
/// </summary>
public sealed partial class InteractionOutlineSystem : EntitySystem
{
    [Dependency] private IConfigurationManager _configManager = default!;
    [Dependency] private IEyeManager _eyeManager = default!;
    [Dependency] private IInputManager _inputManager = default!;
    [Dependency] private IPlayerManager _playerManager = default!;
    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private SpriteSystem _sprite = default!;
    [Dependency] private IStateManager _stateManager = default!;
    [Dependency] private IUserInterfaceManager _uiManager = default!;
    [Dependency] private SharedInteractionSystem _interactionSystem = default!;

    [Dependency] private EntityQuery<InteractionOutlineComponent> _outlineQuery;
    [Dependency] private EntityQuery<SpriteComponent> _spriteQuery;

    private static readonly ProtoId<ShaderPrototype> ShaderInRange = "SelectionOutlineInrange";
    private static readonly ProtoId<ShaderPrototype> ShaderOutOfRange = "SelectionOutline";

    private ShaderInstance? _shaderInRange;
    private ShaderInstance? _shaderOutOfRange;

    private const float DesiredOutlineThickness = 1f;

    /// <summary>
    ///     Whether to currently draw the outline. The outline may be temporarily disabled by other systems
    /// </summary>
    private bool _enabled = true;

    /// <summary>
    ///     Whether to draw the outline at all. Overrides <see cref="_enabled"/>.
    /// </summary>
    private bool _cvarEnabled = true;

    private EntityUid? _lastHoveredEntity;

    public override void Shutdown()
    {
        _shaderInRange?.Dispose();
        _shaderOutOfRange?.Dispose();
        base.Shutdown();
    }

    public override void Initialize()
    {
        base.Initialize();

        Subs.CVar(_configManager, CCVars.OutlineEnabled, SetCvarEnabled);
        UpdatesAfter.Add(typeof(SharedEyeSystem));
    }

    [SubscribeLocalEvent]
    private void OnShutdown(Entity<InteractionOutlineComponent> ent, ref ComponentShutdown args)
    {
        RemoveOutline(ent);

        if (_lastHoveredEntity == ent.Owner)
            _lastHoveredEntity = null;
    }

    public void SetCvarEnabled(bool cvarEnabled)
    {
        _cvarEnabled = cvarEnabled;

        // clear last hover if required:

        if (_cvarEnabled)
            return;

        if (_lastHoveredEntity == null || Deleted(_lastHoveredEntity))
            return;

        if (_outlineQuery.TryComp(_lastHoveredEntity, out var outline))
            RemoveOutline((_lastHoveredEntity.Value, outline));
    }

    public void SetEnabled(bool enabled)
    {
        if (enabled == _enabled)
            return;

        _enabled = enabled;

        if (enabled)
            return;

        if (_lastHoveredEntity == null || Deleted(_lastHoveredEntity))
        {
            _lastHoveredEntity = null;
            return;
        }

        if (_outlineQuery.TryComp(_lastHoveredEntity, out var outline))
            RemoveOutline((_lastHoveredEntity.Value, outline));
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        if (!_enabled || !_cvarEnabled)
        {
            ClearOutline();
            return;
        }

        // If there is no local player, there is no session, and therefore nothing to do here.
        var localSession = _playerManager.LocalSession;
        if (localSession == null)
        {
            ClearOutline();
            return;
        }

        // GameScreen is still in charge of what entities are visible under a specific cursor position.
        // Potentially change someday? who knows.
        var currentState = _stateManager.CurrentState;

        if (currentState is not GameplayStateBase screen)
        {
            ClearOutline();
            return;
        }

        EntityUid? entityToClick = null;

        if (_uiManager.CurrentlyHovered is IViewportControl vp
            && _inputManager.MouseScreenPosition.IsValid)
        {
            var mousePosWorld = vp.PixelToMap(_inputManager.MouseScreenPosition.Position);

            if (vp is ScalingViewport svp)
            {
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
        }

        var inRange = false;
        if (localSession.AttachedEntity != null && !Deleted(entityToClick))
            inRange = _interactionSystem.InRangeUnobstructed(localSession.AttachedEntity.Value, entityToClick.Value);

        InteractionOutlineComponent? outline;

        if (entityToClick == _lastHoveredEntity)
        {
            if (entityToClick != null && _outlineQuery.TryComp(entityToClick, out outline))
            {
                UpdateOutline((entityToClick.Value, outline), inRange);
            }
            else
            {
                ClearOutline();
            }

            return;
        }

        if (_lastHoveredEntity != null &&
            !Deleted(_lastHoveredEntity) &&
            _outlineQuery.TryComp(_lastHoveredEntity, out outline))
        {
            RemoveOutline((_lastHoveredEntity.Value, outline));
        }
        else
        {
            ClearOutline();
        }

        _lastHoveredEntity = entityToClick;

        if (_lastHoveredEntity != null && _outlineQuery.TryComp(_lastHoveredEntity, out outline))
        {
            AddOutline((_lastHoveredEntity.Value, outline), inRange);
        }
    }

    private void AddOutline(Entity<InteractionOutlineComponent> ent, bool inInteractionRange)
    {
        SetPostShader(ent.Owner, inInteractionRange);
    }

    private void RemoveOutline(Entity<InteractionOutlineComponent> ent)
    {
        if (_spriteQuery.TryComp(ent.Owner, out var sprite))
            _sprite.RemovePostShader((ent.Owner, sprite), ContentPostShaderIds.InteractionOutline);
    }

    private void UpdateOutline(Entity<InteractionOutlineComponent> ent, bool inInteractionRange)
    {
        SetPostShader(ent.Owner, inInteractionRange);
    }

    private void ClearOutline()
    {
        if (_lastHoveredEntity != null &&
            !Deleted(_lastHoveredEntity) &&
            _spriteQuery.TryComp(_lastHoveredEntity, out var sprite))
        {
            _sprite.RemovePostShader((_lastHoveredEntity.Value, sprite), ContentPostShaderIds.InteractionOutline);
        }
    }

    private void SetPostShader(EntityUid uid, bool inInteractionRange)
    {
        if (!_spriteQuery.TryComp(uid, out var sprite))
            return;

        var shader = GetShader(inInteractionRange);

        if (_sprite.TryGetPostShader(sprite, ContentPostShaderIds.InteractionOutline, out var entry) &&
            entry.Shader == shader)
        {
            return;
        }

        _sprite.SetPostShader((uid, sprite), new SpriteComponent.PostShaderArgs(ContentPostShaderIds.InteractionOutline, shader)
        {
            After = ContentPostShaderIds.AfterBaseEffects,
            RaiseShaderEvent = true,
        });
    }

    private ShaderInstance GetShader(bool inRange)
    {
        var shader = inRange
            ? _shaderInRange ??= _prototype.Index(ShaderInRange).InstanceUnique()
            : _shaderOutOfRange ??= _prototype.Index(ShaderOutOfRange).InstanceUnique();

        shader.SetParameter("outline_width", DesiredOutlineThickness);
        return shader;
    }

    [SubscribeLocalEvent]
    private void OnBeforePostShaderRender(Entity<InteractionOutlineComponent> ent, ref BeforePostShaderRenderEvent args)
    {
        if (args.Id != ContentPostShaderIds.InteractionOutline)
            return;

        var zoom = args.Viewport.Eye?.Zoom ?? Vector2.Zero;
        var zoomLength = zoom.Length();

        if (zoomLength < float.Epsilon)
            return;

        args.Shader.SetParameter("outline_width", DesiredOutlineThickness * args.Viewport.RenderScale.Length() / zoomLength);
    }
}
