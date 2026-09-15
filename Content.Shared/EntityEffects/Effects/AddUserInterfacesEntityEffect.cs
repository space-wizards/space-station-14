namespace Content.Shared.EntityEffects.Effects;

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class AddUserInterfaces : EntityEffectBase<AddUserInterfaces>
{
    [DataField(required: true)]
    public Dictionary<Enum, InterfaceData> Interfaces = new();
}
