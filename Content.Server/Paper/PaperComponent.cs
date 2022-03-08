using System.Threading.Tasks;
using Content.Server.UserInterface;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Paper;
using Content.Shared.Tag;
using Robust.Server.GameObjects;

using Robust.Shared.Utility;

namespace Content.Server.Paper
{
    [RegisterComponent]
#pragma warning disable 618
    [ComponentReference(typeof(SharedPaperComponent))]
    public sealed class PaperComponent : SharedPaperComponent, IExamine, IInteractUsing
#pragma warning restore 618
    {
        [Dependency] private readonly IEntityManager _entMan = default!;

        public PaperAction Mode;
        [DataField("content")]
        public string Content { get; set; } = "";

        [DataField("contentSize")]
        public int ContentSize { get; set; } = 500;


        [ViewVariables] private BoundUserInterface? UserInterface => Owner.GetUIOrNull(PaperUiKey.Key);

        protected override void Initialize()
        {
            base.Initialize();

            if (UserInterface != null)
            {
                UserInterface.OnReceiveMessage += OnUiReceiveMessage;
            }

            Mode = PaperAction.Read;
            UpdateUserInterface();
        }

        public void SetContent(string content)
        {

            Content = content + '\n';
            UpdateUserInterface();

            if (!_entMan.TryGetComponent(Owner, out AppearanceComponent? appearance))
                return;

            var status = string.IsNullOrWhiteSpace(content)
                ? PaperStatus.Blank
                : PaperStatus.Written;

            appearance.SetData(PaperVisuals.Status, status);
        }

        public void UpdateUserInterface()
        {
            UserInterface?.SetState(new PaperBoundUserInterfaceState(Content, Mode));
        }

        public void Examine(FormattedMessage message, bool inDetailsRange)
        {
            if (!inDetailsRange)
                return;
            if (Content == "")
                return;

            message.AddMarkup(
                Loc.GetString(
                    "paper-component-examine-detail-has-words"
                )
            );
        }

        private void OnUiReceiveMessage(ServerBoundUserInterfaceMessage obj)
        {
            var msg = (PaperInputText) obj.Message;
            if (string.IsNullOrEmpty(msg.Text))
                return;


            if (msg.Text.Length + Content.Length <= ContentSize)
                Content += msg.Text + '\n';

            if (_entMan.TryGetComponent(Owner, out AppearanceComponent? appearance))
            {
                appearance.SetData(PaperVisuals.Status, PaperStatus.Written);
            }

            _entMan.GetComponent<MetaDataComponent>(Owner).EntityDescription = "";
            UpdateUserInterface();
        }

        async Task<bool> IInteractUsing.InteractUsing(InteractUsingEventArgs eventArgs)
        {
            if (!EntitySystem.Get<TagSystem>().HasTag(eventArgs.Using, "Write"))
                return false;
            if (!_entMan.TryGetComponent(eventArgs.User, out ActorComponent? actor))
                return false;

            Mode = PaperAction.Write;
            UpdateUserInterface();
            UserInterface?.Open(actor.PlayerSession);
            return true;
        }
    }
}
