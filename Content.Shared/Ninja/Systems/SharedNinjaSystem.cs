using Content.Shared.Stealth;
using Content.Shared.Stealth.Components;

namespace Content.Shared.Ninja.Systems;

public abstract class SharedNinjaSystem : EntitySystem
{
    [Dependency] protected readonly SharedStealthSystem _stealth = default!;

    protected void SetCloaked(EntityUid user, bool cloaked)
    {
        if (TryComp<StealthComponent>(user, out var stealth))
        {
            // slightly visible, but doesn't change when moving so it's ok
            var visibility = cloaked ? stealth.MinVisibility + 0.25f : stealth.MaxVisibility;
            _stealth.SetVisibility(user, visibility, stealth);

            _stealth.SetEnabled(user, cloaked, stealth);
        }
    }
}
