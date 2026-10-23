import os
import bpy
root = os.environ["VALMON_PROJECT"]
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=os.path.join(root, "Assets/Characters/reyna__valorant.glb"))
bpy.context.view_layer.update()
graph = bpy.context.evaluated_depsgraph_get()
for obj in bpy.context.scene.objects:
    if obj.type != "MESH":
        continue
    for label, mesh in [("base", obj.data), ("posed", obj.evaluated_get(graph).to_mesh())]:
        coords = [obj.matrix_world @ v.co for v in mesh.vertices]
        print("VALMON_BOUNDS", obj.name, label, len(coords),
              [round(min(v[i] for v in coords), 3) for i in range(3)],
              [round(max(v[i] for v in coords), 3) for i in range(3)])
