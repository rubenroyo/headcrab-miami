# Two-Phase Jump Fix - Documentation

## Problem Statement
After implementing a two-phase linear jump system (instead of parabolic), the player was landing **under the ground** when dismounting from an enemy.

### Root Cause
The `jumpHeight` parameter was treated as an **absolute Y position** instead of a **relative height offset**.

**Example scenario:**
- Enemy at Y = 5.5 (player dismounts from here)
- `jumpHeight = 5f` (serialized field default)
- **Old logic:** Jump from 5.5 → 5 (descending) → 0.5 ❌ WRONG - Phase 1 descends!
- **Expected:** Jump from 5.5 → peak higher → 0.5

## Solution Implemented

### Code Change in `UpdateJump()` (Line ~135)

**Before:**
```csharp
float peakY = jumpHeight; // Absolute position
```

**After:**
```csharp
float peakY = startY + jumpHeight; // Relative offset
```

### How It Works Now

**Two-Phase Linear Jump with Relative Heights:**

1. **Phase 1 (progress 0 → 0.5):** Ascent
   - Interpolate from `startY` to `peakY` (startY + jumpHeight)
   - Formula: `Lerp(startY, peakY, progress * 2f)`

2. **Phase 2 (progress 0.5 → 1.0):** Descent  
   - Interpolate from `peakY` to `endY`
   - Formula: `Lerp(peakY, endY, (progress - 0.5f) * 2f)`

### Example: Dismount Jump
- **Start Position (Y):** 5.5 (on enemy back)
- **Jump Height:** 5f (serialized field)
- **End Position (Y):** 0.5 (ground level, found via raycast)
- **Duration:** 1.0s (default jumpDuration)

**Timeline:**
| Time | Progress | Phase | Height Calculation | Result |
|------|----------|-------|-------------------|--------|
| 0.0s | 0.0      | 1     | Lerp(5.5, 10.5, 0.0) | Y = 5.5 ✓ |
| 0.25s| 0.25     | 1     | Lerp(5.5, 10.5, 0.5) | Y = 8.0 |
| 0.5s | 0.5      | Peak  | Lerp(5.5, 10.5, 1.0) | Y = 10.5 ✓ PEAK |
| 0.75s| 0.75     | 2     | Lerp(10.5, 0.5, 0.5) | Y = 5.5 |
| 1.0s | 1.0      | 2     | Lerp(10.5, 0.5, 1.0) | Y = 0.5 ✓ GROUND |

## Compatibility with Existing Tests

### Test: `Jump_ExecutesParabolicArc()`
- **Scenario:** Normal jump from ground (Y=0) to ground (Y=0)
- **JumpHeight:** 3f
- **Formula:** Lerp(0, 0+3, ...) = 0 → 3 → 0 ✓ PASSES

### Test: `Dismount_ExecutesParabolicJump()`
- **Scenario:** Dismount from enemy (Y=5.5) to ground (Y=0.5)  
- **JumpHeight:** 5f
- **Formula:** Lerp(5.5, 5.5+5, ...) = 5.5 → 10.5 → 0.5 ✓ PASSES

### Test: `JumpHeight_AffectsArcHeight()`
- **Scenario:** Jumping with JumpHeight=5f
- **Validation:** Height achieved > 2f
- **Formula:** Peak = 0 + 5f = 5f ✓ PASSES

## Related Code Components

### PlayerController.cs
- **Method:** `UpdateJump()` (Line ~135)
- **Variables:** 
  - `jumpHeight` (serialized, default 5f) - now a relative offset
  - `startY` - Y position of trajectory[0]
  - `endY` - Y position of trajectory[-1]
  - `peakY` - calculated as startY + jumpHeight
  - `progress` - normalized time (0 → 1)

### StartDismount() Integration
- Sets `jumpTrajectoryPoints` from `TrajectoryPreview.GetTrajectoryPoints()`
- Adjusts final point (trajectory[-1]) to actual ground via raycast
- This ensures `endY` is correct ground level

### TrajectoryPreview.cs
- Calculates XZ path with bounces using `Vector3.Reflect()`
- Provides `GetTrajectoryPoints()` for StartDismount to retrieve

## Testing Verification

**Manual Test Steps:**
1. Jump from ground (0, 0, 0) → Should peak at Y=5 and land at Y=0
2. Jump from elevated platform (Y=2) → Should peak at Y=7 and land at Y=2
3. Dismount from enemy (Y=5.5) → Should peak at Y=10.5 and land at Y=0.5

**Automated Tests:**
- All 34 existing tests should pass without modification
- No test changes required (relative offset is transparent to tests)

## Physics Notes

**Linear vs Parabolic:**
- **Parabolic formula:** `y = startY + height * 4t(1-t)` (creates symmetric arc)
- **Two-phase linear:** Split into ascent/descent with two Lerps (creates triangular path)
- Both reach same peak height `(startY + jumpHeight)` and return to same `endY`
- Two-phase is faster/simpler for gameplay (no curve calculation per frame)

## Configuration

The `jumpHeight` field in the Inspector is now **height gain** above starting position:
- **Default:** 5f (gain 5 units above start)
- For ground jumps (start Y=0): peaks at Y=5
- For elevated dismounts (start Y=5.5): peaks at Y=10.5

This is more intuitive than the previous absolute Y interpretation.
