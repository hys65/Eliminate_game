# DEVELOPMENT TASKS

# A. SelectionArea（完成）

- [x] tile generation
- [x] click detection
- [x] first-row unlock
- [x] orthogonal unlock
- [x] SelectionArea only input source

---

# B. TempZone（完成）

- [x] tile receive
- [x] storage semantics
- [x] 0/3 progress
- [x] 1/3 progress
- [x] 2/3 progress
- [x] TMP integration
- [x] progress += removedCount
- [x] slot removal at 3/3

---

# C. Pattern（完成）

- [x] visualization
- [x] bottom-row resolve source
- [x] ghost feedback
- [x] column gravity
- [x] collapse
- [x] camera shake
- [x] no cross-column movement

---

# D. Resolve System（完成）

- [x] progress-driven resolve semantics
- [x] removeCount formula documented and validated
- [x] same-color resolve
- [x] auto resolve chain（same progress-driven rule）
- [x] stale TempZone cleanup
- [x] invariant-aligned runtime behavior

公式（稳定版）：

```text
removeCount =
  if bottomRowCount < 3:
    bottomRowCount
  else:
    min(bottomRowCount, 3 - currentTempSlotProgress)
```

执行：
- Pattern remove removeCount
- TempZone progress += removeCount
- progress == 3 时移除 slot

---

# E. Runtime Stability（完成）

- [x] index safety fixes
- [x] runtime assertions
- [x] count consistency
- [x] sequence validator
- [x] sequence solvability
- [x] deterministic solvability validation
- [x] runtime deadlock fixes
- [x] RuntimeInvariantValidator remains active during gameplay
- [x] invariant tracking:

  ```text
  PatternRemaining[color]
  =
  SelectionRemaining[color] * 3
  +
  TempDebt[color]

  TempDebt[color]
  =
  sum(3 - TempZoneSlot.ProgressMark for same color)
  ```

---

# F. Win/Lose（完成）

- [x] WIN
- [x] LOSE
- [x] input lock
- [x] WIN/LOSE text
- [x] TempZone cleanup after WIN

---

# G. Level Authoring Documentation（完成）

- [x] Level Authoring Guide completed at `docs/level_authoring_guide.md`
- [x] current stable level authoring rules documented
- [x] GameConfig data-only authoring workflow documented
- [x] Editor Validation workflow documented
- [x] committed level content must pass Editor Validation before commit
- [x] stable baseline workflow documented for duplicating known-good GameConfig assets

Level production must follow:

```text
docs/level_authoring_guide.md
```

Before committing any new level or edited level, run Editor Validation from the Unity menu:

```text
Tools / Eliminate Game / Validate Current Config
```

The Unity Console must show:

```text
[EDITOR_VALIDATION] PASS
```

New level commits are not allowed unless Editor Validation passes.

After Editor Validation passes, the level must be Play tested to WIN.

The verified Play Mode run must have:

```text
Console red errors = 0
```

---

# H. Level_001 Stable Baseline（完成）

Current stable baseline asset:

```text
Assets/GameConfigs/Levels/Level_001_GameConfig.asset
```

Verified state:

- [x] `Level_001_GameConfig` exists under `Assets/GameConfigs/Levels/`
- [x] `GameManager` uses `Level_001_GameConfig`
- [x] `Level_001` passes Editor Validation
- [x] `Level_001` has been Play tested to WIN
- [x] verified Play Mode run had `Console red errors = 0`
- [x] `RuntimeInvariantValidator` remains active during valid gameplay

Level_001 is the stable baseline for future authored levels.

Do not claim additional level assets exist until they are actually created and verified.

Do not claim multi-level progression exists.

Do not claim procedural generation exists.

---

# I. Level_002 Safe Small Prototype（完成）

Current verified small expansion prototype asset:

```text
Assets/GameConfigs/Levels/Level_002_GameConfig.asset
```

Verified by user:

- [x] `Level_002_GameConfig` exists under `Assets/GameConfigs/Levels/`
- [x] Green color was added
- [x] Unity compile has no red errors
- [x] Editor Validation passed
- [x] Play Mode has no red errors
- [x] `Level_002` remains within temporary safe prototype limits
- [x] `Level_002` is data-only authored `GameConfig` content
- [x] `Level_002` is the first verified small expansion prototype

Current Level_002 limits and counts:

- Pattern non-None cells = 27
- SelectionArea tiles = 9
- Pattern non-None cells <= 45
- SelectionArea tiles <= 15
- Pattern Red = 6, Blue = 6, Green = 3, Yellow = 6, Purple = 6
- SelectionArea Red = 2, Blue = 2, Green = 1, Yellow = 2, Purple = 2
- `PatternCount[color] = SelectionAreaCount[color] * 3` holds for all colors

Scope:

- `Level_002` is not a large-level support milestone.
- `Level_002` is not the new large-level baseline.
- `Level_002` is not procedural generation.
- `Level_002` is not multi-level progression.

Temporary scaling warnings remain active:

- Do not create oversized levels yet.
- Do not exceed Pattern non-None cells <= 45.
- Do not exceed SelectionArea tiles <= 15.
- Large-level support is still not production-ready.
- Resolve-chain budget, log throttling, deterministic validation search budget, and performance caps still need further testing before scaling up.
- Random-looking mixed layouts can still explode deterministic solvability search complexity.
- For now, level growth must remain gradual and validation-budget-friendly.

---

# J. Level_003 Verified Small Safe Prototype（完成）

Current verified second small expansion prototype asset:

```text
Assets/GameConfigs/Levels/Level_003_GameConfig.asset
```

Verified by user after the simplified 36 / 12 fix:

- [x] `Level_003_GameConfig` exists under `Assets/GameConfigs/Levels/`
- [x] Editor Validation = PASS
- [x] Play Mode result = WIN
- [x] Play Mode initializes visible gameplay content
- [x] MaxSearchNodes error no longer appears
- [x] Menu -> Restart = normal
- [x] Restart after Menu shows gameplay content again
- [x] Console red errors = 0
- [x] `Level_003` is data-only authored `GameConfig` content
- [x] `Level_003` is the second verified small expansion prototype after `Level_002`

Current Level_003 verified design:

- Pattern non-None cells = 36
- SelectionArea tiles = 12
- Pattern dimensions = 6 rows x 6 columns
- SelectionArea dimensions = 4 columns x 3 rows
- 4 colors only
- Purple is not used
- Red remains dominant or tied-dominant
- Pattern is visually mixed
- Pattern is no longer 14x3
- Pattern is no longer uniform same-color rows
- Pattern fits screen better than the rejected 14x3 version
- Pattern Red = 12, Blue = 9, Green = 9, Yellow = 6, Purple = 0
- SelectionArea Red = 4, Blue = 3, Green = 3, Yellow = 2, Purple = 0
- `PatternCount[color] = SelectionAreaCount[color] * 3` holds for all colors

Rejected Level_003 history:

- Rejected attempt used Pattern non-None cells = 42
- Rejected attempt used SelectionArea tiles = 14
- Rejected attempt used 4 colors and no Purple
- Rejected attempt exceeded `MaxSearchNodes = 200000` during deterministic solvability validation
- Observed error:

```text
[SOLVABILITY_VALIDATION][GameManager.StartRun] FAILED
Solvability search exceeded MaxSearchNodes=200000.
This level may be too complex for deterministic validation.
```

Fix record:

- `Level_003` was simplified to Pattern non-None cells = 36
- `Level_003` was simplified to SelectionArea tiles = 12
- The fix was data-only
- Validation was not weakened
- `MaxSearchNodes` was not increased
- `DeterministicSolvabilityValidator` was not bypassed

Scope:

- `Level_003` is a small safe prototype.
- `Level_003` is not a large-level support milestone.
- `Level_003` is not the new large-level baseline.
- `Level_003` is not procedural generation.
- `Level_003` is not multi-level progression.
- `Level_003` does not prove production-ready scaling.

Temporary scaling warnings remain active:

- Do not create oversized levels yet.
- Do not exceed Pattern non-None cells <= 45.
- Do not exceed SelectionArea tiles <= 15.
- Large-level support is still not production-ready.
- Resolve-chain budget, log throttling, deterministic validation search budget, and performance caps still need further testing before scaling up.
- Random-looking mixed layouts can still explode deterministic solvability search complexity.
- For now, level growth must remain gradual and validation-budget-friendly.

---


# K. LargePatternVisual Gameplay Sync Prototype（完成 / Verified）

Milestone name:

```text
LargePatternVisual Gameplay Sync Prototype
```

Verified by user:

- [x] Console red errors = 0
- [x] Clicking SelectionArea tiles causes regions of the 30x28 LargePatternVisual to disappear
- [x] Original gameplay Pattern still resolves normally
- [x] Level reaches WIN
- [x] On WIN, the LargePatternVisual fully disappears
- [x] Menu -> Restart works
- [x] Restart restores the LargePatternVisual fully
- [x] RuntimeInvariantValidator remains active and clean
- [x] RuntimeInvariantValidator no longer reports errors
- [x] MaxSearchNodes is not triggered

Scope:

- [x] visual-only 30x28 large pixel wall
- [x] driven by small gameplay Pattern removals
- [x] does not increase solver complexity
- [x] does not modify gameplay semantics
- [x] does not increase MaxSearchNodes
- [x] keeps RuntimeInvariantValidator active

Runtime source of truth remains:

```text
GameConfig gameplay Pattern
TempZone
SelectionArea
```

Visual sync route:

```text
Gameplay Pattern cell removed
→ PatternController emits removed-cell event
→ visual binder maps removed gameplay cell to a region of 30x28 visual pixels
→ LargePatternVisualController hides those visual pixels
```

Stable coordinate mapping:

- [x] `PatternCell` stores stable `OriginalRow` and `OriginalColumn`
- [x] `PatternRemovedCell` exposes `OriginalRow`, `OriginalColumn`, `CurrentRow`, `CurrentColumn`, and `Color`
- [x] visual binder maps LargePatternVisual regions using `OriginalRow` and `OriginalColumn`
- [x] ghost effects and current-position visuals use `CurrentRow` and `CurrentColumn`

Reason for original-coordinate mapping:

- The first visual-sync attempt used current row / column mapping.
- Current row / column changes over time because Pattern uses bottom-row resolve, column gravity, and collapse.
- Original coordinates prevent visual regions from repeatedly mapping to the same runtime position.
- Original coordinates allow WIN to fully clear the LargePatternVisual.
- Restart restores the LargePatternVisual fully.

Non-gameplay guarantees:

- [x] 30x28 LargePatternVisual does not participate in DeterministicSolvabilityValidator
- [x] 30x28 LargePatternVisual does not participate in RuntimeInvariantValidator
- [x] 30x28 LargePatternVisual does not participate in PatternCount
- [x] 30x28 LargePatternVisual does not participate in SelectionArea count
- [x] 30x28 LargePatternVisual does not participate in TempZone debt
- [x] 30x28 LargePatternVisual does not decide WIN / LOSE
- [x] DeterministicSolvabilityValidator was not modified

Scaling warnings remain active:

- Do not make 30x28 visual pixels into gameplay cells.
- Do not add 840 cells into GameConfig Pattern.
- Do not bypass deterministic validation.
- Do not disable RuntimeInvariantValidator.
- Do not increase MaxSearchNodes as a workaround.
- Large-level gameplay support is still not production-ready.

---

# 当前阶段

System Stabilization and Level Authoring Documentation

当前重点：
- regression prevention
- runtime validation
- solvability validation
- progress-driven semantics lock
- Level_001 stable baseline is recorded
- Level_002 first verified small expansion prototype is recorded
- Level_003 second verified small expansion prototype is recorded
- level production must follow `docs/level_authoring_guide.md`
- Editor Validation must pass before committing new levels
- Play Mode test must reach WIN before committing new levels
- Console red errors must be 0 before committing new levels
- RuntimeInvariantValidator remains active
- LargePatternVisual Gameplay Sync Prototype is documented as verified
- 30x28 LargePatternVisual remains visual-only and gameplay-sync driven

---

# 当前 level authoring workflow

For any new level or edited level:

1. Duplicate a stable `GameConfig`.
2. Edit level data only.
3. Run Editor Validation.
4. Play test to WIN.
5. Require `Console red errors = 0` before commit.

The current stable source asset for duplication is:

```text
Assets/GameConfigs/Levels/Level_001_GameConfig.asset
```

---

# 下一阶段（未开始）

## Level Pipeline

- [ ] procedural generation
- [ ] auto-generated layouts
- [ ] deterministic level generation

These items are not implemented and must not be described as existing gameplay features.

---

## Content

- [x] first verified small expansion prototype: `Level_002_GameConfig`
- [x] second verified small expansion prototype: `Level_003_GameConfig`
- [ ] additional verified level assets
- [ ] additional patterns
- [ ] difficulty curves
- [ ] multi-level progression

These items are not implemented and must not be described as existing gameplay features.

---

## Presentation

- [ ] better VFX
- [ ] transitions
- [ ] production UI
