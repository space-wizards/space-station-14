using Content.Client.Guidebook.RichText;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using Content.Client.UserInterface.ControlExtensions;

namespace Content.Client.Guidebook.Controls;

/// <summary>
/// A RichTextLabel which is a link to a specified IPrototype.
/// The link is activated by the owner if the prototype is represented
/// somewhere in the same document.
/// </summary>
public sealed class GuidebookRichPrototypeLink : Control, IPrototypeLinkControl
{
    private bool _linkActive = false;
    private FormattedMessage? _message;
    private readonly RichTextLabel _richTextLabel;

    public void EnablePrototypeLink()
    {
        if (_message == null)
            return;

        _linkActive = true;

        DefaultCursorShape = CursorShape.Hand;

        _richTextLabel.SetMessage(_message, null, TextLinkTag.LinkColor);
    }

    public GuidebookRichPrototypeLink() : base()
    {
        MouseFilter = MouseFilterMode.Pass;
        OnKeyBindDown += HandleClick;
        _richTextLabel = new RichTextLabel();
        AddChild(_richTextLabel);
    }

    public void SetMessage(FormattedMessage message)
    {
        _message = message;
        _richTextLabel.SetMessage(_message, tagsAllowed: null);
    }

    public IPrototype? LinkedPrototype { get; set; }

    private void HandleClick(GUIBoundKeyEventArgs args)
    {
        if (!_linkActive)
            return;

        if (args.Function != EngineKeyFunctions.UIClick)
            return;

        if (this.TryGetParentHandler<IAnchorClickHandler>(out var handler))
        {
            handler.HandleAnchor(this);
            args.Handle();
        }
        else
            Logger.Warning("Warning! No valid IAnchorClickHandler found.");
    }
}

public interface IAnchorClickHandler
{
    public void HandleAnchor(IPrototypeLinkControl prototypeLinkControl);
}
