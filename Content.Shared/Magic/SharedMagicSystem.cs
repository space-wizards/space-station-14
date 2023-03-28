using Content.Shared.Magic.Events;
using Robust.Shared.Serialization.Manager;
using JetBrains.Annotations;

namespace Content.Shared.Magic;

/// <summary>
/// Handles using spells
/// </summary>
[UsedImplicitly]
public abstract class SharedMagicSystem : EntitySystem
{
    [Dependency] private readonly IComponentFactory _componentFactory = default!;
    [Dependency] private readonly ISerializationManager _serializationManager = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TeleportSpellEvent>(OnTeleportSpell);
        SubscribeLocalEvent<ChangeComponentsSpellEvent>(OnChangeComponentsSpell);
    }

    /// <summary>
    /// Teleports the user to the clicked location
    /// </summary>
    private void OnTeleportSpell(TeleportSpellEvent args)
    {
        if (args.Handled)
            return;

        var transform = Transform(args.Performer);

        if (transform.MapID != args.Target.GetMapId(EntityManager))
            return;

        _transformSystem.SetCoordinates(args.Performer, args.Target);
        _transformSystem.AttachToGridOrMap(args.Performer, transform);
        args.Handled = true;
    }

    /// <summary>
    /// Add or remove components to the target entity
    /// </summary>
    private void OnChangeComponentsSpell(ChangeComponentsSpellEvent args)
    {
        foreach (var toRemove in args.ToRemove)
        {
            if (_componentFactory.TryGetRegistration(toRemove, out var registration))
                RemComp(args.Target, registration.Type);
        }

        foreach (var (name, data) in args.ToAdd)
        {
            if (HasComp(args.Target, data.Component.GetType()))
                continue;

            var component = (Component) _componentFactory.GetComponent(name);
            component.Owner = args.Target;
            var temp = (object) component;
            _serializationManager.CopyTo(data.Component, ref temp);
            EntityManager.AddComponent(args.Target, (Component) temp!);
        }
    }
}
