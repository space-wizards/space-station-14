// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.Inventory.Events;
using Content.Server.DeadSpace.Arkalyse.Components;

namespace Content.Server.DeadSpace.Arkalyse;

public sealed partial class ArkalyseGlovesSystem : EntitySystem
{
    private bool _shouldTerminate;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ArkalyseGlovesComponent, GotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<ArkalyseGlovesComponent, GotUnequippedEvent>(OnUnequipped);
    }
    private void OnEquipped(EntityUid uid, ArkalyseGlovesComponent component, GotEquippedEvent args)
    {
        if (!HasComp<ArkalyseStunComponent>(args.Equipee) || !HasComp<ArkalyseDamageComponent>(args.Equipee) || !HasComp<ArkalyseMutedComponent>(args.Equipee))
        {
            var stunComponent = EnsureComp<ArkalyseStunComponent>(args.Equipee);
            stunComponent.ParalyzeTime = 0.7f;

            var damageComponent = EnsureComp<ArkalyseDamageComponent>(args.Equipee);
            damageComponent.Damage.DamageDict["Piercing"] = 5;
            damageComponent.PushStrength = 0f;

            EnsureComp<ArkalyseMutedComponent>(args.Equipee);

            _shouldTerminate = true;
        }
        else _shouldTerminate = false;
    }

    private void OnUnequipped(EntityUid uid, ArkalyseGlovesComponent component, GotUnequippedEvent args)
    {
        if (_shouldTerminate)
        {
            RemComp<ArkalyseStunComponent>(args.Equipee);
            RemComp<ArkalyseDamageComponent>(args.Equipee);
            RemComp<ArkalyseMutedComponent>(args.Equipee);
        }
        else
        {
            return;
        }
    }
}
