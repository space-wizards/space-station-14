using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Shared._Offbrand.Wounds;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;

namespace Content.IntegrationTests.Tests._Offbrand;

[TestFixture]
public sealed class WoundableTests : GameTest
{
    private const string DamageTypePrototype = "WoundableTestsDamageType";
    private const string DamageContainerPrototype = "WoundableTestsDamageContainer";
    private const string BodyPrototype = "WoundableTestsBody";
    private const string OrganPrototype = "WoundableTestsOrgan";
    private const string OrganCategoryPrototype = "WoundableTestsOrganCategory";
    private const string WoundPrototype = "WoundableTestsWound";

    [TestPrototypes]
    private const string Prototypes = $@"
- type: damageType
  id: {DamageTypePrototype}
  name: damage-type-blunt

- type: damageContainer
  id: {DamageContainerPrototype}
  supportedTypes:
  - {DamageTypePrototype}

- type: entity
  id: {BodyPrototype}
  components:
  - type: StatusEffectContainer
  - type: Damageable
  - type: Body
  - type: InitialBody
    organs:
      {OrganCategoryPrototype}: {OrganPrototype}
  - type: WoundableBody
    damageContainer: {DamageContainerPrototype}
    maximumDamage:
      {DamageTypePrototype}: [200, 10]
    potentialWounds:
      {DamageTypePrototype}:
        0: {WoundPrototype}

- type: entity
  id: {WoundPrototype}
  components:
  - type: StatusEffect
  - type: Wound
    maximumDamage: 100

- type: entity
  id: {OrganPrototype}
  components:
  - type: Organ
    category: {OrganCategoryPrototype}
  - type: StatusEffectContainer
  - type: WoundableOrgan
    weight: 100

- type: organCategory
  id: {OrganCategoryPrototype}
";

    [SidedDependency(Side.Server)] private readonly DamageableSystem _damageable = default!;

    [Test]
    [RunOnSide(Side.Server)]
    public void BasicWoundTests()
    {
        var body = SSpawn(BodyPrototype);

        var dealtDamage = _damageable.ChangeDamage(body,
            new DamageSpecifier()
        {
            DamageDict = { {DamageTypePrototype, 50} },
        });
        Assert.That(dealtDamage.GetTotal(), Is.EqualTo(FixedPoint2.New(50)));
        Assert.That(_damageable.GetAllDamage(body).GetTotal(), Is.EqualTo(FixedPoint2.New(50)));
        Assert.That(SQueryCount<WoundComponent>(), Is.EqualTo(1));

        dealtDamage = _damageable.ChangeDamage(body,
            new DamageSpecifier()
            {
                DamageDict = { {DamageTypePrototype, 50} },
            });
        Assert.That(dealtDamage.GetTotal(), Is.EqualTo(FixedPoint2.New(50)));
        Assert.That(_damageable.GetAllDamage(body).GetTotal(), Is.EqualTo(FixedPoint2.New(100)));
        Assert.That(SQueryCount<WoundComponent>(), Is.EqualTo(1));

        dealtDamage = _damageable.ChangeDamage(body,
            new DamageSpecifier()
            {
                DamageDict = { {DamageTypePrototype, 50} },
            });
        Assert.That(dealtDamage.GetTotal(), Is.EqualTo(FixedPoint2.New(50)));
        Assert.That(_damageable.GetAllDamage(body).GetTotal(), Is.EqualTo(FixedPoint2.New(150)));
        Assert.That(SQueryCount<WoundComponent>(), Is.EqualTo(2));

        foreach (var wound in SQueryList<WoundComponent>())
        {
            SDeleteNow(wound);
        }

        Assert.That(_damageable.GetAllDamage(body).GetTotal(), Is.EqualTo(FixedPoint2.New(0)));
    }
}
