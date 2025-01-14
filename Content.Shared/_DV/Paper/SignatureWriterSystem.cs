using Content.Shared.Popups;
using Content.Shared.Verbs;
using System.Linq;

namespace Content.Shared.DV.Paper;

public sealed class SignatureWriterSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SignatureWriterComponent, ComponentInit>(OnCompInit);
        SubscribeLocalEvent<SignatureWriterComponent, GetVerbsEvent<AlternativeVerb>>(OnGetAltVerbs);
    }

    private void OnCompInit(EntityUid uid, SignatureWriterComponent comp, ref ComponentInit args)
    {
        if (comp.ColorList.Count >= 1)
            comp.Color = comp.ColorList.First().Value;
    }

    private void OnGetAltVerbs(EntityUid uid, SignatureWriterComponent comp, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        // Font selection
        if (comp.FontList.Count >= 2)
        {
            var priority = 0;

            foreach (var entry in comp.FontList)
            {
                AlternativeVerb selection = new()
                {
                    Text = entry.Key,
                    Category = FontSelect,
                    Priority = priority,
                    Act = () =>
                    {
                        comp.Font = entry.Value;
                        _popup.PopupPredicted(Loc.GetString("signature-writer-component-font-set", ("font", entry.Key)), args.User, args.User);
                    }
                };

                priority--;
                args.Verbs.Add(selection);
            }
        }

        // Color selection
        if (comp.ColorList.Count >= 2)
        {
            var priority = 0;

            foreach (var entry in comp.ColorList)
            {
                AlternativeVerb selection = new()
                {
                    Text = entry.Key,
                    Category = ColorSelect,
                    Priority = priority,
                    Act = () =>
                    {
                        comp.Color = entry.Value;
                        _popup.PopupPredicted(Loc.GetString("signature-writer-component-color-set", ("color", entry.Key)), args.User, args.User);
                    }
                };

                priority--;
                args.Verbs.Add(selection);
            }
        }
    }

    private static readonly VerbCategory FontSelect = new("verb-categories-signature-font-select", null);

    private static readonly VerbCategory ColorSelect = new("verb-categories-signature-color-select", null);
}
