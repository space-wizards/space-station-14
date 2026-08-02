// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.DeadSpace.ThermalVision;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;

namespace Content.Server.DeadSpace.ThermalVision;

public sealed class ThermalVisorSystem : EntitySystem
{
    public const SlotFlags ValidSlots = SlotFlags.EYES;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ThermalVisorComponent, GotEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<ThermalVisorComponent, GotUnequippedEvent>(OnGotUnequipped);
    }

    private void OnGotEquipped(EntityUid entity, ThermalVisorComponent comp, ref GotEquippedEvent args)
    {
        if ((args.SlotFlags & ValidSlots) == 0)
            return;

        if (HasComp<ThermalVisionComponent>(args.Equipee))
            return;

        var activeComp = new ThermalVisionComponent
        {
            ActivateSound = comp.ActivateSound,
            ActivateSoundOff = comp.ActivateSoundOff,
            Animation = comp.Animation,
            UseShader = comp.UseShader
        };
        comp.HasThermalVision = true;

        AddComp(args.Equipee, activeComp);
    }

    private void OnGotUnequipped(EntityUid entity, ThermalVisorComponent comp, ref GotUnequippedEvent args)
    {
        if (comp.HasThermalVision && HasComp<ThermalVisionComponent>(args.Equipee))
            RemComp<ThermalVisionComponent>(args.Equipee);
    }
}