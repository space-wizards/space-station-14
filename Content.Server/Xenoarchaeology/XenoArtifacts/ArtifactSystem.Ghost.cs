using Content.Server.GameTicking;
using Content.Shared.Mind.Components;

namespace Content.Server.Xenoarchaeology.XenoArtifacts;

public partial class ArtifactSystem
{
    [Dependency] private readonly GameTicker _gameTicker = default!;

    private void InitializeGhost()
    {
        SubscribeLocalEvent<GhostAttemptHandleEvent>(HandleGhost);
    }

    /// <summary>
    /// The expected way to trigger this handler is from using the /ghost command as an artifact,
    /// which calls OnGhostAttempt with CanReturnGlobal set to `true`. Because of this, the artifact ghost role isn't freed.
    /// This method calls OnGhostAttempt with CanReturnGlobal parameter set to `false`, if the ghosting entity is an artifact.
    /// </summary>
    private void HandleGhost(GhostAttemptHandleEvent ev)
    {
        // Return if CanReturnGlobal is already false
        if (ev.CanReturnGlobal == false)
            return;

        if (!TryComp<ArtifactComponent>(ev.Mind.CurrentEntity, out var artifact))
            return;
        if (!TryComp<MindContainerComponent>(ev.Mind.CurrentEntity, out var mindcontainer))
            return;

        if (!mindcontainer.Mind.HasValue)
            return;

        ev.Handled = true;
        ev.Result = _gameTicker.OnGhostAttempt(mindcontainer.Mind.Value, false, false, ev.Mind);
    }
}
