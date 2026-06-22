# DEVELOPMENT TASKS

# A. SelectionArea（完成）

- [x] tile generation
- [x] click detection
- [x] first-row unlock
- [x] orthogonal unlock
- [x] SelectionArea only input source
- [x] visual display color can be aligned through `GameplayColorVisualMapping`

Rules locked:
- SelectionArea remains the only player input source.
- SelectionArea tile internal color remains gameplay `BlockColor`.
- Visual display color mapping must not change click / unlock / solvability semantics.
- Diagonal unlock remains forbidden.

---

# B. TempZone（完成）

- [x] tile receive
- [x] storage semantics
- [x] 0/3 progress
- [x] 1/3 progress
- [x] 2/3 progress
- [x] TMP integration
- [x] progress += removedCount
- [x] slot removal at 3/3
- [x] stale color cleanup
- [x] visual display color can be aligned through `GameplayColorVisualMapping`

Rules locked:
- TempZone slot internal color remains gameplay `BlockColor`.
- TempZone `ProgressMark` semantics remain unchanged.
- Visual display color mapping must not change progress behavior.

---

# C. Pattern（完成）

- [x] logical grid
- [x] bottom-row resolve source
- [x] ghost feedback
- [x] column gravity
- [x] collapse
- [x] camera shake
- [x] no cross-column movement
- [x] stable original cell identity for visual sync
- [x] gameplay Pattern visual can be hidden for presentation while logic remains active

Rules locked:
- Pattern logical state remains runtime source of truth.
- Pattern visual hiding must not delete Pattern logical cells.
- PatternController removed-cell events must continue.

---

# D. Resolve System（完成）

- [x] progress-driven resolve semantics
- [x] removeCount formula documented and validated
- [x] same-color resolve
- [x] auto resolve chain（same progress-driven rule）
- [x] stale TempZone cleanup
- [x] invariant-aligned runtime behavior

公式（稳定版）：

```text
removeCount =
  if bottomRowCount < 3:
    bottomRowCount
  else:
    min(bottomRowCount, 3 - currentTempSlotProgress)
```

执行：
- Pattern remove removeCount
- TempZone progress += removeCount
- progress == 3 时移除 slot

禁止：
- 不允许改公式。
- 不允许 visual task 修改 resolve semantics。

---

# E. Runtime Stability（完成）

- [x] index safety fixes
- [x] runtime assertions
- [x] count consistency
- [x] sequence validator
- [x] sequence solvability
- [x] deterministic solvability validation
- [x] runtime deadlock fixes
- [x] RuntimeInvariantValidator remains active during gameplay
- [x] invariant tracking:

  ```text
  PatternRemaining[color]
  =
  SelectionRemaining[color] * 3
  +
  TempDebt[color]

  TempDebt[color]
  =
  sum(3 - TempZoneSlot.ProgressMark for same color)
  ```

Locked:
- Visual palette cells are excluded from RuntimeInvariantValidator.
- Visual palette cells are excluded from DeterministicSolvabilityValidator.
- No hidden auto-fix.
- No silent gameplay mutation.

---

# F. Win/Lose（完成）

- [x] WIN
- [x] LOSE
- [x] input lock
- [x] WIN/LOSE text
- [x] TempZone cleanup after WIN
- [x] LargePatternVisual fully hides all remaining visual cells on WIN
- [x] LargePatternVisual restores visual state on Restart

---

# G. Level Authoring Documentation（完成）

- [x] Level Authoring Guide completed at `docs/level_authoring_guide.md`
- [x] current stable level authoring rules documented
- [x] GameConfig data-only authoring workflow documented
- [x] Editor Validation workflow documented
- [x] committed level content must pass Editor Validation before commit
- [x] stable baseline workflow documented for duplicating known-good GameConfig assets
- [x] visual-only LargePatternVisual restrictions documented

Level production must follow:

```text
docs/level_authoring_guide.md
```

Before committing any new level or edited level, run Editor Validation from the Unity menu:

```text
Tools / Eliminate Game / Validate Current Config
```

The Unity Console must show:

```text
[EDITOR_VALIDATION] PASS
```

New level commits are not allowed unless Editor Validation passes.

After Editor Validation passes, the level must be Play tested to WIN.

The verified Play Mode run must have:

```text
Console red errors = 0
```

---

# H. Level_001 Stable Baseline（完成）

Current stable baseline asset:

```text
Assets/GameConfigs/Levels/Level_001_GameConfig.asset
```

Verified state:
- [x] `Level_001_GameConfig` exists under `Assets/GameConfigs/Levels/`
- [x] `GameManager` uses `Level_001_GameConfig`
- [x] `Level_001` passes Editor Validation
- [x] `Level_001` has been Play tested to WIN
- [x] verified Play Mode run had `Console red errors = 0`
- [x] `RuntimeInvariantValidator` remains active during valid gameplay

Level_001 is the stable baseline for future authored levels.

---

# I. Level_002 Safe Small Prototype（完成）

Current verified small expansion prototype asset:

```text
Assets/GameConfigs/Levels/Level_002_GameConfig.asset
```

Verified by user:
- [x] `Level_002_GameConfig` exists under `Assets/GameConfigs/Levels/`
- [x] Green color was added
- [x] Unity compile has no red errors
- [x] Editor Validation passed
- [x] Play Mode has no red errors
- [x] `Level_002` remains within temporary safe prototype limits
- [x] `Level_002` is data-only authored `GameConfig` content
- [x] `Level_002` is the first verified small expansion prototype

Current Level_002 limits and counts:
- Pattern non-None cells = 27
- SelectionArea tiles = 9
- Pattern non-None cells <= 45
- SelectionArea tiles <= 15
- Pattern Red = 6, Blue = 6, Green = 3, Yellow = 6, Purple = 6
- SelectionArea Red = 2, Blue = 2, Green = 1, Yellow = 2, Purple = 2
- `PatternCount[color] = SelectionAreaCount[color] * 3` holds for all colors

Scope:
- `Level_002` is not a large-level support milestone.
- `Level_002` is not the new large-level baseline.
- `Level_002` is not procedural generation.
- `Level_002` is not multi-level progression.

---

# J. Level_003 Verified Small Safe Prototype（完成）

Current verified second small expansion prototype asset:

```text
Assets/GameConfigs/Levels/Level_003_GameConfig.asset
```

Verified by user after the simplified 36 / 12 fix:
- [x] `Level_003_GameConfig` exists under `Assets/GameConfigs/Levels/`
- [x] Editor Validation = PASS
- [x] Play Mode result = WIN
- [x] Play Mode initializes visible gameplay content
- [x] MaxSearchNodes error no longer appears
- [x] Menu -> Restart = normal
- [x] Restart after Menu shows gameplay content again
- [x] Console red errors = 0
- [x] `Level_003` is data-only authored `GameConfig` content
- [x] `Level_003` is the second verified small expansion prototype after `Level_002`

Current Level_003 verified design:
- Pattern non-None cells = 36
- SelectionArea tiles = 12
- Pattern dimensions = 6 rows x 6 columns
- SelectionArea dimensions = 4 columns x 3 rows
- 4 colors only
- Purple is not used
- Red remains dominant or tied-dominant
- Pattern is visually mixed
- Pattern is no longer 14x3
- Pattern is no longer uniform same-color rows
- Pattern fits screen better than the rejected 14x3 version
- Pattern Red = 12, Blue = 9, Green = 9, Yellow = 6, Purple = 0
- SelectionArea Red = 4, Blue = 3, Green = 3, Yellow = 2, Purple = 0
- `PatternCount[color] = SelectionAreaCount[color] * 3` holds for all colors

Rejected Level_003 history:
- Rejected attempt used Pattern non-None cells = 42
- Rejected attempt used SelectionArea tiles = 14
- Rejected attempt used 4 colors and no Purple
- Rejected attempt exceeded `MaxSearchNodes = 200000` during deterministic solvability validation

Observed error:

```text
[SOLVABILITY_VALIDATION][GameManager.StartRun] FAILED
Solvability search exceeded MaxSearchNodes=200000.
This level may be too complex for deterministic validation.
```

Fix record:
- `Level_003` was simplified to Pattern non-None cells = 36
- `Level_003` was simplified to SelectionArea tiles = 12
- The fix was data-only
- Validation was not weakened
- `MaxSearchNodes` was not increased
- `DeterministicSolvabilityValidator` was not bypassed

Scope:
- `Level_003` is a small safe prototype.
- `Level_003` is not a large-level support milestone.
- `Level_003` is not the new large-level baseline.
- `Level_003` is not procedural generation.
- `Level_003` is not multi-level progression.
- `Level_003` does not prove production-ready scaling.

Temporary scaling warnings remain active:
- Do not create oversized gameplay levels yet.
- Do not exceed Pattern non-None cells <= 45.
- Do not exceed SelectionArea tiles <= 15.
- Large-level gameplay support is still not production-ready.
- Random-looking mixed layouts can still explode deterministic solvability search complexity.

---

# K. LargePatternVisual Gameplay Sync Prototype（完成 / Verified）

Milestone name:

```text
LargePatternVisual Gameplay Sync Prototype
```

Verified by user:
- [x] Console red errors = 0
- [x] Clicking SelectionArea tiles causes regions of the 30x28 LargePatternVisual to disappear
- [x] Original gameplay Pattern still resolves normally
- [x] Level reaches WIN
- [x] On WIN, the LargePatternVisual fully disappears
- [x] Menu -> Restart works
- [x] Restart restores the LargePatternVisual fully
- [x] RuntimeInvariantValidator remains active and clean
- [x] RuntimeInvariantValidator no longer reports errors
- [x] MaxSearchNodes is not triggered

Scope:
- [x] visual-only 30x28 large pixel wall
- [x] driven by small gameplay Pattern removals
- [x] does not increase solver complexity
- [x] does not modify gameplay semantics
- [x] does not increase MaxSearchNodes
- [x] keeps RuntimeInvariantValidator active

Runtime source of truth remains:

```text
GameConfig gameplay Pattern
TempZone
SelectionArea
```

---

# L. Image-to-LargePatternVisualConfig Pipeline（完成 / Verified）

Milestone group:

```text
Image-to-LargePatternVisualConfig Pipeline 1.0
Visual Palette Expansion 1.0
LargePatternVisual Vertical Orientation Fix 1.0
```

Verified by user:
- [x] Unity compile has no red errors
- [x] menu exists at `Tools / Eliminate Game / Visual / Generate Large Pattern From Image`
- [x] source image can generate 30x28 visual config
- [x] visual config uses visual-only palette data
- [x] generated image orientation displays correctly
- [x] output remains visual-only

Current generated assets:

```text
Assets/GameConfigs/Visual/LargePatternVisual_30x28_FromImage.asset
Assets/GameConfigs/Visual/LargePatternVisual_30x28_FromImage_ColorMapping.asset
```

Rules:
- [x] 30x28 remains visual-only
- [x] 840 cells are visual cells, not gameplay cells
- [x] generated config does not enter solver
- [x] generated config does not enter RuntimeInvariantValidator
- [x] generated config does not decide WIN / LOSE

---

# M. Visual Palette Expansion 1.0（完成 / Verified）

Purpose:
- Replace extremely limited LargePatternVisual color approximation with visual-only fixed palette support.
- Support broader cartoon/image colors such as black, white, gray, pink, peach, cream, browns, red, orange, yellow, green, teal, blues, purple.

Verified by user:
- [x] LargePatternVisual displays closer image-derived mosaic colors
- [x] monkey source image no longer collapses into only Red / Blue / Green / Yellow / Purple
- [x] gameplay BlockColor was not expanded to 16 gameplay colors
- [x] visual palette remains presentation-only

Rules:
- visual palette does not modify gameplay BlockColor semantics
- visual palette does not modify PatternCount
- visual palette does not modify SelectionAreaCount
- visual palette does not modify TempZoneDebt

---

# N. Presentation Alignment 1.0（完成 / Verified）

Purpose:
- Hide left gameplay Pattern visual by default while keeping Pattern logical data active.
- Make player focus on the 30x28 LargePatternVisual.

Verified by user:
- [x] left gameplay Pattern visual is hidden
- [x] gameplay still progresses
- [x] PatternController logic and removed-cell events still work
- [x] Console red errors = 0

Rules:
- Pattern visual hiding is presentation-only
- Pattern logical data remains runtime source of truth
- Pattern visual hiding does not change resolve, gravity, collapse, or win/lose

---

# O. Visual Interaction Alignment 1.0（完成 / Verified）

Purpose:
- Align SelectionArea / TempZone display colors with LargePatternVisual main palette groups.
- Use PaletteTarget removal so clicks visually remove related color groups instead of obvious rectangular regions.

Completed:
- [x] `GameplayColorVisualMapping` added as visual-only mapping
- [x] SelectionArea display colors can use mapping display colors
- [x] TempZone display colors can use mapping display colors
- [x] PatternToLargeVisualBinder supports PaletteTarget removal
- [x] Region removal remains fallback/debug
- [x] LargePatternVisualController tracks palette indices for visual-only hiding

Verified by user:
- [x] SelectionArea colors changed from pure Red / Blue / Green / Yellow to image-related display colors
- [x] TempZone display color aligns with clicked visual color
- [x] clicking SelectionArea still triggers gameplay
- [x] LargePatternVisual hides palette-targeted cells instead of a clear rectangle region
- [x] Console red errors = 0

Rules:
- internal gameplay colors remain gameplay `BlockColor`
- display mapping is visual-only
- PaletteTarget removal is visual-only
- WIN still depends on gameplay Pattern being empty
- Restart restores visual state

---

# P. Current stage（收口）

Current stage closed as:

```text
Visual-only large pixel presentation pipeline verified.
Gameplay remains small-pattern driven.
30x28 remains presentation layer only.
```

Validated user-visible state:
- 30x28 mosaic image appears correctly oriented
- gameplay Pattern visual hidden
- SelectionArea visual colors mapped to image palette groups
- TempZone visual colors mapped to same groups
- PaletteTarget visual removal works
- Console red errors = 0
- RuntimeInvariantValidator clean
- MaxSearchNodes not triggered

---

# Q. Next stage（未开始）

Recommended next milestone:

```text
Visual Polish 1.0
```

Suggested tasks:
- [ ] background visual polish
- [ ] LargePatternVisual positioning and scale polish
- [ ] SelectionArea layout polish
- [ ] TempZone layout polish
- [ ] fade / scale hide animation for LargePatternVisual cells
- [ ] UI polish
- [ ] more suitable source-image preprocessing workflow

Not recommended next:
- do not enlarge gameplay Pattern
- do not add 840 cells into gameplay
- do not increase MaxSearchNodes as scaling fix

---

# R. Hard claims still forbidden

Do not claim:
- 30x28 gameplay Pattern exists
- 840 gameplay cells are supported
- large-level solver support exists
- procedural generation exists
- multi-level progression exists
- production-ready large-level support exists

Do not do:
- make 30x28 visual pixels into gameplay cells
- bypass deterministic validation
- disable RuntimeInvariantValidator
- modify progress-driven resolve formula
- modify SelectionArea unlock semantics
- modify Pattern gravity semantics
- add cross-column Pattern movement
- mutate runtime gameplay rules for presentation