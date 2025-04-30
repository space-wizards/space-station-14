// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.Damage;
using Content.Shared.IdentityManagement.Components;
using Content.Shared.Mobs.Systems;

namespace Content.Server.DeadSpace.MutilatedCorpse;

/// <summary>
/// This handles changes the character's name to unknown if there is a lot of damage
/// </summary>
public sealed class MutilatedCorpseSystem : EntitySystem
{
    [Dependency] private readonly MetaDataSystem _meta = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<MutilatedCorpseComponent, MapInitEvent>(OnStartUp);
        SubscribeLocalEvent<MutilatedCorpseComponent, DamageChangedEvent>(OnChangeHealth);
    }

    private void OnStartUp(Entity<MutilatedCorpseComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.RealName = EntityManager.GetComponent<MetaDataComponent>(ent.Owner).EntityName;
        ent.Comp.ChangedName = Loc.GetString(ent.Comp.LocIdChangedName);
    }

    private void OnChangeHealth(Entity<MutilatedCorpseComponent> ent, ref DamageChangedEvent args)
    {
        if (!TryComp<DamageableComponent>(ent.Owner, out var damageComp))
            return;

        var damageDict = damageComp.Damage.DamageDict;

        if(!TryComp<IdentityComponent>(ent.Owner, out var identityComp))
            return;

        if (identityComp.IdentityEntitySlot.ContainedEntity is not { } ident)
            return;

        if (damageDict[ent.Comp.DamageType] >= ent.Comp.AmountDamageForMutilated && _mobState.IsDead(ent.Owner))
        {
            _meta.SetEntityName(ent.Owner, ent.Comp.ChangedName, raiseEvents: false);
            _meta.SetEntityName(ident, ent.Comp.ChangedName, raiseEvents: false); //for examination
        }
        else
        {
            _meta.SetEntityName(ent.Owner, ent.Comp.RealName, raiseEvents: false);
            _meta.SetEntityName(ident, ent.Comp.RealName, raiseEvents: false); //for examination
        }
    }
}
