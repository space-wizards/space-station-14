using System.Diagnostics.CodeAnalysis;
using Content.Shared.Administration.Logs;
using Content.Shared.Damage;
using Content.Shared.Database;
using Content.Shared.Humanoid;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.NameModifier.EntitySystems;
using Content.Shared.Paper;
using Content.Shared.Popups;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.KillTome;

/// <summary>
/// This handles KillTome functionality.
/// </summary>

///     Kill Tome Rules:
// 1. The humanoid whose name is written in this note shall die.
// 2. If the name is shared by multiple humanoids, a random humanoid with that name will die.
// 3. Each name shall be written on a new line.
// 4. Names must be written in the format: "Name, Delay (in seconds)" (e.g., John Doe, 40).
// 5. A humanoid can be killed by the same Kill Tome only once.
public sealed class KillTomeSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly DamageableSystem _damageSystem = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogs = default!;
    [Dependency] private readonly NameModifierSystem _nameModifierSystem = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<KillTomeComponent, PaperAfterWriteEvent>(OnPaperAfterWriteInteract);
    }

    public override void Update(float frameTime)
    {
        // Getting all the entities that are targeted by Kill Tome and checking if their kill time has passed.
        // If it has, we kill them and remove the KillTomeTargetComponent.
        var query = EntityQueryEnumerator<KillTomeTargetComponent>();

        while (query.MoveNext(out var uid, out var targetComp))
        {
            if (_gameTiming.CurTime < targetComp.KillTime)
                continue;

            // The component doesn't get removed fast enough and the update loop will run through it a few more times.
            // This check is here to ensure it will not spam popups or kill you several times over.
            if (targetComp.Dead)
                continue;

            Kill(uid, targetComp);

            _popupSystem.PopupPredicted(Loc.GetString("killtome-death"),
                Loc.GetString("killtome-death-others", ("target", uid)),
                uid,
                uid,
                PopupType.LargeCaution);

            targetComp.Dead = true;

            RemCompDeferred<KillTomeTargetComponent>(uid);
        }
    }

    private void OnPaperAfterWriteInteract(Entity<KillTomeComponent> ent, ref PaperAfterWriteEvent args)
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

            var delay = ent.Comp.DefaultKillDelay;

            if (parts.Length == 2 && Parse.TryInt32(parts[1].Trim(), out var parsedDelay) && parsedDelay > 0)
                delay = TimeSpan.FromSeconds(parsedDelay);

            if (!CheckIfEligible(name, ent.Comp, out var uid))
            {
                continue;
            }

            // Compiler will complain if we don't check for null here.
            if (uid is not { } realUid)
                continue;

            showPopup = true;

            EnsureComp<KillTomeTargetComponent>(realUid, out var targetComp);

            targetComp.KillTime = _gameTiming.CurTime + delay;
            targetComp.Damage = ent.Comp.Damage;

            Dirty(realUid, targetComp);

            ent.Comp.KilledEntities.Add(realUid);

            Dirty(ent);

            _adminLogs.Add(LogType.Chat,
                LogImpact.High,
                $"{ToPrettyString(args.Actor)} has written {ToPrettyString(uid)}'s name in Kill Tome.");
        }

        // If we have written at least one eligible name, we show the popup (So the player knows death note worked).
        if (showPopup)
            _popupSystem.PopupEntity(Loc.GetString("killtome-kill-success"), ent.Owner, args.Actor, PopupType.Large);
    }

    // A person to be killed by KillTome must:
    // 1. be with the name
    // 2. have HumanoidAppearanceComponent (so it targets only humanoids, obv)
    // 3. not be already dead
    // 4. not be already killed by Kill Tome

    // If all these conditions are met, we return true and the entityUid of the person to kill.
    private bool CheckIfEligible(string name, KillTomeComponent comp, [NotNullWhen(true)] out EntityUid? entityUid)
    {
        if (!TryFindEntityByName(name, out var uid) ||
            !TryComp<MobStateComponent>(uid, out var mob))
        {
            entityUid = null;
            return false;
        }

        if (uid is not { } realUid)
        {
            entityUid = null;
            return false;
        }

        if (comp.KilledEntities.Contains(realUid))
        {
            entityUid = null;
            return false;
        }

        if (mob.CurrentState == MobState.Dead)
        {
            entityUid = null;
            return false;
        }

        entityUid = uid;
        return true;
    }

    private bool TryFindEntityByName(string name, [NotNullWhen(true)] out EntityUid? entityUid)
    {
        var query = EntityQueryEnumerator<HumanoidAppearanceComponent>();

        while (query.MoveNext(out var uid, out _))
        {
            if (!_nameModifierSystem.GetBaseName(uid).Equals(name, StringComparison.OrdinalIgnoreCase))
                continue;

            entityUid = uid;
            return true;
        }

        entityUid = null;
        return false;
    }

    private void Kill(EntityUid uid, KillTomeTargetComponent comp)
    {
        _damageSystem.TryChangeDamage(uid, comp.Damage, true);
    }
}
