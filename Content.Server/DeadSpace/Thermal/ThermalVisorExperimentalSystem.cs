// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.DeadSpace.ThermalVision;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;

namespace Content.Server.DeadSpace.ThermalVision;

public sealed class ThermalVisorExperimentalSystem : EntitySystem
{
    public const SlotFlags ValidSlots = SlotFlags.EYES;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ThermalVisorExperimentalComponent, GotEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<ThermalVisorExperimentalComponent, GotUnequippedEvent>(OnGotUnequipped);
    }

    private void OnGotEquipped(EntityUid entity, ThermalVisorExperimentalComponent comp, ref GotEquippedEvent args)
    {
        if ((args.SlotFlags & ValidSlots) == 0)
            return;

        if (HasComp<ThermalVisionExperimentalComponent>(args.Equipee))
            return;

        var activeComp = new ThermalVisionExperimentalComponent
        {
            ActivateSound = comp.ActivateSound,
            ActivateSoundOff = comp.ActivateSoundOff,
            VisorUid = entity,
            PulseDuration = comp.PulseDuration,
            UseShader = comp.UseShader
        };
        comp.HasThermalVision = true;

        AddComp(args.Equipee, activeComp);
    }

    private void OnGotUnequipped(EntityUid entity, ThermalVisorExperimentalComponent comp, ref GotUnequippedEvent args)
    {
        if (comp.HasThermalVision && HasComp<ThermalVisionExperimentalComponent>(args.Equipee))
        {
            if (TryComp<ThermalVisionExperimentalComponent>(args.Equipee, out var activeComp))
                activeComp.IsActive = false;

            RemComp<ThermalVisionExperimentalComponent>(args.Equipee);
        }
    }
}