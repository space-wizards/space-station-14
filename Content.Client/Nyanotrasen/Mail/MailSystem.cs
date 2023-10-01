using Robust.Client.GameObjects;
using Content.Shared.Mail;
using Content.Shared.StatusIcon;
using Content.Shared.StatusIcon.Components;
using Robust.Client.State;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client.Mail
{
    /// <summary>
    /// Display a cool stamp on the parcel based on the job of the recipient.
    /// </summary>
    /// <remarks>
    /// GenericVisualizer is not powerful enough to handle setting a string on
    /// visual data then directly relaying that string to a layer's state.
    /// I.e. there is nothing like a regex capture group for visual data.
    ///
    /// Hence why this system exists.
    ///
    /// To do this with GenericVisualizer would require a separate condition
    /// for every job value, which would be extra mess to maintain.
    ///
    /// It would look something like this, multipled a couple dozen times.
    ///
    ///   enum.MailVisuals.JobIcon:
    ///     enum.MailVisualLayers.JobStamp:
    ///       StationEngineer:
    ///         state: StationEngineer
    ///       SecurityOfficer:
    ///         state: SecurityOfficer
    /// </remarks>
    public sealed class MailJobVisualizerSystem : VisualizerSystem<MailComponent>
    {
        protected override void OnAppearanceChange(EntityUid uid, MailComponent component, ref AppearanceChangeEvent args)
        {
            if (args.Sprite == null)
                return;

            if (args.Component.TryGetData(MailVisuals.JobIcon, out string job))
            {
                job = job.Substring(7); // :clueless:

                args.Sprite.LayerSetState(MailVisualLayers.JobStamp, job);
            }

        }
    }

    public enum MailVisualLayers : byte
    {
        Icon,
        Lock,
        FragileStamp,
        JobStamp,
        PriorityTape,
        Breakage,
    }
}
