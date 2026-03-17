#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Reflection;

namespace Content.IntegrationTests.Utility;

public static partial class GameDataScrounger
{
    /// <inheritdoc cref="M:Robust.Shared.Reflection.ReflectionManager.GetAllChildren``1(System.Boolean)"/>
    public static IEnumerable<Type> GetAllChildren<T>(bool inclusive = false)
    {
        return ReflectionManager.GetAllChildren<T>(inclusive);
    }

    /// <inheritdoc cref="M:Robust.Shared.Reflection.ReflectionManager.GetAllChildren(System.Type,System.Boolean)"/>
    public static IEnumerable<Type> GetAllChildren(Type baseType, bool inclusive = false)
    {
        return ReflectionManager.GetAllChildren(baseType, inclusive);
    }

    /// <inheritdoc cref="M:Robust.Shared.Reflection.ReflectionManager.GetType(System.String)"/>
    public static Type? GetType(string name)
    {
        return ReflectionManager.GetType(name);
    }

    /// <inheritdoc cref="M:Robust.Shared.Reflection.ReflectionManager.LooseGetType(System.String)"/>
    public static Type LooseGetType(string name)
    {
        return ReflectionManager.LooseGetType(name);
    }

    /// <inheritdoc cref="M:Content.IntegrationTests.Utility.GameDataScrounger.TryLooseGetType(System.String,System.Type@)"/>
    public static bool TryLooseGetType(string name, out Type? type)
    {
        return ReflectionManager.TryLooseGetType(name, out type);
    }

    /// <inheritdoc cref="M:Robust.Shared.Reflection.ReflectionManager.FindTypesWithAttribute``1"/>
    public static IEnumerable<Type> FindTypesWithAttribute<T>() where T : Attribute
    {
        return ReflectionManager.FindTypesWithAttribute<T>();
    }

    /// <inheritdoc cref="M:Robust.Shared.Reflection.ReflectionManager.FindTypesWithAttribute(System.Type)"/>
    public static IEnumerable<Type> FindTypesWithAttribute(Type attributeType)
    {
        return ReflectionManager.FindTypesWithAttribute(attributeType);
    }

    /// <inheritdoc cref="M:Robust.Shared.Reflection.ReflectionManager.FindAllTypes"/>
    public static IEnumerable<Type> FindAllTypes()
    {
        return ReflectionManager.FindAllTypes();
    }

    /// <inheritdoc cref="M:Robust.Shared.Reflection.ReflectionManager.GetEnumReference(System.Enum)"/>
    public static string GetEnumReference(Enum @enum)
    {
        return ReflectionManager.GetEnumReference(@enum);
    }

    /// <inheritdoc cref="M:Robust.Shared.Reflection.ReflectionManager.TryParseEnumReference(System.String,System.Enum@,System.Boolean)"/>
    public static bool TryParseEnumReference(string reference, out Enum? @enum, bool shouldThrow = true)
    {
        return ReflectionManager.TryParseEnumReference(reference, out @enum, shouldThrow);
    }

    /// <inheritdoc cref="M:Robust.Shared.Reflection.ReflectionManager.YamlTypeTagLookup(System.Type,System.String)"/>
    public static Type? YamlTypeTagLookup(Type baseType, string typeName)
    {
        return ReflectionManager.YamlTypeTagLookup(baseType, typeName);
    }

    private sealed class TestReflectionManager : ReflectionManager
    {
        // We don't support shorthands for the client or server as they conflict often.
        // But you can do, say, `Shared.MyStuff.MyComponent` and it'll work.
        protected override IEnumerable<string> TypePrefixes =>
        [
            "",
            "Robust.Shared.",
            "Robust.",
            "Content.Shared.",
            "Content.IntegrationTests.",
            "Content."
        ];
    }

    private static TestReflectionManager ConstructTestReflectionManager()
    {
        TestReflectionManager? result = null;

        // Jank pending engine change. I need to make IDependencyCollection have a static constructor.
        var t = new Thread(() =>
        {
            IoCManager.InitThread(); // Create a dep collection

            IoCManager.Register<ILogManager, LogManager>();
            IoCManager.Register<TestReflectionManager>();

            IoCManager.BuildGraph();

            result = IoCManager.Resolve<TestReflectionManager>();
            result.Initialize();
            result.LoadAssemblies(AppDomain.CurrentDomain.GetAssemblies().Where(x => x.FullName?.StartsWith("Robust.") ?? false));
            result.LoadAssemblies(AppDomain.CurrentDomain.GetAssemblies().Where(x => x.FullName?.StartsWith("Content.") ?? false));
        });

        t.Start();
        t.Join();

        return result!;
    }


}
