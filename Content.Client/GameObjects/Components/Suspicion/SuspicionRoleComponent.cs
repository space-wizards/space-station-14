#nullable enable
using System.Collections.Generic;
using Content.Client.UserInterface;
using Content.Client.UserInterface.Suspicion;
using Content.Shared.GameObjects.Components.Suspicion;
using Robust.Client.GameObjects;
using Robust.Client.Interfaces.Graphics.ClientEye;
using Robust.Client.Interfaces.Graphics.Overlays;
using Robust.Client.Interfaces.Input;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.IoC;
using Robust.Shared.Players;
using Robust.Shared.Prototypes;

namespace Content.Client.GameObjects.Components.Suspicion
{
    [RegisterComponent]
    public class SuspicionRoleComponent : SharedSuspicionRoleComponent
    {
        [Dependency] private readonly IGameHud _gameHud = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IOverlayManager _overlayManager = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IResourceCache _resourceCache = default!;
        [Dependency] private readonly IComponentManager _componentManager = default!;
        [Dependency] private readonly IInputManager _inputManager = default!;
        [Dependency] private readonly IEyeManager _eyeManager = default!;

        private SuspicionGui? _gui;
        private string? _role;
        private bool? _antagonist;

        public string? Role
        {
            get => _role;
            set
            {
                if (_role == value)
                {
                    return;
                }

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
                if (_antagonist == value)
                {
                    return;
                }

                _antagonist = value;
                _gui?.UpdateLabel();

                if (value ?? false)
                {
                    AddTraitorOverlay();
                }

                Dirty();
            }
        }

        public HashSet<EntityUid> Allies { get; } = new HashSet<EntityUid>();

        private bool AddAlly(EntityUid ally)
        {
            if (!Allies.Add(ally))
            {
                return false;
            }

            if (!_overlayManager.TryGetOverlay<TraitorOverlay>(nameof(TraitorOverlay), out var overlay))
            {
                return false;
            }

            return overlay.AddAlly(ally);
        }

        private bool RemoveAlly(EntityUid ally)
        {
            if (!Allies.Remove(ally))
            {
                return false;
            }

            if (!_overlayManager.TryGetOverlay<TraitorOverlay>(nameof(TraitorOverlay), out var overlay))
            {
                return false;
            }

            return overlay.RemoveAlly(ally);
        }

        private void AddTraitorOverlay()
        {
            if (_overlayManager.HasOverlay(nameof(TraitorOverlay)))
            {
                return;
            }

            var overlay = new TraitorOverlay(Owner, _entityManager, _resourceCache, _eyeManager);
            _overlayManager.AddOverlay(overlay);
        }

        private void RemoveTraitorOverlay()
        {
            _overlayManager.RemoveOverlay(nameof(TraitorOverlay));
        }

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            base.HandleComponentState(curState, nextState);

            if (!(curState is SuspicionRoleComponentState state))
            {
                return;
            }

            Role = state.Role;
            Antagonist = state.Antagonist;
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

                    if (_antagonist ?? false)
                    {
                        AddTraitorOverlay();
                    }

                    break;
                case PlayerDetachedMsg _:
                    _gui?.Parent?.RemoveChild(_gui);
                    RemoveTraitorOverlay();
                    break;
            }
        }

        public override void HandleNetworkMessage(ComponentMessage message, INetChannel netChannel, ICommonSession? session = null)
        {
            base.HandleNetworkMessage(message, netChannel, session);

            switch (message)
            {
                case SuspicionAlliesMessage msg:
                {
                    Allies.Clear();

                    foreach (var uid in msg.Allies)
                    {
                        AddAlly(uid);
                    }

                    break;
                }
                case SuspicionAllyAddedMessage msg:
                {
                    AddAlly(msg.Ally);
                    break;
                }
                case SuspicionAllyRemovedMessage msg:
                {
                    RemoveAlly(msg.Ally);
                    break;
                }
            }
        }

        public override void OnRemove()
        {
            base.OnRemove();

            _gui?.Dispose();
            RemoveTraitorOverlay();
        }
    }
}
