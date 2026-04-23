# ARCHITECTURE

## 总体结构

GameManager
 ├── SelectionAreaController
 ├── TempZoneController
 └── PatternController

---

## 数据流

SelectionTile (点击)
    ↓
SelectionAreaController
    ↓ (事件)
GameManager
    ↓
TempZoneController
    ↓
PatternController

---

## 核心流程

1. 玩家点击 SelectionTile
2. GameManager 接收事件
3. Tile 进入 TempZone
4. PatternController 检查底行
5. 若匹配 → 消除 Pattern 底行
6. 更新 TempZone / Pattern 状态

---

## 重要约束

- SelectionArea 不直接操作 Pattern
- TempZone 不负责消除逻辑
- Pattern 不处理输入
- GameManager 是唯一调度入口