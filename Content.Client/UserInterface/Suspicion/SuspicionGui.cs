using System;
using System.Globalization;
using Content.Client.GameObjects.Components.Suspicion;
using Content.Shared.Interfaces;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Timing;
using static Robust.Client.UserInterface.Controls.BaseButton;

namespace Content.Client.UserInterface.Suspicion
{
    public class SuspicionGui : Control
    {
#pragma warning disable 0649
        [Dependency] private readonly IPlayerManager _playerManager;
#pragma warning restore 0649

        private readonly VBoxContainer _container;
        private readonly Button _roleButton;

        private string _previousRoleName;
        private bool _previousAntagonist;

        public SuspicionGui()
        {
            IoCManager.InjectDependencies(this);

            AddChild(_container = new VBoxContainer
            {
                SeparationOverride = 0,
                Children =
                {
                    (_roleButton = new Button
                    {
                        Name = "Suspicion Role Button"
                    })
                }
            });

            _roleButton.CustomMinimumSize = (200, 60);
            _roleButton.OnPressed += RoleButtonPressed;
        }

        private void RoleButtonPressed(ButtonEventArgs obj)
        {
            if (!TryGetComponent(out var role))
            {
                return;
            }

            if (!role.Antagonist ?? false)
            {
                return;
            }

            var allies = string.Join(", ", role.Allies);
            var message = role.Allies.Count switch
            {
                0 => Loc.GetString("You have no allies"),
                1 => Loc.GetString("Your ally is {0}", allies),
                var n when n > 2 => Loc.GetString("Your allies are {0}", allies),
                _ => throw new ArgumentException($"Invalid number of allies: {role.Allies.Count}")
            };

            role.Owner.PopupMessage(role.Owner, message);
        }

        private bool TryGetComponent(out SuspicionRoleComponent suspicion)
        {
            suspicion = default;

            return _playerManager?.LocalPlayer?.ControlledEntity?.TryGetComponent(out suspicion) == true;
        }

        public void UpdateLabel()
        {
            if (!TryGetComponent(out var suspicion))
            {
                Visible = false;
                return;
            }

            if (suspicion.Role == null || suspicion.Antagonist == null)
            {
                Visible = false;
                return;
            }

            if (_previousRoleName == suspicion.Role && _previousAntagonist == suspicion.Antagonist)
            {
                return;
            }

            _previousRoleName = suspicion.Role;
            _previousAntagonist = suspicion.Antagonist.Value;

            var buttonText = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(_previousRoleName);
            buttonText = Loc.GetString(buttonText);

            _roleButton.Text = buttonText;
            _roleButton.ModulateSelfOverride = _previousAntagonist ? Color.Red : Color.Green;

            Visible = true;
        }

        protected override void FrameUpdate(FrameEventArgs args)
        {
            base.FrameUpdate(args);
            UpdateLabel();
        }
    }
}
