using Content.Client.Resources;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Client.DoAfter.UI;

public sealed class DoAfterControl : PanelContainer
{
    [Dependency] private

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

    public EntityCoordinates Coordinates;

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
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        var screenCoordinates = _eyeManager.CoordinatesToScreen()
        LayoutContainer.SetPosition(this, new Vector2(_playerPosition.X - Width / 2, _playerPosition.Y - Height - 30.0f));
    }
}
