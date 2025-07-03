using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Robust.Client.UserInterface.RichText;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;
using Robust.Shared.IoC;
using Robust.Client.Player;
using Robust.Shared.GameObjects;
using Content.Shared.Humanoid;

using Content.Client.Paper.UI;

namespace Content.Client.UserInterface.RichText
{
    public sealed class SignatureTagHandler : IMarkupTagHandler
    {
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;

        public string Name => "signature";
        private static int _signatureCounter = 0;

        private static int GetSignatureIndex(MarkupNode node)
        {
            return _signatureCounter++;
        }

        public static void ResetSignatureCounter()
        {
            _signatureCounter = 0;
        }

        public SignatureTagHandler()
        {
            IoCManager.InjectDependencies(this);
        }

        public void PushDrawContext(MarkupNode node, MarkupDrawingContext context) { }
        public void PopDrawContext(MarkupNode node, MarkupDrawingContext context) { }

        public string TextBefore(MarkupNode node) => "";
        public string TextAfter(MarkupNode node) => "";

        public bool TryCreateControl(MarkupNode node, [NotNullWhen(true)] out Control? control)
        {
            var btn = new Button
            {
                Text = "Sign",
                MinSize = new Vector2(60, 28),
                MaxSize = new Vector2(60, 28),
                Margin = new Thickness(4, 2, 4, 2)
            };

            // Store signature index in the button's Name property
            var signatureIndex = GetSignatureIndex(node);
            btn.Name = $"signature_{signatureIndex}";

            btn.OnPressed += _ =>
            {
                var signature = GetPlayerSignature();

                // Find the PaperWindow parent
                var parent = btn.Parent;
                while (parent != null && parent is not PaperWindow)
                    parent = parent.Parent;

                if (parent is PaperWindow paperWindow)
                    paperWindow.ReplaceSignature(signatureIndex, signature);
            };

            control = btn;
            return true;
        }

        private string GetPlayerSignature()
        {
            var playerEntity = _playerManager.LocalSession?.AttachedEntity;
            if (playerEntity == null)
                return "[Unknown Signature]";

            var name = "Unknown";

            // Get character name
            if (_entityManager.TryGetComponent<MetaDataComponent>(playerEntity.Value, out var meta))
                name = meta.EntityName;

            return name;
        }
    }
}
