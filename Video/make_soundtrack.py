"""Create an original synth and effects bed for the VALMON opening."""

import math
import os
import wave

import numpy as np


RATE = 44100
DURATION = 13.0
count = int(RATE * DURATION)
t = np.arange(count, dtype=np.float64) / RATE
audio = np.zeros(count, dtype=np.float64)
rng = np.random.default_rng(24017)


def add_tone(start, end, frequency, gain, attack=0.15, release=0.25, harmonics=False):
    index = (t >= start) & (t < end)
    local = t[index] - start
    length = end - start
    env = np.minimum(1, local / max(attack, 0.001)) * np.minimum(1, (length - local) / max(release, 0.001))
    wave_shape = np.sin(2 * math.pi * frequency * local)
    if harmonics:
        wave_shape += 0.28 * np.sin(2 * math.pi * 2 * frequency * local)
        wave_shape += 0.12 * np.sin(2 * math.pi * 3 * frequency * local)
    audio[index] += gain * env * wave_shape


def add_noise(start, end, gain, sweep=False):
    index = (t >= start) & (t < end)
    local = t[index] - start
    length = end - start
    env = np.sin(math.pi * local / length) ** 2
    raw = rng.normal(0, 1, index.sum())
    smoothed = np.convolve(raw, np.ones(140) / 140, mode="same")
    if sweep:
        env *= np.linspace(0.15, 1, index.sum())
    audio[index] += gain * env * smoothed


# Low rift drone and rising pulses.
for note, gain in [(55, 0.075), (82.41, 0.045), (110, 0.026)]:
    add_tone(0.3, 10.8, note, gain, attack=2.0, release=1.0)
add_noise(0.4, 2.8, 0.19, sweep=True)
for start in (2.1, 2.7, 3.3):
    add_tone(start, start + 0.32, 440 + start * 75, 0.052, 0.01, 0.16, True)

# Creature discovery and the approaching Tamer.
for start, note in [(4.0, 220), (4.35, 261.63), (4.7, 329.63), (5.05, 440)]:
    add_tone(start, start + 0.8, note, 0.038, 0.04, 0.45, True)

# Capture throw, dark interruption, and impact.
add_noise(6.1, 7.75, 0.27, sweep=True)
add_tone(6.2, 7.7, 660, 0.035, 0.08, 0.18)
for start in (7.9, 8.45, 9.0, 9.55):
    add_tone(start, start + 0.5, 73.42, 0.16, 0.01, 0.35, True)
add_noise(8.0, 10.4, 0.36)
add_tone(9.9, 10.9, 49, 0.23, 0.004, 0.8, True)
add_noise(9.9, 10.55, 0.75)

# Resolved title chord.
for note, gain in [(110, 0.07), (164.81, 0.052), (220, 0.039), (329.63, 0.035), (440, 0.025)]:
    add_tone(10.8, 13.0, note, gain, 0.25, 0.45, True)

audio = np.tanh(audio * 1.4)
peak = max(0.001, float(np.max(np.abs(audio))))
audio = np.int16(np.clip(audio / peak * 0.78, -1, 1) * 32767)
output = os.path.join(os.path.dirname(__file__), "valmon_soundtrack.wav")
with wave.open(output, "wb") as file:
    file.setnchannels(1)
    file.setsampwidth(2)
    file.setframerate(RATE)
    file.writeframes(audio.tobytes())
print(output)
