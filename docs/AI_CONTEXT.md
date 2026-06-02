# AI_CONTEXT

## 游戏核心（一句话）

玩家点击 SelectionArea 方块
→ 进入 TempZone
→ 触发 Pattern 底行解析
→ 执行消除与列内下落
→ 自动 Resolve Chain
→ 清空 Pattern 获胜。

---

# 当前三层结构（稳定版本）

Top:
- Pattern（目标区）

Middle:
- TempZone（解析暂存区）

Bottom:
- SelectionArea（唯一输入区）

---

# 当前核心规则（必须保持）

## SelectionArea

规则：
- 只有 SelectionArea 可点击。
- 初始仅第一行解锁。
- 点击后解锁十字邻居：
  - up
  - down
  - left
  - right
- 不允许对角解锁。

重要：
SelectionArea 的布局顺序会影响可通关性。

---

## TempZone

规则：
- TempZone 不是输入源。
- TempZone 负责存储解析进度。
- TempZone slot 使用：
  - 0/3
  - 1/3
  - 2/3
  进度状态。

---

## Pattern

规则：
- 不可点击。
- 仅用于显示、解析、消除。
- 使用列内重力。
- 不允许跨列移动。
- 支持 collapse。
- 支持 ghost feedback。
- 支持 camera shake。
- 底行是唯一匹配来源。

---

# Resolve 规则（当前稳定版：Progress-Driven）

## 核心语义

Resolve amount 由 TempZone 当前 slot 的剩余进度容量驱动。

- currentTempSlotProgress = 当前同色 TempZone slot 的 progress（0/1/2）
- bottomRowCount = 当前 Pattern 底行可匹配同色数量

公式：

removeCount =
  if bottomRowCount < 3:
    bottomRowCount
  else:
    min(bottomRowCount, 3 - currentTempSlotProgress)

执行：
- Pattern 移除 removeCount 个同色单元。
- TempZone slot 执行 progress += removeCount。
- 如果 slot progress 达到 3/3，则该 slot 完成并移除。

---

## 已验证行为（示例）

1. TempZone 0/3 + bottom-row same color = 1
   => remove 1 Pattern cell
   => TempZone becomes 1/3

2. TempZone 0/3 + bottom-row same color = 2
   => remove 2 Pattern cells
   => TempZone becomes 2/3

3. TempZone 0/3 + bottom-row same color >= 3
   => remove 3 Pattern cells
   => TempZone slot completes and is removed

4. TempZone 1/3 + bottom-row same color >= 3
   => remove 2 Pattern cells
   => TempZone slot completes and is removed

5. TempZone 2/3 + bottom-row same color >= 3
   => remove 1 Pattern cell
   => TempZone slot completes and is removed

---

# 自动解析链（Auto Resolve Chain）

玩家点击后：

SelectionArea click
→ tile enters TempZone
→ resolve selected color（使用同一 Progress-Driven 规则）
→ gravity
→ collapse
→ 获取新的底行
→ 自动继续解析 TempZone 中可匹配颜色（仍使用同一 Progress-Driven 规则）
→ 直到不存在可匹配颜色

---

# 可通关规则（必须保持）

## 总数量规则

对于每个颜色：

PatternCount[color]
=
SelectionAreaCount[color] * 3

---

## 运行时不变量（核心）

PatternRemaining[color]
=
SelectionRemaining[color] * 3
+
TempDebt[color]

其中：

TempDebt[color]
=
sum(3 - TempZoneSlot.ProgressMark for same color)

说明：
- TempDebt 表示该颜色在 TempZone 中尚未完成的进度债务。
- Resolve 与 auto resolve chain 必须持续维持该不变量。

---

## 顺序可通关规则（重要）

即使总数量正确：

也必须保证：
- SelectionArea 解锁顺序可达
- Pattern 底行推进过程中存在可获取颜色
- 不允许出现“后期缺色”

---

# TempZone 清理规则

如果：
Pattern 已不存在某颜色

则：
TempZone 不允许继续保留该颜色。

必须自动清理 stale slot。

---

# 胜负规则

## WIN

条件：
- Pattern 完全为空

行为：
- 停止输入
- 显示 WIN
- 清空 TempZone 可视状态

---

## LOSE

条件：
- TempZone 已满
- 并且 TempZone 无颜色匹配当前 Pattern 底行

行为：
- 停止输入
- 显示 LOSE

---

# 当前稳定状态（已验证）

已验证：
- Same-color resolve
- Progress-driven resolve（按 slot 剩余容量）
- Auto resolve chain（同规则连续执行）
- Column gravity
- Collapse
- TempZone cleanup
- Win state
- Lose state
- Runtime assertions
- Count consistency
- Sequence solvability

---

# 当前已验证关卡资产

## Level_001 stable baseline

```text
Assets/GameConfigs/Levels/Level_001_GameConfig.asset
```

`Level_001` 保持当前稳定 baseline。

## Level_002 safe small prototype

```text
Assets/GameConfigs/Levels/Level_002_GameConfig.asset
```

用户已验证：

- `Level_002_GameConfig` exists。
- Green color was added。
- Unity compile has no red errors。
- Editor Validation passed。
- Play Mode has no red errors。
- `Level_002` is still within temporary safe prototype limits。

当前 `Level_002` 限制：

- Pattern non-None cells = 27
- SelectionArea tiles = 9
- Pattern non-None cells <= 45
- SelectionArea tiles <= 15

颜色数量：

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

不变量：

```text
PatternCount[color] = SelectionAreaCount[color] * 3
```

该不变量对 `Level_002` 所有颜色成立。

`Level_002` 是 data-only authored `GameConfig`，是第一个 verified small expansion prototype。

`Level_002` 不是 large-level support milestone，不是新的 large-level baseline，不是 procedural generation，不是 multi-level progression。


## Level_003 verified small safe prototype

```text
Assets/GameConfigs/Levels/Level_003_GameConfig.asset
```

用户已验证：

- `Level_003_GameConfig` exists。
- Editor Validation = PASS。
- Play Mode result = WIN。
- Play Mode initializes visible gameplay content。
- MaxSearchNodes error no longer appears。
- Menu -> Restart = normal。
- Restart after Menu shows gameplay content again。
- Console red errors = 0。
- `Level_003` is data-only authored `GameConfig` content。
- `Level_003` is the second verified small expansion prototype after `Level_002`。

最终 verified design：

- Pattern non-None cells = 36。
- SelectionArea tiles = 12。
- Pattern dimensions = 6 rows x 6 columns。
- SelectionArea dimensions = 4 columns x 3 rows。
- 4 colors only。
- Purple is not used。
- Red remains dominant or tied-dominant。
- Pattern is visually mixed。
- Pattern is no longer 14x3。
- Pattern is no longer uniform same-color rows。
- Pattern fits screen better than the rejected 14x3 version。

颜色数量：

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

不变量：

```text
PatternCount[color] = SelectionAreaCount[color] * 3
```

该不变量对 `Level_003` 所有颜色成立。

Level_003 history：

- Rejected attempt: Pattern non-None cells = 42。
- Rejected attempt: SelectionArea tiles = 14。
- Rejected attempt: 4 colors only。
- Rejected attempt: Purple was not used。
- Rejected attempt failed deterministic solvability validation because search exceeded `MaxSearchNodes = 200000`。
- Observed error:

```text
[SOLVABILITY_VALIDATION][GameManager.StartRun] FAILED
Solvability search exceeded MaxSearchNodes=200000.
This level may be too complex for deterministic validation.
```

Fix record：

- `Level_003` was simplified to Pattern non-None cells = 36。
- `Level_003` was simplified to SelectionArea tiles = 12。
- The fix was data-only。
- Validation was not weakened。
- `MaxSearchNodes` was not increased。
- `DeterministicSolvabilityValidator` was not bypassed。

Scope warning：

- `Level_003` is a small safe prototype。
- `Level_003` is not a large-level support milestone。
- `Level_003` is not procedural generation。
- `Level_003` is not multi-level progression。
- `Level_003` is not the new large-level baseline。
- `Level_003` does not prove production-ready scaling。

Scaling warning：

- Do not create oversized levels yet.
- Do not exceed Pattern non-None cells <= 45.
- Do not exceed SelectionArea tiles <= 15.
- Large-level support is still not production-ready.
- Resolve-chain budget, log throttling, deterministic validation search budget, and performance caps still need further testing before scaling up.
- Random-looking mixed layouts can still explode deterministic solvability search complexity.
- For now, level growth must remain gradual and validation-budget-friendly.

---

# LargePatternVisual Gameplay Sync Prototype（Verified）

Milestone name:

```text
LargePatternVisual Gameplay Sync Prototype
```

Status:

```text
Verified
```

User-verified result:

- Console red errors = 0.
- Clicking SelectionArea tiles causes regions of the 30x28 LargePatternVisual to disappear.
- Original gameplay Pattern still resolves normally.
- Level reaches WIN.
- On WIN, the LargePatternVisual fully disappears.
- Menu -> Restart works.
- Restart restores the LargePatternVisual fully.
- RuntimeInvariantValidator remains active and clean.
- RuntimeInvariantValidator no longer reports errors.
- DeterministicSolvabilityValidator was not modified.
- MaxSearchNodes was not increased.
- MaxSearchNodes is not triggered.

Architecture:

- LargePatternVisual is a visual-only 30x28 large pixel wall.
- LargePatternVisual is driven by the small gameplay Pattern.
- LargePatternVisual does not modify gameplay semantics.
- LargePatternVisual does not increase solver complexity.
- LargePatternVisual does not increase MaxSearchNodes.
- LargePatternVisual keeps RuntimeInvariantValidator active.

Runtime source of truth remains only:

```text
GameConfig gameplay Pattern
TempZone
SelectionArea
```

The 30x28 LargePatternVisual does not participate in:

- DeterministicSolvabilityValidator.
- RuntimeInvariantValidator.
- PatternCount.
- SelectionArea count.
- TempZone debt.
- WIN / LOSE decisions.

Visual sync route:

```text
Gameplay Pattern cell removed
→ PatternController emits removed-cell event
→ visual binder maps removed gameplay cell to a region of 30x28 visual pixels
→ LargePatternVisualController hides those visual pixels
```

Stable coordinate mapping rule:

- The first visual-sync attempt used current row / column mapping.
- Current row / column mapping was not stable because Pattern uses bottom-row resolve, column gravity, and collapse.
- Final verified mapping uses stable original identity: `OriginalRow` and `OriginalColumn`.
- `PatternCell` stores `OriginalRow` and `OriginalColumn`.
- `PatternRemovedCell` exposes `OriginalRow`, `OriginalColumn`, `CurrentRow`, `CurrentColumn`, and `Color`.
- The visual binder maps LargePatternVisual regions using `OriginalRow` and `OriginalColumn`.
- Ghost effects and current-position visuals use `CurrentRow` and `CurrentColumn`.

This ensures:

- Visual regions are not repeatedly mapped to the same runtime position.
- WIN can fully clear the LargePatternVisual.
- Restart can fully restore the LargePatternVisual.

Scaling warnings remain active:

- Do not make 30x28 visual pixels into gameplay cells.
- Do not add 840 cells into GameConfig Pattern.
- Do not bypass deterministic validation.
- Do not disable RuntimeInvariantValidator.
- Do not increase MaxSearchNodes as a workaround.
- Large-level gameplay support is still not production-ready.

Do not claim:

- 30x28 gameplay Pattern exists.
- 840 gameplay cells are supported.
- Large-level solver support exists.
- Procedural generation exists.
- Multi-level progression exists.
- Production-ready large-level support exists.

---

# 开发原则（必须遵守）

- Docs 是唯一事实源。
- 禁止猜规则。
- 禁止私自修改玩法语义。
- Runtime correctness 优先。
- 稳定性优先于优化。
- 优先最小安全修复。
