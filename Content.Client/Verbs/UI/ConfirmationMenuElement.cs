using Content.Client.ContextMenu.UI;
using Content.Shared.Verbs;

namespace Content.Client.Verbs.UI;

public partial class ConfirmationMenuElement : ContextMenuElement
{
    public readonly Verb Verb;
    public readonly VerbType Type;

    public ConfirmationMenuElement(Verb verb, string? text, VerbType type) : base(text)
    {
        Verb = verb;
        Type = type;
        SubMenu = null;
    }
}
