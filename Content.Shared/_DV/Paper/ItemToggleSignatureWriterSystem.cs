using Content.Shared.Item.ItemToggle.Components;

namespace Content.Shared.DV.Paper;

public sealed class ItemToggleSignatureWriterSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ItemToggleSignatureWriterComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ItemToggleSignatureWriterComponent, ItemToggledEvent>(OnToggleItem);
    }

    private void OnMapInit(Entity<ItemToggleSignatureWriterComponent> ent, ref MapInitEvent args)
    {
        if (!TryComp<SignatureWriterComponent>(ent, out var signature))
            return;

        if (signature.Font is { } font)
            ent.Comp.DeactivatedFont = font;

        if (signature.Color is { } color)
            ent.Comp.DeactivatedColor = color;

        if (signature.ColorList is { } colorList)
            ent.Comp.DeactivatedColorList = colorList;
    }

    private void OnToggleItem(Entity<ItemToggleSignatureWriterComponent> ent, ref ItemToggledEvent args)
    {
        if (args.Activated)
        {
            // Remove signature writing if no activated data is provided
            if (ent.Comp.ActivatedFont == null &&
                ent.Comp.ActivatedColor == null &&
                ent.Comp.ActivatedColorList.Count == 0)
            {
                RemComp<SignatureWriterComponent>(ent);
                return;
            }

            var signature = EnsureComp<SignatureWriterComponent>(ent);

            if (ent.Comp.ActivatedColor is { } color)
                signature.Color = color;

            if (ent.Comp.ActivatedColorList is { } colorList)
                signature.ColorList = colorList;

            if (ent.Comp.ActivatedFont is { } font)
                signature.Font = font;
        }
        else
        {
            // Remove signature writing if no deactivated data is provided
            if (ent.Comp.DeactivatedFont == null &&
                ent.Comp.DeactivatedColor == null &&
                ent.Comp.DeactivatedColorList.Count == 0)
            {
                RemComp<SignatureWriterComponent>(ent);
                return;
            }

            var signature = EnsureComp<SignatureWriterComponent>(ent);

            if (ent.Comp.DeactivatedColor is { } color)
                signature.Color = color;

            if (ent.Comp.DeactivatedColorList is { } colorList)
                signature.ColorList = colorList;

            if (ent.Comp.DeactivatedFont is { } font)
                signature.Font = font;
        }
    }
}
