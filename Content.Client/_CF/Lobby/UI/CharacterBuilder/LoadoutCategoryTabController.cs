using Content.Shared.CF.Shop;
using Content.Shared.Clothing;
using Content.Shared.Preferences;
using Content.Shared.Preferences.Loadouts;
using Content.Shared.Roles;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Client.CF.Lobby.UI.CharacterBuilder;

/// <summary>
/// Weapons, Defense, and Gear list only what this character bought. Selling refunds the full price.
/// </summary>
public sealed class LoadoutCategoryTabController
{
    private readonly BoxContainer _groups;
    private readonly string _category;

    private HumanoidCharacterProfile? _profile;
    private JobPrototype? _jobOverride;
    private ICommonSession? _session;
    private IPrototypeManager? _prototypes;
    private Action<HumanoidCharacterProfile>? _onChanged;

    public LoadoutCategoryTabController(BoxContainer groups, string category)
    {
        _groups = groups;
        _category = category;
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
        _groups.RemoveAllChildren();
        if (_profile == null || _prototypes == null)
            return;

        if (!CFBuilderRole.TryGet(_profile, _jobOverride, _session, _prototypes, out var roleProto, out var roleLoadout))
        {
            _groups.AddChild(new Label
            {
                Text = Loc.GetString("cf-character-builder-no-role-loadout"),
            });
            return;
        }

        var matched = false;
        var anyOwned = false;
        foreach (var groupId in roleProto.Groups)
        {
            if (!_prototypes.Resolve(groupId, out var group))
                continue;

            if (group.Hidden || !string.Equals(group.EditorTab, _category, StringComparison.OrdinalIgnoreCase))
                continue;

            matched = true;
            if (!roleLoadout.SelectedLoadouts.TryGetValue(group.ID, out var picks) || picks.Count == 0)
                continue;

            anyOwned = true;
            _groups.AddChild(new Label
            {
                Text = Loc.GetString(group.Name),
                StyleClasses = { "LabelHeading" },
                Margin = new Thickness(0, 8, 0, 4),
            });

            foreach (var pick in picks)
            {
                if (!_prototypes.TryIndex(pick.Prototype, out var loadout))
                    continue;

                _groups.AddChild(OwnedRow(group.ID, loadout));
            }
        }

        if (!matched)
        {
            _groups.AddChild(new Label
            {
                Text = Loc.GetString("cf-character-builder-no-loadouts"),
            });
        }
        else if (!anyOwned)
        {
            _groups.AddChild(new Label
            {
                Text = Loc.GetString("cf-character-builder-owned-empty"),
                Margin = new Thickness(0, 8, 0, 0),
            });
        }
    }

    private Control OwnedRow(string groupId, LoadoutPrototype loadout)
    {
        var row = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
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
            Text = CFCoins.FormatPrice(CFCoins.LoadoutPrice(_prototypes!, loadout)),
            MinWidth = 90,
            Align = Label.AlignMode.Right,
        });

        var sell = new Button
        {
            Text = Loc.GetString("cf-character-builder-shop-sell"),
            MinWidth = 70,
        };
        var id = loadout.ID;
        var capturedGroup = groupId;
        sell.OnPressed += _ => Commit(id, capturedGroup);
        row.AddChild(sell);
        return row;
    }

    private void Commit(string loadoutId, string groupId)
    {
        if (_profile == null || _prototypes == null || _onChanged == null)
            return;

        if (!CFBuilderRole.TryGet(_profile, _jobOverride, _session, _prototypes, out _, out var roleLoadout))
            return;

        if (!roleLoadout.RemoveLoadout(groupId, loadoutId, _prototypes))
            return;

        _onChanged(_profile.WithLoadout(roleLoadout));
    }
}
