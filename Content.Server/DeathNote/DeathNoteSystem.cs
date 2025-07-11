using Content.Server.Popups;
using Content.Shared.Damage;
using Content.Shared.Humanoid;
using Content.Shared.Interaction;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Paper;
using Content.Shared.Popups;
using Robust.Shared.Timing;

namespace Content.Server.DeathNote;

/// <summary>
/// This handles Death Note functionality.
/// </summary>
public sealed class DeathNoteSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly DamageableSystem _damageSystem = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<DeathNoteComponent, PaperAfterWriteEvent>(OnPaperAfterWriteInteract);
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

    private void OnPaperAfterWriteInteract(Entity<DeathNoteComponent> ent, ref PaperAfterWriteEvent args)
    {
        // if the entity is not a paper, we don't do anything
        if (!TryComp<PaperComponent>(ent.Owner, out var paper))
            return;

        var content = paper.Content;

        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            if (!line.StartsWith("Name: ", StringComparison.OrdinalIgnoreCase))
                continue;

            var name = line.Substring("Name: ".Length).Trim();

            if (!TryFindEntityByName(name, out var uid) ||
                !TryComp<MobStateComponent>(uid, out var mob) ||
                mob.CurrentState != MobState.Dead)
            {
                _popupSystem.PopupClient("Name is invalid", ent.Owner, PopupType.Medium);
            }

            var targetComp = new DeathNoteTargetComponent(40);

            AddComp(uid, targetComp);

            _popupSystem.PopupClient("You have written a name in the Death Note.", ent.Owner, PopupType.Medium);
        }
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
    }
}
