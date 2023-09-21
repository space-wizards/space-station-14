using Content.Shared.Coordinates;
using Content.Shared.Humanoid;
using Content.Shared.Interaction.Events;

namespace Content.Shared.Whistle;

public abstract class SharedWhistleSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    public override void Initialize()
    {
        base.Initialize();
    }

    private bool ExclamateTarget(EntityUid target, WhistleComponent component)
    {
        SpawnAttachedTo(component.effect, target.ToCoordinates());

        return true;
    }

    public void OnUseInHand(EntityUid uid, WhistleComponent component, UseInHandEvent args)
    {
        TryMakeLoudWhistle(uid, args.User, component);

        args.Handled = true;
    }

    public bool TryMakeLoudWhistle(EntityUid uid, EntityUid owner, WhistleComponent component)
    {
        if (component.Distance <= 0)
            return false;

        MakeLoudWhistle(uid, owner, component);
        return true;
    }

    private bool MakeLoudWhistle(EntityUid uid, EntityUid owner, WhistleComponent component)
    {
        foreach (var iterator in
            _entityLookup.GetComponentsInRange<HumanoidAppearanceComponent>(Transform(uid).Coordinates, component.Distance))
        {
            if (iterator.Owner == owner)
                continue;

            ExclamateTarget(iterator.Owner, component);
        }

        return true;
    }
}
