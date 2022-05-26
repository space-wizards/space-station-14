using Content.Shared.Verbs;

namespace Content.Server.Administration.Components;

public sealed class SmiteableSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<SmiteableComponent, GetVerbsEvent<Verb>>(AddSmites);
    }

    private void AddSmites(EntityUid uid, SmiteableComponent component, GetVerbsEvent<Verb> args)
    {
        var explode = new Verb()
        {
            ConfirmationPopup = true,
            Category = VerbCategory.Smite,
            IconTexture = "/Textures/Interface/VerbIcons/smite.svg.192dpi.png",
            Priority = 0,

        };
    }
}
