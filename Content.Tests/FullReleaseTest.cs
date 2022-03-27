using NUnit.Framework;

namespace Content.Tests;

[TestFixture]
public class FullReleaseTest
{
    [Test]
    public void Test()
    {
#if FULL_RELEASE
        Assert.Fail();
#else
        Assert.Pass();
#endif
    }
}
