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

        DamageSpecifier input1 = new() { DamageDict = _input1 };
        DamageSpecifier input2 = new() { DamageDict = _input2 };
        DamageSpecifier output1 = new() { DamageDict = _output1 };
        DamageSpecifier output2 = new() { DamageDict = _output2 };
        DamageSpecifier output3 = new() { DamageDict = _output3 };
        DamageSpecifier output4 = new() { DamageDict = _output4 };
        DamageSpecifier output5 = new() { DamageDict = _output5 };

        Assert.Multiple(() =>
        {
            Assert.That((-input1).Equals(output1));
            Assert.That((input1 / 2).Equals(output2));
            Assert.That((input1 * 2).Equals(output3));
        });

        var difference = (input1 - input2);
        Assert.That(difference.Equals(output4));

        var difference2 = (-input2) + input1;
        Assert.That(difference.Equals(difference2));

        difference.Clamp(-0.25f, 0.25f);
        Assert.That(difference.Equals(output5));
    }

    static Dictionary<string, FixedPoint2> _input1 = new()
    {
        { "A", 1.5f },
        { "B", 2 },
        { "C", 3 }
    };

    static Dictionary<string, FixedPoint2> _input2 = new()
    {
        { "A", 1 },
        { "B", 2 },
        { "C", 5 },
        { "D", 0.05f }
    };

    static Dictionary<string, FixedPoint2> _output1 = new()
    {
        { "A", -1.5f },
        { "B", -2 },
        { "C", -3 }
    };

    static Dictionary<string, FixedPoint2> _output2 = new()
    {
        { "A", 0.75f },
        { "B", 1 },
        { "C", 1.5 }
    };

    static Dictionary<string, FixedPoint2> _output3 = new()
    {
        { "A", 3f },
        { "B", 4 },
        { "C", 6 }
    };

    static Dictionary<string, FixedPoint2> _output4 = new()
    {
        { "A", 0.5f },
        { "B", 0 },
        { "C", -2 },
        { "D", -0.05f }
    };

    static Dictionary<string, FixedPoint2> _output5 = new()
    {
        { "A", 0.25f },
        { "B", 0 },
        { "C", -0.25f },
        { "D", -0.05f }
    };
}
