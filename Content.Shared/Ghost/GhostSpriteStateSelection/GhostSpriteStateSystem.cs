using Content.Shared.Damage;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.Ghost.GhostSpriteStateSelection;

public sealed class GhostSpriteStateSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GhostSpriteStateComponent, GhostSpriteEvent>(SetGhostSprite);
    }

    private void SetGhostSprite(Entity<GhostSpriteStateComponent> ent, ref GhostSpriteEvent args)
    {
        if(!TryComp<AppearanceComponent>(ent, out var appearance))
            return;

        var spriteState = "";
        var random = new System.Random((int)_timing.CurTick.Value);

        if (TryComp<DamageableComponent>(args.Uid, out var damageComp))
        {
            var highestType = _damageable.GetHighestDamageTypes(damageComp);
            highestType.Sort();
            //check length
            if (highestType.Count == 1) // if its 1, just use that sprite
            {
                spriteState = highestType[0];
            }
            else if (highestType is ["Blunt", "Heat", "Piercing"])  // specific case for explosions
            {
                spriteState = "Explosion";
            }
            else // if it doesn't fall into any specific category, choose randomly
            {
                spriteState = highestType[random.Next(0, highestType.Count - 1)];
            }
        }

        var properStateName = (ent.Comp.prefix + spriteState).ToLower();
        _appearance.SetData(ent, GhostVisuals.Damage, properStateName, appearance);
    }
}
