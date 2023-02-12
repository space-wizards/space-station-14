using JetBrains.Annotations;

namespace Content.Server.Administration.Logs.Converters;

[AttributeUsage(AttributeTargets.Class)]
[BaseTypeRequired(typeof(AdminLogConverter<>))]
[MeansImplicitUse]
public sealed class AdminLogConverterAttribute : Attribute
{
}
