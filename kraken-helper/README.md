# FF7Rando.Kraken

The Oodle Kraken decompressor used by the FF7 Randomizer to read the game files of Final Fantasy VII Remake Intergrade and Rebirth. It is a C# port of the Kraken decoder from [ooz](https://github.com/powzix/ooz) (Copyright (C) 2016 Powzix).

It is licensed under the **GNU General Public License v3**, or (at your option) any later version. The full text is in [`COPYING`](COPYING).

FF7Rando.exe carries this program as a resource. It unpacks it to `%LOCALAPPDATA%\FF7Rando\helper\FF7Rando.Kraken-<hash>.exe`, starts it hidden, and talks to it over stdin/stdout. The request/reply format is described at the top of [`Program.cs`](Program.cs).

## Build it

You need the [.NET 10 SDK](https://dotnet.microsoft.com/download).

```
dotnet publish FF7Rando.Kraken.csproj -c Release -o out
```

This gives `out\FF7Rando.Kraken.exe`, the same self-contained single file that ships inside FF7Rando.exe.
