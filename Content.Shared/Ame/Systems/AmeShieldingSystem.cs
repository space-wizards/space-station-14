using Content.Shared.Ame.Components;

namespace Content.Shared.Ame.Systems;

public sealed partial class AmeShieldingSystem : EntitySystem
{
    [Dependency] private SharedAppearanceSystem _appearanceSystem = default!;
    [Dependency] private SharedPointLightSystem _pointLightSystem = default!;

    public void SetCore(Entity<AmeShieldComponent?> ent, bool value)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return;

        if (value == ent.Comp.IsCore)
            return;

        ent.Comp.IsCore = value;
        _appearanceSystem.SetData(ent.Owner, AmeShieldVisuals.Core, value);
        if (!value)
            UpdateCoreVisuals(ent, 0, false);
    }

    public void UpdateCoreVisuals(Entity<AmeShieldComponent?> ent, int injectionStrength, bool injecting)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return;

        if (!injecting)
        {
            _appearanceSystem.SetData(ent.Owner, AmeShieldVisuals.CoreState, AmeCoreState.Off);
            _pointLightSystem.SetEnabled(ent.Owner, false);
            return;
        }

        _pointLightSystem.SetRadius(ent.Owner, Math.Clamp(injectionStrength, 1, 12));
        _pointLightSystem.SetEnabled(ent.Owner, true);
        _appearanceSystem.SetData(ent.Owner, AmeShieldVisuals.CoreState, injectionStrength > 2 ? AmeCoreState.Strong : AmeCoreState.Weak);
    }
}
