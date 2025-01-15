using System.Linq;
using Content.Client.Administration.Managers;
using Content.Client.ContextMenu.UI;
using Content.Client.Decals;
using Content.Client.Gameplay;
using Content.Client.Maps;
using Content.Client.SubFloor;
using Content.Client.UserInterface.Controls;
using Content.Client.UserInterface.Systems.Gameplay;
using Content.Client.Verbs;
using Content.Shared.Administration;
using Content.Shared.Decals;
using Content.Shared.Input;
using Content.Shared.Mapping;
using Content.Shared.Maps;
using Robust.Client.Console;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Placement;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Enums;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using static Robust.Client.UserInterface.Controls.BaseButton;
using static Robust.Client.UserInterface.Controls.OptionButton;
using static Robust.Shared.Input.Binding.PointerInputCmdHandler;
using Vector2 = System.Numerics.Vector2;

namespace Content.Client.Mapping;

public sealed class MappingState : GameplayStateBase
{
    [Dependency] private readonly IClientAdminManager _admin = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;
    [Dependency] private readonly IEntityNetworkManager _entityNetwork = default!;
    [Dependency] private readonly IInputManager _input = default!;
    [Dependency] private readonly ILogManager _log = default!;
    [Dependency] private readonly IMapManager _mapMan = default!;
    [Dependency] private readonly MappingManager _mapping = default!;
    [Dependency] private readonly IOverlayManager _overlays = default!;
    [Dependency] private readonly IPlacementManager _placement = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IResourceCache _resources = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IClientConsoleHost _consoleHost = default!;
    [Dependency] private readonly ILocalizationManager _locale = default!;

    private EntityMenuUIController _entityMenuController = default!;

    private DecalPlacementSystem _decal = default!;
    private SpriteSystem _sprite = default!;
    private TransformSystem _transform = default!;
    private VerbSystem _verbs = default!;
    private GridDraggingSystem _gridDrag = default!;
    private MapSystem _map = default!;
    private SharedDecalSystem _sharedDecal = default!;

    // 1 off in case something else uses these colors since we use them to compare
    private static readonly Color PickColor = new(1, 255, 0);
    private static readonly Color DeleteColor = new(255, 1, 0);
    private static readonly Color EraseDecalColor = Color.Red.WithAlpha(0.2f);
    private static readonly Color GridSelectColor = Color.Green.WithAlpha(0.2f);
    private static readonly Color GridRemoveColor = Color.Red.WithAlpha(0.2f);

    private readonly ISawmill _sawmill;
    private readonly GameplayStateLoadController _loadController;
    private bool _setup;
    private readonly Dictionary<Type, List<MappingPrototype>> _allPrototypes = new();
    private readonly Dictionary<IPrototype, MappingPrototype> _allPrototypesDict = new();
    private readonly Dictionary<Type, Dictionary<string, MappingPrototype>> _idDict = new();
    private (TimeSpan At, MappingSpawnButton Button)? _lastClicked;
    private (Control, MappingPrototypeList)? _scrollTo;
    private bool _tileErase;
    private int _decalIndex;

    private MappingScreen Screen => (MappingScreen) UserInterfaceManager.ActiveScreen!;
    private MainViewport Viewport => UserInterfaceManager.ActiveScreen!.GetWidget<MainViewport>()!;

    public CursorMeta Meta { get; }

    public MappingState()
    {
        IoCManager.InjectDependencies(this);

        _sawmill = _log.GetSawmill("mapping");
        _loadController = UserInterfaceManager.GetUIController<GameplayStateLoadController>();

        Meta = new CursorMeta();
    }

    protected override void Startup()
    {
        EnsureSetup();
        base.Startup();

        UserInterfaceManager.LoadScreen<MappingScreen>();
        _loadController.LoadScreen();

        var context = _input.Contexts.GetContext("common");
        context.AddFunction(ContentKeyFunctions.MappingUnselect);
        context.AddFunction(ContentKeyFunctions.SaveMap);
        context.AddFunction(ContentKeyFunctions.MappingEnablePick);
        context.AddFunction(ContentKeyFunctions.MappingEnableDecalPick);
        context.AddFunction(ContentKeyFunctions.MappingEnableDelete);
        context.AddFunction(ContentKeyFunctions.MappingPick);
        context.AddFunction(ContentKeyFunctions.MappingRemoveDecal);
        context.AddFunction(ContentKeyFunctions.MappingCancelEraseDecal);
        context.AddFunction(ContentKeyFunctions.MappingOpenContextMenu);
        context.AddFunction(ContentKeyFunctions.MouseMiddle);

        Screen.DecalSystem = _decal;

        Screen.Entities.GetPrototypeData += OnGetData;
        Screen.Entities.SelectionChanged += OnSelected;
        Screen.Tiles.GetPrototypeData += OnGetData;
        Screen.Tiles.SelectionChanged += OnSelected;
        Screen.Decals.GetPrototypeData += OnGetData;
        Screen.Decals.SelectionChanged += OnSelected;

        Screen.Pick.OnPressed += OnPickPressed;
        Screen.PickDecal.OnPressed += OnPickDecalPressed;
        Screen.EntityReplaceButton.OnToggled += OnEntityReplacePressed;
        Screen.EntityPlacementMode.OnItemSelected += OnEntityPlacementSelected;
        Screen.EraseEntityButton.OnToggled += OnEraseEntityPressed;
        Screen.EraseTileButton.OnToggled += OnEraseTilePressed;
        Screen.EraseDecalButton.OnToggled += OnEraseDecalPressed;
        Screen.FixGridAtmos.OnPressed += OnFixGridAtmosPressed;
        Screen.RemoveGrid.OnPressed += OnRemoveGridPressed;
        Screen.MoveGrid.OnPressed += OnMoveGridPressed;
        Screen.GridVV.OnPressed += OnGridVVPressed;
        Screen.PipesColor.OnPressed += OnPipesColorPressed;
        Screen.ChatButton.OnPressed += OnChatButtonPressed;
        _placement.PlacementChanged += OnPlacementChanged;
        _mapping.OnFavoritePrototypesLoaded += OnFavoritesLoaded;

        CommandBinds.Builder
            .Bind(ContentKeyFunctions.MappingUnselect, new PointerInputCmdHandler(HandleMappingUnselect, outsidePrediction: true))
            .Bind(ContentKeyFunctions.SaveMap, new PointerInputCmdHandler(HandleSaveMap, outsidePrediction: true))
            .Bind(ContentKeyFunctions.MappingEnablePick, new PointerStateInputCmdHandler(HandleEnablePick, HandleDisablePick, outsidePrediction: true))
            .Bind(ContentKeyFunctions.MappingEnableDecalPick, new PointerStateInputCmdHandler(HandleEnableDecalPick, HandleDisableDecalPick, outsidePrediction: true))
            .Bind(ContentKeyFunctions.MappingEnableDelete, new PointerStateInputCmdHandler(HandleEnableDelete, HandleDisableDelete, outsidePrediction: true))
            .Bind(ContentKeyFunctions.MappingPick, new PointerInputCmdHandler(HandlePick, outsidePrediction: true))
            .Bind(ContentKeyFunctions.MappingRemoveDecal, new PointerInputCmdHandler(HandleEditorCancelPlace, outsidePrediction: true))
            .Bind(ContentKeyFunctions.MappingCancelEraseDecal, new PointerInputCmdHandler(HandleCancelEraseDecal, outsidePrediction: true))
            .Bind(ContentKeyFunctions.MappingOpenContextMenu, new PointerInputCmdHandler(HandleOpenContextMenu, outsidePrediction: true))
            .Bind(ContentKeyFunctions.MouseMiddle, new PointerInputCmdHandler(HandleMouseMiddle, outsidePrediction: true))
            .Bind(EngineKeyFunctions.Use, new PointerInputCmdHandler(HandleUse, outsidePrediction: true))
            .Register<MappingState>();

        _overlays.AddOverlay(new MappingOverlay(this));

        _prototypeManager.PrototypesReloaded += OnPrototypesReloaded;

        _mapping.LoadFavorites();
        ReloadPrototypes();
        UpdateLocale();
    }

    protected override void Shutdown()
    {
        SaveFavorites();
        CommandBinds.Unregister<MappingState>();

        Screen.Entities.GetPrototypeData -= OnGetData;
        Screen.Entities.SelectionChanged -= OnSelected;
        Screen.Tiles.GetPrototypeData -= OnGetData;
        Screen.Tiles.SelectionChanged -= OnSelected;
        Screen.Decals.GetPrototypeData -= OnGetData;
        Screen.Decals.SelectionChanged -= OnSelected;

        Screen.Pick.OnPressed -= OnPickPressed;
        Screen.PickDecal.OnPressed -= OnPickDecalPressed;
        Screen.EntityReplaceButton.OnToggled -= OnEntityReplacePressed;
        Screen.EntityPlacementMode.OnItemSelected -= OnEntityPlacementSelected;
        Screen.EraseEntityButton.OnToggled -= OnEraseEntityPressed;
        Screen.EraseTileButton.OnToggled -= OnEraseTilePressed;
        Screen.EraseDecalButton.OnToggled -= OnEraseDecalPressed;
        Screen.FixGridAtmos.OnPressed -= OnFixGridAtmosPressed;
        Screen.RemoveGrid.OnPressed -= OnRemoveGridPressed;
        Screen.MoveGrid.OnPressed -= OnMoveGridPressed;
        Screen.GridVV.OnPressed -= OnGridVVPressed;
        Screen.PipesColor.OnPressed -= OnPipesColorPressed;
        Screen.ChatButton.OnPressed -= OnChatButtonPressed;
        _placement.PlacementChanged -= OnPlacementChanged;
        _prototypeManager.PrototypesReloaded -= OnPrototypesReloaded;
        _mapping.OnFavoritePrototypesLoaded -= OnFavoritesLoaded;

        UserInterfaceManager.ClearWindows();
        _loadController.UnloadScreen();
        UserInterfaceManager.UnloadScreen();

        var context = _input.Contexts.GetContext("common");
        context.RemoveFunction(ContentKeyFunctions.MappingUnselect);
        context.RemoveFunction(ContentKeyFunctions.SaveMap);
        context.RemoveFunction(ContentKeyFunctions.MappingEnablePick);
        context.RemoveFunction(ContentKeyFunctions.MappingEnableDecalPick);
        context.RemoveFunction(ContentKeyFunctions.MappingEnableDelete);
        context.RemoveFunction(ContentKeyFunctions.MappingPick);
        context.RemoveFunction(ContentKeyFunctions.MappingRemoveDecal);
        context.RemoveFunction(ContentKeyFunctions.MappingCancelEraseDecal);
        context.RemoveFunction(ContentKeyFunctions.MappingOpenContextMenu);

        _overlays.RemoveOverlay<MappingOverlay>();

        base.Shutdown();
    }

    private void EnsureSetup()
    {
        if (_setup)
            return;

        _setup = true;

        _entityMenuController = UserInterfaceManager.GetUIController<EntityMenuUIController>();

        _decal = _entityManager.System<DecalPlacementSystem>();
        _sprite = _entityManager.System<SpriteSystem>();
        _transform = _entityManager.System<TransformSystem>();
        _verbs = _entityManager.System<VerbSystem>();
        _gridDrag = _entityManager.System<GridDraggingSystem>();
        _map = _entityManager.System<MapSystem>();
        _sharedDecal = _entityManager.System<SharedDecalSystem>();
    }

    private void UpdateLocale()
    {
        if (_input.TryGetKeyBinding(ContentKeyFunctions.MappingEnablePick, out var enablePickBinding))
            Screen.Pick.ToolTip = Loc.GetString("mapping-pick-tooltip", ("key", enablePickBinding.GetKeyString()));

        if (_input.TryGetKeyBinding(ContentKeyFunctions.MappingEnableDecalPick, out var enableDecalPickBinding))
            Screen.PickDecal.ToolTip = Loc.GetString("mapping-pick-decal-tooltip", ("key", enableDecalPickBinding.GetKeyString()));

        if (_input.TryGetKeyBinding(ContentKeyFunctions.MappingEnableDelete, out var enableDeleteBinding))
            Screen.EraseEntityButton.ToolTip = Loc.GetString("mapping-erase-entity-tooltip", ("key", enableDeleteBinding.GetKeyString()));
    }

    private void SaveFavorites()
    {
        Screen.Entities.FavoritesPrototype.Children ??= new List<MappingPrototype>();
        Screen.Tiles.FavoritesPrototype.Children ??= new List<MappingPrototype>();
        Screen.Decals.FavoritesPrototype.Children ??= new List<MappingPrototype>();

        var children = Screen.Entities.FavoritesPrototype.Children
            .Union(Screen.Tiles.FavoritesPrototype.Children)
            .Union(Screen.Decals.FavoritesPrototype.Children)
            .ToList();

        _mapping.SaveFavorites(children);
    }

    private void ReloadPrototypes()
    {
        var mappings = new Dictionary<string, MappingPrototype>();
        var entities = new MappingPrototype(null, Loc.GetString("mapping-entities")) { Children = new List<MappingPrototype>() };
        foreach (var entity in _prototypeManager.EnumeratePrototypes<EntityPrototype>())
        {
            Register(entity, entity.ID, entities);
        }

        Sort(mappings, entities);
        mappings.Clear();

        var tiles = new MappingPrototype(null, Loc.GetString("mapping-tiles")) { Children = new List<MappingPrototype>() };
        foreach (var tile in _prototypeManager.EnumeratePrototypes<ContentTileDefinition>())
        {
            Register(tile, tile.ID, tiles);
        }

        Sort(mappings, tiles);
        mappings.Clear();

        var decals = new MappingPrototype(null, Loc.GetString("mapping-decals")) { Children = new List<MappingPrototype>() };
        foreach (var decal in _prototypeManager.EnumeratePrototypes<DecalPrototype>())
        {
            if (decal.ShowMenu)
                Register(decal, decal.ID, decals);
        }

        Sort(mappings, decals);
        mappings.Clear();

        var entitiesTemplate = new MappingPrototype(null, Loc.GetString("mapping-template"));
        var tilesTemplate = new MappingPrototype(null, Loc.GetString("mapping-template"));
        var decalsTemplate = new MappingPrototype(null, Loc.GetString("mapping-template"));

        foreach (var favorite in _prototypeManager.EnumeratePrototypes<MappingTemplatePrototype>())
        {
            switch (favorite.RootType)
            {
                case TemplateType.Entity:
                    RegisterTemplates(favorite, favorite.RootType, entitiesTemplate);
                    break;
                case TemplateType.Tile:
                    RegisterTemplates(favorite, favorite.RootType, tilesTemplate);
                    break;
                case TemplateType.Decal:
                    RegisterTemplates(favorite, favorite.RootType, decalsTemplate);
                    break;
            }
        }

        Sort(mappings, entitiesTemplate);
        mappings.Clear();
        Screen.Entities.UpdateVisible(
            new (entitiesTemplate.Children?.Count > 0 ? [entitiesTemplate, entities] : [entities]),
            _allPrototypes.GetOrNew(typeof(EntityPrototype)));

        Sort(mappings, tilesTemplate);
        mappings.Clear();
        Screen.Tiles.UpdateVisible(
            new (tilesTemplate.Children?.Count > 0 ? [tilesTemplate, tiles] : [tiles]),
            _allPrototypes.GetOrNew(typeof(ContentTileDefinition)));

        Sort(mappings, decalsTemplate);
        mappings.Clear();
        Screen.Decals.UpdateVisible(
            new (decalsTemplate.Children?.Count > 0 ? [decalsTemplate, decals] : [decals]),
            _allPrototypes.GetOrNew(typeof(DecalPrototype)));
    }

    private void RegisterTemplates(MappingTemplatePrototype templateProto, TemplateType? type, MappingPrototype toplevel)
    {
        if (type == null)
        {
            if (templateProto.RootType == null)
                return;
            type = templateProto.RootType;
        }

        MappingPrototype? proto = null;
        switch (type)
        {
            case TemplateType.Decal:
                if (_idDict.GetOrNew(typeof(DecalPrototype)).TryGetValue(templateProto.ID, out var decal))
                    proto = decal;
                break;
            case TemplateType.Tile:
                if (_idDict.GetOrNew(typeof(ContentTileDefinition)).TryGetValue(templateProto.ID, out var tile))
                    proto = tile;
                break;
            case TemplateType.Entity:
                if (_idDict.GetOrNew(typeof(EntityPrototype)).TryGetValue(templateProto.ID, out var entity))
                    proto = entity;
                break;
        }

        if (proto == null)
        {
            var name = templateProto.ID;
            if (_locale.TryGetString($"mapping-template-{templateProto.ID.ToLower()}", out var locale))
                name = locale;
            proto = new MappingPrototype(null, name);
        }

        proto.Parents ??= new List<MappingPrototype>();
        proto.Parents.Add(toplevel);

        foreach (var child in templateProto.Children)
        {
            RegisterTemplates(child, type, proto);
        }

        toplevel.Children ??= new List<MappingPrototype>();
        toplevel.Children.Add(proto);
    }

    private MappingPrototype? Register<T>(T? prototype, string id, MappingPrototype topLevel) where T : class, IPrototype, IInheritingPrototype
    {
        {
            if (prototype == null &&
                _prototypeManager.TryIndex(id, out prototype) &&
                prototype is EntityPrototype entity)
            {
                if (entity.HideSpawnMenu || entity.Abstract)
                    prototype = null;
            }
        }

        if (prototype == null)
        {
            if (!_prototypeManager.TryGetMapping(typeof(T), id, out var node))
            {
                _sawmill.Error($"No {nameof(T)} found with id {id}");
                return null;
            }

            var ids = _idDict.GetOrNew(typeof(T));
            if (ids.TryGetValue(id, out var mapping))
            {
                return mapping;
            }
            else
            {
                var name = node.TryGet("name", out ValueDataNode? nameNode)
                    ? nameNode.Value
                    : id;

                if (node.TryGet("suffix", out ValueDataNode? suffix))
                    name = $"{name} [{suffix.Value}]";

                mapping = new MappingPrototype(prototype, name);
                _allPrototypes.GetOrNew(typeof(T)).Add(mapping);
                ids.Add(id, mapping);

                if (node.TryGet("parent", out ValueDataNode? parentValue))
                {
                    var parent = Register<T>(null, parentValue.Value, topLevel);

                    if (parent != null)
                    {
                        mapping.Parents ??= new List<MappingPrototype>();
                        mapping.Parents.Add(parent);
                        parent.Children ??= new List<MappingPrototype>();
                        parent.Children.Add(mapping);
                    }
                }
                else if (node.TryGet("parent", out SequenceDataNode? parentSequence))
                {
                    foreach (var parentNode in parentSequence.Cast<ValueDataNode>())
                    {
                        var parent = Register<T>(null, parentNode.Value, topLevel);

                        if (parent != null)
                        {
                            mapping.Parents ??= new List<MappingPrototype>();
                            mapping.Parents.Add(parent);
                            parent.Children ??= new List<MappingPrototype>();
                            parent.Children.Add(mapping);
                        }
                    }
                }
                else
                {
                    topLevel.Children ??= new List<MappingPrototype>();
                    topLevel.Children.Add(mapping);
                    mapping.Parents ??= new List<MappingPrototype>();
                    mapping.Parents.Add(topLevel);
                }

                return mapping;
            }
        }
        else
        {
            var ids = _idDict.GetOrNew(typeof(T));
            if (ids.TryGetValue(id, out var mapping))
            {
                return mapping;
            }
            else
            {
                var entity = prototype as EntityPrototype;
                var name = entity?.Name ?? prototype.ID;

                if (!string.IsNullOrWhiteSpace(entity?.EditorSuffix))
                    name = $"{name} [{entity.EditorSuffix}]";

                mapping = new MappingPrototype(prototype, name);
                _allPrototypes.GetOrNew(typeof(T)).Add(mapping);
                _allPrototypesDict.Add(prototype, mapping);
                ids.Add(prototype.ID, mapping);
            }

            if (prototype.Parents == null)
            {
                topLevel.Children ??= new List<MappingPrototype>();
                topLevel.Children.Add(mapping);
                mapping.Parents ??= new List<MappingPrototype>();
                mapping.Parents.Add(topLevel);
                return mapping;
            }

            foreach (var parentId in prototype.Parents)
            {
                var parent = Register<T>(null, parentId, topLevel);

                if (parent != null)
                {
                    mapping.Parents ??= new List<MappingPrototype>();
                    mapping.Parents.Add(parent);
                    parent.Children ??= new List<MappingPrototype>();
                    parent.Children.Add(mapping);
                }
            }

            return mapping;
        }
    }

    private void Sort(Dictionary<string, MappingPrototype> prototypes, MappingPrototype topLevel)
    {
        static int Compare(MappingPrototype a, MappingPrototype b)
        {
            return string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
        }

        topLevel.Children ??= new List<MappingPrototype>();

        foreach (var prototype in prototypes.Values)
        {
            if (prototype.Parents == null && prototype != topLevel)
            {
                prototype.Parents = new List<MappingPrototype> { topLevel };
                topLevel.Children.Add(prototype);
            }

            prototype.Parents?.Sort(Compare);
            prototype.Children?.Sort(Compare);
        }

        topLevel.Children.Sort(Compare);
    }

    private void Deselect()
    {
        if (Screen.Entities.Selected is { } entitySelected)
        {
            entitySelected.Button.Pressed = false;
            Screen.Entities.Selected = null;

            if (entitySelected.Prototype?.Prototype is EntityPrototype)
                _placement.Clear();
        }

        if (Screen.Tiles.Selected is { } tileSelected)
        {
            tileSelected.Button.Pressed = false;
            Screen.Tiles.Selected = null;

            if (tileSelected.Prototype?.Prototype is ContentTileDefinition)
                _placement.Clear();
        }

        if (Screen.Decals.Selected is { } decalSelected)
        {
            decalSelected.Button.Pressed = false;
            Screen.Decals.Selected = null;

            if (decalSelected.Prototype?.Prototype is DecalPrototype)
                _decal.SetActive(false);
        }
    }

    private void EnableEntityEraser()
    {
        if (_placement.Eraser)
            return;

        Deselect();
        _placement.Clear();
        _placement.ToggleEraser();

        if (Screen.EraseDecalButton.Pressed)
            Screen.EraseDecalButton.Pressed = false;

        Screen.UnPressActionsExcept(Screen.EraseEntityButton);
        Screen.EntityPlacementMode.Disabled = true;

        Meta.State = CursorState.Entity;
        Meta.Color = DeleteColor;
    }

    private void DisableEntityEraser()
    {
        if (!_placement.Eraser)
            return;

        _placement.ToggleEraser();
        Meta.State = CursorState.None;
        Screen.EntityPlacementMode.Disabled = false;
    }

    #region On Event
    private void OnPrototypesReloaded(PrototypesReloadedEventArgs obj)
    {
        if (!obj.WasModified<EntityPrototype>() &&
            !obj.WasModified<ContentTileDefinition>() &&
            !obj.WasModified<DecalPrototype>() &&
            !obj.WasModified<MappingTemplatePrototype>())
        {
            return;
        }

        SaveFavorites();
        ReloadPrototypes();
    }

    private void OnPlacementChanged(object? sender, EventArgs e)
    {
        if (!_placement.IsActive && _decal.GetActiveDecal().Decal == null)
            Deselect();

        Screen.EraseEntityButton.Pressed = _placement.Eraser;
        Screen.EntityPlacementMode.Disabled = _placement.Eraser;
    }

    private void OnFavoritesLoaded(List<IPrototype> prototypes)
    {
        Screen.Entities.FavoritesPrototype.Children = new List<MappingPrototype>();
        Screen.Decals.FavoritesPrototype.Children = new List<MappingPrototype>();
        Screen.Tiles.FavoritesPrototype.Children = new List<MappingPrototype>();

        foreach (var prototype in prototypes)
        {
            switch (prototype)
            {
                case EntityPrototype entityPrototype:
                {
                    if (_idDict.GetOrNew(typeof(EntityPrototype)).TryGetValue(entityPrototype.ID, out var entity))
                    {
                        Screen.Entities.FavoritesPrototype.Children.Add(entity);
                        entity.Parents ??= new List<MappingPrototype>();
                        entity.Parents.Add(Screen.Entities.FavoritesPrototype);
                        entity.Favorite = true;
                    }
                    break;
                }
                case DecalPrototype decalPrototype:
                {
                    if (_idDict.GetOrNew(typeof(DecalPrototype)).TryGetValue(decalPrototype.ID, out var decal))
                    {
                        Screen.Decals.FavoritesPrototype.Children.Add(decal);
                        decal.Parents ??= new List<MappingPrototype>();
                        decal.Parents.Add(Screen.Decals.FavoritesPrototype);
                        decal.Favorite = true;
                    }
                    break;
                }
                case ContentTileDefinition tileDefinition:
                {
                    if (_idDict.GetOrNew(typeof(ContentTileDefinition)).TryGetValue(tileDefinition.ID, out var tile))
                    {
                        Screen.Tiles.FavoritesPrototype.Children.Add(tile);
                        tile.Parents ??= new List<MappingPrototype>();
                        tile.Parents.Add(Screen.Tiles.FavoritesPrototype);
                        tile.Favorite = true;
                    }
                    break;
                }
            }
        }
    }

    protected override void OnKeyBindStateChanged(ViewportBoundKeyEventArgs args)
    {
        if (args.Viewport == null)
            base.OnKeyBindStateChanged(new ViewportBoundKeyEventArgs(args.KeyEventArgs, Viewport.Viewport));
        else
            base.OnKeyBindStateChanged(args);

        UpdateLocale();
    }

    private void OnGetData(IPrototype prototype, List<Texture> textures)
    {
        switch (prototype)
        {
            case EntityPrototype entity:
                textures.AddRange(SpriteComponent.GetPrototypeTextures(entity, _resources).Select(t => t.Default));
                break;
            case DecalPrototype decal:
                textures.Add(_sprite.Frame0(decal.Sprite));
                break;
            case ContentTileDefinition tile:
                if (tile.Sprite?.ToString() is { } sprite)
                    textures.Add(_resources.GetResource<TextureResource>(sprite).Texture);
                break;
        }
    }

    private void OnSelected(MappingPrototypeList list, MappingPrototype mapping)
    {
        if (mapping.Prototype == null)
            return;

        var chain = new Stack<MappingPrototype>();
        chain.Push(mapping);

        var parent = mapping.Parents?.FirstOrDefault();
        while (parent != null)
        {
            chain.Push(parent);
            parent = parent.Parents?.FirstOrDefault();
        }

        _lastClicked = null;

        Control? last = null;
        var children = list.PrototypeList.Children.ToList();
        foreach (var prototype in chain)
        {
            foreach (var child in children)
            {
                if (child is MappingSpawnButton button &&
                    button.Prototype == prototype)
                {
                    button.CollapseButton.Pressed = true;
                    list.ToggleCollapse(button);
                    OnSelected(list, button, prototype.Prototype);
                    children = button.ChildrenPrototypes.Children.ToList();
                    children.AddRange(button.ChildrenPrototypesGallery.Children);
                    last = child;
                    break;
                }
            }
        }

        if (last != null && list.PrototypeList.Visible)
            _scrollTo = (last, list);
    }

    private void OnSelected(MappingPrototypeList list, MappingSpawnButton button, IPrototype? prototype)
    {
        var time = _timing.CurTime;
        if (prototype is DecalPrototype)
            Screen.SelectDecal(prototype.ID);

        // Double-click functionality if it's collapsible.
        if (_lastClicked is { } lastClicked &&
            lastClicked.Button == button &&
            lastClicked.At > time - TimeSpan.FromSeconds(0.333) &&
            string.IsNullOrEmpty(list.SearchBar.Text) &&
            button.CollapseButton.Visible)
        {
            button.CollapseButton.Pressed = !button.CollapseButton.Pressed;
            list.ToggleCollapse(button);
            button.Button.Pressed = true;
            list.Selected = button;
            _lastClicked = null;
            return;
        }

        // Toggle if it's the same button (at least if we just unclicked it).
        if (!button.Button.Pressed && button.Prototype?.Prototype != null && _lastClicked?.Button == button)
        {
            _lastClicked = null;
            Deselect();
            return;
        }

        _lastClicked = (time, button);

        if (button.Prototype == null)
            return;

        if (list.Selected is { } oldButton &&
            oldButton != button)
        {
            Deselect();
        }

        Meta.State = CursorState.None;
        Screen.UnPressActionsExcept(new Control());

        switch (prototype)
        {
            case EntityPrototype entity:
            {
                var placementId = Screen.EntityPlacementMode.SelectedId;

                var placement = new PlacementInformation
                {
                    PlacementOption = placementId > 0 ? EntitySpawnWindow.InitOpts[placementId] : entity.PlacementMode,
                    EntityType = entity.ID,
                    IsTile = false
                };

                _decal.SetActive(false);
                _placement.BeginPlacing(placement);
                break;
            }
            case DecalPrototype decal:
                _placement.Clear();

                _decal.SetActive(true);
                Screen.SelectDecal(decal.ID);
                break;
            case ContentTileDefinition tile:
            {
                var placement = new PlacementInformation
                {
                    PlacementOption = "AlignTileAny",
                    TileType = tile.TileId,
                    IsTile = true
                };

                _decal.SetActive(false);
                _placement.BeginPlacing(placement);
                break;
            }
            default:
                _placement.Clear();
                break;
        }

        list.Selected = button;

        button.Button.Pressed = true;
    }

    private void OnEntityReplacePressed(ButtonToggledEventArgs args)
    {
        _placement.Replacement = args.Pressed;
    }

    private void OnEntityPlacementSelected(ItemSelectedEventArgs args)
    {
        Screen.EntityPlacementMode.SelectId(args.Id);

        if (_placement.CurrentMode != null)
        {
            var placement = new PlacementInformation
            {
                PlacementOption = EntitySpawnWindow.InitOpts[args.Id],
                EntityType = _placement.CurrentPermission!.EntityType,
                TileType = _placement.CurrentPermission.TileType,
                Range = 2,
                IsTile = _placement.CurrentPermission.IsTile,
            };

            _placement.BeginPlacing(placement);
        }
    }

    private void OnEraseEntityPressed(ButtonEventArgs args)
    {
        if (args.Button.Pressed == _placement.Eraser)
            return;

        if (args.Button.Pressed)
            EnableEntityEraser();
        else
            DisableEntityEraser();
    }

    private void OnEraseTilePressed(ButtonEventArgs args)
    {
        Meta.State = CursorState.None;
        _placement.Clear();
        Deselect();

        if (!args.Button.Pressed)
        {
            Screen.EntityPlacementMode.Disabled = false;
            _tileErase = false;
            return;
        }

        _placement.BeginPlacing(new PlacementInformation
        {
            PlacementOption = "AlignTileAny",
            TileType = 0,
            Range = 400,
            IsTile = true,
        });

        Screen.UnPressActionsExcept(Screen.EraseTileButton);
        _tileErase = true;
        Screen.EntityPlacementMode.Disabled = true;
    }

    private void OnEraseDecalPressed(ButtonToggledEventArgs args)
    {
        if (args.Button.Pressed)
        {
            Meta.State = CursorState.Tile;
            Meta.Color = EraseDecalColor;

            Screen.UnPressActionsExcept(Screen.EraseDecalButton);
            _placement.Clear();
            Deselect();
        }
        else
        {
            Meta.State = CursorState.None;
        }
    }
    #endregion

    #region Mapping Actions
    private void OnPickPressed(ButtonEventArgs args)
    {
        if (args.Button.Pressed)
            EnablePick();
        else
            DisablePick();
    }

    private void EnablePick()
    {
        Deselect();
        Screen.UnPressActionsExcept(Screen.Pick);
        Meta.State = CursorState.EntityOrTile;
        Meta.Color = PickColor;
        Meta.SecondColor = PickColor.WithAlpha(0.2f);
    }

    private void DisablePick()
    {
        Screen.Pick.Pressed = false;
        Meta.State = CursorState.None;
    }

    private void OnPickDecalPressed(ButtonEventArgs args)
    {
        if (args.Button.Pressed)
        {
            Deselect();
            Meta.State = CursorState.Decal;
            Meta.Color = PickColor;
            Screen.UnPressActionsExcept(args.Button);
        }
        else
        {
            Meta.State = CursorState.None;
        }
    }

    private void OnFixGridAtmosPressed(ButtonEventArgs args)
    {
        if (args.Button.Pressed)
        {
            Deselect();
            Meta.State = CursorState.Grid;
            Meta.Color = GridSelectColor;
            Screen.UnPressActionsExcept(args.Button);
        }
        else
        {
            Meta.State = CursorState.None;
        }
    }

    private void OnRemoveGridPressed(ButtonEventArgs args)
    {
        if (args.Button.Pressed)
        {
            Deselect();
            Meta.State = CursorState.Grid;
            Meta.Color = GridRemoveColor;
            Screen.UnPressActionsExcept(args.Button);
        }
        else
        {
            Meta.State = CursorState.None;
        }
    }

    private void OnMoveGridPressed(ButtonEventArgs args)
    {
        if (args.Button.Pressed)
        {
            Deselect();
            Meta.State = CursorState.Grid;
            Meta.Color = GridSelectColor;
            Screen.UnPressActionsExcept(args.Button);
        }
        else
        {
            Meta.State = CursorState.None;
        }

        var gridDragSystem = _entitySystemManager.GetEntitySystem<GridDraggingSystem>();
        if (args.Button.Pressed != gridDragSystem.Enabled)
        {
            _consoleHost.ExecuteCommand("griddrag");
        }
    }

    private void OnGridVVPressed(ButtonEventArgs args)
    {
        if (args.Button.Pressed)
        {
            Deselect();
            Meta.State = CursorState.Grid;
            Meta.Color = GridSelectColor;
            Screen.UnPressActionsExcept(args.Button);
        }
        else
        {
            Meta.State = CursorState.None;
        }
    }

    private void OnPipesColorPressed(ButtonEventArgs args)
    {
        _entitySystemManager.GetEntitySystem<SubFloorHideSystem>().ShowAll = args.Button.Pressed;

        if (args.Button.Pressed)
        {
            Deselect();
            Meta.State = CursorState.Entity;
            Meta.Color = PickColor;
            Screen.UnPressActionsExcept(args.Button);
        }
        else
        {
            Meta.State = CursorState.None;
        }
    }

    private void OnChatButtonPressed(ButtonEventArgs args)
    {
        Screen.Chat.Visible = args.Button.Pressed;
    }
    #endregion

    #region Handle Bindings
    private bool HandleOpenContextMenu(in PointerInputCmdArgs args)
    {
        Deselect();

        var coords = _transform.ToMapCoordinates(args.Coordinates);
        if (_verbs.TryGetEntityMenuEntities(coords, out var entities))
            _entityMenuController.OpenRootMenu(entities);

        return true;
    }

    private bool HandleMappingUnselect(in PointerInputCmdArgs args)
    {
        if (Screen.MoveGrid.Pressed && _gridDrag.Enabled)
        {
            _consoleHost.ExecuteCommand("griddrag");
        }

        if (_placement.Eraser)
            _placement.ToggleEraser();

        Screen.UnPressActionsExcept(new Control());
        Meta.State = CursorState.None;

        if (Screen.Decals.Selected is not { Prototype.Prototype: DecalPrototype })
            return false;

        Deselect();
        return true;
    }

    private bool HandleSaveMap(in PointerInputCmdArgs args)
    {
#if FULL_RELEASE
        return false;
#endif
        if (!_admin.IsAdmin(true) || !_admin.HasFlag(AdminFlags.Host))
            return false;

        SaveMap();
        return true;
    }

    private bool HandleEnablePick(ICommonSession? session, EntityCoordinates coords, EntityUid uid)
    {
        EnablePick();
        return true;
    }

    private bool HandleDisablePick(ICommonSession? session, EntityCoordinates coords, EntityUid uid)
    {
        DisablePick();
        return true;
    }

    private bool HandleEnableDecalPick(ICommonSession? session, EntityCoordinates coords, EntityUid uid)
    {
        Deselect();
        Screen.PickDecal.Pressed = true;
        Meta.State = CursorState.Decal;
        Meta.Color = PickColor;
        Screen.UnPressActionsExcept(Screen.PickDecal);
        return true;
    }

    private bool HandleDisableDecalPick(ICommonSession? session, EntityCoordinates coords, EntityUid uid)
    {
        Screen.PickDecal.Pressed = false;
        Meta.State = CursorState.None;
        return true;
    }

    private bool HandleEnableDelete(ICommonSession? session, EntityCoordinates coords, EntityUid uid)
    {
        Screen.EraseEntityButton.Pressed = true;
        EnableEntityEraser();
        return true;
    }

    private bool HandleDisableDelete(ICommonSession? session, EntityCoordinates coords, EntityUid uid)
    {
        Screen.EraseEntityButton.Pressed = false;
        DisableEntityEraser();
        return true;
    }

    private bool HandlePick(ICommonSession? session, EntityCoordinates coords, EntityUid uid)
    {
        MappingPrototype? button = null;

        if (Screen.Pick.Pressed)
        {
            if (!uid.IsValid())
            {
                var mapPos = _transform.ToMapCoordinates(coords);

                if (_mapMan.TryFindGridAt(mapPos, out var gridUid, out var grid) &&
                    _entityManager.System<SharedMapSystem>().TryGetTileRef(gridUid, grid, coords, out var tileRef) &&
                    _allPrototypesDict.TryGetValue(tileRef.GetContentTileDefinition(), out button))
                {
                    switch (button.Prototype)
                    {
                        case EntityPrototype:
                        {
                            OnSelected(Screen.Entities, button);
                            break;
                        }
                        case ContentTileDefinition:
                        {
                            OnSelected(Screen.Tiles, button);
                            break;
                        }
                    }

                    return true;
                }
            }
        }
        else if (Screen.PickDecal.Pressed)
        {
            if (GetHoveredDecal() is { } decal &&
                _prototypeManager.TryIndex<DecalPrototype>(decal.Id, out var decalProto) &&
                _allPrototypesDict.TryGetValue(decalProto, out button))
            {
                OnSelected(Screen.Decals, button);
                Screen.SelectDecal(decal);
                return true;
            }
        }
        else
        {
            return false;
        }

        if (button != null)
            return false;

        if (uid == EntityUid.Invalid ||
            _entityManager.GetComponentOrNull<MetaDataComponent>(uid) is not
                { EntityPrototype: { } prototype } ||
            !_allPrototypesDict.TryGetValue(prototype, out button))
        {
            // we always block other input handlers if pick mode is enabled
            // this makes you not accidentally place something in space because you
            // miss-clicked while holding down the pick hotkey
            return true;
        }

        // Selected an entity
        OnSelected(Screen.Entities, button);

        // Match rotation
        _placement.Direction = _entityManager.GetComponent<TransformComponent>(uid).LocalRotation.GetDir();

        return true;
    }

    private bool HandleEditorCancelPlace(ICommonSession? session, EntityCoordinates coords, EntityUid uid)
    {
        if (!Screen.EraseDecalButton.Pressed)
            return false;

        _entityNetwork.SendSystemNetworkMessage(new RequestDecalRemovalEvent(_entityManager.GetNetCoordinates(coords)));
        return true;
    }

    private bool HandleCancelEraseDecal(in PointerInputCmdArgs args)
    {
        if (!Screen.EraseDecalButton.Pressed)
            return false;

        Screen.EraseDecalButton.Pressed = false;
        return true;
    }

    private bool HandleUse(in PointerInputCmdArgs args)
    {
        if (Screen.FixGridAtmos.Pressed)
        {
            Screen.FixGridAtmos.Pressed = false;
            Meta.State = CursorState.None;
            if (GetHoveredGrid() is { } grid)
                _consoleHost.ExecuteCommand($"fixgridatmos {_entityManager.GetNetEntity(grid.Owner).Id}");

            return true;
        }

        if (Screen.RemoveGrid.Pressed)
        {
            Screen.RemoveGrid.Pressed = false;
            Meta.State = CursorState.None;
            if (GetHoveredGrid() is { } grid)
                _consoleHost.ExecuteCommand($"rmgrid {_entityManager.GetNetEntity(grid.Owner).Id}");

            return true;
        }

        if (Screen.GridVV.Pressed)
        {
            Screen.GridVV.Pressed = false;
            Meta.State = CursorState.None;
            if (GetHoveredGrid() is { } grid)
                _consoleHost.ExecuteCommand($"vv {_entityManager.GetNetEntity(grid.Owner).Id}");

            return true;
        }

        if (Screen.PipesColor.Pressed)
        {
            Screen.PipesColor.Pressed = false;
            Meta.State = CursorState.None;
            if (GetHoveredEntity() is { } entity)
                _consoleHost.ExecuteCommand($"colornetwork {_entityManager.GetNetEntity(entity).Id} Pipe {Screen.DecalColor.ToHex()}");

            return true;
        }

        return false;
    }

    private bool HandleMouseMiddle(in PointerInputCmdArgs args)
    {
        if (Screen.PickDecal.Pressed)
        {
            _decalIndex += 1;
            return true;
        }

        if (_decal.GetActiveDecal() is { Decal: not null })
        {
            Screen.ChangeDecalRotation(90f);
            return true;
        }

        return false;
    }
    #endregion

    private async void SaveMap()
    {
        await _mapping.SaveMap();
    }

    public EntityUid? GetHoveredEntity()
    {
        if (UserInterfaceManager.CurrentlyHovered is not IViewportControl viewport ||
            _input.MouseScreenPosition is not { IsValid: true } position)
        {
            return null;
        }

        var mapPos = viewport.PixelToMap(position.Position);
        return GetClickedEntity(mapPos);
    }

    public Entity<MapGridComponent>? GetHoveredGrid()
    {
        if (UserInterfaceManager.CurrentlyHovered is not IViewportControl viewport ||
            _input.MouseScreenPosition is not { IsValid: true } position)
        {
            return null;
        }

        var mapPos = viewport.PixelToMap(position.Position);
        if (_mapMan.TryFindGridAt(mapPos, out var gridUid, out var grid))
        {
            return new Entity<MapGridComponent>(gridUid, grid);
        }

        return null;
    }

    public Box2Rotated? GetHoveredTileBox2()
    {
        if (UserInterfaceManager.CurrentlyHovered is not IViewportControl viewport ||
            _input.MouseScreenPosition is not { IsValid: true } coords)
        {
            return null;
        }

        if (GetHoveredGrid() is not { } grid)
            return null;

        if (!_entityManager.TryGetComponent<TransformComponent>(grid, out var xform))
            return null;

        var mapCoords = viewport.PixelToMap(coords.Position);
        var tileSize = grid.Comp.TileSize;
        var tileDimensions = new Vector2(tileSize, tileSize);
        var tileRef = _map.GetTileRef(grid, mapCoords);
        var worldCoord = _map.LocalToWorld(grid.Owner, grid.Comp, tileRef.GridIndices);
        var box = Box2.FromDimensions(worldCoord, tileDimensions);

        return new Box2Rotated(box, xform.LocalRotation, box.BottomLeft);
    }

    private Decal? GetHoveredDecal()
    {
        if (UserInterfaceManager.CurrentlyHovered is not IViewportControl viewport ||
            _input.MouseScreenPosition is not { IsValid: true } coords)
        {
            return null;
        }

        if (GetHoveredGrid() is not { } grid)
            return null;

        var mapCoords = viewport.PixelToMap(coords.Position);
        var localCoords = _map.WorldToLocal(grid.Owner, grid.Comp, mapCoords.Position);
        var bounds = Box2.FromDimensions(localCoords, new Vector2(1.05f, 1.05f)).Translated(new Vector2(-1, -1));
        var decals = _sharedDecal.GetDecalsIntersecting(grid.Owner, bounds);

        if (decals.FirstOrDefault() is not { Decal: not null })
            return null;

        if (!decals.ToList().TryGetValue(_decalIndex % decals.Count, out var decal))
            return null;

        _decalIndex %= decals.Count;
        return decal.Decal;
    }

    public (Texture, Box2Rotated)? GetHoveredDecalData()
    {
        if (GetHoveredGrid() is not { } grid ||
            !_entityManager.TryGetComponent<TransformComponent>(grid, out var xform))
            return null;

        if (GetHoveredDecal() is not { } decal ||
            !_prototypeManager.TryIndex<DecalPrototype>(decal.Id, out var decalProto))
            return null;

        var worldCoords = _map.LocalToWorld(grid.Owner, grid.Comp, decal.Coordinates);
        var texture = _sprite.Frame0(decalProto.Sprite);
        var box = Box2.FromDimensions(worldCoords, new Vector2(1, 1));
        return (texture, new Box2Rotated(box, decal.Angle + xform.LocalRotation, box.BottomLeft));
    }

    public override void FrameUpdate(FrameEventArgs e)
    {
        if (!Screen.EraseTileButton.Pressed && _tileErase)
        {
            _placement.Clear();
            _tileErase = false;
        }

        if (_scrollTo is not { } scrollTo)
            return;

        var (control, list) = scrollTo;

        // this is not ideal but we wait until the control's height is computed to use
        // its position to scroll to
        if (control.Height > 0 && list.PrototypeList.Visible)
        {
            var y = control.GlobalPosition.Y - list.ScrollContainer.Height / 2 + control.Height - list.GlobalPosition.Y;
            var scroll = list.ScrollContainer;
            scroll.SetScrollValue(scroll.GetScrollValue() + new Vector2(0, y));
            _scrollTo = null;
        }
    }


    public enum CursorState
    {
        None,
        Tile,
        Decal,
        Entity,
        Grid,
        EntityOrTile,
    }

    public sealed class CursorMeta
    {
        /// <summary>
        ///     Defines how the overlay will be rendered
        /// </summary>
        public CursorState State = CursorState.None;

        /// <summary>
        ///     Color with which the mapping overlay will be drawn
        /// </summary>
        public Color Color = Color.White;

        public Color? SecondColor;
    }
}
