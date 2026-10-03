#!/usr/bin/env python3
"""
FenceKit - Zaunfeld, Zaunpfosten und animiertes Gehfluegeltor.

Passt zum SlidingGate: gleiche Hoehe, gleicher Stabdurchmesser, gleiche
Teilung, gleiche Materialien. Der Stil ist nicht nachgebaut, sondern
importiert - Materialien, Fase, Bake- und Exportlogik kommen direkt aus
sliding_gate_gen.py.

    blender --background --python fence_kit_gen.py

Module und ihre Origins (Origins sind das, was das Kit zusammensteckbar macht):

    Fence_Post        Boden, Pfostenachse           -> auf das Raster setzen
    Fence_Panel       Boden, linke Feldkante        -> zwischen zwei Pfosten
    PedGate_Static    Boden, Achse des Angelpfostens
    PedGate_Leaf      Boden, Scharnierachse         -> reine Rotation um lokal Z

Lizenz der erzeugten Assets: CC0 1.0
"""

import bpy, bmesh, math, os, sys, importlib.util
from mathutils import Vector, Matrix

HERE = os.path.dirname(os.path.abspath(__file__)) if "__file__" in globals() \
    else "/Volumes/media/Blender/SlidingGate"


def load_base():
    """sliding_gate_gen als Modul laden - Helfer, Materialien, Bake, Export."""
    p = os.path.join(HERE, "sliding_gate_gen.py")
    spec = importlib.util.spec_from_file_location("sliding_gate_gen", p)
    m = importlib.util.module_from_spec(spec)
    sys.modules["sliding_gate_gen"] = m
    spec.loader.exec_module(m)
    return m


SG = load_base()
tube, box, disc, part = SG.tube, SG.box, SG.disc, SG.part
sock, osock, finalize, get_coll = SG.sock, SG.osock, SG.finalize, SG.get_coll

# =========================================================== CONFIG

CONFIG = {
    # --- gemeinsam mit dem Schiebetor ---
    "height":       2.00,   # Oberkante ueber Boden [m]
    "bar_d":        0.016,  # Fuellstab-Durchmesser [m]
    "bar_gap":      0.12,   # Achsabstand der Fuellstaebe [m]
    "frame":        0.06,   # Rahmenprofil [m]

    # --- Zaun ---
    "panel_width":  2.00,   # Rastermass: Pfostenachse zu Pfostenachse [m]
    "panel_half":   1.00,   # Halbfeld zur Laufanpassung [m]
    "post":         0.08,   # Zaunpfosten Kantenlaenge [m]
    "post_corner":  0.10,   # Eck-/Endpfosten, kraeftiger [m]
    "rail_w":       0.045,  # Riegeltiefe [m]
    "panel_clear":  0.10,   # Bodenfreiheit der Fuellung [m]

    # --- Gehfluegeltor ---
    "gate_opening": 1.00,   # lichte Durchgangsbreite [m]
    "gate_post":    0.10,   # Torpfosten Kantenlaenge [m]
    "swing_deg":    95.0,   # Oeffnungswinkel
    "swing_sec":    1.6,    # Dauer der Animation [s]

    # --- Doppelfluegeltor ---
    # 4,00 m ist bewusst dieselbe lichte Weite wie beim Schiebetor: beide Tore
    # passen damit an dieselbe Stelle und sind gegeneinander austauschbar.
    "dgate_opening": 4.00,  # lichte Durchfahrtsbreite [m]
    "dgate_post":    0.12,  # Torpfosten, kraeftiger als beim Gehfluegel [m]
    "dgate_gap":     0.016, # Spalt zwischen den Fluegeln in der Mitte [m]
    "dgate_sec":     2.6,   # Dauer der Animation [s]

    # --- Detaillierung / Ausgabe ---
    "penetration":  0.012,
    "bevel":        0.0025,
    "bevel_seg":    2,
    "ao_distance":  0.06,
    "outdir":       HERE,
    "res_panel":    2048,
    "res_post":     1024,
    "res_gate":     2048,
    "samples":      16,
    "fps":          24,
}


def derive(cfg):
    d = dict(cfg)
    gp = cfg["gate_post"]
    # Lichte Weite = Abstand der Pfosteninnenkanten
    d["gx_latch"] = cfg["gate_opening"] + gp

    # Scharnierachse liegt NEBEN dem Pfosten, nicht in seiner Flucht.
    # Saesse sie in der Pfostenachse, faehrt die Bandseite des Fluegels beim
    # Oeffnen durch den Pfosten. Reale Torbaender setzen den Bolzen deshalb
    # vor die Pfostenflanke; der Fluegel haengt versetzt davor.
    d["hinge_x"] = 0.0
    d["hinge_y"] = -(gp / 2 + 0.032)
    d["leaf_t"] = 0.05                        # Fluegel-Rahmentiefe
    # Schlosskante endet VOR der Pfosteninnenkante. Reicht der Fluegel bis in
    # die Pfostenachse, steckt der Schlossholm im Pfosten und der Druecker
    # ragt seitlich hinein - beides faellt erst in der Anschlagpruefung auf.
    d["leaf_gap"] = 0.008
    d["leaf_w"] = d["gx_latch"] - gp / 2 - d["leaf_gap"]
    # Anschlagblech auf der Aussenflanke des Riegelpfostens, greift ueber die
    # Schlosskante des Fluegels
    d["stop_x"] = d["gx_latch"] - gp / 2 - 0.005
    d["stop_y0"] = -gp / 2
    d["stop_y1"] = d["hinge_y"] + d["leaf_t"] / 2 - 0.001

    # --- Doppelfluegeltor ---
    dp = cfg["dgate_post"]
    d["dg_right"] = cfg["dgate_opening"] + dp        # Achsabstand der Torpfosten
    d["dg_hinge_y"] = -(dp / 2 + 0.034)              # Scharnier neben dem Pfosten
    d["dg_leaf_t"] = 0.06                            # Fluegel-Rahmentiefe
    # Beide Fluegel treffen sich mittig, dazwischen bleibt dgate_gap
    d["dg_leaf_w"] = d["dg_right"] / 2 - cfg["dgate_gap"] / 2
    return d

# =========================================================== GEOMETRIE


def build_kit(C, kit):
    M = {k: SG.build_material(k) for k in SG.MATSPEC}
    h, fr = C["height"], C["frame"]
    pen = C["penetration"]
    made = {}

    # ---------------- Zaunpfosten -------------------------------------
    # Origin: Boden, Pfostenachse. Auf das Raster setzen, fertig.
    def make_post(name, ps, fund):
        bm = bmesh.new()
        part(bm, 0, lambda: (
            box(bm, (0, 0, h / 2 + 0.025), (ps, ps, h + 0.05)),          # Pfosten
            box(bm, (0, 0, h + 0.056), (ps + 0.016, ps + 0.016, 0.012)), # Kappe
        ))
        part(bm, 1, lambda: box(bm, (0, 0, -0.16), (fund, fund, 0.32)))  # Fundament
        return finalize(name, bm, [M["SG_Verzinkt"], M["SG_Beton"]], kit)

    ps = C["post"]
    made["Fence_Post"] = make_post("Fence_Post", ps, 0.30)
    # Eck- und Endpfosten nehmen Last aus zwei Richtungen auf und sind an
    # echten Zaeunen sichtbar kraeftiger. Geometrisch noetig ist er nicht -
    # eine 90-Grad-Ecke funktioniert auch mit dem Standardpfosten.
    made["Fence_Post_Corner"] = make_post("Fence_Post_Corner", C["post_corner"], 0.36)

    # ---------------- Zaunfelder --------------------------------------
    # Origin: Boden, linke Feldkante. Das Feld spannt bis zur Rasterweite,
    # sodass zwei Pfosten auf dem Raster genau die Enden treffen.
    rw = C["rail_w"]
    z0 = C["panel_clear"]                      # Unterkante Fuellung
    zb = z0 + fr                               # Oberkante unterer Riegel
    zt = h - fr                                # Unterkante oberer Riegel

    def make_panel(name, W):
        x0, x1 = ps / 2, W - ps / 2            # Riegel laufen zwischen den Pfosten
        bm = bmesh.new()
        def frame_():
            box(bm, ((x0 + x1) / 2, 0, z0 + fr / 2), (x1 - x0, rw, fr))
            box(bm, ((x0 + x1) / 2, 0, h - fr / 2), (x1 - x0, rw, fr))
        part(bm, 0, frame_)
        def bars_():
            # Stabraster von der Feldmitte aus verteilen, damit das Halbfeld
            # kein angeschnittenes Randfach bekommt
            n = max(1, int(round((x1 - x0) / C["bar_gap"])))
            step = (x1 - x0) / n
            for i in range(n):
                x = x0 + step * (i + 0.5)
                tube(bm, (x, 0, zb - pen), (x, 0, zt + pen), C["bar_d"] / 2, 10)
        part(bm, 1, bars_)
        # Riegel UND Staebe anthrazit - wie der Torrahmen. Verzinkt bleiben nur
        # Pfosten, Beschlaege und die Laufschiene des Schiebetors; sonst faellt
        # der Zaun im Aufbau als Fremdkoerper auf.
        return finalize(name, bm, [M["SG_Anthrazit"], M["SG_Anthrazit"]], kit)

    made["Fence_Panel"]    = make_panel("Fence_Panel", C["panel_width"])
    made["Fence_Panel_1m"] = make_panel("Fence_Panel_1m", C["panel_half"])

    # ---------------- Gehfluegeltor: Statik ---------------------------
    gp = C["gate_post"]; gl = C["gx_latch"]
    bm = bmesh.new()

    hy = C["hinge_y"]

    def gate_steel():
        for px in (0.0, gl):
            box(bm, (px, 0, h / 2 + 0.05), (gp, gp, h + 0.10))
            box(bm, (px, 0, h + 0.106), (gp + 0.018, gp + 0.018, 0.012))
        # Bandkonsolen: tragen den Bolzen vor der Pfostenflanke
        for hz in (0.42, h - 0.42):
            box(bm, (0.0, (hy - gp / 2) / 2, hz), (0.07, abs(hy) - gp / 2 + 0.01, 0.05))
            disc(bm, (0.0, hy, hz), 0.019, 0.075, 'Z', 16)
        # Anschlag-/Schliessblech: greift von der Pfostenflanke ueber die
        # Schlosskante des Fluegels, ohne ihn zu beruehren
        sy0, sy1 = C["stop_y0"], C["stop_y1"]
        box(bm, (C["stop_x"], (sy0 + sy1) / 2, 1.05), (0.05, abs(sy1 - sy0), 0.20))
    part(bm, 0, gate_steel)
    part(bm, 1, lambda: [box(bm, (px, 0, -0.16), (0.34, 0.34, 0.32)) for px in (0.0, gl)])
    made["PedGate_Static"] = finalize("PedGate_Static", bm,
                                      [M["SG_Verzinkt"], M["SG_Beton"]], kit)

    # ---------------- Gehfluegeltor: Fluegel --------------------------
    # Origin (0,0,0) liegt auf der Scharnierachse am Boden. Damit ist Oeffnen
    # eine reine Rotation um lokal Z - kein Offset, kein Nachjustieren.
    LW = C["leaf_w"]; lt = C["leaf_t"]
    bm = bmesh.new()

    def leaf_frame():
        box(bm, (0.03, 0, (z0 + h) / 2), (0.06, lt, h - z0))               # Bandholm
        box(bm, (LW - 0.03, 0, (z0 + h) / 2), (0.06, lt, h - z0))          # Schlossholm
        box(bm, (LW / 2, 0, z0 + fr / 2), (LW, lt * 0.9, fr))              # unterer Riegel
        box(bm, (LW / 2, 0, h - fr / 2), (LW, lt * 0.9, fr))               # oberer Riegel
    part(bm, 0, leaf_frame)

    def leaf_bars():
        x = 0.06 + C["bar_gap"] / 2
        while x < LW - 0.06:
            tube(bm, (x, 0, z0 + fr - pen), (x, 0, h - fr + pen), C["bar_d"] / 2, 10)
            x += C["bar_gap"]
    part(bm, 1, leaf_bars)

    def leaf_hw():
        for hz in (0.42, h - 0.42):                                        # Bandlappen am Fluegel
            disc(bm, (0.0, 0, hz), 0.017, 0.115, 'Z', 16)
            box(bm, (0.045, 0, hz), (0.09, 0.030, 0.05))
        for sy in (-1, 1):                                                 # Druecker beidseitig
            box(bm, (LW - 0.10, sy * (lt / 2 + 0.006), 1.05), (0.075, 0.012, 0.13))
            tube(bm, (LW - 0.10, sy * (lt / 2 + 0.010), 1.05),
                 (LW - 0.10, sy * (lt / 2 + 0.045), 1.05), 0.011, 10)
            tube(bm, (LW - 0.10, sy * (lt / 2 + 0.042), 1.05),
                 (LW - 0.145, sy * (lt / 2 + 0.042), 1.045), 0.010, 10)
    part(bm, 2, leaf_hw)
    made["PedGate_Leaf"] = finalize("PedGate_Leaf", bm,
                                    [M["SG_Anthrazit"], M["SG_Anthrazit"], M["SG_Verzinkt"]], kit)

    # ---------------- Doppelfluegeltor --------------------------------
    dp, dr = C["dgate_post"], C["dg_right"]
    dhy, dlt, DW = C["dg_hinge_y"], C["dg_leaf_t"], C["dg_leaf_w"]
    bm = bmesh.new()

    def dg_steel():
        for px in (0.0, dr):
            box(bm, (px, 0, h / 2 + 0.07), (dp, dp, h + 0.14))
            box(bm, (px, 0, h + 0.146), (dp + 0.020, dp + 0.020, 0.014))
            for hz in (0.45, h - 0.40):                       # Bandkonsolen
                sx = 1 if px == 0.0 else -1
                box(bm, (px, (dhy - dp / 2) / 2, hz), (0.08, abs(dhy) - dp / 2 + 0.012, 0.055))
                disc(bm, (px, dhy, hz), 0.021, 0.085, 'Z', 16)
        # Bodenhuelse fuer den Fallriegel, mittig
        box(bm, (dr / 2, dhy, 0.006), (0.10, 0.10, 0.012))
        disc(bm, (dr / 2, dhy, 0.010), 0.016, 0.016, 'Z', 14)
    part(bm, 0, dg_steel)
    part(bm, 1, lambda: [box(bm, (px, 0, -0.18), (0.42, 0.42, 0.36)) for px in (0.0, dr)])
    made["DblGate_Static"] = finalize("DblGate_Static", bm,
                                      [M["SG_Verzinkt"], M["SG_Beton"]], kit)

    # Der rechte Fluegel ist ein echt gespiegelter Koerper, kein um 180 Grad
    # gedrehter. Grund: der Fallriegel sitzt auf der Aussenflanke und macht das
    # Blatt in Y unsymmetrisch - gedreht laegen die beiden Fluegel 30 mm
    # versetzt und die Riegel auf gegenueberliegenden Seiten. Kostet einen
    # zweiten Texturensatz, dafuer ist "geschlossen" bei beiden 0 Grad.
    bm = bmesh.new()
    def dg_frame():
        box(bm, (0.035, 0, (z0 + h) / 2), (0.07, dlt, h - z0))            # Bandholm
        box(bm, (DW - 0.035, 0, (z0 + h) / 2), (0.07, dlt, h - z0))       # Schlossholm
        box(bm, (DW / 2, 0, z0 + fr / 2), (DW, dlt * 0.9, fr))            # unterer Riegel
        box(bm, (DW / 2, 0, h - fr / 2), (DW, dlt * 0.9, fr))             # oberer Riegel
        box(bm, (DW / 2, 0, (z0 + h) / 2), (0.05, dlt * 0.85, h - z0))    # Mittelpfosten
    part(bm, 0, dg_frame)
    def dg_bars():
        x0b, x1b = 0.07, DW - 0.07
        n = max(1, int(round((x1b - x0b) / C["bar_gap"])))
        step = (x1b - x0b) / n
        for i in range(n):
            x = x0b + step * (i + 0.5)
            if abs(x - DW / 2) < 0.035: continue                          # Mittelpfosten frei
            tube(bm, (x, 0, z0 + fr - pen), (x, 0, h - fr + pen), C["bar_d"] / 2, 10)
    part(bm, 1, dg_bars)
    def dg_hw():
        for hz in (0.45, h - 0.40):                                       # Bandlappen
            disc(bm, (0.0, 0, hz), 0.019, 0.125, 'Z', 16)
            box(bm, (0.05, 0, hz), (0.10, 0.034, 0.058))
        # Fallriegel am Schlossholm
        tube(bm, (DW - 0.035, -dlt / 2 - 0.018, 0.06), (DW - 0.035, -dlt / 2 - 0.018, 1.15), 0.012, 10)
        for gz in (0.30, 1.00):
            box(bm, (DW - 0.035, -dlt / 2 - 0.010, gz), (0.05, 0.022, 0.045))
        tube(bm, (DW - 0.035, -dlt / 2 - 0.018, 1.15),
             (DW - 0.10, -dlt / 2 - 0.018, 1.20), 0.011, 10)
    part(bm, 2, dg_hw)
    mats3 = [M["SG_Anthrazit"], M["SG_Anthrazit"], M["SG_Verzinkt"]]
    # bm wird von finalize freigegeben - vorher spiegeln
    bm2 = bm.copy()
    bmesh.ops.scale(bm2, vec=Vector((-1, 1, 1)), verts=bm2.verts)
    bmesh.ops.reverse_faces(bm2, faces=bm2.faces)   # sonst zeigen die Normalen nach innen
    made["DblGate_Leaf_L"] = finalize("DblGate_Leaf_L", bm, mats3, kit)
    made["DblGate_Leaf_R"] = finalize("DblGate_Leaf_R", bm2, mats3, kit)
    # Slot 0 und 1 zeigen bewusst auf dasselbe Material: Rahmen und Staebe sind
    # gleich lackiert, die Trennung bleibt fuer spaetere Varianten erhalten.
    return made


def place_leaf(C, made):
    """Fluegel an ihre Scharnierachsen setzen."""
    made["PedGate_Leaf"].location = (C["hinge_x"], C["hinge_y"], 0)
    made["DblGate_Leaf_L"].location = (0.0, C["dg_hinge_y"], 0)
    made["DblGate_Leaf_R"].location = (C["dg_right"], C["dg_hinge_y"], 0)


def check_double_swing(C, made, steps=36):
    """Beide Fluegel gegen ihre Pfosten UND gegeneinander pruefen."""
    import mathutils
    leaf = made["DblGate_Leaf_L"]
    pts = sorted({(round(v.co.x, 3), round(v.co.y, 3)) for v in leaf.data.vertices})
    dp, dr, dhy = C["dgate_post"], C["dg_right"], C["dg_hinge_y"]
    posts = [(0.0, 0.0), (dr, 0.0)]
    EPS = 0.002
    hits = []

    ptsR = sorted({(round(v.co.x, 3), round(v.co.y, 3))
                   for v in made["DblGate_Leaf_R"].data.vertices})

    def world(pt, hinge, ang):
        ca, sa = math.cos(ang), math.sin(ang)
        return (hinge[0] + pt[0]*ca - pt[1]*sa, hinge[1] + pt[1]*ca + pt[0]*sa)

    for i in range(steps + 1):
        ang = math.radians(-C["swing_deg"] * i / steps)
        L = [world(p, (0.0, dhy), ang) for p in pts]
        R = [world(p, (dr, dhy), -ang) for p in ptsR]
        for tag, pl in (("L", L), ("R", R)):
            for wx, wy in pl:
                for px, py in posts:
                    if abs(wx-px) < dp/2 - EPS and abs(wy-py) < dp/2 - EPS:
                        hits.append({"fluegel": tag, "grad": round(math.degrees(ang),1),
                                     "punkt": (round(wx,3), round(wy,3))})
        # gegenseitig: nur im nahezu geschlossenen Zustand kritisch
        if i <= 2:
            lx = max(p[0] for p in L); rx = min(p[0] for p in R)
            if lx > rx + EPS:
                hits.append({"fluegel": "L/R", "grad": round(math.degrees(ang),1),
                             "ueberlappung_mm": round((lx-rx)*1000,1)})
    return hits


def check_swing(C, made, steps=36):
    """Schlaegt der Fluegel beim Oeffnen an einen Pfosten?

    Geprueft werden die ECHTEN Vertices im Grundriss, nicht die Ecken der
    Bounding-Box: bei einem Tor mit Bandlappen auf der einen und Druecker auf
    der anderen Seite existiert die Kombination min-x/max-y gar nicht, und die
    BBox meldet Anschlaege, die es nicht gibt.
    """
    leaf = made["PedGate_Leaf"]
    pts = sorted({(round(v.co.x, 3), round(v.co.y, 3)) for v in leaf.data.vertices})
    hx, hy = C["hinge_x"], C["hinge_y"]
    gp, gl = C["gate_post"], C["gx_latch"]
    posts = [(0.0, 0.0), (gl, 0.0)]
    EPS = 0.002
    hits = []
    for i in range(steps + 1):
        a = math.radians(-C["swing_deg"] * i / steps)
        ca, sa = math.cos(a), math.sin(a)
        for cx, cy in pts:
            wx = hx + cx * ca - cy * sa
            wy = hy + cy * ca + cx * sa
            for px, py in posts:
                if abs(wx - px) < gp / 2 - EPS and abs(wy - py) < gp / 2 - EPS:
                    hits.append({"grad": round(math.degrees(a), 1),
                                 "punkt": (round(wx, 3), round(wy, 3)),
                                 "pfosten_x": px})
    return hits


def build_colliders(C, made, colcoll):
    viz = bpy.data.materials.get("SG_ColliderViz") or bpy.data.materials.new("SG_ColliderViz")
    h, ps, gp, gl = C["height"], C["post"], C["gate_post"], C["gx_latch"]
    LW, z0 = C["leaf_w"], C["panel_clear"]
    W, Wh, pc = C["panel_width"], C["panel_half"], C["post_corner"]
    coldef = {
        "Fence_Post":        [((0, 0, h / 2 + 0.025), (ps, ps, h + 0.05)),
                              ((0, 0, -0.16), (0.30, 0.30, 0.32))],
        "Fence_Post_Corner": [((0, 0, h / 2 + 0.025), (pc, pc, h + 0.05)),
                              ((0, 0, -0.16), (0.36, 0.36, 0.32))],
        "Fence_Panel":       [((W / 2, 0, (z0 + h) / 2), (W - ps, 0.05, h - z0))],
        "Fence_Panel_1m":    [((Wh / 2, 0, (z0 + h) / 2), (Wh - ps, 0.05, h - z0))],
        "PedGate_Static": [((0, 0, h / 2 + 0.05), (gp, gp, h + 0.10)),
                           ((0, 0, -0.16), (0.34, 0.34, 0.32)),
                           ((gl, 0, h / 2 + 0.05), (gp, gp, h + 0.10)),
                           ((gl, 0, -0.16), (0.34, 0.34, 0.32))],
        "PedGate_Leaf":   [((LW / 2, 0, (z0 + h) / 2), (LW, 0.05, h - z0))],
        "DblGate_Static": [((0, 0, h / 2 + 0.07), (C["dgate_post"], C["dgate_post"], h + 0.14)),
                           ((0, 0, -0.18), (0.42, 0.42, 0.36)),
                           ((C["dg_right"], 0, h / 2 + 0.07),
                            (C["dgate_post"], C["dgate_post"], h + 0.14)),
                           ((C["dg_right"], 0, -0.18), (0.42, 0.42, 0.36))],
        "DblGate_Leaf_L": [((C["dg_leaf_w"] / 2, 0, (z0 + h) / 2),
                            (C["dg_leaf_w"], C["dg_leaf_t"], h - z0))],
        "DblGate_Leaf_R": [((-C["dg_leaf_w"] / 2, 0, (z0 + h) / 2),
                            (C["dg_leaf_w"], C["dg_leaf_t"], h - z0))],
    }
    out = []
    for asset, boxes in coldef.items():
        par = made[asset]
        for i, (c, d) in enumerate(boxes):
            me = bpy.data.meshes.new("%s_COL_%02d" % (asset, i)); bm = bmesh.new()
            Mx = Matrix.Translation(Vector(c)) @ Matrix.Diagonal((d[0], d[1], d[2], 1.0))
            bmesh.ops.create_cube(bm, size=1.0, matrix=Mx); bm.to_mesh(me); bm.free()
            ob = bpy.data.objects.new(me.name, me); ob.data.materials.append(viz)
            colcoll.objects.link(ob)
            ob.parent = par
            ob.matrix_parent_inverse = par.matrix_world.inverted()
            ob.hide_render = True; ob.display_type = 'WIRE'
            out.append(ob)
    return out


def _swing(obj, deg, f0, f1, track, prefix):
    """Ein Fluegel: Keyframes, gleiche Interpolation, eigener NLA-Track.

    Der glTF-Exporter gruppiert Kanaele nach Track-Namen. Alle Fluegel eines
    Tores bekommen deshalb denselben Track, sonst schreibt er je Objekt einen
    eigenen Clip und in der Engine bewegt sich nur eines der Bleche.
    """
    obj.animation_data_clear()
    obj.rotation_euler = (0, 0, 0); obj.keyframe_insert("rotation_euler", frame=f0)
    obj.rotation_euler = (0, 0, math.radians(deg))
    obj.keyframe_insert("rotation_euler", frame=f1)
    obj.animation_data.action.name = prefix + "_" + track
    for fc in SG.fcurves_of(obj):
        for kp in fc.keyframe_points:
            kp.interpolation = 'BEZIER'
            kp.handle_left_type = kp.handle_right_type = 'AUTO_CLAMPED'
        fc.update()
    ad = obj.animation_data; act = ad.action
    for t in list(ad.nla_tracks):
        ad.nla_tracks.remove(t)
    tr = ad.nla_tracks.new(); tr.name = track
    strip = tr.strips.new(track, int(act.frame_range[0]), act)
    slot = getattr(ad, "action_slot", None)
    if slot is not None:
        try: strip.action_slot = slot
        except Exception: pass
    ad.action = None


def build_swing_animation(C, made, track="Open"):
    """Beide Tore animieren. Der Gehfluegel bestimmt die Szenenlaenge, das
    Doppeltor laeuft laenger und wird deshalb zuerst gesetzt."""
    scn = bpy.context.scene
    scn.render.fps = C["fps"]
    fd1 = int(1 + C["fps"] * C["dgate_sec"])
    fp1 = int(1 + C["fps"] * C["swing_sec"])
    scn.frame_start, scn.frame_end = 1, max(fd1, fp1)

    _swing(made["PedGate_Leaf"], -C["swing_deg"], 1, fp1, track, "PedGate")
    # Beide Fluegel schwenken nach -Y. Der rechte ist gespiegelt gebaut,
    # deshalb dreht er in die andere Richtung um dieselbe Achse.
    _swing(made["DblGate_Leaf_L"], -C["swing_deg"], 1, fd1, track, "DblGate_L")
    _swing(made["DblGate_Leaf_R"],  C["swing_deg"], 1, fd1, track, "DblGate_R")
    scn.frame_set(1)
    return {"track": track, "ped_frames": [1, fp1], "dbl_frames": [1, fd1],
            "deg": C["swing_deg"]}


def export_module(name, objs, path):
    if bpy.context.mode != 'OBJECT':
        bpy.ops.object.mode_set(mode='OBJECT')
    bpy.ops.object.select_all(action='DESELECT')
    for o in objs:
        o.hide_set(False); o.hide_viewport = False; o.select_set(True)
        for c in o.children:
            c.hide_set(False); c.hide_viewport = False; c.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]
    bpy.ops.export_scene.gltf(filepath=path, export_format='GLB', use_selection=True,
                              export_apply=True, export_image_format='AUTO',
                              export_yup=True, export_animations=True,
                              export_animation_mode='NLA_TRACKS', export_frame_range=False)
    return round(os.path.getsize(path) / 1e6, 2)


def reset():
    for cn in ("FenceKit", "FenceKit_Colliders"):
        c = bpy.data.collections.get(cn)
        if c:
            for o in list(c.objects):
                bpy.data.objects.remove(o, do_unlink=True)
    for m in list(bpy.data.meshes):
        if m.users == 0:
            bpy.data.meshes.remove(m)


def main(cfg=None):
    C = derive(cfg or CONFIG)
    reset()
    kit = get_coll("FenceKit"); colc = get_coll("FenceKit_Colliders")
    texdir = os.path.join(C["outdir"], "textures"); os.makedirs(texdir, exist_ok=True)

    made = build_kit(C, kit)
    place_leaf(C, made)
    hits = check_swing(C, made)
    if hits:
        raise RuntimeError("Fluegel schlaegt an: %r" % hits[:3])

    order = ["Fence_Post", "Fence_Post_Corner", "Fence_Panel", "Fence_Panel_1m",
             "PedGate_Static", "PedGate_Leaf",
             "DblGate_Static", "DblGate_Leaf_L", "DblGate_Leaf_R"]
    objs = [made[n] for n in order]
    SG.bevel_apply(objs, C["bevel"], C["bevel_seg"])
    SG.uv_unwrap(objs)
    SG.setup_cycles(C["samples"])
    res = {"Fence_Post": C["res_post"], "Fence_Post_Corner": C["res_post"],
           "Fence_Panel": C["res_panel"], "Fence_Panel_1m": C["res_panel"] // 2,
           "PedGate_Static": C["res_gate"], "PedGate_Leaf": C["res_gate"],
           "DblGate_Static": C["res_gate"], "DblGate_Leaf_L": C["res_gate"],
           "DblGate_Leaf_R": C["res_gate"]}
    for n in order:
        r = res[n]
        SG.bake_asset(n, texdir, r, r // 2, r // 2, C["ao_distance"])
        made[n].data.materials.clear()
        made[n].data.materials.append(SG.textured_material(n))

    build_colliders(C, made, colc)
    anim = build_swing_animation(C, made)

    paths = {}
    paths["Fence_Post"] = export_module(
        "Fence_Post", [made["Fence_Post"]], os.path.join(C["outdir"], "Fence_Post.glb"))
    paths["Fence_Post_Corner"] = export_module(
        "Fence_Post_Corner", [made["Fence_Post_Corner"]],
        os.path.join(C["outdir"], "Fence_Post_Corner.glb"))
    paths["Fence_Panel"] = export_module(
        "Fence_Panel", [made["Fence_Panel"]], os.path.join(C["outdir"], "Fence_Panel_2m.glb"))
    paths["Fence_Panel_1m"] = export_module(
        "Fence_Panel_1m", [made["Fence_Panel_1m"]],
        os.path.join(C["outdir"], "Fence_Panel_1m.glb"))
    paths["PedestrianGate"] = export_module(
        "PedestrianGate", [made["PedGate_Static"], made["PedGate_Leaf"]],
        os.path.join(C["outdir"], "PedestrianGate.glb"))
    paths["DoubleGate"] = export_module(
        "DoubleGate", [made["DblGate_Static"], made["DblGate_Leaf_L"], made["DblGate_Leaf_R"]],
        os.path.join(C["outdir"], "DoubleGate.glb"))

    print("OK  FenceKit")
    for k, v in paths.items():
        print("    %-16s %.2f MB" % (k, v))
    print("    Fluegel %.0f Grad um lokal Z, Frames %d-%d"
          % (anim["deg"], anim["frames"][0], anim["frames"][1]))
    return made, anim


if __name__ == "__main__":
    main()
