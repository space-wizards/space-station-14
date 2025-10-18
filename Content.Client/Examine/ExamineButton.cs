using Content.Shared.Verbs;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.Utility;
using Robust.Shared.Utility;

namespace Content.Client.Examine;

/// <summary>
///     Buttons that show up in the examine tooltip to specify more detailed
///     ways to examine an item.
/// </summary>
public sealed class ExamineButton : ContainerButton
{
    public const string StyleClassExamineButton = "examine-button";

    public const int ElementHeight = 32;
    public const int ElementWidth = 32;

    private const int Thickness = 4;

    public TextureRect Icon;

    public ExamineVerb Verb;
    private SpriteSystem _sprite;

    public ExamineButton(ExamineVerb verb, SpriteSystem spriteSystem)
    {
        Margin = new Thickness(Thickness, Thickness, Thickness, Thickness);

        SetOnlyStyleClass(StyleClassExamineButton);

        Verb = verb;
        _sprite = spriteSystem;

        if (verb.Disabled)
        {
            Disabled = true;
        }

        TooltipSupplier = sender =>
        {
            var label = new RichTextLabel();
            label.SetMessage(FormattedMessage.FromMarkupOrThrow(verb.Message ?? verb.Text));

            var tooltip = new Tooltip();
            tooltip.GetChild(0).Children.Clear();
            tooltip.GetChild(0).Children.Add(label);

            return tooltip;
        };

        Icon = new TextureRect
        {
            SetWidth = ElementWidth,
            SetHeight = ElementHeight
        };

        if (verb.Icon != null)
        {
            Icon.Texture = _sprite.Frame0(verb.Icon);
            Icon.Stretch = TextureRect.StretchMode.KeepAspectCentered;

            AddChild(Icon);
        }
    }
}
