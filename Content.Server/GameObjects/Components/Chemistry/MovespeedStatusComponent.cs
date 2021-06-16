using Content.Shared.Movement.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.ViewVariables;
using System;
using System.Threading;

namespace Content.Server.GameObjects.Components.Chemistry
{
    //TODO: refactor movement modifier component because this is a pretty poor solution
    [RegisterComponent]
    class MovespeedStatusComponent : Component, IMoveSpeedModifier
    {
        
        public override string Name => "ChemicalMovementSpeedChangeStatus";

        [ViewVariables]
        private float walkSpeedModifier;
        public float WalkSpeedModifier
        {
            get => walkSpeedModifier;
            set
            {
                walkSpeedModifier = value;
                ResetTimer();
            }
        }
        [ViewVariables]
        private float sprintSpeedModifier;
        public float SprintSpeedModifier
        {
            get => sprintSpeedModifier;
            set
            {
                sprintSpeedModifier = value;
                ResetTimer();
            }
        }

        public int EffectTime { get; set; }

        private readonly CancellationTokenSource _cancellation = new CancellationTokenSource();

        public override void Initialize()
        {
            base.Initialize();
            Owner.SpawnTimer(EffectTime, ResetModifiers, _cancellation.Token);
        }

        private void ResetModifiers()
        {
            WalkSpeedModifier = 0;
            SprintSpeedModifier = 0;
        }

        private void ResetTimer()
        {
            _cancellation.Cancel();
            Owner.SpawnTimer(EffectTime, ResetModifiers, _cancellation.Token);
        }
    }
}
