using Robust.Client.UserInterface.Controls;
using Robust.Shared.Timing;

namespace Content.Client.UserInterface.Controls;

/// <summary>
/// A Button that requires a second click to actually invoke its OnPressed action. <br/>
/// When clicked once it will change rendering modes to be prefixed by <see cref="ConfirmPrefix"/>
/// and displays <see cref="ConfirmationText"/> on the button instead of <see cref="Text"/>.<br/>
/// <br/>
/// After the first click <see cref="CooldownTime"/> needs to elapse before it can be clicked again to confirm.<br/>
/// When the button doesn't get clicked a second time before <see cref="ResetTime"/> passes it changes back to its normal state.<br/>
/// </summary>
/// <remarks>
/// Colors for the different states need to be set in the stylesheet
/// </remarks>
public sealed class ConfirmButton : Button
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    public const string ConfirmPrefix = "confirm-";

    private TimeSpan? _nextReset;
    private TimeSpan? _nextCooldown;
    private string? _confirmationText;
    private string? _text;

    /// <summary>
    /// Fired when the button was pressed and confirmed
    /// </summary>
    public new event Action<ButtonEventArgs>? OnPressed;

    /// <inheritdoc cref="Button.Text"/>
    /// <remarks>
    /// Hides the buttons text property to be able to sanely replace the button text with
    /// <see cref="_confirmationText"/> when asking for confirmation
    /// </remarks>
    public new string? Text
    {
        get => _text;
        set
        {
            _text = value;
            base.Text = IsConfirming ? _confirmationText : value;
        }
    }

    /// <summary>
    /// The text displayed on the button when waiting for a second click
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public string ConfirmationText
    {
        get => _confirmationText ?? Loc.GetString("generic-confirm");
        set => _confirmationText = value;
    }

    /// <summary>
    /// The time until the button reverts to normal
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan ResetTime { get; set; } = TimeSpan.FromSeconds(2);

    /// <summary>
    /// The time until the button accepts a second click. This is to prevent accidentally confirming the button
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan CooldownTime { get; set; } = TimeSpan.FromSeconds(.5);

    [ViewVariables]
    public bool IsConfirming = false;

    public ConfirmButton()
    {
        IoCManager.InjectDependencies(this);

        base.OnPressed += HandleOnPressed;
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        if (IsConfirming && _gameTiming.CurTime > _nextReset)
        {
            IsConfirming = false;
            base.Text = Text;
            DrawModeChanged();
        }

        if (Disabled && _gameTiming.CurTime > _nextCooldown)
            Disabled = false;
    }

    protected override void DrawModeChanged()
    {
        if (IsConfirming)
        {
            switch (DrawMode)
            {
                case DrawModeEnum.Normal:
                    SetOnlyStylePseudoClass(ConfirmPrefix + StylePseudoClassNormal);
                    break;
                case DrawModeEnum.Pressed:
                    SetOnlyStylePseudoClass(ConfirmPrefix + StylePseudoClassPressed);
                    break;
                case DrawModeEnum.Hover:
                    SetOnlyStylePseudoClass(ConfirmPrefix + StylePseudoClassHover);
                    break;
                case DrawModeEnum.Disabled:
                    SetOnlyStylePseudoClass(ConfirmPrefix + StylePseudoClassDisabled);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            return;
        }

        base.DrawModeChanged();
    }

    private void HandleOnPressed(ButtonEventArgs buttonEvent)
    {
        //Prevent accidental confirmations from double clicking
        if (IsConfirming && _nextCooldown > _gameTiming.CurTime)
            return;

        switch (IsConfirming)
        {
            case false:
                _nextCooldown  = _gameTiming.CurTime + CooldownTime;
                _nextReset = _gameTiming.CurTime + ResetTime;
                Disabled = true;
                break;
            case true:
                OnPressed?.Invoke(buttonEvent);
                break;
        }

        base.Text = IsConfirming ? Text : ConfirmationText;

        IsConfirming = !IsConfirming;
    }
}
