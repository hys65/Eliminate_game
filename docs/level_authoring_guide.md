# Level Authoring Guide

This guide defines the current level authoring pipeline for the stable gameplay rules.

Level authoring must not change gameplay semantics. A level is valid only when its data fits the existing runtime rules and passes the editor validation workflow, Play Mode WIN verification, and Console red error check.

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
- Observed error:

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

## 3. Three-layer structure

Every authored level must respect the current three-layer gameplay structure.

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

`Pattern` is the target area.

It displays the blocks that must be removed for the player to win. It is not an input source. The player does not click Pattern cells directly.

### TempZone

`TempZone` is the resolve progress storage area.

It stores clicked SelectionArea tiles as progress slots. A TempZone slot can show partial progress toward completion:

```text
0/3
1/3
2/3
```

When the slot reaches `3/3`, that slot is complete and is removed.

### SelectionArea

`SelectionArea` is the only player input source.

The player clicks available SelectionArea tiles. Those tiles enter TempZone and may trigger Pattern resolve.

---

## 4. Final resolve rule

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

Do not replace this formula with another rule while authoring levels.

---

## 5. Required invariant

Every authored level must preserve the runtime invariant for each color.

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

- `PatternRemaining[color]` is the number of remaining Pattern cells of that color.
- `SelectionRemaining[color]` is the number of remaining SelectionArea tiles of that color.
- `TempDebt[color]` is the unfinished resolve debt currently stored in TempZone slots of that color.
- A TempZone slot with progress `0/3` contributes `3` debt.
- A TempZone slot with progress `1/3` contributes `2` debt.
- A TempZone slot with progress `2/3` contributes `1` debt.

At initial level start, before the player clicks anything, TempZone should have no active debt. Therefore the authored count relationship is:

```text
PatternCount[color] = SelectionAreaCount[color] * 3
```

This count rule is necessary, but not sufficient. The level must also be playable in order, because SelectionArea unlock order and Pattern bottom-row progression can still create a deadlock.

`RuntimeInvariantValidator` must remain active during valid gameplay. Do not disable it to make level data appear valid.

---

## 6. SelectionArea authoring rules

SelectionArea is the only input source.

Authoring rules:

- Only SelectionArea tiles are clickable.
- Pattern cells are not clickable.
- TempZone slots are not clickable.
- At least one initial unlocked tile is required.
- The initial unlocked tile must be a real tile with a non-empty color.
- Unlocking is orthogonal only.
- Orthogonal neighbors are:
  - up
  - down
  - left
  - right
- Diagonal unlock is not allowed.
- SelectionArea tile order affects solvability.

When a player clicks an unlocked SelectionArea tile, only the orthogonal neighbors of that tile may become unlocked. A tile touching only by a corner must not unlock from that click.

Authoring warning:

A level can have correct total color counts and still fail if the required colors are locked behind an unreachable SelectionArea order. Always validate and play test after editing.

---

## 7. Pattern authoring rules

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

---

## 8. Editor Validation workflow

Run validation every time any `GameConfig` is edited.

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

---

## 9. Play test workflow

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

---

## 10. Level asset naming convention

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

Examples of the naming pattern:

```text
Level_001_GameConfig.asset
```

Rules:

- Use three-digit level numbers.
- Keep the `Level_` prefix.
- Keep the `_GameConfig.asset` suffix.
- Do not use ambiguous names such as `NewGameConfig.asset`, `Test.asset`, or `Final.asset` for committed level content.
- `Level_002_GameConfig.asset` exists and has been verified by the user as the first small safe prototype.
- `Level_003_GameConfig.asset` exists and has been verified by the user as the second small safe prototype.
- Do not claim any later level asset exists unless that file has actually been created and verified.

---

## 11. Hard restrictions

Level authoring must stay data-only unless a separate engineering task explicitly changes gameplay rules.

Do not do any of the following while authoring levels:

- Do not add hidden auto-fix behavior.
- Do not rely on hidden auto-fix behavior.
- Do not bypass validation.
- Do not commit a level that has not passed validation.
- Do not commit a level that has not been Play tested to WIN.
- Do not commit a level if Console red errors are greater than 0.
- Do not disable or bypass RuntimeInvariantValidator.
- Do not change resolve semantics.
- Do not change SelectionArea unlock semantics.
- Do not change Pattern gravity semantics.
- Do not add diagonal unlock.
- Do not add cross-column Pattern movement.
- Do not mutate runtime rules to make one level pass.
- Do not create oversized levels yet.
- Do not exceed Pattern non-None cells <= 45.
- Do not exceed SelectionArea tiles <= 15.
- Do not claim `Level_002` or `Level_003` is large-level support or a new large-level baseline.
- Do not claim `Level_003` proves production-ready scaling.
- Do not claim multi-level progression exists.
- Do not claim procedural generation exists.

The level must fit the stable rules. The stable rules must not be changed to fit the level. Large-level support is still not production-ready. Resolve-chain budget, log throttling, deterministic validation search budget, and performance caps still need further testing before scaling up. Random-looking mixed layouts can still explode deterministic solvability search complexity. For now, level growth must remain gradual and validation-budget-friendly.
