using Content.Shared.Interaction;
using Content.Shared.Whitelist;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client.Outline;

/// <summary>
///     System used to indicate whether an entity is a valid target based on some criteria.
/// </summary>
public sealed class TargetOutlineSystem : EntitySystem
{
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly IInputManager _inputManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;

    private bool _enabled = false;

    /// <summary>
    ///     Whitelist that the target must satisfy.
    /// </summary>
    public EntityWhitelist? Whitelist = null;

    /// <summary>
    ///     Predicate the target must satisfy.
    /// </summary>
    public Func<EntityUid, bool>? Predicate = null;

    /// <summary>
    ///     Event to raise as targets to check whether they are valid.
    /// </summary>
    /// <remarks>
    ///     This event will be uncanceled and re-used.
    /// </remarks>
    public CancellableEntityEventArgs? ValidationEvent = null;

    /// <summary>
    ///     Minimum range for a target to be valid.
    /// </summary>
    /// <remarks>
    ///     If a target is further than this distance, they will still be highlighted in a different color.
    /// </remarks>
    public float Range = -1;

    /// <summary>
    ///     Whether to check if the player is unobstructed to the target;
    /// </summary>
    public bool CheckObstruction = true;

    /// <summary>
    ///     The size of the box around the mouse to use when looking for valid targets.
    /// </summary>
    public float LookupSize = 2;

    private const string ShaderTargetValid = "SelectionOutlineInrange";
    private const string ShaderTargetInvalid = "SelectionOutline";
    private ShaderInstance? _shaderTargetValid;
    private ShaderInstance? _shaderTargetInvalid;

    private readonly HashSet<SpriteComponent> _highlightedSprites = new();

    public override void Initialize()
    {
        base.Initialize();

        _shaderTargetValid = _prototypeManager.Index<ShaderPrototype>(ShaderTargetValid).InstanceUnique();
        _shaderTargetInvalid = _prototypeManager.Index<ShaderPrototype>(ShaderTargetInvalid).InstanceUnique();
    }

    public void Disable()
    {
        if (_enabled == false)
            return;

        _enabled = false;
        RemoveHighlights();
    }

    public void Enable(float range, bool checkObstructions, Func<EntityUid, bool>? predicate, EntityWhitelist? whitelist, CancellableEntityEventArgs? validationEvent)
    {
        Range = range;
        CheckObstruction = checkObstructions;
        Predicate = predicate;
        Whitelist = whitelist;
        ValidationEvent = validationEvent;

        _enabled = Predicate != null || Whitelist != null || ValidationEvent != null;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_enabled || !_timing.IsFirstTimePredicted)
            return;

        HighlightTargets();
    }

    private void HighlightTargets()
    {
        if (_playerManager.LocalPlayer?.ControlledEntity is not { Valid: true } player)
            return;

        // remove current highlights
        RemoveHighlights();

        // find possible targets on screen
        // TODO: Duplicated in SpriteSystem and DragDropSystem. Should probably be cached somewhere for a frame?
        var mousePos = _eyeManager.ScreenToMap(_inputManager.MouseScreenPosition).Position;
        var bounds = new Box2(mousePos - LookupSize / 2f, mousePos + LookupSize / 2f);
        var pvsEntities = _lookup.GetEntitiesIntersecting(_eyeManager.CurrentMap, bounds, LookupFlags.Approximate | LookupFlags.Static);
        var spriteQuery = GetEntityQuery<SpriteComponent>();

        foreach (var entity in pvsEntities)
        {
            if (!spriteQuery.TryGetComponent(entity, out var sprite) || !sprite.Visible)
                continue;

            // Check the predicate
            var valid = Predicate?.Invoke(entity) ?? true;

            // check the entity whitelist
            if (valid && Whitelist != null)
                valid = Whitelist.IsValid(entity);

            // and check the cancellable event
            if (valid && ValidationEvent != null)
            {
                ValidationEvent.Uncancel();
                RaiseLocalEvent(entity, (object) ValidationEvent, broadcast: false);
                valid = !ValidationEvent.Cancelled;
            }

            if (!valid)
            {
                // was this previously valid?
                if (_highlightedSprites.Remove(sprite) && (sprite.PostShader == _shaderTargetValid || sprite.PostShader == _shaderTargetInvalid))
                {
                    sprite.PostShader = null;
                    sprite.RenderOrder = 0;
                }

                continue;
            }

            // Range check
            if (CheckObstruction)
                valid = _interactionSystem.InRangeUnobstructed(player, entity, Range);
            else if (Range >= 0)
            {
                var origin = Transform(player).WorldPosition;
                var target = Transform(entity).WorldPosition;
                valid = (origin - target).LengthSquared <= Range;
            }

            if (sprite.PostShader != null &&
                sprite.PostShader != _shaderTargetValid &&
                sprite.PostShader != _shaderTargetInvalid)
                return;

            // highlight depending on whether its in or out of range
            sprite.PostShader = valid ? _shaderTargetValid : _shaderTargetInvalid;
            sprite.RenderOrder = EntityManager.CurrentTick.Value;
            _highlightedSprites.Add(sprite);
        }
    }

    private void RemoveHighlights()
    {
        foreach (var sprite in _highlightedSprites)
        {
            if (sprite.PostShader != _shaderTargetValid && sprite.PostShader != _shaderTargetInvalid)
                continue;

            sprite.PostShader = null;
            sprite.RenderOrder = 0;
        }

        _highlightedSprites.Clear();
    }
}
