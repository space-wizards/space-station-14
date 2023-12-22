using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using System.Linq;
using System.Numerics;

namespace Content.Client.UserInterface.Controls;

[Virtual]
public class RadialMenu : BaseWindow
{
    private TextureButton? _backButton;
    private List<Control> _path = new();

    public RadialMenu()
    {
        _backButton = new TextureButton()
        {
            HorizontalAlignment = HAlignment.Center,
            VerticalAlignment = VAlignment.Center,
            SetSize = new Vector2(64f, 64f),
            StyleClasses = { "RadialMenuCloseButton" },
        };

        _backButton.OnButtonUp += _ => ReturnToPreviousLayer();

        AddChild(_backButton);

        OnChildAdded += child => child.Visible = ChildCount <= 2;
    }

    private Control? GetCurrentActiveLayer()
    {
        return Children.FirstOrDefault(x => x.Visible && x != _backButton);
    }

    public bool TryToMoveToNewLayer(string newLayer)
    {
        if (newLayer == string.Empty)
            return false;

        var currentLayer = GetCurrentActiveLayer();

        if (currentLayer == null)
            return false;

        var result = false;

        foreach (var child in Children)
        {
            if (child == _backButton)
                continue;

            if (result == true || child.Name != newLayer)
            {
                child.Visible = false;
            }

            else
            {
                child.Visible = true;
                result = true;
            }
        }

        if (result)
            _path.Add(currentLayer);

        if (_path.Count > 0 && _backButton != null)
            _backButton.SetOnlyStyleClass("RadialMenuBackButton");

        return result;
    }

    public void ReturnToPreviousLayer()
    {
        if (_path.Count == 0)
        {
            Close();
            return;
        }

        var lastChild = _path.Last();

        foreach (var child in Children)
        {
            if (child != _backButton)
                child.Visible = false;
        }

        lastChild.Visible = true;
        _path.RemoveAt(_path.Count - 1);

        if (_path.Count == 0 && _backButton != null)
            _backButton.SetOnlyStyleClass("RadialMenuCloseButton");
    }
}

[Virtual]
public class RadialMenuButton : Button
{
    /// <summary>
    /// Upon clicking this button the radial menu will be moved to the named layer
    /// </summary>
    public string? TargetLayer { get; set; }

    public RadialMenuButton()
    {
        OnButtonUp += OnClicked;
    }

    private void OnClicked(ButtonEventArgs args)
    {
        if (TargetLayer == null || TargetLayer == string.Empty)
            return;

        var parent = FindParentMultiLayerContainer(this);

        if (parent == null)
            return;

        parent.TryToMoveToNewLayer(TargetLayer);
    }

    private RadialMenu? FindParentMultiLayerContainer(Control control)
    {
        foreach (var ancestor in control.GetSelfAndLogicalAncestors())
        {
            if (ancestor is RadialMenu)
                return ancestor as RadialMenu;
        }

        return null;
    }
}

[Virtual]
public class RadialMenuTextureButton : TextureButton
{
    /// <summary>
    /// Upon clicking this button the radial menu will be moved to the named layer
    /// </summary>
    public string TargetLayer { get; set; } = string.Empty;

    public RadialMenuTextureButton()
    {
        OnButtonUp += OnClicked;
    }

    private void OnClicked(ButtonEventArgs args)
    {
        if (TargetLayer == string.Empty)
            return;

        var parent = FindParentMultiLayerContainer(this);

        if (parent == null)
            return;

        parent.TryToMoveToNewLayer(TargetLayer);
    }

    private RadialMenu? FindParentMultiLayerContainer(Control control)
    {
        foreach (var ancestor in control.GetSelfAndLogicalAncestors())
        {
            if (ancestor is RadialMenu)
                return ancestor as RadialMenu;
        }

        return null;
    }
}
