using Content.Shared.Explosion.Components;
using Content.Shared.Armor;

namespace Content.Shared.Explosion.EntitySystems;

public abstract class SharedExplosionSystem : EntitySystem
{

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ExplosionResistanceComponent, ArmorExamineEvent>(OnArmorExamine);
    }

    private void OnArmorExamine(EntityUid uid, ExplosionResistanceComponent component, ref ArmorExamineEvent args)
    {
        var value = MathF.Round((1f - component.DamageCoefficient) * 100, 1);

        if (value == 0)
            return;

        args.Msg.PushNewline();
        args.Msg.AddMarkupOrThrow(Loc.GetString(component.Examine, ("value", value)));
    }
}
