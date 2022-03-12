using Content.Client.Stylesheets;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;

namespace Content.Client.UserInterface.Controls;

public sealed class HLine : Container
{
    public Color? Color
    {
        get
        {
            if (_line.PanelOverride is StyleBoxFlat styleBox) return styleBox.BackgroundColor;
            return null;
        }
        set
        {
            if (_line.PanelOverride is StyleBoxFlat styleBox) styleBox.BackgroundColor = value!.Value;
        }
    }

    public float? Thickness {
        get
        {
            if (_line.PanelOverride is StyleBoxFlat styleBox) return styleBox.ContentMarginTopOverride;
            return null;
        }
        set
        {
            if (_line.PanelOverride is StyleBoxFlat styleBox) styleBox.ContentMarginTopOverride = value!.Value;
        }
    }

    private readonly PanelContainer _line;

    public HLine()
    {
        _line = new PanelContainer();
        _line.PanelOverride = new StyleBoxFlat();
        _line.PanelOverride.ContentMarginTopOverride = Thickness;
        AddChild(_line);
    }

}


/*
 <PanelContainer>
                    <PanelContainer.PanelOverride>
                        <gfx:StyleBoxFlat BackgroundColor="{x:Static style:StyleNano.NanoGold}"
                                          ContentMarginTopOverride="2" />
                    </PanelContainer.PanelOverride>
                </PanelContainer>

*/
