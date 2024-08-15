using Content.Shared.CrewManifest;
using Content.Shared.StatusIcon;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;
using System.Numerics;
using Content.Shared.Roles;

namespace Content.Client.CrewManifest.UI;

public sealed class CrewManifestSection : BoxContainer
{
    public CrewManifestSection(
        IPrototypeManager prototypeManager,
        SpriteSystem spriteSystem,
        DepartmentPrototype section,
        List<CrewManifestEntry> entries)
    {
        Orientation = LayoutOrientation.Vertical;
        HorizontalExpand = true;

        AddChild(new Label()
        {
            StyleClasses = { "LabelBig" },
            Text = Loc.GetString($"department-{section.ID}")
        });

        var departmentContainer = new BoxContainer()
        {
            Orientation = LayoutOrientation.Horizontal,
            HorizontalExpand = true,
        };

        var namesContainer = new BoxContainer()
        {
            Orientation = LayoutOrientation.Vertical,
            HorizontalExpand = true,
            SizeFlagsStretchRatio = 3,
        };

        var titlesContainer = new BoxContainer()
        {
            Orientation = LayoutOrientation.Vertical,
            HorizontalExpand = true,
            SizeFlagsStretchRatio = 2,
        };

        departmentContainer.AddChild(namesContainer);
        departmentContainer.AddChild(titlesContainer);

        AddChild(departmentContainer);

        foreach (var entry in entries)
        {
            var nameContainer = new BoxContainer()
            {
                Orientation = LayoutOrientation.Horizontal,
                HorizontalExpand = true,
            };

            var name = new RichTextLabel();
            name.SetMessage(entry.Name);

            var gender = new RichTextLabel()
            {
                Margin = new Thickness(6, 0, 0, 0),
                StyleClasses = { "CrewManifestGender" }
            };
            gender.SetMessage(Loc.GetString("gender-display", ("gender", entry.Gender)));

            nameContainer.AddChild(name);
            nameContainer.AddChild(gender);

            var titleContainer = new BoxContainer()
            {
                Orientation = LayoutOrientation.Horizontal,
                HorizontalExpand = true,
                SizeFlagsStretchRatio = 1,
            };

            var title = new RichTextLabel();
            title.SetMessage(entry.JobTitle);


            if (prototypeManager.TryIndex<JobIconPrototype>(entry.JobIcon, out var jobIcon))
            {
                var icon = new TextureRect()
                {
                    TextureScale = new Vector2(2, 2),
                    VerticalAlignment = VAlignment.Center,
                    Texture = spriteSystem.Frame0(jobIcon.Icon),
                    Margin = new Thickness(0, 0, 4, 0)
                };

                titleContainer.AddChild(icon);
                titleContainer.AddChild(title);
            }
            else
            {
                titleContainer.AddChild(title);
            }

            namesContainer.AddChild(nameContainer);
            titlesContainer.AddChild(titleContainer);
        }
    }
}
