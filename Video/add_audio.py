"""Combine the rendered opening and original soundtrack into the final MP4."""

import bpy
import os


folder = os.path.dirname(os.path.abspath(__file__))
video = os.path.join(folder, "valmon_opening_silent.mp4")
audio = os.path.join(folder, "valmon_soundtrack.wav")
output = os.path.join(folder, "VALMON_Opening.mp4")

scene = bpy.context.scene
scene.render.engine = "BLENDER_EEVEE"
scene.render.resolution_x = 960
scene.render.resolution_y = 540
scene.render.resolution_percentage = 100
scene.render.fps = 12
scene.frame_start = 1
scene.frame_end = 156
scene.render.use_sequencer = True
scene.render.image_settings.media_type = "VIDEO"
scene.render.image_settings.file_format = "FFMPEG"
scene.render.ffmpeg.format = "MPEG4"
scene.render.ffmpeg.codec = "H264"
scene.render.ffmpeg.audio_codec = "AAC"
scene.render.ffmpeg.audio_bitrate = 192
scene.render.ffmpeg.constant_rate_factor = "MEDIUM"
scene.render.ffmpeg.ffmpeg_preset = "GOOD"
scene.render.filepath = output

editor = scene.sequence_editor_create()
editor.strips.new_movie("Opening", video, channel=1, frame_start=1)
editor.strips.new_sound("Original score", audio, channel=2, frame_start=1)
bpy.ops.render.render(animation=True)
