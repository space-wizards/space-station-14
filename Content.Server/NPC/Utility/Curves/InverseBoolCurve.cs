namespace Content.Server.NPC.Utility.Curves
{
    public struct InverseBoolCurve : IResponseCurve
    {
        public float GetResponse(float score)
        {
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            return score == 0.0f ? 1.0f : 0.0f;
        }
    }
}
