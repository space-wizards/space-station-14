using JetBrains.Annotations;

namespace Content.Server.AI.WorldState.States.Utility
{
    /// <summary>
    /// Used for the utility AI; sets the threshold score we need to beat
    /// </summary>
    [UsedImplicitly]
    public sealed class LastUtilityScoreState : StateData<float>
    {
        public override string Name => "LastBonus";
        private float _value = 0.0f;

        public void SetValue(float value)
        {
            _value = value;
        }

        public override float GetValue()
        {
            return _value;
        }
    }
}
