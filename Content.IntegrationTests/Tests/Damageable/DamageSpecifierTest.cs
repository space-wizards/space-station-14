using System.Collections.Generic;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;

namespace Content.IntegrationTests.Tests.Damageable;

[TestFixture]
[TestOf(typeof(DamageSpecifier))]
public sealed class DamageSpecifierTest
{
    [Test]
    public void TestDamageSpecifierOperations()
    {
        // Test basic math operations.
        // I've already nearly broken these once. When editing the operators.

        DamageSpecifier input1 = new() { DamageDict = Input1 };
        DamageSpecifier input2 = new() { DamageDict = Input2 };
        DamageSpecifier output1 = new() { DamageDict = Output1 };
        DamageSpecifier output2 = new() { DamageDict = Output2 };
        DamageSpecifier output3 = new() { DamageDict = Output3 };
        DamageSpecifier output4 = new() { DamageDict = Output4 };
        DamageSpecifier output5 = new() { DamageDict = Output5 };

        Assert.Multiple(() =>
        {
            Assert.That(-input1, Is.EqualTo(output1));
            Assert.That(input1 / 2, Is.EqualTo(output2));
            Assert.That(input1 * 2, Is.EqualTo(output3));
        });

        var difference = input1 - input2;
        Assert.That(difference, Is.EqualTo(output4));

        var difference2 = -input2 + input1;
        Assert.That(difference, Is.EqualTo(difference2));

        difference.Clamp(-0.25f, 0.25f);
        Assert.That(difference, Is.EqualTo(output5));
    }

    private static readonly Dictionary<string, FixedPoint2> Input1 = new()
    {
        { "A", 1.5f },
        { "B", 2 },
        { "C", 3 }
    };

    private static readonly Dictionary<string, FixedPoint2> Input2 = new()
    {
        { "A", 1 },
        { "B", 2 },
        { "C", 5 },
        { "D", 0.05f }
    };

    private static readonly Dictionary<string, FixedPoint2> Output1 = new()
    {
        { "A", -1.5f },
        { "B", -2 },
        { "C", -3 }
    };

    private static readonly Dictionary<string, FixedPoint2> Output2 = new()
    {
        { "A", 0.75f },
        { "B", 1 },
        { "C", 1.5 }
    };

    private static readonly Dictionary<string, FixedPoint2> Output3 = new()
    {
        { "A", 3f },
        { "B", 4 },
        { "C", 6 }
    };

    private static readonly Dictionary<string, FixedPoint2> Output4 = new()
    {
        { "A", 0.5f },
        { "B", 0 },
        { "C", -2 },
        { "D", -0.05f }
    };

    private static readonly Dictionary<string, FixedPoint2> Output5 = new()
    {
        { "A", 0.25f },
        { "B", 0 },
        { "C", -0.25f },
        { "D", -0.05f }
    };
}
