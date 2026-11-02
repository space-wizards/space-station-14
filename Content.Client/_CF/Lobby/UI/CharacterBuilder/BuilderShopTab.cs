using Content.Shared.CF.Shop;
using Content.Shared.Clothing;
using Content.Shared.Preferences;
using Content.Shared.Preferences.Loadouts;
using Content.Shared.Roles;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Client.CF.Lobby.UI.CharacterBuilder;

/// <summary>
/// Character-creation shop. Buying adds a loadout the character spawns with.
/// Selling removes it and refunds the full price. In-game merchant rates are a later build.
/// </summary>
public sealed class BuilderShopTab : BoxContainer
{
    private readonly Label _purse;
    private readonly BoxContainer _rows;

    private HumanoidCharacterProfile? _profile;
    private JobPrototype? _jobOverride;
    private ICommonSession? _session;
    private IPrototypeManager? _prototypes;
    private Action<HumanoidCharacterProfile>? _onChanged;

    public BuilderShopTab()
    {
        Orientation = LayoutOrientation.Vertical;
        HorizontalExpand = true;
        VerticalExpand = true;

        _purse = new Label
        {
            FontColorOverride = Color.FromHex("#E6C15A"),
            Margin = new Thickness(10, 10, 10, 4),
        };
        AddChild(_purse);

        var scroll = new ScrollContainer
        {
            VerticalExpand = true,
            HorizontalExpand = true,
        };
        _rows = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            HorizontalExpand = true,
            Margin = new Thickness(10, 0, 10, 10),
            SeparationOverride = 4,
        };
        scroll.AddChild(_rows);
        AddChild(scroll);
    }

    public void Rebuild(
        HumanoidCharacterProfile? profile,
        JobPrototype? jobOverride,
        ICommonSession? session,
        IDependencyCollection? collection,
        IPrototypeManager prototypes,
        Func<HumanoidCharacterProfile?> getProfile,
        Action<HumanoidCharacterProfile> onChanged)
    {
        _ = collection;
        _ = getProfile;
        _profile = profile;
        _jobOverride = jobOverride;
        _session = session;
        _prototypes = prototypes;
        _onChanged = onChanged;
        Paint();
    }

    public void Apply(HumanoidCharacterProfile profile)
    {
        if (_prototypes == null || _onChanged == null)
            return;

        _profile = profile;
        Paint();
    }

    private void Paint()
    {
        _rows.RemoveAllChildren();
        if (_profile == null || _prototypes == null)
        {
            _purse.Text = CFCoins.FormatWallet(CFCoins.StartingCopper(1));
            return;
        }

        var job = _jobOverride ?? CFBuilderRole.PreferredJob(_profile, _prototypes);
        var remaining = CFCoins.Remaining(_profile, job.ID, _prototypes);
        _purse.Text = CFCoins.FormatWallet(remaining);

        if (!CFBuilderRole.TryGet(_profile, _jobOverride, _session, _prototypes, out var roleProto, out var roleLoadout))
        {
            _rows.AddChild(new Label { Text = Loc.GetString("cf-character-builder-no-role-loadout") });
            return;
        }

        var matched = false;
        foreach (var groupId in roleProto.Groups)
        {
            if (!_prototypes.Resolve(groupId, out var group))
                continue;

            if (group.Hidden)
                continue;

            if (!string.Equals(group.EditorTab, LoadoutEditorTabs.Weapons, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(group.EditorTab, LoadoutEditorTabs.Defense, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(group.EditorTab, LoadoutEditorTabs.Gear, StringComparison.OrdinalIgnoreCase))
                continue;

            var listed = false;
            foreach (var loadoutId in group.Loadouts)
            {
                if (!_prototypes.TryIndex(loadoutId, out var loadout))
                    continue;

                var price = CFCoins.LoadoutPrice(_prototypes, loadout);
                if (price <= 0)
                    continue;

                if (!listed)
                {
                    listed = true;
                    matched = true;
                    _rows.AddChild(new Label
                    {
                        Text = Loc.GetString(group.Name),
                        StyleClasses = { "LabelHeading" },
                        Margin = new Thickness(0, 8, 0, 4),
                    });
                }

                _rows.AddChild(OfferRow(roleLoadout, group.ID, loadout, price, remaining));
            }
        }

        if (!matched)
            _rows.AddChild(new Label { Text = Loc.GetString("cf-character-builder-no-loadouts") });
    }

    private Control OfferRow(RoleLoadout roleLoadout, string groupId, LoadoutPrototype loadout, int price, int remaining)
    {
        var owned = false;
        if (roleLoadout.SelectedLoadouts.TryGetValue(groupId, out var picks))
        {
            foreach (var pick in picks)
            {
                if (pick.Prototype == loadout.ID)
                {
                    owned = true;
                    break;
                }
            }
        }

        var row = new BoxContainer
        {
            Orientation = LayoutOrientation.Horizontal,
            HorizontalExpand = true,
            SeparationOverride = 8,
        };

        row.AddChild(new Label
        {
            Text = IoCManager.Resolve<IEntityManager>().System<LoadoutSystem>().GetName(loadout),
            HorizontalExpand = true,
            ClipText = true,
        });
        row.AddChild(new Label
        {
            Text = CFCoins.FormatPrice(price),
            MinWidth = 90,
            Align = Label.AlignMode.Right,
        });

        var button = new Button
        {
            Text = Loc.GetString(owned ? "cf-character-builder-shop-sell" : "cf-character-builder-shop-buy"),
            MinWidth = 70,
            Disabled = !owned && remaining < price,
        };
        var id = loadout.ID;
        var capturedGroup = groupId;
        button.OnPressed += _ => Commit(id, capturedGroup, add: !owned);
        row.AddChild(button);
        return row;
    }

    private void Commit(string loadoutId, string groupId, bool add)
    {
        if (_profile == null || _prototypes == null || _onChanged == null)
            return;

        if (!CFBuilderRole.TryGet(_profile, _jobOverride, _session, _prototypes, out _, out var roleLoadout))
            return;

        if (!_prototypes.TryIndex<LoadoutPrototype>(loadoutId, out var loadout))
            return;

        if (add)
        {
            var job = _jobOverride ?? CFBuilderRole.PreferredJob(_profile, _prototypes);
            var remaining = CFCoins.Remaining(_profile, job.ID, _prototypes);
            if (remaining < CFCoins.LoadoutPrice(_prototypes, loadout))
                return;

            if (!roleLoadout.AddLoadout(groupId, loadoutId, _prototypes))
                return;
        }
        else if (!roleLoadout.RemoveLoadout(groupId, loadoutId, _prototypes))
        {
            return;
        }

        _onChanged(_profile.WithLoadout(roleLoadout));
    }
}
