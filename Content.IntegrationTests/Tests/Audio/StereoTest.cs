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
    [Test]
    public async Task CheckSoundSpecifiers()
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
            var collectionPrototype = protoMan.Index<SoundCollectionPrototype>(collectionSpecifier.Collection);
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
