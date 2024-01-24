using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;

namespace Content.Client.UserInterface.Controls;

/// <summary>
/// Character counter is to be used with either TextEdit or LineEdit control.
/// </summary>
/// <remarks>
/// Don't forget to bind it with <code>Edit.OnTextChange += CharacterCounter.HandleTextChanged</code>
/// </remarks>
public sealed class CharacterCounter : Control
{
    [Dependency] private readonly ILocalizationManager _loc = default!;

    /// <summary>
    /// If specified, the control displays the remaining length instead of the total length.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public int? MaxLength;

    /// <summary>
    /// If specified, the control hides itself when there are more than this
    /// number of characters remains to reach the <see cref="MaxLength"/>.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public int? MaxLengthVisibleRemaining = 20;

    private readonly Label _label = new()
    {
        Visible = false,
        HorizontalExpand = true,
        HorizontalAlignment = HAlignment.Right,
        MouseFilter = MouseFilterMode.Pass
    };

    public CharacterCounter()
    {
        IoCManager.InjectDependencies(this);
        AddChild(_label);
    }

    public void HandleTextChanged(TextEdit.TextEditEventArgs args)
    {
        HandleTextLengthChanged(Rope.CalcTotalLength(args.TextRope));
    }

    public void HandleTextChanged(LineEdit.LineEditEventArgs args)
    {
        HandleTextLengthChanged(args.Text.Length);
    }

    private void HandleTextLengthChanged(long length)
    {
        if (MaxLength is not null)
        {
            var remaining = MaxLength - length;
            var isTooLong = remaining < 0;

            _label.Visible = remaining <= MaxLengthVisibleRemaining;
            _label.Text = $"{remaining}";
            _label.FontColorOverride = isTooLong ? Color.Red : null;
            _label.ToolTip = isTooLong
                ? _loc.GetString("ui-character-counter-too-long-tooltip")
                : _loc.GetString("ui-character-counter-remaining-tooltip", ("remaining", remaining));
        }
        else
        {
            _label.Visible = true;
            _label.Text = $"{length}";
            _label.FontColorOverride = null;
            _label.ToolTip = _loc.GetString("ui-character-counter-length-tooltip", ("length", length));
        }
    }
}
