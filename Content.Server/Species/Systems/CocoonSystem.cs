using Content.Server.Speech.Components;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Humanoid;
using Content.Shared.Interaction.Components;
using Content.Shared.Species.Arachnid;
using Content.Shared.Standing;

namespace Content.Server.Species.Arachnid;

public sealed class CocoonSystem : SharedCocoonSystem
{
    [Dependency] private readonly StandingStateSystem _standing = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLog = default!;

    protected override void OnCocoonContainerShutdownRemoveMumbleAccent(EntityUid victim)
    {
        if (HasComp<MumbleAccentComponent>(victim))
        {
            RemComp<MumbleAccentComponent>(victim);
        }
    }

    protected override void OnWrapActionServer(EntityUid user, EntityUid target)
    {
        _adminLog.Add(LogType.Action, LogImpact.High,
            $"{ToPrettyString(user):player} is trying to cocoon {ToPrettyString(target):player}");
    }

    protected override void OnWrapDoAfterServer(EntityUid performer, EntityUid target, EntityUid cocoonContainer)
    {
        _adminLog.Add(LogType.Action, LogImpact.High,
            $"{ToPrettyString(performer):player} has cocooned {ToPrettyString(target):player}");
    }

    protected override void OnWrapDoAfterSetupVictimEffects(EntityUid victim)
    {
        // Force prone
        if (HasComp<StandingStateComponent>(victim))
        {
            _standing.Down(victim);
        }

        EnsureComp<BlockMovementComponent>(victim);

        EnsureComp<MumbleAccentComponent>(victim);
        EnsureComp<TemporaryBlindnessComponent>(victim);
    }
}
