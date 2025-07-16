#nullable enable
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Content.Shared.Audio;
using Robust.Client.ResourceManagement;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.IntegrationTests.Tests.Audio;

public sealed class StereoTest
{
    /// <summary>
    /// Scans all registered component types for <see cref="SoundSpecifier"/> fields, then inspects
    /// every <see cref="EntityPrototype"/> to make sure that the files assigned to those fields
    /// are in mono format. Fields marked with <see cref="AllowStereoAttribute"/> are exempt from this check.
    /// </summary>
    [Test]
    public async Task CheckComponentSoundSpecifiers()
    {
        await using var pair = await PoolManager.GetServerClient();
        var client = pair.Client;
        var protoMan = client.ProtoMan;
        var compFactory = client.EntMan.ComponentFactory;
        var resCache = client.Resolve<IResourceCache>();

        await client.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                Dictionary<string, List<FieldInfo>> soundSpecifierFields = [];
                foreach (var componentType in compFactory.AllRegisteredTypes)
                {
                    var componentName = compFactory.GetComponentName(componentType);
                    foreach (var field in componentType.GetFields())
                    {
                        // If the field has the [AllowStereo] attribute, we can ignore it
                        if (field.HasCustomAttribute<AllowStereoAttribute>())
                            continue;

                        // Special handling for generic types
                        if (field.FieldType.IsGenericType)
                        {
                            foreach (var typeArg in field.FieldType.GenericTypeArguments)
                            {
                                if (typeArg.IsAssignableTo(typeof(SoundSpecifier)))
                                    soundSpecifierFields.GetOrNew(componentName).Add(field);
                            }
                            continue;
                        }

                        if (field.FieldType.IsAssignableTo(typeof(SoundSpecifier)))
                            soundSpecifierFields.GetOrNew(componentName).Add(field);
                    }
                }

                // Unlikely, but if no components have SoundSpecifier fields, we're done.
                if (soundSpecifierFields.Count == 0)
                    return;

                foreach (var proto in protoMan.EnumeratePrototypes<EntityPrototype>())
                {
                    foreach (var (componentName, fields) in soundSpecifierFields)
                    {
                        foreach (var (compName, compRegistryEntry) in proto.Components)
                        {
                            // Find the flagged component
                            if (compName != componentName)
                                continue;

                            // Check the fields we flagged
                            foreach (var field in fields)
                            {
                                var fieldValue = field.GetValue(compRegistryEntry.Component);
                                var datafieldName = $"{compName}.{field.Name}";

                                CheckValue(fieldValue, datafieldName, resCache, protoMan);
                            }
                        }
                    }
                }
            });
        });

        await pair.CleanReturnAsync();
    }

    /// <summary>
    /// Scans every registered prototype kind for <see cref="SoundSpecifier"/> fields, then inspects
    /// every instance of those prototype kinds to make sure that the files assigned to those fields
    /// are in mono format. Fields marked with <see cref="AllowStereoAttribute"/> are exempt from this check.
    /// </summary>
    [Test]
    public async Task CheckPrototypeSoundSpecifiers()
    {
        await using var pair = await PoolManager.GetServerClient();
        var client = pair.Client;
        var protoMan = client.ProtoMan;
        var resCache = client.Resolve<IResourceCache>();

        await client.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                foreach (var type in protoMan.EnumeratePrototypeKinds())
                {
                    // Identify any fields in this prototype kind that contain SoundSpecifiers
                    List<FieldInfo> soundSpecifierFields = [];
                    foreach (var field in type.GetFields())
                    {
                        // If the field has the [AllowStereo] attribute, we can ignore it
                        if (field.HasCustomAttribute<AllowStereoAttribute>())
                            continue;

                        // Special handling for generic types
                        if (field.FieldType.IsGenericType)
                        {
                            foreach (var typeArg in field.FieldType.GenericTypeArguments)
                            {
                                if (typeArg.IsAssignableTo(typeof(SoundSpecifier)))
                                    soundSpecifierFields.Add(field);
                            }
                            continue;
                        }

                        if (field.FieldType.IsAssignableTo(typeof(SoundSpecifier)))
                            soundSpecifierFields.Add(field);
                    }

                    // No fields contain SoundSpecifiers, so we're done with this prototype kind
                    if (soundSpecifierFields.Count == 0)
                        continue;

                    // Inspect all instances of this prototype kind
                    foreach (var proto in protoMan.EnumeratePrototypes(type))
                    {
                        // Check the fields we flagged
                        foreach (var field in soundSpecifierFields)
                        {
                            var datafieldName = $"{type.Name}.{field.Name}";
                            var fieldValue = field.GetValue(proto);

                            CheckValue(fieldValue, datafieldName, resCache, protoMan);
                        }
                    }
                }
            });
        });

        await pair.CleanReturnAsync();
    }

    private static void CheckValue(object? value, string datafieldName, IResourceCache resCache, IPrototypeManager protoMan)
    {
        // Special handling for collection types
        if (value is IDictionary dict)
        {
            foreach (var v in dict.Values)
            {
                CheckValue(v, datafieldName, resCache, protoMan);
            }
            return;
        }
        if (value is IList list)
        {
            foreach (var v in list)
            {
                CheckValue(v, datafieldName, resCache, protoMan);
            }
            return;
        }

        // Make sure the test isn't missing any generic types
        Assert.That(value?.GetType().IsGenericType, Is.Not.True,
            $"Unhandled generic type containing SoundSpecifier: {value?.GetType()}");

        // Single, non-collection value
        CheckSpecifier(value, datafieldName, resCache, protoMan);
    }

    private static void CheckSpecifier(object specifier, string datafieldName, IResourceCache resCache, IPrototypeManager protoMan)
    {
        if (specifier is SoundPathSpecifier pathSpecifier)
        {
            ValidateFromPath(pathSpecifier.Path, datafieldName, resCache);
        }
        else if (specifier is SoundCollectionSpecifier collectionSpecifier)
        {
            if (collectionSpecifier.Collection is not { } collection)
                return;

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
            $"{path} has multiple channels, but {datafieldName} only allows mono audio.");
    }
}
