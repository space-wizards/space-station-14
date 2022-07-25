using Content.Client.Resources;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Client.DoAfter.UI;

public sealed class DoAfterControl : PanelContainer
{
    public float Ratio
    {
        get => _bar.Ratio;
        set => _bar.Ratio = value;
    }

    public bool Cancelled
    {
        get => _bar.Cancelled;
        set => _bar.Cancelled = value;
    }

    private DoAfterBar _bar;

    public DoAfterControl()
    {
        IoCManager.InjectDependencies(this);

        var cache = IoCManager.Resolve<IResourceCache>();

        AddChild(new TextureRect
        {
            Texture = cache.GetTexture("/Textures/Interface/Misc/progress_bar.rsi/icon.png"),
            TextureScale = Vector2.One * DoAfterBar.DoAfterBarScale,
            VerticalAlignment = VAlignment.Center,
        });

        _bar = new DoAfterBar();
        AddChild(_bar);
        VerticalAlignment = VAlignment.Bottom;
        _bar.Measure(Vector2.Infinity);
        Measure(Vector2.Infinity);
    }
}
