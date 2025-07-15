using Content.Shared.Administration.Logs;
using Content.Shared.Damage;
using Content.Shared.Database;
using Content.Shared.Humanoid;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Paper;
using Content.Shared.Popups;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Shared.DeathNote;

/// <summary>
/// This handles Death Note functionality.
/// </summary>
///
/// Death Note Rules:
/// 1. One humanoid can be killed by Death Note only once.
/// 2. If the name, that is shared by multiple humanoid, is written, every humanoid with that name dies.
/// 3. One Death Note can kill only 5 humanoids.
/// 4. In order to use Death Note, one should pay 10 000 credits for each name.
/// 5. Writing a name should look like this: "{Name}, {KillDelay}" (John Marston, 40)
public sealed class DeathNoteSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly DamageableSystem _damageSystem = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogs = default!;
    [Dependency] private readonly PaperSystem _paperSystem = default!;

    // to keep a track of already killed people so they won't be killed again
    private readonly HashSet<EntityUid> _killedEntities = [];

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<DeathNoteComponent, PaperAfterWriteEvent>(OnPaperAfterWriteInteract);
        SubscribeLocalEvent<DeathNoteComponent, InteractEvent>(OnInteract);
    }


    public override void Update(float frameTime)
    {
        // This is used to check if the Death Note target is still valid.
        // If the target is not valid, we remove the component.
        var query = EntityQueryEnumerator<DeathNoteTargetComponent>();

        while (query.MoveNext(out var uid, out var targetComp))
        {
            if (_gameTiming.CurTime < targetComp.KillTime)
                continue;

            RemCompDeferred<DeathNoteTargetComponent>(uid);

            Kill(uid);
        }
    }

    private void OnInteract(Entity<DeathNoteComponent> ent, ref InteractEvent args)
    {
        ent.Comp.TouchedBy.Add(args.User);
    }

    private void OnPaperAfterWriteInteract(Entity<DeathNoteComponent> ent, ref PaperAfterWriteEvent args)
    {
        // if the entity is not a paper, we don't do anything
        if (!TryComp<PaperComponent>(ent.Owner, out var paper))
            return;

        var content = paper.Content;

        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        var showPopup = false;

        foreach (var line in lines)
        {
            Log.Debug($"Processing line: {line}");
            if (string.IsNullOrEmpty(line))
                continue;

            var parts = line.Split(',', 2, StringSplitOptions.RemoveEmptyEntries);

            var name = parts[0].Trim();

            var delay = 40f;

            if (parts.Length == 2 && int.TryParse(parts[1].Trim(), out var parsedDelay) && parsedDelay > 0)
                delay = parsedDelay;

            Log.Debug($"Line processed: {name} - {delay}");

            if (!CheckIfEligible(name, out var uid))
            {
                continue;
            }

            showPopup = true;

            var killTime = _gameTiming.CurTime + TimeSpan.FromSeconds(delay);

            var targetComp = new DeathNoteTargetComponent(delay, killTime);

            AddComp(uid, targetComp);

            _killedEntities.Add(uid);

            _adminLogs.Add(LogType.Chat,
                LogImpact.High,
                $"{Name(args.Actor)} has written {name} in the Death Note. Target UID: {uid}");
        }

        // If we have written at least one eligible name, we show the popup (So the player knows death note worked).
        if(showPopup)
            _popupSystem.PopupEntity("The name is written. The countdown begins.", ent.Owner, args.Actor, PopupType.Large);
    }

    // A person to be killed by DeathNote must:
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

        Log.Debug($"{name} is eligible for Death Note.");
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
