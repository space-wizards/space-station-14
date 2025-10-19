// SPDX-FileCopyrightText: Space Station 14 Contributors <https://spacestation14.com/about/about/>
//
// SPDX-License-Identifier: MIT

using JetBrains.Annotations;

namespace Content.Server.Administration.Logs.Converters;

[AttributeUsage(AttributeTargets.Class)]
[BaseTypeRequired(typeof(AdminLogConverter<>))]
[MeansImplicitUse]
public sealed class AdminLogConverterAttribute : Attribute
{
}
