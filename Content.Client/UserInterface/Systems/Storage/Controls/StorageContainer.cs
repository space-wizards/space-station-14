using System.Linq;
using System.Numerics;
using Content.Client.Items.Systems;
using Content.Client.Storage.Systems;
using Content.Shared.Storage;
using Content.Shared.Storage.EntitySystems;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.UserInterface.Systems.Storage.Controls;

public sealed class StorageContainer : BoxContainer
{
    [Dependency] private readonly IEntityManager _entity = default!;
    private ItemSystem? _itemSystem;
    private StorageSystem? _storageSystem;

    private readonly GridContainer _grid;
    private readonly GridContainer _sidebar;
    private readonly Label _nameLabel;

    private readonly BoxContainer _tempContainer = new();

    //todo support reloading
    private Texture? _emptyTexture;
    private Texture? _blockedTexture;
    private Texture? _exitTexture;
    private Texture? _sidebarTopTexture;
    private Texture? _sidebarMiddleTexture;
    private Texture? _sidebarBottomTexture;

    private readonly string _emptyTexturePath = "Storage/tile_empty";
    private readonly string _blockedTexturePath = "Storage/tile_blocked";
    private readonly string _exitTexturePath = "Storage/exit";
    private readonly string _sidebarTopTexturePath = "Storage/sidebar_top";
    private readonly string _sidebarMiddleTexturePath = "Storage/sidebar_mid";
    private readonly string _sidebarBottomTexturePath = "Storage/sidebar_bottom";

    public StorageContainer()
    {
        IoCManager.InjectDependencies(this);

        _emptyTexture = Theme.ResolveTextureOrNull(_emptyTexturePath)?.Texture;
        _blockedTexture = Theme.ResolveTextureOrNull(_blockedTexturePath)?.Texture;
        _exitTexture = Theme.ResolveTextureOrNull(_exitTexturePath)?.Texture;
        _sidebarTopTexture = Theme.ResolveTextureOrNull(_sidebarTopTexturePath)?.Texture;
        _sidebarMiddleTexture = Theme.ResolveTextureOrNull(_sidebarMiddleTexturePath)?.Texture;
        _sidebarBottomTexture = Theme.ResolveTextureOrNull(_sidebarBottomTexturePath)?.Texture;

        _nameLabel = new Label
        {
            ReservesSpace = true,
            Visible = false,
            HorizontalAlignment = HAlignment.Left
        };

        _sidebar = new GridContainer
        {
            HSeparationOverride = 0,
            VSeparationOverride = 0,
            Columns = 1
        };

        _grid = new GridContainer
        {
            HSeparationOverride = 0,
            VSeparationOverride = 0
        };

        var container = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            Children =
            {
                _nameLabel,
                new BoxContainer
                {
                    Orientation = LayoutOrientation.Horizontal,
                    Children =
                    {
                        _sidebar,
                        _grid,
                        _tempContainer
                    }
                }
            }
        };

        AddChild(container);
    }

    public void UpdateContainer(Entity<StorageComponent>? entity)
    {
        Visible = entity != null;
        if (entity == null)
            return;

        _nameLabel.Text = _entity.GetComponent<MetaDataComponent>(entity.Value).EntityName;

        BuildGridRepresentation(entity.Value);
    }

    private void BuildGridRepresentation(Entity<StorageComponent> entity)
    {
        _itemSystem ??= _entity.System<ItemSystem>();
        _storageSystem = _entity.System<StorageSystem>();

        var comp = entity.Comp;
        if (!comp.StorageGrid.Any())
            return;

        var boundingGrid = SharedStorageSystem.GetBoundingBox(comp.StorageGrid);
        var totalWidth = boundingGrid.Width + 1;

        _grid.Children.Clear();
        _grid.Rows = boundingGrid.Height;
        _grid.Columns = totalWidth;
        for (var y = boundingGrid.Bottom; y <= boundingGrid.Top; y++)
        {
            for (var x = boundingGrid.Left; x <= boundingGrid.Right; x++)
            {
                var texture = comp.StorageGrid.Any(g => g.Contains(x, y))
                    ? _emptyTexture
                    : _blockedTexture;
                _grid.AddChild(new TextureRect
                {
                    Texture = texture,
                    TextureScale = new Vector2(2, 2)
                });
            }
        }

        #region Sidebar
        _sidebar.Children.Clear();
        _sidebar.Rows = boundingGrid.Height + 1;
        //todo this should change when there is a parent container to return to.
        var exitButton = new TextureButton
        {
            TextureNormal = _exitTexture,
            Scale = new Vector2(2, 2),
        };
        exitButton.OnPressed += _ =>
        {
            if (_entity.TryGetComponent<UserInterfaceComponent>(entity, out var ui))
                ui.OpenInterfaces.GetValueOrDefault(StorageComponent.StorageUiKey.Key)?.Close();
        };
        var exitContainer = new BoxContainer
        {
            Children =
            {
                new TextureRect
                {
                    Texture = _sidebarTopTexture,
                    TextureScale = new Vector2(2, 2),
                    Children =
                    {
                        exitButton
                    }
                }
            }
        };
        _sidebar.AddChild(exitContainer);
        for (var i = 0; i < boundingGrid.Height - 1; i++)
        {
            _sidebar.AddChild(new TextureRect
            {
                Texture = _sidebarMiddleTexture,
                TextureScale = new Vector2(2, 2),
            });
        }
        _sidebar.AddChild(new TextureRect
        {
            Texture = _sidebarBottomTexture,
            TextureScale = new Vector2(2, 2),
        });
        #endregion
    }
}
