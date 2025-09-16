using Robust.Client.GameObjects;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;
using System.Numerics;
using Content.Shared.Roles;

namespace Content.Client.ReadyManifest.UI;

public sealed class ReadyManifestEntry : GridContainer
{
    public ReadyManifestEntry(
        JobPrototype job,
        Dictionary<ProtoId<JobPrototype>, int> jobCounts,
        IPrototypeManager prototypeManager,
        SpriteSystem spriteSystem)
    {
        HorizontalExpand = true;
        Columns = 2;

        var jobContainer = new BoxContainer()
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
        };

        var title = new RichTextLabel()
        {
            HorizontalExpand = true
        };
        title.SetMessage(job.LocalizedName + ":");

        var icon = new TextureRect
        {
            TextureScale = new Vector2(2, 2),
            VerticalAlignment = VAlignment.Center,
            Margin = new Thickness(0, 0, 4, 0),
            Texture = spriteSystem.Frame0(prototypeManager.Index(job.Icon).Icon)
        };

        var readyCount = new RichTextLabel()
        {
            HorizontalExpand = true
        };

        var jobCount = jobCounts.ContainsKey(job.ID) ? jobCounts[job.ID] : 0;
        var color = jobCount > 0 ? Color.White : Color.Red;
        readyCount.SetMessage(jobCount.ToString(), null, color);

        jobContainer.AddChild(icon);
        jobContainer.AddChild(title);
        AddChild(jobContainer);
        AddChild(readyCount);
    }
}
