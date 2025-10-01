using System;
namespace Content.Shared.Starlight.Abstract.Codegen;

[AttributeUsage(AttributeTargets.Class)]
public sealed class GenerateLocalSubscriptionsAttribute<T> : Attribute
{
}