using Content.Shared.ActionBlocker;

namespace Content.Shared.EffectBlocker
{
    /// <summary>
    /// This interface gives components the ability to block certain effects
    /// from affecting the owning entity. For actions see <see cref="IActionBlocker"/>
    /// </summary>
    public interface IEffectBlocker
    {
        bool CanFall() => true;
        bool CanSlip() => true;
    }
}
