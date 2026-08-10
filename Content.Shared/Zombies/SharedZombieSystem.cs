using Content.Shared.Armor;
using Content.Shared.Body;
using Content.Shared.Body.Systems;
using Content.Shared.Inventory;
using Content.Shared.Movement.Systems;
using Content.Shared.NameModifier.EntitySystems;

namespace Content.Shared.Zombies;

public abstract partial class SharedZombieSystem : EntitySystem
{
    [Dependency] protected BloodstreamSystem Bloodstream = default!;
    [Dependency] protected SharedVisualBodySystem VisualBody = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ZombieComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshSpeed);
        SubscribeLocalEvent<ZombieComponent, RefreshNameModifiersEvent>(OnRefreshNameModifiers);
        SubscribeLocalEvent<ZombificationResistanceComponent, ArmorExamineEvent>(OnArmorExamine);
        SubscribeLocalEvent<ZombificationResistanceComponent, InventoryRelayedEvent<ZombificationResistanceQueryEvent>>(OnResistanceQuery);
    }

    private void OnResistanceQuery(Entity<ZombificationResistanceComponent> ent, ref InventoryRelayedEvent<ZombificationResistanceQueryEvent> query)
    {
        query.Args.TotalCoefficient *= ent.Comp.ZombificationResistanceCoefficient;
    }

    private void OnArmorExamine(Entity<ZombificationResistanceComponent> ent, ref ArmorExamineEvent args)
    {
        var value = MathF.Round((1f - ent.Comp.ZombificationResistanceCoefficient) * 100, 1);

        if (value == 0)
            return;

        args.Msg.PushNewline();
        args.Msg.AddMarkupOrThrow(Loc.GetString(ent.Comp.Examine, ("value", value)));
    }

    private void OnRefreshSpeed(EntityUid uid, ZombieComponent component, RefreshMovementSpeedModifiersEvent args)
    {
        var mod = component.ZombieMovementSpeedDebuff;
        args.ModifySpeed(mod, mod);
    }

    private void OnRefreshNameModifiers(Entity<ZombieComponent> entity, ref RefreshNameModifiersEvent args)
    {
        args.AddModifier("zombie-name-prefix");
    }


    /// <summary>
    /// This is the function to call if you want to unzombify an entity.
    /// </summary>
    /// <param name="source">the entity having the ZombieComponent</param>
    /// <param name="target">the entity you want to unzombify (different from source in case of cloning, for example)</param>
    /// <param name="zombiecomp"></param>
    /// <remarks>
    /// this currently only restore the skin/eye color from before zombified
    /// TODO: completely rethink how zombies are done to allow reversal.
    /// </remarks>
    public bool UnZombify(EntityUid source, EntityUid target, ZombieComponent? zombiecomp)
    {
        if (!Resolve(source, ref zombiecomp))
            return false;

        VisualBody.ApplyProfiles(target, zombiecomp.BeforeZombifiedProfiles);
        VisualBody.ApplyMarkings(target, zombiecomp.BeforeZombifiedMarkings);

        Bloodstream.ChangeBloodReagents(target, zombiecomp.BeforeZombifiedBloodReagents);

        return true;
    }
}
