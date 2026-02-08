using System.Diagnostics.CodeAnalysis;
using System.Text;
using Content.Client.Stylesheets.Fonts;
using Content.Shared.Access.Components;
using Robust.Client.Graphics;
using Robust.Shared.Enums;

namespace Content.Client.Access;

public sealed class AccessOverlay : Overlay
{
    private const int TextFontSize = 12;

    private readonly IEntityManager _entityManager;
    private readonly IFontSelectionManager _fontSelection;
    private readonly SharedTransformSystem _transformSystem;
    private Font _font;

    public override OverlaySpace Space => OverlaySpace.ScreenSpace;

    public AccessOverlay(IEntityManager entityManager, IFontSelectionManager fontSelection, SharedTransformSystem transformSystem)
    {
        _entityManager = entityManager;
        _transformSystem = transformSystem;
        _fontSelection = fontSelection;

        UpdateFont();
        _fontSelection.OnFontChanged += OnFontChanged;
    }

    protected override void DisposeBehavior()
    {
        base.DisposeBehavior();

        _fontSelection.OnFontChanged -= OnFontChanged;
    }

    private void OnFontChanged(StandardFontType type)
    {
        if (type == StandardFontType.Main)
            UpdateFont();
    }

    [MemberNotNull(nameof(_font))]
    private void UpdateFont()
    {
        _font = _fontSelection.GetFont(StandardFontType.Main, TextFontSize);
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (args.ViewportControl == null)
            return;

        var textBuffer = new StringBuilder();
        var query = _entityManager.EntityQueryEnumerator<AccessReaderComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var accessReader, out var transform))
        {
            textBuffer.Clear();

            var entityName = _entityManager.ToPrettyString(uid);
            textBuffer.AppendLine(entityName.Prototype);
            textBuffer.Append("UID: ");
            textBuffer.Append(entityName.Uid.Id);
            textBuffer.Append(", NUID: ");
            textBuffer.Append(entityName.Nuid.Id);
            textBuffer.AppendLine();

            if (!accessReader.Enabled)
            {
                textBuffer.AppendLine("-Disabled");
                continue;
            }

            if (accessReader.AccessLists.Count > 0)
            {
                var groupNumber = 0;
                foreach (var accessList in accessReader.AccessLists)
                {
                    groupNumber++;
                    foreach (var entry in accessList)
                    {
                        textBuffer.Append("+Set ");
                        textBuffer.Append(groupNumber);
                        textBuffer.Append(": ");
                        textBuffer.Append(entry.Id);
                        textBuffer.AppendLine();
                    }
                }
            }
            else
            {
                textBuffer.AppendLine("+Unrestricted");
            }

            foreach (var key in accessReader.AccessKeys)
            {
                textBuffer.Append("+Key ");
                textBuffer.Append(key.OriginStation);
                textBuffer.Append(": ");
                textBuffer.Append(key.Id);
                textBuffer.AppendLine();
            }

            foreach (var tag in accessReader.DenyTags)
            {
                textBuffer.Append("-Tag ");
                textBuffer.AppendLine(tag.Id);
            }

            var accessInfoText = textBuffer.ToString();
            var screenPos = args.ViewportControl.WorldToScreen(_transformSystem.GetWorldPosition(transform));
            args.ScreenHandle.DrawString(_font, screenPos, accessInfoText, Color.Gold);
        }
    }
}
