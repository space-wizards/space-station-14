using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Robust.Client.UserInterface.StylesheetHelpers;

namespace Content.Client.Stylesheets;

public static class StylesheetHelpers
{
    public static MutableSelector Modulate(this MutableSelector selector, Color modulate)
    {
        return selector.Prop(Control.StylePropertyModulateSelf, modulate);
    }

    public static MutableSelector Margin(this MutableSelector selector, Thickness margin)
    {
        return selector.Prop(nameof(Control.Margin), margin);
    }

    public static MutableSelector Margin(this MutableSelector selector, float margin)
    {
        return selector.Margin(new Thickness(margin));
    }

    public static MutableSelector MinWidth(this MutableSelector selector, float width)
    {
        return selector.Prop(nameof(Control.MinWidth), width);
    }

    public static MutableSelector MinHeight(this MutableSelector selector, float height)
    {
        return selector.Prop(nameof(Control.MinHeight), height);
    }

    public static MutableSelector MinSize(this MutableSelector selector, Vector2 size)
    {
        return selector.MinWidth(size.X).MinHeight(size.Y);
    }

    public static MutableSelector MaxWidth(this MutableSelector selector, float width)
    {
        return selector.Prop(nameof(Control.MaxWidth), width);
    }

    public static MutableSelector MaxHeight(this MutableSelector selector, float height)
    {
        return selector.Prop(nameof(Control.MaxHeight), height);
    }

    public static MutableSelector MaxSize(this MutableSelector selector, Vector2 size)
    {
        return selector.MaxWidth(size.X).MaxHeight(size.Y);
    }

    public static MutableSelector SetWidth(this MutableSelector selector, float width)
    {
        return selector.Prop(nameof(Control.SetWidth), width);
    }

    public static MutableSelector SetHeight(this MutableSelector selector, float height)
    {
        return selector.Prop(nameof(Control.SetHeight), height);
    }

    public static MutableSelector SetSize(this MutableSelector selector, Vector2 size)
    {
        return selector.SetWidth(size.X).SetHeight(size.Y);
    }

    public static MutableSelector HorizontalExpand(this MutableSelector selector, bool val)
    {
        return selector.Prop(nameof(Control.HorizontalExpand), val);
    }

    public static MutableSelector VerticalExpand(this MutableSelector selector, bool val)
    {
        return selector.Prop(nameof(Control.VerticalExpand), val);
    }

    public static MutableSelector HorizontalAlignment(this MutableSelector selector, Control.HAlignment val)
    {
        return selector.Prop(nameof(Control.HorizontalExpand), val);
    }

    public static MutableSelector VerticalAlignment(this MutableSelector selector, Control.VAlignment val)
    {
        return selector.Prop(nameof(Control.VerticalExpand), val);
    }

    public static MutableSelector AlignMode(this MutableSelector selector, Label.AlignMode mode)
    {
        return selector.Prop(Label.StylePropertyAlignMode, mode);
    }

    // Pseudo class helpers

    public static MutableSelectorElement PseudoNormal(this MutableSelectorElement selector)
    {
        return selector.Pseudo(ContainerButton.StylePseudoClassNormal);
    }

    public static MutableSelectorElement PseudoHovered(this MutableSelectorElement selector)
    {
        return selector.Pseudo(ContainerButton.StylePseudoClassHover);
    }

    public static MutableSelectorElement PseudoPressed(this MutableSelectorElement selector)
    {
        return selector.Pseudo(ContainerButton.StylePseudoClassPressed);
    }

    public static MutableSelectorElement PseudoDisabled(this MutableSelectorElement selector)
    {
        return selector.Pseudo(ContainerButton.StylePseudoClassDisabled);
    }

    public static MutableSelectorElement MaybeClass(this MutableSelectorElement selector, string? styleclass)
    {
        if (styleclass is { } c)
            return selector.Class(c);

        return selector;
    }

    public static MutableSelectorElement E<T>() where T : Control
    {
        return new MutableSelectorElement { Type = typeof(T) };
    }

    public static MutableSelectorElement E()
    {
        return new MutableSelectorElement();
    }

    public static MutableSelector Panel(this MutableSelector selector, StyleBox panel)
    {
        return selector.Prop(PanelContainer.StylePropertyPanel, panel);
    }

    public static MutableSelector Box(this MutableSelector selector, StyleBox box)
    {
        return selector.Prop(ContainerButton.StylePropertyStyleBox, box);
    }

    public static MutableSelector Font(this MutableSelector selector, Font font)
    {
        return selector.Prop(Label.StylePropertyFont, font);
    }

    public static MutableSelector FontColor(this MutableSelector selector, Color fontColor)
    {
        return selector.Prop(Label.StylePropertyFontColor, fontColor);
    }

    public static StyleBoxTexture IntoPatch(this Texture texture, StyleBox.Margin patchMargin, float amount)
    {
        var stylebox = new StyleBoxTexture
        {
            Texture = texture,
        };
        stylebox.SetPatchMargin(patchMargin, amount);

        return stylebox;
    }

    public static MutableSelectorChild ParentOf(this MutableSelector selector, MutableSelector other)
    {
        return Child().Parent(selector).Child(other);
    }
}
