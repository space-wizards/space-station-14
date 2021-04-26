using System.Threading;
using Content.Client.GameObjects.Components.Command;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Client.Command
{
    public class CommunicationsConsoleMenu : SS14Window
    {
        private CommunicationsConsoleBoundUserInterface Owner { get; set; }
        private readonly CancellationTokenSource _timerCancelTokenSource = new();
        private LineEdit _messageInput { get; set; }
        public readonly Button AnnounceButton;
        public readonly Button EmergencyShuttleButton;
        private readonly RichTextLabel _countdownLabel;

        public CommunicationsConsoleMenu(CommunicationsConsoleBoundUserInterface owner)
        {
            SetSize = MinSize = (600, 400);
            IoCManager.InjectDependencies(this);

            Title = Loc.GetString("communications-console-title");
            Owner = owner;

            _messageInput = new LineEdit
            {
                PlaceHolder = Loc.GetString("communications-console-announcement"),
                HorizontalExpand = true,
                SizeFlagsStretchRatio = 1
            };
            AnnounceButton = new Button();
            AnnounceButton.Text = "Announce";
            AnnounceButton.OnPressed += (_) => Owner.AnnounceButtonPressed(_messageInput.Text.Trim());
            AnnounceButton.Disabled = !owner.CanAnnounce;

            _countdownLabel = new RichTextLabel(){MinSize = new Vector2(0, 200)};
            EmergencyShuttleButton = new Button();
            EmergencyShuttleButton.OnPressed += (_) => Owner.EmergencyShuttleButtonPressed();
            EmergencyShuttleButton.Disabled = !owner.CanCall;

            var vbox = new VBoxContainer() {HorizontalExpand = true, VerticalExpand = true};
            vbox.AddChild(_messageInput);
            vbox.AddChild(new Control(){MinSize = new Vector2(0,10), HorizontalExpand = true});
            vbox.AddChild(AnnounceButton);
            vbox.AddChild(new Control(){MinSize = new Vector2(0,10), HorizontalExpand = true});
            vbox.AddChild(_countdownLabel);
            vbox.AddChild(EmergencyShuttleButton);

            var hbox = new HBoxContainer() {HorizontalExpand = true, VerticalExpand = true};
            hbox.AddChild(new Control(){MinSize = new Vector2(100,0), HorizontalExpand = true});
            hbox.AddChild(vbox);
            hbox.AddChild(new Control(){MinSize = new Vector2(100,0), HorizontalExpand = true});

            Contents.AddChild(hbox);

            UpdateCountdown();
            Timer.SpawnRepeating(1000, UpdateCountdown, _timerCancelTokenSource.Token);
        }

        public void UpdateCountdown()
        {
            if (!Owner.CountdownStarted)
            {
                _countdownLabel.SetMessage("");
                EmergencyShuttleButton.Text = Loc.GetString("communications-console-call-shuttle");
                return;
            }

            EmergencyShuttleButton.Text = Loc.GetString("communications-console-recall-shuttle");
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
