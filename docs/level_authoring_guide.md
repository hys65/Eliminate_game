# Level Authoring Guide

This guide defines the current level authoring pipeline for the stable gameplay rules.

Level authoring must not change gameplay semantics. A level is valid only when its data fits the existing runtime rules and passes the editor validation workflow.

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

Recommended level asset location and naming:

```text
Assets/GameConfigs/Levels/Level_001_GameConfig.asset
Assets/GameConfigs/Levels/Level_002_GameConfig.asset
```

Continue the same numbering format for additional levels:

```text
Assets/GameConfigs/Levels/Level_003_GameConfig.asset
Assets/GameConfigs/Levels/Level_004_GameConfig.asset
```

---

## 2. Three-layer structure

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

## 3. Final resolve rule

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

## 4. Required invariant

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

---

## 5. SelectionArea authoring rules

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

## 6. Pattern authoring rules

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

## 7. Validation workflow

Run validation every time any `GameConfig` is edited.

Required workflow:

1. Edit the target `GameConfig` asset.
2. Open the gameplay scene that contains `GameManager`.
3. Make sure the edited `GameConfig` is the current config used by `GameManager` or by the active `LevelDatabase` entry.
4. In the Unity top menu, click:

```text
Tools / Eliminate Game / Validate Current Config
```

5. Open the Console window.
6. Confirm the Console shows:

```text
[EDITOR_VALIDATION] PASS
```

A PASS is required before Play testing.

After validation passes:

1. Enter Play Mode.
2. Play the level manually.
3. Confirm SelectionArea unlock order works.
4. Confirm Pattern resolves through bottom-row matches.
5. Confirm TempZone progress behaves as expected.
6. Confirm the level reaches WIN without deadlock.
7. Only commit after both editor validation and Play testing are complete.

Validation is read-only. It reports invalid authored data; it does not rewrite the level for you.

---

## 8. Level asset naming convention

Store authored level configs under:

```text
Assets/GameConfigs/Levels/
```

Use this naming format:

```text
Level_001_GameConfig.asset
Level_002_GameConfig.asset
Level_003_GameConfig.asset
```

Rules:

- Use three-digit level numbers.
- Keep the `Level_` prefix.
- Keep the `_GameConfig.asset` suffix.
- Do not use ambiguous names such as `NewGameConfig.asset`, `Test.asset`, or `Final.asset` for committed level content.

---

## 9. Hard restrictions

Level authoring must stay data-only unless a separate engineering task explicitly changes gameplay rules.

Do not do any of the following while authoring levels:

- Do not add hidden auto-fix behavior.
- Do not rely on hidden auto-fix behavior.
- Do not bypass validation.
- Do not commit a level that has not passed validation.
- Do not commit a level that has not been play tested.
- Do not change resolve semantics.
- Do not change SelectionArea unlock semantics.
- Do not change Pattern gravity semantics.
- Do not add diagonal unlock.
- Do not add cross-column Pattern movement.
- Do not mutate runtime rules to make one level pass.

The level must fit the stable rules. The stable rules must not be changed to fit the level.
