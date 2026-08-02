using Content.Shared.Mind.Components;

namespace Content.Shared.Mind;

/// <summary>
/// This marks any entity with the component as dead
/// for stuff like objectives and round-end
/// used for nymphs and reformed diona.
/// </summary>
public sealed class IsDeadICSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<IsDeadICComponent, GetCharactedDeadIcEvent>(OnGetDeadIC);
    }

    private void OnGetDeadIC(EntityUid uid, IsDeadICComponent component, ref GetCharactedDeadIcEvent args)
    {
        args.Dead = true;
    }
}
