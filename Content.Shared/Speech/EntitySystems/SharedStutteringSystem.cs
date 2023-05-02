using Content.Shared.StatusEffect;

namespace Content.Shared.Speech.EntitySystems;

public abstract class SharedStutteringSystem : EntitySystem
{
    public const string StutterKey = "Stutter";

    [Dependency] private readonly StatusEffectsSystem _statusEffectsSystem = default!;  

    // For code in shared... I imagine we ain't getting accent prediction anytime soon so let's not bother.
    public virtual void DoStutter(EntityUid uid, TimeSpan time, bool refresh, StatusEffectsComponent? status = null)
    {
    }
    
    public virtual void DoRemoveStutterTime(EntityUid uid, double timeRemoved)
    {
        _statusEffectsSystem.TryRemoveTime(uid, StutterKey, TimeSpan.FromSeconds(timeRemoved));
    }
    
    public void DoRemoveStutter(EntityUid uid, double timeRemoved)
    {
       _statusEffectsSystem.TryRemoveStatusEffect(uid, StutterKey);
    }
}
