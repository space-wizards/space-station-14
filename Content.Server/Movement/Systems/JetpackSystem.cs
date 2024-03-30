using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Robust.Shared.Collections;
using Robust.Shared.Timing;

namespace Content.Server.Movement.Systems;

public sealed class JetpackSystem : SharedJetpackSystem
{
    [Dependency] private readonly GasTankSystem _gasTank = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;

    // have to refactor these magic strings into the sharedinventorysystem; trygetslotentity calls are full of them
    private const string BackSlot = "back";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<JetpackComponent, GotUnequippedEvent>(OnJetpackUnequip);
    }

    private void OnJetpackUnequip(EntityUid uid, JetpackComponent component, ref GotUnequippedEvent args)
    {
        DisableJetpack(uid, component, args.Equipee);
    }

    protected override bool CanEnable(EntityUid uid, EntityUid user, JetpackComponent component)
    {
        return base.CanEnable(uid, user, component) &&
               TryComp<GasTankComponent>(uid, out var gasTank) &&
               gasTank.Air.TotalMoles >= component.MoleUsage &&
               _inventory.TryGetSlotEntity(user, BackSlot, out var back) &&
               back == uid;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var toDisable = new ValueList<(EntityUid Uid, JetpackComponent Component)>();
        var query = EntityQueryEnumerator<ActiveJetpackComponent, JetpackComponent, GasTankComponent>();

        while (query.MoveNext(out var uid, out var active, out var comp, out var gasTankComp))
        {
            if (_timing.CurTime < active.TargetTime)
                continue;

            var gasTank = (uid, gasTankComp);
            active.TargetTime = _timing.CurTime + TimeSpan.FromSeconds(active.EffectCooldown);
            var usedAir = _gasTank.RemoveAir(gasTank, comp.MoleUsage);

            if (usedAir == null)
                continue;

            var usedEnoughAir =
                MathHelper.CloseTo(usedAir.TotalMoles, comp.MoleUsage, comp.MoleUsage / 100);

            if (!usedEnoughAir)
            {
                toDisable.Add((uid, comp));
            }

            _gasTank.UpdateUserInterface(gasTank);
        }

        foreach (var (uid, comp) in toDisable)
        {
            DisableJetpack(uid, comp);
        }
    }
}
