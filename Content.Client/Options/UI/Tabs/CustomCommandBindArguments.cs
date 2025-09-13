using Robust.Client.Input;
using Robust.Shared.Input;

namespace Content.Client.Options.UI.Tabs;

/// <summary> Arguments for keybinding of custom command. </summary>
public sealed class CustomCommandBindArguments(
    BoundKeyFunction function,
    IKeyBinding? binding,
    string commandText
) : IKeyBindArguments
{
    /// <inheritdoc />
    public BoundKeyFunction Function { get; } = function;

    /// <inheritdoc />
    public IKeyBinding? ExistingBinding { get; } = binding;

    /// <summary>
    /// Text write into console when custom command is invoked.
    /// </summary>
    public string CommandText { get; } = commandText;
}
