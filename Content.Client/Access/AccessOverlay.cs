using System.Text;
using Content.Client.Resources;
using Content.Shared.Access.Components;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Enums;

namespace Content.Client.Access;

public sealed class AccessOverlay : Overlay
{
    private readonly IEntityManager _entityManager;
    private readonly EntityLookupSystem _lookup;
    private readonly SharedTransformSystem _xform;
    private readonly Font _font;

    public override OverlaySpace Space => OverlaySpace.ScreenSpace;

    public AccessOverlay(IEntityManager entManager, IClientResourceCache cache, EntityLookupSystem lookup, SharedTransformSystem xform)
    {
        _entityManager = entManager;
        _lookup = lookup;
        _xform = xform;

        _font = cache.GetFont("/Fonts/NotoSans/NotoSans-Regular.ttf", 12);
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (args.ViewportControl == null)
            return;

        var readerQuery = _entityManager.GetEntityQuery<AccessReaderComponent>();
        var xformQuery = _entityManager.GetEntityQuery<TransformComponent>();

        foreach (var ent in _lookup.GetEntitiesIntersecting(args.MapId, args.WorldAABB,
                         LookupFlags.Static | LookupFlags.Approximate))
        {
            if (!readerQuery.TryGetComponent(ent, out var reader) ||
                !xformQuery.TryGetComponent(ent, out var xform))
            {
                continue;
            }

            var text = new StringBuilder();
            var index = 0;
            var a = $"{_entityManager.ToPrettyString(ent)}";
            text.Append(a);

            foreach (var list in reader.AccessLists)
            {
                a = $"Tag {index}";
                text.AppendLine(a);

                foreach (var entry in list)
                {
                    a = $"- {entry}";
                    text.AppendLine(a);
                }

                index++;
            }

            string textStr;

            if (text.Length >= 2)
            {
                textStr = text.ToString();
                textStr = textStr[..^2];
            }
            else
            {
                textStr = "";
            }

            var screenPos = args.ViewportControl.WorldToScreen(_xform.GetWorldPosition(xform));

            args.ScreenHandle.DrawString(_font, screenPos, textStr, Color.Gold);
        }
    }
}
