using Content.Shared.Eye.Blinding.Components;

namespace Content.Shared.Eye.Blinding.Systems;

public sealed class TemporaryBlindnessSystem : EntitySystem
{
    public const string BlindingStatusEffect = "TemporaryBlindness";

    [Dependency] private readonly BlindableSystem _blindableSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TemporaryBlindnessComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<TemporaryBlindnessComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<TemporaryBlindnessComponent, CanSeeAttemptEvent>(OnBlindTrySee);
    }

    private void OnStartup(EntityUid uid, TemporaryBlindnessComponent component, ComponentStartup args)
    {
        _blindableSystem.UpdateIsBlind(uid);
    }

    private void OnShutdown(EntityUid uid, TemporaryBlindnessComponent component, ComponentShutdown args)
    {
        _blindableSystem.UpdateIsBlind(uid);
    }

    private void OnBlindTrySee(EntityUid uid, TemporaryBlindnessComponent component, CanSeeAttemptEvent args)
    {
        if (component.LifeStage <= ComponentLifeStage.Running)
            args.Cancel();
    }
}
