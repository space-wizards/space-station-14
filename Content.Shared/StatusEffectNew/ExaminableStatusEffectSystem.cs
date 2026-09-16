using Content.Shared.Examine;
using Content.Shared.IdentityManagement;
using Content.Shared.StatusEffectNew.Components;

namespace Content.Shared.StatusEffectNew;

/// <summary>
/// Handler for <see cref="ExaminableStatusEffectComponent"/>.
/// </summary>
public sealed partial class ExaminableStatusEffectSystem : EntitySystem
{
    [SubscribeLocalEvent]
    private void OnExaminedEvent(Entity<ExaminableStatusEffectComponent> ent, ref ExaminedEvent args)
    {
        using (args.PushGroup(nameof(ExaminableStatusEffectSystem)))
        {
            args.PushMarkup(Loc.GetString(ent.Comp.MessageId, ("target", Identity.Entity(args.Examined, EntityManager))));
        }
    }
}
