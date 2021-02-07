namespace Content.Server.AI.Utility.Curves
{
    /// <summary>
    /// For stuff that's a simple 0.0f or 1.0f
    /// </summary>
    public struct BoolCurve : IResponseCurve
    {
        public float GetResponse(float score)
        {
            return score > 0.0f ? 1.0f : 0.0f;
        }
    }
}
