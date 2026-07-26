using System.Linq;
using Content.Client.Lobby.UI.Roles;
using Content.Client.Stylesheets;
using Content.Shared.Traits;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;

namespace Content.Client.Lobby.UI;

public sealed partial class HumanoidProfileEditor
{

    /// <summary>
    /// Refreshes traits selector
    /// </summary>
    public void RefreshTraits()
    {
        TraitsList.RemoveAllChildren();

        var traits = _prototypeManager.EnumeratePrototypes<TraitPrototype>().OrderBy(t => Loc.GetString(t.Name)).ToList();
        TabContainer.SetTabTitle(3, Loc.GetString("humanoid-profile-editor-traits-tab"));

        if (traits.Count < 1)
        {
            TraitsList.AddChild(new Label
            {
                Text = Loc.GetString("humanoid-profile-editor-no-traits"),
                FontColorOverride = Color.Gray,
            });
            return;
        }

        // Setup model
        Dictionary<string, List<string>> traitGroups = new();
        List<string> defaultTraits = new();
        traitGroups.Add(TraitCategoryPrototype.Default, defaultTraits);

        foreach (var trait in traits)
        {
            if (trait.Category == null)
            {
                defaultTraits.Add(trait.ID);
                continue;
            }

            if (!_prototypeManager.HasIndex(trait.Category))
                continue;

            var group = traitGroups.GetOrNew(trait.Category);
            group.Add(trait.ID);
        }

        var firstCategory = false;
        var disableTraitLabel = new Label
        {
            Text = Loc.GetString("humanoid-profile-editor-antag-disable-trait"),
            Margin = new Thickness(0, 10, 0, 0),
            SizeFlagsStretchRatio = 3,
            HorizontalExpand = true,
        };


        // Create UI view from model
        foreach (var (categoryId, categoryTraits) in traitGroups)
        {
            TraitCategoryPrototype? category = null;

            if (categoryId != TraitCategoryPrototype.Default)
            {
                category = _prototypeManager.Index<TraitCategoryPrototype>(categoryId);
                // Label
                var box = new BoxContainer();
                box.AddChild(new Label
                {
                    Text = Loc.GetString(category.Name),
                    Margin = new Thickness(0, 10, 0, 0),
                    StyleClasses = { StyleClass.LabelHeading },
                });

                if (!firstCategory)
                {
                    firstCategory = true;
                    box.AddChild(disableTraitLabel);
                }

                TraitsList.AddChild(box);
            }

            List<TraitPreferenceSelector?> selectors = new();
            var selectionCount = 0;
            var i = 0;

            foreach (var traitProto in categoryTraits)
            {
                var trait = _prototypeManager.Index<TraitPrototype>(traitProto);
                var selector = new TraitPreferenceSelector(trait);
                var bgColor = i % 2 == 0 ? Color.FromHex("#292B38") : Color.FromHex("#2F2F3B");
                i++;

                selector.Container.PanelOverride = new StyleBoxFlat(bgColor);
                selector.Preference = Profile?.TraitPreferences.Contains(trait.ID) == true;
                selector.CheckboxAntagDisable.Visible = trait.AllowAntagDisable;
                selector.CheckboxAntagDisable.Disabled = !selector.Preference;

                if (selector.Preference)
                    selectionCount += trait.Cost;

                selector.AntagDisablePreference = Profile?.AntagDisableTraitPreferences.Contains(trait.ID) == true;

                selector.PreferenceChanged += preference =>
                {
                    if (preference)
                    {
                        selector.CheckboxAntagDisable.Disabled = true;
                        Profile = Profile?.WithTraitPreference(trait.ID, _prototypeManager);
                    }
                    else
                    {
                        selector.CheckboxAntagDisable.Disabled = false;
                        Profile?.AntagDisableTraitPreferences.Remove(trait.ID);
                        Profile = Profile?.WithoutTraitPreference(trait.ID, _prototypeManager);
                    }

                    SetDirty();
                    RefreshTraits(); // If too many traits are selected, they will be reset to the real value.
                };
                selector.AntagDisablePreferenceChanged += preference =>
                {
                    if (preference)
                    {
                        Profile?.AntagDisableTraitPreferences.Add(trait.ID);
                    }
                    else
                    {
                        Profile?.AntagDisableTraitPreferences.Remove(trait.ID);
                    }

                    SetDirty();
                };
                selectors.Add(selector);
            }

            // Selection counter
            if (category is { MaxTraitPoints: >= 0 })
            {
                TraitsList.AddChild(new Label
                {
                    Text = Loc.GetString("humanoid-profile-editor-trait-count-hint", ("current", selectionCount), ("max", category.MaxTraitPoints)),
                    FontColorOverride = Color.Gray
                });
            }

            foreach (var selector in selectors)
            {
                if (selector == null)
                    continue;

                if (category is { MaxTraitPoints: >= 0 } &&
                    selector.Cost + selectionCount > category.MaxTraitPoints)
                {
                    selector.Checkbox.Label.FontColorOverride = Color.Red;
                }

                if (selector.CheckboxAntagDisable.Visible)
                    disableTraitLabel.Visible = true;

                TraitsList.AddChild(selector);
            }
        }
    }
}
