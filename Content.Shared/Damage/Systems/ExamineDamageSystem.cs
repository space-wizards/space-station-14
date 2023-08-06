using Content.Shared.FixedPoint;
using Robust.Shared.Utility;

namespace Content.Shared.Damage.Systems;

public sealed class ExamineDamageSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
    }

    /// <summary>
    /// Retrieves the damage examine values.
    /// </summary>
    public FormattedMessage GetDamageExamine(DamageSpecifier damageSpecifier, string? type = null)
    {
        var msg = new FormattedMessage();

        if (string.IsNullOrEmpty(type))
        {
            msg.AddMarkup(Loc.GetString("damage-examine"));
        }
        else
        {
            msg.AddMarkup(Loc.GetString("damage-examine-type", ("type", type)));
        }

        foreach (var damage in damageSpecifier.DamageDict)
        {
            if (damage.Value != FixedPoint2.Zero)
            {
                msg.PushNewline();
                msg.AddMarkup(Loc.GetString("damage-value", ("type", damage.Key), ("amount", damage.Value)));
            }
        }

        return msg;
    }
}
