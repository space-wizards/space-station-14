using Content.Shared.Item.ItemToggle;
using Content.Shared.Lock;
using Content.Shared.Trigger.Components.Triggers;

namespace Content.Shared.SecretLocks;

public sealed partial class SharedVoiceTriggerLockSystem : EntitySystem
{
    [Dependency] private readonly ItemToggleSystem _toggle = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VoiceTriggerLockComponent, LockToggledEvent>(OnLockToggled);
    }

    private void OnLockToggled(Entity<VoiceTriggerLockComponent> ent, ref LockToggledEvent args)
    {
        if (!TryComp<TriggerOnVoiceComponent>(ent.Owner, out var triggerComp))
            return;

        triggerComp.ShowVerbs = !args.Locked;
        triggerComp.ShowExamine = !args.Locked;

        _toggle.TryDeactivate(ent.Owner, null, true, false);

        Dirty(ent.Owner, triggerComp);
    }
}
