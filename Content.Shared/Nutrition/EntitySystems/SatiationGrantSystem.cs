using Content.Shared.Nutrition.Components;

namespace Content.Shared.Nutrition.EntitySystems;

/// <summary>
/// <see cref="SatiationGrantComponent"/>
/// </summary>
public sealed partial class SatiationGrantSystem : EntitySystem
{
    [Dependency] private SatiationSystem _satiation = default!;

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<SatiationGrantComponent> ent, ref MapInitEvent args)
    {
        foreach (var satiation in ent.Comp.Satiation)
        {
            _satiation.AddSatiation(ent.Owner, satiation.Key, satiation.Value);
        }
    }

    [SubscribeLocalEvent]
    private void OnShutdown(Entity<SatiationGrantComponent> ent, ref ComponentShutdown args)
    {
        if (!ent.Comp.RemoveOnShutdown)
            return;

        foreach (var satiation in ent.Comp.Satiation)
        {
            _satiation.RemoveSatiationType(ent.Owner, satiation.Key);
        }
    }
}
