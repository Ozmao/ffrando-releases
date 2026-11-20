// Oodle Kraken decompressor, ported to C# from ooz's kraken.cpp.
//
// ooz: Kraken Decompressor for Windows, Copyright (C) 2016, Powzix.
// This program is free software: you can redistribute it and/or modify it under
// the terms of the GNU General Public License as published by the Free Software
// Foundation, either version 3 of the License, or (at your option) any later version.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY
// WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A
// PARTICULAR PURPOSE. See the GNU General Public License for more details.
//
// Only the Kraken codec (decoder type 6) is here: it is what FF7 Rebirth's
// IoStore containers use. Unreal compresses every 64 KB block on its own, so
// each call decodes one small block.
using System.Numerics;
using System.Runtime.InteropServices;

namespace FFRando.KrakenHelper;

#pragma warning disable CS0649 // HuffReader fields start at zero on purpose

static unsafe class Kraken
{
    const int Pad = 64;

    /// <summary>Decompresses one Oodle Kraken stream. Throws on bad data.</summary>
    public static byte[] Decompress(ReadOnlySpan<byte> src, int dstLen)
    {
        var dst = new byte[dstLen];
        Decompress(src, dst);
        return dst;
    }

    public static void Decompress(ReadOnlySpan<byte> src, Span<byte> dst)
    {
        // The decoder reads and writes a few bytes past its buffers, as ooz does,
        // so everything works in padded copies.
        var srcBuf = new byte[src.Length + Pad * 2];
        src.CopyTo(srcBuf.AsSpan(Pad));
        var dstBuf = new byte[dst.Length + Pad * 2];
        var scratch = new byte[0x6C000 + Pad * 2];
        fixed (byte* ps = srcBuf, pd = dstBuf, pscr = scratch)
        {
            var dec = new Decoder { Scratch = pscr + Pad, ScratchSize = 0x6C000 };
            byte* s = ps + Pad; int srcLeft = src.Length;
            byte* d = pd + Pad; int dstLeft = dst.Length, offset = 0;
            while (dstLeft != 0)
            {
                if (!DecodeStep(ref dec, d, offset, dstLeft, s, srcLeft) || dec.SrcUsed == 0)
                    throw new InvalidDataException("Bad Kraken data.");
                s += dec.SrcUsed; srcLeft -= dec.SrcUsed;
                dstLeft -= dec.DstUsed; offset += dec.DstUsed;
            }
            if (srcLeft != 0) throw new InvalidDataException("Bad Kraken data (trailing bytes).");
        }
        dstBuf.AsSpan(Pad, dst.Length).CopyTo(dst);
    }

    struct Decoder
    {
        public int SrcUsed, DstUsed;
        public byte* Scratch; public int ScratchSize;
        public int DecoderType; public bool Restart, Uncompressed, UseChecksums;
    }

    struct BitReader { public byte* P, PEnd; public uint Bits; public int BitPos; }
    struct BitReader2 { public byte* P, PEnd; public uint BitPos; }

    static int Bsr(uint x) => 31 - BitOperations.LeadingZeroCount(x);
    static int Bsf(uint x) => BitOperations.TrailingZeroCount(x);
    static int Clz(uint x) => BitOperations.LeadingZeroCount(x);
    static uint Swap32(uint x) => System.Buffers.Binary.BinaryPrimitives.ReverseEndianness(x);
    static ulong Swap64(ulong x) => System.Buffers.Binary.BinaryPrimitives.ReverseEndianness(x);
    static uint U32(byte* p) => *(uint*)p;
    static ushort U16(byte* p) => *(ushort*)p;

    // ---- bit reader ----

    static void Refill(ref BitReader b)
    {
        while (b.BitPos > 0)
        {
            b.Bits |= (uint)(b.P < b.PEnd ? *b.P : 0) << b.BitPos;
            b.BitPos -= 8; b.P++;
        }
    }

    static void RefillBackwards(ref BitReader b)
    {
        while (b.BitPos > 0)
        {
            b.P--;
            b.Bits |= (uint)(b.P >= b.PEnd ? *b.P : 0) << b.BitPos;
            b.BitPos -= 8;
        }
    }

    static int ReadBit(ref BitReader b) { Refill(ref b); return ReadBitNoRefill(ref b); }

    static int ReadBitNoRefill(ref BitReader b)
    {
        int r = (int)(b.Bits >> 31); b.Bits <<= 1; b.BitPos += 1; return r;
    }

    static int ReadBitsNoRefill(ref BitReader b, int n)
    {
        int r = (int)(b.Bits >> (32 - n)); b.Bits <<= n; b.BitPos += n; return r;
    }

    static int ReadBitsNoRefillZero(ref BitReader b, int n)
    {
        int r = (int)(b.Bits >> 1 >> (31 - n)); b.Bits = n == 32 ? 0 : b.Bits << n; b.BitPos += n; return r;
    }

    static uint ReadMoreThan24Bits(ref BitReader b, int n)
    {
        uint rv;
        if (n <= 24) rv = (uint)ReadBitsNoRefillZero(ref b, n);
        else { rv = (uint)ReadBitsNoRefill(ref b, 24) << (n - 24); Refill(ref b); rv += (uint)ReadBitsNoRefill(ref b, n - 24); }
        Refill(ref b);
        return rv;
    }

    static uint ReadMoreThan24BitsB(ref BitReader b, int n)
    {
        uint rv;
        if (n <= 24) rv = (uint)ReadBitsNoRefillZero(ref b, n);
        else { rv = (uint)ReadBitsNoRefill(ref b, 24) << (n - 24); RefillBackwards(ref b); rv += (uint)ReadBitsNoRefill(ref b, n - 24); }
        RefillBackwards(ref b);
        return rv;
    }

    static uint ReadDistance(ref BitReader b, uint v, bool backwards)
    {
        uint w, m, rv; int n;
        if (v < 0xF0)
        {
            n = (int)(v >> 4) + 4;
            w = BitOperations.RotateLeft(b.Bits | 1, n);
            b.BitPos += n; m = (2u << n) - 1; b.Bits = w & ~m;
            rv = ((w & m) << 4) + (v & 0xF) - 248;
        }
        else
        {
            n = (int)v - 0xF0 + 4;
            w = BitOperations.RotateLeft(b.Bits | 1, n);
            b.BitPos += n; m = (2u << n) - 1; b.Bits = w & ~m;
            rv = 8322816 + ((w & m) << 12);
            if (backwards) RefillBackwards(ref b); else Refill(ref b);
            rv += b.Bits >> 20;
            b.BitPos += 12; b.Bits <<= 12;
        }
        if (backwards) RefillBackwards(ref b); else Refill(ref b);
        return rv;
    }

    static bool ReadLength(ref BitReader b, out uint v, bool backwards)
    {
        v = 0;
        int n = Clz(b.Bits);
        if (n > 12) return false;
        b.BitPos += n; b.Bits <<= n;
        if (backwards) RefillBackwards(ref b); else Refill(ref b);
        n += 7;
        b.BitPos += n;
        v = (b.Bits >> (32 - n)) - 64;
        b.Bits <<= n;
        if (backwards) RefillBackwards(ref b); else Refill(ref b);
        return true;
    }

    static int ReadFluff(ref BitReader b, int numSymbols)
    {
        if (numSymbols == 256) return 0;
        int x = 257 - numSymbols;
        if (x > numSymbols) x = numSymbols;
        x *= 2;
        int y = Bsr((uint)(x - 1)) + 1;
        uint v = b.Bits >> (32 - y);
        uint z = (1u << y) - (uint)x;
        if ((v >> 1) >= z) { b.Bits <<= y; b.BitPos += y; return (int)(v - z); }
        b.Bits <<= y - 1; b.BitPos += y - 1; return (int)(v >> 1);
    }

    // ---- headers and the block loop ----

    static byte* ParseHeader(ref Decoder d, byte* p)
    {
        int b = p[0];
        if ((b & 0xF) != 0xC || ((b >> 4) & 3) != 0) return null;
        d.Restart = ((b >> 7) & 1) != 0;
        d.Uncompressed = ((b >> 6) & 1) != 0;
        b = p[1];
        d.DecoderType = b & 0x7F;
        d.UseChecksums = (b >> 7) != 0;
        if (d.DecoderType != 6) return null; // only Kraken is ported
        return p + 2;
    }

    static bool DecodeStep(ref Decoder dec, byte* dstStart, int offset, int dstBytesLeftIn, byte* src, int srcBytesLeft)
    {
        byte* srcIn = src, srcEnd = src + srcBytesLeft;
        if ((offset & 0x3FFFF) == 0)
        {
            src = ParseHeader(ref dec, src);
            if (src == null) return false;
        }
        int dstBytesLeft = Math.Min(0x40000, dstBytesLeftIn);
        if (dec.Uncompressed)
        {
            if (srcEnd - src < dstBytesLeft) { dec.SrcUsed = dec.DstUsed = 0; return true; }
            Buffer.MemoryCopy(src, dstStart + offset, dstBytesLeft, dstBytesLeft);
            dec.SrcUsed = (int)(src - srcIn) + dstBytesLeft; dec.DstUsed = dstBytesLeft;
            return true;
        }

        // Quantum header.
        uint v = (uint)(src[0] << 16 | src[1] << 8 | src[2]);
        uint size = v & 0x3FFFF, compressedSize, checksum = 0;
        if (size != 0x3FFFF)
        {
            compressedSize = size + 1;
            src += dec.UseChecksums ? 6 : 3;
        }
        else if ((v >> 18) == 1)
        {
            checksum = src[3]; compressedSize = 0; src += 4;
        }
        else return false;
        if (src > srcEnd) return false;
        if ((ulong)(srcEnd - src) < compressedSize) { dec.SrcUsed = dec.DstUsed = 0; return true; }
        if (compressedSize > (uint)dstBytesLeft) return false;
        if (compressedSize == 0)
        {
            new Span<byte>(dstStart + offset, dstBytesLeft).Fill((byte)checksum);
            dec.SrcUsed = (int)(src - srcIn); dec.DstUsed = dstBytesLeft;
            return true;
        }
        // ooz never checks the checksum either.
        if (compressedSize == dstBytesLeft)
        {
            Buffer.MemoryCopy(src, dstStart + offset, dstBytesLeft, dstBytesLeft);
            dec.SrcUsed = (int)(src - srcIn) + dstBytesLeft; dec.DstUsed = dstBytesLeft;
            return true;
        }
        int n = DecodeQuantum(dstStart + offset, dstStart + offset + dstBytesLeft, dstStart, src, src + compressedSize, dec.Scratch, dec.Scratch + dec.ScratchSize);
        if (n != compressedSize) return false;
        dec.SrcUsed = (int)(src - srcIn) + n; dec.DstUsed = dstBytesLeft;
        return true;
    }

    static int GetBlockSize(byte* src, byte* srcEnd, out int destSize, int destCapacity)
    {
        byte* srcOrg = src; int srcSize, dstSize; destSize = 0;
        if (srcEnd - src < 2) return -1;
        int chunkType = (src[0] >> 4) & 7;
        if (chunkType == 0)
        {
            if (src[0] >= 0x80) { srcSize = ((src[0] << 8) | src[1]) & 0xFFF; src += 2; }
            else
            {
                if (srcEnd - src < 3) return -1;
                srcSize = (src[0] << 16) | (src[1] << 8) | src[2];
                if ((srcSize & ~0x3FFFF) != 0) return -1;
                src += 3;
            }
            if (srcSize > destCapacity || srcEnd - src < srcSize) return -1;
            destSize = srcSize;
            return (int)(src + srcSize - srcOrg);
        }
        if (chunkType >= 6) return -1;
        if (src[0] >= 0x80)
        {
            if (srcEnd - src < 3) return -1;
            uint bits = (uint)((src[0] << 16) | (src[1] << 8) | src[2]);
            srcSize = (int)(bits & 0x3FF);
            dstSize = srcSize + (int)((bits >> 10) & 0x3FF) + 1;
            src += 3;
        }
        else
        {
            if (srcEnd - src < 5) return -1;
            uint bits = (uint)((src[1] << 24) | (src[2] << 16) | (src[3] << 8) | src[4]);
            srcSize = (int)(bits & 0x3FFFF);
            dstSize = (int)((((bits >> 18) | ((uint)src[0] << 14)) & 0x3FFFF) + 1);
            if (srcSize >= dstSize) return -1;
            src += 5;
        }
        if (srcEnd - src < srcSize || dstSize > destCapacity) return -1;
        destSize = dstSize;
        return srcSize;
    }

    static int DecodeBytes(ref byte* output, byte* src, byte* srcEnd, out int decodedSize, long outputSize, bool forceMemmove, byte* scratch, byte* scratchEnd)
    {
        byte* srcOrg = src; int srcSize, dstSize; decodedSize = 0;
        if (srcEnd - src < 2) return -1;
        int chunkType = (src[0] >> 4) & 7;
        if (chunkType == 0)
        {
            if (src[0] >= 0x80) { srcSize = ((src[0] << 8) | src[1]) & 0xFFF; src += 2; }
            else
            {
                if (srcEnd - src < 3) return -1;
                srcSize = (src[0] << 16) | (src[1] << 8) | src[2];
                if ((srcSize & ~0x3FFFF) != 0) return -1;
                src += 3;
            }
            if (srcSize > outputSize || srcEnd - src < srcSize) return -1;
            decodedSize = srcSize;
            if (forceMemmove) Buffer.MemoryCopy(src, output, srcSize, srcSize);
            else output = src;
            return (int)(src + srcSize - srcOrg);
        }
        if (src[0] >= 0x80)
        {
            if (srcEnd - src < 3) return -1;
            uint bits = (uint)((src[0] << 16) | (src[1] << 8) | src[2]);
            srcSize = (int)(bits & 0x3FF);
            dstSize = srcSize + (int)((bits >> 10) & 0x3FF) + 1;
            src += 3;
        }
        else
        {
            if (srcEnd - src < 5) return -1;
            uint bits = (uint)((src[1] << 24) | (src[2] << 16) | (src[3] << 8) | src[4]);
            srcSize = (int)(bits & 0x3FFFF);
            dstSize = (int)((((bits >> 18) | ((uint)src[0] << 14)) & 0x3FFFF) + 1);
            if (srcSize >= dstSize) return -1;
            src += 5;
        }
        if (srcEnd - src < srcSize || dstSize > outputSize) return -1;
        byte* dst = output;
        if (dst == scratch)
        {
            if (scratchEnd - scratch < dstSize) return -1;
            scratch += dstSize;
        }
        int srcUsed = chunkType switch
        {
            2 or 4 => DecodeBytesType12(src, srcSize, dst, dstSize, chunkType >> 1),
            5 => DecodeRecursive(src, srcSize, dst, dstSize, scratch, scratchEnd),
            3 => DecodeRle(src, srcSize, dst, dstSize, scratch, scratchEnd),
            1 => DecodeTans(src, srcSize, dst, dstSize, scratch, scratchEnd),
            _ => -1,
        };
        if (srcUsed != srcSize) return -1;
        decodedSize = dstSize;
        return (int)(src + srcSize - srcOrg);
    }

    // ---- Huffman ----

    static int ReadCodeLengthsOld(ref BitReader bits, byte* syms, uint* codePrefix)
    {
        if (ReadBitNoRefill(ref bits) != 0)
        {
            int n, sym = 0, codelen, numSymbols = 0, avgBitsX4 = 32;
            int forcedBits = ReadBitsNoRefill(ref bits, 2);
            uint thres = 1u << (31 - (int)(20u >> forcedBits));
            bool skip = ReadBit(ref bits) != 0;
            do
            {
                if (!skip)
                {
                    if ((bits.Bits & 0xff000000) == 0) return -1;
                    sym += ReadBitsNoRefill(ref bits, 2 * (Clz(bits.Bits) + 1)) - 2 + 1;
                    if (sym >= 256) break;
                }
                skip = false;
                Refill(ref bits);
                if ((bits.Bits & 0xff000000) == 0) return -1;
                n = ReadBitsNoRefill(ref bits, 2 * (Clz(bits.Bits) + 1)) - 2 + 1;
                if (sym + n > 256) return -1;
                Refill(ref bits);
                numSymbols += n;
                do
                {
                    if (bits.Bits < thres) return -1;
                    int lz = Clz(bits.Bits);
                    int v = ReadBitsNoRefill(ref bits, lz + forcedBits + 1) + ((lz - 1) << forcedBits);
                    codelen = (-(v & 1) ^ (v >> 1)) + ((avgBitsX4 + 2) >> 2);
                    if (codelen < 1 || codelen > 11) return -1;
                    avgBitsX4 = codelen + ((3 * avgBitsX4 + 2) >> 2);
                    Refill(ref bits);
                    syms[codePrefix[codelen]++] = (byte)sym++;
                } while (--n != 0);
            } while (sym != 256);
            return sym == 256 && numSymbols >= 2 ? numSymbols : -1;
        }
        else
        {
            int numSymbols = ReadBitsNoRefill(ref bits, 8);
            if (numSymbols == 0) return -1;
            if (numSymbols == 1) syms[0] = (byte)ReadBitsNoRefill(ref bits, 8);
            else
            {
                int codelenBits = ReadBitsNoRefill(ref bits, 3);
                if (codelenBits > 4) return -1;
                for (int i = 0; i < numSymbols; i++)
                {
                    Refill(ref bits);
                    int sym = ReadBitsNoRefill(ref bits, 8);
                    int codelen = ReadBitsNoRefillZero(ref bits, codelenBits) + 1;
                    if (codelen > 11) return -1;
                    syms[codePrefix[codelen]++] = (byte)sym;
                }
            }
            return numSymbols;
        }
    }

    static readonly uint[] RiceBits2Value = [
        0x80000000, 0x00000007, 0x10000006, 0x00000006, 0x20000005, 0x00000105, 0x10000005, 0x00000005,
        0x30000004, 0x00000204, 0x10000104, 0x00000104, 0x20000004, 0x00010004, 0x10000004, 0x00000004,
        0x40000003, 0x00000303, 0x10000203, 0x00000203, 0x20000103, 0x00010103, 0x10000103, 0x00000103,
        0x30000003, 0x00020003, 0x10010003, 0x00010003, 0x20000003, 0x01000003, 0x10000003, 0x00000003,
        0x50000002, 0x00000402, 0x10000302, 0x00000302, 0x20000202, 0x00010202, 0x10000202, 0x00000202,
        0x30000102, 0x00020102, 0x10010102, 0x00010102, 0x20000102, 0x01000102, 0x10000102, 0x00000102,
        0x40000002, 0x00030002, 0x10020002, 0x00020002, 0x20010002, 0x01010002, 0x10010002, 0x00010002,
        0x30000002, 0x02000002, 0x11000002, 0x01000002, 0x20000002, 0x00000012, 0x10000002, 0x00000002,
        0x60000001, 0x00000501, 0x10000401, 0x00000401, 0x20000301, 0x00010301, 0x10000301, 0x00000301,
        0x30000201, 0x00020201, 0x10010201, 0x00010201, 0x20000201, 0x01000201, 0x10000201, 0x00000201,
        0x40000101, 0x00030101, 0x10020101, 0x00020101, 0x20010101, 0x01010101, 0x10010101, 0x00010101,
        0x30000101, 0x02000101, 0x11000101, 0x01000101, 0x20000101, 0x00000111, 0x10000101, 0x00000101,
        0x50000001, 0x00040001, 0x10030001, 0x00030001, 0x20020001, 0x01020001, 0x10020001, 0x00020001,
        0x30010001, 0x02010001, 0x11010001, 0x01010001, 0x20010001, 0x00010011, 0x10010001, 0x00010001,
        0x40000001, 0x03000001, 0x12000001, 0x02000001, 0x21000001, 0x01000011, 0x11000001, 0x01000001,
        0x30000001, 0x00000021, 0x10000011, 0x00000011, 0x20000001, 0x00001001, 0x10000001, 0x00000001,
        0x70000000, 0x00000600, 0x10000500, 0x00000500, 0x20000400, 0x00010400, 0x10000400, 0x00000400,
        0x30000300, 0x00020300, 0x10010300, 0x00010300, 0x20000300, 0x01000300, 0x10000300, 0x00000300,
        0x40000200, 0x00030200, 0x10020200, 0x00020200, 0x20010200, 0x01010200, 0x10010200, 0x00010200,
        0x30000200, 0x02000200, 0x11000200, 0x01000200, 0x20000200, 0x00000210, 0x10000200, 0x00000200,
        0x50000100, 0x00040100, 0x10030100, 0x00030100, 0x20020100, 0x01020100, 0x10020100, 0x00020100,
        0x30010100, 0x02010100, 0x11010100, 0x01010100, 0x20010100, 0x00010110, 0x10010100, 0x00010100,
        0x40000100, 0x03000100, 0x12000100, 0x02000100, 0x21000100, 0x01000110, 0x11000100, 0x01000100,
        0x30000100, 0x00000120, 0x10000110, 0x00000110, 0x20000100, 0x00001100, 0x10000100, 0x00000100,
        0x60000000, 0x00050000, 0x10040000, 0x00040000, 0x20030000, 0x01030000, 0x10030000, 0x00030000,
        0x30020000, 0x02020000, 0x11020000, 0x01020000, 0x20020000, 0x00020010, 0x10020000, 0x00020000,
        0x40010000, 0x03010000, 0x12010000, 0x02010000, 0x21010000, 0x01010010, 0x11010000, 0x01010000,
        0x30010000, 0x00010020, 0x10010010, 0x00010010, 0x20010000, 0x00011000, 0x10010000, 0x00010000,
        0x50000000, 0x04000000, 0x13000000, 0x03000000, 0x22000000, 0x02000010, 0x12000000, 0x02000000,
        0x31000000, 0x01000020, 0x11000010, 0x01000010, 0x21000000, 0x01001000, 0x11000000, 0x01000000,
        0x40000000, 0x00000030, 0x10000020, 0x00000020, 0x20000010, 0x00001010, 0x10000010, 0x00000010,
        0x30000000, 0x00002000, 0x10001000, 0x00001000, 0x20000000, 0x00100000, 0x10000000, 0x00000000,
    ];

    static byte RiceBits2Len(uint v) => (byte)BitOperations.PopCount(v);

    static bool DecodeGolombRiceLengths(byte* dst, int size, ref BitReader2 br)
    {
        byte* p = br.P, pEnd = br.PEnd, dstEnd = dst + size;
        if (p >= pEnd) return false;
        int count = -(int)br.BitPos;
        uint v = (uint)(*p++ & (255 >> (int)br.BitPos));
        for (; ; )
        {
            if (v == 0) count += 8;
            else
            {
                uint x = RiceBits2Value[v];
                *(uint*)&dst[0] = (uint)count + (x & 0x0f0f0f0f);
                *(uint*)&dst[4] = (x >> 4) & 0x0f0f0f0f;
                dst += RiceBits2Len(v);
                if (dst >= dstEnd) break;
                count = (int)(x >> 28);
            }
            if (p >= pEnd) return false;
            v = *p++;
        }
        if (dst > dstEnd)
        {
            long n = dst - dstEnd;
            do v &= v - 1; while (--n != 0);
        }
        uint bitpos = 0;
        if ((v & 1) == 0)
        {
            p--;
            bitpos = (uint)(8 - Bsf(v));
        }
        br.P = p; br.BitPos = bitpos;
        return true;
    }

    static bool DecodeGolombRiceBits(byte* dst, uint size, uint bitcount, ref BitReader2 br)
    {
        if (bitcount == 0) return true;
        byte* dstEnd = dst + size, p = br.P;
        int bitpos = (int)br.BitPos;
        uint bitsRequired = (uint)bitpos + bitcount * size;
        uint bytesRequired = (bitsRequired + 7) >> 3;
        if (bytesRequired > br.PEnd - p) return false;
        br.P = p + (bitsRequired >> 3);
        br.BitPos = bitsRequired & 7;
        ulong bak = *(ulong*)dstEnd;
        if (bitcount < 2)
        {
            do
            {
                ulong bits = (byte)(Swap32(U32(p)) >> (24 - bitpos));
                p += 1;
                bits = (bits | (bits << 28)) & 0xF0000000Ful;
                bits = (bits | (bits << 14)) & 0x3000300030003ul;
                bits = (bits | (bits << 7)) & 0x0101010101010101ul;
                *(ulong*)dst = *(ulong*)dst * 2 + Swap64(bits);
                dst += 8;
            } while (dst < dstEnd);
        }
        else if (bitcount == 2)
        {
            do
            {
                ulong bits = (ushort)(Swap32(U32(p)) >> (16 - bitpos));
                p += 2;
                bits = (bits | (bits << 24)) & 0xFF000000FFul;
                bits = (bits | (bits << 12)) & 0xF000F000F000Ful;
                bits = (bits | (bits << 6)) & 0x0303030303030303ul;
                *(ulong*)dst = *(ulong*)dst * 4 + Swap64(bits);
                dst += 8;
            } while (dst < dstEnd);
        }
        else
        {
            do
            {
                ulong bits = (Swap32(U32(p)) >> (8 - bitpos)) & 0xffffff;
                p += 3;
                bits = (bits | (bits << 20)) & 0xFFF00000FFFul;
                bits = (bits | (bits << 10)) & 0x3F003F003F003Ful;
                bits = (bits | (bits << 5)) & 0x0707070707070707ul;
                *(ulong*)dst = *(ulong*)dst * 8 + Swap64(bits);
                dst += 8;
            } while (dst < dstEnd);
        }
        *(ulong*)dstEnd = bak;
        return true;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct HuffRange { public ushort Symbol, Num; }

    static int ConvertToRanges(HuffRange* range, int numSymbols, int P, byte* symlen, ref BitReader bits)
    {
        int numRanges = P >> 1, v, symIdx = 0;
        if ((P & 1) != 0)
        {
            Refill(ref bits);
            v = *symlen++;
            if (v >= 8) return -1;
            symIdx = ReadBitsNoRefill(ref bits, v + 1) + (1 << (v + 1)) - 1;
        }
        int symsUsed = 0;
        for (int i = 0; i < numRanges; i++)
        {
            Refill(ref bits);
            v = symlen[0];
            if (v >= 9) return -1;
            int num = ReadBitsNoRefillZero(ref bits, v) + (1 << v);
            v = symlen[1];
            if (v >= 8) return -1;
            int space = ReadBitsNoRefill(ref bits, v + 1) + (1 << (v + 1)) - 1;
            range[i].Symbol = (ushort)symIdx;
            range[i].Num = (ushort)num;
            symsUsed += num;
            symIdx += num + space;
            symlen += 2;
        }
        if (symIdx >= 256 || symsUsed >= numSymbols || symIdx + numSymbols - symsUsed > 256) return -1;
        range[numRanges].Symbol = (ushort)symIdx;
        range[numRanges].Num = (ushort)(numSymbols - symsUsed);
        return numRanges + 1;
    }

    static int ReadCodeLengthsNew(ref BitReader bits, byte* syms, uint* codePrefix)
    {
        int forcedBits = ReadBitsNoRefill(ref bits, 2);
        int numSymbols = ReadBitsNoRefill(ref bits, 8) + 1;
        int fluff = ReadFluff(ref bits, numSymbols);
        byte* codeLen = stackalloc byte[512 + 32];
        var br2 = new BitReader2
        {
            BitPos = (uint)((bits.BitPos - 24) & 7),
            PEnd = bits.PEnd,
            P = bits.P - (uint)((24 - bits.BitPos + 7) >> 3),
        };
        if (!DecodeGolombRiceLengths(codeLen, numSymbols + fluff, ref br2)) return -1;
        new Span<byte>(codeLen + numSymbols + fluff, 16).Clear();
        if (!DecodeGolombRiceBits(codeLen, (uint)numSymbols, (uint)forcedBits, ref br2)) return -1;
        bits.BitPos = 24; bits.P = br2.P; bits.Bits = 0;
        Refill(ref bits);
        bits.Bits <<= (int)br2.BitPos;
        bits.BitPos += (int)br2.BitPos;

        uint runningSum = 0x1e;
        for (int i = 0; i < numSymbols; i++)
        {
            int v = codeLen[i];
            v = -(v & 1) ^ (v >> 1);
            codeLen[i] = (byte)(v + (int)(runningSum >> 2) + 1);
            if (codeLen[i] < 1 || codeLen[i] > 11) return -1;
            runningSum = (uint)(runningSum + v);
        }
        HuffRange* range = stackalloc HuffRange[128];
        int ranges = ConvertToRanges(range, numSymbols, fluff, &codeLen[numSymbols], ref bits);
        if (ranges <= 0) return -1;
        byte* cp = codeLen;
        for (int i = 0; i < ranges; i++)
        {
            int sym = range[i].Symbol, n = range[i].Num;
            do syms[codePrefix[*cp++]++] = (byte)sym++; while (--n != 0);
        }
        return numSymbols;
    }

    static readonly uint[] CodePrefixOrg = [0x0, 0x0, 0x2, 0x6, 0xE, 0x1E, 0x3E, 0x7E, 0xFE, 0x1FE, 0x2FE, 0x3FE];

    static bool MakeLut(uint* prefixCur, byte* bits2len, byte* bits2sym, byte* syms)
    {
        uint currslot = 0;
        for (int i = 1; i < 11; i++)
        {
            uint start = CodePrefixOrg[i], count = prefixCur[i] - start;
            if (count != 0)
            {
                uint stepsize = 1u << (11 - i), numToSet = count << (11 - i);
                if (currslot + numToSet > 2048) return false;
                new Span<byte>(bits2len + currslot, (int)numToSet).Fill((byte)i);
                byte* p = bits2sym + currslot;
                for (uint j = 0; j != count; j++, p += stepsize)
                    new Span<byte>(p, (int)stepsize).Fill(syms[start + j]);
                currslot += numToSet;
            }
        }
        if (prefixCur[11] - CodePrefixOrg[11] != 0)
        {
            uint numToSet = prefixCur[11] - CodePrefixOrg[11];
            if (currslot + numToSet > 2048) return false;
            new Span<byte>(bits2len + currslot, (int)numToSet).Fill(11);
            Buffer.MemoryCopy(&syms[CodePrefixOrg[11]], bits2sym + currslot, numToSet, numToSet);
            currslot += numToSet;
        }
        return currslot == 2048;
    }

    static readonly ushort[] Rev11 = Enumerable.Range(0, 2048).Select(i =>
    {
        int r = 0; for (int b = 0; b < 11; b++) if ((i & (1 << b)) != 0) r |= 1 << (10 - b);
        return (ushort)r;
    }).ToArray();

    struct HuffReader
    {
        public byte* Output, OutputEnd, Src, SrcMid, SrcEnd, SrcMidOrg;
        public int SrcBitpos, SrcMidBitpos, SrcEndBitpos;
        public uint SrcBits, SrcMidBits, SrcEndBits;
    }

    static bool DecodeBytesCore(ref HuffReader hr, byte* lenLut, byte* symLut)
    {
        byte* src = hr.Src; uint srcBits = hr.SrcBits; int srcBitpos = hr.SrcBitpos;
        byte* srcMid = hr.SrcMid; uint srcMidBits = hr.SrcMidBits; int srcMidBitpos = hr.SrcMidBitpos;
        byte* srcEnd = hr.SrcEnd; uint srcEndBits = hr.SrcEndBits; int srcEndBitpos = hr.SrcEndBitpos;
        int k, n;
        byte* dst = hr.Output, dstEnd = hr.OutputEnd;
        if (src > srcMid) return false;
        if (hr.SrcEnd - srcMid >= 4 && dstEnd - dst >= 6)
        {
            dstEnd -= 5; srcEnd -= 4;
            while (dst < dstEnd && src <= srcMid && srcMid <= srcEnd)
            {
                srcBits |= U32(src) << srcBitpos; src += (31 - srcBitpos) >> 3;
                srcEndBits |= Swap32(U32(srcEnd)) << srcEndBitpos; srcEnd -= (31 - srcEndBitpos) >> 3;
                srcMidBits |= U32(srcMid) << srcMidBitpos; srcMid += (31 - srcMidBitpos) >> 3;
                srcBitpos |= 0x18; srcEndBitpos |= 0x18; srcMidBitpos |= 0x18;

                k = (int)(srcBits & 0x7FF); n = lenLut[k]; srcBits >>= n; srcBitpos -= n; dst[0] = symLut[k];
                k = (int)(srcEndBits & 0x7FF); n = lenLut[k]; srcEndBits >>= n; srcEndBitpos -= n; dst[1] = symLut[k];
                k = (int)(srcMidBits & 0x7FF); n = lenLut[k]; srcMidBits >>= n; srcMidBitpos -= n; dst[2] = symLut[k];
                k = (int)(srcBits & 0x7FF); n = lenLut[k]; srcBits >>= n; srcBitpos -= n; dst[3] = symLut[k];
                k = (int)(srcEndBits & 0x7FF); n = lenLut[k]; srcEndBits >>= n; srcEndBitpos -= n; dst[4] = symLut[k];
                k = (int)(srcMidBits & 0x7FF); n = lenLut[k]; srcMidBits >>= n; srcMidBitpos -= n; dst[5] = symLut[k];
                dst += 6;
            }
            dstEnd += 5;
            src -= srcBitpos >> 3; srcBitpos &= 7;
            srcEnd += 4 + (srcEndBitpos >> 3); srcEndBitpos &= 7;
            srcMid -= srcMidBitpos >> 3; srcMidBitpos &= 7;
        }
        for (; ; )
        {
            if (dst >= dstEnd) break;
            if (srcMid - src <= 1) { if (srcMid - src == 1) srcBits |= (uint)*src << srcBitpos; }
            else srcBits |= (uint)U16(src) << srcBitpos;
            k = (int)(srcBits & 0x7FF); n = lenLut[k];
            srcBitpos -= n; srcBits >>= n;
            *dst++ = symLut[k];
            src += (7 - srcBitpos) >> 3; srcBitpos &= 7;
            if (dst < dstEnd)
            {
                if (srcEnd - srcMid <= 1)
                {
                    if (srcEnd - srcMid == 1) { srcEndBits |= (uint)*srcMid << srcEndBitpos; srcMidBits |= (uint)*srcMid << srcMidBitpos; }
                }
                else
                {
                    uint v = U16(srcEnd - 2);
                    srcEndBits |= (((v >> 8) | (v << 8)) & 0xffff) << srcEndBitpos;
                    srcMidBits |= (uint)U16(srcMid) << srcMidBitpos;
                }
                n = lenLut[srcEndBits & 0x7FF];
                *dst++ = symLut[srcEndBits & 0x7FF];
                srcEndBitpos -= n; srcEndBits >>= n;
                srcEnd -= (7 - srcEndBitpos) >> 3; srcEndBitpos &= 7;
                if (dst < dstEnd)
                {
                    n = lenLut[srcMidBits & 0x7FF];
                    *dst++ = symLut[srcMidBits & 0x7FF];
                    srcMidBitpos -= n; srcMidBits >>= n;
                    srcMid += (7 - srcMidBitpos) >> 3; srcMidBitpos &= 7;
                }
            }
            if (src > srcMid || srcMid > srcEnd) return false;
        }
        return src == hr.SrcMidOrg && srcEnd == srcMid;
    }

    static int DecodeBytesType12(byte* src, int srcSize, byte* output, int outputSize, int type)
    {
        byte* srcEnd = src + srcSize;
        var bits = new BitReader { BitPos = 24, Bits = 0, P = src, PEnd = srcEnd };
        Refill(ref bits);
        uint* codePrefix = stackalloc uint[12];
        for (int i = 0; i < 12; i++) codePrefix[i] = CodePrefixOrg[i];
        byte* syms = stackalloc byte[1280];
        int numSyms;
        if (ReadBitNoRefill(ref bits) == 0) numSyms = ReadCodeLengthsOld(ref bits, syms, codePrefix);
        else if (ReadBitNoRefill(ref bits) == 0) numSyms = ReadCodeLengthsNew(ref bits, syms, codePrefix);
        else return -1;
        if (numSyms < 1) return -1;
        src = bits.P - ((24 - bits.BitPos) / 8);
        if (numSyms == 1)
        {
            new Span<byte>(output, outputSize).Fill(syms[0]);
            return srcSize;
        }
        byte* lut = stackalloc byte[(2048 + 16) * 2];
        byte* bits2len = lut, bits2sym = lut + 2048 + 16;
        if (!MakeLut(codePrefix, bits2len, bits2sym, syms)) return -1;
        byte* rev = stackalloc byte[4096];
        byte* revLen = rev, revSym = rev + 2048;
        for (int i = 0; i < 2048; i++) { revLen[i] = bits2len[Rev11[i]]; revSym[i] = bits2sym[Rev11[i]]; }

        var hr = new HuffReader();
        if (type == 1)
        {
            if (src + 3 > srcEnd) return -1;
            uint splitMid = U16(src); src += 2;
            hr.Output = output; hr.OutputEnd = output + outputSize;
            hr.Src = src; hr.SrcEnd = srcEnd; hr.SrcMidOrg = hr.SrcMid = src + splitMid;
            if (!DecodeBytesCore(ref hr, revLen, revSym)) return -1;
        }
        else
        {
            if (src + 6 > srcEnd) return -1;
            int halfOutputSize = (outputSize + 1) >> 1;
            uint splitMid = U32(src) & 0xFFFFFF; src += 3;
            if (splitMid > srcEnd - src) return -1;
            byte* srcMid = src + splitMid;
            uint splitLeft = U16(src); src += 2;
            if (srcMid - src < splitLeft + 2 || srcEnd - srcMid < 3) return -1;
            uint splitRight = U16(srcMid);
            if (srcEnd - (srcMid + 2) < splitRight + 2) return -1;

            hr.Output = output; hr.OutputEnd = output + halfOutputSize;
            hr.Src = src; hr.SrcEnd = srcMid; hr.SrcMidOrg = hr.SrcMid = src + splitLeft;
            if (!DecodeBytesCore(ref hr, revLen, revSym)) return -1;

            hr = new HuffReader
            {
                Output = output + halfOutputSize, OutputEnd = output + outputSize,
                Src = srcMid + 2, SrcEnd = srcEnd,
            };
            hr.SrcMidOrg = hr.SrcMid = srcMid + 2 + splitRight;
            if (!DecodeBytesCore(ref hr, revLen, revSym)) return -1;
        }
        return srcSize;
    }

    // ---- multi-array, recursive, RLE ----

    static uint BitMask(int n) => n >= 32 ? 0xffffffff : (2u << n) - 1;

    static int DecodeMultiArray(byte* src, byte* srcEnd, byte* dst, byte* dstEnd, byte** arrayData, int* arrayLens, int arrayCount,
        out int totalSizeOut, bool forceMemmove, byte* scratch, byte* scratchEnd)
    {
        byte* srcOrg = src; totalSizeOut = 0;
        if (srcEnd - src < 4) return -1;
        int decodedSize;
        int numArraysInFile = *src++;
        if ((numArraysInFile & 0x80) == 0) return -1;
        numArraysInFile &= 0x3f;
        if (dst == scratch)
        {
            scratch += (scratchEnd - scratch - 0xc000) >> 1;
            dstEnd = scratch;
        }
        int totalSize = 0;
        if (numArraysInFile == 0)
        {
            for (int i = 0; i < arrayCount; i++)
            {
                byte* chunkDst = dst;
                int dec = DecodeBytes(ref chunkDst, src, srcEnd, out decodedSize, dstEnd - dst, forceMemmove, scratch, scratchEnd);
                if (dec < 0) return -1;
                dst += decodedSize; arrayLens[i] = decodedSize; arrayData[i] = chunkDst;
                src += dec; totalSize += decodedSize;
            }
            totalSizeOut = totalSize;
            return (int)(src - srcOrg);
        }
        byte** entropyArrayData = stackalloc byte*[32];
        uint* entropyArraySize = stackalloc uint[32];
        byte* scratchCur = scratch;
        for (int i = 0; i < numArraysInFile; i++)
        {
            byte* chunkDst = scratchCur;
            int dec = DecodeBytes(ref chunkDst, src, srcEnd, out decodedSize, scratchEnd - scratchCur, forceMemmove, scratchCur, scratchEnd);
            if (dec < 0) return -1;
            entropyArrayData[i] = chunkDst; entropyArraySize[i] = (uint)decodedSize;
            scratchCur += decodedSize; totalSize += decodedSize; src += dec;
        }
        totalSizeOut = totalSize;
        if (srcEnd - src < 3) return -1;
        int Q = U16(src); src += 2;
        if (GetBlockSize(src, srcEnd, out int outSize, totalSize) < 0) return -1;
        int numIndexes = outSize;
        int numLens = numIndexes - arrayCount;
        if (numLens < 1) return -1;
        if (scratchEnd - scratchCur < numIndexes) return -1;
        byte* intervalLenlog2 = scratchCur; scratchCur += numIndexes;
        if (scratchEnd - scratchCur < numIndexes) return -1;
        byte* intervalIndexes = scratchCur; scratchCur += numIndexes;
        if ((Q & 0x8000) != 0)
        {
            int n = DecodeBytes(ref intervalIndexes, src, srcEnd, out int sizeOut, numIndexes, false, scratchCur, scratchEnd);
            if (n < 0 || sizeOut != numIndexes) return -1;
            src += n;
            for (int i = 0; i < numIndexes; i++)
            {
                int t = intervalIndexes[i];
                intervalLenlog2[i] = (byte)(t >> 4); intervalIndexes[i] = (byte)(t & 0xF);
            }
            numLens = numIndexes;
        }
        else
        {
            int lenlog2Chunksize = numIndexes - arrayCount;
            int n = DecodeBytes(ref intervalIndexes, src, srcEnd, out int sizeOut, numIndexes, false, scratchCur, scratchEnd);
            if (n < 0 || sizeOut != numIndexes) return -1;
            src += n;
            n = DecodeBytes(ref intervalLenlog2, src, srcEnd, out sizeOut, lenlog2Chunksize, false, scratchCur, scratchEnd);
            if (n < 0 || sizeOut != lenlog2Chunksize) return -1;
            src += n;
            for (int i = 0; i < lenlog2Chunksize; i++) if (intervalLenlog2[i] > 16) return -1;
        }
        if (scratchEnd - scratchCur < 4) return -1;
        scratchCur = (byte*)(((nuint)scratchCur + 3) & ~(nuint)3);
        if (scratchEnd - scratchCur < numLens * 4) return -1;
        uint* decodedIntervals = (uint*)scratchCur;
        int varbitsComplen = Q & 0x3FFF;
        if (srcEnd - src < varbitsComplen) return -1;
        byte* f = src; uint bitsF = 0; int bitposF = 24;
        byte* srcEndActual = src + varbitsComplen;
        byte* b = srcEndActual; uint bitsB = 0; int bitposB = 24;
        int ii;
        for (ii = 0; ii + 2 <= numLens; ii += 2)
        {
            bitsF |= Swap32(U32(f)) >> (24 - bitposF); f += (bitposF + 7) >> 3;
            bitsB |= ((uint*)b)[-1] >> (24 - bitposB); b -= (bitposB + 7) >> 3;
            int numbitsF = intervalLenlog2[ii + 0], numbitsB = intervalLenlog2[ii + 1];
            bitsF = BitOperations.RotateLeft(bitsF | 1, numbitsF); bitposF += numbitsF - 8 * ((bitposF + 7) >> 3);
            bitsB = BitOperations.RotateLeft(bitsB | 1, numbitsB); bitposB += numbitsB - 8 * ((bitposB + 7) >> 3);
            uint valueF = bitsF & BitMask(numbitsF); bitsF &= ~BitMask(numbitsF);
            uint valueB = bitsB & BitMask(numbitsB); bitsB &= ~BitMask(numbitsB);
            decodedIntervals[ii + 0] = valueF; decodedIntervals[ii + 1] = valueB;
        }
        if (ii < numLens)
        {
            bitsF |= Swap32(U32(f)) >> (24 - bitposF);
            int numbitsF = intervalLenlog2[ii];
            bitsF = BitOperations.RotateLeft(bitsF | 1, numbitsF);
            decodedIntervals[ii] = bitsF & BitMask(numbitsF);
        }
        if (intervalIndexes[numIndexes - 1] != 0) return -1;
        int indi = 0, leni = 0, source;
        int incrementLeni = (Q & 0x8000) != 0 ? 1 : 0;
        for (int arri = 0; arri < arrayCount; arri++)
        {
            arrayData[arri] = dst;
            if (indi >= numIndexes) return -1;
            while ((source = intervalIndexes[indi++]) != 0)
            {
                if (source > numArraysInFile) return -1;
                if (leni >= numLens) return -1;
                int curLen = (int)decodedIntervals[leni++];
                int bytesLeft = (int)entropyArraySize[source - 1];
                if (curLen > bytesLeft || curLen > dstEnd - dst) return -1;
                byte* blksrc = entropyArrayData[source - 1];
                entropyArraySize[source - 1] -= (uint)curLen;
                entropyArrayData[source - 1] += curLen;
                Buffer.MemoryCopy(blksrc, dst, curLen, curLen);
                dst += curLen;
            }
            leni += incrementLeni;
            arrayLens[arri] = (int)(dst - arrayData[arri]);
        }
        if (indi != numIndexes || leni != numLens) return -1;
        for (int i = 0; i < numArraysInFile; i++) if (entropyArraySize[i] != 0) return -1;
        return (int)(srcEndActual - srcOrg);
    }

    static int DecodeRecursive(byte* src, int srcSize, byte* output, int outputSize, byte* scratch, byte* scratchEnd)
    {
        byte* srcOrg = src, outputEnd = output + outputSize, srcEnd = src + srcSize;
        if (srcSize < 6) return -1;
        int n = src[0] & 0x7f;
        if (n < 2) return -1;
        if ((src[0] & 0x80) == 0)
        {
            src++;
            do
            {
                int dec = DecodeBytes(ref output, src, srcEnd, out int decodedSize, outputEnd - output, true, scratch, scratchEnd);
                if (dec < 0) return -1;
                output += decodedSize; src += dec;
            } while (--n != 0);
            if (output != outputEnd) return -1;
            return (int)(src - srcOrg);
        }
        else
        {
            byte* arrayData; int arrayLen;
            int dec = DecodeMultiArray(src, srcEnd, output, outputEnd, &arrayData, &arrayLen, 1, out int decodedSize, true, scratch, scratchEnd);
            if (dec < 0) return -1;
            output += decodedSize;
            if (output != outputEnd) return -1;
            return dec;
        }
    }

    static int DecodeRle(byte* src, int srcSize, byte* dst, int dstSize, byte* scratch, byte* scratchEnd)
    {
        if (srcSize <= 1)
        {
            if (srcSize != 1) return -1;
            new Span<byte>(dst, dstSize).Fill(src[0]);
            return 1;
        }
        byte* dstEnd = dst + dstSize;
        byte* cmdPtr = src + 1, cmdPtrEnd = src + srcSize;
        if (src[0] != 0)
        {
            byte* dstPtr = scratch;
            int n = DecodeBytes(ref dstPtr, src, src + srcSize, out int decSize, scratchEnd - scratch, true, scratch, scratchEnd);
            if (n <= 0) return -1;
            int cmdLen = srcSize - n + decSize;
            if (cmdLen > scratchEnd - scratch) return -1;
            Buffer.MemoryCopy(src + n, dstPtr + decSize, srcSize - n, srcSize - n);
            cmdPtr = dstPtr; cmdPtrEnd = &dstPtr[cmdLen];
        }
        byte rleByte = 0;
        while (cmdPtr < cmdPtrEnd)
        {
            uint cmd = cmdPtrEnd[-1];
            if (cmd - 1 >= 0x2f)
            {
                cmdPtrEnd--;
                uint bytesToCopy = (uint)(-1 - (int)cmd) & 0xF, bytesToRle = cmd >> 4;
                if (dstEnd - dst < bytesToCopy + bytesToRle || cmdPtrEnd - cmdPtr < bytesToCopy) return -1;
                Buffer.MemoryCopy(cmdPtr, dst, bytesToCopy, bytesToCopy);
                cmdPtr += bytesToCopy; dst += bytesToCopy;
                new Span<byte>(dst, (int)bytesToRle).Fill(rleByte); dst += bytesToRle;
            }
            else if (cmd >= 0x10)
            {
                uint data = (uint)U16(cmdPtrEnd - 2) - 4096;
                cmdPtrEnd -= 2;
                uint bytesToCopy = data & 0x3F, bytesToRle = data >> 6;
                if (dstEnd - dst < bytesToCopy + bytesToRle || cmdPtrEnd - cmdPtr < bytesToCopy) return -1;
                Buffer.MemoryCopy(cmdPtr, dst, bytesToCopy, bytesToCopy);
                cmdPtr += bytesToCopy; dst += bytesToCopy;
                new Span<byte>(dst, (int)bytesToRle).Fill(rleByte); dst += bytesToRle;
            }
            else if (cmd == 1)
            {
                rleByte = *cmdPtr++;
                cmdPtrEnd--;
            }
            else if (cmd >= 9)
            {
                uint bytesToRle = ((uint)U16(cmdPtrEnd - 2) - 0x8ff) * 128;
                cmdPtrEnd -= 2;
                if (dstEnd - dst < bytesToRle) return -1;
                new Span<byte>(dst, (int)bytesToRle).Fill(rleByte); dst += bytesToRle;
            }
            else
            {
                uint bytesToCopy = ((uint)U16(cmdPtrEnd - 2) - 511) * 64;
                cmdPtrEnd -= 2;
                if (cmdPtrEnd - cmdPtr < bytesToCopy || dstEnd - dst < bytesToCopy) return -1;
                Buffer.MemoryCopy(cmdPtr, dst, bytesToCopy, bytesToCopy);
                dst += bytesToCopy; cmdPtr += bytesToCopy;
            }
        }
        if (cmdPtrEnd != cmdPtr) return -1;
        if (dst != dstEnd) return -1;
        return srcSize;
    }

    // ---- tANS ----

    sealed class TansData { public int AUsed, BUsed; public byte[] A = new byte[256]; public uint[] B = new uint[256]; }

    static bool TansDecodeTable(ref BitReader bits, int LBits, TansData t)
    {
        Refill(ref bits);
        if (ReadBitNoRefill(ref bits) != 0)
        {
            int Q = ReadBitsNoRefill(ref bits, 3);
            int numSymbols = ReadBitsNoRefill(ref bits, 8) + 1;
            if (numSymbols < 2) return false;
            int fluff = ReadFluff(ref bits, numSymbols);
            int totalRiceValues = fluff + numSymbols;
            byte* rice = stackalloc byte[512 + 16 + 16];
            var br2 = new BitReader2
            {
                P = bits.P - (uint)((24 - bits.BitPos + 7) >> 3),
                PEnd = bits.PEnd,
                BitPos = (uint)((bits.BitPos - 24) & 7),
            };
            if (!DecodeGolombRiceLengths(rice, totalRiceValues, ref br2)) return false;
            new Span<byte>(rice + totalRiceValues, 16).Clear();
            bits.BitPos = 24; bits.P = br2.P; bits.Bits = 0;
            Refill(ref bits);
            bits.Bits <<= (int)br2.BitPos; bits.BitPos += (int)br2.BitPos;
            HuffRange* range = stackalloc HuffRange[133];
            fluff = ConvertToRanges(range, numSymbols, fluff, &rice[numSymbols], ref bits);
            if (fluff < 0) return false;
            Refill(ref bits);
            uint L = 1u << LBits;
            byte* curRice = rice;
            int average = 6, somesum = 0, ai = 0, bi = 0;
            for (int ri = 0; ri < fluff; ri++)
            {
                int symbol = range[ri].Symbol, num = range[ri].Num;
                do
                {
                    Refill(ref bits);
                    int nextra = Q + *curRice++;
                    if (nextra > 15) return false;
                    int v = ReadBitsNoRefillZero(ref bits, nextra) + (1 << nextra) - (1 << Q);
                    int averageDiv4 = average >> 2;
                    int limit = 2 * averageDiv4;
                    if (v <= limit) v = averageDiv4 + (-(v & 1) ^ (int)((uint)v >> 1));
                    if (limit > v) limit = v;
                    v += 1;
                    average += limit - averageDiv4;
                    t.A[ai] = (byte)symbol;
                    t.B[bi] = (uint)((symbol << 16) + v);
                    if (v == 1) ai++;
                    if (v >= 2) bi++;
                    somesum += v;
                    symbol += 1;
                } while (--num != 0);
            }
            t.AUsed = ai; t.BUsed = bi;
            return somesum == L;
        }
        else
        {
            var seen = new bool[256];
            uint L = 1u << LBits;
            int count = ReadBitsNoRefill(ref bits, 3) + 1;
            int bitsPerSym = Bsr((uint)LBits) + 1;
            int maxDeltaBits = ReadBitsNoRefill(ref bits, bitsPerSym);
            if (maxDeltaBits == 0 || maxDeltaBits > LBits) return false;
            int ai = 0, bi = 0, weight = 0, totalWeights = 0;
            do
            {
                Refill(ref bits);
                int sym = ReadBitsNoRefill(ref bits, 8);
                if (seen[sym]) return false;
                int delta = ReadBitsNoRefill(ref bits, maxDeltaBits);
                weight += delta;
                if (weight == 0) return false;
                seen[sym] = true;
                if (weight == 1) t.A[ai++] = (byte)sym;
                else t.B[bi++] = (uint)((sym << 16) + weight);
                totalWeights += weight;
            } while (--count != 0);
            Refill(ref bits);
            int lastSym = ReadBitsNoRefill(ref bits, 8);
            if (seen[lastSym]) return false;
            if (L - totalWeights < weight || L - totalWeights <= 1) return false;
            t.B[bi++] = (uint)((lastSym << 16) + (L - totalWeights));
            t.AUsed = ai; t.BUsed = bi;
            Array.Sort(t.A, 0, ai);
            Array.Sort(t.B, 0, bi);
            return true;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    struct TansLutEnt { public uint X; public byte BitsX; public byte Symbol; public ushort W; }

    static void TansInitLut(TansData t, int LBits, TansLutEnt* lut)
    {
        TansLutEnt** pointers = stackalloc TansLutEnt*[4];
        int L = 1 << LBits;
        int aUsed = t.AUsed;
        uint slotsLeft = (uint)(L - aUsed);
        uint sa = slotsLeft >> 2;
        pointers[0] = lut;
        uint sb = sa + ((slotsLeft & 3) > 0 ? 1u : 0);
        pointers[1] = lut + sb;
        sb += sa + ((slotsLeft & 3) > 1 ? 1u : 0);
        pointers[2] = lut + sb;
        sb += sa + ((slotsLeft & 3) > 2 ? 1u : 0);
        pointers[3] = lut + sb;
        {
            TansLutEnt* singles = lut + slotsLeft;
            var le = new TansLutEnt { W = 0, BitsX = (byte)LBits, X = (1u << LBits) - 1 };
            for (int i = 0; i < aUsed; i++) { singles[i] = le; singles[i].Symbol = t.A[i]; }
        }
        int weightsSum = 0;
        for (int i = 0; i < t.BUsed; i++)
        {
            int weight = (int)(t.B[i] & 0xffff);
            int symbol = (int)(t.B[i] >> 16);
            if (weight > 4)
            {
                int symBits = Bsr((uint)weight);
                int Z = LBits - symBits;
                var le = new TansLutEnt { Symbol = (byte)symbol, BitsX = (byte)Z, X = (1u << Z) - 1, W = (ushort)((L - 1) & (weight << Z)) };
                int whatToAdd = 1 << Z;
                int X = (1 << (symBits + 1)) - weight;
                for (int j = 0; j < 4; j++)
                {
                    TansLutEnt* dst = pointers[j];
                    int Y = (weight + ((weightsSum - j - 1) & 3)) >> 2;
                    if (X >= Y)
                    {
                        for (int n = Y; n != 0; n--) { *dst++ = le; le.W = (ushort)(le.W + whatToAdd); }
                        X -= Y;
                    }
                    else
                    {
                        for (int n = X; n != 0; n--) { *dst++ = le; le.W = (ushort)(le.W + whatToAdd); }
                        Z--;
                        whatToAdd >>= 1;
                        le.BitsX = (byte)Z; le.W = 0; le.X >>= 1;
                        for (int n = Y - X; n != 0; n--) { *dst++ = le; le.W = (ushort)(le.W + whatToAdd); }
                        X = weight;
                    }
                    pointers[j] = dst;
                }
            }
            else
            {
                uint bits = (uint)(((1 << weight) - 1) << (weightsSum & 3));
                bits |= bits >> 4;
                int n = weight, ww = weight;
                do
                {
                    int idx = Bsf(bits);
                    bits &= bits - 1;
                    TansLutEnt* dst = pointers[idx]++;
                    dst->Symbol = (byte)symbol;
                    int weightBits = Bsr((uint)ww);
                    dst->BitsX = (byte)(LBits - weightBits);
                    dst->X = (1u << (LBits - weightBits)) - 1;
                    dst->W = (ushort)((L - 1) & (ww++ << (LBits - weightBits)));
                } while (--n != 0);
            }
            weightsSum += weight;
        }
    }

    static int DecodeTans(byte* src, int srcSize, byte* dst, int dstSize, byte* scratch, byte* scratchEnd)
    {
        if (srcSize < 8 || dstSize < 5) return -1;
        byte* srcEnd = src + srcSize;
        var br = new BitReader { BitPos = 24, Bits = 0, P = src, PEnd = srcEnd };
        Refill(ref br);
        if (ReadBitNoRefill(ref br) != 0) return -1;
        int LBits = ReadBitsNoRefill(ref br, 2) + 8;
        var tans = new TansData();
        if (!TansDecodeTable(ref br, LBits, tans)) return -1;
        src = br.P - (24 - br.BitPos) / 8;
        if (src >= srcEnd) return -1;
        uint lutSpace = (uint)((sizeof(TansLutEnt) << LBits) + 15) & ~15u;
        if (lutSpace > scratchEnd - scratch) return -1;
        byte* dstEnd = dst + dstSize - 5;
        var lut = (TansLutEnt*)(((nuint)scratch + 15) & ~(nuint)15);
        TansInitLut(tans, LBits, lut);

        uint LMask = (1u << LBits) - 1;
        uint bitsF = U32(src); src += 4;
        uint bitsB = Swap32(U32(srcEnd - 4)); srcEnd -= 4;
        int bitposF = 32, bitposB = 32;
        uint s0 = bitsF & LMask, s1 = bitsB & LMask;
        bitsF >>= LBits; bitposF -= LBits; bitsB >>= LBits; bitposB -= LBits;
        uint s2 = bitsF & LMask, s3 = bitsB & LMask;
        bitsF >>= LBits; bitposF -= LBits; bitsB >>= LBits; bitposB -= LBits;
        bitsF |= U32(src) << bitposF; src += (31 - bitposF) >> 3; bitposF |= 24;
        uint s4 = bitsF & LMask;
        bitsF >>= LBits; bitposF -= LBits;

        byte* ptrF = src - (bitposF >> 3); bitposF &= 7;
        byte* ptrB = srcEnd + (bitposB >> 3); bitposB &= 7;

        if (ptrF > ptrB) return -1;
        TansLutEnt* e;
        if (dst < dstEnd)
        {
            for (; ; )
            {
                bitsF |= U32(ptrF) << bitposF; ptrF += (31 - bitposF) >> 3; bitposF |= 24;
                e = &lut[s0]; *dst++ = e->Symbol; bitposF -= e->BitsX; s0 = (bitsF & e->X) + e->W; bitsF >>= e->BitsX; if (dst >= dstEnd) break;
                e = &lut[s1]; *dst++ = e->Symbol; bitposF -= e->BitsX; s1 = (bitsF & e->X) + e->W; bitsF >>= e->BitsX; if (dst >= dstEnd) break;
                bitsF |= U32(ptrF) << bitposF; ptrF += (31 - bitposF) >> 3; bitposF |= 24;
                e = &lut[s2]; *dst++ = e->Symbol; bitposF -= e->BitsX; s2 = (bitsF & e->X) + e->W; bitsF >>= e->BitsX; if (dst >= dstEnd) break;
                e = &lut[s3]; *dst++ = e->Symbol; bitposF -= e->BitsX; s3 = (bitsF & e->X) + e->W; bitsF >>= e->BitsX; if (dst >= dstEnd) break;
                bitsF |= U32(ptrF) << bitposF; ptrF += (31 - bitposF) >> 3; bitposF |= 24;
                e = &lut[s4]; *dst++ = e->Symbol; bitposF -= e->BitsX; s4 = (bitsF & e->X) + e->W; bitsF >>= e->BitsX; if (dst >= dstEnd) break;
                bitsB |= Swap32(((uint*)ptrB)[-1]) << bitposB; ptrB -= (31 - bitposB) >> 3; bitposB |= 24;
                e = &lut[s0]; *dst++ = e->Symbol; bitposB -= e->BitsX; s0 = (bitsB & e->X) + e->W; bitsB >>= e->BitsX; if (dst >= dstEnd) break;
                e = &lut[s1]; *dst++ = e->Symbol; bitposB -= e->BitsX; s1 = (bitsB & e->X) + e->W; bitsB >>= e->BitsX; if (dst >= dstEnd) break;
                bitsB |= Swap32(((uint*)ptrB)[-1]) << bitposB; ptrB -= (31 - bitposB) >> 3; bitposB |= 24;
                e = &lut[s2]; *dst++ = e->Symbol; bitposB -= e->BitsX; s2 = (bitsB & e->X) + e->W; bitsB >>= e->BitsX; if (dst >= dstEnd) break;
                e = &lut[s3]; *dst++ = e->Symbol; bitposB -= e->BitsX; s3 = (bitsB & e->X) + e->W; bitsB >>= e->BitsX; if (dst >= dstEnd) break;
                bitsB |= Swap32(((uint*)ptrB)[-1]) << bitposB; ptrB -= (31 - bitposB) >> 3; bitposB |= 24;
                e = &lut[s4]; *dst++ = e->Symbol; bitposB -= e->BitsX; s4 = (bitsB & e->X) + e->W; bitsB >>= e->BitsX; if (dst >= dstEnd) break;
            }
        }
        if (ptrB - ptrF + (bitposF >> 3) + (bitposB >> 3) != 0) return -1;
        if (((s0 | s1 | s2 | s3 | s4) & ~0xFFu) != 0) return -1;
        dstEnd[0] = (byte)s0; dstEnd[1] = (byte)s1; dstEnd[2] = (byte)s2; dstEnd[3] = (byte)s3; dstEnd[4] = (byte)s4;
        return srcSize;
    }

    // ---- LZ ----

    struct LzTable
    {
        public byte* CmdStream; public int CmdStreamSize;
        public int* OffsStream; public int OffsStreamSize;
        public byte* LitStream; public int LitStreamSize;
        public int* LenStream; public int LenStreamSize;
    }

    static bool UnpackOffsets(byte* src, byte* srcEnd, byte* packedOffsStream, byte* packedOffsStreamExtra, int packedOffsStreamSize,
        int multiDistScale, byte* packedLitlenStream, int packedLitlenStreamSize, int* offsStream, int* lenStream)
    {
        int n, i, u32LenStreamSize = 0;
        var a = new BitReader { BitPos = 24, Bits = 0, P = src, PEnd = srcEnd };
        Refill(ref a);
        var b = new BitReader { BitPos = 24, Bits = 0, P = srcEnd, PEnd = src };
        RefillBackwards(ref b);

        if (b.Bits < 0x2000) return false;
        n = 31 - Bsr(b.Bits);
        b.BitPos += n; b.Bits <<= n;
        RefillBackwards(ref b);
        n++;
        u32LenStreamSize = (int)(b.Bits >> (32 - n)) - 1;
        b.BitPos += n; b.Bits <<= n;
        RefillBackwards(ref b);

        byte* packedEnd = packedOffsStream + packedOffsStreamSize;
        if (multiDistScale == 0)
        {
            while (packedOffsStream != packedEnd)
            {
                *offsStream++ = -(int)ReadDistance(ref a, *packedOffsStream++, false);
                if (packedOffsStream == packedEnd) break;
                *offsStream++ = -(int)ReadDistance(ref b, *packedOffsStream++, true);
            }
        }
        else
        {
            int* offsOrg = offsStream;
            uint cmd, offs;
            while (packedOffsStream != packedEnd)
            {
                cmd = *packedOffsStream++;
                if ((cmd >> 3) > 26) return false;
                offs = ((8 + (cmd & 7)) << (int)(cmd >> 3)) | ReadMoreThan24Bits(ref a, (int)(cmd >> 3));
                *offsStream++ = 8 - (int)offs;
                if (packedOffsStream == packedEnd) break;
                cmd = *packedOffsStream++;
                if ((cmd >> 3) > 26) return false;
                offs = ((8 + (cmd & 7)) << (int)(cmd >> 3)) | ReadMoreThan24BitsB(ref b, (int)(cmd >> 3));
                *offsStream++ = 8 - (int)offs;
            }
            if (multiDistScale != 1)
            {
                long count = offsStream - offsOrg;
                for (long k = 0; k != count; k++) offsOrg[k] = multiDistScale * offsOrg[k] - packedOffsStreamExtra[k];
            }
        }
        if (u32LenStreamSize > 512) return false;
        uint* u32 = stackalloc uint[512];
        for (i = 0; i + 1 < u32LenStreamSize; i += 2)
        {
            if (!ReadLength(ref a, out u32[i + 0], false)) return false;
            if (!ReadLength(ref b, out u32[i + 1], true)) return false;
        }
        if (i < u32LenStreamSize && !ReadLength(ref a, out u32[i], false)) return false;

        a.P -= (24 - a.BitPos) >> 3;
        b.P += (24 - b.BitPos) >> 3;
        if (a.P != b.P) return false;

        uint* u = u32, uEnd = u32 + u32LenStreamSize;
        for (i = 0; i < packedLitlenStreamSize; i++)
        {
            uint v = packedLitlenStream[i];
            if (v == 255) v = *u++ + 255;
            lenStream[i] = (int)v + 3;
        }
        return u == uEnd;
    }

    static byte* Align16(byte* p) => (byte*)(((nuint)p + 15) & ~(nuint)15);

    static bool ReadLzTable(int mode, byte* src, byte* srcEnd, byte* dst, int dstSize, long offset, byte* scratch, byte* scratchEnd, ref LzTable lz)
    {
        int decodeCount, n;
        if (mode > 1) return false;
        if (srcEnd - src < 13) return false;
        if (offset == 0) { *(ulong*)dst = *(ulong*)src; dst += 8; src += 8; }
        if ((*src & 0x80) != 0) return false; // excess bytes not supported (as in ooz)
        bool forceCopy = dst <= srcEnd && src <= dst + dstSize;

        byte* o = scratch;
        n = DecodeBytes(ref o, src, srcEnd, out decodeCount, Math.Min(scratchEnd - scratch, dstSize), forceCopy, scratch, scratchEnd);
        if (n < 0) return false;
        src += n; lz.LitStream = o; lz.LitStreamSize = decodeCount; scratch += decodeCount;

        o = scratch;
        n = DecodeBytes(ref o, src, srcEnd, out decodeCount, Math.Min(scratchEnd - scratch, dstSize), forceCopy, scratch, scratchEnd);
        if (n < 0) return false;
        src += n; lz.CmdStream = o; lz.CmdStreamSize = decodeCount; scratch += decodeCount;

        if (srcEnd - src < 3) return false;
        int offsScaling = 0;
        byte* packedOffsStreamExtra = null, packedOffsStream;
        if ((src[0] & 0x80) != 0)
        {
            offsScaling = src[0] - 127;
            src++;
            packedOffsStream = scratch;
            n = DecodeBytes(ref packedOffsStream, src, srcEnd, out lz.OffsStreamSize, Math.Min(scratchEnd - scratch, lz.CmdStreamSize), false, scratch, scratchEnd);
            if (n < 0) return false;
            src += n; scratch += lz.OffsStreamSize;
            if (offsScaling != 1)
            {
                packedOffsStreamExtra = scratch;
                n = DecodeBytes(ref packedOffsStreamExtra, src, srcEnd, out decodeCount, Math.Min(scratchEnd - scratch, lz.OffsStreamSize), false, scratch, scratchEnd);
                if (n < 0 || decodeCount != lz.OffsStreamSize) return false;
                src += n; scratch += decodeCount;
            }
        }
        else
        {
            packedOffsStream = scratch;
            n = DecodeBytes(ref packedOffsStream, src, srcEnd, out lz.OffsStreamSize, Math.Min(scratchEnd - scratch, lz.CmdStreamSize), false, scratch, scratchEnd);
            if (n < 0) return false;
            src += n; scratch += lz.OffsStreamSize;
        }

        byte* packedLenStream = scratch;
        n = DecodeBytes(ref packedLenStream, src, srcEnd, out lz.LenStreamSize, Math.Min(scratchEnd - scratch, dstSize >> 2), false, scratch, scratchEnd);
        if (n < 0) return false;
        src += n; scratch += lz.LenStreamSize;

        scratch = Align16(scratch);
        lz.OffsStream = (int*)scratch; scratch += lz.OffsStreamSize * 4;
        scratch = Align16(scratch);
        lz.LenStream = (int*)scratch; scratch += lz.LenStreamSize * 4;
        if (scratch + 64 > scratchEnd) return false;

        return UnpackOffsets(src, srcEnd, packedOffsStream, packedOffsStreamExtra, lz.OffsStreamSize, offsScaling,
            packedLenStream, lz.LenStreamSize, lz.OffsStream, lz.LenStream);
    }

    static void Copy64(byte* d, byte* s) => *(ulong*)d = *(ulong*)s;

    static void Copy64Add(byte* d, byte* s, byte* t)
    {
        ulong a = *(ulong*)s, b = *(ulong*)t;
        *(ulong*)d = ((a & 0x7f7f7f7f7f7f7f7ful) + (b & 0x7f7f7f7f7f7f7f7ful)) ^ ((a ^ b) & 0x8080808080808080ul);
    }

    static bool ProcessLzRuns(int mode, byte* dst, byte* dstEnd, byte* dstStart, ref LzTable lz)
    {
        byte* cmd = lz.CmdStream, cmdEnd = cmd + lz.CmdStreamSize;
        int* len = lz.LenStream, lenEnd = len + lz.LenStreamSize;
        byte* lit = lz.LitStream, litEnd = lit + lz.LitStreamSize;
        int* offs = lz.OffsStream, offsEnd = offs + lz.OffsStreamSize;
        int* recent = stackalloc int[7];
        recent[3] = -8; recent[4] = -8; recent[5] = -8;
        int lastOffset = -8;
        bool delta = mode == 0;

        while (cmd < cmdEnd)
        {
            uint f = *cmd++;
            uint litlen = f & 3, offsIndex = f >> 6, matchlen = (f >> 2) & 0xF;
            uint nextLong = (uint)*len;
            if (litlen == 3) { len++; litlen = nextLong; }
            recent[6] = *offs;

            if (delta)
            {
                Copy64Add(dst, lit, &dst[lastOffset]);
                if (litlen > 8)
                {
                    Copy64Add(dst + 8, lit + 8, &dst[lastOffset + 8]);
                    if (litlen > 16)
                    {
                        Copy64Add(dst + 16, lit + 16, &dst[lastOffset + 16]);
                        while (litlen > 24) { Copy64Add(dst + 24, lit + 24, &dst[lastOffset + 24]); litlen -= 8; dst += 8; lit += 8; }
                    }
                }
            }
            else
            {
                Copy64(dst, lit);
                if (litlen > 8)
                {
                    Copy64(dst + 8, lit + 8);
                    if (litlen > 16)
                    {
                        Copy64(dst + 16, lit + 16);
                        while (litlen > 24) { Copy64(dst + 24, lit + 24); litlen -= 8; dst += 8; lit += 8; }
                    }
                }
            }
            dst += litlen; lit += litlen;

            int offset = recent[offsIndex + 3];
            recent[offsIndex + 3] = recent[offsIndex + 2];
            recent[offsIndex + 2] = recent[offsIndex + 1];
            recent[offsIndex + 1] = recent[offsIndex + 0];
            recent[3] = offset;
            lastOffset = offset;
            offs = (int*)((byte*)offs + ((offsIndex + 1) & 4));

            if ((nuint)(nint)offset < (nuint)(nint)(dstStart - dst)) return false;
            byte* copyfrom = dst + offset;
            if (matchlen != 15)
            {
                Copy64(dst, copyfrom); Copy64(dst + 8, copyfrom + 8);
                dst += matchlen + 2;
            }
            else
            {
                matchlen = (uint)(14 + *len++);
                if (matchlen > (ulong)(dstEnd - dst)) return false;
                Copy64(dst, copyfrom); Copy64(dst + 8, copyfrom + 8); Copy64(dst + 16, copyfrom + 16);
                do { Copy64(dst + 24, copyfrom + 24); matchlen -= 8; dst += 8; copyfrom += 8; } while (matchlen > 24);
                dst += matchlen;
            }
        }
        if (offs != offsEnd || len != lenEnd) return false;
        long finalLen = dstEnd - dst;
        if (finalLen != litEnd - lit) return false;
        if (delta) { while (finalLen-- > 0) { *dst = (byte)(*lit++ + dst[lastOffset]); dst++; } }
        else Buffer.MemoryCopy(lit, dst, finalLen, finalLen);
        return true;
    }

    static int DecodeQuantum(byte* dst, byte* dstEnd, byte* dstStart, byte* src, byte* srcEnd, byte* scratch, byte* scratchEnd)
    {
        byte* srcIn = src;
        while (dstEnd - dst != 0)
        {
            int dstCount = (int)Math.Min(dstEnd - dst, 0x20000);
            if (srcEnd - src < 4) return -1;
            int chunkhdr = src[2] | src[1] << 8 | src[0] << 16;
            int srcUsed;
            if ((chunkhdr & 0x800000) == 0)
            {
                byte* o = dst;
                srcUsed = DecodeBytes(ref o, src, srcEnd, out int written, dstCount, false, scratch, scratchEnd);
                if (srcUsed < 0 || written != dstCount) return -1;
                if (o != dst) Buffer.MemoryCopy(o, dst, dstCount, dstCount);
            }
            else
            {
                src += 3;
                srcUsed = chunkhdr & 0x7FFFF;
                int mode = (chunkhdr >> 19) & 0xF;
                if (srcEnd - src < srcUsed) return -1;
                if (srcUsed < dstCount)
                {
                    long scratchUsage = Math.Min(Math.Min(3 * dstCount + 32 + 0xd000, 0x6C000), scratchEnd - scratch);
                    var lz = new LzTable();
                    if (!ReadLzTable(mode, src, src + srcUsed, dst, dstCount, dst - dstStart, scratch + 64, scratch + scratchUsage, ref lz)) return -1;
                    long off = dst - dstStart;
                    if (!ProcessLzRuns(mode, dst + (off == 0 ? 8 : 0), dst + dstCount, dst - off, ref lz)) return -1;
                }
                else if (srcUsed > dstCount || mode != 0) return -1;
                else Buffer.MemoryCopy(src, dst, dstCount, dstCount);
            }
            src += srcUsed;
            dst += dstCount;
        }
        return (int)(src - srcIn);
    }
}
