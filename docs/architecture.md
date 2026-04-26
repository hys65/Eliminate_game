# ARCHITECTURE

## 总体结构

GameManager
 ├── SelectionAreaController
 ├── TempZoneController
 └── PatternController

---

## 三区域空间布局（运行时）
- Top: Pattern（目标区）
- Middle: TempZone（暂存区）
- Bottom: SelectionArea（输入区）

---

## 数据流（稳定版本）
SelectionTile (点击，SelectionArea only)
    ↓
SelectionAreaController
    ↓ (事件)
GameManager
    ↓
TempZoneController（入列 + 进度文本更新）
    ↓
PatternController（底行匹配/消除 + ghost 反馈 + 列内下落）
    ↓
GameManager（胜负判定 + 成功消除触发相机抖动）

---

## 核心流程（当前已完成）
1. 玩家点击 SelectionArea 可点击方块。
2. 触发 SelectionArea 解锁扩展（十字邻居：left/right/up/down）。
3. GameManager 接收事件并将方块送入 TempZone。
4. TempZone 更新容量进度文本（0/3、1/3、2/3）。
5. PatternController 检测并解析 Pattern 底行匹配。
6. 匹配成功时：
   - 底行相关单元消除（保留 BlockColor.None 结构槽语义）
   - 显示 removed-cell ghost 反馈
   - 执行同列重力下落（仅目标列移动）
   - 触发相机抖动
7. GameManager 按规则更新胜负状态。

---

## 模块职责边界（保持不变）
- SelectionArea：
  - 负责输入与解锁状态。
  - 不直接操作 Pattern 消除。
- TempZone：
  - 负责暂存与进度显示。
  - 不是输入源，不负责消除逻辑。
- Pattern：
  - 负责显示、匹配、消除、列内重力与视觉反馈。
  - 不处理点击输入。
- GameManager：
  - 唯一调度入口。
  - 负责串联 Selection → TempZone → Pattern 与胜负判定。

---

## 胜负规则（当前实现）
- WIN：Pattern 为空。
- LOSE：TempZone 已满，且 TempZone 无颜色匹配当前 Pattern 底行。

---

## 已知回退约束
- PR #31 的一次消除反馈实现曾破坏 TempZone（出现消失问题）并已回退。
- 后续反馈层改动必须是纯视觉，不得影响 resolve / gravity / TempZone 流程。
