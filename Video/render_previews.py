import bpy
import os

scene = bpy.context.scene
scene.render.image_settings.media_type = "IMAGE"
scene.render.image_settings.file_format = "PNG"
for frame in (145, 220, 300):
    scene.frame_set(frame)
    scene.render.filepath = os.path.join(os.path.dirname(bpy.data.filepath), f"preview_{frame:03d}.png")
    bpy.ops.render.render(write_still=True)
