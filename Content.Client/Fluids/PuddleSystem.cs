using System.Numerics;
using Content.Client.IconSmoothing;
using Content.Shared.CCVar;
using Content.Shared.Chemistry.Components;
using Content.Shared.Fluids;
using Content.Shared.Fluids.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Configuration;

namespace Content.Client.Fluids;

public sealed partial class PuddleSystem : SharedPuddleSystem
{
    private static readonly ProtoId<ShaderPrototype> PuddleColorBlendShader = "PuddleColorBlend";
    private static readonly Color PuddleFallbackColor = Color.White.WithAlpha(0.5f);

    [Dependency] private AppearanceSystem _appearance = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private IconSmoothSystem _smooth = default!;
    [Dependency] private SpriteSystem _sprite = default!;

    [Dependency] private EntityQuery<PuddleComponent> _puddleQuery;
    [Dependency] private EntityQuery<PuddleColorBlendComponent> _blendQuery;
    [Dependency] private EntityQuery<IconSmoothComponent> _smoothQuery;
    [Dependency] private EntityQuery<SpriteComponent> _spriteQuery;
    [Dependency] private EntityQuery<TransformComponent> _transformQuery;
    [Dependency] private EntityQuery<MapGridComponent> _gridQuery;

    private bool _blendEnabled = true;

    public override void Initialize()
    {
        base.Initialize();
        Subs.CVar(_cfg, CCVars.PuddleBlending, SetPuddleBlendingEnabled);
    }

    [SubscribeLocalEvent]
    private void OnPuddleStartup(Entity<PuddleComponent> entity, ref ComponentStartup args)
    {
        if (_blendEnabled && _spriteQuery.TryComp(entity, out var sprite))
            EnsureBlendShader(entity.Owner, sprite, PuddleFallbackColor);
    }

    protected override void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        base.OnPrototypesReloaded(args);

        if (!_blendEnabled ||
            !args.TryGetModified<ShaderPrototype>(out var modified) ||
            !modified.Contains(PuddleColorBlendShader.Id))
        {
            return;
        }

        var query = AllEntityQuery<PuddleColorBlendComponent, SpriteComponent>();
        while (query.MoveNext(out var uid, out var blend, out var sprite))
        {
            blend.Shader?.Dispose();
            blend.Shader = null;
            EnsureBlendShader(uid, sprite, blend.SelfColor);
            UpdateBlendShader(uid);
        }
    }

    [SubscribeLocalEvent]
    private void OnPuddleAppearance(EntityUid uid, PuddleComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        var volume = 1f;

        if (args.AppearanceData.TryGetValue(PuddleVisuals.CurrentVolume, out var volumeObj))
        {
            volume = (float)volumeObj;
        }

        // Update smoothing and sprite based on volume.
        if (_smoothQuery.TryComp(uid, out var smooth))
        {
            if (volume < LowThreshold)
            {
                _sprite.LayerSetRsiState((uid, args.Sprite), 0, $"{smooth.StateBase}a");
                _smooth.SetEnabled(uid, false, smooth);
            }
            else if (volume < MediumThreshold)
            {
                _sprite.LayerSetRsiState((uid, args.Sprite), 0, $"{smooth.StateBase}b");
                _smooth.SetEnabled(uid, false, smooth);
            }
            else
            {
                if (!smooth.Enabled)
                {
                    _sprite.LayerSetRsiState((uid, args.Sprite), 0, $"{smooth.StateBase}0");
                    _smooth.SetEnabled(uid, true, smooth);
                    _smooth.DirtyNeighbours(uid);
                }
            }
        }

        Color color;
        if (args.AppearanceData.TryGetValue(PuddleVisuals.SolutionColor, out var colorObj))
        {
            color = (Color) colorObj;
        }
        else if (_blendQuery.TryComp(uid, out var existingBlend))
        {
            color = existingBlend.SelfColor;
        }
        else
        {
            color = PuddleFallbackColor;
        }

        if (!_blendEnabled)
        {
            _sprite.SetColor((uid, args.Sprite), color);
            return;
        }

        EnsureBlendShader(uid, args.Sprite, color);
        UpdatePuddleAndNeighbors(uid);
    }

    private void SetPuddleBlendingEnabled(bool enabled)
    {
        _blendEnabled = enabled;

        var query = AllEntityQuery<PuddleComponent, SpriteComponent>();
        while (query.MoveNext(out var uid, out _, out var sprite))
        {
            if (enabled)
            {
                var color = _blendQuery.TryComp(uid, out var blend) ? blend.SelfColor : sprite.Color;
                EnsureBlendShader(uid, sprite, color);
                UpdateBlendShader(uid);
                continue;
            }

            if (!_blendQuery.TryComp(uid, out var existingBlend))
                continue;

            sprite.LayerSetShader(0, null, null);
            _sprite.SetColor((uid, sprite), existingBlend.SelfColor);
            existingBlend.Shader?.Dispose();
            existingBlend.Shader = null;
        }
    }

    [SubscribeLocalEvent]
    private void OnIconSmoothUpdated(Entity<PuddleComponent> entity, ref IconSmoothUpdatedEvent args)
    {
        // Cardinal IconSmooth modes do not necessarily dirty diagonal entities, but the color shader uses all eight.
        UpdatePuddleAndNeighbors(entity.Owner);
    }

    [SubscribeLocalEvent]
    private void OnPuddleShutdown(Entity<PuddleComponent> entity, ref ComponentShutdown args)
    {
        // The component is still queryable while this event is raised, so exclude it explicitly.
        UpdatePuddleAndNeighbors(entity.Owner, false, entity.Owner);
    }

    [SubscribeLocalEvent]
    private void OnBlendRemove(Entity<PuddleColorBlendComponent> entity, ref ComponentRemove args)
    {
        if (_spriteQuery.TryComp(entity, out var sprite))
        {
            sprite.LayerSetShader(0, null, null);
            _sprite.SetColor((entity.Owner, sprite), entity.Comp.SelfColor);
        }

        entity.Comp.Shader?.Dispose();
        entity.Comp.Shader = null;
        UpdatePuddleAndNeighbors(entity.Owner);
    }

    private void EnsureBlendShader(EntityUid uid, SpriteComponent sprite, Color color)
    {
        var blend = EnsureComp<PuddleColorBlendComponent>(uid);
        blend.SelfColor = color;

        if (blend.Shader == null || blend.Shader.Disposed)
        {
            Array.Clear(blend.NeighborPresent);

            blend.Shader = ProtoMan.Index(PuddleColorBlendShader).InstanceUnique();
            blend.Shader.SetParameter("neighborColors", blend.NeighborColors);
            blend.Shader.SetParameter("neighborPresent", blend.NeighborPresent);
        }

        blend.Shader.SetParameter("selfColor", color);
        sprite.LayerSetShader(0, blend.Shader, PuddleColorBlendShader.Id);

        // The shader owns the complete RGBA tint. Leaving it on the sprite as well would apply it twice.
        _sprite.SetColor((uid, sprite), Color.White);
    }

    private void UpdateBlendShader(EntityUid uid)
    {
        if (!_blendEnabled)
            return;

        if (!_blendQuery.TryComp(uid, out var blend) ||
            blend.Shader == null ||
            blend.Shader.Disposed)
        {
            return;
        }

        Array.Clear(blend.NeighborPresent);

        if (_smoothQuery.TryComp(uid, out var smooth) &&
            smooth.Enabled &&
            TryGetPuddleTile(uid, out var grid, out var position))
        {
            for (var i = 0; i < (int) PuddleNeighbor.Count; i++)
            {
                var direction = (Direction) (((int) Direction.North - i) & 7);
                if (!TryGetNeighborColor(grid, position.Offset(direction), smooth, out var color))
                    continue;

                blend.NeighborColors[i] = color;
                blend.NeighborPresent[i] = 1f;
            }
        }

        blend.Shader.SetParameter("neighborColors", blend.NeighborColors);
        blend.Shader.SetParameter("neighborPresent", blend.NeighborPresent);
    }

    private bool TryGetNeighborColor(
        Entity<MapGridComponent> grid,
        Vector2i position,
        IconSmoothComponent smooth,
        out Color color)
    {
        foreach (var candidate in _map.GetAnchoredEntities(grid.Owner, grid.Comp, position))
        {
            if (!_puddleQuery.HasComp(candidate) ||
                !_smoothQuery.TryComp(candidate, out var otherSmooth) ||
                !otherSmooth.Enabled ||
                (otherSmooth.SmoothKey != smooth.SmoothKey &&
                 !smooth.AdditionalKeys.Contains(otherSmooth.SmoothKey)))
            {
                continue;
            }

            if (_blendQuery.TryComp(candidate, out var otherBlend))
            {
                color = otherBlend.SelfColor;
                return true;
            }

            if (_appearance.TryGetData(candidate, PuddleVisuals.SolutionColor, out color))
                return true;
        }

        color = default;
        return false;
    }

    private void UpdatePuddleAndNeighbors(EntityUid uid, bool updateSelf = true, EntityUid? excluded = null)
    {
        if (!_blendEnabled)
            return;

        if (updateSelf)
            UpdateBlendShader(uid);

        if (!TryGetPuddleTile(uid, out var grid, out var position))
            return;

        for (var i = 0; i < (int) PuddleNeighbor.Count; i++)
        {
            var direction = (Direction) (((int) Direction.North - i) & 7);

            foreach (var neighbor in _map.GetAnchoredEntities(grid.Owner, grid.Comp, position.Offset(direction)))
            {
                if (neighbor != excluded && _puddleQuery.HasComp(neighbor))
                    UpdateBlendShader(neighbor);
            }
        }
    }

    private bool TryGetPuddleTile(
        EntityUid uid,
        out Entity<MapGridComponent> gridEntity,
        out Vector2i position)
    {
        if (_transformQuery.TryComp(uid, out var transform) &&
            transform.Anchored &&
            _gridQuery.TryComp(transform.GridUid, out var grid))
        {
            gridEntity = (transform.GridUid.Value, grid);
            position = _map.TileIndicesFor(gridEntity, transform.Coordinates);
            return true;
        }

        if (_smoothQuery.TryComp(uid, out var smooth) &&
            smooth.LastPosition is ({ } gridUid, var lastPosition) &&
            _gridQuery.TryComp(gridUid, out grid))
        {
            gridEntity = (gridUid, grid);
            position = lastPosition;
            return true;
        }

        gridEntity = default;
        position = default;
        return false;
    }

    #region Spill

    // Maybe someday we'll have clientside prediction for entity spawning, but not today.
    // Until then, these methods do nothing on the client.
    /// <inheritdoc/>
    public override bool TrySplashSpillAt(Entity<SpillableComponent?> entity, EntityCoordinates coordinates, out EntityUid puddleUid, out Solution solution, bool sound = true, EntityUid? user = null)
    {
        puddleUid = EntityUid.Invalid;
        solution = new Solution();
        return false;
    }

    public override bool TrySplashSpillAt(EntityUid entity,
        EntityCoordinates coordinates,
        Solution spilled,
        out EntityUid puddleUid,
        bool sound = true,
        EntityUid? user = null)
    {
        puddleUid = EntityUid.Invalid;
        return false;
    }

    /// <inheritdoc/>
    public override bool TrySpillAt(EntityCoordinates coordinates, Solution solution, out EntityUid puddleUid, bool sound = true)
    {
        puddleUid = EntityUid.Invalid;
        return false;
    }

    /// <inheritdoc/>
    public override bool TrySpillAt(EntityUid uid, Solution solution, out EntityUid puddleUid, bool sound = true, TransformComponent? transformComponent = null)
    {
        puddleUid = EntityUid.Invalid;
        return false;
    }

    /// <inheritdoc/>
    public override bool TrySpillAt(TileRef tileRef, Solution solution, out EntityUid puddleUid, bool sound = true, bool tileReact = true)
    {
        puddleUid = EntityUid.Invalid;
        return false;
    }

    #endregion Spill
}
