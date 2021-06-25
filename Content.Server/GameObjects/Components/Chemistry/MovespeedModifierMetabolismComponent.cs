using Content.Shared.Movement.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.ViewVariables;
using System;
using System.Threading;

namespace Content.Server.GameObjects.Components.Chemistry
{
    //TODO: refactor movement modifier component because this is a pretty poor solution
    [RegisterComponent]
    class MovespeedModifierMetabolismComponent : Component, IMoveSpeedModifier
    {
        [ViewVariables]
        public override string Name => "MovespeedModifierMetabolism";

        [ViewVariables]
        public float WalkSpeedModifier { get; set; }

        [ViewVariables]
        public float SprintSpeedModifier { get; set; }

        [ViewVariables]
        public int EffectTime { get; set; }

        private CancellationTokenSource? _cancellation;

        private void ResetModifiers()
        {
            _cancellation?.Cancel();
            WalkSpeedModifier = 1;
            SprintSpeedModifier = 1;
            var movement = Owner.GetComponent<MovementSpeedModifierComponent>();
            movement.RefreshMovementSpeedModifiers();
        }

        public void ResetTimer()
        {
            _cancellation?.Cancel();
            _cancellation = new CancellationTokenSource();
            Owner.SpawnTimer(EffectTime, ResetModifiers, _cancellation.Token);
        }
    }
}
