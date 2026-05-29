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

# 当前阶段

System Stabilization and Level Authoring Documentation

当前重点：
- regression prevention
- runtime validation
- solvability validation
- progress-driven semantics lock
- Level_001 stable baseline is recorded
- level production must follow `docs/level_authoring_guide.md`
- Editor Validation must pass before committing new levels
- Play Mode test must reach WIN before committing new levels
- Console red errors must be 0 before committing new levels
- RuntimeInvariantValidator remains active

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
