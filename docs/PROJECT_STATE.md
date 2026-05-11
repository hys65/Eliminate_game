- # PROJECT_STATE

  # 当前项目状态

  当前版本：
  稳定可玩原型。

  核心 runtime 系统已跑通。

  ---

  # 当前已验证

  ## Gameplay

  - SelectionArea input
  - orthogonal unlock
  - TempZone storage
  - Pattern resolve
  - auto resolve chain
  - gravity
  - collapse

  ---

  ## Resolve Semantics

  - Case A
  - Case B
  - Case B fallback
  - same-color matching
  - TempZone stale cleanup

  ---

  ## Runtime Stability

  - 无已知 resolve deadlock
  - 无已知 index crash
  - runtime assertions 生效
  - count consistency 已验证

  ---

  ## End States

  - WIN
  - LOSE
  - input lock
  - WIN cleanup

  ---

  # 当前可通关规则

  ## 总数量

  PatternCount[color]
  =
  SelectionAreaCount[color] * 3

  ---

  ## 顺序可通关

  已验证：
  - unlock order
  - reachable colors
  - bottom-row progression
  - endgame color availability

  ---

  # 当前已知限制

  尚未 production-ready：

  - polished UI
  - advanced VFX
  - level generator
  - meta progression
  - difficulty balancing

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
  - 擅自改变 Case A / B
  - 私自修改 resolve chain
