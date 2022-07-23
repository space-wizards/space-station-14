namespace Content.Server.Destructible.Thresholds
{
    [Serializable]
    [DataDefinition]
    public struct MinMax
    {
        [ViewVariables]
        [DataField("min")]
        public int Min;

        [ViewVariables]
        [DataField("max")]
        public int Max;
    }
}
