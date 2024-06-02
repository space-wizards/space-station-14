using Content.Shared.GreyStation.Hailer;

namespace Content.Client.GreyStation.Hailer.UI;

public sealed class HailerBoundUserInterface : BoundUserInterface
{
    private readonly SharedHailerSystem _hailer = default!;

    public HailerBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        _hailer = EntMan.System<SharedHailerSystem>();
    }

    protected override void Open()
    {
        base.Open();

        
    }
}
