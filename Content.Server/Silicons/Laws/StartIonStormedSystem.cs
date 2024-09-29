using Content.Shared.Silicons.Laws.Components;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Silicons.Laws;

namespace Content.Server.Silicons.Laws;

/// <summary>
/// This handles running the ion storm event on specific entities when spawned in.
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

    private void OnMapInit(EntityUid uid, StartIonStormedComponent component, ref MapInitEvent args)
    {
        if (!TryComp<SiliconLawBoundComponent>(uid, out var lawBound))
            return;
        if (!TryComp<TransformComponent>(uid, out var xform))
            return;
        if (!TryComp<IonStormTargetComponent>(uid, out var target))
            return;

        for (int currentIonStorm = 1; currentIonStorm <= component.IonStormAmount; currentIonStorm++)
        {
            _ionStorm.IonStormTarget(uid, lawBound, xform, target, null, true, true);
        }

        var laws = _siliconLaw.GetLaws(uid, lawBound);
        _adminLogger.Add(LogType.Mind, LogImpact.High, $"{ToPrettyString(uid):silicon} spawned with ion stormed laws: {laws.LoggingString()}");
    }
}
