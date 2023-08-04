using System.Collections.Generic;
using System.Reflection;
using Robust.Shared.Reflection;
using Robust.Shared.Utility;

namespace Content.IntegrationTests;

// Partial class for handling the discovering and validating of test prototypes.
public static partial class PoolManager
{
    private static List<string>? _testPrototypes;

    public static void DiscoverTestPrototypes()
    {
        if (_testPrototypes != null)
            throw new Exception("Prototypes already discovered.");

        _testPrototypes = new();
        var fullName = new HashSet<string>();

        foreach (var type in typeof(PoolManager).Assembly.GetTypes())
        {
            var attribute = (ReflectAttribute?)Attribute.GetCustomAttribute(type, typeof(ReflectAttribute));
            if (!(attribute?.Discoverable ?? ReflectAttribute.DEFAULT_DISCOVERABLE))
                continue;

            const BindingFlags flags = BindingFlags.Static
                                       | BindingFlags.NonPublic
                                       | BindingFlags.Public
                                       | BindingFlags.DeclaredOnly;

            foreach (var field in type.GetFields(flags))
            {
                if (!field.HasCustomAttribute<TestPrototypesAttribute>())
                    continue;

                var val = field.GetValue(null);
                if (val is not string str)
                    throw new Exception($"TestPrototypeAttribute is only valid on non-null string fields");

                _testPrototypes.Add(str);
            }
        }
    }
}
