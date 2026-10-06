"""Cuts the Steam trailer (1080p, 60 fps) from the gameplay clips the TrailerCapture play-mode test records
(Logs/Trailer/clips: <name>.mp4 at 60 fps plus <name>.cues.tsv sound cues), the title cards in cards/, and the
synthesized soundtrack (soundtrack.py). Every segment lasts a whole number of beats at 96 BPM, so cuts land on
the beat and the music's sections follow the edit. Output: ship_it_trailer_1080p60.mp4

Usage: python build_trailer.py
"""
import json
import os
import re
import subprocess
import wave

import numpy as np

import soundtrack as S

HERE = os.path.dirname(os.path.abspath(__file__))
CLIPS = os.path.normpath(os.path.join(HERE, "..", "..", "..", "Logs", "Trailer", "clips"))
WORK = os.path.join(HERE, "work60")
FFDIR = os.environ.get("FFDIR", r"C:\Users\Omer\AppData\Local\Microsoft\WinGet\Packages\Gyan.FFmpeg_Microsoft.Winget.Source_8wekyb3d8bbwe\ffmpeg-8.0.1-full_build\bin")
FF = os.path.join(FFDIR, "ffmpeg.exe")
PROBE = os.path.join(FFDIR, "ffprobe.exe")
FPS = 60
OUT = os.path.join(HERE, "ship_it_trailer_1080p60.mp4")

# (section, kind, name, beats, options). A clip's options: "trim" seconds in, or "anchor": (cue, fraction) to put
# that cue at a fraction of the segment.
EDIT = [
    ("intro", "card", "open", 4, {}),
    ("intro", "card", "c1", 3, {}),
    ("intro", "clip", "01_name", 9, {"trim": 0.6}),
    ("light", "card", "c2", 3, {}),
    ("light", "clip", "02_giggrid", 7, {"trim": 0.1}),
    ("light", "clip", "03_bug_squash", 10, {"trim": 0.9}),
    ("light", "card", "c3", 3, {}),
    ("light", "clip", "04_new_project", 7, {"trim": 0.2}),
    ("light", "clip", "05_wire_code", 10, {"anchor": ("solved", 0.62)}),
    ("groove", "card", "c4", 4, {}),
    ("groove", "clip", "06_wire_1_art", 4, {"trim": 0.25}),
    ("groove", "clip", "06_wire_2_audio", 4, {"trim": 0.25}),
    ("groove", "clip", "06_wire_3_design", 4, {"trim": 0.25}),
    ("groove", "clip", "06_wire_4_writing", 4, {"trim": 0.25}),
    ("groove", "card", "c5", 3, {}),
    ("groove", "clip", "07_pixel_match", 6, {"trim": 0.3}),
    ("groove", "clip", "09_feedback_sort", 6, {"trim": 0.5}),
    ("groove", "clip", "10_synth_tuner", 5, {"trim": 0.5}),
    ("breakdown", "card", "c6", 3, {}),
    ("breakdown", "clip", "11_coffee_cat", 9, {"trim": 0.6}),
    ("breakdown", "clip", "12_feature_complete", 10, {"anchor": ("confetti", 0.42)}),
    ("breakdown", "clip", "13_night", 6, {"anchor": ("morning", 0.55)}),
    ("riser", "card", "c7", 4, {}),
    ("finale", "clip", "14_launch_day", 12, {"anchor": ("launch", 0.0)}),
    ("finale", "clip", "16_reviews", 8, {"trim": 0.3}),
    ("outro", "card", "end", 8, {}),
]


def run(args):
    subprocess.run(args, check=True, stdout=subprocess.PIPE, stderr=subprocess.PIPE)


def probe_duration(path):
    out = subprocess.run([PROBE, "-v", "error", "-show_entries", "format=duration", "-of", "csv=p=0", path], capture_output=True, text=True)
    return float(out.stdout.strip())


def read_cues(name):
    path = os.path.join(CLIPS, name + ".cues.tsv")
    cues = []
    if os.path.exists(path):
        for line in open(path, encoding="utf8"):
            if line.strip():
                t, cue = line.rstrip("\n").split("\t")
                cues.append((float(t), cue))
    return cues


ENC = ["-c:v", "libx264", "-preset", "slow", "-crf", "14", "-pix_fmt", "yuv420p", "-r", str(FPS), "-an"]


def make_card(name, seconds, out, first=False, last=False):
    frames = int(round(seconds * FPS))
    fades = []
    if first:
        fades.append("fade=t=in:st=0:d=0.4")
    if last:
        fades.append(f"fade=t=out:st={seconds - 0.9:.3f}:d=0.9")
    vf = (f"scale=2112:1188,zoompan=z='1+0.03*on/{frames}':x='iw/2-(iw/zoom/2)':y='ih/2-(ih/zoom/2)':d=1:s=1920x1080:fps={FPS}"
          + ("," + ",".join(fades) if fades else ""))
    run([FF, "-y", "-v", "error", "-loop", "1", "-framerate", str(FPS), "-i", os.path.join(HERE, "cards", name + ".png"),
         "-t", f"{seconds:.4f}", "-vf", vf, *ENC, out])


def make_clip(name, trim, seconds, out):
    src = os.path.join(CLIPS, name + ".mp4")
    have = probe_duration(src) - trim
    pad = f",tpad=stop_mode=clone:stop_duration={seconds - have + 0.1:.3f}" if have < seconds else ""
    run([FF, "-y", "-v", "error", "-ss", f"{trim:.4f}", "-i", src, "-t", f"{seconds:.4f}", "-vf", f"fps={FPS}{pad}", *ENC, out])


def main():
    os.makedirs(WORK, exist_ok=True)
    beat = S.BEAT
    timeline, events, sections = [], [], []
    t_beats = 0
    for i, (section, kind, name, beats, opt) in enumerate(EDIT):
        seconds = beats * beat
        start = t_beats * beat
        out = os.path.join(WORK, f"{i:02d}_{name}.mp4")
        if kind == "card":
            make_card(name, seconds, out, first=(i == 0), last=(i == len(EDIT) - 1))
            if 0 < i < len(EDIT) - 1:
                events.append((start, "title"))
        else:
            cues = read_cues(name)
            length = probe_duration(os.path.join(CLIPS, name + ".mp4"))
            if "anchor" in opt:
                cue, frac = opt["anchor"]
                at = next((t for t, c in cues if c == cue), None)
                trim = 0.0 if at is None else at - frac * seconds
            else:
                trim = opt.get("trim", 0.0)
            trim = max(0.0, min(trim, max(0.0, length - seconds)))
            make_clip(name, trim, seconds, out)
            for t, cue in cues:
                if trim <= t < trim + seconds - 0.05:
                    events.append((start + t - trim, cue))
        timeline.append(out)
        if not sections or sections[-1][0] != section:
            sections.append([section, t_beats, 0])
        sections[-1][2] += beats
        t_beats += beats
        print(f"{start:6.2f}s  {kind:4s} {name:22s} {seconds:5.2f}s")

    total = t_beats * beat
    with open(os.path.join(WORK, "list.txt"), "w", encoding="utf8") as f:
        for path in timeline:
            f.write(f"file '{os.path.basename(path)}'\n")
    video = os.path.join(WORK, "video.mp4")
    run([FF, "-y", "-v", "error", "-f", "concat", "-safe", "0", "-i", os.path.join(WORK, "list.txt"), "-c", "copy", video])

    # ---- audio: music arranged on the edit's sections, plus sound effects on the logged cues
    music = S.compose([tuple(s) for s in sections], total + 0.5)
    fx = S.render_sfx(events, total + 0.5)
    rms = lambda x: 20 * np.log10(np.sqrt(np.mean(x ** 2)) + 1e-12)  # noqa: E731
    active = np.abs(fx).max(0) > 1e-4
    print(f"music {rms(music):.1f} dB, effects {rms(0.85 * fx[:, active]):.1f} dB while they play, effects peak {20 * np.log10(np.abs(0.85 * fx).max()):.1f} dB vs music peak {20 * np.log10(np.abs(music).max()):.1f} dB")
    mix = music + 0.85 * fx
    n = int(total * S.SR)
    mix = mix[:, :n]
    fade = int(1.2 * S.SR)
    mix[:, -fade:] *= np.linspace(1.0, 0.0, fade) ** 2
    mix /= np.max(np.abs(mix)) / 0.9
    raw = os.path.join(WORK, "mix.wav")
    with wave.open(raw, "wb") as w:
        w.setnchannels(2)
        w.setsampwidth(2)
        w.setframerate(S.SR)
        w.writeframes((mix.T * 32767).astype(np.int16).tobytes())

    # two-pass loudness normalisation to -14 LUFS, -1 dBTP (streaming level)
    first = subprocess.run([FF, "-hide_banner", "-i", raw, "-af", "loudnorm=I=-14:TP=-1.0:LRA=11:print_format=json", "-f", "null", "-"],
                           capture_output=True, text=True).stderr
    m = json.loads(re.search(r"\{[^{}]*\"input_i\"[^{}]*\}", first, re.S).group(0))
    norm = (f"loudnorm=I=-14:TP=-1.0:LRA=11:measured_I={m['input_i']}:measured_TP={m['input_tp']}:measured_LRA={m['input_lra']}"
            f":measured_thresh={m['input_thresh']}:offset={m['target_offset']}:linear=true")
    run([FF, "-y", "-v", "error", "-i", video, "-i", raw, "-map", "0:v", "-map", "1:a", "-c:v", "copy",
         "-af", norm + ",aresample=48000", "-c:a", "aac", "-b:a", "320k", "-movflags", "+faststart", "-shortest", OUT])
    print(f"\n{OUT}: {probe_duration(OUT):.2f}s, {os.path.getsize(OUT) / 1e6:.1f} MB, {len(events)} sound cues")
    print("sections:", [(s[0], round(s[1] * beat, 2)) for s in sections])


if __name__ == "__main__":
    main()
