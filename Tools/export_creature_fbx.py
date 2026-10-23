"""Export one existing Blender or glTF creature model to Unity-friendly FBX.

Set VALMON_SOURCE and VALMON_DEST, then run this script with Blender in background.
"""
import os
import bpy

source = os.environ["VALMON_SOURCE"]
destination = os.environ["VALMON_DEST"]

if source.lower().endswith(".blend"):
    bpy.ops.wm.open_mainfile(filepath=source)
else:
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    bpy.ops.import_scene.gltf(filepath=source)

os.makedirs(os.path.dirname(destination), exist_ok=True)
bpy.ops.export_scene.fbx(
    filepath=destination,
    use_selection=False,
    object_types={"MESH", "ARMATURE"},
    path_mode="COPY",
    embed_textures=True,
    bake_anim=False,
)
print("VALMON_EXPORTED", destination)
