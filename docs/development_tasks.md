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

removeCount =
  if bottomRowCount < 3:
    bottomRowCount
  else:
    min(bottomRowCount, 3 - currentTempSlotProgress)

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
- [x] invariant tracking:

  PatternRemaining[color]
  =
  SelectionRemaining[color] * 3
  +
  TempDebt[color]

  TempDebt[color]
  =
  sum(3 - TempZoneSlot.ProgressMark for same color)

---

# F. Win/Lose（完成）

- [x] WIN
- [x] LOSE
- [x] input lock
- [x] WIN/LOSE text
- [x] TempZone cleanup after WIN

---

# 当前阶段

System Stabilization

当前重点：
- regression prevention
- runtime validation
- solvability validation
- progress-driven semantics lock

---

# 下一阶段（未开始）

## Level Pipeline

- [ ] procedural generation
- [ ] auto-generated layouts
- [ ] deterministic level generation

---

## Content

- [ ] additional patterns
- [ ] difficulty curves
- [ ] multi-level progression

---

## Presentation

- [ ] better VFX
- [ ] transitions
- [ ] production UI
