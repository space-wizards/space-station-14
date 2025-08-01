using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Lock;

namespace Content.Shared.SecretLocks;

public abstract partial class SharedVoiceTriggerLockSystem : EntitySystem
{
    [Dependency] protected readonly LockSystem _lock = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VoiceTriggerLockComponent, ComponentInit>(OnComponentInit);
    }

    private void OnComponentInit(Entity<VoiceTriggerLockComponent> ent, ref ComponentInit args)
    {
        if (!HasComp<LockComponent>(ent))
            Log.Warning($"Entity with VoiceTriggerLockComponent {ent.Owner} has no lock component.");
    }
}
