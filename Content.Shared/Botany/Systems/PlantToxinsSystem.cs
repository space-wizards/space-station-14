using JetBrains.Annotations;
using Content.Shared.Botany.Components;
using Content.Shared.Botany.Events;

namespace Content.Shared.Botany.Systems;

/// <summary>
/// Handles toxin accumulation and tolerance for plants, applying health damage
/// and decrementing toxins based on per-tick uptake.
/// </summary>
public sealed partial class PlantToxinsSystem : EntitySystem
{
    [Dependency] private BotanySystem _botany = default!;
    [Dependency] private PlantMutationSystem _mutation = default!;
    [Dependency] private PlantHolderSystem _plantHolder = default!;
    [Dependency] private PlantTraySystem _plantTray = default!;

    [SubscribeLocalEvent]
    private void OnCrossPollinate(Entity<PlantToxinsComponent> ent, ref PlantCrossPollinateEvent args)
    {
        if (!_botany.TryGetPlantComponent<PlantToxinsComponent>(args.PollenData, args.PollenProtoId, out var pollenData))
            return;

        _mutation.CrossFloat(ent, ref ent.Comp.ToxinsTolerance, pollenData.ToxinsTolerance);
        _mutation.CrossFloat(ent, ref ent.Comp.ToxinUptakeDivisor, pollenData.ToxinUptakeDivisor);
        Dirty(ent);
    }

    [SubscribeLocalEvent]
    private void OnPlantGrow(Entity<PlantToxinsComponent> ent, ref PlantGrowEvent args)
    {
        var trayUid = GetEntity(args.Tray);
        if (!TryComp<PlantTrayComponent>(trayUid, out var tray)
            || !TryComp<PlantHolderComponent>(ent.Owner, out var holder))
            return;

        if (ent.Comp.ToxinUptakeDivisor <= 0)
            return;

        var toxinUptake = MathF.Max(1, MathF.Round(tray.ToxinLevel / ent.Comp.ToxinUptakeDivisor));
        if (tray.ToxinLevel > ent.Comp.ToxinsTolerance)
        {
            // Get minimum value between health left and toxin uptake.
            var actualUptake = Math.Min(toxinUptake, holder.Health);

            _plantHolder.AdjustsHealth(ent.Owner, -actualUptake);
            _plantTray.AdjustToxin((trayUid, tray), -actualUptake);
        }
        else
        {
            _plantTray.AdjustToxin((trayUid, tray), -toxinUptake);
        }
    }

    /// <summary>
    /// Adjusts maximum toxin level the plant can tolerate before taking damage.
    /// </summary>
    [PublicAPI]
    public void AdjustToxinsTolerance(Entity<PlantToxinsComponent?> ent, float amount)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false))
            return;

        ent.Comp.ToxinsTolerance = MathF.Max(0f, ent.Comp.ToxinsTolerance + amount);
        DirtyField(ent, nameof(ent.Comp.ToxinsTolerance));
    }
}
