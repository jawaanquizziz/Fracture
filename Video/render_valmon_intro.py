"""Render the 13-second VALMON cinematic with Blender 5.2.

Run from the project root:
  blender -b -t 4 --python Video/render_valmon_intro.py
"""

import bpy
import math
import os
import random
from mathutils import Vector


ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, "Video")
os.makedirs(OUT, exist_ok=True)
FPS = 24
END = 13 * FPS
CYAN = (0.05, 0.82, 1.0, 1.0)
VIOLET = (0.57, 0.12, 1.0, 1.0)
NAVY = (0.004, 0.009, 0.025, 1.0)

bpy.ops.object.select_all(action="SELECT")
bpy.ops.object.delete(use_global=False)
for material in list(bpy.data.materials):
    bpy.data.materials.remove(material)

scene = bpy.context.scene
scene.render.engine = "BLENDER_EEVEE"
scene.eevee.taa_render_samples = 4
scene.render.resolution_x = 960
scene.render.resolution_y = 540
scene.render.resolution_percentage = 100
scene.render.fps = 12
scene.frame_start = 1
scene.frame_end = END
scene.frame_step = 2
scene.render.image_settings.color_mode = "RGB"
scene.render.image_settings.media_type = "VIDEO"
scene.render.image_settings.file_format = "FFMPEG"
scene.render.ffmpeg.format = "MPEG4"
scene.render.ffmpeg.codec = "H264"
scene.render.ffmpeg.constant_rate_factor = "MEDIUM"
scene.render.ffmpeg.ffmpeg_preset = "GOOD"
scene.render.filepath = os.path.join(OUT, "valmon_opening_silent.mp4")
scene.render.film_transparent = False
scene.world.color = NAVY[:3]
scene.world.use_nodes = True
scene.world.node_tree.nodes.get("Background").inputs["Color"].default_value = NAVY
scene.world.node_tree.nodes.get("Background").inputs["Strength"].default_value = 0.25
scene.view_settings.view_transform = "AgX"
scene.view_settings.look = "AgX - Medium High Contrast"


def key_value(obj, prop, frame, value):
    setattr(obj, prop, value)
    obj.keyframe_insert(data_path=prop, frame=frame)


def set_linear(obj):
    if not obj.animation_data or not obj.animation_data.action:
        return
    for curve in obj.animation_data.action.fcurves:
        for point in curve.keyframe_points:
            point.interpolation = "BEZIER"


def mat(name, color, strength=0.0, metallic=0.0):
    m = bpy.data.materials.new(name)
    m.diffuse_color = color
    m.use_nodes = True
    bsdf = m.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = color
    bsdf.inputs["Metallic"].default_value = metallic
    bsdf.inputs["Roughness"].default_value = 0.35
    bsdf.inputs["Emission Color"].default_value = color
    bsdf.inputs["Emission Strength"].default_value = strength
    return m


def cube(name, location, scale, material):
    bpy.ops.mesh.primitive_cube_add(size=1, location=location)
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    obj.data.materials.append(material)
    return obj


def sphere(name, location, radius, material, segments=12):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments, ring_count=8, radius=radius, location=location)
    obj = bpy.context.object
    obj.name = name
    obj.data.materials.append(material)
    return obj


def light(name, location, color, energy, size):
    data = bpy.data.lights.new(name, "AREA")
    data.energy = energy
    data.color = color[:3]
    data.shape = "DISK"
    data.size = size
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    obj.location = location
    obj.rotation_euler = (Vector((0, 0, 1)) - obj.location).to_track_quat("-Z", "Y").to_euler()
    return obj


def import_model(name, relative_path, height, position, rotation=0.0, silhouette=False):
    path = os.path.join(ROOT, relative_path)
    if not os.path.exists(path):
        print("MODEL MISSING", path)
        return None
    before = set(bpy.data.objects)
    bpy.ops.import_scene.gltf(filepath=path)
    new = [o for o in bpy.data.objects if o not in before]
    root = bpy.data.objects.new(name, None)
    bpy.context.collection.objects.link(root)
    for obj in new:
        if obj.parent not in new:
            matrix = obj.matrix_world.copy()
            obj.parent = root
            obj.matrix_world = matrix
    meshes = [o for o in new if o.type == "MESH"]
    if not meshes:
        return root
    if silhouette:
        shadow = mat(name + " silhouette", (0.025, 0.06, 0.12, 1), 0.2)
        for obj in meshes:
            obj.data.materials.clear()
            obj.data.materials.append(shadow)
    # Agent GLBs include far-away helper meshes; size and center the actual body.
    body = max(meshes, key=lambda item: len(item.data.vertices))
    bpy.context.view_layer.update()
    points = [body.matrix_world @ Vector(c) for c in body.bound_box]
    minimum = min(p.z for p in points)
    maximum = max(p.z for p in points)
    root.scale = (height / max(0.001, maximum - minimum),) * 3
    root.rotation_euler.z = math.radians(rotation)
    bpy.context.view_layer.update()
    points = [body.matrix_world @ Vector(c) for c in body.bound_box]
    minimum = min(p.z for p in points)
    center_x = (min(p.x for p in points) + max(p.x for p in points)) / 2
    center_y = (min(p.y for p in points) + max(p.y for p in points)) / 2
    offset = Vector((-center_x, -center_y, -minimum))
    for obj in new:
        if obj.parent == root:
            matrix = obj.matrix_world.copy()
            matrix.translation += offset
            obj.matrix_world = matrix
    root.location = position
    return root


floor = mat("midnight floor", (0.008, 0.018, 0.05, 1), 0.05, 0.35)
cyan = mat("electric cyan", CYAN, 5.0)
purple = mat("rift violet", VIOLET, 5.0)
white = mat("impact white", (0.9, 0.98, 1, 1), 7.0)
dark = mat("void", NAVY, 0.0)
cube("arena", (0, 0.3, -0.18), (30, 13, 0.3), floor)

# The camera sees the rift in the X/Z plane, behind the actors.
rift = bpy.data.objects.new("RIFT", None)
bpy.context.collection.objects.link(rift)
rift.location = (0, 2.1, 2.3)
for i in range(36):
    a = i * 2 * math.pi / 36
    b = (i + 1) * 2 * math.pi / 36
    start = Vector((3.0 * math.cos(a), 0, 1.7 * math.sin(a)))
    end = Vector((3.0 * math.cos(b), 0, 1.7 * math.sin(b)))
    segment = cube("rift shard %02d" % i, (0, 0, 0),
                   ((end - start).length * 1.2, 0.09, 0.11),
                   purple if i % 5 == 0 else cyan)
    segment.parent = rift
    segment.location = (start + end) / 2
    segment.rotation_euler = (end - start).to_track_quat("X", "Y").to_euler()
seam = cube("rift seam", (0, 0, 0), (0.07, 0.07, 3.2), purple)
seam.parent = rift
key_value(rift, "scale", 1, (0.001, 0.001, 0.001))
key_value(rift, "scale", 55, (1, 1, 1))
key_value(rift, "scale", END, (1.05, 1.05, 1.05))
key_value(rift, "rotation_euler", 1, (0, 0, 0))
key_value(rift, "rotation_euler", END, (0, 0.1, 0))

random.seed(1224)
for i in range(85):
    x = random.uniform(-8.6, 8.6)
    y = random.uniform(-0.5, 5.0)
    z = random.uniform(0.4, 5.5)
    dot = sphere("rift spark %02d" % i, (x, y, z), random.uniform(0.012, 0.035),
                 purple if i % 4 == 0 else cyan, 8)
    dot.location.y += random.uniform(-0.15, 0.15)
    dot.keyframe_insert(data_path="location", frame=1)
    dot.location.y += random.uniform(0.2, 0.75)
    dot.keyframe_insert(data_path="location", frame=END)

creature = import_model("CREATURE / BULBASAUR", "Assets/Models/Pokemon/Bulbasaur.glb",
                        1.55, (1.4, 0.4, 0), 0)
tamer = import_model("TAMER / GEKKO", "Assets/Models/Valorant/Gekko.glb",
                     2.35, (-5.4, -0.5, 0), 0)
warden = import_model("WARDEN / REYNA", "Assets/Models/Valorant/Reyna.glb",
                      2.35, (5.3, 0.1, 0), 0)
orb = import_model("CAPTURE ORB", "Assets/Models/Pokemon/PokeBall.glb",
                   0.4, (-1.2, -1.1, 0.9), 0)
glimpses = [
    import_model("GLIMPSE / IVYSAUR", "Assets/Models/Pokemon/Ivysaur.glb", 1.5,
                 (-2.1, 0.8, 0), 0, True),
    import_model("GLIMPSE / CHARIZARD", "Assets/Models/Pokemon/Charizard.glb", 2.8,
                 (0.3, 0.9, 0), 0, True),
    import_model("GLIMPSE / VENUSAUR", "Assets/Models/Pokemon/Venusaur.glb", 1.8,
                 (2.4, 0.8, 0), 0, True),
]


def animate_appearance(obj, first, peak, last, scale=(1, 1, 1)):
    if obj is None:
        return
    base = tuple(obj.scale)
    visible = tuple(base[i] * scale[i] for i in range(3))
    key_value(obj, "scale", max(1, first - 1), tuple(v * 0.001 for v in base))
    key_value(obj, "scale", first + 1, visible)
    key_value(obj, "scale", peak, visible)
    key_value(obj, "scale", last, tuple(v * 0.001 for v in base))


for index, glimpse in enumerate(glimpses):
    animate_appearance(glimpse, 43 + index * 14, 50 + index * 14, 57 + index * 14)
animate_appearance(creature, 86, 200, END + 1)
animate_appearance(tamer, 99, 245, END + 1)
animate_appearance(warden, 187, 250, END + 1)
animate_appearance(orb, 146, 215, 240)

if tamer:
    key_value(tamer, "location", 101, (-5.4, -0.5, tamer.location.z))
    key_value(tamer, "location", 146, (-2.7, -0.5, tamer.location.z))
if warden:
    key_value(warden, "location", 187, (5.3, 0.1, warden.location.z))
    key_value(warden, "location", 232, (2.6, 0.1, warden.location.z))
if orb:
    key_value(orb, "location", 146, (-1.2, -1.1, 1.0))
    key_value(orb, "location", 166, (0.1, -1.1, 3.7))
    key_value(orb, "location", 187, (1.25, -0.9, 1.3))
    key_value(orb, "rotation_euler", 146, (0, 0, 0))
    key_value(orb, "rotation_euler", 187, (0, 0, 7 * math.pi))

# Purple corruption blooms across the capture circle.
for index in range(4):
    ring = bpy.data.objects.new("CORRUPTION WAVE %d" % index, None)
    bpy.context.collection.objects.link(ring)
    ring.location = (1.0, -0.3, 0.4)
    for i in range(24):
        a = i * 2 * math.pi / 24
        b = (i + 1) * 2 * math.pi / 24
        start = Vector((math.cos(a), math.sin(a), 0))
        end = Vector((math.cos(b), math.sin(b), 0))
        shard = cube("corruption edge", (0, 0, 0), ((end - start).length, 0.055, 0.08), purple)
        shard.parent = ring
        shard.location = (start + end) / 2
        shard.rotation_euler.z = a + math.pi / 2
    start_frame = 189 + index * 6
    key_value(ring, "scale", start_frame, (0.001, 0.001, 0.001))
    key_value(ring, "scale", start_frame + 33, (4.4, 4.4, 4.4))
    key_value(ring, "scale", start_frame + 40, (0.001, 0.001, 0.001))

impact = sphere("CLASH OF LIGHT", (0.2, -1.2, 1.7), 1, white, 20)
animate_appearance(impact, 231, 239, 260, (0.001, 0.001, 0.001))
key_value(impact, "scale", 231, (0.001, 0.001, 0.001))
key_value(impact, "scale", 239, (3.6, 3.6, 3.6))
key_value(impact, "scale", 248, (0.001, 0.001, 0.001))

rift_light = light("rift glow", (0, 0.6, 3.6), CYAN, 1100, 5)
creature_light = light("creature reveal", (1.2, -3, 3.3), CYAN, 700, 4)
warden_light = light("corruption purple", (3, -2, 3), VIOLET, 0, 4)
fill_light = light("soft front", (-5, -7, 6), (0.55, 0.7, 1, 1), 750, 9)
for frame, energy in [(1, 0), (36, 0), (54, 1150), (END, 1150)]:
    rift_light.data.energy = energy
    rift_light.data.keyframe_insert(data_path="energy", frame=frame)
for frame, energy in [(1, 0), (86, 0), (112, 1100), (END, 1100)]:
    creature_light.data.energy = energy
    creature_light.data.keyframe_insert(data_path="energy", frame=frame)
for frame, energy in [(1, 0), (186, 0), (228, 1800), (END, 1400)]:
    warden_light.data.energy = energy
    warden_light.data.keyframe_insert(data_path="energy", frame=frame)

camera_data = bpy.data.cameras.new("Cinematic camera")
camera = bpy.data.objects.new("Cinematic camera", camera_data)
bpy.context.collection.objects.link(camera)
scene.camera = camera
camera.location = (0, -16.5, 3.6)
camera.rotation_euler = (Vector((0, 0.6, 1.5)) - camera.location).to_track_quat("-Z", "Y").to_euler()
camera_data.type = "PERSP"
camera_data.lens = 38
key_value(camera, "location", 1, (0, -17.8, 3.6))
key_value(camera, "location", 205, (0, -14.5, 3.5))
key_value(camera, "location", 241, (0.14, -13.8, 3.65))
key_value(camera, "location", 249, (-0.08, -15.8, 3.6))
key_value(camera, "location", END, (0, -17.4, 3.6))


def text_object(name, body, size, location, material, spacing=1.0):
    curve = bpy.data.curves.new(name, type="FONT")
    curve.body = body
    curve.size = size
    curve.align_x = "CENTER"
    curve.align_y = "CENTER"
    curve.space_character = spacing
    curve.extrude = 0.006
    curve.bevel_depth = 0.002
    obj = bpy.data.objects.new(name, curve)
    bpy.context.collection.objects.link(obj)
    obj.location = location
    obj.rotation_euler.x = math.pi / 2
    curve.materials.append(material)
    return obj


title = text_object("VALMON TITLE", "VALMON", 1.45, (0, -4.8, 2.5), white, 1.2)
tagline = text_object("TAGLINE", "CATCH THE CREATURE.  RESIST THE CORRUPTION.  MASTER THE RIFT.",
                      0.21, (0, -4.8, 1.25), cyan)
subline = text_object("PRESS START", "PRESS START", 0.25, (0, -4.8, 0.35), white)
for obj in (title, tagline, subline):
    key_value(obj, "scale", 1, (0.001, 0.001, 0.001))
    key_value(obj, "scale", 254 if obj is title else 270, (0.001, 0.001, 0.001))
    key_value(obj, "scale", 275 if obj is title else 289, (1, 1, 1))
    key_value(obj, "scale", END, (1, 1, 1))

# Subtle caption cards keep the story readable without covering the action.
caption_specs = [
    ("A RIFT AWAKENS", 18, 38),
    ("THE TAMER APPROACHES", 104, 137),
    ("THE CAPTURE BEGINS", 151, 183),
    ("THE WARDEN INTERVENES", 197, 227),
]
for body, start, end in caption_specs:
    caption = text_object(body, body, 0.31, (0, -5.0, 0.15), cyan)
    key_value(caption, "scale", 1, (0.001, 0.001, 0.001))
    key_value(caption, "scale", start - 1, (0.001, 0.001, 0.001))
    key_value(caption, "scale", start + 4, (1, 1, 1))
    key_value(caption, "scale", end - 4, (1, 1, 1))
    key_value(caption, "scale", end, (0.001, 0.001, 0.001))

scene.frame_set(1)
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT, "VALMON_Opening.blend"))
if os.environ.get("VALMON_PREVIEW_FRAME"):
    frame = int(os.environ["VALMON_PREVIEW_FRAME"])
    scene.frame_set(frame)
    scene.render.image_settings.media_type = "IMAGE"
    scene.render.image_settings.file_format = "PNG"
    scene.render.filepath = os.path.join(OUT, "preview_%03d.png" % frame)
    bpy.ops.render.render(write_still=True)
else:
    bpy.ops.render.render(animation=True)
