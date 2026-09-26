using Content.Shared.CosmicCult.Components;
using Content.Shared.DoAfter;
using Content.Shared.GameTicking;
using Content.Shared.GameTicking.Components;
using Content.Shared.GameTicking.Rules.Components;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Power;
using Content.Shared.Singularity.Components;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;

namespace Content.Shared.CosmicCult;

public sealed partial class CosmicLambdaDeviceSystem : EntitySystem
{
    [Dependency] private GameTicker _ticker = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedPopupSystem _popUp = default!;

    private static readonly EntProtoId LambdaParticles = "CosmicAnomalousParticleLambda";

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<CosmicLambdaDeviceComponent> ent, ref MapInitEvent args)
    {
        var doUpgrade = false;
        if (_ticker.IsGameRuleActive<MalignRiftSpawnRuleComponent>())
            doUpgrade = true;

        var query = EntityQueryEnumerator<CosmicCultRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var comp, out var gameRule))
        {
            if (!ent.Comp.Upgraded && _ticker.IsGameRuleActive((uid, gameRule)) && comp.Tier >= 2)
                doUpgrade = true;
        }

        if (!doUpgrade)
            return;

        ent.Comp.Upgraded = doUpgrade;

        var evt = new CosmicDeviceUpgradeEvent();
        RaiseLocalEvent(ent, ref evt);
    }

    [SubscribeLocalEvent]
    private void OnDeviceUpgrade(Entity<CosmicLambdaDeviceComponent> ent, ref CosmicDeviceUpgradeEvent args)
    {
        if (ent.Comp.Upgraded)
            args.Cancelled = true;

        _popUp.PopupEntity(Loc.GetString("cosmiccult-device-upgraded"), ent, PopupType.Large);
        _audio.PlayPredicted(ent.Comp.UpgradeSound, ent, ent, AudioParams.Default.WithVariation(0.05f));
    }

    [SubscribeLocalEvent]
    private void OnDeviceUpgrade(Entity<EmitterComponent> ent, ref CosmicDeviceUpgradeEvent args)
    {
        if (args.Cancelled)
            return;

        ent.Comp.SelectableTypes.Add(LambdaParticles);
    }
}

[ByRefEvent]
public record struct CosmicDeviceUpgradeEvent(bool Cancelled = false);
