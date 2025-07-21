using System.Linq;
using Content.Server.Damage.Components;
using Content.Server.Destructible;
using Content.Server.Destructible.Thresholds.Triggers;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Examine;
using Content.Shared.Rounding;
using Robust.Shared.Prototypes;

namespace Content.Server.Damage.Systems;

public sealed class ExaminableDamageSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ExaminableDamageComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<ExaminableDamageComponent, ExaminedEvent>(OnExamine);
    }

    private void OnInit(EntityUid uid, ExaminableDamageComponent component, ComponentInit args)
    {
        if (component.MessagesProtoId == null)
            return;
        component.MessagesProto = _prototype.Index<ExaminableDamagePrototype>(component.MessagesProtoId);
    }

    private void OnExamine(EntityUid uid, ExaminableDamageComponent component, ExaminedEvent args)
    {
        if (component.MessagesProto == null)
            return;

        var messages = component.MessagesProto.Messages;
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

        if (component.MessagesProto == null)
            return 0;

        var maxLevels = component.MessagesProto.Messages.Length - 1;
        if (maxLevels <= 0)
            return 0;

        var trigger = (DamageTrigger?) destructible.Thresholds
            .LastOrDefault(threshold => threshold.Trigger is DamageTrigger)?.Trigger;
        if (trigger == null)
            return 0;

        var damage = damageable.TotalDamage;
        var damageThreshold = trigger.Damage;
        var fraction = damageThreshold == 0 ? 0f : (float) damage / damageThreshold;

        var level = ContentHelpers.RoundToNearestLevels(fraction, 1, maxLevels);
        return level;
    }
}
