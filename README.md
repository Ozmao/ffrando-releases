# FF Randomizer

A randomizer for **Final Fantasy** on PC (Steam):

- **Final Fantasy I, II, III, IV, V and VI**: the Pixel Remasters.
- **Final Fantasy VII**: the original game (both the current Steam release and the 2013 one), **Remake Intergrade** and **Rebirth**.
- **Final Fantasy VIII**: the 2013 Steam release and the Remaster.
- **Final Fantasy IX**, with or without the Memoria mod engine.
- **Final Fantasy X** and **Final Fantasy X-2**: the HD Remaster. Each is randomized on its own.
- **Final Fantasy XII The Zodiac Age**.
- **Final Fantasy XIII** and **Lightning Returns: Final Fantasy XIII**.

Pick a seed, tick what to shuffle, press **Randomize**, and play. It was called FF7Rando until version 1.5.72.

<p align="center"><img src="screenshots/ff7.png" width="860" alt="FFRando with Final Fantasy VII picked: game, seed and the Standard preset on the left, the item and battle options on the right"></p>

- **No mod loader needed.** No 7th Heaven, FFNx, Reloaded-II or anything else to install for FF7, FF8, FFX, FFX-2, FFXII, FFXIII, Lightning Returns or the Pixel Remasters. It changes the game itself. For FF9 it works on the plain game, and does even more if you have Memoria.
- **Your originals are kept.** **Put originals back** undoes everything, whenever you like.
- **Seeds.** The same seed and options always give the same game, so you can race a friend on the same shuffle.
- **Races.** For every game: join a race with an invite link or host one yourself, with a live leaderboard.
- **Spoiler log.** Every randomize writes one, and the app shows it in its own window, so you can look up where something went.

## Download

**[⬇ Latest release](https://github.com/Ozmao/ffrando-releases/releases/latest)**: download `FFRando.exe`.

- It's a single file with nothing to install, for 64-bit Windows 10 or 11. Keep it somewhere you can write to, such as your Desktop or Documents. Updates can't be installed inside Program Files.
- **It updates itself.** When a new version is out, a banner at the top of the window offers it. One click downloads it, checks it against the checksum GitHub has for it, and restarts. FF7Rando 1.4.60 updates to FFRando by itself.
- **Version numbers** read 1.*games supported*.*features released*: 1.14.165 supports 14 games and has 165 features.
- The exe isn't code-signed, so Windows SmartScreen may warn you the first time. Click **More info → Run anyway**.

## How to use it

1. **Close the game.**
2. Pick the game in the **Game** list. Steam installs are found automatically. If yours isn't, use **Browse…** to point at the game folder.
3. Type a seed or press **New seed**, then pick your options. Each game has its own set. A **Preset** sets them all at once: **Light** (the usual options), **Standard** (everything that's safe, numbers moved a little) or **Chaos** (everything, the risky options too, numbers moved a lot).
4. Press **Randomize**.
5. Start the game. In the original FF7, FF9 and the Pixel Remasters, start a **new game**: starting materia, items and gear only apply to a new game, FF9's Chaos Seed randomizes when a new game starts, and chests already opened in a save stay opened. Drops, chests, shops and the rest of FF7's options apply everywhere, and so do all of FF8's. FFX's, FFX-2's, FFXII's, FFXIII's and Lightning Returns' options work in a save you already have too, except for chests you already opened.
6. To go back to the normal game, press **Put originals back**. Do this before using Steam's *Verify integrity of game files*.

Every randomize starts from the original files, so seeds never stack. Backups and spoiler logs are saved in an `FFRando` folder inside the game's folder.

## What it can shuffle

### Final Fantasy I to VI (Pixel Remaster, beta)

All six share most options:

- **On by default:** treasure chests, shop stock, and enemy drops and steals. Each becomes something of a similar price: items stay items, and weapons and armor stay gear.
- **Off by default:**
  - shop prices (half to twice as much), and weapon and armor stats;
  - enemy HP and stats, boss HP and stats, enemy weaknesses and immunities, enemy EXP and gil, and enemy levels (FF3 to FF6);
  - item power, starting gear, battle backgrounds, and weapon and armor elements;
  - character stats and growth, faster leveling (not FF2), spell power and MP cost, and music.
- **For some games only:** spells learned by level (FF4, FF6), job stats (FF3, FF5), job abilities (FF5), esper spells, Mog's dances and Gau's Rages (FF6).
- **⚠ Risky:** random encounters (each swapped for a battle of a similar level), who can equip what, job commands (FF3, FF5) and double bosses.

Key items, gil, spells, summons (FF4's and FF5's summon items, FF3's spell items, FF2's tomes) and boss fights never move. FFRando rewrites the game's own data files and keeps the originals in the `FFRando` folder. Steam's *Verify integrity* or an update also puts the originals back.

<p align="center"><img src="screenshots/pr.png" width="680" alt="The Final Fantasy VI Pixel Remaster options: items, shops and enemies"></p>

### Final Fantasy VII (Steam release and 2013)

Enemy drops, steals and morphs · items in chests and materia lying around · shops and shop prices · random battles (at a similar level, or anywhere) · enemy HP and stats · spells between magic materia · summons between summon materia · materia slots on gear · starting materia, items and gear · stat growth · the order limit breaks are learned in (or, experimentally, limits between characters).

Off by default: boss HP and stats · EXP, AP and gil · elemental weaknesses and status immunities · weapon and armor stats · materia growth on gear · accessory effects · spell power and MP · materia AP · item power (Potions, Ethers, Grenades...) · new elements for elemental armor · battle backgrounds · world map battles (moved between areas, chocobo battles stay) · drop and steal chances · enemy levels · enemy attack power · elements on weapons · limit break needs.

**⚠ Risky:** field music and double bosses.

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
- Weapon attack and magic attack.
- Drop chances, stagger gauges and materia AP.
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
- Weapon attack and magic attack.
- Drop chances, stagger gauges and materia AP.
- **⚠ Risky:** roaming enemies, Queen's Blood decks, field music, Folio skill trees and Queen's Blood card power.

Key items, summons and other things the story needs never move.

<p align="center"><img src="screenshots/rebirth.png" width="680" alt="The Final Fantasy VII Rebirth options: items, chests, shops and battles"></p>

### Final Fantasy VIII (beta)

Both Steam releases: **FINAL FANTASY VIII** (2013) and **FINAL FANTASY VIII Remastered**. There's no mod loader to install. FFRando adds the changed files to the end of the game's own archives (`battle.fs` and `menu.fs` in the 2013 release, `main.zzz` in the Remaster, for every language) and points the game at them. **Put originals back** removes them again. The Remaster's `main.zzz` grows by about 470 MB while the game is randomized.

- **Enemy drops** and **Mug items**: monsters trade with monsters and bosses with bosses, and each slot keeps its level range.
- **Shop stock**: every shop sells a different mix. Potion, Phoenix Down and Normal Ammo stay, and items that need Familiar still do. **Shop prices** can go from half to double.
- **Draw lists**: shuffled within each level range. Guardian Forces stay where they are.
- **Enemy cards**: the cards monsters turn into with the Card command.
- **Enemy HP and stats** and **enemy elements** (off unless you tick them).

Everything works in a save you already have. It hasn't been tried in the game yet, so please report anything that looks wrong.

<p align="center"><img src="screenshots/ff8.png" width="680" alt="The Final Fantasy VIII Remastered options: items, shops, draws and cards"></p>

### Final Fantasy IX

How FF9 gets randomized depends on whether **[Memoria](https://github.com/Albeoris/Memoria)** (FF9's mod engine) is installed. The card at the top of the FF9 panel says which way your install goes.

- **With Memoria:** FFRando installs the **Chaos Seed** mod and switches it on in Memoria's mod list, alongside your other Memoria mods. Every option works, and Chaos Seed randomizes inside the game when you start a new game. The Memoria launcher shows the same choices.
- **Plain game (beta):** without Memoria, FFRando writes the same shuffles straight into the game's files. Options it can't do this way are marked **needs Memoria** in the window and greyed out.

<p align="center"><img src="screenshots/ff9.png" width="680" alt="The Final Fantasy IX options with Memoria found: Chaos Seed randomizes inside the game"></p>

| Option | Plain game | With Memoria |
| --- | --- | --- |
| Equipment abilities | yes | yes |
| Shop stock and synthesis shops | yes | yes |
| Steal items | yes | yes |
| Treasure chests and finds | yes | yes |
| Tetra Master cards | yes | yes |
| Enemy drops | yes | yes |
| Card drops | yes | yes |
| EXP, AP and gil | yes | yes |
| Enemy stats | yes | yes |
| Weaknesses and immunities | yes | yes |
| Blue Magic (who teaches what) | yes | yes |
| Ability AP | yes | yes |
| Weapon and armor stats | yes | yes |
| Spell power and MP | yes | yes |
| Starting equipment (needs a new game) | yes | yes |
| Character stats (the party trades base stats) | yes | yes |
| Magic stone costs | yes | yes |
| Item prices | yes | yes |
| Who can wear armor | yes | yes |
| ⚠ Synthesis recipes | yes | yes |
| ⚠ Boss stats | yes | yes |
| ⚠ Double bosses | yes | yes |
| ⚠ Random encounters | needs Memoria | yes |
| ⚠ Key items | needs Memoria | yes |
| Cutscene skipper (doesn't change the seed) | needs Memoria | yes |

### Final Fantasy X (beta)

**FINAL FANTASY X/X-2 HD Remaster** on Steam. Only FFX is changed here; FFX-2 has its own entry in the list (below). There's no mod loader to install. FFRando adds the changed files to the end of the game's archive (`data\FFX_Data.vbf`, about 16 MB) and points the game at them. **Put originals back** removes them again.

- **Treasure chests**: what every chest and gift holds (items, gil and equipment) is shuffled between them. Key items stay where they are.
- **Item shops**: every shop sells a different mix with as many items as before. Potion, Phoenix Down and Antidote stay.
- **Weapon shops**: each weapon and armor becomes another one for the same character, from what the shops around that point of the story sell.
- **Enemy drops**, **steal items** and **bribe items**: monsters trade with monsters of a similar strength, and bosses with bosses.
- **Item prices**, **equipment drops** (the abilities and slots of the gear monsters drop) and **enemy HP and stats** (off unless you tick them).

Everything works in a save you already have, but chests you already opened stay opened. It hasn't been tried in the game yet, so please report anything that looks wrong.

<p align="center"><img src="screenshots/ffx.png" width="680" alt="The Final Fantasy X HD Remaster options: chests, shops, drops, steals and bribes"></p>

### Final Fantasy X-2 (beta)

The same **FINAL FANTASY X/X-2 HD Remaster** install, picked as **Final Fantasy X-2 HD Remaster** in the list. Only FFX-2 is changed; FFX isn't touched. There's no mod loader to install. FFRando adds the changed tables to the end of `data\FFX2_Data.vbf` (under 1 MB) and points the game at them. **Put originals back** removes them again.

- **Treasure chests**: what every chest and gift holds (items, accessories and gil) is shuffled between them. Dresspheres, garment grids and key items stay where they are.
- **Garment grids** (off unless you tick it): the grids you find or are given trade places with each other.
- **Item shops**: every shop sells a different mix with as many items as before. Potion, Phoenix Down and Antidote stay.
- **Accessories in shops**: each accessory a shop sells becomes another one from the shops that costs about as much.
- **Enemy drops**, **steal items** and **bribe items**: fiends trade with fiends of a similar level, oversouls with oversouls, and bosses with bosses.
- **Prices** and **enemy HP and stats** (off unless you tick them).

Everything works in a save you already have, but chests you already opened stay opened. It hasn't been tried in the game yet, so please report anything that looks wrong.

<p align="center"><img src="screenshots/ffx2.png" width="680" alt="The Final Fantasy X-2 HD Remaster options: chests, garment grids, shops, drops, steals, bribes and stats"></p>

### Final Fantasy XII The Zodiac Age (beta)

**FINAL FANTASY XII THE ZODIAC AGE** on Steam. There's no mod loader to install. FFRando adds the changed tables to the end of the game's archive (`FFXII_TZA.vbf`, about 45 MB, because every language has its own copy of each map) and points the game at them. **Put originals back** removes them again.

- **Treasure chests**: what every chest holds (items, equipment and gil, with its chance to appear) trades places with chests in areas of a similar rank. Chests that come back keep coming back.
- **Shops**: the weapons, armor, accessories, items, magicks and technicks shops sell trade places with others of the same kind and rank. Potion, Phoenix Down, Antidote and gambits stay.
- **Enemy drops**, **steal items** and **poach items**: shuffled between enemies with similar HP. Bosses and marks trade with each other, and key items stay where they are.
- **Prices** and **enemy HP and stats** (off unless you tick them).

Everything works in a save you already have, but chests you already opened stay opened. It hasn't been tried in the game yet, so please report anything that looks wrong.

<p align="center"><img src="screenshots/ff12.png" width="680" alt="The Final Fantasy XII The Zodiac Age options: chests, shops, prices, drops, steals, poaches and stats"></p>

### Final Fantasy XIII (beta)

**FINAL FANTASY XIII** on Steam. There's no mod loader to install. FFRando adds the changed tables to the end of the game's archives (`white_data\sys\white_imgu.win32.bin` and `white_imgc.win32.bin`, the English and Japanese voice versions, about 70 KB each) and points the game at them. **Put originals back** removes them again.

- **Treasure**: what every treasure sphere and mission reward holds (items, equipment and gil) trades places with others of a similar difficulty. Key items stay.
- **Shops**: what the shops sell changes to other items of the same kind and rank, the same way in every shop and tier, so each character's weapons stay their own. Every character's first weapons stay on sale somewhere, and Potion and Phoenix Down stay.
- **Enemy drops**: what enemies drop (common and rare) is shuffled between enemies of a similar rank. Bosses trade with each other, and key items stay.
- **Prices** and **enemy HP and stats** (off unless you tick them).

Everything works in a save you already have, but treasure you already opened stays opened. It hasn't been tried in the game yet, so please report anything that looks wrong.

<p align="center"><img src="screenshots/ff13.png" width="680" alt="The Final Fantasy XIII options: treasure, shops, prices, enemy drops and enemy stats"></p>

### Lightning Returns: Final Fantasy XIII (beta)

**LIGHTNING RETURNS: FINAL FANTASY XIII** on Steam. There's no mod loader to install. FFRando adds the changed tables to the end of the game's archives (`weiss_data\sys\white_img2a.win32.bin` and `white_img2v.win32.bin`, about 1.2 MB each) and points the game at them. **Put originals back** removes them again.

- **Treasure**: what every treasure and quest reward holds (items, equipment, abilities and gil) trades places with others of a similar difficulty. Key items, the cruxes and trade rewards stay.
- **Shops**: what the shops sell changes to other items of the same kind and rank, the same way in every shop and on every day. Potion, Phoenix Down, materials and Libra notes stay.
- **Enemy drops**: what enemies drop (materials, abilities and the special drops) is shuffled between enemies of a similar HP. Bosses trade with each other, and key items stay.
- **Prices** and **enemy HP and stats** (off unless you tick them).

A save you already have should pick up the changes, but treasure you already opened stays opened. It hasn't been tried in the game yet, so please report anything that looks wrong.

<p align="center"><img src="screenshots/lr.png" width="680" alt="The Lightning Returns options: treasure, shops, prices, enemy drops and enemy stats"></p>

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

In the game list, but **not implemented yet**. It's planned for a later update. I don't own the game yet so I can't build it because I dont have the tables :(

### About the "⚠ Risky" options

The options in the **⚠ Risky: may break the game** section change things the game may rely on. They could soft-lock a quest, crash a scene or make an area far too hard. Keep a save from before you turn them on, and turn them off again if something goes wrong.

**⚠ Double bosses** (original FF7, FF9 and the Pixel Remasters) gives boss battles a second copy of their boss when there's room for one more enemy. Bosses made of parts, and battles with helpers that join later or with a battle script, stay as they are. Killing one copy can end some fights, and big bosses can overlap. With Memoria, randomize again after changing your other Memoria mods. Remake and Rebirth don't have it: their boss battles can't be changed yet.

## This is an early version

Not everything has been tried in game yet:
If something breaks, press **Put originals back**, and please report it on the [issues page](https://github.com/Ozmao/ffrando-releases/issues) with your seed and the options you picked.

## Licenses

FFRando.exe contains **FFRando.Kraken**, a small separate program that decompresses Remake's and Rebirth's game files. It is based on [ooz](https://github.com/powzix/ooz) and licensed under the **GNU GPL v3**. Its full source and the license are in [`kraken-helper/`](kraken-helper). The app unpacks it to `%LOCALAPPDATA%\FFRando\helper` and runs it in the background while it reads the game.

The FFXII chest addresses, area and item ranks, shop names and boss list come from Bartz24's [FF12 Open World Randomizer](https://github.com/Bartz24/FFModdingWPF) (MIT License, Copyright (c) 2023 Bartz24).

The FFXIII treasure difficulties and locations, item and enemy ranks, shop names and boss list come from Bartz24's [FF13 Randomizer](https://github.com/Bartz24/FFModdingWPF) (MIT License, Copyright (c) 2023 Bartz24).

The Lightning Returns treasure difficulties and locations, item kinds and ranks, enemy names, shop names and boss list come from Bartz24's [LR Randomizer](https://github.com/Bartz24/FFModdingWPF) (MIT License, Copyright (c) 2023 Bartz24). How Lightning Returns scrambles its file lists was learned from Surihix's [WhiteBinTools](https://github.com/Surihix/WhiteBinTools) (GPL-3.0); FFRando has its own code for it.

FINAL FANTASY is a registered trademark of Square Enix Holdings Co., Ltd. This is a fan project and isn't affiliated with or endorsed by Square Enix. It ships no game data: it only changes the copy of the game you own.
