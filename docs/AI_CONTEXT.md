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

# 开发原则（必须遵守）

- Docs 是唯一事实源。
- 禁止猜规则。
- 禁止私自修改玩法语义。
- Runtime correctness 优先。
- 稳定性优先于优化。
- 优先最小安全修复。
