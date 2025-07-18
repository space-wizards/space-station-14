#nullable enable
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Content.Shared.Audio;
using Robust.Client.ResourceManagement;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Reflection;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;

namespace Content.IntegrationTests.Tests.Audio;

public sealed class StereoTest
{
    /// <summary>
    /// Scans all registered component and prototype types for <see cref="SoundSpecifier"/> fields, then inspects
    /// every <see cref="EntityPrototype"/> and <see cref="IPrototype"/> instance to make sure that the files assigned to those fields
    /// are in mono format. Fields marked with <see cref="AllowStereoAttribute"/> are exempt from this check.
    /// </summary>
    [Test]
    public async Task TestSoundSpecifiers()
    {
        await using var pair = await PoolManager.GetServerClient();

        var server = pair.Server;
        var protoMan = server.ProtoMan;
        var reflectionMan = server.Resolve<IReflectionManager>();
        var compFactory = server.EntMan.ComponentFactory;

        var client = pair.Client;
        var resCache = client.Resolve<IResourceCache>();

        await client.WaitAssertion(() =>
        {
            // Find all data definition types that may contain SoundSpecifiers.
            // We get some false positives, but it's not a big deal.
            var dataDefinitionTypes = reflectionMan.FindTypesWithAttribute<DataDefinitionAttribute>();
            var dataDefinitions = GetRelevantFields(dataDefinitionTypes);

            // Scan all prototype types for SoundSpecifier and DataDefinition fields
            var prototypeFields = GetRelevantFields(protoMan.EnumeratePrototypeKinds());

            // Iterate over the flagged prototype types
            foreach (var (kind, fields) in prototypeFields)
            {
                // Inspect all prototype instances of the type
                foreach (var proto in protoMan.EnumeratePrototypes(kind))
                {
                    // Check each flagged field
                    foreach (var field in fields)
                    {
                        // Skip if null
                        if (field.GetValue(proto) is not { } fieldValue)
                            continue;

                        CheckValue(fieldValue, kind.Name, dataDefinitions, resCache, protoMan);
                    }
                }
            }

            // Scan all component types for SoundSpecifiers and DataDefinition fields
            var componentFields = GetRelevantFields(compFactory.AllRegisteredTypes);
            // Inspect all EntityPrototypes
            foreach (var proto in protoMan.EnumeratePrototypes<EntityPrototype>())
            {
                // Iterate over the flagged Component types
                foreach (var (comp, fields) in componentFields)
                {
                    // Get the registered name of the component type
                    var compName = compFactory.GetComponentName(comp);
                    // Get the component data from the prototype, if it has it
                    if (!proto.Components.TryGetComponent(compName, out var component))
                        continue;

                    // Iterate over the flagged fields
                    foreach (var field in fields)
                    {
                        // Skip if null
                        if (field.GetValue(component) is not { } fieldValue)
                            continue;

                        CheckValue(fieldValue, comp.Name, dataDefinitions, resCache, protoMan);
                    }
                }
            }
        });

        await pair.CleanReturnAsync();
    }

    private static Dictionary<Type, List<FieldInfo>> GetRelevantFields(IEnumerable<Type> types)
    {
        Dictionary<Type, List<FieldInfo>> dict = [];
        foreach (var type in types)
        {
            // Inspect all fields
            foreach (var field in type.GetFields())
            {
                // Ignore the field if it has AllowStereo
                if (field.HasCustomAttribute<AllowStereoAttribute>())
                    continue;

                // No infinite recursion
                if (field.FieldType == type)
                    continue;

                // Flag the field if it might be relevant
                if (IsRelevantType(field.FieldType))
                    dict.GetOrNew(type).Add(field);
            }
        }

        return dict;
    }

    private static void CheckValue(object value, string path, Dictionary<Type, List<FieldInfo>> dataDefs, IResourceCache resCache, IPrototypeManager protoMan)
    {
        if (value is SoundSpecifier soundSpecifier)
        {
            // Huzzah, we found a SoundSpecifier! Let's make sure it's mono
            CheckSpecifier(soundSpecifier, path, resCache, protoMan);
        }
        else if (value is IList list)
        {
            // Recursively check each element of the list
            foreach (var element in list)
            {
                CheckValue(element, path, dataDefs, resCache, protoMan);
            }
        }
        else if (value is IDictionary dictionary)
        {
            // Recursively check each value in the dictionary
            foreach (var v in dictionary.Values)
            {
                CheckValue(v, $"{path}.Value", dataDefs, resCache, protoMan);
            }
        }
        else if (dataDefs.TryGetValue(value.GetType(), out var dataDefFields))
        {
            // The field contains a potentially-relevant DataDefinition type
            // Recursively check any flagged fields for this type
            foreach (var field in dataDefFields)
            {
                // Ignore null
                if (field.GetValue(value) is not { } fieldValue)
                    continue;

                CheckValue(fieldValue, $"{path}.{field.Name}", dataDefs, resCache, protoMan);
            }
        }
    }

    private static void CheckSpecifier(SoundSpecifier specifier, string datafieldName, IResourceCache resCache, IPrototypeManager protoMan)
    {
        if (specifier is SoundPathSpecifier pathSpecifier)
        {
            // Validate single file
            ValidateFromPath(pathSpecifier.Path, datafieldName, resCache);
        }
        else if (specifier is SoundCollectionSpecifier collectionSpecifier)
        {
            if (collectionSpecifier.Collection is not { } collection)
                return;

            // Validate all possible files
            var collectionPrototype = protoMan.Index<SoundCollectionPrototype>(collection);
            foreach (var path in collectionPrototype.PickFiles)
            {
                ValidateFromPath(path, datafieldName, resCache);
            }
        }
    }

    private static void ValidateFromPath(ResPath path, string datafieldName, IResourceCache resCache)
    {
        var audio = resCache.GetResource<AudioResource>(path);
        Assert.That(audio.AudioStream.ChannelCount, Is.EqualTo(1),
            $"{path} has multiple channels, but {datafieldName} only allows mono audio. Stereo audio cannot be played positionally and should be converted to mono. If {datafieldName} is only played globally (without positional data, like music), add [AllowStereo] to the SoundSpecifer to remove this error.");
    }

    private static bool IsRelevantType(Type type)
    {
        // Primitive types obviously are not and do not contain SoundSpecifiers
        if (type.IsPrimitive)
            return false;

        // SoundSpecifiers are obviously relevant - that's why we're here!
        if (type.IsAssignableTo(typeof(SoundSpecifier)))
            return true;

        if (type.IsGenericType)
        {
            if (type.IsAssignableTo(typeof(IList)))
            {
                // Recursively check the list element type
                return IsRelevantType(type.GenericTypeArguments[0]);
            }

            if (type.IsAssignableTo(typeof(IDictionary)))
            {
                // Recursively check the dictionary value type
                // Don't check the key type because it's probably not needed?
                return IsRelevantType(type.GenericTypeArguments[1]);
            }
        }

        // Assume any DataDefinition field is relevant
        // There will be false positives, but we'll just skip them when checking values
        if (type.HasCustomAttribute<DataDefinitionAttribute>())
            return true;

        // None of the above, so we don't care
        return false;
    }
}
