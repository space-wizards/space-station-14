namespace Content.Shared.QuickDialog;

/// <summary>
/// The buttons available in a quick dialog.
/// </summary>
[Flags]
public enum QuickDialogButtonFlags : byte
{
    /// <summary>
    ///
    /// </summary>
    OkButton = 1 << 0,

    /// <summary>
    ///
    /// </summary>
    CancelButton = 1 << 1,

    /// <summary>
    ///
    /// </summary>
    All = OkButton | CancelButton,
}
