# Metal Fence Kit — CC0

Seven game-ready modules in one visual family: a **cantilever sliding gate**, a
**double swing gate**, a **pedestrian gate**, two **fence panels** and two
**fence posts**. All three gates ship with a working glTF animation clip.

Baked PBR (ORM-packed), box colliders, real-world scale, glTF/GLB.
Built in Blender 5.1 from two parametric generators, which are included.

**Version 1.2 · License: CC0 1.0 (public domain)**

---

## Contents

| Module | File | Faces | Size | Animation |
|---|---|---:|---:|---|
| Sliding gate (cantilever) | `glb/SlidingGate.glb` | 10,604 | 22.7 MB | **`Open`** — 4.00 m travel, 5.0 s |
| Double swing gate | `glb/DoubleGate.glb` | 7,046 | 8.4 MB | **`Open`** — 2 × 95°, 2.6 s |
| Pedestrian gate | `glb/PedestrianGate.glb` | 2,830 | 6.2 MB | **`Open`** — 95°, 1.6 s |
| Fence panel, 2 m | `glb/Fence_Panel_2m.glb` | 2,220 | 2.7 MB | — |
| Fence panel, 1 m | `glb/Fence_Panel_1m.glb` | 1,164 | 0.6 MB | — |
| Fence post | `glb/Fence_Post.glb` | 162 | 0.4 MB | — |
| Fence post, corner | `glb/Fence_Post_Corner.glb` | 162 | 0.5 MB | — |

```
glb/          the seven modules, textures embedded
textures/     36 loose PNGs, if you prefer your own material setup
src/          the two parametric generators (Python, Blender 5.1)
previews/     renders, two animated GIFs and two MP4s
MetalFenceKit.blend   source file, includes example assemblies
```

---

## Grid and origins

The origins are what makes this a kit rather than six props.

| Module | Origin sits at |
|---|---|
| `Fence_Post`, `Fence_Post_Corner` | ground, post axis |
| `Fence_Panel`, `Fence_Panel_1m` | ground, left edge — spans +X to 2.00 m / 1.00 m |
| `PedGate_Static`, `DblGate_Static` | ground, axis of the (left) hinge post |
| `DblGate_Leaf_L` / `_R` | ground, **own hinge axis** — closed is 0° for both |
| `PedGate_Leaf` | ground, **hinge axis** → opening is a pure local-Z rotation |
| `SlidingGate` | ground, closing edge of the driveway opening |

**Fence grid: 2.00 m** post axis to post axis, with a **1.00 m** half panel for
runs that are not a multiple of two. Drop posts on the grid, drop a panel
between them — the panel ends exactly on the post axes.

Shared across all modules: height **2.00 m**, infill bar **Ø 16 mm**, bar pitch
**120 mm**, frame section **60 mm**.

### Corners

A 90° corner needs no special panel. Place a post at the corner, run one panel
into it and rotate the next panel 90° about Z:

```
Fence_Post_Corner   @ (0, 0, 0)
Fence_Panel         @ (-2, 0, 0)              leg running in −X
Fence_Panel         @ (0, 0, 0)   rot Z 90°   leg running in +Y
```

The panel rails end 40 mm from the post axis. Against the standard 80 mm post
that is exactly flush; against the 100 mm corner post the rail sockets 10 mm
into it, which reads better and hides any placement slop. Neither case leaves a
gap, and the rail does not exit the far side of the post.

`Fence_Post_Corner` is **optional** — geometrically the corner also works with
the standard post. It exists because real perimeter fences use a heavier post
where two runs meet, and because it makes corners readable at a glance.

Example assembly (see `previews/fence_corner.png`, also saved in the .blend).

### Double swing gate

Its clear opening is **4.00 m — deliberately the same as the sliding gate's**,
so the two are interchangeable at the same location. Posts sit 4.12 m apart
(axis to axis).

The right leaf is a genuinely **mirrored body**, not the left one rotated 180°.
The drop bolt sits on the outer face and makes the leaf asymmetric in Y; rotated,
the two leaves would end up 30 mm out of plane with their bolts on opposite
sides. Mirroring costs a second texture set and buys a clean API: **closed is 0°
for both leaves**, open is −95° and +95°.

### Straight run with a gate

```
PedGate_Static  @ -5.22    its latch post lands on -4.12 = fence grid
Fence_Panel     @ -4.12
Fence_Panel     @ -2.12
Fence_Post      @ -2.12
SlidingGate     @  0.00    its catch post at -0.12 terminates the fence
```

---

## Animations

Both gates carry a glTF clip named **`Open`**. Play it in reverse to close.

| Node | Axis | Range | Length |
|---|---|---|---|
| `PedGate_Leaf` | rotation, local **Z** | 0 → **−95°** | 39 frames @ 24 fps |
| `DblGate_Leaf_L` | rotation, local **Z** | 0 → **−95°** | 63 frames @ 24 fps |
| `DblGate_Leaf_R` | rotation, local **Z** | 0 → **+95°** | same clip |
| `SlidingGate_Leaf` | translation, local **+X** | 0 → **4.00 m** | 121 frames @ 24 fps |
| `SlidingGate_Roller_00..03` | rotation, local **Y** | 0 → **+18.19 turns** | same clip |

The sliding gate's leaf and its four rollers share one interpolation curve so the
rollers cannot slip against the rail. Measured residual slip across the full
travel: **2.3 × 10⁻⁶ turns**.

If you want a different travel or swing angle, change `opening` or `swing_deg`
in the generators and re-run — the animation is rebuilt from the geometry.

---

## Materials and textures

One material per moving part, one atlas each.

| Asset | BaseColor | MetallicRoughness | Normal |
|---|---:|---:|---:|
| SlidingGate_Leaf | 4096 | 2048 | 2048 |
| SlidingGate_Static | 4096 | 2048 | 2048 |
| SlidingGate_Roller | 1024 | 512 | 512 |
| Fence_Panel | 2048 | 1024 | 1024 |
| Fence_Panel_1m | 1024 | 512 | 512 |
| Fence_Post, Fence_Post_Corner | 1024 | 512 | 512 |
| DblGate_Static | 2048 | 1024 | 1024 |
| DblGate_Leaf_L, DblGate_Leaf_R | 2048 | 1024 | 1024 |
| PedGate_Static | 2048 | 1024 | 1024 |
| PedGate_Leaf | 2048 | 1024 | 1024 |

**MetallicRoughness is ORM-packed: R = Occlusion, G = Roughness, B = Metallic.**
The `occlusionTexture` is set in every GLB and points at the same image.
Normal maps are tangent space (OpenGL/glTF convention).

Ambient occlusion is **self-occlusion only**. Baking the posts' shadow into a
moving leaf would make that shadow travel with it. AO range is 6 cm, so it
darkens joints and leaves open surfaces at 1.0.

Colour split: anthracite (RAL 7016-ish) for frames, rails and infill bars;
hot-dip galvanised for posts, hardware and the sliding gate's rail beam.

---

## Engine notes

glTF has no collider concept, so colliders travel as separate named meshes
(`<Asset>_COL_NN`).

- **Unity** — add a BoxCollider per `_COL` mesh and disable their MeshRenderers.
  Metallic-roughness maps directly. Import via glTFast or UnityGLTF.
- **Unreal** — rename the `_COL` meshes to `UCX_<RenderMeshName>_NN` before
  import and the importer assigns them automatically. Unreal expects ORM, which
  is what is packed here.
- **Godot** — `_COL` suffix; or rename to `-col` before import and Godot builds
  the collision itself.
- **three.js / Babylon** — standard glTF PBR; hide the `_COL` nodes.

Exported with **+Y up** (glTF standard). Real-world scale, 1 unit = 1 m.

---

## Rebuilding / customising

Both generators have a `CONFIG` dict at the top. Change a value, re-run, and the
geometry, UVs, bakes, colliders and animation are rebuilt.

```bash
blender --background --python src/sliding_gate_gen.py
blender --background --python src/fence_kit_gen.py
```

Useful knobs: `opening` (driveway width, also the travel distance), `height`,
`bar_gap`, `bar_d`, `panel_width`, `panel_half`, `post`, `post_corner`,
`gate_opening`, `swing_deg`.

`fence_kit_gen.py` imports its materials, bevel, bake and export logic from
`sliding_gate_gen.py`, so the two halves of the kit cannot drift apart
stylistically.

Note: the code comments are in German. The `CONFIG` keys and the structure are
English; the running commentary is not. Sorry about that.

---

## Known limitations

- **UV fill on the sliding gate leaf is 25 %.** 43 thin round bars pack badly;
  effective density is around 700 px/m. Overlapping the identical bars' UVs
  would be the next big win, but it breaks lightmap UV2.
- Corners are clean at **90° only**. A 135° corner against a square post leaves
  a wedge-shaped gap; you would need a rotated or round post for that.
- **No slope or stepped variant** — the kit assumes level ground.
- Gusset plates and hinge straps are plain plates — no weld-bead detail.
- Preview lighting is a procedural sky, not a photographic HDRI.

---

## How this was made

The geometry is produced **deterministically from bmesh code** — no text-to-3D,
no image-to-3D, no sampled meshes. The surfaces are procedural Blender shaders
(noise → bump / roughness / base colour) baked down to bitmaps; no generated
textures.

The code was written in an agent workflow (Claude driving Blender through an
MCP bridge). That is also why the generators ship with the kit: they are the
complete, inspectable provenance of every vertex, and they make the kit
parametric.

**If you republish this on a platform with an AI disclosure requirement, please
carry this paragraph over.**

---

## License

CC0 1.0 Universal. Use it for anything, commercial or not, no attribution
required. See `LICENSE.txt`.
