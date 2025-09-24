
using Content.Client.Clothing.Systems;
using Content.Shared.Clothing.Components;
using Content.Shared.Clothing.EntitySystems;
using Robust.Client.GameObjects;
using Robust.Client.Player;

namespace Content.Client.Clothing.UI;

public sealed class HailerBoundUserInterface : BoundUserInterface
{
    [Dependency] private readonly IPlayerManager _player = default!;
    private readonly HailerSystem _hailer = default!;
    private readonly SpriteSystem _sprite = default!;

    private HailerRadialMenu? _menu;

    public HailerBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        _hailer = EntMan.System<HailerSystem>();
        _sprite = EntMan.System<SpriteSystem>();
    }

    protected override void Open()
    {
        base.Open();

        if (_menu == null)
        {
            _menu = new(Owner, EntMan, _player, _hailer, _sprite);

            _menu.OnLinePicked += index =>
            {
                SendPredictedMessage(new HailerOrderMessage(index));
                Close();
            };

            _menu.OnClose += () => Close();
        }

        _menu.OpenCentered();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing)
            return;

        _menu?.Close();
    }
}
