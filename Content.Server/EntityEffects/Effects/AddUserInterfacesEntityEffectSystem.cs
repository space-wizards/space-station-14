using Content.Shared.EntityEffects;
using Robust.Server.GameObjects;

namespace Content.Server.EntityEffects.Effects;

public sealed partial class AddUserInterfacesEntityEffectSystem : EntityEffectSystem<MetaDataComponent, AddUserInterfaces>
{
    [Dependency] private UserInterfaceSystem _ui = default!;

    protected override void Effect(Entity<MetaDataComponent> entity, ref EntityEffectEvent<AddUserInterfaces> args)
    {
        var userInterface = EnsureComp<UserInterfaceComponent>(entity);

        foreach (var (key, data) in args.Effect.Interfaces)
        {
            _ui.SetUi((entity.Owner, userInterface), key, data);
        }
    }
}

public sealed partial class AddUserInterfaces : EntityEffectBase<AddUserInterfaces>
{
    [DataField(required: true)]
    public Dictionary<Enum, InterfaceData> Interfaces = new();
}
