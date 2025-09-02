using System;
using System.Drawing;
using System.Linq;
using System.Reflection;
using Content.Shared.Humanoid.ColoringScheme;
using NUnit.Framework;

namespace Content.Tests.Shared.Preferences.Humanoid;

[TestFixture]
public sealed class SkinTonesTest
{
    [Test]
    public void TestSkinToneValidity()
    {
        var baseType = typeof(ColoringSchemeRule);
        var derivedTypes = Assembly.GetAssembly(baseType)
            ?.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && baseType.IsAssignableFrom(t));

        if (derivedTypes == null)
            return;

        foreach (var rule in derivedTypes)
        {
            var instance = (ColoringSchemeRule)Activator.CreateInstance(rule)!;

            for (var r = 0; r <= 255; r += 5)
            {
                for (var g = 0; g <= 255; g += 5)
                {
                    for (var b = 0; b <= 255; b += 5)
                    {
                        var color = Color.FromArgb(r, g, b);
                        var clamped = instance.Clamp(color);
                        Assert.That(instance.Verify(clamped), $"Failed for color {color}, for rule {rule.Name}");
                    }
                }
            }
        }
    }
}
