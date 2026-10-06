"""The trailer's soundtrack and sound effects, synthesized (original, royalty-free).

Music: a cozy lo-fi groove in F major at 96 BPM: FM electric piano, kalimba melody, round sine bass, brushed
drums with swing, a warm pad and a little vinyl crackle, with convolution reverb. The arrangement follows the
trailer's sections (see build_trailer.py), so every cut lands on a beat.

Sound effects: soft, woody UI sounds (clicks, pops, chimes, a launch-day shimmer) placed on the cues the
capture test logs for each clip. Nothing harsh: every voice is band-limited and kept below the music.
"""
import numpy as np
from scipy import signal

SR = 48000
BPM = 96
BEAT = 60.0 / BPM
rng = np.random.default_rng(1234)


# ----------------------------------------------------------------------------------------------- helpers
def hz(midi):
    return 440.0 * 2.0 ** ((np.asarray(midi, dtype=float) - 69.0) / 12.0)


def tvec(seconds):
    return np.arange(int(seconds * SR)) / SR


def lowpass(x, cutoff, order=2):
    return signal.sosfilt(signal.butter(order, cutoff, "low", fs=SR, output="sos"), x)


def highpass(x, cutoff, order=2):
    return signal.sosfilt(signal.butter(order, cutoff, "high", fs=SR, output="sos"), x)


def bandpass(x, lo, hi, order=2):
    return signal.sosfilt(signal.butter(order, [lo, hi], "band", fs=SR, output="sos"), x)


def adsr(n, attack=0.005, decay_rate=0.0, release=0.05, hold=None):
    """Attack ramp, exponential decay, linear release over the last `release` seconds."""
    t = np.arange(n) / SR
    env = np.minimum(1.0, t / max(attack, 1e-4))
    if decay_rate:
        env *= np.exp(-t * decay_rate)
    r = int(release * SR)
    if r and n > r:
        env[-r:] *= np.linspace(1.0, 0.0, r) ** 1.5
    return env


def biquad(kind, f0, gain_db=0.0, q=0.707):
    """RBJ cookbook biquad as (b, a): 'peak' or 'highshelf'."""
    A = 10 ** (gain_db / 40)
    w = 2 * np.pi * f0 / SR
    alpha = np.sin(w) / (2 * q)
    cw = np.cos(w)
    if kind == "peak":
        b = [1 + alpha * A, -2 * cw, 1 - alpha * A]
        a = [1 + alpha / A, -2 * cw, 1 - alpha / A]
    else:  # high shelf
        sq = 2 * np.sqrt(A) * alpha
        b = [A * ((A + 1) + (A - 1) * cw + sq), -2 * A * ((A - 1) + (A + 1) * cw), A * ((A + 1) + (A - 1) * cw - sq)]
        a = [(A + 1) - (A - 1) * cw + sq, 2 * ((A - 1) - (A + 1) * cw), (A + 1) - (A - 1) * cw - sq]
    return np.array(b) / a[0], np.array(a) / a[0]


def master_eq(x):
    """Sub cut, a dip in the low mids that make lo-fi muddy, and a gentle presence lift."""
    x = highpass(x, 38, order=4)
    for kind, f0, g, q in (("peak", 300, -3.5, 0.9), ("peak", 140, -1.5, 1.0), ("peak", 2800, 2.0, 0.8), ("highshelf", 6000, 3.0, 0.7)):
        b, a = biquad(kind, f0, g, q)
        x = signal.lfilter(b, a, x)
    return x


class Track:
    """A stereo buffer that notes are mixed into."""

    def __init__(self, seconds):
        self.n = int(seconds * SR) + SR * 4
        self.l = np.zeros(self.n)
        self.r = np.zeros(self.n)

    def add(self, x, at, gain=1.0, pan=0.0):
        i = int(round(at * SR))
        if i < 0:
            x, i = x[-i:], 0
        if i >= self.n or len(x) == 0:
            return
        x = x[: self.n - i] * gain
        self.l[i : i + len(x)] += x * np.sqrt(0.5 * (1.0 - pan))
        self.r[i : i + len(x)] += x * np.sqrt(0.5 * (1.0 + pan))

    def stereo(self):
        return np.stack([self.l, self.r])


def reverb_ir(seconds=2.2, decay=3.2, predelay=0.018, bright=5500):
    """A smooth stereo room: decaying filtered noise with a short predelay."""
    n = int(seconds * SR)
    t = np.arange(n) / SR
    env = np.exp(-t * decay) * np.minimum(1.0, t / 0.01)
    irs = []
    for _ in range(2):
        noise = rng.standard_normal(n)
        noise = lowpass(highpass(noise, 180), bright)
        ir = noise * env
        ir = np.concatenate([np.zeros(int(predelay * SR)), ir])
        irs.append(ir / np.sqrt(np.sum(ir ** 2)))
    return irs


IR = reverb_ir()


def reverb(st, wet):
    out = np.copy(st)
    for ch in range(2):
        tail = signal.fftconvolve(st[ch], IR[ch])[: st.shape[1]]
        out[ch] = st[ch] + wet * tail
    return out


# ------------------------------------------------------------------------------------------- instruments
def epiano(midi, dur, vel=0.8):
    """FM electric piano: a 1:1 tine with a decaying index, a faint bell on the attack, gentle tremolo."""
    f = float(hz(midi))
    n = int((dur + 0.45) * SR)
    t = np.arange(n) / SR
    index = (1.1 + 1.3 * vel) * np.exp(-t * 5.5) + 0.32
    tone = np.sin(2 * np.pi * f * t + index * np.sin(2 * np.pi * f * t))
    if f * 14 < 16000:
        tone += 0.05 * vel * np.sin(2 * np.pi * f * 14 * t) * np.exp(-t * 40)
    env = np.minimum(1.0, t / 0.004) * np.exp(-t * (0.9 + f / 900.0))
    off = int(dur * SR)
    env[off:] *= np.exp(-(t[off:] - dur) * 14.0)
    return lowpass(tone * env * vel, 5600)


def kalimba(midi, vel=0.8, dur=1.6):
    """Kalimba / thumb piano: a warm fundamental, its inharmonic overtone, a tiny attack tick."""
    f = float(hz(midi))
    t = tvec(dur)
    x = np.sin(2 * np.pi * f * t) * np.exp(-t * 3.0)
    x += 0.10 * np.sin(2 * np.pi * f * 2.0 * t) * np.exp(-t * 7.0)
    if f * 5.9 < 14000:
        x += 0.10 * np.sin(2 * np.pi * f * 5.9 * t) * np.exp(-t * 18.0)
    tick = rng.standard_normal(len(t)) * np.exp(-t * 900.0) * 0.12
    x = (x + bandpass(tick, 1500, 5000)) * np.minimum(1.0, t / 0.0015)
    return lowpass(x * vel, 8500)


def bass(midi, dur, vel=0.8):
    """Round sine bass with a touch of second harmonic and tape-like saturation."""
    f = float(hz(midi))
    n = int((dur + 0.08) * SR)
    t = np.arange(n) / SR
    x = np.sin(2 * np.pi * f * t) + 0.22 * np.sin(2 * np.pi * 2 * f * t) + 0.06 * np.sin(2 * np.pi * 3 * f * t)
    x = np.tanh(1.4 * x) / np.tanh(1.4)
    env = np.minimum(1.0, t / 0.006) * np.exp(-t * 1.6)
    off = int(dur * SR)
    env[off:] *= np.exp(-(t[off:] - dur) * 40.0)
    return lowpass(x * env * vel, 700)


def pad(midis, dur, vel=0.5):
    """A soft, slow string-like pad: detuned saws through a low filter."""
    n = int((dur + 1.2) * SR)
    t = np.arange(n) / SR
    x = np.zeros(n)
    for m in midis:
        for detune in (-0.08, 0.0, 0.08):
            f = float(hz(m + detune))
            x += signal.sawtooth(2 * np.pi * f * t + rng.uniform(0, 6.28))
    x = lowpass(x / (3 * len(midis)), 950, order=4)
    env = np.minimum(1.0, t / 0.9)
    off = int(dur * SR)
    env[off:] *= np.linspace(1.0, 0.0, n - off) ** 2
    return x * env * vel


def kick(vel=0.9):
    t = tvec(0.45)
    freq = 54 + 80 * np.exp(-t * 30)
    x = np.sin(2 * np.pi * np.cumsum(freq) / SR) * np.exp(-t * 9.5)
    click = bandpass(rng.standard_normal(len(t)), 1200, 4000) * np.exp(-t * 600) * 0.15
    return lowpass(x + click, 3000) * vel


def snare(vel=0.7):
    """A brushy lo-fi snare: band-passed noise and a short body tone."""
    t = tvec(0.35)
    noise = bandpass(rng.standard_normal(len(t)), 1100, 5200) * np.exp(-t * 16)
    body = np.sin(2 * np.pi * 185 * t) * np.exp(-t * 28) * 0.5
    return lowpass(noise * 0.8 + body, 6500) * vel


def rim(vel=0.5):
    t = tvec(0.12)
    x = np.sin(2 * np.pi * 820 * t) * np.exp(-t * 60) + bandpass(rng.standard_normal(len(t)), 1500, 4500) * np.exp(-t * 90) * 0.4
    return lowpass(x, 5000) * vel


def hat(vel=0.4, open_=False):
    t = tvec(0.35 if open_ else 0.08)
    x = bandpass(rng.standard_normal(len(t)), 6500, 11000) * np.exp(-t * (12 if open_ else 70))
    return x * vel * 1.7


def shaker(vel=0.3):
    t = tvec(0.09)
    x = bandpass(rng.standard_normal(len(t)), 4500, 9000) * np.minimum(1.0, t / 0.012) * np.exp(-t * 45)
    return x * vel * 1.6


def crash(vel=0.5):
    t = tvec(2.2)
    x = bandpass(rng.standard_normal(len(t)), 3500, 11000) * np.exp(-t * 2.2) * np.minimum(1.0, t / 0.003)
    return lowpass(x, 9000) * vel


def riser(seconds):
    t = tvec(seconds)
    noise = rng.standard_normal(len(t))
    # sweep a band-pass upward by crossfading a few fixed bands
    bands = [(300, 900), (700, 2000), (1500, 4000), (3000, 7500)]
    x = np.zeros(len(t))
    for i, (lo, hi) in enumerate(bands):
        centre = (i + 0.5) / len(bands)
        weight = np.clip(1.0 - abs(t / seconds - centre) * len(bands) * 0.8, 0, 1)
        x += bandpass(noise, lo, hi) * weight
    return x * (t / seconds) ** 2


# ------------------------------------------------------------------------------------------------ the song
# Chords per bar (4-bar cycle). C7sus4 resolves to C7 on beat 3.
CHORDS = [
    [(0, [53, 57, 60, 64])],                      # Fmaj7
    [(0, [57, 60, 64, 67])],                      # Am7
    [(0, [58, 62, 65, 69])],                      # Bbmaj7
    [(0, [55, 58, 60, 65]), (2, [55, 58, 60, 64])],  # C7sus4 -> C7
]
ROOTS = [41, 45, 46, 36]
FIFTHS = [48, 52, 53, 43]

# Kalimba melody over the 4-bar cycle: (beat, midi, length)
MELODY = [
    (0.0, 72, 1.5), (1.5, 74, 0.5), (2.0, 72, 0.5), (2.5, 69, 1.5),
    (4.0, 67, 1.0), (5.0, 69, 0.5), (5.5, 72, 1.0), (6.5, 76, 1.5),
    (8.0, 77, 1.0), (9.0, 76, 0.5), (9.5, 74, 1.0), (10.5, 72, 0.5), (11.0, 69, 1.0),
    (12.0, 70, 1.0), (13.0, 72, 1.0), (14.0, 67, 1.5), (15.5, 65, 0.5),
]
MELODY_FILL = [(12.0, 70, 0.5), (12.5, 72, 0.5), (13.0, 74, 0.5), (13.5, 72, 0.5), (14.0, 67, 1.0), (15.0, 69, 0.5), (15.5, 72, 0.5)]

SWING = 0.08  # beats: the off-beat eighth lands late


def sw(beat):
    """Swing: off-beat eighths are pushed later."""
    frac = beat % 1.0
    return beat + (SWING if abs(frac - 0.5) < 1e-6 else 0.0)


def chord_at(bar, beat_in_bar):
    current = CHORDS[bar % 4][0][1]
    for start, notes in CHORDS[bar % 4]:
        if beat_in_bar >= start:
            current = notes
    return current


def compose(sections, total_seconds, stems=None):
    """sections: list of (name, start_beat, beats). Returns a stereo float array (2, n) at SR."""
    keys, bassT, drums, melody, pads, fx = (Track(total_seconds) for _ in range(6))
    b = lambda beat: beat * BEAT  # noqa: E731

    for name, start, length in sections:
        bars = int(round(length / 4))
        for k in range(bars):
            bar = start // 4 + k
            t0 = start + 4 * k
            cyc = bar % 4
            last_bar = k == bars - 1

            # ---------- keys
            if name in ("intro", "breakdown", "outro"):
                for at, notes in CHORDS[cyc]:
                    dur = (4 - at) if len(CHORDS[cyc]) == 1 or at > 0 else 2
                    for i, m in enumerate(notes):
                        keys.add(epiano(m, b(dur) - 0.05, 0.42), b(t0 + at) + i * 0.012)
            elif name == "riser":
                pass
            else:
                pattern = [(0, 1.0), (1.5, 0.5), (2.5, 1.0), (3.5, 0.5)] if name in ("groove", "finale") else [(0, 1.5), (2.0, 1.0), (3.5, 0.5)]
                for at, dur in pattern:
                    notes = chord_at(bar, at)
                    vel = 0.55 if at == 0 else 0.42
                    for i, m in enumerate(notes):
                        keys.add(epiano(m, b(dur) - 0.04, vel), b(sw(t0 + at)) + i * 0.008)

            # ---------- pad
            if name in ("breakdown", "outro", "finale"):
                pads.add(pad([n - 12 if n > 62 else n for n in CHORDS[cyc][0][1]], b(4), 0.55 if name != "finale" else 0.35), b(t0))

            # ---------- bass
            root, fifth = ROOTS[cyc], FIFTHS[cyc]
            if name == "light":
                for at, m, d in [(0, root, 1.5), (1.5, fifth, 0.5), (2, root, 1.5), (3.5, fifth, 0.5)]:
                    bassT.add(bass(m, b(d) - 0.03, 0.8), b(sw(t0 + at)))
            elif name in ("groove", "finale"):
                nxt = ROOTS[(cyc + 1) % 4]
                for at, m, d in [(0, root, 0.75), (0.75, root, 0.25), (1.5, fifth, 0.5), (2, root, 1.0), (3, fifth, 0.5), (3.5, nxt - 1 if nxt - 1 > 30 else nxt + 1, 0.5)]:
                    bassT.add(bass(m, b(d) - 0.03, 0.85), b(sw(t0 + at)))
            elif name == "breakdown":
                bassT.add(bass(root, b(4) - 0.1, 0.55), b(t0))
            elif name == "outro" and k == 0:
                bassT.add(bass(ROOTS[0], b(8), 0.7), b(t0))

            # ---------- drums
            if name == "light":
                drums.add(kick(0.7), b(t0))
                drums.add(kick(0.55), b(t0 + 2.5))
                drums.add(rim(0.45), b(t0 + 1))
                drums.add(rim(0.45), b(t0 + 3))
                for e in range(8):
                    drums.add(hat(0.22 if e % 2 else 0.3), b(sw(t0 + e * 0.5)), pan=0.3)
            elif name in ("groove", "finale"):
                for at in (0, 1.5, 2.5):
                    drums.add(kick(0.85 if at == 0 else 0.7), b(sw(t0 + at)))
                drums.add(snare(0.62), b(t0 + 1))
                drums.add(snare(0.66), b(t0 + 3))
                for e in range(8):
                    drums.add(hat(0.26 if e % 2 else 0.34, open_=(e == 7 and cyc == 3)), b(sw(t0 + e * 0.5)), pan=0.3)
                for s16 in range(16):
                    drums.add(shaker(0.12 + 0.06 * (s16 % 2)), b(t0 + s16 * 0.25 + (0.03 if s16 % 2 else 0)), pan=-0.35)
                if last_bar and name == "groove":
                    for s16 in (12, 13, 14, 15):
                        drums.add(snare(0.25 + 0.08 * (s16 - 12)), b(t0 + s16 * 0.25))
            elif name == "breakdown":
                drums.add(rim(0.35), b(t0 + 1))
                drums.add(rim(0.35), b(t0 + 3))
                for e in range(4):
                    drums.add(shaker(0.12), b(sw(t0 + e + 0.5)), pan=-0.3)
            elif name == "riser":
                for s in range(14):  # a building snare roll, then a breath before the drop
                    at = s * 0.25
                    drums.add(snare(0.12 + 0.045 * s), b(t0 + at))
                fx.add(riser(b(3.5)), b(t0), gain=0.18)

            if name == "finale" and k == 0:
                drums.add(crash(0.55), b(t0), pan=0.2)

            # ---------- melody
            if name in ("light", "groove", "finale") or (name == "intro" and k >= bars - 2):
                cycle_start = t0 - 4 * cyc
                notes = MELODY
                if name in ("groove", "finale") and cyc == 3:
                    notes = [m for m in MELODY if m[0] < 12] + MELODY_FILL
                for at, m, d in notes:
                    if 4 * cyc <= at < 4 * cyc + 4:
                        vel = 0.62 if name != "intro" else 0.45
                        melody.add(kalimba(m, vel * rng.uniform(0.9, 1.05), dur=max(1.0, b(d) + 0.8)), b(sw(cycle_start + at)) + rng.uniform(-0.004, 0.004), pan=-0.15)
            elif name == "breakdown" and cyc in (0, 2):
                for at, m in ((0, 72), (1.5, 69), (3, 67)):
                    melody.add(kalimba(m, 0.4, 1.8), b(t0 + at), pan=-0.15)
            elif name == "outro" and k == 0:
                for at, m in ((0, 72), (0.5, 69), (1.0, 65), (2.0, 77)):
                    melody.add(kalimba(m, 0.5, 2.5), b(t0 + at), pan=-0.15)

    # ---------- mix
    keys_st = keys.stereo()
    trem = 1.0 + 0.10 * np.sin(2 * np.pi * 4.2 * np.arange(keys.n) / SR)
    keys_st[0] *= trem
    keys_st[1] *= 2.0 - trem
    parts = {
        "keys": 0.30 * reverb(keys_st, 0.30),
        "bass": 0.30 * bassT.stereo(),
        "drums": 0.62 * reverb(drums.stereo(), 0.10),
        "melody": 0.56 * reverb(melody.stereo(), 0.42),
        "pad": 0.22 * reverb(pads.stereo(), 0.5),
        "fx": 0.30 * fx.stereo(),
    }
    if stems is not None:
        stems.update(parts)
    mix = (
        0.30 * reverb(keys_st, 0.30)
        + 0.30 * bassT.stereo()
        + 0.62 * reverb(drums.stereo(), 0.10)
        + 0.56 * reverb(melody.stereo(), 0.42)
        + 0.22 * reverb(pads.stereo(), 0.5)
        + 0.30 * fx.stereo()
    )
    # vinyl: a quiet bed of hiss and sparse crackle
    n = mix.shape[1]
    hiss = lowpass(rng.standard_normal(n), 4500) * 0.0016
    crackle = np.zeros(n)
    hits = rng.integers(0, n, int(total_seconds * 5))
    crackle[hits] = rng.uniform(0.01, 0.05, len(hits)) * rng.choice([-1, 1], len(hits))
    crackle = bandpass(crackle, 900, 5000)
    mix += np.stack([hiss + crackle, hiss + np.roll(crackle, 7)])
    # gentle glue: soft saturation, then a high-shelf-ish roll-off above 11 kHz
    mix = np.stack([master_eq(mix[0]), master_eq(mix[1])])
    mix = np.tanh(mix * 1.15) / 1.15
    mix = np.stack([lowpass(mix[0], 13000), lowpass(mix[1], 13000)])
    return mix[:, : int(total_seconds * SR)]


# ------------------------------------------------------------------------------------------- sound effects
def bell(midi, vel=0.5, dur=1.2, bright=1.0):
    """A soft FM bell (ratio 3.5) for chimes."""
    f = float(hz(midi))
    t = tvec(dur)
    index = 1.6 * bright * np.exp(-t * 6)
    x = np.sin(2 * np.pi * f * t + index * np.sin(2 * np.pi * f * 3.5 * t)) * np.exp(-t * 3.5) * np.minimum(1.0, t / 0.002)
    return lowpass(x * vel, 7000)


def woody(freq, vel=0.5):
    """A small wooden click: a pitched knock with a quick drop and a tick."""
    t = tvec(0.09)
    f = freq * (1.0 + 0.35 * np.exp(-t * 120))
    x = np.sin(2 * np.pi * np.cumsum(f) / SR) * np.exp(-t * 55)
    x += bandpass(rng.standard_normal(len(t)), 2000, 6000) * np.exp(-t * 700) * 0.25
    return lowpass(x * vel, 6000)


def pop(vel=0.5, lo=380, hi=950):
    t = tvec(0.09)
    f = lo + (hi - lo) * np.minimum(1.0, t / 0.04)
    x = np.sin(2 * np.pi * np.cumsum(f) / SR) * np.exp(-t * 45) * np.minimum(1.0, t / 0.002)
    return lowpass(x * vel, 5000)


def swish(vel=0.4, dur=0.22, lo=500, hi=3500):
    t = tvec(dur)
    env = np.sin(np.pi * t / dur) ** 2
    return bandpass(rng.standard_normal(len(t)), lo, hi) * env * vel


def splat(vel=0.6):
    t = tvec(0.14)
    x = lowpass(rng.standard_normal(len(t)), 1300) * np.exp(-t * 35) * 0.9
    thump = np.sin(2 * np.pi * np.cumsum(170 - 80 * np.minimum(1, t / 0.06)) / SR) * np.exp(-t * 30)
    return (x + thump) * vel


def purr(vel=0.4, dur=1.3):
    t = tvec(dur)
    am = 0.5 + 0.5 * np.sin(2 * np.pi * 24 * t)
    x = lowpass(rng.standard_normal(len(t)), 260, order=4) * am * np.sin(np.pi * t / dur) ** 0.6
    return x * vel * 3.0


PENTA = [65, 67, 69, 72, 74, 77, 79]


def sfx(cue, track, at):
    """Places one cue's sound on `track` at time `at` (seconds)."""
    r = rng.uniform
    if cue == "type":
        track.add(woody(r(1500, 1900), 0.18), at, pan=r(-0.2, 0.2))
    elif cue in ("click", "page"):
        track.add(woody(r(900, 1100), 0.35), at)
        if cue == "page":
            track.add(swish(0.10, 0.18, 800, 4000), at - 0.03)
    elif cue == "open":
        track.add(swish(0.14, 0.26, 400, 2800), at - 0.08)
        track.add(kalimba(72, 0.14, 1.2), at + 0.02)
    elif cue == "confirm":
        track.add(kalimba(69, 0.16, 1.2), at)
        track.add(kalimba(72, 0.16, 1.4), at + 0.09)
    elif cue == "tile":
        track.add(woody(r(700, 950), 0.30), at, pan=r(-0.3, 0.3))
        track.add(kalimba(int(rng.choice(PENTA)), 0.10, 0.6), at + 0.005)
    elif cue == "solved":
        for i, m in enumerate((65, 69, 72, 77)):
            track.add(kalimba(m, 0.13, 1.6), at + 0.07 * i)
        track.add(bell(84, 0.06, 1.4), at + 0.28)
    elif cue == "squash":
        track.add(splat(0.42), at, pan=r(-0.25, 0.25))
    elif cue == "result":
        track.add(bell(72, 0.18), at)
        track.add(bell(77, 0.15), at + 0.12)
    elif cue == "paint":
        track.add(pop(0.16, 500, 800 + r(0, 300)), at, pan=r(-0.3, 0.3))
    elif cue == "pop" or cue == "review":
        track.add(pop(0.30 if cue == "pop" else 0.24, 360, 900 + r(-80, 120)), at, pan=r(-0.2, 0.2))
    elif cue == "card":
        track.add(swish(0.16, 0.16, 900, 4500), at, pan=r(-0.3, 0.3))
        track.add(woody(600, 0.18), at + 0.12)
    elif cue == "tune":
        t = tvec(0.5)
        f = 300 + 140 * np.minimum(1.0, t / 0.35)
        x = np.sin(2 * np.pi * np.cumsum(f) / SR) * np.sin(np.pi * t / 0.5) * 0.18
        track.add(x, at)
        track.add(bell(69, 0.12), at + 0.4)
    elif cue == "coffee":
        for i in range(3):
            track.add(pop(0.12, 300, 520 + 60 * i), at + 0.11 * i)
        track.add(swish(0.06, 0.6, 1500, 6000), at + 0.2)
    elif cue == "purr":
        track.add(purr(0.22), at)
        for i, m in enumerate((77, 81, 84)):
            track.add(bell(m, 0.05, 0.8), at + 0.25 + 0.18 * i, pan=0.3)
    elif cue == "confetti":
        for i in range(9):
            track.add(pop(0.18, 330, 750 + r(0, 400)), at + r(0, 0.32), pan=r(-0.6, 0.6))
        for i, m in enumerate((65, 69, 72, 77)):
            track.add(bell(m, 0.10, 1.6), at + 0.05 + 0.06 * i)
    elif cue == "night":
        track.add(bell(65, 0.12, 2.0, 0.6), at)
        track.add(bell(72, 0.08, 2.0, 0.6), at + 0.25)
    elif cue == "morning":
        for i, m in enumerate((72, 77, 81)):
            track.add(kalimba(m, 0.12, 1.4), at + 0.12 * i)
    elif cue == "title":
        track.add(swish(0.07, 0.35, 300, 2400), at - 0.12)
    elif cue == "launch":
        track.add(swish(0.18, 0.45, 400, 5000), at - 0.3)
        for i, m in enumerate((65, 69, 72, 77)):
            track.add(bell(m, 0.14, 2.2), at + 0.04 * i)
        for i in range(14):  # a soft shimmer of coins
            track.add(bell(int(rng.choice([84, 86, 88, 89])), 0.035, 0.5, 0.5), at + 0.15 + r(0, 1.6), pan=r(-0.7, 0.7))


def render_sfx(events, total_seconds):
    """events: list of (seconds, cue). Returns a stereo (2, n) array."""
    track = Track(total_seconds)
    for at, cue in events:
        sfx(cue, track, at)
    st = reverb(track.stereo(), 0.18)
    return st[:, : int(total_seconds * SR)]
