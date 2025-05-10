using Content.Shared.Access.Systems;
using Content.Shared.Paper;
using Content.Shared.Verbs;

namespace Content.Shared.DV.Paper;

public abstract class SharedSignatureSystem : EntitySystem
{
    [Dependency] private readonly PaperSystem _paper = default!;
    [Dependency] private readonly SharedIdCardSystem _idCard = default!;

    // The sprite used to visualize "signatures" on paper entities.
    public const string SignatureStampState = "paper_stamp-signature";

    public override void Initialize()
    {
        SubscribeLocalEvent<PaperComponent, GetVerbsEvent<AlternativeVerb>>(OnGetAltVerbs);
    }

    private void OnGetAltVerbs(Entity<PaperComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        if (args.Using is not { } pen)
            return;

        if (!TryComp<SignatureWriterComponent>(pen, out var signatureComp))
            return;

        var user = args.User;
        AlternativeVerb verb = new()
        {
            Act = () =>
            {
                TrySignPaper(ent, user, pen, signatureComp);
            },
            Text = Loc.GetString("paper-sign-verb"),
            DoContactInteraction = true,
            Priority = 10
        };
        args.Verbs.Add(verb);
    }

    /// <summary>
    ///     Tries add add a signature to the paper with signer's name.
    /// </summary>
    public bool TrySignPaper(Entity<PaperComponent> paper, EntityUid signer, EntityUid pen, SignatureWriterComponent signatureComp)
    {
        var ev = new SignAttemptEvent(paper, signer, pen);
        RaiseLocalEvent(pen, ref ev);
        if (ev.Cancelled)
            return false;

        _paper.UpdateUserInterface(paper);

        return true;
    }

    public string DetermineEntitySignature(EntityUid signer, EntityUid pen)
    {
        // imp - if there's a signature override, return that.
        if (TryComp<SignatureWriterComponent>(pen, out var signatureComp) && signatureComp.NameOverride != null)
            return signatureComp.NameOverride;

        // If the entity has an ID, use the name on it.
        if (_idCard.TryFindIdCard(signer, out var id) && !string.IsNullOrWhiteSpace(id.Comp.FullName))
        {
            return id.Comp.FullName;
        }

        // Alternatively, return the entity name
        return Name(signer);
    }
}
