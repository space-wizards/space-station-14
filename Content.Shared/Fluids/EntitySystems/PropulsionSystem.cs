using System.Linq;
using Content.Shared.Fluids.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Whitelist;
using Robust.Shared.Physics.Events;

namespace Content.Shared.Fluids.EntitySystems;

public sealed class PropulsionSystem : EntitySystem
{
    [Dependency] private readonly SpeedModifierContactsSystem _speedModifier = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PropulsionComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<PropulsionComponent, StartCollideEvent>(OnStartCollide);
        SubscribeLocalEvent<PropulsionComponent, EndCollideEvent>(OnEndCollide);
        SubscribeLocalEvent<PropulsedByComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshSpeed);
    }

    public void OnComponentInit(Entity<PropulsionComponent> ent, ref ComponentInit args)
    {
        EnsureComp<SpeedModifierContactsComponent>(ent);
    }

    public void OnStartCollide(Entity<PropulsionComponent> ent, ref StartCollideEvent args)
    {
        if (!HasComp<MovementSpeedModifierComponent>(args.OtherEntity))
            return;

        if (_whitelistSystem.IsWhitelistFail(ent.Comp.Whitelist, args.OtherEntity))
            return;

        _speedModifier.AddModifiedEntity(args.OtherEntity);

        var propulse = EnsureComp<PropulsedByComponent>(args.OtherEntity);
        propulse.Sources.Add(ent);
    }

    public void OnEndCollide(Entity<PropulsionComponent> ent, ref EndCollideEvent args)
    {
        if (TryComp<PropulsedByComponent>(args.OtherEntity, out var propulse))
        {
            propulse.Sources.Remove(ent);
        }
    }

    public static void OnRefreshSpeed(Entity<PropulsedByComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        ent.Comp.Sources.RemoveWhere((ent) => !ent.Owner.IsValid() || ent.Comp.Deleted);

        if (ent.Comp.Sources.Count == 0)
            return;

        var modifier = ent.Comp.Sources.First();
        args.ModifySpeed(modifier.Comp.WalkSpeedModifier, modifier.Comp.SprintSpeedModifier);
    }
}
