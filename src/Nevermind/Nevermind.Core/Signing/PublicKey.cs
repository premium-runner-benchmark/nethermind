﻿using System;
using System.Threading;
using Nevermind.Core.Encoding;

namespace Nevermind.Core.Signing
{
    internal class PublicKey
    {
        private const int PublicKeyWithPrefixLengthInBytes = 65;
        private const int PublicKeyLengthInBytes = 64;
        private Address _address;

        public PublicKey(byte[] bytes, byte[] compressedBytes)
            : this(bytes)
        {
            CompressedBytes = compressedBytes;
        }

        public PublicKey(byte[] bytes)
        {
            if (bytes == null)
            {
                throw new ArgumentNullException(nameof(bytes));
            }

            if (bytes.Length != PublicKeyLengthInBytes && bytes.Length != PublicKeyWithPrefixLengthInBytes)
            {
                throw new ArgumentException($"{nameof(PublicKey)} should be {PublicKeyLengthInBytes} bytes long", nameof(bytes));
            }

            if (bytes.Length == PublicKeyWithPrefixLengthInBytes && bytes[0] != 0x04)
            {
                throw new ArgumentException($"Expected prefix of 0x04 for {PublicKeyWithPrefixLengthInBytes} bytes long {nameof(PublicKey)}");
            }

            Buffer.BlockCopy(new byte[] { 0x04 }, 0, PrefixedBytes, 0, 1);
            Buffer.BlockCopy(bytes, bytes.Length - PublicKeyLengthInBytes, PrefixedBytes, 1, 64);
        }

        private Address ComputeAddress()
        {
            byte[] bytes = new byte[64];
            Buffer.BlockCopy(PrefixedBytes, 1, bytes, 0, 64);
            byte[] hash = Keccak.Compute(bytes).Bytes;
            byte[] last160Bits = new byte[20];
            Buffer.BlockCopy(hash, 12, last160Bits, 0, 20);
            return new Address(last160Bits);
        }

        public Address Address => LazyInitializer.EnsureInitialized(ref _address, ComputeAddress);

        public byte[] PrefixedBytes { get; } = new byte[65];

        public byte[] CompressedBytes { get; }

        public override string ToString()
        {
            return Hex.FromBytes(PrefixedBytes, true);
        }
    }
}