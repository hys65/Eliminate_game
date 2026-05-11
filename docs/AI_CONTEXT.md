- # AI_CONTEXT

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
  - TempZone 使用：
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

  # Resolve 规则（当前稳定版）

  ## Case A

  条件：
  - 当前底行同色数量 < 3
  或
  - Case B 条件不足

  行为：
  - 消除当前匹配 Pattern 单元
  - TempZone 增加 progress
  - TempZone slot 保留

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

  ## Case B fallback 规则（重要）

  如果：
  - 底行同色 >= 3
  但
  - TempZone 数量不足 3

  必须：
  → fallback 到 Case A

  禁止：
  - resolve chain 卡死
  - 直接中断解析

  ---

  # 自动解析链（Auto Resolve Chain）

  玩家点击后：

  SelectionArea click
  → tile enters TempZone
  → resolve selected color
  → gravity
  → collapse
  → 获取新的底行
  → 自动继续解析 TempZone 中可匹配颜色
  → 直到不存在可匹配颜色

  ---

  # 可通关规则（必须保持）

  ## 总数量规则

  对于每个颜色：

  PatternCount[color]
  =
  SelectionAreaCount[color] * 3

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
  并且
  - TempZone 无颜色匹配当前 Pattern 底行

  行为：
  - 停止输入
  - 显示 LOSE

  ---

  # 当前稳定状态（已验证）

  已验证：
  - Same-color resolve
  - Auto resolve chain
  - Case A
  - Case B
  - Case B fallback
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
