using System.Numerics;
using Content.Client.Stylesheets;
using Content.Client.UserInterface.Controls;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Prototypes;
using static Robust.Client.UserInterface.Control;
using static Robust.Client.UserInterface.Controls.BaseButton;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client._Starlight.UI;
internal sealed class SLWindow : DefaultWindow
{
    private readonly IStylesheetManager _stylesheetManager = default!;
    internal SLWindow()
    {
        _stylesheetManager = IoCManager.Resolve<IStylesheetManager>();
        Stylesheet = _stylesheetManager.Starlight;
        CloseButton.Stylesheet = Stylesheet;
        CloseButton.AddStyleClass("CrossButtonRed");
    }
    public SLWindow Style(Func<IStylesheetManager, Stylesheet> func)
    {
        Stylesheet = func(_stylesheetManager);
        return this;
    }
    public SLWindow Grid(int columns, Action<SLGrid> action)
    {
        var grid = new SLGrid(columns)
        {
            Stylesheet = Stylesheet
        };
        action(grid);
        Contents.AddChild(grid);
        return this;
    }
    public SLWindow Scroll(Action<SLScroll> action)
    {
        var scroll = new SLScroll()
        {
            Stylesheet = Stylesheet
        };
        action(scroll);
        Contents.AddChild(scroll);
        return this;
    }
    public SLWindow SelectBox<T>(Func<T, string> render, Action<SLSelect<T>> action)
    {
        var select = new SLSelect<T>(render)
        {
            Stylesheet = Stylesheet
        };
        action(select);
        Contents.AddChild(select);
        return this;
    }
    public SLWindow Box(LayoutOrientation orientation, Action<SLBox> action)
    {
        var select = new SLBox(orientation)
        {
            Stylesheet = Stylesheet
        };
        action(select);
        Contents.AddChild(select);
        return this;
    }
}
public sealed class SLBox : BoxContainer
{
    public SLBox(LayoutOrientation orientation) => Orientation = orientation;
}
public sealed class SLLayout : LayoutContainer
{
}

public sealed class SLGrid : GridContainer
{
    internal SLGrid(int columns)
    {
        Columns = columns;
        HorizontalAlignment = HAlignment.Stretch;
    }
}
public  sealed class SLSelect<T> : OptionButton
{
    private List<T> _items = [];
    private readonly Func<T, string> _render = null!;
    private event Action<T> _onItemSelected = delegate { };
    internal SLSelect(Func<T, string> render)
    {
        _render = render;
        _onItemSelected += (item) => _items.Remove(item);
    }

    public List<T> Items
    {
        get => _items;
        set
        {
            _items = value;
            StateHasChanged();
        }
    }
    public SLSelect<T> SetItems(List<T> items)
    {
        Items = items;
        return this;
    }
    public SLSelect<T> StateHasChanged()
    {
        var i = -1;
        foreach (var item in _items)
            AddItem(_render(item), ++i);
        return this;
    }
    public SLSelect<T> Bind(Action<T> handler)
    {
        _onItemSelected += handler;
        return this;
    }
}
public sealed class SLStripe : StripeBack
{
}
public sealed class SLTextureRect : TextureRect
{
}
public sealed class SLLayeredTextureRect : LayeredTextureRect
{
}

public sealed class SLScroll : ScrollContainer
{
}
public sealed class SLPanel : PanelContainer
{
}
[Virtual]
public class SLButton : Button
{
}
public sealed class SLButtonWithShader : Button
{
    public ShaderInstance? ShaderInstance { get; private set; }
    public SLButtonWithShader WithShader(ProtoId<ShaderPrototype> shader)
    {
        if(!IoCManager.Resolve<IPrototypeManager>().TryIndex(shader, out var shaderProto))
            return this;
        ShaderInstance = shaderProto.Instance();
        return this;
    }
    public SLButtonWithShader WithShader(ShaderInstance shader)
    {
        ShaderInstance = shader;
        return this;
    }
    protected override void Draw(IRenderHandle renderHandle)
    {
        renderHandle.DrawingHandleScreen.UseShader(ShaderInstance);
        base.Draw(renderHandle);
    }
}
public sealed class SLTextureButton : TextureButton
{
}
public sealed class Subscription(Action onDisposed) : IDisposable
{
    public void Dispose() => onDisposed?.Invoke();
}
public sealed class SLLabel : Label
{
    public SLLabel WithText(string text)
    {
        Text = text;
        return this;
    }
    public SLLabel WithFont(string path, int size)
    {
        var font = new VectorFont(IoCManager.Resolve<IResourceCache>().GetResource<FontResource>(path), size);
        FontOverride = font;
        return this;
    }
}
public sealed class SLRichTextLabel : RichTextLabel
{
    public RichTextLabel WithText(string text)
    {
        Text = text;
        return this;
    }
}
public static class ControlHelper
{
    public static T SaveTo<T>(this T control, Action<T> action) where T : Control
    {
        action(control);
        return control;
    }
    public static TextureButton OnClick(this TextureButton button, Action action)
    {
        button.OnPressed += _ => action();
        return button;
    }
}

public static class SLControlExtensions
{
    public static BaseButton WhenPressed(this BaseButton parent, Action<ButtonEventArgs> OnPressed, Action<IDisposable>? subscription = null)
    {
        parent.OnPressed += OnPressed;
        subscription?.Invoke(new Subscription(() => parent.OnPressed -= OnPressed));
        return parent;
    }
    public static BaseButton WhenMouseEntered(this BaseButton parent, Action<GUIMouseHoverEventArgs> OnPressed, Action<IDisposable>? subscription = null)
    {
        parent.OnMouseEntered += OnPressed;
        subscription?.Invoke(new Subscription(() => parent.OnMouseEntered -= OnPressed));
        return parent;
    }
    public static BaseButton WhenMouseExited(this BaseButton parent, Action<GUIMouseHoverEventArgs> OnPressed, Action<IDisposable>? subscription = null)
    {
        parent.OnMouseExited += OnPressed;
        subscription?.Invoke(new Subscription(() => parent.OnMouseEntered -= OnPressed));
        return parent;
    }
    public static Control Add(this Control parent, Control control)
    {
        parent.AddChild(control);
        return parent;
    }
    public static Control Grid(this Control parent, int columns, Action<SLGrid> action)
    {
        var grid = new SLGrid(columns);
        action(grid);
        parent.AddChild(grid);
        return parent;
    }
    public static Control Box(this Control parent, LayoutOrientation orientation, Action<SLBox> action)
    {
        var select = new SLBox(orientation);
        action(select);
        parent.AddChild(select);
        return parent;
    }
    public static Control Layout(this Control parent, Action<SLLayout> action)
    {
        var select = new SLLayout();
        action(select);
        parent.AddChild(select);
        return parent;
    }
    public static Control TextureRect(this Control parent, Action<SLTextureRect> action)
    {
        var select = new SLTextureRect();
        action(select);
        parent.AddChild(select);
        return parent;
    }
    public static Control LayeredTextureRect(this Control parent, Action<SLLayeredTextureRect> action)
    {
        var select = new SLLayeredTextureRect();
        action(select);
        parent.AddChild(select);
        return parent;
    }
    public static Control Button(this Control parent, Action<SLButton> action)
    {
        var select = new SLButton();
        action(select);
        parent.AddChild(select);
        return parent;
    }

    public static Control Panel(this Control parent, Action<SLPanel> action)
    {
        var select = new SLPanel();
        action(select);
        parent.AddChild(select);
        return parent;
    }
    public static Control Label(this Control parent, Action<SLLabel> action)
    {
        var select = new SLLabel();
        action(select);
        parent.AddChild(select);
        return parent;
    }
    public static Control RichText(this Control parent, Action<SLRichTextLabel> action)
    {
        var select = new SLRichTextLabel();
        action(select);
        parent.AddChild(select);
        return parent;
    }
    public static Control SelectBox<T>(this Control parent, Func<T, string> render, Action<SLSelect<T>> action)
    {
        var select = new SLSelect<T>(render);
        action(select);
        parent.AddChild(select);
        return parent;
    }
    public static Control AddChildren(this Control parent, IEnumerable<Control> controls)
    {
        foreach (var control in controls)
            parent.AddChild(control);
        return parent;
    }
    public static Control WithVerticalExp(this Control parent)
    {
        parent.VerticalExpand = true;
        return parent;
    }
    public static Control WithHorizontalExp(this Control parent)
    {
        parent.HorizontalExpand = true;
        return parent;
    }
    public static Control WithVAlignment(this Control parent, VAlignment alignment)
    {
        parent.VerticalAlignment = alignment;
        return parent;
    }
    public static Control WithMargin(this Control parent, Thickness thickness)
    {
        parent.Margin = thickness;
        return parent;
    }
    public static Control AddClass(this Control parent, string @class)
    {
        parent.AddStyleClass(@class);
        return parent;
    }
    public static Control Modulate(this Control parent, Color color)
    {
        parent.ModulateSelfOverride = color;
        return parent;
    }
    public static Control FixSize(this Control parent, Vector2 size)
    {
        parent.MinSize = size;
        parent.SetSize = size;
        parent.MaxSize = size;
        return parent;
    }
}