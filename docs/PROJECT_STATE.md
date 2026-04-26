# PROJECT_STATE

## 当前阶段
C 阶段目标已达成：Pattern 可视化 / 消除反馈 / 列内重力功能完成并可玩。

---

## 当前已完成（As-Is）
- 三区域布局已建立：Top=Pattern，Middle=TempZone，Bottom=SelectionArea。
- SelectionArea：
  - 仅该区域方块可点击。
  - 初始仅第一行解锁，其余下方行锁定。
  - 点击后执行十字邻居解锁（left/right/up/down）。
- TempZone：
  - 接收已点击方块，作为存储/展示区。
  - 显示进度文本 0/3、1/3、2/3。
  - TMP（TextMeshPro Essentials）已导入且正常。
- Pattern：
  - 顶部目标区，显示/消除用途，不可点击。
  - 无文字标签。
  - 保留 BlockColor.None 结构空槽。
  - 使用全局列对齐与列内重力。
  - 底行匹配消除后仅同列下落，无跨列移动。
  - 被消除单元 ghost 反馈正常。
  - 成功消除相机抖动正常。

---

## 当前稳定循环（已验证）
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
- LOSE：TempZone 已满，且 TempZone 中无颜色匹配当前 Pattern 底行。

---

## 近期回退记录
- PR #31 的 elimination feedback 实现曾导致 TempZone disappearance。
- 该变更已回退。
- 结论：后续消除反馈必须保持纯视觉层，不得影响 resolve / gravity / TempZone 流程。

---

## 下一开发重点
- 优先进行 Win/Lose UI 与状态展示的验证和加固。
- 不调整核心玩法规则与主流程。
