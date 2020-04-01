using JetBrains.Annotations;

namespace Content.Server.AI.WorldState.States.Combat.Ranged
{
    [UsedImplicitly]
    public sealed class Accuracy : StateData<float>
    {
        public override string Name => "Accuracy";

        public override float GetValue()
        {
            // TODO: Maybe just make it a SetValue (maybe make a third type besides sensor / daemon called settablestate)
            return 1.0f;
        }
    }
}
