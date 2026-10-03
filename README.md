# No Lightning Fires

A RimWorld 1.6 mod (Harmony) that stops lightning from starting fires.

A strike keeps its bolt, flash, smoke, thunder and burn damage. Pawns and buildings next to it still get hurt, but
nothing catches fire. This covers thunderstorms, dry thunderstorms and flashstorms. The mod has no settings, and you
can add it to or remove it from a save at any time.

## How it works

Every lightning strike calls `WeatherEvent_LightningStrike.DoStrike`, which sets off a radius 1.9 explosion of
`DamageDefOf.Flame`. The `Flame` damage worker ignites the cells and things it hits. Vanilla also defines `Burn`, a
child of `Flame` with the same damage, armor category, visuals and sound, whose worker never starts or attaches
fire. A Harmony transpiler swaps the one `Flame` load in `DoStrike` for `Burn`. If a game update removes that load,
the mod logs an error instead of failing silently.

Fire from other sources (incendiary weapons, molotovs, explosions) is unaffected, because the patch only touches
`DoStrike`.

| Path | What |
|---|---|
| `Source/NoLightningFires` | The mod: the Harmony transpiler |
| `Source/NoLightningFires.DevHarness` | In-game automated scenarios and the play-test playground (never shipped) |

## Development

You need the .NET SDK and RimWorld 1.6 on Steam, with Harmony subscribed on the Workshop.

```
make smoke      # full automated in-game suite (Steam running, RimWorld closed)
make play       # isolated play-test profile; start a NEW colony to get a flashstorm over a grass field
make package    # dist/NoLightningFires, ready to upload
```

`make smoke` and `make play` use their own save-data folders, `tools/.smoke-profile` and `tools/.dev-profile`,
with their own mod lists. They never touch your real saves, settings or mod list. Both launch with all sound muted.

### Automated scenarios

Each scenario builds a grass field with wood log stacks and wooden walls, which a vanilla strike would set alight.

| Scenario | Checks |
|---|---|
| boot | The transpiler is applied to `DoStrike`, and `Burn` still uses a worker that cannot ignite |
| strike | 9 direct strikes on the field, one on a naked colonist: no fire, the colonist gets a burn, a wooden wall loses hit points |
| flashstorm | A real flashstorm over the field: 6 strikes, all inside the field, no fire |
| storm | A dry thunderstorm over the whole unfogged map: 3 natural strikes, no fire anywhere on the map |
| control | The same Flame explosion set off directly, not through `DoStrike`, does start fires. This proves the field is a valid test |

## Releasing

1. `make smoke` passes.
2. `make package` builds `dist/NoLightningFires`, which `make deploy` links into `RimWorld/Mods/NoLightningFires`.
3. With dev mode on, open Mods in RimWorld, select No Lightning Fires, not the Dev Harness, and click Upload.
   Steam writes `About/PublishedFileId.txt` into `dist/`, and `package.ps1` keeps it across repackages.
4. On the Workshop page, paste `workshop/description.bbcode.txt`.

`tools/asset-pipeline/generate_asset.py` made `About/Preview.png` and `About/ModIcon.png` with Gemini. It reads
`GEMINI_API_KEY` from the environment. `title_preview.py` adds the title to the preview.
