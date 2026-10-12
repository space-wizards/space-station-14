using Content.Shared.Examine;
using Content.Shared.IdentityManagement;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.StatusEffectNew.Components;

namespace Content.Shared.StatusEffectNew;

/// <summary>
/// Handler for <see cref="ExaminableStatusEffectComponent"/>.
/// </summary>
public sealed partial class ExaminableStatusEffectSystem : EntitySystem
{
    [SubscribeLocalEvent]
    private void OnExaminedEvent(Entity<ExaminableStatusEffectComponent> ent, ref StatusEffectRelayedEvent<ExaminedEvent> args)
    {
        // Early-out for effect requiring a concious mob
        if (ent.Comp.RequiresConciousness
            && (!TryComp<MobStateComponent>(args.AppliedTo, out var mob) || mob.CurrentState != MobState.Alive))
            return;

        using (args.Args.PushGroup(nameof(ExaminableStatusEffectSystem)))
        {
            args.Args.PushMarkup(Loc.GetString(ent.Comp.MessageId, ("target", Identity.Entity(args.AppliedTo, EntityManager))));
        }
    }
}
