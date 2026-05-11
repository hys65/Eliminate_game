- # ARCHITECTURE

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
  → Pattern resolve
  → gravity
  → collapse
  → auto resolve chain
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

  ---

  ## TempZone

  职责：
  - 解析暂存
  - progress tracking
  - Case A / B 数据管理

  规则：
  - 非输入层
  - 不负责 Pattern gravity
  - 不负责点击

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

  # Resolve Semantics

  ## Case A

  条件：
  - 底行同色数量 < 3
  或
  - Case B 条件不足

  行为：
  - 消除 Pattern 单元
  - 增加 TempZone progress

  ---

  ## Case B

  条件：
  - 底行同色数量 >= 3
  并且
  - TempZone 同色数量 >= 3

  行为：
  - 消除 3 个 Pattern 单元
  - 消除 3 个 TempZone 单元

  ---

  # Fallback Rule（关键）

  Case B 条件不足：
  必须 fallback 到 Case A。

  禁止：
  - resolve chain deadlock
  - 卡死

  ---

  # Auto Resolve Chain

  Resolve 后必须：

  - 获取新底行
  - 查找 TempZone 可匹配颜色
  - 自动继续 resolve
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
