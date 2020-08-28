#nullable enable
using System.Collections.Generic;
using System.Linq;
using Content.Client.UserInterface;
using Content.Client.UserInterface.Suspicion;
using Content.Shared.GameObjects.Components.Suspicion;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.IoC;
using Robust.Shared.Players;

namespace Content.Client.GameObjects.Components.Suspicion
{
    [RegisterComponent]
    public class SuspicionRoleComponent : SharedSuspicionRoleComponent
    {
        [Dependency] private readonly IGameHud _gameHud = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;

        private SuspicionGui? _gui;
        private string? _role;
        private bool? _antagonist;

        public string? Role
        {
            get => _role;
            set
            {
                _role = value;
                _gui?.UpdateLabel();
                Dirty();
            }
        }

        public bool? Antagonist
        {
            get => _antagonist;
            set
            {
                _antagonist = value;
                _gui?.UpdateLabel();
                Dirty();
            }
        }

        public HashSet<IEntity> Allies { get; } = new HashSet<IEntity>();

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            base.HandleComponentState(curState, nextState);

            if (!(curState is SuspicionRoleComponentState state))
            {
                return;
            }

            _role = state.Role;
            _antagonist = state.Antagonist;
        }

        public override void HandleMessage(ComponentMessage message, IComponent? component)
        {
            base.HandleMessage(message, component);

            switch (message)
            {
                case PlayerAttachedMsg _:
                    if (_gui == null)
                    {
                        _gui = new SuspicionGui();
                    }
                    else
                    {
                        _gui.Parent?.RemoveChild(_gui);
                    }

                    _gameHud.SuspicionContainer.AddChild(_gui);
                    _gui.UpdateLabel();

                    break;
                case PlayerDetachedMsg _:
                    _gui?.Parent?.RemoveChild(_gui);
                    break;
            }
        }

        public override void HandleNetworkMessage(ComponentMessage message, INetChannel netChannel, ICommonSession? session = null)
        {
            base.HandleNetworkMessage(message, netChannel, session);

            switch (message)
            {
                case SuspicionAlliesMessage msg:
                    Allies.Clear();
                    Allies.UnionWith(msg.Allies.Select(_entityManager.GetEntity));
                    break;
            }
        }

        public override void OnRemove()
        {
            base.OnRemove();

            _gui?.Dispose();
        }
    }
}
