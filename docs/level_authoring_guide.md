# Level Authoring Guide

This guide defines the current level authoring pipeline for the stable gameplay rules.

Level authoring must not change gameplay semantics. A level is valid only when its data fits the existing runtime rules and passes the editor validation workflow, Play Mode WIN verification, and Console red error check.

Visual-only presentation assets such as LargePatternVisual configs are not level gameplay data. They must not be counted as Pattern cells, SelectionArea tiles, TempZone debt, solver input, or win/lose state.

---

## 1. Current level data source: GameConfig

The current level data source is `GameConfig`.

A `GameConfig` asset contains the authored data that the runtime uses to start a level:

- Pattern rows
- TempZone capacity
- SelectionArea width and height
- SelectionArea tile definitions
- Rescue settings

When authoring levels, edit the `GameConfig` asset data only. Do not edit runtime gameplay code to make a specific level work.

Current stable baseline asset:

```text
Assets/GameConfigs/Levels/Level_001_GameConfig.asset
```

Verified baseline state:

- `Level_001_GameConfig` exists under `Assets/GameConfigs/Levels/`.
- `GameManager` uses `Level_001_GameConfig`.
- `Level_001` passes Editor Validation.
- `Level_001` has been Play tested to WIN.
- The verified Play Mode run had `Console red errors = 0`.
- `RuntimeInvariantValidator` remains active during valid gameplay.

Use `Level_001_GameConfig` as the known stable baseline when starting new level authoring work.

`Level_002_GameConfig` exists and has been verified by the user as the first small safe expansion prototype. It is documented as data-only authored `GameConfig` content, not as a large-level support milestone and not as the new large-level baseline.

`Level_003_GameConfig` exists and has been verified by the user as the second small safe expansion prototype after the simplified 36 Pattern / 12 Selection fix. It is documented as data-only authored `GameConfig` content, not as a large-level support milestone, not as the new large-level baseline, and not as proof of production-ready scaling.

`LargePatternVisual` is verified as a visual-only 30x28 large pixel presentation layer driven by small gameplay Pattern removals. It is not a level asset, not a 30x28 gameplay Pattern, not 840 gameplay cells support, not large-level solver support, and not production-ready large-level gameplay support.

Do not claim another level asset exists until that asset is actually created, validated, Play tested to WIN, and recorded.

---

## 2. Required level authoring workflow

For any new level or edited level, use this workflow:

1. Duplicate stable `GameConfig`.
2. Edit level data.
3. Run Editor Validation.
4. Play test to WIN.
5. Require `Console red errors = 0` before commit.

The current stable `GameConfig` to duplicate is:

```text
Assets/GameConfigs/Levels/Level_001_GameConfig.asset
```

This workflow is required for committed level content.

A level is not ready to commit if any of the following is true:

- Editor Validation has not passed.
- Play Mode has not been tested to WIN.
- Console red errors are greater than 0.
- RuntimeInvariantValidator is disabled or bypassed.
- DeterministicSolvabilityValidator is bypassed.
- `MaxSearchNodes` is increased as a workaround.

---

## 2.1 Verified small expansion prototype: Level_002

Verified small expansion prototype asset:

```text
Assets/GameConfigs/Levels/Level_002_GameConfig.asset
```

User-verified status:

- `Level_002_GameConfig` exists.
- Green color was added.
- Unity compile has no red errors.
- Editor Validation passed.
- Play Mode has no red errors.
- `Level_002` is still within temporary safe prototype limits.

Current Level_002 temporary safe limits:

- Pattern non-None cells = 27.
- SelectionArea tiles = 9.
- Pattern non-None cells <= 45.
- SelectionArea tiles <= 15.

Color counts:

| Area | Red | Blue | Green | Yellow | Purple |
| --- | ---: | ---: | ---: | ---: | ---: |
| Pattern | 6 | 6 | 3 | 6 | 6 |
| SelectionArea | 2 | 2 | 1 | 2 | 2 |

Invariant status:

```text
PatternCount[color] = SelectionAreaCount[color] * 3
```

This holds for all colors in `Level_002`.

Scope rules for Level_002:

- `Level_002` is a safe small prototype.
- `Level_002` is data-only authored `GameConfig` content.
- `Level_002` is the first verified small expansion prototype.
- `Level_002` is not the new large-level baseline.
- `Level_002` is not a large-level support milestone.
- `Level_002` is not procedural generation.
- `Level_002` is not multi-level progression.

---

## 2.2 Verified small expansion prototype: Level_003

Verified small safe prototype asset:

```text
Assets/GameConfigs/Levels/Level_003_GameConfig.asset
```

User-verified status after the simplified 36 / 12 fix:

- `Level_003_GameConfig` exists.
- Editor Validation = PASS.
- Play Mode result = WIN.
- Play Mode initializes visible gameplay content.
- MaxSearchNodes error no longer appears.
- Menu -> Restart = normal.
- Restart after Menu shows gameplay content again.
- Console red errors = 0.
- `Level_003` is data-only authored `GameConfig` content.
- `Level_003` is the second verified small expansion prototype after `Level_002`.

Current Level_003 verified design:

- Pattern non-None cells = 36.
- SelectionArea tiles = 12.
- Pattern dimensions = 6 rows x 6 columns.
- SelectionArea dimensions = 4 columns x 3 rows.
- 4 colors only.
- Purple is not used.
- Red remains dominant or tied-dominant.
- Pattern is visually mixed.
- Pattern is no longer 14x3.
- Pattern is no longer uniform same-color rows.
- Pattern fits screen better than the rejected 14x3 version.

Color counts:

| Area | Red | Blue | Green | Yellow | Purple |
| --- | ---: | ---: | ---: | ---: | ---: |
| Pattern | 12 | 9 | 9 | 6 | 0 |
| SelectionArea | 4 | 3 | 3 | 2 | 0 |

Invariant status:

```text
PatternCount[color] = SelectionAreaCount[color] * 3
```

This holds for all colors in `Level_003`.

Rejected Level_003 history:

- Original attempt used Pattern non-None cells = 42.
- Original attempt used SelectionArea tiles = 14.
- Original attempt used 4 colors.
- Original attempt did not use Purple.
- Original attempt exceeded `MaxSearchNodes = 200000` during deterministic solvability validation.

Observed error:

```text
[SOLVABILITY_VALIDATION][GameManager.StartRun] FAILED
Solvability search exceeded MaxSearchNodes=200000.
This level may be too complex for deterministic validation.
```

Fix record:

- `Level_003` was simplified to Pattern non-None cells = 36.
- `Level_003` was simplified to SelectionArea tiles = 12.
- The fix was data-only.
- Validation was not weakened.
- `MaxSearchNodes` was not increased.
- `DeterministicSolvabilityValidator` was not bypassed.

Scope rules for Level_003:

- `Level_003` is a small safe prototype.
- `Level_003` is data-only authored `GameConfig` content.
- `Level_003` is the second verified small expansion prototype.
- `Level_003` is not the new large-level baseline.
- `Level_003` is not a large-level support milestone.
- `Level_003` is not procedural generation.
- `Level_003` is not multi-level progression.
- `Level_003` does not prove production-ready scaling.

---

## 3. LargePatternVisual Presentation Pipeline

Milestone group:

```text
LargePatternVisual Gameplay Sync Prototype
Image-to-LargePatternVisualConfig Pipeline 1.0
Visual Palette Expansion 1.0
Presentation Alignment 1.0
Visual Interaction Alignment 1.0
LargePatternVisual Vertical Orientation Fix 1.0
```

Status:

```text
Verified visual-only presentation milestone
```

User-verified result:

- Console red errors = 0.
- Left gameplay Pattern visual is hidden by default.
- Gameplay Pattern logic still resolves normally.
- SelectionArea remains the only input source.
- SelectionArea display colors are mapped to image-related visual colors.
- TempZone display colors are mapped to the same visual colors.
- 30x28 LargePatternVisual displays image-derived mosaic with correct vertical orientation.
- Clicking SelectionArea tiles causes LargePatternVisual palette-targeted cells to disappear gradually.
- Visual removal is not a visible rectangle block by default.
- Level reaches WIN.
- On WIN, the LargePatternVisual fully disappears.
- Menu -> Restart works.
- Restart restores the LargePatternVisual fully.
- RuntimeInvariantValidator remains active and clean.
- RuntimeInvariantValidator no longer reports errors.
- MaxSearchNodes is not triggered.

Authoring meaning:

- The 30x28 LargePatternVisual is a visual-only wall.
- It is driven by the small gameplay Pattern.
- It must not be authored as GameConfig gameplay Pattern cells.
- It must not be counted as PatternCount.
- It must not be counted as SelectionArea tiles.
- It must not be counted as TempZone debt.
- It must not decide WIN / LOSE.
- Its palette colors are presentation colors, not gameplay colors.

Runtime source of truth remains:

```text
GameConfig gameplay Pattern
TempZone
SelectionArea
```

Current visual-only sync route is:

```text
Gameplay Pattern cell removed
→ PatternController emits removed-cell event
→ PatternToLargeVisualBinder receives removed cell Color
→ GameplayColorVisualMapping resolves visual target palette indices
→ LargePatternVisualController hides matching visible palette cells
```

Region fallback route remains available:

```text
Gameplay Pattern cell removed
→ PatternController emits removed-cell event
→ visual binder maps removed gameplay cell to a region of 30x28 visual pixels
→ LargePatternVisualController hides those visual pixels
```

Stable original-coordinate mapping for Region fallback:

- Current row / column is not stable for visual-region mapping because Pattern uses bottom-row resolve, column gravity, and collapse.
- `PatternCell` stores stable `OriginalRow` and `OriginalColumn`.
- `PatternRemovedCell` exposes `OriginalRow`, `OriginalColumn`, `CurrentRow`, `CurrentColumn`, and `Color`.
- Region fallback maps LargePatternVisual regions using `OriginalRow` and `OriginalColumn`.
- Ghost effects and current-position visuals use `CurrentRow` and `CurrentColumn`.

Verification consequences:

- Visual regions are not repeatedly mapped to the same runtime position in Region fallback.
- PaletteTarget removal aligns user-visible disappearance with image palette groups.
- WIN hides all remaining LargePatternVisual pixels.
- Restart restores all LargePatternVisual pixels.
- RuntimeInvariantValidator remains active and clean.
- DeterministicSolvabilityValidator was not modified.
- MaxSearchNodes was not increased.

Scaling warnings:

- Do not make 30x28 visual pixels into gameplay cells.
- Do not add 840 cells into GameConfig Pattern.
- Do not bypass deterministic validation.
- Do not disable RuntimeInvariantValidator.
- Do not increase MaxSearchNodes as a workaround.
- Large-level gameplay support is still not production-ready.

---

## 3.1 Image-to-LargePatternVisualConfig workflow

Menu path:

```text
Tools / Eliminate Game / Visual / Generate Large Pattern From Image
```

Generated / updated assets:

```text
Assets/GameConfigs/Visual/LargePatternVisual_30x28_FromImage.asset
Assets/GameConfigs/Visual/LargePatternVisual_30x28_FromImage_ColorMapping.asset
```

Current output data:

- `width = 30`
- `height = 28`
- `cellSize`
- `paletteColors`
- `cellPaletteIndices`
- top-to-bottom image data orientation
- visual-only color mapping asset

Important:
- The generated visual config is not a level `GameConfig`.
- The generated visual config does not need Editor Validation as a gameplay level.
- The active gameplay `GameConfig` still needs Editor Validation if edited.
- Replacing visual config does not change gameplay solvability.

---

## 3.2 GameplayColorVisualMapping workflow

`GameplayColorVisualMapping` is visual-only.

It can be used by:
- SelectionArea display color
- TempZone display color
- LargePatternVisual PaletteTarget removal

Rules:
- Do not change tile internal `BlockColor`.
- Do not change TempZone slot internal `Color`.
- Do not change Pattern logical color.
- Do not expand gameplay BlockColor to 16 colors for visual purposes.
- Do not include mapping palette colors in PatternCount.
- Do not include mapping palette colors in RuntimeInvariantValidator.

---

## 4. Three-layer structure

Every authored gameplay level must respect the current three-layer gameplay structure.

Top layer:

```text
Pattern
```

Middle layer:

```text
TempZone
```

Bottom layer:

```text
SelectionArea
```

### Pattern

`Pattern` is the gameplay target area.

It contains the logical blocks that must be removed for the player to win. It is not an input source. The player does not click Pattern cells directly.

Pattern visual can be hidden for presentation, but the Pattern logical grid must remain active.

### TempZone

`TempZone` is the resolve progress storage area.

It stores clicked SelectionArea tiles as progress slots. A TempZone slot can show partial progress toward completion:

```text
0/3
1/3
2/3
```

When the slot reaches `3/3`, that slot is complete and is removed.

TempZone visual color can be mapped for presentation only.

### SelectionArea

`SelectionArea` is the only player input source.

The player clicks available SelectionArea tiles. Those tiles enter TempZone and may trigger Pattern resolve.

SelectionArea visual color can be mapped for presentation only.

---

## 5. Final resolve rule

The current stable resolve rule is progress-driven.

Definitions:

```text
bottomRowCount
```

The number of same-color cells currently available in the Pattern bottom row.

```text
currentTempSlotProgress
```

The progress value of the matching same-color TempZone slot before resolve. Valid progress values are:

```text
0
1
2
```

Final resolve formula:

```text
removeCount =
if bottomRowCount < 3:
    bottomRowCount
else:
    min(bottomRowCount, 3 - currentTempSlotProgress)
```

Resolve execution:

1. Pattern removes `removeCount` cells of the matching color from the bottom row source.
2. The matching TempZone slot increases progress by `removeCount`.
3. If the TempZone slot reaches `3/3`, that slot is removed.
4. Pattern applies column gravity.
5. Pattern applies collapse.
6. Auto resolve continues with the same rule while a TempZone color can match the current Pattern bottom row.

Do not replace this formula with another rule while authoring levels or visual systems.

---

## 6. Required invariant

Every authored gameplay level must preserve the runtime invariant for each gameplay color.

Formula:

```text
PatternRemaining[color] =
SelectionRemaining[color] * 3 + TempDebt[color]
```

Temp debt formula:

```text
TempDebt[color] =
sum(3 - TempZoneSlot.ProgressMark for same color)
```

Meaning:

- `PatternRemaining[color]` is the number of remaining Pattern cells of that gameplay color.
- `SelectionRemaining[color]` is the number of remaining SelectionArea tiles of that gameplay color.
- `TempDebt[color]` is the unfinished resolve debt currently stored in TempZone slots of that gameplay color.
- A TempZone slot with progress `0/3` contributes `3` debt.
- A TempZone slot with progress `1/3` contributes `2` debt.
- A TempZone slot with progress `2/3` contributes `1` debt.

At initial level start, before the player clicks anything, TempZone should have no active debt. Therefore the authored count relationship is:

```text
PatternCount[color] = SelectionAreaCount[color] * 3
```

This count rule is necessary, but not sufficient. The level must also be playable in order, because SelectionArea unlock order and Pattern bottom-row progression can still create a deadlock.

`RuntimeInvariantValidator` must remain active during valid gameplay. Do not disable it to make level data appear valid.

LargePatternVisual palette data is excluded from this invariant.

---

## 7. SelectionArea authoring rules

SelectionArea is the only input source.

Authoring rules:

- Only SelectionArea tiles are clickable.
- Pattern cells are not clickable.
- TempZone slots are not clickable.
- At least one initial unlocked tile is required.
- The initial unlocked tile must be a real tile with a non-empty gameplay color.
- Unlocking is orthogonal only.
- Orthogonal neighbors are:
  - up
  - down
  - left
  - right
- Diagonal unlock is not allowed.
- SelectionArea tile order affects solvability.

When a player clicks an unlocked SelectionArea tile, only the orthogonal neighbors of that tile may become unlocked. A tile touching only by a corner must not unlock from that click.

Presentation rule:
- SelectionArea display color can differ from internal gameplay `BlockColor` through `GameplayColorVisualMapping`.
- This must remain visual-only.

---

## 8. Pattern authoring rules

Pattern is bottom-row driven.

Authoring rules:

- Pattern is not clickable.
- Pattern is resolved only through matches against the current bottom row.
- The bottom row is the only matching source.
- Pattern uses column gravity only.
- Cells fall within their own column.
- Cross-column movement is not allowed.
- Collapse is enabled.
- Do not design a level that depends on blocks moving sideways between columns.
- Do not design a level that depends on diagonal or cross-column Pattern behavior.

After a resolve removes Pattern cells, remaining cells fall within their original columns. If collapse changes the occupied Pattern shape, it must still respect the existing runtime collapse behavior. Level data must adapt to the current Pattern semantics, not the other way around.

Presentation rule:
- Pattern visual can be hidden.
- Pattern logical grid must remain active.
- Pattern removed-cell events must continue.

---

## 9. Editor Validation workflow

Run validation every time any gameplay `GameConfig` is edited.

Required Editor Validation workflow:

1. Open the gameplay scene that contains `GameManager`.
2. Make sure the edited `GameConfig` is the current config used by `GameManager` or by the active level data path.
3. In the Unity top menu, click:

```text
Tools / Eliminate Game / Validate Current Config
```

4. Open the Console window.
5. Confirm the Console shows:

```text
[EDITOR_VALIDATION] PASS
```

A PASS is required before Play testing.

Validation is read-only. It reports invalid authored data; it does not rewrite the level for you.

Visual config regeneration does not replace gameplay Editor Validation if gameplay `GameConfig` was edited.

---

## 10. Play test workflow

After Editor Validation passes:

1. Enter Play Mode.
2. Play the level manually.
3. Confirm SelectionArea unlock order works.
4. Confirm Pattern resolves through bottom-row matches.
5. Confirm TempZone progress behaves as expected.
6. Confirm `RuntimeInvariantValidator` remains active during valid gameplay.
7. Confirm the level reaches WIN without deadlock.
8. Open the Console window.
9. Confirm the verified run has:

```text
Console red errors = 0
```

Only commit after Editor Validation passes, Play Mode reaches WIN, and Console red errors equal 0.

For visual-only changes, also confirm:
- LargePatternVisual displays correctly.
- SelectionArea display colors remain mapped if mapping is expected.
- TempZone display colors remain mapped if mapping is expected.
- LargePatternVisual hides on clicks and fully hides on WIN.
- LargePatternVisual restores on Restart.

---

## 11. Level asset naming convention

Store authored level configs under:

```text
Assets/GameConfigs/Levels/
```

Known stable baseline:

```text
Assets/GameConfigs/Levels/Level_001_GameConfig.asset
```

First verified small expansion prototype:

```text
Assets/GameConfigs/Levels/Level_002_GameConfig.asset
```

Second verified small expansion prototype:

```text
Assets/GameConfigs/Levels/Level_003_GameConfig.asset
```

Use this naming format for future level assets only after they are actually created:

```text
Level_###_GameConfig.asset
```

Rules:

- Use three-digit level numbers.
- Keep the `Level_` prefix.
- Keep the `_GameConfig.asset` suffix.
- Do not use ambiguous names such as `NewGameConfig.asset`, `Test.asset`, or `Final.asset` for committed level content.
- Do not claim any later level asset exists unless that file has actually been created and verified.

Visual configs are stored separately under:

```text
Assets/GameConfigs/Visual/
```

Visual configs are not gameplay levels.

---

## 12. Hard restrictions

Level authoring must stay data-only unless a separate engineering task explicitly changes gameplay rules.

Do not do any of the following while authoring levels or visual presentation:

- Do not add hidden auto-fix behavior.
- Do not rely on hidden auto-fix behavior.
- Do not bypass validation.
- Do not commit a level that has not passed validation.
- Do not commit a level that has not been Play tested to WIN.
- Do not commit a level if Console red errors are greater than 0.
- Do not disable or bypass RuntimeInvariantValidator.
- Do not make 30x28 visual pixels into gameplay cells.
- Do not add 840 cells into GameConfig Pattern.
- Do not make LargePatternVisual participate in DeterministicSolvabilityValidator.
- Do not make LargePatternVisual participate in RuntimeInvariantValidator.
- Do not make LargePatternVisual participate in PatternCount, SelectionArea count, or TempZone debt.
- Do not make LargePatternVisual decide WIN / LOSE.
- Do not increase MaxSearchNodes as a workaround.
- Do not change resolve semantics.
- Do not change SelectionArea unlock semantics.
- Do not change Pattern gravity semantics.
- Do not add diagonal unlock.
- Do not add cross-column Pattern movement.
- Do not mutate runtime rules to make one level pass.
- Do not create oversized gameplay levels yet.
- Do not exceed Pattern non-None cells <= 45.
- Do not exceed SelectionArea tiles <= 15.
- Do not claim `Level_002` or `Level_003` is large-level support or a new large-level baseline.
- Do not claim `Level_003` proves production-ready scaling.
- Do not claim 30x28 gameplay Pattern exists.
- Do not claim 840 gameplay cells are supported.
- Do not claim large-level solver support exists.
- Do not claim production-ready large-level support exists.
- Do not claim multi-level progression exists.
- Do not claim procedural generation exists.

---

## 13. Current visual-only content workflow

Use this workflow for visual-only LargePatternVisual content:

1. Keep the gameplay `GameConfig` unchanged unless the task is explicitly a level-authoring task.
2. Open the image generator:

```text
Tools / Eliminate Game / Visual / Generate Large Pattern From Image
```

3. Generate / update:

```text
Assets/GameConfigs/Visual/LargePatternVisual_30x28_FromImage.asset
Assets/GameConfigs/Visual/LargePatternVisual_30x28_FromImage_ColorMapping.asset
```

4. Confirm SampleScene references the generated visual config and mapping.
5. Enter Play Mode.
6. Confirm:
   - LargePatternVisual displays with correct orientation.
   - SelectionArea display colors align with mapping.
   - TempZone display colors align with mapping.
   - Clicking SelectionArea progresses gameplay.
   - LargePatternVisual uses PaletteTarget visual removal.
   - WIN hides all visual cells.
   - Restart restores all visual cells.
   - Console red errors = 0.

Important:
- This workflow is visual-only.
- It does not validate or change gameplay solvability.
- It does not replace Editor Validation for gameplay level edits.

---

## 14. Current stage closure

Current stage is closed as:

```text
Visual-only large pixel presentation pipeline verified.
Gameplay remains small-pattern driven.
30x28 remains presentation layer only.
```

Recommended next milestone:

```text
Visual Polish 1.0
```

Do not use next milestone to enlarge gameplay Pattern.

Suggested Visual Polish scope:
- background visual polish
- LargePatternVisual scale / position polish
- SelectionArea layout polish
- TempZone layout polish
- visual cell fade / scale animation
- UI polish
- better source image preprocessing guidance