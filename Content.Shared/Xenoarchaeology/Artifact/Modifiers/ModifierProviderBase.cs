using JetBrains.Annotations;
using Robust.Shared.Serialization;

namespace Content.Shared.Xenoarchaeology.Artifact.Modifiers;

[ImplicitDataDefinitionForInheritors, UsedImplicitly(ImplicitUseTargetFlags.WithInheritors), Serializable, NetSerializable]
public abstract partial class ModifierProviderBase
{
    public abstract float Modify(float originalValue);
}
