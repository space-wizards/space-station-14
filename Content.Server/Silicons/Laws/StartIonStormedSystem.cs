using Content.Shared.Silicons.Laws.Components;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;

namespace Content.Server.Silicons.Laws;

/// <summary>
/// This handles running the ion storm event a on specific entity when that entity is spawned in.
/// </summary>
public sealed class StartIonStormedSystem : EntitySystem
{
    [Dependency] private readonly IonStormSystem _ionStorm = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SiliconLawSystem _siliconLaw = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<StartIonStormedComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<StartIonStormedComponent> ent, ref MapInitEvent args)
    {
        if (!TryComp<SiliconLawBoundComponent>(ent.Owner, out var lawBound))
            return;
        if (!TryComp<IonStormTargetComponent>(ent.Owner, out var target))
            return;

        for (int currentIonStorm = 0; currentIonStorm < ent.Comp.IonStormAmount; currentIonStorm++)
        {
            _ionStorm.IonStormTarget((ent.Owner, lawBound, target), false);
        }

        var laws = _siliconLaw.GetLaws(ent.Owner, lawBound);
        _adminLogger.Add(LogType.Mind, LogImpact.High, $"{ToPrettyString(ent.Owner):silicon} spawned with ion stormed laws: {laws.LoggingString()}");
    }
}
