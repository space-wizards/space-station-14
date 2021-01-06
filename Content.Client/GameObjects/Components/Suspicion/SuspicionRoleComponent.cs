#nullable enable
using System.Collections.Generic;
using Content.Client.UserInterface;
using Content.Client.UserInterface.Suspicion;
using Content.Shared.GameObjects.Components.Suspicion;
using Robust.Client.GameObjects;
using Robust.Client.Interfaces.Graphics.ClientEye;
using Robust.Client.Interfaces.Graphics.Overlays;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.ViewVariables;

namespace Content.Client.GameObjects.Components.Suspicion
{
    [RegisterComponent]
    public class SuspicionRoleComponent : SharedSuspicionRoleComponent
    {
        [Dependency] private readonly IGameHud _gameHud = default!;
        [Dependency] private readonly IOverlayManager _overlayManager = default!;
        [Dependency] private readonly IResourceCache _resourceCache = default!;
        [Dependency] private readonly IEyeManager _eyeManager = default!;

        private SuspicionGui? _gui;
        private string? _role;
        private bool? _antagonist;
        private bool _overlayActive;

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

        [ViewVariables]
        public List<(string name, EntityUid uid)> Allies { get; } = new();

        private void AddTraitorOverlay()
        {
            if (_overlayManager.HasOverlay(nameof(TraitorOverlay)))
            {
                return;
            }

            _overlayActive = true;
            var overlay = new TraitorOverlay(Owner.EntityManager, _resourceCache, _eyeManager);
            _overlayManager.AddOverlay(overlay);
        }

        private void RemoveTraitorOverlay()
        {
            if (!_overlayActive)
            {
                return;
            }

            _overlayManager.RemoveOverlay(nameof(TraitorOverlay));
        }

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            base.HandleComponentState(curState, nextState);

            if (curState is not SuspicionRoleComponentState state)
            {
                return;
            }

            Role = state.Role;
            Antagonist = state.Antagonist;
            Allies.Clear();
            Allies.AddRange(state.Allies);
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

        public override void OnRemove()
        {
            base.OnRemove();

            _gui?.Dispose();
            RemoveTraitorOverlay();
        }
    }
}
