using Content.Server.GameObjects.Components.Chemistry;
using Content.Server.Interfaces;
using Content.Shared.Chemistry;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Fluids
{
    [RegisterComponent]
    class SprayComponent : Component, IAfterInteract
    {
        [Dependency] private readonly IServerNotifyManager _notifyManager = default!;
        [Dependency] private readonly IServerEntityManager _serverEntityManager = default!;

        public override string Name => "Spray";

        private ReagentUnit _transferAmount;
        private string _spraySound;
        private float _sprayVelocity;

        /// <summary>
        ///     The amount of solution to be sprayer from this solution when using it
        /// </summary>
        [ViewVariables]
        public ReagentUnit TransferAmount
        {
            get => _transferAmount;
            set => _transferAmount = value;
        }

        /// <summary>
        ///     The speed at which the vapor starts when sprayed
        /// </summary>
        [ViewVariables]
        public float Velocity
        {
            get => _sprayVelocity;
            set => _sprayVelocity = value;
        }

        private SolutionComponent _contents;
        public ReagentUnit CurrentVolume => _contents.CurrentVolume;

        public override void Initialize()
        {
            base.Initialize();
            _contents = Owner.GetComponent<SolutionComponent>();
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _transferAmount, "transferAmount", ReagentUnit.New(10));
            serializer.DataField(ref _sprayVelocity, "sprayVelocity", 5.0f);
            serializer.DataField(ref _spraySound, "spraySound", string.Empty);
        }

        void IAfterInteract.AfterInteract(AfterInteractEventArgs eventArgs)
        {
            if (CurrentVolume <= 0)
            {
                _notifyManager.PopupMessage(Owner, eventArgs.User, Loc.GetString("It's empty!"));
                return;
            }

            var playerPos = eventArgs.User.Transform.GridPosition;
            if (eventArgs.ClickLocation.GridID != playerPos.GridID)
                return;

            var direction = (eventArgs.ClickLocation.Position - playerPos.Position).Normalized;
            var solution = _contents.SplitSolution(_transferAmount);

            playerPos = playerPos.Offset(direction); // Move a bit so we don't hit the player
            //TODO: check for wall?
            var vapor = _serverEntityManager.SpawnEntity("Vapor", playerPos);
            // Add the solution to the vapor and actually send the thing
            var vaporComponent = vapor.GetComponent<VaporComponent>();
            vaporComponent.TryAddSolution(solution);
            vaporComponent.Start(direction, _sprayVelocity); //TODO: maybe make the velocity depending on the distance to the click

            //Play sound
            EntitySystem.Get<AudioSystem>().PlayFromEntity(_spraySound, Owner);
        }
    }
}
