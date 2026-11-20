# FFRando.Kraken

The Oodle Kraken decompressor used by the FF Randomizer to read the game files of Final Fantasy VII Remake Intergrade and Rebirth. It is a C# port of the Kraken decoder from [ooz](https://github.com/powzix/ooz) (Copyright (C) 2016 Powzix).

It is licensed under the **GNU General Public License v3**, or (at your option) any later version. The full text is in [`COPYING`](COPYING).

FFRando.exe carries this program as a resource. It unpacks it to `%LOCALAPPDATA%\FFRando\helper\FFRando.Kraken-<hash>.exe`, starts it hidden, and talks to it over stdin/stdout. The request/reply format is described at the top of [`Program.cs`](Program.cs).

## Build it

You need the [.NET 10 SDK](https://dotnet.microsoft.com/download).

```
dotnet publish FFRando.Kraken.csproj -c Release -o out
```

This gives `out\FFRando.Kraken.exe`, the same self-contained single file that ships inside FFRando.exe.
