using System;

namespace Content.Server.AI.Utility.Curves
{
    public struct LogisticCurve : IResponseCurve
    {
        private readonly float _slope;

        private readonly float _exponent;
        // Vertical shift
        private readonly float _yOffset;
        // Horizontal shift
        private readonly float _xOffset;

        public LogisticCurve(float slope, float exponent, float yOffset, float xOffset)
        {
            _slope = slope;
            _exponent = exponent;
            _yOffset = yOffset;
            _xOffset = xOffset;
        }

        public float GetResponse(float score)
        {
            return _exponent * (1 / (1 + (float) Math.Pow(Math.Log(1000) * _slope, -1 * score + _xOffset))) + _yOffset;
        }
    }
}
