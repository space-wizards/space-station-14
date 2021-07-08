using Robust.Shared.GameObjects;
using Robust.Shared.Players;
using System.Threading;

namespace Content.Shared.Chemistry.Components
{
    //TODO: refactor movement modifier component because this is a pretty poor solution
    [RegisterComponent]
    public class MovespeedModifierMetabolismComponent : SharedMovespeedModifierMetabolismComponent
    {

        private CancellationTokenSource? _cancellation;

        public void ResetTimer()
        {
            _cancellation?.Cancel();
            _cancellation = new CancellationTokenSource();
            Owner.SpawnTimer(EffectTime, ResetModifiers, _cancellation.Token);
        }

        public override ComponentState GetComponentState(ICommonSession player)
        {
            return new MovespeedModifierMetabolismComponentState(WalkSpeedModifier, SprintSpeedModifier);
        }
    }
}

