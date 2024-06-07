using Content.Shared.Examine;

namespace Content.Shared.Contraband;

/// <summary>
/// This handles showing examine messages for contraband-marked items.
/// </summary>
public sealed class ContrabandSystem : EntitySystem
{
    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ContrabandComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(EntityUid uid, ContrabandComponent component, ExaminedEvent args)
    {
        var str = Loc.GetString($"contraband-examine-text-{component.Severity.ToString()}");
        args.PushMarkup(str);
    }
}
