using Content.Shared.StatusEffect;

namespace Content.Shared.Flash
{
    public abstract class SharedFlashSystem : EntitySystem
    {
        [ValidatePrototypeId<StatusEffectPrototype>]
        public const string FlashedKey = "Flashed";
    }
}
