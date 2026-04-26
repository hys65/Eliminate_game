# DEVELOPMENT TASKS

## 阶段划分（按当前项目状态）

### A. Selection Area（完成）
- [x] 方块生成
- [x] 点击检测（仅 SelectionArea 可点击）
- [x] 初始解锁：仅第一行
- [x] 点击后十字邻居解锁（left/right/up/down）

---

### B. TempZone（完成）
- [x] 接收 SelectionArea 方块
- [x] 存储/展示定位（非输入源）
- [x] 进度文本：0/3、1/3、2/3
- [x] TextMeshPro Essentials 导入并稳定工作

---

### C. Pattern 可视化 / 消除 / 重力（完成）
- [x] Top 区域可视化显示
- [x] 保留 BlockColor.None 结构槽
- [x] 全局列对齐
- [x] 底行匹配消除
- [x] removed-cell ghost 反馈
- [x] 同列重力下落（仅目标列移动，无跨列）
- [x] 成功消除触发相机抖动

---

### D. 游戏循环稳定性（完成）
- [x] 稳定循环：Selection 点击 → TempZone 入列 → Pattern 解析 → 反馈 → 下落 → 相机抖动
- [x] WIN：Pattern 为空
- [x] LOSE：TempZone 满且无底行可匹配颜色

---

### E. 下一阶段（进行中）
- [ ] 胜负 UI 可视状态核对（Win/Lose 展示一致性）
- [ ] 状态提示文本强化（不改变核心规则）
- [ ] 回归验证（确保视觉反馈不干扰 resolve/gravity/TempZone）

---

## 风险与约束（必须遵守）
- PR #31 曾因消除反馈实现导致 TempZone 消失，已回退。
- 后续“消除反馈”改动必须保持纯视觉层，不得中断主流程。
