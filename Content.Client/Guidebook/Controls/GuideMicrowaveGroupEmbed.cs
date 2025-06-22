using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Client.Guidebook.Richtext;
using Content.Shared.Kitchen;
using JetBrains.Annotations;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;

namespace Content.Client.Guidebook.Controls;

/// <summary>
/// Control for listing microwave recipes in a guidebook
/// </summary>
[UsedImplicitly]
public sealed partial class GuideMicrowaveGroupEmbed : BoxContainer, IDocumentTag
{
    [Dependency] private readonly ILogManager _logManager = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private readonly ISawmill _sawmill;

    public GuideMicrowaveGroupEmbed()
    {
        Orientation = LayoutOrientation.Vertical;
        IoCManager.InjectDependencies(this);
        _sawmill = _logManager.GetSawmill("guidebook.microwave_group");
        MouseFilter = MouseFilterMode.Stop;
    }

    public GuideMicrowaveGroupEmbed(string group) : this()
    {
        CreateEntries(group);
    }

    public bool TryParseTag(Dictionary<string, string> args, [NotNullWhen(true)] out Control? control)
    {
        control = null;
        if (!args.TryGetValue("Group", out var group))
        {
            _sawmill.Error("Microwave group embed tag is missing group argument");
            return false;
        }

        CreateEntries(group);

        control = this;
        return true;
    }

    private void CreateEntries(string group)
    {
        var prototypes = _prototype.EnumeratePrototypes<FoodRecipePrototype>()
            .Where(p => p.Group.Equals(group))
            .OrderBy(p => p.Name);

        foreach (var recipe in prototypes)
        {
            var embed = new GuideMicrowaveEmbed(recipe);
            AddChild(embed);
        }
    }
}
