using Content.Shared.Vampire.Components;
using Content.Shared.FixedPoint;

namespace Content.Shared.Vampire;

public sealed class SharedVampireSystem : EntitySystem
{
    public FixedPoint2 GetBloodEssence(EntityUid vampire)
    {
        if (!TryComp<VampireComponent>(vampire, out var comp))
            return 0;
        
        if (comp.Balance != null && comp.Balance.TryGetValue(VampireComponent.CurrencyProto, out var val))
            return val;

        return 0;
    }
    
    public void SetAlertBloodAmount(VampireAlertComponent component, int amount)
    {
        component.BloodAmount = amount;
        Dirty(component.Owner, component);
    }
}