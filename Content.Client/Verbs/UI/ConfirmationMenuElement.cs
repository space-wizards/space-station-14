using Content.Client.ContextMenu.UI;
using Content.Shared.Verbs;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Content.Client.Verbs.UI;

public partial class ConfirmationMenuElement : ContextMenuElement
{
    public const string StyleClassConfirmationContextMenuButton = "confirmationContextMenuButton";

    public readonly Verb Verb;
    public readonly VerbType Type;

    public override string Text
    {
        set
        {
            var message = new FormattedMessage();
            message.PushColor(Color.White);
            message.AddMarkupPermissive(value.Trim());
            Label.SetMessage(message);
        }
    }

    public ConfirmationMenuElement(Verb verb, string? text, VerbType type) : base(text)
    {
        Verb = verb;
        Type = type;
        Icon.Visible = false;

        SetOnlyStyleClass(StyleClassConfirmationContextMenuButton);
    }
}
