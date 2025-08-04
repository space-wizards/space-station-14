using Content.Shared.Speech.Components.AccentComponents;
using Content.Shared.Speech.EntitySystems;
using Content.Shared.StatusEffect;

namespace Content.Server.Speech.EntitySystems;

public sealed class StutteringSystem : SharedStutteringSystem
{
    [Dependency] private readonly StatusEffectsSystem _statusEffectsSystem = default!;

    public override void DoStutter(EntityUid uid, TimeSpan time, bool refresh, StatusEffectsComponent? status = null)
    {
        if (!Resolve(uid, ref status, false))
            return;

        _statusEffectsSystem.TryAddStatusEffect<StutteringAccentComponent>(uid, StutterKey, time, refresh, status);
    }
}
