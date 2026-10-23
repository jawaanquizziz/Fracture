import bpy
import os
bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene
scene.render.resolution_x = 960
scene.render.resolution_y = 540
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = 'PNG'
scene.render.use_sequencer = True
editor = scene.sequence_editor_create()
clip = editor.strips.new_movie('Reference', r'C:\Users\araji\Downloads\question bank\Screen Recording 2026-10-23 220002.mov', channel=1, frame_start=1)
scene.render.fps = round(clip.fps)
os.makedirs('Tools/Reference', exist_ok=True)
for second in [6, 22, 40]:
    scene.frame_set(1 + second * scene.render.fps)
    scene.render.filepath = os.path.abspath(f'Tools/Reference/frame-{second}.png')
    bpy.ops.render.render(write_still=True)
print('VALOMON_REFERENCE_READY', clip.fps, clip.frame_final_duration)
