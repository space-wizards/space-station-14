using Robust.Shared.GameObjects;

namespace Content.Server.Kitchen.Events
{
    /// <summary>
    /// Used in scaling amount of solution to extract in juicing
    /// </summary>
    public class JuiceableScalingEvent : EntityEventArgs
    {
        
        public JuiceableScalingEvent()
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
