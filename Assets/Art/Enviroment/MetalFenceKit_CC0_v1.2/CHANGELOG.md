# Changelog

## 1.2 — 2026-07-28

Double swing gate. Purely additive — everything from 1.0 and 1.1 is unchanged.

**Added**

- `DoubleGate.glb` — double swing gate, both leaves animated in one clip
  (`Open`, 2 × 95°, 63 frames @ 24 fps). Clear opening **4.00 m**, which is
  deliberately the same as the sliding gate's: the two are interchangeable at
  the same location.
- Centre drop bolt with ground socket, heavier 120 mm gate posts.
- `previews/doublegate_closed.png`, `doublegate_open.png`,
  `DoubleGate_Open.gif`, `DoubleGate_Open.mp4`.

**A construction decision worth knowing about**

The right leaf is a **genuinely mirrored body**, not the left one rotated 180°.

Sharing one mesh between both leaves looked attractive — it would have saved a
whole texture set. It does not work here: the drop bolt sits on the outer face
and makes the leaf asymmetric in Y. Rotated by 180° the two leaves end up
30 mm out of plane, with their bolts on opposite faces. Measured before it was
built, not after.

Mirroring costs a second texture set and buys a clean API: **closed is 0° for
both leaves**, open is −95° and +95°. The mirror is done in bmesh with the face
winding reversed, so the normals stay outward — verified via signed volume,
identical and positive for both leaves.

**Verified before release**

- Both leaves clear their posts across the full 95° swing, and each other in
  the closed position. Checked against real vertices in plan, per leaf.
- Both leaves sit in the same plane (world Y −0.154 … −0.064) with a 16 mm
  centre gap.
- `DoubleGate.glb` carries one clip `Open` with channels on **both** leaves —
  the glTF exporter groups by NLA track name, and without a shared track it
  writes one clip per object.

---

## 1.1 — 2026-07-28

Corner support and run adjustment. No changes to the existing four modules —
1.0 files remain valid, this is purely additive.

**Added**

- `Fence_Panel_1m.glb` — 1 m half panel, for runs that are not a multiple of
  two metres. The bar pitch is distributed from the panel centre, so the half
  panel has no clipped edge bay.
- `Fence_Post_Corner.glb` — heavier post (100 mm instead of 80 mm) with a
  larger foundation, for corners and run ends.
- Two corner renders and two animated GIFs in `previews/`.

**Corrected in the documentation**

Version 1.0 listed "no corner variant, overlap two posts for now" as a
limitation. That was wrong. A 90° corner works with the modules as shipped:
place a post at the corner, run one panel into it, rotate the next panel 90°
about Z. Measured at the joint:

- panel rails end 40 mm from the post axis
- against the 80 mm standard post: flush, 0 mm gap
- against the 100 mm corner post: rail sockets 10 mm into the post
- in neither case does the rail exit the far side

The corner post is therefore optional. It ships because real perimeter fences
use a heavier post where two runs meet, and because it makes corners readable.

**Still open**

- corners are clean at 90° only
- no slope or stepped variant

---

## 1.0 — 2026-07-28

First public release.

**Contents**

- Cantilever sliding gate, animated (`Open`, 4.00 m travel, rollers coupled)
- Pedestrian gate, animated (`Open`, 95° swing)
- Fence panel, 2 m grid
- Fence post
- Both parametric generators
- 21 baked PBR textures, ORM-packed
- Box colliders on every module

**Notes on what was verified before release**

- Sliding gate: no static part intersects the leaf across the full 4 m travel.
  Checked against the leaf's half-width resolved in 1 cm height slices, because
  the leaf is ±44 mm wide at the rail beam and ±25 mm at the top rail — a single
  bounding box reports collisions at the guide head that do not exist.
- Pedestrian gate: no collision across the full 95° swing, no interpenetration
  when closed. Checked against the leaf's real vertices in plan, because a leaf
  with hinge straps on one side and a handle on the other has no min-x/max-y
  bounding-box corner.
- Roller/leaf sync: residual slip 2.3 × 10⁻⁶ turns across the full travel.
- All GLBs: every textured material carries baseColor, metallicRoughness,
  normal and occlusion texture indices.
