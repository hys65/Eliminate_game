# AI_CONTEXT

## 游戏核心（一句话）
玩家点击 SelectionArea 方块 → 进入 TempZone → 触发 Pattern 底行消除与列内下落 → 清空 Pattern 获胜。

---

## 当前三大区域布局（已建立）
- Top = Pattern
- Middle = TempZone
- Bottom = SelectionArea

---

## 当前系统状态（已完成）

### 1. SelectionArea（选区）
- 仅 SelectionArea 方块可点击。
- 初始解锁：仅第一行解锁。
- 其余下方行初始均锁定。
- 点击后保持十字邻居解锁规则：left / right / up / down（存在则解锁）。
- 该解锁规则为当前项目的明确保留规则。

### 2. TempZone（临时区）
- 接收来自 SelectionArea 的已点击方块。
- 定位为存储/展示区，不是输入源。
- 显示进度文本：0/3、1/3、2/3。
- TextMeshPro Essentials 已导入，TMP 文本工作正常。

### 3. Pattern（目标区）
- 顶部目标区域，仅用于显示/消除，不可点击。
- Pattern 内不显示文字标签。
- 保留 BlockColor.None 结构空槽。
- 使用全局列对齐。
- 使用列内重力：底行匹配消除后，仅同列上方方块下落。
- 下落动画已正确：仅目标列移动，无跨列下落。
- 已具备被消除单元 ghost 反馈。
- 成功消除时相机抖动已生效。

---

## 当前稳定玩法循环（已验证）
SelectionArea click
→ tile enters TempZone
→ Pattern bottom row resolves
→ TempZone progress updates
→ Pattern removed cells show feedback
→ same-column gravity fall occurs
→ camera shake triggers on successful elimination

---

## 胜负规则（当前保留）
- WIN：Pattern 为空。
- LOSE：TempZone 已满，且 TempZone 中不存在可匹配当前 Pattern 底行的颜色。

---

## 开发状态
- 核心玩法循环可玩。
- C. Pattern 可视化 / 重力 / 反馈：功能完成。
- 下一步应聚焦：胜负 UI 与状态展示的验证与加固；不改核心规则。

---

## 近期变更记录（风险提示）
- PR #31 的消除反馈实现曾导致 TempZone 消失，已回退。
- 后续做消除反馈时必须保持“纯视觉层”实现，不得打断 resolve / gravity / TempZone 主流程。
