using System.Numerics;
using Content.Shared.Maps;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Console;
using Robust.Shared.Enums;
using Robust.Shared.Map;

namespace Content.Client.Mapping;

/// <summary>
/// Command to toggle visibility of mapping entities.
/// </summary>
/// <seealso cref="ShowInvisibleEntitySystem"/>
public sealed class ShowInvisibleCommand : LocalizedEntityCommands
{
    [Dependency] private readonly ShowInvisibleEntitySystem _entitySystem = null!;

    public override string Command => "show_invisible";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (!_entitySystem.Active)
        {
            _entitySystem.SetActive(true);
            shell.WriteLine(LocalizationManager.GetString("cmd-show_invisible-activated"));
        }
        else
        {
            _entitySystem.SetActive(false);
            shell.WriteLine(LocalizationManager.GetString("cmd-show_invisible-deactivated"));
        }
    }
}

/// <summary>
/// Indicates that this is a mapping entity that is normally invisible, but can be turned visible with map tooling.
/// </summary>
/// <remarks>
/// Entities with this component must have a <see cref="SpriteComponent"/>, of which the <see cref="SpriteComponent.Visible"/> will be changed.
/// </remarks>
/// <seealso cref="ShowInvisibleEntitySystem"/>
/// <seealso cref="ShowInvisibleCommand"/>
/// <seealso cref="SpriteComponent"/>
[RegisterComponent]
public sealed partial class MapInvisibleComponent : Component;

/// <summary>
/// Implements toggling visibility of normally-invisible special mapping tiles and entities.
/// </summary>
/// <seealso cref="ContentTileDefinition.InvisibleSprite"/>
public sealed class ShowInvisibleEntitySystem : EntitySystem
{
    [Dependency] private readonly IEntityManager _entityManager = null!;
    [Dependency] private readonly ITileDefinitionManager _tileDefinitionManager = null!;
    [Dependency] private readonly IOverlayManager _overlayManager = default!;
    [Dependency] private readonly SpriteSystem _spriteSystem = default!;

    public bool Active { get; private set; }

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MapInvisibleComponent, ComponentStartup>(OnComponentStartup);
    }

    public void SetActive(bool newActive)
    {
        if (Active == newActive)
            return;

        Active = newActive;
        UpdateAllComponents();
        if (Active)
        {
            _overlayManager.AddOverlay(new InvisibleTilesOverlay(_entityManager, _tileDefinitionManager));
        }
        else
        {
            _overlayManager.RemoveOverlay<InvisibleTilesOverlay>();
        }
    }

    private void OnComponentStartup(Entity<MapInvisibleComponent> ent, ref ComponentStartup args)
    {
        if (!TryComp(ent, out SpriteComponent? sprite))
            return;

        UpdateComponent(ent, ent, sprite);
    }

    private void UpdateAllComponents()
    {
        var query = AllEntityQuery<MapInvisibleComponent, SpriteComponent>();
        while (query.MoveNext(out var ent, out var component, out var sprite))
        {
            UpdateComponent(ent, component, sprite);
        }
    }

    private void UpdateComponent(EntityUid ent, MapInvisibleComponent component, SpriteComponent sprite)
    {
        _spriteSystem.SetVisible((ent, sprite), Active);
    }

    private sealed class InvisibleTilesOverlay : GridOverlay
    {
        private readonly IEntityManager _entManager;
        private readonly ITileDefinitionManager _tileDefinitionManager;

        public override OverlaySpace Space => OverlaySpace.WorldSpaceGrids;

        internal InvisibleTilesOverlay(IEntityManager entManager, ITileDefinitionManager tileDefinitionManager)
        {
            _entManager = entManager;
            _tileDefinitionManager = tileDefinitionManager;
        }

        protected override void Draw(in OverlayDrawArgs args)
        {
            if (!_entManager.TryGetComponent(Grid, out TransformComponent? xform))
                return;

            var xformSystem = _entManager.System<TransformSystem>();
            var mapSystem = _entManager.System<MapSystem>();
            var spriteSys = _entManager.System<SpriteSystem>();

            var handle = (DrawingHandleWorld)args.DrawingHandle;
            handle.SetTransform(xformSystem.GetWorldMatrix(Grid));

            var tileSize = Grid.Comp.TileSizeVector;

            var enumerator = mapSystem.GetAllTilesEnumerator(Grid, Grid);
            while (enumerator.MoveNext(out var tileRefNullable))
            {
                var tileRef = tileRefNullable.Value;
                if (!_tileDefinitionManager.TryGetDefinition(tileRef.Tile.TypeId, out var tileDefinition))
                    continue;

                if (tileDefinition is not ContentTileDefinition contentTileDef)
                    continue;

                if (contentTileDef.InvisibleSprite is not { } invisSprite)
                    continue;

                var pos = tileRef.GridIndices * tileSize;

                handle.DrawTextureRect(spriteSys.Frame0(invisSprite), Box2.FromDimensions(pos, tileSize));
            }

            handle.SetTransform(Matrix3x2.Identity);
            RequiresFlush = true;
        }
    }
}
