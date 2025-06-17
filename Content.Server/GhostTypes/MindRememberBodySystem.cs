using Content.Server.Body.Components;
using Content.Shared.Damage;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;

namespace Content.Server.GhostTypes;

public sealed class MindRememberBodySystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<MindRememberBodyComponent, BeforeBodyDestructionEvent>(SaveBody);
    }

    private void SaveBody(EntityUid uid, MindRememberBodyComponent component, BeforeBodyDestructionEvent args)
    {
        if (!TryComp<DamageableComponent>(uid, out var damageable)
            || !TryComp<MindContainerComponent>(uid, out var mindContainer)
            || !TryComp<MindComponent>(mindContainer.Mind, out var mind))
            return;

        mind.DamagePerGroup = damageable.DamagePerGroup;
        mind.Damage = damageable.Damage;
    }
}
