using Robust.Client.Input;
using Robust.Shared.Input;

namespace Content.Client.Options.UI.Tabs;

/// <summary> Generalized interface of keybinding control. </summary>
public interface IKeyBindingControl
{
    /// <summary>
    /// Function, keybinding for which this control manages.
    /// </summary>
    BoundKeyFunction Function { get; }

    /// <summary> Binding option 1. </summary>
    IKeyBinding? Bind1 { get; }

    /// <summary> Binding option 2. </summary>
    IKeyBinding? Bind2 { get; }

    /// <summary> Update binding button text. </summary>
    void UpdateBindText();

    /// <summary> Update keybinding related data. </summary>
    void UpdateData(IReadOnlyList<IKeyBinding> activeBinds, bool isModified);
}
