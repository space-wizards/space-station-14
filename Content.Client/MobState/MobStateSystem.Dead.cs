using Content.Shared.MobState;

namespace Content.Client.MobState;

public sealed partial class MobStateSystem
{
    public override void EnterDeadState(EntityUid uid)
    {
        base.EnterDeadState(uid);

        if (TryComp<AppearanceComponent>(uid, out var appearance))
        {
            appearance.SetData(DamageStateVisuals.State, DamageState.Dead);
        }
    }
}
