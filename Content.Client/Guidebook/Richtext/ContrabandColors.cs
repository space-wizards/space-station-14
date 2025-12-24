using System.Linq;
using System.Diagnostics.CodeAnalysis;
using Content.Shared.Contraband;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.RichText;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using Content.Shared.Localizations;

namespace Content.Client.Guidebook.RichText;

[UsedImplicitly]
public sealed class ContrabandColors : IMarkupTagHandler
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public string Name => "contrabandcolors";

    /// <inheritdoc/>
    public bool TryCreateControl(MarkupNode node, [NotNullWhen(true)] out Control? control)
    {
        var text = ContentLocalizationManager.FormatList([.. _prototypeManager.EnumeratePrototypes<ContrabandSeverityPrototype>().Select(x =>
        {
            return $"[color={x.ExamineColor}]{x.ID}[/color]";
        })]);

        var label = new RichTextLabel()
        {
            Text = text,
        };

        control = label;
        return true;
    }
}
