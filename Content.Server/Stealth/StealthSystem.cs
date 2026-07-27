using Content.Shared.Examine;
using Content.Shared.Mobs;
using Content.Shared.Stealth;
using Content.Shared.Stealth.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Light;

namespace Content.Server.Stealth;

public sealed class StealthSystem : SharedStealthSystem
{
    [Dependency] private LightLevelSystem _lightLevelSystem = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<StealthComponent, StealthInDarkComponent>();
        while (query.MoveNext(out var uid, out var stealth, out var darkStealth))
        {
            if (!_lightLevelSystem.TryCalculateLightLevel(uid, out var lightLevel))
                return;

            if (darkStealth.ActivatedLightLevel > lightLevel)
            {
                if (GetVisibility(uid) >= stealth.MinVisibility)
                    ModifyVisibility(uid, darkStealth.DarkVisibilityRate);

                darkStealth.ChangedVisibility += darkStealth.DarkVisibilityRate;
            }
            else
            {
                if (darkStealth.ChangedVisibility > 0)
                    return;

                ModifyVisibility(uid, darkStealth.LightVisibilityRate);
                darkStealth.ChangedVisibility += darkStealth.LightVisibilityRate;
            }
        }
    }
}
