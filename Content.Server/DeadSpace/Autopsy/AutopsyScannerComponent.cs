// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Robust.Shared.Audio;

namespace Content.Server.DeadSpace.Autopsy
{
    /// <summary>
    ///    After scanning, retrieves the target Uid to use with its related UI.
    /// </summary>
    [RegisterComponent]
    public sealed partial class AutopsyScannerComponent : Component
    {
        /// <summary>
        /// How long it takes to scan someone.
        /// </summary>
        [DataField]
        public float ScanDelay = 2f;

        /// <summary>
        ///     Sound played on scanning begin and end
        /// </summary>
        [DataField]
        public SoundSpecifier ScanningSound;

        /// <summary>
        ///     Sound played on scanning end
        /// </summary>
        [DataField]
        public SoundSpecifier ScanningErrorSound;
    }
}
