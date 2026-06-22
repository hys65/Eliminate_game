# ARCHITECTURE

# 总体结构

```text
GameManager
 ├── SelectionAreaController
 ├── TempZoneController
 └── PatternController

Visual-only presentation layer:
 LargePatternVisualController
   ↑
 PatternToLargeVisualBinder
   ↑
 PatternController removed-cell event

Visual-only color alignment:
 GameplayColorVisualMapping
   ├── SelectionArea display color
   ├── TempZone display color
   └── LargePatternVisual palette target removal
```

---

# 三层 gameplay 结构

Top:
- Pattern

Middle:
- TempZone

Bottom:
- SelectionArea

Runtime source of truth remains only:

```text
GameConfig gameplay Pattern
TempZone
SelectionArea
```

LargePatternVisual is not runtime source of truth.

---

# Runtime Flow（稳定版）

```text
Selection click
→ TempZone add
→ Pattern resolve（progress-driven）
→ gravity
→ collapse
→ auto resolve chain（same progress-driven rule）
→ win/lose check
```

Visual-only side route:

```text
Gameplay Pattern cell removed
→ PatternController emits removed-cell event
→ PatternToLargeVisualBinder receives PatternRemovedCell
→ Binder chooses visual removal mode
→ LargePatternVisualController hides visual-only cells
```

Current default visual removal:

```text
PaletteTarget removal
```

Fallback / debug visual removal:

```text
Region removal
```

---

# 模块职责

## SelectionArea

职责：
- 玩家输入。
- 解锁逻辑。
- tile 提供。

Gameplay rules：
- 仅输入层。
- 不直接操作 Pattern。
- orthogonal unlock only。
- 不允许 diagonal unlock。
- tile internal color remains gameplay `BlockColor`。

Visual presentation：
- SelectionArea tile display color may use `GameplayColorVisualMapping`。
- This display mapping is visual-only。
- It must not change tile internal `BlockColor`。
- It must not change click / unlock / solvability semantics。

---

## TempZone

职责：
- 解析暂存。
- progress tracking。
- slot 生命周期管理。

Gameplay rules：
- 非输入层。
- 不负责 Pattern gravity。
- 不负责点击。
- slot progress 到 3/3 后移除。
- slot internal color remains gameplay `BlockColor`。

Visual presentation：
- TempZone slot display color may use `GameplayColorVisualMapping`。
- This display mapping is visual-only。
- It must not change slot `Color` or `ProgressMark`。

---

## Pattern

职责：
- logical grid。
- bottom-row query。
- resolve。
- gravity。
- collapse。
- removed-cell event source。

Rules：
- Pattern logical state is runtime source of truth。
- column gravity only。
- no cross-column movement。
- bottom-row is resolve source。
- Pattern cells are not player input。

Presentation：
- Gameplay Pattern visual can be hidden for presentation alignment。
- Hiding Pattern visual must not remove Pattern logical data。
- Hiding Pattern visual must not stop PatternController events。
- Debug visibility can be restored if needed。

---

## LargePatternVisual（Verified visual-only presentation layer）

Milestone group:

```text
LargePatternVisual Gameplay Sync Prototype
Visual Palette Expansion 1.0
Presentation Alignment 1.0
Visual Interaction Alignment 1.0
LargePatternVisual Vertical Orientation Fix 1.0
```

Status:

```text
Verified / current visual presentation milestone closed
```

职责：
- visual-only 30x28 large pixel wall。
- display image-derived mosaic pattern using visual-only palette。
- receive one-way visual sync from gameplay Pattern removal events。
- hide visual-only cells using PaletteTarget removal by default。
- fully hide remaining visual cells on WIN。
- fully restore visual cells on Restart。

Rules：
- 30x28 LargePatternVisual is not gameplay logic。
- Runtime source of truth remains GameConfig gameplay Pattern, TempZone, and SelectionArea。
- 30x28 LargePatternVisual does not participate in DeterministicSolvabilityValidator。
- 30x28 LargePatternVisual does not participate in RuntimeInvariantValidator。
- 30x28 LargePatternVisual does not participate in PatternCount。
- 30x28 LargePatternVisual does not participate in SelectionArea count。
- 30x28 LargePatternVisual does not participate in TempZone debt。
- 30x28 LargePatternVisual does not decide WIN / LOSE。

Current visual config data:
- `width`
- `height`
- `cellSize`
- `paletteColors`
- `cellPaletteIndices`

Default size:

```text
30 x 28 = 840 visual cells
```

The 840 cells are visual cells only, not gameplay cells.

Coordinate convention:
- LargePatternVisualConfig stores image data in top-to-bottom order。
- `index = y * width + x`。
- `y = 0` represents top row of source image data。
- LargePatternVisualController renders this orientation correctly in Unity view。

---

# GameplayColorVisualMapping（visual-only）

Purpose:
- Map gameplay `BlockColor` to presentation colors and LargePatternVisual palette targets。

It can provide:
- display color for SelectionArea tile visuals。
- display color for TempZone slot visuals。
- target palette indices for LargePatternVisual PaletteTarget removal。

Rules:
- gameplay `BlockColor` enum remains gameplay token set。
- Mapping does not expand gameplay to 16 colors。
- Mapping does not change SelectionArea tile internal color。
- Mapping does not change TempZone slot internal color。
- Mapping does not affect PatternCount or RuntimeInvariantValidator。

Example conceptual mapping:

```text
BlockColor.Red    -> brown display / brown-ish palette targets
BlockColor.Blue   -> green display / green-ish palette targets
BlockColor.Green  -> cream display / cream-white palette targets
BlockColor.Yellow -> pink display / pink-yellow-red targets
BlockColor.Purple -> dark outline display / black-purple-dark-blue targets
```

---

# PatternToLargeVisualBinder

Responsibilities:
- Subscribe to PatternController removed-cell events。
- Subscribe to GameManager run lifecycle for reset/win sync。
- Forward visual-only removal commands to LargePatternVisualController。

Stable coordinate mapping for Region fallback:
- Current row / column mapping is not stable because Pattern uses bottom-row resolve, column gravity, and collapse。
- PatternCell stores stable original identity: `OriginalRow` and `OriginalColumn`。
- PatternRemovedCell exposes `OriginalRow`, `OriginalColumn`, `CurrentRow`, `CurrentColumn`, and `Color`。
- Region fallback maps LargePatternVisual regions using `OriginalRow` and `OriginalColumn`。
- Ghost effects and current-position visuals use `CurrentRow` and `CurrentColumn`。

Current default removal mode:

```text
PaletteTarget
```

PaletteTarget behavior:
- For each removed gameplay cell, use removed cell `Color`。
- Query `GameplayColorVisualMapping` target palette indices。
- Hide deterministic visible cells in LargePatternVisual whose palette index belongs to target set。
- If no target cells remain, safe deterministic fallback can be used。
- WIN still calls HideAllCells。
- Restart still calls ResetVisualState。

Region behavior:
- Kept as fallback/debug。
- One gameplay cell maps to a region of 30x28 visual pixels。

---

## GameManager

职责：
- 唯一调度入口。
- 串联 Selection → TempZone → Pattern。
- 管理 resolve chain。
- 管理 win/lose。
- Emits run lifecycle events used by visual-only layer。

Rules:
- GameManager remains gameplay orchestrator。
- Visual-only layer must not make GameManager depend on 30x28 cells as gameplay data。

---

# Resolve Semantics（Progress-Driven）

核心：
Resolve amount 由 TempZone 同色 slot 的剩余容量决定。

定义：
- `currentTempSlotProgress`：当前同色 slot progress（0/1/2）
- `bottomRowCount`：底行同色可匹配数量

公式：

```text
removeCount =
  if bottomRowCount < 3:
    bottomRowCount
  else:
    min(bottomRowCount, 3 - currentTempSlotProgress)
```

执行规则：
- Pattern 移除 removeCount。
- TempZone slot progress += removeCount。
- progress 达到 3/3 时，slot 完成并移除。

说明：
- 不再使用旧 Case A / Case B 作为主语义分支。
- auto resolve chain 每一步都重复同一 progress-driven 规则。
- visual-only systems must not alter this formula。

---

# Runtime Invariant

对任意 gameplay BlockColor：

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

该不变量在单步 resolve 与 auto resolve chain 过程中都必须保持。

LargePatternVisual visual palette cells are excluded from this invariant.

---

# Auto Resolve Chain

Resolve 后必须：
- 获取新底行。
- 查找 TempZone 可匹配颜色。
- 自动继续 resolve。
- 使用同一 progress-driven 规则。
- 直到不存在匹配。

---

# Runtime Safety

允许：
- Debug.Assert。
- Runtime validation。
- Deterministic logs。
- Visual-only mapping and presentation alignment。

禁止：
- Hidden auto-fix。
- Silent gameplay mutation。
- Runtime rule rewriting。
- Disabling RuntimeInvariantValidator。
- Increasing MaxSearchNodes as workaround。

---

# 可通关规则

## 总数量规则

```text
PatternCount[color]
=
SelectionAreaCount[color] * 3
```

## 顺序可通关

SelectionArea 解锁顺序必须支持：
- Pattern 推进。
- 底行变化。
- 后期颜色可达。

---

# 现有能力边界（文档对齐）

已接入 / 已验证：
- deterministic solvability validator。
- RuntimeInvariantValidator。
- visual-only 30x28 large mosaic display。
- visual-only palette config。
- visual-only image-to-config generator。
- gameplay-color-to-visual-color mapping。
- PaletteTarget visual removal。
- gameplay Pattern visual hiding for presentation。

未完成 / 不得声称：
- 30x28 gameplay Pattern。
- 840 gameplay cells support。
- large-level solver support。
- procedural generation。
- multi-level progression。
- production-ready large-level support。

---

# 下一阶段建议

```text
Visual Polish 1.0
```

目标：
- 背景视觉优化。
- LargePatternVisual 居中 / 尺寸优化。
- SelectionArea layout polish。
- TempZone layout polish。
- LargePatternVisual hide animation（fade / scale / particles）。
- UI polish。

下一阶段不应继续扩大 gameplay Pattern。