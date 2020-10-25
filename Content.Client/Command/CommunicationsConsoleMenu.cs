using System.Threading;
using Content.Client.GameObjects.Components.Command;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Timer = Robust.Shared.Timers.Timer;

namespace Content.Client.Command
{
    public class CommunicationsConsoleMenu : SS14Window
    {
        protected override Vector2? CustomSize => new Vector2(600, 400);

        private CommunicationsConsoleBoundUserInterface Owner { get; set; }
        private readonly CancellationTokenSource _timerCancelTokenSource = new CancellationTokenSource();
        private readonly Button _emergencyShuttleButton;
        private readonly RichTextLabel _countdownLabel;

        public CommunicationsConsoleMenu(CommunicationsConsoleBoundUserInterface owner)
        {
            IoCManager.InjectDependencies(this);

            Title = Loc.GetString("Communications Console");
            Owner = owner;

            _countdownLabel = new RichTextLabel(){CustomMinimumSize = new Vector2(0, 200)};
            _emergencyShuttleButton = new Button();
            _emergencyShuttleButton.OnPressed += (e) => Owner.EmergencyShuttleButtonPressed();

            var vbox = new VBoxContainer() {SizeFlagsHorizontal = SizeFlags.FillExpand, SizeFlagsVertical = SizeFlags.FillExpand};

            vbox.AddChild(_countdownLabel);
            vbox.AddChild(_emergencyShuttleButton);

            var hbox = new HBoxContainer() {SizeFlagsHorizontal = SizeFlags.FillExpand, SizeFlagsVertical = SizeFlags.FillExpand};
            hbox.AddChild(new Control(){CustomMinimumSize = new Vector2(100,0), SizeFlagsHorizontal = SizeFlags.FillExpand});
            hbox.AddChild(vbox);
            hbox.AddChild(new Control(){CustomMinimumSize = new Vector2(100,0), SizeFlagsHorizontal = SizeFlags.FillExpand});

            Contents.AddChild(hbox);

            UpdateCountdown();
            Timer.SpawnRepeating(1000, UpdateCountdown, _timerCancelTokenSource.Token);
        }

        public void UpdateCountdown()
        {
            if (!Owner.CountdownStarted)
            {
                _countdownLabel.SetMessage("");
                _emergencyShuttleButton.Text = Loc.GetString("Call emergency shuttle");
                return;
            }

            _emergencyShuttleButton.Text = Loc.GetString("Recall emergency shuttle");
            _countdownLabel.SetMessage($"Time remaining\n{Owner.Countdown.ToString()}s");
        }

        public override void Close()
        {
            base.Close();

            _timerCancelTokenSource.Cancel();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
                _timerCancelTokenSource.Cancel();
        }
    }
}
