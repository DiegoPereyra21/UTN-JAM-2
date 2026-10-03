# Credits

## Third-party content

**None.** This package contains no third-party models, scans, photographs,
textures, HDRIs or sound. Every asset was created from scratch for this kit.

That is deliberate: CC0 only works if the whole chain is clean. A single
CC-BY texture in the stack would make the CC0 dedication false.

## Tools

- **Blender 5.1** — modelling (bmesh), UV, Cycles bakes, glTF export
- Textures are bakes of procedural Blender shader networks
  (Noise → Bump / ColorRamp → Roughness / Base Color), plus a baked
  ambient-occlusion pass packed into the R channel

No image generators, no photogrammetry, no 3D generative models, no asset
libraries were used at any stage.

## Authorship

Geometry authored as Python/bmesh code. Both generators are included in `src/`
so the entire construction is reproducible and inspectable — run them and you
get this kit back, byte-comparable geometry and all.

The generator code was written in an agent-assisted workflow (Claude driving
Blender through an MCP bridge). See the AI disclosure section in `LICENSE.txt`.

## Attribution

Not required — this is CC0. If you want to link back anyway, that is
appreciated but never expected.
