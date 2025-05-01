using Content.Shared.StatusEffect;

namespace Content.Shared.Drowsiness;

public abstract class SharedDrowsinessSystem : EntitySystem
{
    [ValidatePrototypeId<StatusEffectPrototype>]
    public const string DrowsinessKey = "Drowsiness";
}
