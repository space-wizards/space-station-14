using Robust.Shared.IoC;
using Robust.Shared.Reflection;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.GameObjects.Components
{
    public partial class SharedStackComponentDataClass : ISerializationHooks
    {
        [DataField("stacktype")] public string StackTypeId;

        [DataClassTarget("stacktype")] public object StackType;

        public void AfterDeserialization()
        {
            var refl = IoCManager.Resolve<IReflectionManager>();

            if (refl.TryParseEnumReference(StackTypeId, out var @enum))
            {
                StackType = @enum;
            }
            else
            {
                StackType = StackTypeId;
            }
        }
    }
}
