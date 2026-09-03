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
public sealed partial class ConfirmButton : Button
{
    [Dependency] private IGameTiming _gameTiming = default!;

    public const string ConfirmPrefix = "confirm-";

    /// <summary>
    /// The time when the button will revert from confirming if left unpressed.
    /// </summary>
    private TimeSpan? _nextReset;
    /// <summary>
    /// The time when the button should re-enable itself (to avoid debouncing).
    /// </summary>
    private TimeSpan? _nextCooldown;
    private bool _isConfirming;
    private string? _confirmationText;
    private string? _text;

    /// <summary>
    /// Fired when the button was pressed and confirmed
    /// </summary>
    public new event Action<ButtonEventArgs>? OnPressed;

    /// <summary>
    /// Fired when the button has started to confirm and is awaiting a second button press.
    /// </summary>
    public event Action<ButtonEventArgs>? OnConfirming;

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
            UpdateText();
        }
    }

    /// <inheritdoc cref="BaseButton.Disabled"/>
    /// <remarks>
    /// Overrides the confirming state of the button when set.
    /// Intended for public use, should not be used inside the class due to IsConfirming side-effects.
    /// </remarks>
    public new bool Disabled
    {
        get => base.Disabled;
        set
        {
            // Force out of confirming status (may change base.Disabled), then write your new disabled value.
            SetIsConfirming(false);
            SetDisabled(value);
        }
    }

    /// <summary>
    /// The text displayed on the button when waiting for a second click
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public string ConfirmationText
    {
        get => _confirmationText ?? Loc.GetString("generic-confirm");
        set
        {
            _confirmationText = value;
            UpdateText();
        }
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

    /// <summary>
    /// A property to get or change whether the button is confirming (awaiting a second press within a time limit) or not
    /// </summary>
    [ViewVariables]
    public bool IsConfirming
    {
        get => _isConfirming;
        set
        {
            if (_isConfirming != value)
                SetIsConfirming(value);
        }
    }

    public ConfirmButton()
    {
        IoCManager.InjectDependencies(this);

        base.OnPressed += HandleOnPressed;
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        if (!IsConfirming)
            return;

        if (_gameTiming.CurTime > _nextReset)
            SetIsConfirming(false);
        else if (Disabled && _gameTiming.CurTime > _nextCooldown)
            SetDisabled(false);
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
        }
        else
        {
            base.DrawModeChanged();
        }
    }

    private void HandleOnPressed(ButtonEventArgs buttonEvent)
    {
        //Prevent accidental confirmations from double clicking
        if (IsConfirming && _nextCooldown > _gameTiming.CurTime)
            return;

        // Update the state machine before invoking our events.
        SetIsConfirming(!IsConfirming);

        switch (IsConfirming)
        {
            case true:
                OnConfirming?.Invoke(buttonEvent);
                break;
            case false:
                OnPressed?.Invoke(buttonEvent);
                break;
        }
    }

    /// <summary>
    /// Updates the text shown on the button depending on whether the button is confirming or not.
    /// </summary>
    private void UpdateText()
    {
        base.Text = IsConfirming ? ConfirmationText : Text;
    }

    /// <summary>
    /// Internal setter logi
    /// </summary>
    private void SetDisabled(bool value)
    {
        base.Disabled = value;
    }

    /// <summary>
    /// Internal setter logic for <see cref="IsConfirming"/>
    /// Sets the text, the timers, and the disabled state.
    /// </summary>
    private void SetIsConfirming(bool value)
    {
        _isConfirming = value;

        // Update button visuals.
        UpdateText();
        DrawModeChanged();

        if (value)
        {
            // Start our timers, disable button until cooldown.
            SetDisabled(true);
            _nextCooldown = _gameTiming.CurTime + CooldownTime;
            _nextReset = _gameTiming.CurTime + ResetTime;
        }
        else
        {
            // Clear timers, ensure button is enabled if it was disabled before (previous SetIsConfirming(true) call => valid reset timer)
            if (Disabled && _nextReset != null)
                SetDisabled(false);
            _nextReset = null;
            _nextCooldown = null;
        }
    }
}
