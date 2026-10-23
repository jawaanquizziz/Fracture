"""Rebind the supplied Reyna mesh to its evaluated rest pose, preserving skin weights."""
import os
import bpy

root = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=os.path.join(root, 'Assets/Characters/reyna__valorant.glb'))
bpy.context.view_layer.update()
graph = bpy.context.evaluated_depsgraph_get()
meshes = [o for o in bpy.context.scene.objects if o.type == 'MESH' and o.name != 'Icosphere']
rigs = [o for o in bpy.context.scene.objects if o.type == 'ARMATURE']
assert len(rigs) == 1, 'Expected one source armature'
rig = rigs[0]
for obj in meshes:
    evaluated = obj.evaluated_get(graph)
    mesh = bpy.data.meshes.new_from_object(evaluated, depsgraph=graph)
    assert len(mesh.vertices) == len(obj.data.vertices), 'Rebinding must preserve vertex order'
    # Evaluated geometry retains deform groups; copy explicitly for importer consistency.
    weights = [[(g.group, g.weight) for g in v.groups] for v in obj.data.vertices]
    obj.data = mesh
    for i, groups in enumerate(weights):
        for group, weight in groups:
            obj.vertex_groups[group].add([i], weight, 'REPLACE')
    print('REBIND', obj.name, len(mesh.vertices), len(obj.vertex_groups))
bpy.ops.object.select_all(action='DESELECT')
rig.select_set(True)
bpy.context.view_layer.objects.active = rig
bpy.ops.object.mode_set(mode='POSE')
bpy.ops.pose.armature_apply(selected=False)
bpy.ops.object.mode_set(mode='OBJECT')
bpy.context.view_layer.update()
for obj in meshes:
    obj.select_set(True)
destination = os.path.join(root, 'Assets/Characters/ImportedCharacters/ReynaWalking.fbx')
bpy.ops.export_scene.fbx(filepath=destination, use_selection=True,
                         object_types={'MESH', 'ARMATURE'}, path_mode='COPY', embed_textures=True,
                         add_leaf_bones=False, bake_anim=False)
print('VALMON_REYNA_RIGGED', destination)
