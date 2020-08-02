using Content.Server.GameObjects.Components.Fluids;
using Content.Shared.Chemistry;
using Microsoft.EntityFrameworkCore.Update.Internal;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.Timers;
using Robust.Shared.ViewVariables;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Timer = Robust.Shared.Timers.Timer;

namespace Content.Server.GameObjects.Components.Chemistry
{
    [RegisterComponent]
    class VaporComponent : Component
    {
        public override string Name => "Vapor";

        [ViewVariables]
        private SolutionComponent _contents;
        [ViewVariables]
        private ReagentUnit _transferAmount;

        private float _settleTime;

        private CancellationTokenSource _updateToken;


        public override void Initialize()
        {
            base.Initialize();
            _contents = Owner.GetComponent<SolutionComponent>();
            SpawnTimer();
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _transferAmount, "transferAmount", ReagentUnit.New(5));
            serializer.DataField(ref _settleTime, "settleTime", 0.5f);
        }

        public void SpawnTimer()
        {
            _updateToken = new CancellationTokenSource();
            Timer.Spawn(TimeSpan.FromSeconds(_settleTime), Update, _updateToken.Token);
        }

        public void Update()
        {
            _updateToken?.Cancel();
            if (Owner.Deleted) return;

            SpillHelper.SpillAt(Owner, _contents.SplitSolution(_transferAmount), "PuddleSmear", false); //make non PuddleSmear?
            if (_contents.CurrentVolume == 0)
            {
                //Delete this
                Owner.Delete();
            }
            else
            {
                //Spawn another one in some time
                SpawnTimer();
            }
        }

        internal bool TryAddSolution(Solution solution)
        {
            if (solution.TotalVolume == 0)
            {
                return false;
            }
            var result = _contents.TryAddSolution(solution);
            if (!result)
            {
                return false;
            }

            return true;
        }
    }
}
