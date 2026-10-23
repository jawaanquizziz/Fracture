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
clip = editor.strips.new_movie('Reference', r'C:\Users\araji\AI\GAME DEV HACK\Video\VALMON_Championship_Opening.mp4', channel=1, frame_start=1)
scene.render.fps = round(clip.fps)
os.makedirs('Tools/Reports/OpeningVideo', exist_ok=True)
for second in [4, 10]:
    scene.frame_set(1 + second * scene.render.fps)
    scene.render.filepath = os.path.abspath(f'Tools/Reports/OpeningVideo/frame-{second}.png')
    bpy.ops.render.render(write_still=True)
print('VALMON_VIDEO_VERIFIED', clip.fps, clip.frame_final_duration)

