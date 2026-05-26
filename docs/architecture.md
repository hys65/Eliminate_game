# ARCHITECTURE

# 总体结构

GameManager
 ├── SelectionAreaController
 ├── TempZoneController
 └── PatternController

---

# 三层结构

Top:
- Pattern

Middle:
- TempZone

Bottom:
- SelectionArea

---

# Runtime Flow（稳定版）

Selection click
→ TempZone add
→ Pattern resolve（progress-driven）
→ gravity
→ collapse
→ auto resolve chain（same progress-driven rule）
→ win/lose check

---

# 模块职责

## SelectionArea

职责：
- 玩家输入
- 解锁逻辑
- tile 提供

规则：
- 仅输入层
- 不直接操作 Pattern
- orthogonal unlock only

---

## TempZone

职责：
- 解析暂存
- progress tracking
- slot 生命周期管理

规则：
- 非输入层
- 不负责 Pattern gravity
- 不负责点击
- slot progress 到 3/3 后移除

---

## Pattern

职责：
- logical grid
- visual grid
- bottom-row query
- resolve
- gravity
- collapse

规则：
- column gravity only
- no cross-column movement
- logical state 是 runtime source of truth

---

## GameManager

职责：
- 唯一调度入口
- 串联：
  Selection
  → TempZone
  → Pattern
- 管理 resolve chain
- 管理 win/lose

---

# Resolve Semantics（Progress-Driven）

核心：
Resolve amount 由 TempZone 同色 slot 的剩余容量决定。

定义：
- currentTempSlotProgress：当前同色 slot progress（0/1/2）
- bottomRowCount：底行同色可匹配数量

公式：

removeCount =
  if bottomRowCount < 3:
    bottomRowCount
  else:
    min(bottomRowCount, 3 - currentTempSlotProgress)

执行规则：
- Pattern 移除 removeCount。
- TempZone slot progress += removeCount。
- progress 达到 3/3 时，slot 完成并移除。

说明：
- 不再使用旧 Case A / Case B 作为主语义分支。
- auto resolve chain 每一步都重复同一 progress-driven 规则。

---

# Runtime Invariant

对任意颜色：

PatternRemaining[color]
=
SelectionRemaining[color] * 3
+
TempDebt[color]

其中：

TempDebt[color]
=
sum(3 - TempZoneSlot.ProgressMark for same color)

该不变量在单步 resolve 与 auto resolve chain 过程中都必须保持。

---

# Auto Resolve Chain

Resolve 后必须：

- 获取新底行
- 查找 TempZone 可匹配颜色
- 自动继续 resolve
- 使用同一 progress-driven 规则
- 直到不存在匹配

---

# Runtime Safety

允许：
- Debug.Assert
- Runtime validation
- Deterministic logs

禁止：
- Hidden auto-fix
- Silent gameplay mutation
- Runtime rule rewriting

---

# 可通关规则

## 总数量规则

PatternCount[color]
=
SelectionAreaCount[color] * 3

---

## 顺序可通关

SelectionArea 解锁顺序
必须支持：
- Pattern 推进
- 底行变化
- 后期颜色可达

---

# 现有能力边界（文档对齐）

- deterministic solvability validator：已接入
- LevelDatabase：若当前项目已接入，则保持支持语义
- restart/menu：若当前项目已接入，则保持支持语义
