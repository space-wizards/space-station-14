using Content.Client.Administration.Systems;
using Content.Client.Players.PlayerInfo;
using Content.Client.UserInterface.Systems.Character.Controls;
using Content.Client.UserInterface.Systems.Objectives.Controls;
using JetBrains.Annotations;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controls;
using Robust.Client.Utility;
using Robust.Shared.Network;
using Robust.Shared.Utility;
using System.Linq;

namespace Content.Client.UserInterface.Systems.Admin
{
    [UsedImplicitly]
    public sealed class ObjectivesUIController : UIController, IOnSystemChanged<AdminSystem>
    {
        private AdminSystem? _adminSystem;

        private ObjectivesWindow? _window = default!;

        private void EnsureWindow()
        {
            if (_window is { Disposed: false })
                return;

            _window = UIManager.CreateWindow<ObjectivesWindow>();
            LayoutContainer.SetAnchorPreset(_window, LayoutContainer.LayoutPreset.CenterTop);
        }

        public void OpenWindow(NetUserId sessionId)
        {
            EnsureWindow();

            if (_window == null || _adminSystem == null)
            {
                return;
            }

            var selectedAntagonist = _adminSystem.PlayerList.Where(x => x.SessionId == sessionId).FirstOrDefault();

            if (selectedAntagonist?.Objectives == null)
            {
                return;
            }

            _window.Objectives.RemoveAllChildren();

            _window.Title = $"{Loc.GetString("character-info-objectives-label")} {selectedAntagonist.CharacterName}";

            foreach (var (groupId, conditions) in selectedAntagonist.Objectives)
            {
                var objectiveControl = new CharacterObjectiveControl
                {
                    Orientation = BoxContainer.LayoutOrientation.Vertical,
                    Modulate = Color.Gray
                };

                objectiveControl.AddChild(new Label
                {
                    Text = groupId,
                    Modulate = Color.LightSkyBlue
                });

                foreach (var condition in conditions)
                {
                    var conditionControl = new ObjectiveConditionsControl();
                    conditionControl.ProgressTexture.Texture = condition.SpriteSpecifier.Frame0();
                    conditionControl.ProgressTexture.Progress = condition.Progress;
                    var titleMessage = new FormattedMessage();
                    var descriptionMessage = new FormattedMessage();
                    titleMessage.AddText(condition.Title);
                    descriptionMessage.AddText(condition.Description);

                    conditionControl.Title.SetMessage(titleMessage);
                    conditionControl.Description.SetMessage(descriptionMessage);

                    objectiveControl.AddChild(conditionControl);
                }

                var briefingControl = new ObjectiveBriefingControl();

                objectiveControl.AddChild(briefingControl);
                _window.Objectives.AddChild(objectiveControl);
            }

            _window.Open();
        }

        public void OnSystemLoaded(AdminSystem system)
        {
            _adminSystem = system;
        }

        public void OnSystemUnloaded(AdminSystem system)
        {
            _adminSystem = system;
        }
    }
}
