using Robust.Shared.Interfaces.Reflection;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components
{
    public partial class SharedStackComponentDataClass
    {
        [DataClassTarget("stacktype")]
        public object StackType;

        public void ExposeData(ObjectSerializer serializer)
        {
            if (serializer.Writing)
            {
                return;
            }

            if (serializer.TryReadDataFieldCached("stacktype", out string raw))
            {
                var refl = IoCManager.Resolve<IReflectionManager>();
                if (refl.TryParseEnumReference(raw, out var @enum))
                {
                    StackType = @enum;
                }
                else
                {
                    StackType = raw;
                }
            }
            else
            {
                StackType = null;
            }
        }
    }
}
