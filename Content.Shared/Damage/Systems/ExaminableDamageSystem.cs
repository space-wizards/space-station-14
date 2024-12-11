using System.Linq;
using Content.Shared.Destructible;
using Content.Shared.Destructible.Thresholds.Triggers;
using Content.Shared.Examine;
using Content.Shared.Rounding;
using Robust.Shared.Prototypes;
using ExaminableDamageComponent = Content.Shared.Damage.Components.ExaminableDamageComponent;

namespace Content.Shared.Damage.Systems;

public sealed class ExaminableDamageSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ExaminableDamageComponent, ExaminedEvent>(OnExamine);
    }

    private void OnExamine(EntityUid uid, ExaminableDamageComponent component, ExaminedEvent args)
    {
        if (!_prototype.TryIndex(component.MessagesProtoId, out var proto))
            return;

        var messages = proto.Messages;

        if (messages.Length == 0)
            return;

        var level = GetDamageLevel(uid, component);
        var msg = Loc.GetString(messages[level]);
        args.PushMarkup(msg,-99);
    }

    private int GetDamageLevel(EntityUid uid, ExaminableDamageComponent? component = null,
        DamageableComponent? damageable = null, DestructibleComponent? destructible = null)
    {
        if (!Resolve(uid, ref component, ref damageable, ref destructible))
            return 0;

        if (!_prototype.TryIndex(component.MessagesProtoId, out var proto))
            return 0;

        var maxLevels = proto.Messages.Length - 1;
        if (maxLevels <= 0)
            return 0;

        var trigger = (DamageTrigger?) destructible.Thresholds
            .LastOrDefault(threshold => threshold.Trigger is DamageTrigger)
            ?.Trigger;

        if (trigger == null)
            return 0;

        var damage = damageable.TotalDamage;
        var damageThreshold = trigger.Damage;
        var fraction = damageThreshold == 0 ? 0f : (float) damage / damageThreshold;

        var level = ContentHelpers.RoundToNearestLevels(fraction, 1, maxLevels);
        return level;
    }
}
