"""Encode the game's own captured stadium opening into a portable MP4.

Run after -valomonRecordIntro Tools/IntroFrames has produced 288 PNGs.
The original VALMON Blender opening remains available under Video/.
"""
import os
import bpy

root = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
frames = os.path.join(root, 'Tools', 'IntroFrames')
files = sorted(name for name in os.listdir(frames) if name.endswith('.png'))
if len(files) != 288:
    raise RuntimeError(f'Expected 288 opening frames; found {len(files)}')
bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene
scene.render.resolution_x = 1280
scene.render.resolution_y = 720
scene.render.resolution_percentage = 100
scene.render.fps = 24
scene.frame_start = 1
scene.frame_end = len(files)
scene.render.use_sequencer = True
editor = scene.sequence_editor_create()
strip = editor.strips.new_image('VALMON Championship Opening', os.path.join(frames, files[0]), channel=1, frame_start=1)
for filename in files[1:]:
    strip.elements.append(filename)
strip.frame_final_duration = len(files)

strip.crop.min_y = 18
editor.strips.new_sound('Original Rift Score', os.path.join(root, 'Video', 'valmon_soundtrack.wav'), channel=2, frame_start=1)
scene.render.image_settings.media_type = 'VIDEO'
scene.render.image_settings.file_format = 'FFMPEG'
scene.render.ffmpeg.format = 'MPEG4'
scene.render.ffmpeg.codec = 'H264'
scene.render.ffmpeg.audio_codec = 'AAC'
scene.render.ffmpeg.constant_rate_factor = 'HIGH'
scene.render.ffmpeg.ffmpeg_preset = 'GOOD'
scene.render.filepath = os.path.join(root, 'Video', 'VALMON_Championship_Opening.mp4')
bpy.ops.render.render(animation=True)
print('VALMON_STADIUM_VIDEO_READY', scene.render.filepath)


