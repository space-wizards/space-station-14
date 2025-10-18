using Robust.Client.Input;
using Robust.Shared.Input;

namespace Content.Client.Options.UI.Tabs;

/// <summary>
/// Parameters, required for generic keybinding action.
/// </summary>
public interface IKeyBindArguments
{
    /// <summary>
    /// Function to which keybinding should be applied.
    /// </summary>
    BoundKeyFunction Function { get; }

    /// <summary>
    /// Keybinding that was attached to function previously and needs to be overwritten.
    /// </summary>
    IKeyBinding? ExistingBinding { get; }
}
