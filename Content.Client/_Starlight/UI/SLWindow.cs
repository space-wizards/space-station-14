using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.UserInterface.XAML;
using static Robust.Client.UserInterface.Controls.BoxContainer;
using YamlDotNet.Core.Tokens;
using System.Numerics;
using Content.Client.Stylesheets;
using Robust.Client.UserInterface;
using Content.Client.UserInterface.Controls;

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
internal sealed class SLBox : BoxContainer
{
    public SLBox(LayoutOrientation orientation) => Orientation = orientation;

    public SLBox Grid(int columns, Action<SLGrid> action)
    {
        var grid = new SLGrid(columns)
        {
            Stylesheet = Stylesheet
        };
        action(grid);
        AddChild(grid);
        return this;
    }
    public SLBox SelectBox<T>(Func<T, string> render, Action<SLSelect<T>> action)
    {
        var select = new SLSelect<T>(render)
        {
            Stylesheet = Stylesheet
        };
        action(select);
        AddChild(select);
        return this;
    }
}
internal sealed class SLScroll : ScrollContainer
{
    internal SLScroll()
    {
    }
    public SLScroll Grid(int columns, Action<SLGrid> action)
    {
        var grid = new SLGrid(columns)
        {
            Stylesheet = Stylesheet
        };
        action(grid);
        AddChild(grid);
        return this;
    }
    public SLScroll SelectBox<T>(Func<T, string> render, Action<SLSelect<T>> action)
    {
        var select = new SLSelect<T>(render)
        {
            Stylesheet = Stylesheet
        };
        action(select);
        AddChild(select);
        return this;
    }
    public SLScroll Box(LayoutOrientation orientation, Action<SLBox> action)
    {
        var select = new SLBox(orientation)
        {
            Stylesheet = Stylesheet
        };
        action(select);
        AddChild(select);
        return this;
    }
}
internal sealed class SLGrid : GridContainer
{
    internal SLGrid(int columns)
    {
        Columns = columns;
        HorizontalAlignment = HAlignment.Stretch;
    }
    public SLGrid AddChildren(IEnumerable<Control> controls)
    {
        foreach (var control in controls)
            AddChild(control);
        return this;
    }
    public SLGrid Grid(int columns, Action<SLGrid> action)
    {
        var grid = new SLGrid(columns)
        {
            Stylesheet = Stylesheet
        };
        action(grid);
        AddChild(grid);
        return this;
    }
    public SLGrid VerticalExp()
    {
        VerticalExpand = true;
        return this;
    }
    public SLGrid HorizontalExp()
    {
        HorizontalExpand = true;
        return this;
    }
}
internal sealed class SLStripe : StripeBack
{
    public SLStripe AddChildren(IEnumerable<Control> controls)
    {
        foreach (var control in controls)
            AddChild(control);
        return this;
    }
    public SLStripe Add(Control control)
    {
        AddChild(control);
        return this;
    }
}
internal sealed class SLSelect<T> : OptionButton
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