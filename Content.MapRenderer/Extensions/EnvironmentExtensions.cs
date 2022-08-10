#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;

namespace Content.MapRenderer.Extensions
{
    public static class EnvironmentExtensions
    {
        public static bool TryGetVariable(string key, [NotNullWhen(true)] out string? value)
        {
            return (value = Environment.GetEnvironmentVariable(key)) != null;
        }

        public static string GetVariableOrThrow(string key)
        {
            return Environment.GetEnvironmentVariable(key) ??
                   throw new ArgumentException($"No environment variable found with key {key}");
        }
    }
}
