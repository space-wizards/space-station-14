using Content.Shared.Paper;

namespace Content.Shared.Signature;

public abstract class SharedSignatureSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<PaperComponent, SignatureSubmitMessage>(OnSubmitSignature);
    }

    private void OnSubmitSignature(Entity<PaperComponent> ent, ref SignatureSubmitMessage args)
    {
        var signature = EnsureComp<SignatureComponent>(ent);

        var changedSignature = signature.Data == null || !signature.Data.Equals(args.Data);

        if (changedSignature)
        {
            signature.Data = args.Data;
            Dirty(ent.Owner, signature);
        }

        AfterSubmitSignature((ent.Owner, ent.Comp, signature), ref args, changedSignature);
    }

    protected virtual void AfterSubmitSignature(Entity<PaperComponent, SignatureComponent> ent, ref SignatureSubmitMessage args, bool changedSignature) { }
}
