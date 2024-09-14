using Content.Shared.Atmos.Components;

public abstract class SharedGasPipeSensorSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    protected void UpdateVisuals(EntityUid uid, GasPipeSensorComponent component, bool isActive)
    {
        _appearance.SetData(uid, GasPipeSensorVisuals.State, isActive);
    }
}
