"""Bake the supplied Reyna glTF pose into a static FBX for consistent Unity rendering.

Run in a separate Blender background process; the source GLB is preserved.
"""
import os
import bpy

root = os.environ["VALMON_PROJECT"]
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=os.path.join(root, "Assets/Characters/reyna__valorant.glb"))
bpy.context.scene.frame_set(0)
bpy.context.view_layer.update()
graph = bpy.context.evaluated_depsgraph_get()
baked = []
for source in list(bpy.context.scene.objects):
    # glTF import creates this non-character helper for the malformed skin.
    if source.type != "MESH" or source.name == "Icosphere":
        continue
    evaluated = source.evaluated_get(graph)
    mesh = bpy.data.meshes.new_from_object(evaluated, depsgraph=graph)
    obj = bpy.data.objects.new(source.name + "_Baked", mesh)
    bpy.context.collection.objects.link(obj)
    obj.matrix_world = source.matrix_world.copy()
    baked.append(obj)
bpy.ops.object.select_all(action="DESELECT")
for obj in baked:
    obj.select_set(True)
destination = os.path.join(root, "Assets/Characters/ImportedCharacters/Reyna.fbx")
os.makedirs(os.path.dirname(destination), exist_ok=True)
texture_folder = os.path.join(os.path.dirname(destination), "ReynaTextures")
os.makedirs(texture_folder, exist_ok=True)
for material in bpy.data.materials:
    if not material.use_nodes:
        continue
    for node in material.node_tree.nodes:
        if node.type == 'TEX_IMAGE' and node.image:
            image = node.image
            filename = bpy.path.clean_name(image.name) + '.png'
            image.filepath_raw = os.path.join(texture_folder, filename)
            image.file_format = 'PNG'
            image.save()
            print('VALOMON_MATERIAL_IMAGE', material.name, node.label, filename)
bpy.ops.export_scene.fbx(filepath=destination, use_selection=True,
                         object_types={"MESH"}, path_mode="COPY",
                         embed_textures=True, bake_anim=False)
print("VALMON_REYNA_BAKED", len(baked), destination)
