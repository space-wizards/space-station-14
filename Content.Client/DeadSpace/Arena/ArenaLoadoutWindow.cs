using System.Numerics;
using Content.Client.Humanoid;
using Content.Client.Inventory;
using Content.Client.Lobby;
using Content.Shared.DeadSpace.Arena;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Preferences;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Input;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Client.DeadSpace.Arena;

public sealed class ArenaLoadoutWindow : DefaultWindow
{
    public event Action<int>? OnLoadoutConfirmed;
    public event Action<int>? OnCostumeBuy;
    public event Action<List<int>>? OnCostumeEquip;

    private int _weaponSelection = -1;
    private ArenaWeaponCard? _selectedCard;
    private readonly BoxContainer _categoriesContainer;
    private readonly Button _confirmButton;

    // костюм.
    private readonly TabContainer _tabs;
    private readonly Label _balanceLabel;
    private readonly Button _cloaksButton;
    private readonly Button _jumpsuitsButton;
    private readonly Button _vestsButton;
    private readonly BoxContainer _costumeListContainer;
    private readonly SpriteView _previewView;
    private readonly Label _equippedLabel;

    private readonly List<ArenaCostumeOption> _costumes = new();
    private HashSet<int> _owned = new();
    private List<int> _equipped = new();
    private int _balance;
    private string _activeCategory = "cloak";
    private readonly List<ArenaCostumeCard> _costumeCards = new();

    // Превью перса.
    private EntityUid? _previewDummy;
    private readonly List<EntityUid> _previewItems = new();

    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IClientPreferencesManager _preferencesManager = default!;

    public ArenaLoadoutWindow()
    {
        IoCManager.InjectDependencies(this);

        var humanoid = _entManager.System<HumanoidAppearanceSystem>();
        var inventory = _entManager.System<ClientInventorySystem>();

        Title = Loc.GetString("arena-loadout-title");
        MinSize = new Vector2(640, 660);
        SetSize = new Vector2(640, 660);

        var outerContainer = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            HorizontalExpand = true,
            VerticalExpand = true,
        };

        var subtitle = new Label
        {
            Text = Loc.GetString("arena-loadout-subtitle"),
            HorizontalAlignment = HAlignment.Center,
            Margin = new Thickness(0, 0, 0, 6),
        };
        outerContainer.AddChild(subtitle);

        _tabs = new TabContainer
        {
            HorizontalExpand = true,
            VerticalExpand = true,
        };
        outerContainer.AddChild(_tabs);

        // оружие вкладка.
        var weaponTab = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            HorizontalExpand = true,
            VerticalExpand = true,
        };

        _categoriesContainer = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            HorizontalExpand = true,
            SeparationOverride = 4,
        };

        var scroll = new ScrollContainer
        {
            VerticalExpand = true,
            HorizontalExpand = true,
            Margin = new Thickness(6, 0),
        };
        scroll.AddChild(_categoriesContainer);
        weaponTab.AddChild(scroll);

        _tabs.AddChild(weaponTab);
        _tabs.SetTabTitle(0, Loc.GetString("arena-tab-weapons"));

        // костюм вкладка.
        var costumeTab = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            HorizontalExpand = true,
            VerticalExpand = true,
        };

        _balanceLabel = new Label
        {
            Text = Loc.GetString("arena-costume-balance", ("amount", 0)),
            HorizontalAlignment = HAlignment.Center,
            Margin = new Thickness(0, 2, 0, 4),
            FontColorOverride = new Color(0.85f, 0.8f, 0.4f),
        };
        costumeTab.AddChild(_balanceLabel);

        var subTabRow = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            HorizontalExpand = true,
            Margin = new Thickness(4, 0, 4, 4),
        };

        _cloaksButton = CreateCategoryButton(Loc.GetString("arena-costume-category-cloak"), "cloak");
        _jumpsuitsButton = CreateCategoryButton(Loc.GetString("arena-costume-category-jumpsuit"), "jumpsuit");
        _vestsButton = CreateCategoryButton(Loc.GetString("arena-costume-category-vest"), "vest");

        subTabRow.AddChild(_cloaksButton);
        subTabRow.AddChild(_jumpsuitsButton);
        subTabRow.AddChild(_vestsButton);
        costumeTab.AddChild(subTabRow);

        var bodyRow = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            HorizontalExpand = true,
            VerticalExpand = true,
            SeparationOverride = 6,
        };

        // одежла.
        _costumeListContainer = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            HorizontalExpand = true,
            VerticalExpand = true,
            SeparationOverride = 4,
        };

        var costumeScroll = new ScrollContainer
        {
            HorizontalExpand = true,
            VerticalExpand = true,
        };
        costumeScroll.AddChild(_costumeListContainer);
        bodyRow.AddChild(costumeScroll);

        // превью перса.
        var previewPanel = new PanelContainer
        {
            SetSize = new Vector2(210, 300),
            Margin = new Thickness(0, 0, 6, 0),
        };
        var previewStyle = new StyleBoxFlat
        {
            BackgroundColor = new Color(0.08f, 0.09f, 0.12f),
            BorderColor = new Color(0.2f, 0.2f, 0.25f),
            BorderThickness = new Thickness(1, 1, 1, 1),
        };
        previewPanel.PanelOverride = previewStyle;

        var previewBox = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            HorizontalExpand = true,
            VerticalExpand = true,
            Margin = new Thickness(4, 4, 4, 4),
        };

        _previewView = new SpriteView
        {
            MinSize = new Vector2(160, 220),
            HorizontalExpand = true,
            VerticalExpand = true,
            HorizontalAlignment = HAlignment.Center,
            VerticalAlignment = VAlignment.Top,
            OverrideDirection = Direction.South,
            Scale = new Vector2(3, 3),
            SpriteOffset = true,
        };
        previewBox.AddChild(_previewView);

        _equippedLabel = new Label
        {
            Text = string.Empty,
            HorizontalAlignment = HAlignment.Center,
            HorizontalExpand = true,
            Margin = new Thickness(2, 4, 2, 2),
            FontColorOverride = new Color(0.6f, 0.75f, 0.6f),
        };
        previewBox.AddChild(_equippedLabel);

        previewPanel.AddChild(previewBox);
        bodyRow.AddChild(previewPanel);

        costumeTab.AddChild(bodyRow);

        var saveHint = new Label
        {
            Text = Loc.GetString("arena-costume-save-hint"),
            HorizontalAlignment = HAlignment.Center,
            Margin = new Thickness(0, 4, 0, 2),
            FontColorOverride = new Color(0.5f, 0.5f, 0.5f),
        };
        costumeTab.AddChild(saveHint);

        _tabs.AddChild(costumeTab);
        _tabs.SetTabTitle(1, Loc.GetString("arena-tab-costume"));

        _confirmButton = new Button
        {
            Text = Loc.GetString("arena-loadout-confirm"),
            Disabled = true,
            Margin = new Thickness(8, 6),
        };
        _confirmButton.OnPressed += _ =>
        {
            if (_weaponSelection >= 0)
                OnLoadoutConfirmed?.Invoke(_weaponSelection);
        };
        outerContainer.AddChild(_confirmButton);

        Contents.AddChild(outerContainer);

        // Локальные обработчики превью.
        _previewDummy = null;
    }

    private Button CreateCategoryButton(string text, string category)
    {
        var button = new Button
        {
            Text = text,
            ToggleMode = true,
            HorizontalExpand = true,
            Margin = new Thickness(2, 0),
        };
        button.OnPressed += _ =>
        {
            _activeCategory = category;
            UpdateCategoryButtons();
            RebuildCostumeList();
        };
        return button;
    }

    private void UpdateCategoryButtons()
    {
        _cloaksButton.Pressed = _activeCategory == "cloak";
        _jumpsuitsButton.Pressed = _activeCategory == "jumpsuit";
        _vestsButton.Pressed = _activeCategory == "vest";
    }

    public void UpdateState(ArenaLoadoutEuiState state)
    {
        UpdateWeaponState(state);

        _costumes.Clear();
        _costumes.AddRange(state.Costumes);
        _owned = new HashSet<int>(state.OwnedCostumes);
        _equipped = new List<int>(state.EquippedCostumes);
        _balance = state.Balance;
        _balanceLabel.Text = Loc.GetString("arena-costume-balance", ("amount", _balance));

        UpdateCategoryButtons();
        RebuildCostumeList();
        RebuildPreview();
    }

    private void UpdateWeaponState(ArenaLoadoutEuiState state)
    {
        _categoriesContainer.RemoveAllChildren();
        _selectedCard = null;
        _weaponSelection = -1;
        _confirmButton.Disabled = true;

        var categories = new List<(string Category, List<ArenaLoadoutOption> Options)>();
        var categoryMap = new Dictionary<string, List<ArenaLoadoutOption>>();

        foreach (var option in state.Weapons)
        {
            var category = Loc.GetString(option.Category);
            if (!categoryMap.TryGetValue(category, out var list))
            {
                list = new List<ArenaLoadoutOption>();
                categoryMap[category] = list;
                categories.Add((category, list));
            }
            list.Add(option);
        }

        foreach (var (category, options) in categories)
        {
            var header = new Label
            {
                Text = category,
                Margin = new Thickness(4, 6, 0, 2),
            };
            _categoriesContainer.AddChild(header);

            foreach (var option in options)
            {
                var card = new ArenaWeaponCard(
                    option.Index,
                    Loc.GetString(option.Name),
                    option.SpritePrototype,
                    Loc.GetString(option.Description));
                card.OnSelected += OnCardSelected;
                _categoriesContainer.AddChild(card);
            }
        }
    }

    private void RebuildCostumeList()
    {
        _costumeListContainer.RemoveAllChildren();
        _costumeCards.Clear();

        foreach (var costume in _costumes)
        {
            if (costume.Category != _activeCategory)
                continue;

            var owned = _owned.Contains(costume.Index);
            var equipped = owned && _equipped.Contains(costume.Index);

            var card = new ArenaCostumeCard(costume, owned, equipped, _balance >= costume.Price);
            card.OnBuy += index => OnCostumeBuy?.Invoke(index);
            card.OnToggleEquip += OnToggleEquip;
            _costumeCards.Add(card);
            _costumeListContainer.AddChild(card);
        }
    }

    private void OnToggleEquip(int costumeIndex)
    {
        var equipped = new List<int>(_equipped);
        if (equipped.Contains(costumeIndex))
            equipped.Remove(costumeIndex);
        else
            equipped.Add(costumeIndex);

        _equipped = equipped;
        OnCostumeEquip?.Invoke(equipped);

        RebuildCostumeList();
        RebuildPreview();
    }

    private void RebuildPreview()
    {
        foreach (var item in _previewItems)
        {
            if (_entManager.EntityExists(item))
                _entManager.DeleteEntity(item);
        }
        _previewItems.Clear();

        if (_previewDummy is { } oldDummy && _entManager.EntityExists(oldDummy))
            _entManager.DeleteEntity(oldDummy);
        _previewDummy = null;

        _previewView.SetEntity(null);

        var profile = _preferencesManager.Preferences?.SelectedCharacter as HumanoidCharacterProfile;
        if (profile == null)
            return;

        // На арене IPC и Vox принудительно становятся людьми — превью показывает их так же.
        var speciesId = profile.Species;
        if (ArenaConstants.SpeciesBlacklist.Contains(speciesId))
        {
            speciesId = SharedHumanoidAppearanceSystem.DefaultSpecies;
            profile = profile.WithSpecies(speciesId);
        }

        if (!_prototypeManager.TryIndex<SpeciesPrototype>(speciesId, out var species))
            return;

        var dummy = _entManager.SpawnEntity(species.DollPrototype, MapCoordinates.Nullspace);
        _previewDummy = dummy;

        _entManager.System<HumanoidAppearanceSystem>().LoadProfile(dummy, profile);

        foreach (var index in _equipped)
        {
            if (index < 0 || index >= _costumes.Count)
                continue;

            var costume = _costumes[index];
            if (!_owned.Contains(index) || !_prototypeManager.HasIndex<EntityPrototype>(costume.ItemPrototype))
                continue;

            var item = _entManager.SpawnEntity(costume.ItemPrototype, MapCoordinates.Nullspace);
            _previewItems.Add(item);
            _entManager.System<ClientInventorySystem>().TryEquip(dummy, item, costume.Slot, silent: true, force: true);
        }

        _previewView.SetEntity(dummy);

        var equippedNames = new List<string>();
        foreach (var index in _equipped)
        {
            if (index >= 0 && index < _costumes.Count)
                equippedNames.Add(Loc.GetString(_costumes[index].Name));
        }
        _equippedLabel.Text = equippedNames.Count > 0
            ? string.Join("\n", equippedNames)
            : Loc.GetString("arena-costume-nothing-equipped");
    }

    public void CleanupPreview()
    {
        foreach (var item in _previewItems)
        {
            if (_entManager.EntityExists(item))
                _entManager.DeleteEntity(item);
        }
        _previewItems.Clear();

        if (_previewDummy is { } dummy && _entManager.EntityExists(dummy))
            _entManager.DeleteEntity(dummy);
        _previewDummy = null;
    }

    private void OnCardSelected(ArenaWeaponCard card)
    {
        _selectedCard?.SetSelected(false);
        _selectedCard = card;
        _weaponSelection = card.WeaponIndex;
        card.SetSelected(true);
        _confirmButton.Disabled = false;
    }

    private sealed class ArenaWeaponCard : PanelContainer
    {
        public event Action<ArenaWeaponCard>? OnSelected;
        public int WeaponIndex { get; }

        private bool _isSelected;
        private static readonly StyleBoxFlat _selectedStyle = new()
        {
            BackgroundColor = new Color(0.2f, 0.45f, 0.2f),
            BorderColor = new Color(0.3f, 0.9f, 0.3f),
            BorderThickness = new Thickness(2, 2, 2, 2),
        };
        private static readonly StyleBoxFlat _defaultStyle = new()
        {
            BackgroundColor = new Color(0.1f, 0.1f, 0.12f),
            BorderColor = new Color(0.2f, 0.2f, 0.25f),
            BorderThickness = new Thickness(1, 1, 1, 1),
        };

        public ArenaWeaponCard(int weaponIndex, string weaponName, string? spritePrototype, string? tooltip = null)
        {
            WeaponIndex = weaponIndex;
            MouseFilter = MouseFilterMode.Stop;
            MinHeight = 56;
            HorizontalExpand = true;

            PanelOverride = _defaultStyle;

            if (!string.IsNullOrEmpty(tooltip))
            {
                ToolTip = tooltip;
            }

            var hbox = new BoxContainer
            {
                Orientation = BoxContainer.LayoutOrientation.Horizontal,
                HorizontalExpand = true,
                VerticalExpand = true,
            };

            if (!string.IsNullOrEmpty(spritePrototype))
            {
                var spriteView = new EntityPrototypeView
                {
                    MinSize = new Vector2(48, 48),
                    SetSize = new Vector2(48, 48),
                    HorizontalAlignment = HAlignment.Center,
                    VerticalAlignment = VAlignment.Center,
                    OverrideDirection = Direction.South,
                };
                spriteView.SetPrototype(spritePrototype);
                hbox.AddChild(spriteView);
            }

            var nameLabel = new Label
            {
                Text = weaponName,
                VerticalAlignment = VAlignment.Center,
                HorizontalExpand = true,
                Margin = new Thickness(8, 0, 0, 0),
            };
            hbox.AddChild(nameLabel);
            AddChild(hbox);

            OnKeyBindDown += args =>
            {
                if (args.Function != EngineKeyFunctions.UIClick)
                    return;
                OnSelected?.Invoke(this);
                args.Handle();
            };
        }

        public void SetSelected(bool selected)
        {
            _isSelected = selected;
            PanelOverride = selected ? _selectedStyle : _defaultStyle;
        }
    }

    private sealed class ArenaCostumeCard : PanelContainer
    {
        public event Action<int>? OnBuy;
        public event Action<int>? OnToggleEquip;
        public int CostumeIndex { get; }

        private static readonly StyleBoxFlat _equippedStyle = new()
        {
            BackgroundColor = new Color(0.2f, 0.4f, 0.2f),
            BorderColor = new Color(0.3f, 0.9f, 0.3f),
            BorderThickness = new Thickness(2, 2, 2, 2),
        };
        private static readonly StyleBoxFlat _defaultStyle = new()
        {
            BackgroundColor = new Color(0.1f, 0.1f, 0.12f),
            BorderColor = new Color(0.2f, 0.2f, 0.25f),
            BorderThickness = new Thickness(1, 1, 1, 1),
        };

        public ArenaCostumeCard(ArenaCostumeOption costume, bool owned, bool equipped, bool canAfford)
        {
            CostumeIndex = costume.Index;
            MouseFilter = MouseFilterMode.Stop;
            MinHeight = 56;
            HorizontalExpand = true;
            PanelOverride = equipped ? _equippedStyle : _defaultStyle;

            if (!string.IsNullOrEmpty(costume.Description))
                ToolTip = Loc.GetString(costume.Description);

            var hbox = new BoxContainer
            {
                Orientation = BoxContainer.LayoutOrientation.Horizontal,
                HorizontalExpand = true,
                VerticalExpand = true,
            };

            if (!string.IsNullOrEmpty(costume.ItemPrototype))
            {
                var spriteView = new EntityPrototypeView
                {
                    MinSize = new Vector2(48, 48),
                    SetSize = new Vector2(48, 48),
                    HorizontalAlignment = HAlignment.Center,
                    VerticalAlignment = VAlignment.Center,
                    OverrideDirection = Direction.South,
                };
                spriteView.SetPrototype(costume.ItemPrototype);
                hbox.AddChild(spriteView);
            }

            var infoBox = new BoxContainer
            {
                Orientation = BoxContainer.LayoutOrientation.Vertical,
                HorizontalExpand = true,
                VerticalAlignment = VAlignment.Center,
                Margin = new Thickness(8, 0, 4, 0),
            };

            infoBox.AddChild(new Label { Text = Loc.GetString(costume.Name) });

            Button actionButton;
            if (!owned)
            {
                infoBox.AddChild(new Label
                {
                    Text = Loc.GetString("arena-costume-price", ("amount", costume.Price)),
                    FontColorOverride = new Color(0.85f, 0.8f, 0.4f),
                });

                actionButton = new Button
                {
                    Text = Loc.GetString("arena-costume-buy"),
                    Disabled = !canAfford,
                    VerticalAlignment = VAlignment.Center,
                };
                actionButton.OnPressed += _ => OnBuy?.Invoke(CostumeIndex);
            }
            else if (equipped)
            {
                infoBox.AddChild(new Label
                {
                    Text = Loc.GetString("arena-costume-owned"),
                    FontColorOverride = new Color(0.3f, 0.9f, 0.3f),
                });

                actionButton = new Button
                {
                    Text = Loc.GetString("arena-costume-unequip"),
                    VerticalAlignment = VAlignment.Center,
                };
                actionButton.OnPressed += _ => OnToggleEquip?.Invoke(CostumeIndex);
            }
            else
            {
                infoBox.AddChild(new Label
                {
                    Text = Loc.GetString("arena-costume-owned"),
                    FontColorOverride = new Color(0.3f, 0.9f, 0.3f),
                });

                actionButton = new Button
                {
                    Text = Loc.GetString("arena-costume-equip"),
                    VerticalAlignment = VAlignment.Center,
                };
                actionButton.OnPressed += _ => OnToggleEquip?.Invoke(CostumeIndex);
            }

            hbox.AddChild(infoBox);
            hbox.AddChild(actionButton);
            AddChild(hbox);
        }
    }
}
