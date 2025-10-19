using Robust.Client.Input;
using Robust.Shared.Input;

namespace Content.Client.Options.UI.Tabs;

/// <summary> Simple arguments for keybinding process. </summary>
public sealed class SimpleKeyBindArguments(BoundKeyFunction function, IKeyBinding? binding)
    : IKeyBindArguments
{

    /// <inheritdoc />
    public BoundKeyFunction Function { get; } = function;

    /// <inheritdoc />
    public IKeyBinding? ExistingBinding { get; } = binding;
}
