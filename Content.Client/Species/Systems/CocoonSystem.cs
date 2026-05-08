using Content.Shared.Species.Arachnid;

namespace Content.Client.Species.Arachnid;

public sealed class CocoonSystem : SharedCocoonSystem
{
    protected override void OnWrapDoAfterServer(EntityUid performer, EntityUid target, EntityUid cocoonContainer)
    {
    }

    protected override void OnWrapActionServer(EntityUid user, EntityUid target)
    {
    }

    protected override void OnWrapDoAfterSetupVictimEffects(EntityUid victim)
    {
    }

    protected override void OnCocoonContainerShutdownRemoveMumbleAccent(EntityUid victim)
    {
    }
}
