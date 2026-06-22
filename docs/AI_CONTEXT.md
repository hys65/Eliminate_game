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
- Pattern（目标区 / gameplay source of truth）

Middle:
- TempZone（解析暂存区）

Bottom:
- SelectionArea（唯一输入区）

---

# 当前核心规则（必须保持）

## SelectionArea

规则：
- 只有 SelectionArea 可点击。
- Pattern 不可点击。
- TempZone 不可点击。
- 初始仅部分 tile 解锁。
- 点击后只解锁正交邻居：
  - up
  - down
  - left
  - right
- 不允许对角解锁。
- SelectionArea 的布局顺序会影响可通关性。

## TempZone

规则：
- TempZone 不是输入源。
- TempZone 负责存储解析进度。
- TempZone slot 使用：
  - 0/3
  - 1/3
  - 2/3
- slot progress 到 3/3 后立即完成并移除。
- Pattern 已不存在某颜色时，该颜色 stale slot 必须自动清理。

## Pattern

规则：
- Pattern 是 gameplay logical target。
- Pattern 不可点击。
- Pattern 只通过底行 bottom-row resolve。
- 使用 column gravity。
- 不允许 cross-column movement。
- 支持 collapse。
- 支持 ghost feedback。
- 支持 camera shake。
- Pattern logical state 是 runtime source of truth。

---

# Resolve 规则（当前稳定版：Progress-Driven）

Resolve amount 由 TempZone 当前同色 slot 的剩余进度容量驱动。

定义：
- `currentTempSlotProgress` = 当前同色 TempZone slot 的 progress（0/1/2）
- `bottomRowCount` = 当前 Pattern 底行可匹配同色数量

公式：

```text
removeCount =
  if bottomRowCount < 3:
    bottomRowCount
  else:
    min(bottomRowCount, 3 - currentTempSlotProgress)
```

执行：
- Pattern 移除 `removeCount` 个同色底行来源单元。
- TempZone slot 执行 `progress += removeCount`。
- 如果 slot progress 达到 3/3，则该 slot 完成并移除。
- auto resolve chain 每一步都使用同一 progress-driven rule。

禁止：
- 不允许替换公式。
- 不允许回到旧 Case A / Case B 语义。
- 不允许为单个视觉需求修改 resolve semantics。

---

# 自动解析链（Auto Resolve Chain）

玩家点击后：

```text
SelectionArea click
→ tile enters TempZone
→ resolve selected color（使用同一 Progress-Driven 规则）
→ gravity
→ collapse
→ 获取新的底行
→ 自动继续解析 TempZone 中可匹配颜色（仍使用同一 Progress-Driven 规则）
→ 直到不存在可匹配颜色
```

---

# 可通关规则（必须保持）

## 初始总数量规则

对于每个 gameplay BlockColor：

```text
PatternCount[color]
=
SelectionAreaCount[color] * 3
```

## 运行时不变量（核心）

```text
PatternRemaining[color]
=
SelectionRemaining[color] * 3
+
TempDebt[color]
```

其中：

```text
TempDebt[color]
=
sum(3 - TempZoneSlot.ProgressMark for same color)
```

说明：
- TempDebt 表示该颜色在 TempZone 中尚未完成的进度债务。
- Resolve 与 auto resolve chain 必须持续维持该不变量。
- RuntimeInvariantValidator 必须保持开启。

禁止：
- 禁用 RuntimeInvariantValidator。
- 绕过 RuntimeInvariantValidator。
- 修改不变量公式。
- 添加 hidden auto-fix。
- 静默修改 runtime 数据来让关卡通过。

---

# 胜负规则

## WIN

条件：
- Pattern logical grid 完全为空。

行为：
- 停止输入。
- 显示 WIN。
- 清空 TempZone 可视状态。
- LargePatternVisual fully hides all remaining visible visual cells。

## LOSE

条件：
- TempZone 已满。
- 并且 TempZone 无颜色匹配当前 Pattern 底行。

行为：
- 停止输入。
- 显示 LOSE。

---

# 当前已验证关卡资产

## Level_001 stable baseline

```text
Assets/GameConfigs/Levels/Level_001_GameConfig.asset
```

已验证：
- Editor Validation PASS。
- Play Mode WIN。
- Console red errors = 0。
- RuntimeInvariantValidator remains active during valid gameplay。

## Level_002 safe small prototype

```text
Assets/GameConfigs/Levels/Level_002_GameConfig.asset
```

已验证：
- Green color added。
- Pattern non-None cells = 27。
- SelectionArea tiles = 9。
- 5 colors。
- Editor Validation PASS。
- Play Mode no red errors。
- Console red errors = 0。

Scope：
- `Level_002` 是 first verified small expansion prototype。
- `Level_002` 不是 large-level baseline。
- `Level_002` 不是 procedural generation。
- `Level_002` 不是 multi-level progression。

## Level_003 verified small safe prototype

```text
Assets/GameConfigs/Levels/Level_003_GameConfig.asset
```

最终 verified design：
- Pattern non-None cells = 36。
- SelectionArea tiles = 12。
- Pattern dimensions = 6 rows x 6 columns。
- SelectionArea dimensions = 4 columns x 3 rows。
- 4 colors only。
- Purple is not used。
- Pattern Red = 12, Blue = 9, Green = 9, Yellow = 6, Purple = 0。
- SelectionArea Red = 4, Blue = 3, Green = 3, Yellow = 2, Purple = 0。
- `PatternCount[color] = SelectionAreaCount[color] * 3` holds for all colors。

已验证：
- Editor Validation = PASS。
- Play Mode result = WIN。
- Menu -> Restart = normal。
- Restart after Menu shows gameplay content again。
- Console red errors = 0。
- MaxSearchNodes error no longer appears。

Rejected history：
- Level_003 曾尝试 Pattern non-None cells = 42 / SelectionArea tiles = 14。
- 该版本触发：

```text
[SOLVABILITY_VALIDATION][GameManager.StartRun] FAILED
Solvability search exceeded MaxSearchNodes=200000.
```

Fix record：
- `Level_003` simplified to 36 / 12。
- Validation was not weakened。
- `MaxSearchNodes` was not increased。
- `DeterministicSolvabilityValidator` was not bypassed。

Scope：
- `Level_003` is a small safe prototype。
- `Level_003` is not large-level support。
- `Level_003` is not procedural generation。
- `Level_003` is not multi-level progression。
- `Level_003` does not prove production-ready scaling。

---

# LargePatternVisual System（当前收口状态 / Verified）

## Milestone group

```text
LargePatternVisual Gameplay Sync Prototype
Visual Palette Expansion 1.0
Presentation Alignment 1.0
Visual Interaction Alignment 1.0
LargePatternVisual Vertical Orientation Fix 1.0
```

## 当前 verified status

用户已验证：
- Console red errors = 0。
- 左侧 gameplay Pattern visual 默认隐藏。
- gameplay Pattern logical state 仍存在并驱动 gameplay。
- 30x28 LargePatternVisual 显示方向正确。
- 30x28 LargePatternVisual 使用 visual-only palette 显示近似马赛克图。
- SelectionArea tile 显示颜色通过 visual mapping 对齐到图案主色。
- TempZone slot 显示颜色通过同一 visual mapping 对齐。
- 点击 SelectionArea 后 gameplay 正常推进。
- TempZone progress 正常显示 0/3、1/3、2/3。
- LargePatternVisual 使用 PaletteTarget removal 按 visual palette 色组逐步隐藏。
- 点击后不再表现为明显的矩形大块区域消失。
- WIN 后 LargePatternVisual fully hidden。
- Restart 后 LargePatternVisual fully restored。
- RuntimeInvariantValidator remains active and clean。
- MaxSearchNodes not triggered。

## 当前 visual-only architecture

```text
GameConfig gameplay Pattern / TempZone / SelectionArea
= runtime source of truth

PatternController removed-cell event
→ PatternToLargeVisualBinder
→ LargePatternVisualController
→ visual-only palette cells hidden
```

30x28 LargePatternVisual 不参与：
- GameConfig Pattern。
- PatternCount。
- SelectionArea count。
- TempZone debt。
- RuntimeInvariantValidator。
- DeterministicSolvabilityValidator。
- WIN / LOSE 判断。

## Visual palette

LargePatternVisualConfig 支持 visual-only palette：
- `paletteColors`
- `cellPaletteIndices`
- `cellSize`
- `width`
- `height`

默认目标尺寸：

```text
30 x 28 = 840 visual cells
```

该 840 是 visual cells，不是 gameplay cells。

## GameplayColorVisualMapping

当前引入 visual-only mapping：
- gameplay `BlockColor` 仍然只有 Red / Blue / Green / Yellow / Purple。
- SelectionArea 和 TempZone 内部逻辑仍使用 gameplay BlockColor。
- 显示颜色通过 `GameplayColorVisualMapping` 映射为更接近图片主色的 display colors。
- LargePatternVisual 的 PaletteTarget removal 使用同一 mapping 的 target palette indices。

该 mapping 是 visual-only，不改变 gameplay semantics。

## Removal mode

当前支持：
- Region fallback（旧映射区域隐藏，debug / fallback 用）
- PaletteTarget（当前默认，按 mapped palette target 逐步隐藏）

PaletteTarget 不影响 gameplay resolve，只影响 LargePatternVisual 的隐藏选择。

---

# Image-to-LargePatternVisualConfig Pipeline（当前能力）

菜单路径：

```text
Tools / Eliminate Game / Visual / Generate Large Pattern From Image
```

当前能力：
- 选择 Texture2D 参考图。
- 自动裁剪 / 缩放到 30x28。
- 使用 visual-only fixed palette 生成近似马赛克图。
- 生成 / 更新：

```text
Assets/GameConfigs/Visual/LargePatternVisual_30x28_FromImage.asset
Assets/GameConfigs/Visual/LargePatternVisual_30x28_FromImage_ColorMapping.asset
```

- 支持背景转 None。
- 支持 alpha threshold。
- 支持 visual palette cell indices。
- 支持 top-to-bottom 数据约定与正确显示方向。

限制：
- 该 pipeline 不是 procedural level generation。
- 该 pipeline 不生成 gameplay levels。
- 该 pipeline 不保证商业级美术质量。
- 当前目标是 visual-only 近似马赛克图。

---

# 当前不能做的事

不要声称：
- 30x28 gameplay Pattern 已支持。
- 840 gameplay cells 已支持。
- large-level solver support 已完成。
- procedural generation 已完成。
- multi-level progression 已完成。
- production-ready large-level support 已完成。

不要做：
- 把 30x28 visual cells 放进 GameConfig Pattern。
- 把 840 visual cells 加入 solver。
- 提高 MaxSearchNodes 作为解决方案。
- 关闭 DeterministicSolvabilityValidator。
- 关闭 RuntimeInvariantValidator。
- 改 resolve formula。
- 改 SelectionArea unlock 规则。
- 改 TempZone progress 规则。
- 改 Pattern gravity / collapse 语义。
- 把 gameplay BlockColor 扩展成 16 色作为 visual 解决方案。

---

# 当前阶段结论

当前阶段收口为：

```text
Visual-only large pixel presentation pipeline verified.
Gameplay remains small-pattern driven.
30x28 remains presentation layer only.
```

下一阶段建议：

```text
Visual Polish 1.0
```

目标方向：
- 背景视觉优化。
- 大图位置 / 尺寸优化。
- SelectionArea 布局优化。
- TempZone 位置优化。
- LargePatternVisual 消除动画（fade / scale / particles）。
- UI polish。

不建议下一阶段继续扩大 gameplay Pattern。