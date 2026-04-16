using Content.Shared.Examine;

namespace Content.Shared._Remnants.Looting;

public sealed class FoundInRaidSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<FoundInRaidComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(EntityUid uid, FoundInRaidComponent component, ExaminedEvent args)
    {
        args.PushMarkup($"[color=yellow]This item has been Found in Raid.[/color]");
    }
}
