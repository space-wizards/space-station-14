using System.Net;
using Content.Server.IP;
using NUnit.Framework;

namespace Content.Tests.Server.Utility
{
    public sealed class IPAddressExtTest
    {
        [Test]
        [TestCase("192.168.5.85/24", "192.168.5.1")]
        [TestCase("192.168.5.85/24", "192.168.5.254")]
        [TestCase("10.128.240.50/30", "10.128.240.48")]
        [TestCase("10.128.240.50/30", "10.128.240.49")]
        [TestCase("10.128.240.50/30", "10.128.240.50")]
        [TestCase("10.128.240.50/30", "10.128.240.51")]
        public void IpV4SubnetMaskMatchesValidIpAddress(string netMask, string ipAddress)
        {
            var ipAddressObj = IPAddress.Parse(ipAddress);
            Assert.That(ipAddressObj.IsInSubnet(netMask), Is.True);
        }

        [Test]
        [TestCase("192.168.5.85/24", "192.168.4.254")]
        [TestCase("192.168.5.85/24", "191.168.5.254")]
        [TestCase("10.128.240.50/30", "10.128.240.47")]
        [TestCase("10.128.240.50/30", "10.128.240.52")]
        [TestCase("10.128.240.50/30", "10.128.239.50")]
        [TestCase("10.128.240.50/30", "10.127.240.51")]
        [TestCase("10.128.240.50/30", "2001:0DB8:ABCD:0012:0000:0000:0000:0000")]
        public void IpV4SubnetMaskDoesNotMatchInvalidIpAddress(string netMask, string ipAddress)
        {
            var ipAddressObj = IPAddress.Parse(ipAddress);
            Assert.That(ipAddressObj.IsInSubnet(netMask), Is.False);
        }

        // ReSharper disable StringLiteralTypo
        [Test]
        [TestCase("2001:db8:abcd:0012::0/64", "2001:0DB8:ABCD:0012:0000:0000:0000:0000")]
        [TestCase("2001:db8:abcd:0012::0/64", "2001:0DB8:ABCD:0012:FFFF:FFFF:FFFF:FFFF")]
        [TestCase("2001:db8:abcd:0012::0/64", "2001:0DB8:ABCD:0012:0001:0000:0000:0000")]
        [TestCase("2001:db8:abcd:0012::0/64", "2001:0DB8:ABCD:0012:FFFF:FFFF:FFFF:FFF0")]
        [TestCase("2001:db8:abcd:0012::0/128", "2001:0DB8:ABCD:0012:0000:0000:0000:0000")]
        public void IpV6SubnetMaskMatchesValidIpAddress(string netMask, string ipAddress)
        {
            var ipAddressObj = IPAddress.Parse(ipAddress);
            Assert.That(ipAddressObj.IsInSubnet(netMask), Is.True);
        }

        [Test]
        [TestCase("2001:db8:abcd:0012::0/64", "2001:0DB8:ABCD:0011:FFFF:FFFF:FFFF:FFFF")]
        [TestCase("2001:db8:abcd:0012::0/64", "2001:0DB8:ABCD:0013:0000:0000:0000:0000")]
        [TestCase("2001:db8:abcd:0012::0/64", "2001:0DB8:ABCD:0013:0001:0000:0000:0000")]
        [TestCase("2001:db8:abcd:0012::0/64", "2001:0DB8:ABCD:0011:FFFF:FFFF:FFFF:FFF0")]
        [TestCase("2001:db8:abcd:0012::0/128", "2001:0DB8:ABCD:0012:0000:0000:0000:0001")]
        [TestCase("2001:db8:abcd:0012::0/128", "10.128.239.50")]
        // ReSharper restore StringLiteralTypo
        public void IpV6SubnetMaskDoesNotMatchInvalidIpAddress(string netMask, string ipAddress)
        {
            var ipAddressObj = IPAddress.Parse(ipAddress);
            Assert.That(ipAddressObj.IsInSubnet(netMask), Is.False);
        }
    }
}
