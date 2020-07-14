using System;

namespace Content.Server.AI.Utility.Curves
{
    /// <summary>
    /// Also Linear
    /// </summary>
    public struct QuadraticCurve : IResponseCurve
    {
        private readonly float _slope;

        private readonly float _exponent;
        // Vertical shift
        private readonly float _yOffset;
        // Horizontal shift
        private readonly float _xOffset;

        public QuadraticCurve(float slope, float exponent, float yOffset, float xOffset)
        {
            _slope = slope;
            _exponent = exponent;
            _yOffset = yOffset;
            _xOffset = xOffset;
        }

        public float GetResponse(float score)
        {
            return _slope * (float) Math.Pow(score - _xOffset, _exponent) + _yOffset;
        }
    }
}
