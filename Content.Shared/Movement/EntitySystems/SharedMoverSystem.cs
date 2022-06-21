using System.Diagnostics.CodeAnalysis;
using Content.Shared.MobState.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Vehicle.Components;
using Robust.Shared.Containers;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Players;

namespace Content.Shared.Movement.EntitySystems
{
    /// <summary>
    ///     Handles converting inputs into movement.
    /// </summary>
    public sealed class SharedMoverSystem : EntitySystem
    {
        /// <inheritdoc />
        public override void Shutdown()
        {
            CommandBinds.Unregister<SharedMoverSystem>();
            base.Shutdown();
        }


    }

    public sealed class RelayMoveInputEvent : EntityEventArgs
    {
        public ICommonSession Session { get; }

        public RelayMoveInputEvent(ICommonSession session)
        {
            Session = session;
        }
    }
}
