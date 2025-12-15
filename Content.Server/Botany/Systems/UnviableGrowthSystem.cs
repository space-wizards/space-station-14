using Content.Server.Botany.Components;

namespace Content.Server.Botany.Systems;

/// <summary>
/// Applies a death chance and damage to unviable plants each growth tick, updating visuals when necessary.
/// </summary>
public sealed class UnviableGrowthSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<UnviableGrowthComponent, OnPlantGrowEvent>(OnPlantGrow);
    }

    private void OnPlantGrow(Entity<UnviableGrowthComponent> ent, ref OnPlantGrowEvent args)
    {
        var (plantUid, component) = ent;
        var (_, tray) = args.Tray;

        if (!TryComp<PlantHolderComponent>(plantUid, out var holder))
            return;

        holder.Health -= component.UnviableDamage;
        if (tray.DrawWarnings)
            tray.UpdateSpriteAfterUpdate = true;
    }
}
