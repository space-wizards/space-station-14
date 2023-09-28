using Content.Client.Eui;
using Content.Shared.Cloning;
using JetBrains.Annotations;
using Robust.Client.Graphics;

namespace Content.Client.SS220.DeathReminder.UI
{
    [UsedImplicitly]
    public sealed class DeathReminderEui : BaseEui
    {
        private readonly DeathReminderWindow _window;

        public DeathReminderEui()
        {
            _window = new DeathReminderWindow();

            _window.OnClose += () => SendMessage(new AcceptCloningChoiceMessage(AcceptCloningUiButton.Deny));

            _window.AcceptButton.OnPressed += _ =>
            {
                SendMessage(new AcceptCloningChoiceMessage(AcceptCloningUiButton.Accept));
                _window.Close();
            };
        }

        public override void Opened()
        {
            IoCManager.Resolve<IClyde>().RequestWindowAttention();
            _window.OpenCentered();
        }

        public override void Closed()
        {
            _window.Close();
        }

    }
}
