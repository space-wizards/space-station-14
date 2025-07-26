using Content.Shared.Damage;
using Content.Shared.Mind;
using Robust.Shared.Timing;

namespace Content.Server.GhostTypes;

public sealed class GhostSpriteStateSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public void SetGhostSprite(EntityUid ent, MindComponent mind, GhostSpriteStateComponent state)
    {
        if (!TryComp<AppearanceComponent>(ent, out var appearance))
            return;

        var highestType = new List<string>();
        if (TryComp<DamageableComponent>(mind.CurrentEntity, out var damageComp))
        {
            highestType = _damageable.GetHighestDamageTypes(damageComp.DamagePerGroup, damageComp.Damage);
        }
        else if (mind.DamagePerGroup != null && mind.Damage != null)
        {
            highestType = _damageable.GetHighestDamageTypes(mind.DamagePerGroup, mind.Damage);
        }
        else
            return;

        if (highestType.Count == 0)
            return;
        highestType.Sort();

        string spriteState = "wawa";
        var random = new System.Random((int)_timing.CurTick.Value);
        //check length  (yeah i gotta smooth this whole thingi dw)
        if (highestType is ["Blunt", "Heat", "Piercing"])
        {
            var number = random.Next(1, 3);
            spriteState = "explosion" + number;
            Log.Debug($"THINGI EXPLOSION!! {spriteState}");
        }
        else if ( highestType.Count == 1)
        {
            if (highestType[0] == "Blunt"
                || highestType[0] == "Slash"
                || highestType[0] == "Pierce")
            {
                var number = random.Next(1, 3);
                spriteState = highestType[0] + number;
                Log.Debug($"THINGI CHOOSING FROM 3!! {spriteState}");
            }
            else
            {
                spriteState = highestType[0];
                Log.Debug($"THINGI JUST CHOSING 1!! {spriteState}");
            }
        }
        else // if it doesn't fall into any specific category, choose randomly
            spriteState = highestType[random.Next(0, highestType.Count - 1)];

        var properStateName = (state.Prefix + spriteState).ToLower();
        Log.Debug($"LAST THINGI {properStateName}");
        _appearance.SetData(ent, GhostVisuals.Damage, properStateName, appearance);
    }
}
