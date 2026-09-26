using Content.Shared.Nutrition.Components;

namespace Content.Shared.Nutrition.EntitySystems;

/// <summary>
/// <see cref="SatiationModifyComponent"/>
/// </summary>
public sealed partial class SatiationModifySystem : EntitySystem
{
    [Dependency] private SatiationSystem _satiation = default!;

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<SatiationModifyComponent> ent, ref MapInitEvent args)
    {
        if (ent.Comp.SatiationGrant is { } toAdd)
        {
            foreach (var satiation in toAdd)
            {
                _satiation.AddSatiation(ent.Owner, satiation.Key, satiation.Value);
            }
        }

        if (ent.Comp.SatiationRemove is { } toRemove)
        {
            foreach (var satiation in toRemove)
            {
                _satiation.RemoveSatiationType(ent.Owner, satiation.Key);
            }
        }
    }


    [SubscribeLocalEvent]
    private void OnShutdown(Entity<SatiationModifyComponent> ent, ref ComponentShutdown args)
    {
        if (!ent.Comp.RemoveOnShutdown)
            return;

        if (ent.Comp.SatiationGrant is { } toAdd)
        {
            foreach (var satiation in toAdd)
            {
                _satiation.RemoveSatiationType(ent.Owner, satiation.Key);
            }
        }
    }
}
