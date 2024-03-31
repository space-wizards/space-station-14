using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using NpgsqlTypes;

namespace Content.Server.IP
{
    public static class IPAddressExt
    {
        // Npgsql used to map inet types as a tuple like this.
        // I'm upgrading the dependencies and I don't wanna rewrite a bunch of DB code, so a few helpers it shall be.
        [return: NotNullIfNotNull(nameof(tuple))]
        public static NpgsqlInet? ToNpgsqlInet(this (IPAddress, int)? tuple)
        {
            if (tuple == null)
                return null;

            return new NpgsqlInet(tuple.Value.Item1, (byte) tuple.Value.Item2);
        }

        [return: NotNullIfNotNull(nameof(inet))]
        public static (IPAddress, int)? ToTuple(this NpgsqlInet? inet)
        {
            if (inet == null)
                return null;

            return (inet.Value.Address, inet.Value.Netmask);
        }

        // Taken from https://stackoverflow.com/a/56461160/4678631
        public static bool IsInSubnet(this System.Net.IPAddress address, string subnetMask)
        {
            var slashIdx = subnetMask.IndexOf("/", StringComparison.Ordinal);
            if (slashIdx == -1)
            {
                // We only handle netmasks in format "IP/PrefixLength".
                throw new NotSupportedException("Only SubNetMasks with a given prefix length are supported.");
            }

            // First parse the address of the netmask before the prefix length.
            var maskAddress = System.Net.IPAddress.Parse(subnetMask[..slashIdx]);

            if (maskAddress.AddressFamily != address.AddressFamily)
            {
                // We got something like an IPV4-Address for an IPv6-Mask. This is not valid.
                return false;
            }

            // Now find out how long the prefix is.
            int maskLength = int.Parse(subnetMask[(slashIdx + 1)..]);

            return address.IsInSubnet(maskAddress, maskLength);
        }

        public static bool IsInSubnet(this System.Net.IPAddress address, (System.Net.IPAddress maskAddress, int maskLength) tuple)
        {
            return address.IsInSubnet(tuple.maskAddress, tuple.maskLength);
        }

        public static bool IsInSubnet(this System.Net.IPAddress address, System.Net.IPAddress maskAddress, int maskLength)
        {
            if (maskAddress.AddressFamily == AddressFamily.InterNetwork)
            {
                // Convert the mask address to an unsigned integer.
                var maskAddressBits = BitConverter.ToUInt32(maskAddress.GetAddressBytes().Reverse().ToArray(), 0);

                // And convert the IpAddress to an unsigned integer.
                var ipAddressBits = BitConverter.ToUInt32(address.GetAddressBytes().Reverse().ToArray(), 0);

                // Get the mask/network address as unsigned integer.
                uint mask = uint.MaxValue << (32 - maskLength);

                // https://stackoverflow.com/a/1499284/3085985
                // Bitwise AND mask and MaskAddress, this should be the same as mask and IpAddress
                // as the end of the mask is 0000 which leads to both addresses to end with 0000
                // and to start with the prefix.
                return (maskAddressBits & mask) == (ipAddressBits & mask);
            }

            if (maskAddress.AddressFamily == AddressFamily.InterNetworkV6)
            {
                // Convert the mask address to a BitArray.
                var maskAddressBits = new BitArray(maskAddress.GetAddressBytes());

                // And convert the IpAddress to a BitArray.
                var ipAddressBits = new BitArray(address.GetAddressBytes());

                if (maskAddressBits.Length != ipAddressBits.Length)
                {
                    throw new ArgumentException("Length of IP Address and Subnet Mask do not match.");
                }

                // Compare the prefix bits.
                for (int maskIndex = 0; maskIndex < maskLength; maskIndex++)
                {
                    if (ipAddressBits[maskIndex] != maskAddressBits[maskIndex])
                    {
                        return false;
                    }
                }

                return true;
            }

            throw new NotSupportedException("Only InterNetworkV6 or InterNetwork address families are supported.");
        }
    }
}
