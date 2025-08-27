using Content.Shared._Ronstation.Vampire.Components;
using Content.Shared.Actions;
using Content.Shared.Antag;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Player;

namespace Content.Shared._Ronstation.Vampire.EntitySystems;

public abstract class SharedVampireSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
    }
    private void OnStartup(EntityUid uid, VampireComponent component, ComponentStartup args)
    {
        //update the icon
        ChangeVitaeAmount(uid, 0, component);
    }

    public bool ChangeVitaeAmount(EntityUid uid, FixedPoint2 amount, VampireComponent? component = null, bool regenCap = false)
    {
        if (!Resolve(uid, ref component))
            return false;

        component.Vitae += amount;
        Dirty(uid, component);

        if (regenCap)
            FixedPoint2.Min(component.Vitae, component.VitaeRegenCap);

        // _alerts.ShowAlert(uid, component.VitaeAlert);

        return true;
    }

}