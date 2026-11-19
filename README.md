# FF7 Randomizer

A randomizer for **Final Fantasy VII** on PC (Steam): the original game (both the current Steam release and the 2013 one), **Remake Intergrade** and **Rebirth**. Pick a seed, tick what to shuffle, press **Randomize**, and play.

- **No mod loader.** No 7th Heaven, FFNx, Reloaded-II or anything else to install. It changes the game itself.
- **Your originals are kept.** **Put originals back** undoes everything, whenever you like.
- **Seeds.** The same seed and options always give the same game, so you can race a friend on the same shuffle.
- **Spoiler log.** Every randomize writes one, so you can look up where something went.

## Download

**[⬇ Latest release](https://github.com/Ozmao/ff7rando-releases/releases/latest)**: download `FF7Rando.exe`.

- It's a single file with nothing to install, for 64-bit Windows 10 or 11. Keep it somewhere you can write to, such as your Desktop or Documents. Updates can't be installed inside Program Files.
- **It updates itself.** When a new version is out, a banner at the top of the window offers it. One click downloads it, checks it against the checksum GitHub has for it, and restarts.
- **Version numbers** read 1.*games supported*.*features released*: 1.4.60 supports 4 games and has 60 options.
- The exe isn't code-signed, so Windows SmartScreen may warn you the first time. Click **More info → Run anyway**.

## How to use it

1. **Close the game.**
2. Pick the game in the **Game** list. Steam installs are found automatically. If yours isn't, use **Browse…** to point at the game folder.
3. Type a seed or press **New seed**, then pick your options. Each game has its own set.
4. Press **Randomize**.
5. Start the game. In the original FF7, start a **new game**: starting materia, items and gear only apply to a new game. Drops, chests, shops and the rest apply everywhere.
6. To go back to the normal game, press **Put originals back**. Do this before using Steam's *Verify integrity of game files*.

Every randomize starts from the original files, so seeds never stack. The spoiler log is saved in an `FF7Rando` folder inside the game's folder.

## What it can shuffle

### Final Fantasy VII (Steam release and 2013)

Enemy drops, steals and morphs · items in chests and materia lying around · shops and shop prices · random battles (at a similar level, or anywhere) · enemy HP and stats · spells between magic materia · summons between summon materia · materia slots on gear · starting materia, items and gear · stat growth · the order limit breaks are learned in (or, experimentally, limits between characters).

On the new Steam release, chests and random battles are only shuffled when playing in English.

### Remake Intergrade

On by default: enemy drops and steals · Shinra boxes · chests · shop prices.

Off by default:

- Shop stock, enemy EXP, gil and AP, and enemy HP and stats.
- Elemental weaknesses. The enemy book and Assess show the new ones.
- Chadley's materia.
- Armor and accessory effects.
- Side quest, Colosseum and other rewards.
- Starting armor and materia.
- Spells between magic materia, and the effects of command, support and independent materia.
- Weapon abilities and weapon upgrade skills.
- Stat growth between Cloud, Barret, Tifa and Aerith.
- **⚠ Risky:** enemy groups between fights, Fort Condor opponents and music.

Remake isn't changed at all: everything goes into one mod file (`End\Content\Paks\~mods\FF7Rando_P.pak`), and **Put originals back** deletes it.

### Rebirth

On by default: enemy drops and steals · crates and field drop spots · chests · shop prices.

Off by default:

- Shop stock, enemy EXP, gil and AP, and enemy HP and stats.
- Gathering points and crafting recipes.
- Elemental weaknesses.
- Chadley's materia.
- Armor and accessory effects.
- Side quest, Colosseum and minigame rewards.
- Starting gear.
- Spells between magic materia, and the other materia kinds.
- Weapon abilities.
- **⚠ Risky:** roaming enemies, Queen's Blood decks, field music and Folio skill trees.

Key items, summons and other things the story needs never move.

### Crisis Core -Final Fantasy VII- Reunion

In the game list, but **not implemented yet**. It's planned for a later update.

### About the "⚠ Risky" options

The options in the **⚠ Risky: may break the game** section change things the game may rely on. They could soft-lock a quest, crash a scene or make an area far too hard. Keep a save from before you turn them on, and turn them off again if something goes wrong.

## This is an early version

Not everything has been tried in game yet:

- **Confirmed in game:** for the original FF7, drops, morphs, starting gear, materia slots, chests and shops.
- **Not yet confirmed:** everything else, including all Remake and Rebirth options.

If something breaks, press **Put originals back**, and please report it on the [issues page](https://github.com/Ozmao/ff7rando-releases/issues) with your seed and the options you picked.

## Licenses

FF7Rando.exe contains **FF7Rando.Kraken**, a small separate program that decompresses Remake's and Rebirth's game files. It is based on [ooz](https://github.com/powzix/ooz) and licensed under the **GNU GPL v3**. Its full source and the license are in [`kraken-helper/`](kraken-helper). The app unpacks it to `%LOCALAPPDATA%\FF7Rando\helper` and runs it in the background while it reads the game.

FINAL FANTASY is a registered trademark of Square Enix Holdings Co., Ltd. This is a fan project and isn't affiliated with or endorsed by Square Enix. It ships no game data: it only changes the copy of the game you own.
