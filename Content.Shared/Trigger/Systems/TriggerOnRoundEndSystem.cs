using Content.Shared.GameTicking;
using Content.Shared.Trigger.Components.Triggers;

namespace Content.Shared.Trigger.Systems;

/// <summary>
/// System for creating a trigger when the round ends.
/// </summary>
public sealed class TriggerOnRoundEndSystem : TriggerOnXSystem
{
    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundEndMessageEvent>(OnRoundEnd);
    }

    private void OnRoundEnd(RoundEndMessageEvent args)
    {
        var triggerQuery = EntityQueryEnumerator<TriggerOnRoundEndComponent>();

        // trigger everything with the component
        while (triggerQuery.MoveNext(out var uid, out var comp))
        {
            Trigger.Trigger(uid, null, comp.KeyOut);
        }
    }
}
