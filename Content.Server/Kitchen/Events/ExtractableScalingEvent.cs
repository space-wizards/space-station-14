using Robust.Shared.GameObjects;

namespace Content.Server.Kitchen.Events
{
    /// <summary>
    /// Used in scaling amount of solution to extract in juicing
    /// </summary>
    public sealed class ExtractableScalingEvent : EntityEventArgs
    {

        public ExtractableScalingEvent()
        {
            Scalar = 1f;
        }

        public float Scalar
        {
            get;
            set;
        }

    }
}
