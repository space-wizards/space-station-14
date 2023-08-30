namespace Content.Server.GameTicking
{
    /// <summary>
    ///     Describes an entry in the crew manifest.
    /// </summary>
    public sealed class ManifestEntry
    {
        public ManifestEntry(string characterName, string jobId)
        {
            CharacterName = characterName;
            JobId = jobId;
        }

        /// <summary>
        ///     The name of the character on the manifest.
        /// </summary>
        [ViewVariables]
        public string CharacterName { get; }

        /// <summary>
        ///     The ID of the job they picked.
        /// </summary>
        [ViewVariables]
        public string JobId { get; }
    }
}
