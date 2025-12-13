using Content.Server.Botany.Components;

namespace Content.Server.Botany.Systems;

/// <summary>
/// Applies a death chance and damage to unviable plants each growth tick, updating visuals when necessary.
/// </summary>
public sealed class UnviableGrowthSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<UnviableGrowthComponent, OnPlantGrowEvent>(OnPlantGrow);
    }

    private void OnPlantGrow(Entity<UnviableGrowthComponent> ent, ref OnPlantGrowEvent args)
    {
        var (uid, component) = ent;

        PlantHolderComponent? holder = null;
        if (!Resolve(uid, ref holder))
            return;

        holder.Health -= component.UnviableDamage;
        if (holder.DrawWarnings)
            holder.UpdateSpriteAfterUpdate = true;
    }
}
