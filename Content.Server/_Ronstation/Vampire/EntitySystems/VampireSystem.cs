using Content.Shared._Ronstation.Vampire.EntitySystems;
using Content.Shared._Ronstation.Vampire.Components;
using Content.Shared.Actions;
using Content.Shared.Alert;
using Content.Shared.Antag;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Player;

namespace Content.Server._Ronstation.Vampire.EntitySystems;

public sealed partial class VampireSystem : SharedVampireSystem
{
    [Dependency] private readonly AlertsSystem _alerts = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VampireComponent, ComponentStartup>(OnStartup);
        InitializeAbilities();
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

        _alerts.ShowAlert(uid, component.VitaeAlert);

        return true;
    }
    public bool LevelUp(EntityUid uid, VampireComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;
        if (component.StolenVitae < component.LevelUpValue * component.CurseLevel)
            return false;

        component.CurseLevel += 1;
        component.VitaeRegenCap += component.VitaeCapUpgradeAmount;
        Dirty(uid, component);

        return true;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<VampireComponent>();
        while (query.MoveNext(out var uid, out var vamp))
        {
            vamp.Accumulator += frameTime;

            if (vamp.Accumulator <= 1)
                continue;
            vamp.Accumulator -= 1;

            if (vamp.Vitae < vamp.VitaeRegenCap)
            {
                ChangeVitaeAmount(uid, vamp.VitaePerSecond, vamp, regenCap: true);
            }
        }
    }
}