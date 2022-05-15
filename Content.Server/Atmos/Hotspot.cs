namespace Content.Server.Atmos
{
    public struct Hotspot
    {
        [ViewVariables]
        public bool Valid;

        [ViewVariables]
        public bool SkippedFirstProcess;

        [ViewVariables]
        public bool Bypassing;

        [ViewVariables]
        public float Temperature;

        [ViewVariables]
        public float Volume;

        /// <summary>
        ///     State for the fire sprite.
        /// </summary>
        [ViewVariables]
        public byte State;
    }
}
