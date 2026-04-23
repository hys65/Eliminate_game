# AI_CONTEXT

## 游戏核心（一句话）
玩家点击选区方块 → 进入临时区 → 触发图案底行消除 → 清空图案获胜。

---

## 当前系统划分

### 1. SelectionArea（选区）
- 数据来源：GameConfig.SelectionTiles
- 职责：
  - 显示可点击方块
  - 控制解锁状态（isUnlocked）
  - 点击后发出事件

---

### 2. TempZone（临时区）
- 职责：
  - 接收点击的方块
  - 横向排列（容量限制）
  - 触发 Pattern 检测

---

### 3. Pattern（目标图案）
- 数据来源：GameConfig.PatternRows
- 职责：
  - 维护目标图案结构
  - 检测底行是否匹配
  - 执行消除逻辑

---

### 4. GameManager（调度层）
- 职责：
  - 连接 Selection → TempZone → Pattern
  - 控制流程顺序
  - 管理游戏状态（胜负）

---

## 当前可视化状态

| 系统          | 状态                         |
| ------------- | ---------------------------- |
| SelectionArea | 已可视化（方块）             |
| TempZone      | 已可视化（紧邻方块）         |
| Pattern       | 逻辑已完成，当前开发：可视化 |