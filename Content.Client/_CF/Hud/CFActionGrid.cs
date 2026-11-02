using System.Numerics;
using Content.Client.UserInterface.Systems.Actions.Controls;
using Content.Shared.Input;
using Robust.Client.Input;
using Robust.Client.UserInterface;
using Robust.Shared.Maths;

namespace Content.Client.CF.Hud;

/// <summary>
/// Two rows of ten slots. The first row is keys 1–0. The second row uses the same numbers
/// with whatever modifier the player bound, and the label shows only the key.
/// </summary>
public sealed class CFActionGrid : ActionButtonContainer
{
    private const int ColumnCount = 10;
    private const float Slot = 64f;
    private const float Separation = 2f;

    private readonly IInputManager _keys;

    public CFActionGrid()
    {
        MinimumButtons = ContentKeyFunctions.GetHotbarBoundKeys().Length;
        MouseFilter = MouseFilterMode.Stop;
        _keys = IoCManager.Resolve<IInputManager>();
        _keys.OnKeyBindingAdded += OnKeybindsChanged;
        _keys.OnKeyBindingRemoved += OnKeybindsChanged;
    }

    protected override string FormatKeyLabel(int index, IKeyBinding binding)
    {
        var text = binding.GetKeyString();
        if (index < ColumnCount)
            return text;

        var plus = text.LastIndexOf('+');
        return plus >= 0 && plus < text.Length - 1 ? text[(plus + 1)..] : text;
    }

    protected override Vector2 MeasureOverride(Vector2 availableSize)
    {
        foreach (var child in Children)
            child.Measure(new Vector2(Slot, Slot));

        var rows = Math.Max(1, (ChildCount + ColumnCount - 1) / ColumnCount);
        return new Vector2(
            ColumnCount * Slot + (ColumnCount - 1) * Separation,
            rows * Slot + (rows - 1) * Separation);
    }

    protected override Vector2 ArrangeOverride(Vector2 finalSize)
    {
        for (var i = 0; i < ChildCount; i++)
        {
            var column = i % ColumnCount;
            var row = i / ColumnCount;
            GetChild(i).Arrange(UIBox2.FromDimensions(
                column * (Slot + Separation),
                row * (Slot + Separation),
                Slot,
                Slot));
        }

        return finalSize;
    }

    private void OnKeybindsChanged(IKeyBinding _)
    {
        var keys = ContentKeyFunctions.GetHotbarBoundKeys();
        for (var i = 0; i < ChildCount && i < keys.Length; i++)
        {
            if (GetChild(i) is not ActionButton button)
                continue;

            if (_keys.TryGetKeyBinding(keys[i], out var binding))
                button.Label.Text = FormatKeyLabel(i, binding);
        }
    }
}
