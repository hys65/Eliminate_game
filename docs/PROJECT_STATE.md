# PROJECT_STATE

# 当前项目状态

当前版本：
稳定可玩原型。

核心 runtime 系统已跑通。

`Level_001_GameConfig` 是当前已记录的稳定 baseline。

当前稳定 baseline asset 位置：

```text
Assets/GameConfigs/Levels/Level_001_GameConfig.asset
```

Level Authoring Guide 已完成，位置：

```text
docs/level_authoring_guide.md
```

后续 level production 必须遵守该 guide。

---

# 当前已验证

## Level_001 Stable Baseline

已验证 baseline：

```text
Assets/GameConfigs/Levels/Level_001_GameConfig.asset
```

当前 verified state：

- `Level_001_GameConfig` exists under `Assets/GameConfigs/Levels/`
- `GameManager` uses `Level_001_GameConfig`
- `Level_001` passes Editor Validation
- `Level_001` has been Play tested to WIN
- verified Play Mode run had `Console red errors = 0`
- `RuntimeInvariantValidator` remains active during valid gameplay

说明：
- `Level_001_GameConfig` 是当前稳定关卡数据 baseline。
- 该 baseline 可作为后续关卡制作时复制的起点。
- 不声明 `Level_002` 已存在。
- 不声明 multi-level progression 已存在。
- 不声明 procedural generation 已存在。

---

## Gameplay

- SelectionArea input
- SelectionArea is the only input source
- orthogonal unlock
- TempZone storage
- Pattern resolve
- auto resolve chain
- gravity
- collapse
- no cross-column movement

---

## Resolve Semantics（Final Stabilized）

- progress-driven resolve（按 TempZone slot 剩余容量决定 removeCount）
- TempZone progress += removedCount
- slot progress 到 3/3 后移除
- auto resolve chain 持续使用同一 progress-driven 规则
- same-color matching
- TempZone stale cleanup

公式：

```text
removeCount =
  if bottomRowCount < 3:
    bottomRowCount
  else:
    min(bottomRowCount, 3 - currentTempSlotProgress)
```

---

## Runtime Stability

- 无已知 resolve deadlock
- 无已知 index crash
- runtime assertions 生效
- count consistency 已验证
- deterministic solvability validation 已接入 GameManager.StartRun()
- RuntimeInvariantValidator remains active during valid gameplay

---

## Deterministic Solvability Validation（已接入）

在 GameManager.StartRun() 执行 deterministic solvability validation。

当前 validation checks：
- PatternCount[color] == SelectionCount[color] * 3
- SelectionArea orthogonal reachability
- playable sequence solvability
- endgame color availability
- unavoidable deadlock

约束说明：
- validator is read-only
- validator uses copied simulation data
- no gameplay semantics changed
- no hidden auto-fixes
- runtime behavior remains source of truth

---

## Level Authoring Guide（已完成）

Level Authoring Guide 已完成：

```text
docs/level_authoring_guide.md
```

该 guide 是当前 level production 的标准流程。

所有新增关卡与已有关卡编辑必须遵守：
- 使用 `GameConfig` 作为当前 level data source
- 保持 data-only authoring
- 不修改 gameplay semantics 来适配单个关卡
- 保持 SelectionArea 只允许 orthogonal unlock
- 保持 Pattern bottom-row resolve source
- 保持 column gravity
- 保持 no cross-column movement
- 保持 progress-driven resolve formula
- 保持 RuntimeInvariantValidator active

当前 workflow：

1. duplicate stable GameConfig
2. edit level data
3. run Editor Validation
4. Play test to WIN
5. require Console red errors = 0 before commit

当前 stable GameConfig 是：

```text
Assets/GameConfigs/Levels/Level_001_GameConfig.asset
```

提交新关卡或编辑后的关卡前，必须运行 Editor Validation：

```text
Tools / Eliminate Game / Validate Current Config
```

Unity Console 必须显示：

```text
[EDITOR_VALIDATION] PASS
```

Editor Validation 通过后，还必须 Play test 并确认关卡可以到达 WIN，没有 deadlock。

Play test verified run 必须满足：

```text
Console red errors = 0
```

---

## Runtime Invariant（核心）

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

说明：
- invariant 在每次 resolve 与 auto resolve chain 中持续成立。
- RuntimeInvariantValidator remains active during valid gameplay.

---

## End States

- WIN
- LOSE
- input lock
- WIN cleanup

Level_001 has been Play tested to WIN.

---

# 当前可通关规则

## 总数量

```text
PatternCount[color]
=
SelectionAreaCount[color] * 3
```

---

## 顺序可通关

已验证：
- unlock order
- reachable colors
- bottom-row progression
- endgame color availability

Level_001 verified run reaches WIN.

---

# 当前已知限制

尚未 production-ready：

- polished UI
- advanced VFX
- level generator
- meta progression
- difficulty balancing

说明：
- procedural generation 未完成。
- next-level flow 未声明完成。
- multi-level progression 未声明完成。
- `Level_002` 未声明存在。

---

# 当前开发优先级

1. runtime correctness
2. stability
3. solvability
4. tooling
5. content
6. presentation

---

# 重要规则

禁止：
- 未确认情况下修改 gameplay semantics
- 私自偏离 progress-driven resolve 语义
- 私自修改 resolve chain
- 未按照 `docs/level_authoring_guide.md` 制作或编辑关卡
- 在 Editor Validation 未通过时提交新关卡或编辑后的关卡
- 在 Play test 未到达 WIN 时提交新关卡或编辑后的关卡
- 在 Console red errors 不为 0 时提交新关卡或编辑后的关卡
- 声称 `Level_002` 存在，除非该 asset 已实际创建并验证
- 声称 multi-level progression 存在
- 声称 procedural generation 存在
