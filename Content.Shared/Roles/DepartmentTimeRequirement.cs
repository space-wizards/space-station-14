using JetBrains.Annotations;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Roles
{
    [UsedImplicitly]
    public sealed class DepartmentTimeRequirement : JobRequirement
    {
        [DataField("department", customTypeSerializer:typeof(PrototypeIdSerializer<DepartmentPrototype>))]
        public string Department = default!;

        /// <summary>
        /// How long (in seconds) this requirement is.
        /// </summary>
        [DataField("time")]
        public TimeSpan Time;
    }
}
