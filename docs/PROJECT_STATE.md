# PROJECT_STATE

# 当前项目状态

当前版本：
稳定可玩原型。

核心 runtime 系统已跑通。

`Level_001_GameConfig` 是当前已记录的稳定 baseline。

`Level_002_GameConfig` 是用户已验证的第一个 small safe expansion prototype。它不是新的 large-level baseline。

`Level_003_GameConfig` 是用户已验证的第二个 small safe expansion prototype。它不是新的 large-level baseline，不是 large-level support milestone，也不证明 production-ready scaling。

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
- `Level_002_GameConfig` 已存在并已由用户验证为 small safe expansion prototype。
- `Level_002` 不是新的 large-level baseline。
- 不声明 multi-level progression 已存在。
- 不声明 procedural generation 已存在。

---

## Level_002 Safe Small Prototype

已由用户验证的 small safe expansion prototype：

```text
Assets/GameConfigs/Levels/Level_002_GameConfig.asset
```

当前 verified state：

- `Level_002_GameConfig` exists under `Assets/GameConfigs/Levels/`
- Green color was added
- Unity compile has no red errors
- Editor Validation passed
- Play Mode has no red errors
- `Level_002` remains within the current temporary safe prototype limits
- `Level_002` is data-only authored `GameConfig` content
- `Level_002` is the first verified small expansion prototype

Current temporary safe prototype limits：

- Pattern non-None cells = 27
- SelectionArea tiles = 9
- Pattern non-None cells <= 45
- SelectionArea tiles <= 15

Color counts：

Pattern：

- Red = 6
- Blue = 6
- Green = 3
- Yellow = 6
- Purple = 6

SelectionArea：

- Red = 2
- Blue = 2
- Green = 1
- Yellow = 2
- Purple = 2

Invariant：

```text
PatternCount[color] = SelectionAreaCount[color] * 3
```

This holds for all colors in `Level_002`.

Scope warning：

- `Level_002` is not a large-level support milestone.
- `Level_002` is not procedural generation.
- `Level_002` is not multi-level progression.
- `Level_002` does not prove large-level support is production-ready.

---

## Level_003 Verified Small Safe Prototype

已由用户验证的第二个 small safe expansion prototype：

```text
Assets/GameConfigs/Levels/Level_003_GameConfig.asset
```

当前 verified state：

- `Level_003_GameConfig` exists under `Assets/GameConfigs/Levels/`
- Editor Validation = PASS
- Play Mode result = WIN
- Play Mode initializes visible gameplay content
- MaxSearchNodes error no longer appears
- Menu -> Restart = normal
- Restart after Menu shows gameplay content again
- Console red errors = 0
- `Level_003` is data-only authored `GameConfig` content
- `Level_003` is the second verified small expansion prototype after `Level_002`

Current verified design：

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

Color counts：

Pattern：

- Red = 12
- Blue = 9
- Green = 9
- Yellow = 6
- Purple = 0

SelectionArea：

- Red = 4
- Blue = 3
- Green = 3
- Yellow = 2
- Purple = 0

Invariant：

```text
PatternCount[color] = SelectionAreaCount[color] * 3
```

This holds for all colors in `Level_003`.

Rejected Level_003 history：

- Original attempt used Pattern non-None cells = 42
- Original attempt used SelectionArea tiles = 14
- Original attempt used 4 colors
- Original attempt did not use Purple
- Original attempt exceeded `MaxSearchNodes = 200000` during deterministic solvability validation
- Observed error:

```text
[SOLVABILITY_VALIDATION][GameManager.StartRun] FAILED
Solvability search exceeded MaxSearchNodes=200000.
This level may be too complex for deterministic validation.
```

Fix record：

- `Level_003` was simplified to Pattern non-None cells = 36
- `Level_003` was simplified to SelectionArea tiles = 12
- The fix was data-only
- Validation was not weakened
- `MaxSearchNodes` was not increased
- `DeterministicSolvabilityValidator` was not bypassed

Scope warning：

- `Level_003` is a small safe prototype.
- `Level_003` is not a large-level support milestone.
- `Level_003` is not procedural generation.
- `Level_003` is not multi-level progression.
- `Level_003` is not the new large-level baseline.
- `Level_003` does not prove production-ready scaling.

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

Temporary safe prototype limits remain active:

- Do not create oversized levels yet.
- Do not exceed Pattern non-None cells <= 45.
- Do not exceed SelectionArea tiles <= 15.
- Large-level support is still not production-ready.
- Resolve-chain budget, log throttling, deterministic validation search budget, and performance caps still need further testing before scaling up.
- Random-looking mixed layouts can still explode deterministic solvability search complexity.
- For now, level growth must remain gradual and validation-budget-friendly.

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
- `Level_002` 已由用户验证为 small safe expansion prototype，但不是 large-level support milestone。
- `Level_003` 已由用户验证为第二个 small safe expansion prototype，但不是 large-level support milestone。

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
- 将 `Level_002` 或 `Level_003` 描述为 large-level support milestone 或新的 large-level baseline
- 声称 multi-level progression 存在
- 声称 procedural generation 存在
