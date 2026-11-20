// FFRando.Kraken: decompresses Oodle Kraken blocks for the FF Randomizer.
// Copyright (C) 2026 Ozmao. Based on ooz, Copyright (C) 2016 Powzix.
//
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU General
// Public License as published by the Free Software Foundation, either version 3 of the License, or (at your
// option) any later version. This program is distributed in the hope that it will be useful, but WITHOUT ANY
// WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License (COPYING) for more details.
//
// A program of its own: the randomizer starts it and talks to it over stdin/stdout.
// Request:  int32 compressed length, int32 decompressed length, then the compressed bytes (little-endian).
// Reply:    int32 status (0 = ok, then the decompressed bytes; 1 = bad data, then a UTF-8 message:
//           int32 length + bytes).
// It runs until stdin closes. Run it with no input redirected and it just prints what it is.

using System.Buffers.Binary;
using System.Text;
using FFRando.KrakenHelper;

if (!Console.IsInputRedirected)
{
    Console.WriteLine("FFRando.Kraken: Oodle Kraken decompressor used by the FF Randomizer (GPL-3.0, based on ooz).");
    return 0;
}

using var input = Console.OpenStandardInput();
using var output = new BufferedStream(Console.OpenStandardOutput(), 1 << 16);
var header = new byte[8];
while (true)
{
    if (!ReadAll(input, header)) return 0; // stdin closed: the randomizer is done
    int srcLen = BinaryPrimitives.ReadInt32LittleEndian(header);
    int dstLen = BinaryPrimitives.ReadInt32LittleEndian(header.AsSpan(4));
    if (srcLen < 0 || dstLen < 0 || srcLen > 1 << 28 || dstLen > 1 << 28) return 2;
    var src = new byte[srcLen];
    if (!ReadAll(input, src)) return 2;
    byte[] reply;
    int status;
    try { reply = Kraken.Decompress(src, dstLen); status = 0; }
    catch (Exception e) { reply = Encoding.UTF8.GetBytes(e.Message); status = 1; }
    Span<byte> word = stackalloc byte[4];
    BinaryPrimitives.WriteInt32LittleEndian(word, status);
    output.Write(word);
    if (status != 0)
    {
        BinaryPrimitives.WriteInt32LittleEndian(word, reply.Length);
        output.Write(word);
    }
    output.Write(reply);
    output.Flush();
}

static bool ReadAll(Stream s, byte[] buffer)
{
    int got = 0;
    while (got < buffer.Length)
    {
        int n = s.Read(buffer, got, buffer.Length - got);
        if (n == 0)
        {
            if (got == 0) return false;
            throw new EndOfStreamException();
        }
        got += n;
    }
    return true;
}
