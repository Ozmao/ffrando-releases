# FF Randomizer

A randomizer for **Final Fantasy** on PC (Steam):

- **Final Fantasy I, II, III, IV, V and VI**: the Pixel Remasters.
- **Final Fantasy VII**: the original game (both the current Steam release and the 2013 one), **Remake Intergrade** and **Rebirth**.
- **Final Fantasy IX**, with or without the Memoria mod engine.

Pick a seed, tick what to shuffle, press **Randomize**, and play. It was called FF7Rando until version 1.5.72.

- **No mod loader needed.** No 7th Heaven, FFNx, Reloaded-II or anything else to install for FF7 or the Pixel Remasters. It changes the game itself. For FF9 it works on the plain game, and does even more if you have Memoria.
- **Your originals are kept.** **Put originals back** undoes everything, whenever you like.
- **Seeds.** The same seed and options always give the same game, so you can race a friend on the same shuffle.
- **Races.** For every game: join a race with an invite link or host one yourself, with a live leaderboard.
- **Spoiler log.** Every randomize writes one, and the app shows it in its own window, so you can look up where something went.

## Download

**[⬇ Latest release](https://github.com/Ozmao/ffrando-releases/releases/latest)**: download `FFRando.exe`.

- It's a single file with nothing to install, for 64-bit Windows 10 or 11. Keep it somewhere you can write to, such as your Desktop or Documents. Updates can't be installed inside Program Files.
- **It updates itself.** When a new version is out, a banner at the top of the window offers it. One click downloads it, checks it against the checksum GitHub has for it, and restarts. FF7Rando 1.4.60 updates to FFRando by itself.
- **Version numbers** read 1.*games supported*.*features released*: 1.11.80 supports 11 games and has 80 features.
- The exe isn't code-signed, so Windows SmartScreen may warn you the first time. Click **More info → Run anyway**.

## How to use it

1. **Close the game.**
2. Pick the game in the **Game** list. Steam installs are found automatically. If yours isn't, use **Browse…** to point at the game folder.
3. Type a seed or press **New seed**, then pick your options. Each game has its own set.
4. Press **Randomize**.
5. Start the game. In the original FF7, FF9 and the Pixel Remasters, start a **new game**: starting materia, items and gear only apply to a new game, FF9's Chaos Seed randomizes when a new game starts, and chests already opened in a save stay opened. Drops, chests, shops and the rest of FF7's options apply everywhere.
6. To go back to the normal game, press **Put originals back**. Do this before using Steam's *Verify integrity of game files*.

Every randomize starts from the original files, so seeds never stack. Backups and spoiler logs are saved in an `FFRando` folder inside the game's folder.

## What it can shuffle

### Final Fantasy I to VI (Pixel Remaster, beta)

All six share most options:

- **On by default:** treasure chests, shop stock, and enemy drops and steals. Each becomes something of a similar price: items stay items, and weapons and armor stay gear.
- **Off by default:**
  - shop prices (half to twice as much), and weapon and armor stats;
  - enemy HP and stats, boss HP and stats, enemy weaknesses and immunities, and enemy EXP and gil;
  - character stats and growth, faster leveling (not FF2), spell power and MP cost, and music.
- **For some games only:** spells learned by level (FF4, FF6), job abilities (FF5), esper spells, Mog's dances and Gau's Rages (FF6).
- **⚠ Risky:** random encounters (each swapped for a battle of a similar level), who can equip what, and job commands (FF3, FF5).

Key items, gil, spells, summons (FF4's and FF5's summon items, FF3's spell items, FF2's tomes) and boss fights never move. FFRando rewrites the game's own data files and keeps the originals in the `FFRando` folder. Steam's *Verify integrity* or an update also puts the originals back.

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

Remake isn't changed at all: everything goes into one mod file (`End\Content\Paks\~mods\FFRando_P.pak`), and **Put originals back** deletes it.

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

### Final Fantasy IX

How FF9 gets randomized depends on whether **[Memoria](https://github.com/Albeoris/Memoria)** (FF9's mod engine) is installed. The card at the top of the FF9 panel says which way your install goes.

- **With Memoria:** FFRando installs the **Chaos Seed** mod and switches it on in Memoria's mod list, alongside your other Memoria mods. Every option works, and Chaos Seed randomizes inside the game when you start a new game. The Memoria launcher shows the same choices.
- **Plain game (beta):** without Memoria, FFRando writes the same shuffles straight into the game's files. Options it can't do this way are marked **needs Memoria** in the window and greyed out.

| Option | Plain game | With Memoria |
| --- | --- | --- |
| Equipment abilities | yes | yes |
| Shop stock and synthesis shops | yes | yes |
| Steal items | yes | yes |
| Treasure chests and finds | yes | yes |
| Tetra Master cards | yes | yes |
| ⚠ Synthesis recipes | yes | yes |
| ⚠ Random encounters | needs Memoria | yes |
| ⚠ Key items | needs Memoria | yes |
| Cutscene skipper (doesn't change the seed) | needs Memoria | yes |

### Races

Races work for every game in the list. Open the **Race** card in the left column, under **Seed**.

- **Host a race:**
  - Pick the game, the seed and the options, then click **Start hosting**. FFRando runs the race server for as long as it stays open.
  - Send racers the invite link. It's a Cloudflare link, so your IP isn't shown and you don't need port forwarding. A new link can take a minute before it works.
  - FFRando uses cloudflared if it's already on your PC, or downloads it once from Cloudflare. Either way, it only runs a copy signed by Cloudflare.
  - **Start the race** starts the clock. **Use this game, seed and options** updates what racers who join after that get.
  - The Race card also opens the moderator page and the leaderboard.
- **Join a race:** paste the invite link and type the name other racers will see, then click **Join**.
  - FFRando switches to the race's game and sets its seed and options. They stay locked while you race, and the spoiler log stays hidden.
  - Press **Randomize** and start a new game.
  - Keep FFRando open while you race so you show up on the live leaderboard. Press **I finished** when you beat the game.
  - Use the same FFRando version as the host: the same seed can shuffle differently between versions.
  - **Leave** takes you out of the race again.
- **FF9 with Memoria:** Chaos Seed reports from inside the game, including which items you found, your disc and your play time.

### Crisis Core -Final Fantasy VII- Reunion

In the game list, but **not implemented yet**. It's planned for a later update.

### About the "⚠ Risky" options

The options in the **⚠ Risky: may break the game** section change things the game may rely on. They could soft-lock a quest, crash a scene or make an area far too hard. Keep a save from before you turn them on, and turn them off again if something goes wrong.

## This is an early version

Not everything has been tried in game yet:
If something breaks, press **Put originals back**, and please report it on the [issues page](https://github.com/Ozmao/ffrando-releases/issues) with your seed and the options you picked.

## Licenses

FFRando.exe contains **FFRando.Kraken**, a small separate program that decompresses Remake's and Rebirth's game files. It is based on [ooz](https://github.com/powzix/ooz) and licensed under the **GNU GPL v3**. Its full source and the license are in [`kraken-helper/`](kraken-helper). The app unpacks it to `%LOCALAPPDATA%\FFRando\helper` and runs it in the background while it reads the game.

FINAL FANTASY is a registered trademark of Square Enix Holdings Co., Ltd. This is a fan project and isn't affiliated with or endorsed by Square Enix. It ships no game data: it only changes the copy of the game you own.
