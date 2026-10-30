using Content.Shared.Examine;
using Content.Shared.IdentityManagement;
using Content.Shared.Mobs.Systems;
using Content.Shared.StatusEffectNew.Components;

namespace Content.Shared.StatusEffectNew;

/// <summary>
/// Handler for <see cref="ExaminableStatusEffectComponent"/>.
/// </summary>
public sealed partial class ExaminableStatusEffectSystem : EntitySystem
{
    [Dependency] private MobStateSystem _mobState = default!;

    [SubscribeLocalEvent]
    private void OnExaminedEvent(Entity<ExaminableStatusEffectComponent> ent, ref StatusEffectRelayedEvent<ExaminedEvent> args)
    {
        // Early-out for effect requiring a concious mob
        if (ent.Comp.RequiresConciousness && !_mobState.IsAlive(args.AppliedTo))
            return;

        using (args.Args.PushGroup(nameof(ExaminableStatusEffectSystem)))
        {
            args.Args.PushMarkup(Loc.GetString(ent.Comp.MessageId, ("target", Identity.Entity(args.AppliedTo, EntityManager))));
        }
    }
}
