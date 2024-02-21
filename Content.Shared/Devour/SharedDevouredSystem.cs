using Content.Shared.Devour.Components;
using Content.Shared.Interaction.Events;

namespace Content.Shared.Devour;

public abstract class SharedDevouredSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DevouredComponent, AttackAttemptEvent>(OnAttackAttempt);
    }

    /// <summary>
    ///     Prevents attacking while devoured.
    /// </summary>
    private void OnAttackAttempt(EntityUid uid, DevouredComponent component, AttackAttemptEvent args)
    {
        args.Cancel();
    }
}
