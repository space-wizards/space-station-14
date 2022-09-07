using Content.Server.Projectiles.Components;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server.Weapon.Ranged.Systems;

public sealed partial class GunSystem
{
    protected override void InitializeCartridge()
    {
        base.InitializeCartridge();
        SubscribeLocalEvent<CartridgeAmmoComponent, ExaminedEvent>(OnCartridgeExamine);
        SubscribeLocalEvent<CartridgeAmmoComponent, GetVerbsEvent<ExamineVerb>>(OnCartridgeVerbExamine);
    }

    private void OnCartridgeVerbExamine(EntityUid uid, CartridgeAmmoComponent component, GetVerbsEvent<ExamineVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        var projectile = GetProjectile(component.Prototype);

        if (projectile == null)
            return;

        var verb = new ExamineVerb()
        {
            Act = () =>
            {
                var markup = GetCartridgeMarkup(component.Prototype);
                _examine.SendExamineTooltip(args.User, uid, markup, false, false);
            },
            Text = Loc.GetString("damage-examinable-verb-text"),
            Message = Loc.GetString("damage-examinable-verb-message"),
            Category = VerbCategory.Examine,
            IconTexture = "/Textures/Interface/VerbIcons/smite.svg.192dpi.png"
        };

        args.Verbs.Add(verb);
    }

    private ProjectileComponent? GetProjectile(string proto)
    {
        if (ProtoManager.Index<EntityPrototype>(proto).Components
            .TryGetValue(_factory.GetComponentName(typeof(ProjectileComponent)), out var projectile))
        {
            var p = (ProjectileComponent) projectile.Component;

            if (p.Damage.Total > FixedPoint2.Zero)
            {
                return p;
            }
        }

        return null;
    }

    private FormattedMessage GetCartridgeMarkup(string proto)
    {
        var projectile = GetProjectile(proto);

        if (projectile == null)
            return new FormattedMessage();

        var msg = new FormattedMessage();
        msg.AddMarkup(Loc.GetString("damage-examine"));

        foreach (var damage in projectile.Damage.DamageDict)
        {
            msg.PushNewline();
            msg.AddMarkup(Loc.GetString("damage-value", ("type", damage.Key), ("amount", damage.Value)));
        }

        return msg;
    }

    private void OnCartridgeExamine(EntityUid uid, CartridgeAmmoComponent component, ExaminedEvent args)
    {
        if (component.Spent)
        {
            args.PushMarkup(Loc.GetString("gun-cartridge-spent"));
        }
        else
        {
            args.PushMarkup(Loc.GetString("gun-cartridge-unspent"));
        }
    }
}
