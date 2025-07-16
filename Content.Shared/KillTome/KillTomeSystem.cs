using Content.Shared.Administration.Logs;
using Content.Shared.Damage;
using Content.Shared.Database;
using Content.Shared.Humanoid;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Paper;
using Content.Shared.Popups;
using Robust.Shared.Timing;

namespace Content.Shared.KillTome;

/// <summary>
/// This handles KillTome functionality.
/// </summary>
///
/// Death Note Rules:
/// 1. One humanoid can be killed by Death Note only once.
/// 2. If the name, that is shared by multiple humanoid, is written, random humanoid with that name dies.
/// 5. Writing a name should look like this: "{Name}, {KillDelay}" (John Marston, 40)
public sealed class KillTomeSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly DamageableSystem _damageSystem = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogs = default!;

    // to keep a track of already killed people so they won't be killed again
    private readonly HashSet<EntityUid> _killedEntities = [];

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<KillTomeComponent, PaperAfterWriteEvent>(OnPaperAfterWriteInteract);
    }


    public override void Update(float frameTime)
    {
        // This is used to check if the Kill Tome target is still valid.
        // If the target is not valid, we remove the component.
        var query = EntityQueryEnumerator<KillTomeTargetComponent>();

        while (query.MoveNext(out var uid, out var targetComp))
        {
            if (_gameTiming.CurTime < targetComp.KillTime)
                continue;

            RemCompDeferred<KillTomeTargetComponent>(uid);

            Kill(uid);
        }
    }

    private void OnPaperAfterWriteInteract(Entity<KillTome.KillTomeComponent> ent, ref PaperAfterWriteEvent args)
    {
        // if the entity is not a paper, we don't do anything
        if (!TryComp<PaperComponent>(ent.Owner, out var paper))
            return;

        var content = paper.Content;

        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        var showPopup = false;

        foreach (var line in lines)
        {
            if (string.IsNullOrEmpty(line))
                continue;

            var parts = line.Split(',', 2, StringSplitOptions.RemoveEmptyEntries);

            var name = parts[0].Trim();

            var delay = 40f;

            if (parts.Length == 2 && int.TryParse(parts[1].Trim(), out var parsedDelay) && parsedDelay > 0)
                delay = parsedDelay;

            if (!CheckIfEligible(name, out var uid))
            {
                continue;
            }

            showPopup = true;

            var killTime = _gameTiming.CurTime + TimeSpan.FromSeconds(delay);

            var targetComp = new KillTomeTargetComponent(delay, killTime);

            AddComp(uid, targetComp);

            _killedEntities.Add(uid);

            _adminLogs.Add(LogType.Chat,
                LogImpact.High,
                $"{ToPrettyString(args.Actor)} has written {ToPrettyString(uid)}'s name in Kill Tome.");
        }

        // If we have written at least one eligible name, we show the popup (So the player knows death note worked).
        if(showPopup)
            _popupSystem.PopupEntity(Loc.GetString("killtome-kill-success"), ent.Owner, args.Actor, PopupType.Large);
    }

    // A person to be killed by KillTome must:
    // 1. be with the name
    // 2. have HumanoidAppearanceComponent (so it targets only humanoids, obv)
    // 3. not be already dead
    // 4. not be already killed by Death Note

    // If all these conditions are met, we return true and the entityUid of the person to kill.
    private bool CheckIfEligible(string name, out EntityUid entityUid)
    {
        if (!TryFindEntityByName(name, out var uid) ||
            !TryComp<MobStateComponent>(uid, out var mob))
        {
            entityUid = default;
            return false;
        }

        if (_killedEntities.Contains(uid))
        {
            entityUid = default;
            return false;
        }

        if (mob.CurrentState == MobState.Dead)
        {
            entityUid = default;
            return false;
        }

        entityUid = uid;
        return true;
    }

    private bool TryFindEntityByName(string name, out EntityUid entityUid)
    {
        var query = EntityQueryEnumerator<HumanoidAppearanceComponent>();

        while(query.MoveNext(out var uid, out var _))
        {
            if (!Name(uid).Equals(name, StringComparison.OrdinalIgnoreCase))
                continue;

            entityUid = uid;
            return true;
        }

        entityUid = default;
        return false;
    }

    private void Kill(EntityUid uid)
    {
        var damage = new DamageSpecifier();
        damage.DamageDict.Add("Brute", 200);

        if (!TryComp<DamageableComponent>(uid, out var comp))
            return;

        _damageSystem.SetDamage(uid, comp, damage);

        _killedEntities.Add(uid);
    }
}
