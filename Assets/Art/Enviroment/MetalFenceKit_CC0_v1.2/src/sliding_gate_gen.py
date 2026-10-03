#!/usr/bin/env python3
"""
SlidingGate - parametrischer Generator fuer ein freitragendes Schiebetor.

Erzeugt ein game-ready Asset mit beweglichen Teilen: Torblatt (Linearachse),
vier Laufrollen (Rotationsachse), Box-Collider, gebackene PBR-Maps, glTF-Export.

Aufruf headless:
    blender --background --python sliding_gate_gen.py

Alles Wesentliche steht in CONFIG. Aendere `opening` und das Tor wird breiter,
der Gegengewichtsarm laenger, die Fuellstaebe werden neu verteilt, die Laufwagen
ruecken mit, die Collider passen sich an.

Geometrie entsteht deterministisch aus bmesh-Code. Keine gesampelten Meshes,
keine generierten Texturen - die Oberflaechen sind prozedurale Blender-Shader,
die in Bitmaps gebacken werden.

Lizenz des erzeugten Assets: CC0 1.0
"""

import bpy, bmesh, math, os
from mathutils import Vector, Matrix

# =========================================================== CONFIG

CONFIG = {
    # --- Hauptmasse ---
    "opening":      4.00,   # lichte Durchfahrtsbreite [m] -> zugleich der Verfahrweg
    "height":       2.00,   # Torhoehe ueber Boden [m]
    "tail_ratio":   0.50,   # Gegengewichtsarm als Anteil der Oeffnung
    "ground_clear": 0.09,   # Bodenfreiheit unter der Laufschiene [m]

    # --- Profile ---
    "bar_d":        0.016,  # Fuellstab-Durchmesser [m]
    "bar_gap":      0.12,   # Achsabstand der Fuellstaebe [m]
    "frame":        0.06,   # Rahmenprofil [m]
    "beam_h":       0.095,  # Laufschienen-Profilhoehe [m]
    "beam_w":       0.088,  # Laufschienen-Profilbreite [m]
    "post":         0.10,   # Anschlagpfosten Kantenlaenge [m]
    "gpost":        0.12,   # Fuehrungspfosten Kantenlaenge [m]
    "roller_r":     0.035,  # Laufrollen-Radius [m]
    "roller_w":     0.05,   # Laufrollen-Breite [m]

    # --- Detaillierung ---
    "penetration":  0.014,  # Staebe/Strebe stecken so tief im Rahmen [m]
    "bevel":        0.0025, # Kantenfase [m] - gewalzte Kante / Pulverbeschichtung
    "bevel_seg":    2,
    "ao_distance":  0.06,   # AO-Reichweite: Kontaktmassstab, nicht Raummassstab

    # --- Ausgabe ---
    "outdir":       "/Volumes/media/Blender/SlidingGate",
    "res_bc":       4096,   # BaseColor
    "res_mr":       2048,   # MetallicRoughness (binaeres Metallic braucht kein 4K)
    "res_nm":       2048,   # Normal (Mikrostruktur braucht kein 4K)
    "res_small":    1024,   # Kleinteile (Rolle): BaseColor; MR/NM = haelfte
    "samples":      16,
}

# =========================================================== HELPERS

def tube(bm, p1, p2, r, seg=12):
    p1, p2 = Vector(p1), Vector(p2); v = p2 - p1; L = v.length
    if L < 1e-6: return
    q = Vector((0, 0, 1)).rotation_difference(v.normalized())
    M = Matrix.Translation((p1 + p2) / 2) @ q.to_matrix().to_4x4()
    bmesh.ops.create_cone(bm, cap_ends=True, segments=seg, radius1=r, radius2=r, depth=L, matrix=M)


def box(bm, center, dims):
    M = Matrix.Translation(Vector(center)) @ Matrix.Diagonal((dims[0], dims[1], dims[2], 1.0))
    bmesh.ops.create_cube(bm, size=1.0, matrix=M)


def disc(bm, center, radius, thick, axis='Y', seg=28):
    d = {'X': Vector((1, 0, 0)), 'Y': Vector((0, 1, 0)), 'Z': Vector((0, 0, 1))}[axis]
    q = Vector((0, 0, 1)).rotation_difference(d)
    M = Matrix.Translation(Vector(center)) @ q.to_matrix().to_4x4()
    bmesh.ops.create_cone(bm, cap_ends=True, segments=seg, radius1=radius, radius2=radius,
                          depth=thick, matrix=M)


def part(bm, idx, fn):
    """fn ausfuehren und die neu entstandenen Faces dem Materialslot idx zuordnen."""
    prev = set(bm.faces); fn(); bm.faces.ensure_lookup_table()
    for f in bm.faces:
        if f not in prev:
            f.material_index = idx


def sock(node, ident):
    """Socket ueber .identifier holen.

    Auf einem lokalisierten Blender (de_DE) ist .name uebersetzt - 'Grundfarbe'
    statt 'Base Color'. .identifier bleibt stabil. Ohne das schlaegt jeder
    Zugriff per Namen fehl.
    """
    for s in node.inputs:
        if s.identifier == ident:
            return s
    raise KeyError("Input '%s' nicht gefunden an %s" % (ident, node.name))


def osock(node, ident):
    for s in node.outputs:
        if s.identifier == ident:
            return s
    raise KeyError("Output '%s' nicht gefunden an %s" % (ident, node.name))


def finalize(name, bm, materials, coll):
    me = bpy.data.meshes.new(name); bm.to_mesh(me); bm.free()
    ob = bpy.data.objects.new(name, me)
    for m in materials:
        ob.data.materials.append(m)
    coll.objects.link(ob)
    return ob


def get_coll(name):
    c = bpy.data.collections.get(name)
    if c is None:
        c = bpy.data.collections.new(name)
        bpy.context.scene.collection.children.link(c)
    return c


def derive(cfg):
    o = cfg["opening"]
    d = dict(cfg)
    d["L"]   = o * (1.0 + cfg["tail_ratio"])        # Blattlaenge
    d["TRV"] = o                                     # Verfahrweg
    d["bt"]  = cfg["ground_clear"] + cfg["beam_h"]   # Oberkante Laufschiene
    d["gx"]  = o + 0.15                              # Fuehrungspfosten
    d["cxa"] = o + 0.55                              # Laufwagen A
    d["cxb"] = o + 1.75                              # Laufwagen B
    d["rz"]  = cfg["ground_clear"] - cfg["roller_r"] # Rollenachse
    return d

# =========================================================== MATERIALIEN

# (Basisfarbe, metallic, roughness, noise_fein, det_fein, bump, bump_dist,
#  noise_grob, det_grob, rough_lo, rough_hi, farbstreuung, kantenabrieb,
#  schmutzmenge, schmutzhoehe)
#
# Zu den Kantenabrieb-Werten: Pointiness feuert nach dem Fasen auf JEDER
# konvexen Kante, und Kastenprofile haben davon sehr viele. Werte ueber ~0.2
# lassen die Profile flaechig aufhellen statt nur die Kanten - dann sieht
# pulverbeschichteter Stahl aus wie Zink. Lieber zu wenig als zu viel.
MATSPEC = {
    "SG_Anthrazit": ((0.055, 0.058, 0.062), 0.0, 0.42, 70.0, 2.5, 0.30, 0.016, 18.0, 2.0, 0.33, 0.47, 0.03, 0.15, 0.45, 0.40),
    # Feuerverzinkung: stumpfes Mittelgrau, kein Chrom. Bei metallic=1 bestimmt
    # aber nicht die Albedo die wahrgenommene Helligkeit, sondern die Spiegelung.
    # Der wirksame Hebel gegen den "weisses Plastik"-Look ist deshalb die
    # Rauheit (0.62-0.80), nicht die Grundfarbe.
    "SG_Verzinkt":  ((0.400, 0.415, 0.430), 1.0, 0.70, 45.0, 3.0, 0.28, 0.020, 12.0, 2.0, 0.62, 0.80, 0.06, 0.10, 0.55, 0.45),
    "SG_Beton":     ((0.420, 0.410, 0.390), 0.0, 0.90, 18.0, 4.0, 0.55, 0.060,  5.0, 2.5, 0.80, 0.96, 0.11, 0.08, 0.85, 0.55),
    "SG_Gummi":     ((0.028, 0.028, 0.030), 0.0, 0.78, 30.0, 3.0, 0.35, 0.030,  8.0, 2.0, 0.70, 0.88, 0.04, 0.06, 0.50, 0.40),
}
DIRT_COL = (0.105, 0.090, 0.072)     # Spritzwasser / Schlamm von unten


def build_material(name):
    (col, met, rgh, s1, d1, bst, bdi, s2, d2, r0, r1, cj, ew, da, dh) = MATSPEC[name]
    m = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    m.use_nodes = True
    nt = m.node_tree; N, LK = nt.nodes, nt.links
    for n in list(N):
        N.remove(n)
    out  = N.new('ShaderNodeOutputMaterial'); out.location = (900, 0)
    bsdf = N.new('ShaderNodeBsdfPrincipled'); bsdf.location = (620, 0)
    LK.new(bsdf.outputs['BSDF'], out.inputs['Surface'])
    sock(bsdf, 'Metallic').default_value = met

    tc = N.new('ShaderNodeTexCoord'); tc.location = (-1200, 0)

    # --- feine Frequenz -> Bump/Normal ---
    nz = N.new('ShaderNodeTexNoise'); nz.location = (-1000, 320)
    LK.new(osock(tc, 'Object'), sock(nz, 'Vector'))
    sock(nz, 'Scale').default_value = s1
    sock(nz, 'Detail').default_value = d1
    bp = N.new('ShaderNodeBump'); bp.location = (-760, 320)
    sock(bp, 'Strength').default_value = bst
    sock(bp, 'Distance').default_value = bdi
    LK.new(osock(nz, 'Fac'), sock(bp, 'Height'))
    LK.new(osock(bp, 'Normal'), sock(bsdf, 'Normal'))

    # --- grobe Frequenz -> Rauheit und Farbstreuung ---
    nz2 = N.new('ShaderNodeTexNoise'); nz2.location = (-1000, -120)
    LK.new(osock(tc, 'Object'), sock(nz2, 'Vector'))
    sock(nz2, 'Scale').default_value = s2
    sock(nz2, 'Detail').default_value = d2

    # --- Masken: Kanten (Pointiness) und Schmutz (Objekt-Z) ---
    geo = N.new('ShaderNodeNewGeometry'); geo.location = (-1200, -420)
    ewr = N.new('ShaderNodeValToRGB'); ewr.location = (-1000, -420)
    ewr.color_ramp.elements[0].position = 0.545      # eng: nur echte Kanten
    ewr.color_ramp.elements[1].position = 0.575
    LK.new(osock(geo, 'Pointiness'), sock(ewr, 'Fac'))

    sxyz = N.new('ShaderNodeSeparateXYZ'); sxyz.location = (-1000, -700)
    LK.new(osock(tc, 'Object'), sock(sxyz, 'Vector'))
    dr = N.new('ShaderNodeValToRGB'); dr.location = (-800, -700)
    dr.color_ramp.elements[0].position = 0.0; dr.color_ramp.elements[0].color = (1, 1, 1, 1)
    dr.color_ramp.elements[1].position = dh;  dr.color_ramp.elements[1].color = (0, 0, 0, 1)
    LK.new(osock(sxyz, 'Z'), sock(dr, 'Fac'))

    def mix_f(a, b, fac_out, loc):
        n = N.new('ShaderNodeMix'); n.data_type = 'FLOAT'; n.location = loc
        sock(n, 'A_Float').default_value = a
        sock(n, 'B_Float').default_value = b
        LK.new(fac_out, sock(n, 'Factor_Float'))
        return n

    ewm = mix_f(0.0, ew, osock(ewr, 'Color'), (-560, 40))     # Kantenmaske skaliert
    dm  = mix_f(0.0, da, osock(dr, 'Color'), (-380, -220))    # Schmutzmaske skaliert

    # --- BaseColor: Streuung -> Kantenabrieb -> Schmutz ---
    cr = N.new('ShaderNodeValToRGB'); cr.location = (-760, -120)
    lo = tuple(max(0.0, c * (1 - cj)) for c in col)
    hi = tuple(min(1.0, c * (1 + cj)) for c in col)
    cr.color_ramp.elements[0].position = 0.30; cr.color_ramp.elements[0].color = (*lo, 1)
    cr.color_ramp.elements[1].position = 0.72; cr.color_ramp.elements[1].color = (*hi, 1)
    LK.new(osock(nz2, 'Fac'), sock(cr, 'Fac'))

    wear = N.new('ShaderNodeMix'); wear.data_type = 'RGBA'; wear.location = (-380, 40)
    LK.new(osock(cr, 'Color'), sock(wear, 'A_Color'))
    sock(wear, 'B_Color').default_value = (*tuple(min(1.0, c * 1.35 + 0.015) for c in col), 1)
    LK.new(osock(ewm, 'Result_Float'), sock(wear, 'Factor_Float'))

    dirty = N.new('ShaderNodeMix'); dirty.data_type = 'RGBA'; dirty.location = (-120, 40)
    LK.new(osock(wear, 'Result_Color'), sock(dirty, 'A_Color'))
    sock(dirty, 'B_Color').default_value = (*DIRT_COL, 1)
    LK.new(osock(dm, 'Result_Float'), sock(dirty, 'Factor_Float'))
    LK.new(osock(dirty, 'Result_Color'), sock(bsdf, 'Base Color'))

    # --- Roughness: Noise -> Kanten rauher -> Schmutz rauher ---
    rr = N.new('ShaderNodeValToRGB'); rr.location = (-760, -300)
    rr.color_ramp.elements[0].position = 0.35; rr.color_ramp.elements[0].color = (r0, r0, r0, 1)
    rr.color_ramp.elements[1].position = 0.68; rr.color_ramp.elements[1].color = (r1, r1, r1, 1)
    LK.new(osock(nz2, 'Fac'), sock(rr, 'Fac'))

    rw = N.new('ShaderNodeMix'); rw.data_type = 'FLOAT'; rw.location = (-380, -340)
    LK.new(osock(rr, 'Color'), sock(rw, 'A_Float'))
    sock(rw, 'B_Float').default_value = min(1.0, r1 + 0.10)
    LK.new(osock(ewm, 'Result_Float'), sock(rw, 'Factor_Float'))

    rd = N.new('ShaderNodeMix'); rd.data_type = 'FLOAT'; rd.location = (-120, -340)
    LK.new(osock(rw, 'Result_Float'), sock(rd, 'A_Float'))
    sock(rd, 'B_Float').default_value = 0.94
    LK.new(osock(dm, 'Result_Float'), sock(rd, 'Factor_Float'))
    LK.new(osock(rd, 'Result_Float'), sock(bsdf, 'Roughness'))
    return m

# =========================================================== GEOMETRIE

def build_geometry(C, kit):
    L, bt, gx, cxa, cxb, rz = C["L"], C["bt"], C["gx"], C["cxa"], C["cxb"], C["rz"]
    h, fr, gc = C["height"], C["frame"], C["ground_clear"]
    M = {k: build_material(k) for k in MATSPEC}

    # ---------- Torblatt: Ursprung (0,0,0) = Boden an der Schliesskante ----------
    bm = bmesh.new()
    part(bm, 1, lambda: box(bm, (L / 2, 0, gc + C["beam_h"] / 2),
                            (L, C["beam_w"], C["beam_h"])))          # Laufschiene

    PEN = C["penetration"]
    n = max(1, int(round(L / 1.5)))
    VERT_X = [L * i / n for i in range(1, n)]     # Zwischenpfosten
    END_X = L - 0.03                               # Endprofil-Mitte
    bay0, bay1 = VERT_X[-1], END_X                 # Gegengewichtsfeld

    def frame():
        box(bm, (L / 2, 0, h - fr / 2), (L, 0.05, fr))               # Oberholm
        box(bm, (0.045, 0, (bt + h) / 2), (0.09, 0.06, h - bt))      # Anschlagprofil
        box(bm, (END_X, 0, (bt + h) / 2), (0.06, 0.05, h - bt))      # Endprofil
        for x in VERT_X:
            box(bm, (x, 0, (bt + h) / 2), (0.05, 0.05, h - bt))
        # Diagonalstrebe Ecke zu Ecke im Gegengewichtsfeld. Beide Enden ragen
        # ueber den Knoten hinaus - endet sie davor, schwebt sie sichtbar.
        p0 = Vector((bay0, 0, bt)); p1 = Vector((bay1, 0, h - fr))
        d = (p1 - p0).normalized() * 0.055
        tube(bm, p0 - d, p1 + d, 0.019, 12)
        box(bm, (bay0 + 0.065, 0, bt + 0.065), (0.15, 0.042, 0.14))          # Knotenbleche
        box(bm, (bay1 - 0.065, 0, h - fr - 0.065), (0.15, 0.042, 0.14))
    part(bm, 0, frame)

    def bars():
        # Staebe stecken oben und unten im Rahmen. Enden sie buendig, klafft
        # bei streifendem Licht eine Fuge und die Fuellung wirkt lose eingelegt.
        z0, z1 = bt - PEN, h - fr + PEN
        x = 0.14
        while x < L - 0.10:
            collides = any(abs(x - vx) < 0.05 for vx in VERT_X) or abs(x - END_X) < 0.05
            if not collides:
                tube(bm, (x, 0, z0), (x, 0, z1), C["bar_d"] / 2, 10)
            x += C["bar_gap"]
    part(bm, 0, bars)
    leaf = finalize("SlidingGate_Leaf", bm, [M["SG_Anthrazit"], M["SG_Verzinkt"]], kit)

    # ---------- Statik ----------
    POST_H, ARM_Z, ARM_T = h + 0.20, h + 0.06, 0.08
    GR_Y, GR_R = 0.058, 0.026
    GR_Z0, GR_Z1 = h - 0.10, ARM_Z - ARM_T / 2
    bm = bmesh.new()

    def bolts(cx, cy, dx, dy, z, r=0.009, t=0.007):
        for sx in (-1, 1):
            for sy in (-1, 1):
                disc(bm, (cx + sx * dx, cy + sy * dy, z), r, t, 'Z', 10)

    def steel():
        box(bm, (-0.12, 0, 1.05), (C["post"], C["post"], 2.10))              # Anschlagpfosten
        box(bm, (-0.12, 0, 2.105), (0.116, 0.116, 0.012))                    # Pfostenkappe
        box(bm, (-0.12, 0, 0.008), (0.20, 0.20, 0.016))                      # Fussplatte
        bolts(-0.12, 0, 0.072, 0.072, 0.019)
        box(bm, (gx, -0.18, POST_H / 2), (C["gpost"], C["gpost"], POST_H))   # Fuehrungspfosten
        box(bm, (gx, -0.18, POST_H + 0.006), (0.136, 0.136, 0.012))          # Kappe
        box(bm, (gx, -0.18, 0.008), (0.22, 0.22, 0.016))                     # Fussplatte
        bolts(gx, -0.18, 0.082, 0.082, 0.019)
        box(bm, (gx, -0.075, ARM_Z), (0.12, 0.33, ARM_T))                    # Kopf ueber dem Blatt
        for sy in (-1, 1):
            box(bm, (gx, sy * GR_Y, (GR_Z0 + GR_Z1) / 2), (0.05, 0.014, GR_Z1 - GR_Z0))
        for cx in (cxa, cxb):
            box(bm, (cx, 0, 0.012), (0.30, 0.16, 0.024))                     # Grundplatte
            bolts(cx, 0, 0.125, 0.062, 0.028)                                # Ankerschrauben
            for sy in (-1, 1):
                box(bm, (cx, sy * 0.042, 0.035), (0.26, 0.010, 0.046))       # offene Bogie-Wangen
                for rx in (-0.09, 0.09):
                    disc(bm, (cx + rx, sy * 0.042, rz), 0.012, 0.014, 'Y', 12)

    def conc():
        for cx in (-0.12, gx, cxa, cxb):
            yy = -0.18 if abs(cx - gx) < 1e-6 else 0.0
            d = (0.42, 0.34, 0.32) if cx in (cxa, cxb) else (0.36, 0.36, 0.32)
            box(bm, (cx, yy, -0.16), d)

    def rubber():
        box(bm, (-0.035, 0, 1.00), (0.07, 0.07, 0.34))                       # Anschlagpuffer
        for sy in (-1, 1):
            disc(bm, (gx, sy * GR_Y, (GR_Z0 + GR_Z1) / 2), GR_R,
                 GR_Z1 - GR_Z0 - 0.01, 'Z', 20)                              # Fuehrungsrollen
    part(bm, 0, steel); part(bm, 1, conc); part(bm, 2, rubber)
    static = finalize("SlidingGate_Static", bm,
                      [M["SG_Verzinkt"], M["SG_Beton"], M["SG_Gummi"]], kit)

    # ---------- Laufrolle: Ursprung auf der eigenen Drehachse ----------
    bm = bmesh.new()
    part(bm, 1, lambda: disc(bm, (0, 0, 0), C["roller_r"], C["roller_w"], 'Y', 28))
    part(bm, 0, lambda: disc(bm, (0, 0, 0), 0.013, C["roller_w"] + 0.012, 'Y', 16))
    r0 = finalize("SlidingGate_Roller_00", bm, [M["SG_Verzinkt"], M["SG_Gummi"]], kit)
    r0.location = (cxa - 0.09, 0, rz)
    rollers = [r0]
    for i, p in enumerate([(cxa + 0.09, 0, rz), (cxb - 0.09, 0, rz), (cxb + 0.09, 0, rz)], start=1):
        ob = bpy.data.objects.new("SlidingGate_Roller_%02d" % i, r0.data)   # geteilte Mesh-Daten
        ob.location = p; kit.objects.link(ob); rollers.append(ob)
    return leaf, static, rollers

# =========================================================== KOLLISIONSPRUEFUNG

def check_travel(leaf, static, rollers, C):
    """Darf das Blatt seinen ganzen Weg fahren, ohne etwas Statisches zu schneiden?

    Eine einzelne Bounding-Box reicht dafuer nicht: das Blatt ist unten
    (Laufschiene, +-44 mm) breiter als oben (Oberholm, +-25 mm). Deshalb wird
    die Halbbreite in 1-cm-Hoehenscheiben aufgeloest.
    """
    NS, ZMIN, ZMAX = 240, 0.0, C["height"] + 0.4
    step = (ZMAX - ZMIN) / NS
    idx = lambda z: min(NS - 1, max(0, int((z - ZMIN) / step)))   # sauber geklemmt
    half = [0.0] * NS
    for p in leaf.data.polygons:
        co = [leaf.data.vertices[i].co for i in p.vertices]
        hy = max(abs(c.y) for c in co)
        for i in range(idx(min(c.z for c in co)), idx(max(c.z for c in co)) + 1):
            half[i] = max(half[i], hy)

    lz0 = min(v.co.z for v in leaf.data.vertices)
    lz1 = max(v.co.z for v in leaf.data.vertices)
    SX = (0.0, C["L"] + C["TRV"]); EPS = 0.0015
    hits = []
    for p in static.data.polygons:
        co = [static.data.vertices[i].co for i in p.vertices]
        bb = [(min(c[k] for c in co), max(c[k] for c in co)) for k in range(3)]
        if bb[0][1] <= SX[0] + EPS or bb[0][0] >= SX[1] - EPS: continue
        if bb[2][1] <= lz0 + EPS or bb[2][0] >= lz1 - EPS: continue
        lh = max(half[idx(bb[2][0]): idx(bb[2][1]) + 1])
        if lh <= 0: continue
        ymin = 0.0 if (bb[1][0] < 0 < bb[1][1]) else min(abs(bb[1][0]), abs(bb[1][1]))
        if ymin < lh - EPS:
            hits.append(bb)
    return hits

# =========================================================== UV + BAKE

def reset_scene():
    """Vorherigen Lauf entfernen, damit das Skript wiederholbar ist.
    Nur die eigenen Collections - fremde Objekte bleiben unangetastet."""
    for cn in ("SlidingGate", "SlidingGate_Colliders"):
        c = bpy.data.collections.get(cn)
        if c:
            for o in list(c.objects):
                bpy.data.objects.remove(o, do_unlink=True)
    for m in list(bpy.data.meshes):
        if m.users == 0:
            bpy.data.meshes.remove(m)


def bevel_apply(objs, width, segments):
    """Fase als echte Geometrie, nicht als Live-Modifier.

    Zwei Gruende: der Bake laeuft auf dem evaluierten Mesh, dessen Fasenflaechen
    in den UVs der Basis nicht vorkommen - das schmiert. Und ein zweiter Aufruf
    auf schon gefaster Geometrie erzeugt entartete Mikroflaechen, also darf die
    Fase genau einmal laufen.
    """
    if bpy.context.mode != 'OBJECT':
        bpy.ops.object.mode_set(mode='OBJECT')
    for o in objs:
        if o.data.users > 1:
            # Modifier lassen sich nicht auf Multi-User-Daten anwenden
            raise RuntimeError("%s teilt seine Mesh-Daten - vor dem Fasen entkoppeln" % o.name)
        bpy.ops.object.select_all(action='DESELECT')
        o.select_set(True); bpy.context.view_layer.objects.active = o
        for m in [m for m in o.modifiers if m.type == 'BEVEL']:
            o.modifiers.remove(m)
        bv = o.modifiers.new("Bevel", 'BEVEL')
        bv.limit_method = 'ANGLE'
        bv.angle_limit = math.radians(32)
        bv.width = width
        bv.segments = segments
        bv.miter_outer = 'MITER_ARC'
        bpy.ops.object.modifier_apply(modifier=bv.name)


def uv_unwrap(objs):
    if bpy.context.mode != 'OBJECT':
        bpy.ops.object.mode_set(mode='OBJECT')
    for o in objs:
        bpy.ops.object.select_all(action='DESELECT')
        o.select_set(True); bpy.context.view_layer.objects.active = o
        try: bpy.ops.object.shade_auto_smooth(angle=math.radians(35))
        except Exception: bpy.ops.object.shade_smooth()
        bpy.ops.object.mode_set(mode='EDIT'); bpy.ops.mesh.select_all(action='SELECT')
        try: bpy.ops.uv.smart_project(angle_limit=math.radians(66), island_margin=0.02)
        except TypeError: bpy.ops.uv.smart_project(island_margin=0.02)
        # smart_project packt lange duenne Inseln schlecht - nachpacken bringt
        # bei diesem Asset 50 -> 83 % Flaechennutzung
        try:
            bpy.ops.uv.pack_islands(rotate=True, margin=0.004, scale=True,
                                    shape_method='CONCAVE', rotate_method='ANY')
        except TypeError:
            bpy.ops.uv.pack_islands(rotate=True, margin=0.004)
        bpy.ops.object.mode_set(mode='OBJECT')


def setup_cycles(samples):
    scn = bpy.context.scene
    scn.render.engine = 'CYCLES'
    try:
        cp = bpy.context.preferences.addons['cycles'].preferences
        for t in ('METAL', 'OPTIX', 'CUDA', 'HIP'):
            try:
                cp.compute_device_type = t
                if any(d.type == t for d in cp.devices):
                    for d in cp.devices: d.use = (d.type == t)
                    scn.cycles.device = 'GPU'; break
            except Exception: continue
    except Exception: pass
    scn.cycles.samples = samples
    scn.render.bake.margin = 10
    scn.render.bake.use_selected_to_active = False


def bake_asset(name, texdir, res, res_mr, res_nm, ao_distance=0.06):
    import numpy as np
    obj = bpy.data.objects[name]
    scn = bpy.context.scene
    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True); bpy.context.view_layer.objects.active = obj

    def newimg(n, r, nc):
        old = bpy.data.images.get(n)
        if old: bpy.data.images.remove(old)
        img = bpy.data.images.new(n, r, r, alpha=False, float_buffer=False)
        img.colorspace_settings.name = 'Non-Color' if nc else 'sRGB'
        return img

    def target(img):
        for slot in obj.material_slots:
            m = slot.material
            if not m or not m.use_nodes: continue
            nt = m.node_tree
            n = next((x for x in nt.nodes if x.get("bake_target")), None)
            if n is None:
                n = nt.nodes.new('ShaderNodeTexImage'); n["bake_target"] = 1
                n.location = (-900, -700)
            n.image = img; nt.nodes.active = n

    bs = [next(x for x in s.material.node_tree.nodes if x.type == 'BSDF_PRINCIPLED')
          for s in obj.material_slots if s.material and s.material.use_nodes]
    bc = newimg(name + "_BC", res, False); rg = newimg(name + "_RG", res, True)
    mt = newimg(name + "_MT", res, True);  nm = newimg(name + "_NM", res, True)
    ao = newimg(name + "_AO", res, True)

    # BaseColor: Metallic temporaer auf 0. Der DIFFUSE-Pass liefert die diffuse
    # Albedo - bei metallic=1 gibt es keine, das Ergebnis waere schwarz.
    saved = [(b, sock(b, 'Metallic').default_value) for b in bs]
    for b in bs: sock(b, 'Metallic').default_value = 0.0
    target(bc); bpy.ops.object.bake(type='DIFFUSE', pass_filter={'COLOR'})
    for b, v in saved: sock(b, 'Metallic').default_value = v

    target(rg); bpy.ops.object.bake(type='ROUGHNESS')

    # Metallic: Cycles hat keinen METALLIC-Pass. Umweg ueber Emission.
    se = []
    for b in bs:
        v = sock(b, 'Metallic').default_value
        se.append((b, sock(b, 'Emission Color').default_value[:],
                   sock(b, 'Emission Strength').default_value))
        sock(b, 'Emission Color').default_value = (v, v, v, 1.0)
        sock(b, 'Emission Strength').default_value = 1.0
    target(mt); bpy.ops.object.bake(type='EMIT')
    for b, ec, es in se:
        sock(b, 'Emission Color').default_value = ec
        sock(b, 'Emission Strength').default_value = es

    target(nm); bpy.ops.object.bake(type='NORMAL', normal_space='TANGENT')

    # Ambient Occlusion, ausschliesslich Selbstverschattung: das Blatt faehrt,
    # eine eingebackene Verschattung durch die Pfosten wuerde mitwandern.
    # Die AO-Reichweite steht am World-Lightsetting und ist per Default 10 m -
    # damit verdunkelt sie alles gleichmaessig statt nur die Knoten.
    hidden = [o for o in bpy.data.objects
              if o.type == 'MESH' and o is not obj and not o.hide_render]
    for o in hidden: o.hide_render = True
    old_dist = scn.world.light_settings.distance
    old_smp = scn.cycles.samples
    scn.world.light_settings.distance = ao_distance
    scn.cycles.samples = 64
    target(ao); bpy.ops.object.bake(type='AO')
    scn.world.light_settings.distance = old_dist
    scn.cycles.samples = old_smp
    for o in hidden: o.hide_render = False

    ra = np.array(rg.pixels[:]).reshape(-1, 4)
    ma = np.array(mt.pixels[:]).reshape(-1, 4)
    aa = np.array(ao.pixels[:]).reshape(-1, 4)
    mr = np.ones_like(ra)
    mr[:, 0] = aa[:, 0]      # R = Occlusion (glTF ORM)
    mr[:, 1] = ra[:, 0]      # G = Roughness
    mr[:, 2] = ma[:, 0]      # B = Metallic
    mr[:, 3] = 1.0
    mrimg = newimg(name + "_MR", res, True); mrimg.pixels = mr.flatten().tolist()

    for img, r, fn in [(bc, res, "_BaseColor.png"),
                       (mrimg, res_mr, "_MetallicRoughness.png"),
                       (nm, res_nm, "_Normal.png")]:
        if img.size[0] != r:
            img.scale(r, r)
        img.filepath_raw = os.path.join(texdir, name + fn)
        img.file_format = 'PNG'; img.save()

# =========================================================== FINALES MATERIAL

def gltf_output_group():
    """Der glTF-Exporter schreibt occlusionTexture nur, wenn eine Node-Group
    exakt dieses Namens mit Eingang 'Occlusion' im Material haengt. Ohne sie
    bleibt der gebackene AO-Kanal im Bild liegen und wird nie benutzt."""
    nm = "glTF Material Output"
    g = bpy.data.node_groups.get(nm)
    if g is None:
        g = bpy.data.node_groups.new(nm, 'ShaderNodeTree')
        try:
            g.interface.new_socket("Occlusion", in_out='INPUT', socket_type='NodeSocketFloat')
        except AttributeError:
            g.inputs.new('NodeSocketFloat', "Occlusion")
        g.nodes.new('NodeGroupInput')
    return g


def textured_material(name):
    old = bpy.data.materials.get(name + "_MAT")
    if old: bpy.data.materials.remove(old)
    m = bpy.data.materials.new(name + "_MAT"); m.use_nodes = True
    nt = m.node_tree; N, LK = nt.nodes, nt.links
    for n in list(N): N.remove(n)
    out  = N.new('ShaderNodeOutputMaterial'); out.location = (600, 0)
    bsdf = N.new('ShaderNodeBsdfPrincipled'); bsdf.location = (280, 0)
    LK.new(bsdf.outputs['BSDF'], out.inputs['Surface'])
    bc = N.new('ShaderNodeTexImage'); bc.location = (-320, 260)
    bc.image = bpy.data.images[name + "_BC"]; bc.image.colorspace_settings.name = 'sRGB'
    LK.new(bc.outputs['Color'], sock(bsdf, 'Base Color'))
    mr = N.new('ShaderNodeTexImage'); mr.location = (-320, -20)
    mr.image = bpy.data.images[name + "_MR"]; mr.image.colorspace_settings.name = 'Non-Color'
    sep = N.new('ShaderNodeSeparateColor'); sep.location = (-40, -20)
    LK.new(mr.outputs['Color'], sep.inputs['Color'])
    LK.new(sep.outputs['Green'], sock(bsdf, 'Roughness'))   # G = Roughness
    LK.new(sep.outputs['Blue'],  sock(bsdf, 'Metallic'))    # B = Metallic
    gg = N.new('ShaderNodeGroup'); gg.node_tree = gltf_output_group()
    gg.location = (400, -280)
    LK.new(sep.outputs['Red'], gg.inputs['Occlusion'])      # R = Occlusion
    nm = N.new('ShaderNodeTexImage'); nm.location = (-320, -320)
    nm.image = bpy.data.images[name + "_NM"]; nm.image.colorspace_settings.name = 'Non-Color'
    nmap = N.new('ShaderNodeNormalMap'); nmap.location = (-40, -320)
    LK.new(nm.outputs['Color'], nmap.inputs['Color'])
    LK.new(nmap.outputs['Normal'], sock(bsdf, 'Normal'))
    return m

# =========================================================== COLLIDER

def fcurves_of(obj):
    """F-Curves eines Objekts holen.

    Blender 5 hat die Action-API auf Layer/Slots umgestellt: action.fcurves
    existiert nicht mehr, die Kurven liegen im Channelbag des Slots.
    """
    ad = obj.animation_data
    if not ad or not ad.action:
        return []
    a = ad.action
    legacy = getattr(a, "fcurves", None)
    if legacy is not None and len(legacy):
        return list(legacy)
    out = []
    for layer in a.layers:
        for strip in layer.strips:
            if strip.type != 'KEYFRAME':
                continue
            slot = getattr(ad, "action_slot", None) or (a.slots[0] if a.slots else None)
            if slot is None:
                continue
            cb = strip.channelbag(slot)
            if cb:
                out.extend(cb.fcurves)
    return out


def build_animation(C, leaf, rollers, seconds=5.0, fps=24, track="Open"):
    """Ein Clip: Blatt faehrt auf, Laufrollen drehen synchron mit.

    Zwei Fallstricke:
      * Rollen und Blatt brauchen dieselbe Interpolation, sonst schlupfen sie
        gegeneinander (weiches Anfahren beim einen, linear beim anderen).
      * Der glTF-Exporter gruppiert Kanaele nach NLA-Track-Namen. Ohne
        gemeinsamen Track wird je Objekt ein eigener Clip geschrieben - dann
        faehrt in der Engine das Blatt und die Rollen stehen still.
    """
    scn = bpy.context.scene
    scn.render.fps = fps
    f0, f1 = 1, int(1 + fps * seconds)
    scn.frame_start, scn.frame_end = f0, f1

    circ = 2 * math.pi * C["roller_r"]
    # R_y(+t) fuehrt +Z nach +X: bei positivem Winkel laeuft der Rollenscheitel
    # in +X, also in dieselbe Richtung wie die Schiene beim Oeffnen.
    ang = (C["TRV"] / circ) * 2 * math.pi

    for o in [leaf] + rollers:
        o.animation_data_clear()
    leaf.location = (0, 0, 0); leaf.keyframe_insert("location", frame=f0)
    leaf.location = (C["TRV"], 0, 0); leaf.keyframe_insert("location", frame=f1)
    leaf.animation_data.action.name = "SlidingGate_" + track
    for r in rollers:
        r.rotation_euler = (0, 0, 0); r.keyframe_insert("rotation_euler", frame=f0)
        r.rotation_euler = (0, ang, 0); r.keyframe_insert("rotation_euler", frame=f1)

    for o in [leaf] + rollers:
        for fc in fcurves_of(o):
            for kp in fc.keyframe_points:
                kp.interpolation = 'BEZIER'
                kp.handle_left_type = kp.handle_right_type = 'AUTO_CLAMPED'
            fc.update()

    # Schlupfpruefung: Weg/Umfang muss jederzeit der Umdrehungszahl entsprechen
    worst = 0.0
    for f in range(f0, f1 + 1, 4):
        scn.frame_set(f)
        worst = max(worst, abs(rollers[0].rotation_euler.y / (2 * math.pi)
                               - leaf.location.x / circ))
    scn.frame_set(f0)
    if worst > 1e-3:
        raise RuntimeError("Rollen schlupfen gegen das Blatt: %.5f Umdrehungen" % worst)

    # Alles auf einen gemeinsamen NLA-Track, aktive Action leeren
    for o in [leaf] + rollers:
        ad = o.animation_data; act = ad.action
        for t in list(ad.nla_tracks):
            ad.nla_tracks.remove(t)
        tr = ad.nla_tracks.new(); tr.name = track
        strip = tr.strips.new(track, int(act.frame_range[0]), act)
        slot = getattr(ad, "action_slot", None)
        if slot is not None:
            try: strip.action_slot = slot
            except Exception: pass
        ad.action = None
    return {"track": track, "frames": [f0, f1], "fps": fps,
            "turns": C["TRV"] / circ, "max_slip": worst}


def build_colliders(C, colcoll):
    viz = bpy.data.materials.get("SG_ColliderViz") or bpy.data.materials.new("SG_ColliderViz")
    viz.use_nodes = True
    nt = viz.node_tree
    for n in list(nt.nodes): nt.nodes.remove(n)
    o = nt.nodes.new('ShaderNodeOutputMaterial')
    t = nt.nodes.new('ShaderNodeBsdfTransparent')
    nt.links.new(t.outputs[0], o.inputs['Surface'])
    viz.diffuse_color = (0.1, 0.9, 0.3, 0.25)

    gx, cxa, cxb, L = C["gx"], C["cxa"], C["cxb"], C["L"]
    h, gc, bh = C["height"], C["ground_clear"], C["beam_h"]
    coldef = {
        "SlidingGate_Static": [
            ((-0.12, 0.0, 1.05),  (0.10, 0.10, 2.10)),
            ((-0.12, 0.0, -0.16), (0.36, 0.36, 0.32)),
            ((gx, -0.18, (h + 0.20) / 2), (C["gpost"], C["gpost"], h + 0.20)),
            ((gx, -0.075, h + 0.06), (0.12, 0.33, 0.08)),
            ((gx, -0.18, -0.16), (0.36, 0.36, 0.32)),
            ((cxa, 0.0, 0.035), (0.30, 0.16, 0.07)),
            ((cxa, 0.0, -0.16), (0.42, 0.34, 0.32)),
            ((cxb, 0.0, 0.035), (0.30, 0.16, 0.07)),
            ((cxb, 0.0, -0.16), (0.42, 0.34, 0.32)),
        ],
        "SlidingGate_Leaf": [
            ((L / 2, 0.0, gc + bh / 2), (L, C["beam_w"], bh)),
            ((L / 2, 0.0, (gc + bh + h) / 2), (L, 0.06, h - gc - bh)),
        ],
    }
    made = []
    for asset, boxes in coldef.items():
        par = bpy.data.objects[asset]
        for i, (c, d) in enumerate(boxes):
            me = bpy.data.meshes.new("%s_COL_%02d" % (asset, i)); bm = bmesh.new()
            M = Matrix.Translation(Vector(c)) @ Matrix.Diagonal((d[0], d[1], d[2], 1.0))
            bmesh.ops.create_cube(bm, size=1.0, matrix=M); bm.to_mesh(me); bm.free()
            ob = bpy.data.objects.new(me.name, me); ob.data.materials.append(viz)
            colcoll.objects.link(ob)
            ob.parent = par                       # Collider folgt dem beweglichen Teil
            ob.matrix_parent_inverse = par.matrix_world.inverted()
            # Ohne hide_render rendern sie als opake weisse Bloecke vor dem Tor:
            # use_nodes ueberschreibt diffuse_color, die Transparenz greift nicht.
            ob.hide_render = True
            ob.display_type = 'WIRE'
            made.append(ob)
    return made

# =========================================================== MAIN

def main(cfg=None):
    C = derive(cfg or CONFIG)
    reset_scene()
    kit = get_coll("SlidingGate")
    colc = get_coll("SlidingGate_Colliders")
    os.makedirs(C["outdir"], exist_ok=True)
    texdir = os.path.join(C["outdir"], "textures"); os.makedirs(texdir, exist_ok=True)

    leaf, static, rollers = build_geometry(C, kit)

    hits = check_travel(leaf, static, rollers, C)
    if hits:
        raise RuntimeError("Statik schneidet den Verfahrweg an %d Stellen: %r" % (len(hits), hits[:3]))

    # Die drei Rollen-Instanzen teilen sich das Mesh von Roller_00. Fuer Fase
    # und Unwrap muss es Single-User sein, danach wieder verlinken.
    tmp = bpy.data.meshes.new("SG_tmp")
    for r in rollers[1:]: r.data = tmp
    unique = [leaf, static, rollers[0]]
    bevel_apply(unique, C["bevel"], C["bevel_seg"])
    uv_unwrap(unique)
    setup_cycles(C["samples"])
    bake_asset(leaf.name,       texdir, C["res_bc"],    C["res_mr"],       C["res_nm"],       C["ao_distance"])
    bake_asset(static.name,     texdir, C["res_bc"],    C["res_mr"],       C["res_nm"],       C["ao_distance"])
    bake_asset(rollers[0].name, texdir, C["res_small"], C["res_small"]//2, C["res_small"]//2, C["ao_distance"])
    for r in rollers[1:]: r.data = rollers[0].data
    if tmp.users == 0: bpy.data.meshes.remove(tmp)

    for ob in unique:
        ob.data.materials.clear()
        ob.data.materials.append(textured_material(ob.name))

    build_colliders(C, colc)
    anim = build_animation(C, leaf, rollers)

    root = bpy.data.objects.get("SlidingGate")
    if root is None:
        root = bpy.data.objects.new("SlidingGate", None)
        root.empty_display_type = 'PLAIN_AXES'; root.empty_display_size = 0.6
        kit.objects.link(root)
    root.location = (0, 0, 0)
    for ob in [static, leaf] + rollers:
        ob.parent = root
        ob.matrix_parent_inverse = root.matrix_world.inverted()

    if bpy.context.mode != 'OBJECT':
        bpy.ops.object.mode_set(mode='OBJECT')
    bpy.ops.object.select_all(action='DESELECT')
    for ob in list(kit.objects) + list(colc.objects):
        ob.select_set(True)
    bpy.context.view_layer.objects.active = leaf
    glb = os.path.join(C["outdir"], "SlidingGate_v3.glb")
    bpy.ops.export_scene.gltf(filepath=glb, export_format='GLB', use_selection=True,
                              export_apply=True, export_image_format='AUTO', export_yup=True,
                              export_animations=True, export_animation_mode='NLA_TRACKS',
                              export_frame_range=False)
    print("OK  %s  (%.2f MB)" % (glb, os.path.getsize(glb) / 1e6))
    print("    Verfahrweg %.2f m entlang +X, Rollen %.2f Umdrehungen"
          % (C["TRV"], anim["turns"]))
    print("    Clip '%s', Frames %d-%d @ %d fps, Schlupf %.2e Umdrehungen"
          % (anim["track"], anim["frames"][0], anim["frames"][1], anim["fps"], anim["max_slip"]))
    return glb


if __name__ == "__main__":
    main()
