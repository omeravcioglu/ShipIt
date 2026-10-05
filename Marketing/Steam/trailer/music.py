"""Synthesizes the trailer's music: a cozy 100 BPM chiptune loop (I-vi-IV-V in C) with pads, bass,
a pulse-wave arpeggio and soft drums. Original and royalty-free. Usage: python music.py out.wav seconds"""
import sys
import wave
import numpy as np

SR = 44100
BPM = 100
BEAT = 60.0 / BPM
BAR = 4 * BEAT

out_path = sys.argv[1] if len(sys.argv) > 1 else "music.wav"
length = float(sys.argv[2]) if len(sys.argv) > 2 else 90.0
n = int(length * SR) + SR
left = np.zeros(n)
right = np.zeros(n)


def hz(midi):
    return 440.0 * 2 ** ((midi - 69) / 12)


def env(count, attack=0.005, release=0.1, decay=None):
    t = np.arange(count) / SR
    e = np.minimum(1.0, t / max(attack, 1e-4))
    if decay:
        e *= np.exp(-t / decay)
    tail = int(release * SR)
    if tail and count > tail:
        e[-tail:] *= np.linspace(1, 0, tail)
    return e


def add(signal, start, pan=0.0, gain=1.0):
    i = int(start * SR)
    if i >= n:
        return
    signal = signal[: n - i] * gain
    left[i : i + len(signal)] += signal * (1 - pan) * 0.5 * 2 ** 0.5
    right[i : i + len(signal)] += signal * (1 + pan) * 0.5 * 2 ** 0.5


def lowpass(x, cutoff):
    a = np.exp(-2 * np.pi * cutoff / SR)
    y = np.empty_like(x)
    acc = 0.0
    for k in range(len(x)):  # short signals only
        acc = (1 - a) * x[k] + a * acc
        y[k] = acc
    return y


def tone(freq, dur, kind="tri", duty=0.25, vibrato=0.0):
    t = np.arange(int(dur * SR)) / SR
    phase = freq * t + (vibrato * np.sin(2 * np.pi * 5.5 * t) / (2 * np.pi * 5.5) if vibrato else 0)
    frac = phase % 1.0
    if kind == "tri":
        return 4 * np.abs(frac - 0.5) - 1
    if kind == "pulse":
        return np.where(frac < duty, 1.0, -1.0)
    if kind == "saw":
        return 2 * frac - 1
    return np.sin(2 * np.pi * phase)


# C, Am, F, G (with a sus G at the turnaround)
chords = [[60, 64, 67, 72], [57, 60, 64, 69], [53, 57, 60, 65], [55, 59, 62, 67]]
roots = [36, 33, 29, 31]
rng = np.random.default_rng(7)
bars = int(length / BAR) + 1

for bar in range(bars):
    t0 = bar * BAR
    chord = chords[bar % 4]
    root = roots[bar % 4]
    section = 0 if bar < 2 else 1 if bar < 6 else 2

    # pad: soft detuned saws, low-passed
    pad = sum(tone(hz(m) * d, BAR, "saw") for m in chord[:3] for d in (0.997, 1.003))
    pad = lowpass(pad, 1400) * env(len(pad), attack=0.35, release=0.6)
    add(pad, t0, gain=0.035 if section else 0.05)

    if section >= 1:
        # bass: root on the beat, octave pickup
        for b, (step, up) in enumerate([(0, 0), (1.5, 0), (2, 0), (3, 12)]):
            note = tone(hz(root + 12 + up), BEAT * 0.45, "tri")
            add(note * env(len(note), decay=0.35, release=0.03), t0 + step * BEAT, gain=0.22)
        # arpeggio: pulse wave, eighth notes up and down the chord
        pattern = [0, 1, 2, 3, 2, 1, 2, 3]
        for i, p in enumerate(pattern):
            note = tone(hz(chord[p] + 12), BEAT * 0.48, "pulse", duty=0.25, vibrato=1.5)
            sig = note * env(len(note), decay=0.18, release=0.02)
            add(sig, t0 + i * BEAT / 2, pan=-0.25, gain=0.045)
            add(sig, t0 + i * BEAT / 2 + 0.18, pan=0.35, gain=0.018)  # echo

    if section >= 2:
        for beat in range(4):
            bt = t0 + beat * BEAT
            if beat in (0, 2):  # kick
                t = np.arange(int(0.3 * SR)) / SR
                kick = np.sin(2 * np.pi * (50 * t + 70 * (1 - np.exp(-t * 30)) / 30)) * np.exp(-t * 9)
                add(kick, bt, gain=0.5)
            if beat in (1, 3):  # clap
                noise = rng.standard_normal(int(0.18 * SR))
                clap = (noise - lowpass(noise, 900)) * env(len(noise), decay=0.06, release=0.01)
                add(clap, bt, gain=0.12)
            for half in (0, 0.5):  # hats
                noise = rng.standard_normal(int(0.05 * SR))
                hat = (noise - lowpass(noise, 6000)) * env(len(noise), decay=0.015, release=0.005)
                add(hat, bt + half * BEAT, pan=0.3, gain=0.06)
        if bar % 4 == 3:  # a little lead melody hook every turnaround
            for i, m in enumerate([79, 76, 74, 72]):
                note = tone(hz(m), BEAT * 0.5, "pulse", duty=0.5, vibrato=3)
                add(note * env(len(note), decay=0.3, release=0.03), t0 + 2 * BEAT + i * BEAT / 2, gain=0.035)

mix = np.stack([left, right], axis=1)[: int(length * SR)]
mix /= np.max(np.abs(mix)) + 1e-9
mix *= 0.89
fade = int(2.5 * SR)
mix[-fade:] *= np.linspace(1, 0, fade)[:, None]
pcm = (mix * 32767).astype(np.int16)
with wave.open(out_path, "wb") as w:
    w.setnchannels(2)
    w.setsampwidth(2)
    w.setframerate(SR)
    w.writeframes(pcm.tobytes())
print(f"wrote {out_path}: {length:.1f}s")
