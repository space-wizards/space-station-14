using Content.Server.Weapon.Ranged.Ammunition.Components;
using Content.Shared.Verbs;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;

namespace Content.Server.Weapon.Ranged.Ammunition
{
    public sealed class AmmunitionSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<AmmoBoxComponent, GetAlternativeVerbsEvent>(AddDumpVerb);
        }

        private void AddDumpVerb(EntityUid uid, AmmoBoxComponent component, GetAlternativeVerbsEvent args)
        {
            if (args.Hands == null || !args.CanAccess || !args.CanInteract)
                return;

            if (component.AmmoLeft == 0)
                return;

            Verb verb = new();
            verb.Text = Loc.GetString("dump-vert-get-data-text");
            verb.IconTexture = "/Textures/Interface/VerbIcons/eject.svg.192dpi.png";
            verb.Act = () => component.EjectContents(10);
            args.Verbs.Add(verb);
        }
    }
}
