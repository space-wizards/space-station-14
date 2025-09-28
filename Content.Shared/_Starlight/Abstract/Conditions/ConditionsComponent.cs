using Content.Shared._Starlight.Abstract.Conditions;

namespace Content.Shared._Starlight.Railroading;

[RegisterComponent]
public sealed partial class ConditionsComponent : Component
{
    [NonSerialized]
    [DataField(serverOnly: true)]
    public List<BaseCondition> Conditions = [];
}
