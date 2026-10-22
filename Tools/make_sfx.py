"""
Makes every sound effect and music loop for Knockback Rumble from scratch (pure synthesis,
no samples, so there are no licenses to worry about).

Output: Assets/KnockbackRumble/Art/Resources/SFX/<name>.bytes
  (plain 16-bit mono WAV files with a .bytes extension; the game decodes them itself,
   so Unity's audio import settings never matter. Same trick as the menu art.)

Run:  python Tools/make_sfx.py           (needs numpy and scipy)
To replace a sound with a real recording later: save it as 16-bit mono WAV and
rename it to <name>.bytes in that folder.
"""
import os
import wave

import numpy as np
from scipy import signal

SR = 22050
OUT = os.path.join(os.path.dirname(__file__), "..", "Assets", "KnockbackRumble", "Art", "Resources", "SFX")
rng = np.random.default_rng(69)


# ---------------------------------------------------------------- building blocks

def t_(dur):
    return np.arange(int(SR * dur)) / SR


def osc(freq, dur, kind="sine"):
    """freq may be a number or an array (pitch sweep)."""
    n = int(SR * dur)
    f = np.broadcast_to(np.asarray(freq, dtype=float), (n,)) if np.ndim(freq) == 0 else np.asarray(freq, dtype=float)[:n]
    ph = 2 * np.pi * np.cumsum(f) / SR
    if kind == "sine":
        return np.sin(ph)
    if kind == "square":
        return np.sign(np.sin(ph)) * 0.6
    if kind == "saw":
        return 2 * ((ph / (2 * np.pi)) % 1.0) - 1
    if kind == "tri":
        return 2 * np.abs(2 * ((ph / (2 * np.pi)) % 1.0) - 1) - 1
    raise ValueError(kind)


def sweep(f0, f1, dur, curve="exp"):
    x = np.linspace(0, 1, int(SR * dur))
    if curve == "exp":
        return f0 * (f1 / f0) ** x
    return f0 + (f1 - f0) * x


def noise(dur):
    return rng.uniform(-1, 1, int(SR * dur))


def env(dur, a=0.005, d=0.1, s=0.0, r=0.05, hold=None):
    """Attack-decay-sustain-release envelope over dur seconds."""
    n = int(SR * dur)
    e = np.zeros(n)
    ai, di, ri = int(SR * a), int(SR * d), int(SR * r)
    ai = max(ai, 1)
    e[:ai] = np.linspace(0, 1, ai)
    end_d = min(n, ai + di)
    e[ai:end_d] = np.linspace(1, s, end_d - ai) if end_d > ai else []
    e[end_d:] = s
    if ri > 0 and s > 0:
        e[-ri:] *= np.linspace(1, 0, ri)
    return e


def decay(dur, k):
    return np.exp(-t_(dur) * k)


def lp(x, cutoff, order=2):
    b, a = signal.butter(order, min(cutoff / (SR / 2), 0.99), "low")
    return signal.lfilter(b, a, x)


def hp(x, cutoff, order=2):
    b, a = signal.butter(order, min(cutoff / (SR / 2), 0.99), "high")
    return signal.lfilter(b, a, x)


def bp(x, lo, hi, order=2):
    b, a = signal.butter(order, [lo / (SR / 2), min(hi / (SR / 2), 0.99)], "band")
    return signal.lfilter(b, a, x)


def sweep_bp(x, f0, f1, q=3.0):
    """Band-pass with a moving center (processed in small blocks)."""
    out = np.zeros_like(x)
    block = 256
    centers = np.geomspace(f0, f1, max(1, len(x) // block + 1))
    zi = None
    for i, c in enumerate(centers):
        s = x[i * block:(i + 1) * block]
        if len(s) == 0:
            break
        lo, hi = c / (1 + 1 / q), c * (1 + 1 / q)
        b, a = signal.butter(2, [lo / (SR / 2), min(hi / (SR / 2), 0.99)], "band")
        if zi is None or len(zi) != max(len(a), len(b)) - 1:
            zi = signal.lfilter_zi(b, a) * 0
        o, zi = signal.lfilter(b, a, s, zi=zi)
        out[i * block:i * block + len(o)] = o
    return out


def drive(x, amount):
    return np.tanh(x * amount) / np.tanh(amount)


def mix(*parts):
    n = max(len(p) for p in parts)
    out = np.zeros(n)
    for p in parts:
        out[:len(p)] += p
    return out


def at(x, start, total=None):
    """Place x starting at `start` seconds inside a buffer."""
    s = int(SR * start)
    n = int(SR * total) if total else s + len(x)
    out = np.zeros(max(n, s + len(x)))
    out[s:s + len(x)] += x
    return out


def bell(freq, dur, k=6.0):
    """Bright metallic bell (inharmonic partials)."""
    parts = [(1.0, 1.0), (2.76, 0.5), (5.4, 0.25), (8.93, 0.12)]
    return sum(osc(freq * m, dur) * g * decay(dur, k * (1 + m * 0.3)) for m, g in parts)


def echo(x, delay, fb, n=4):
    out = np.copy(x)
    d = int(SR * delay)
    out = np.concatenate([out, np.zeros(d * n)])
    for i in range(1, n + 1):
        out[d * i:d * i + len(x)] += x * (fb ** i)
    return out


def norm(x, peak=0.9):
    m = np.max(np.abs(x)) or 1.0
    return x / m * peak


def fade(x, ms=4):
    n = min(len(x) // 2, int(SR * ms / 1000))
    if n > 0:
        x[:n] *= np.linspace(0, 1, n)
        x[-n:] *= np.linspace(1, 0, n)
    return x


def note(name):
    names = {"C": 0, "C#": 1, "Db": 1, "D": 2, "D#": 3, "Eb": 3, "E": 4, "F": 5, "F#": 6, "Gb": 6,
             "G": 7, "G#": 8, "Ab": 8, "A": 9, "A#": 10, "Bb": 10, "B": 11}
    pitch, octave = name[:-1], int(name[-1])
    midi = 12 * (octave + 1) + names[pitch]
    return 440.0 * 2 ** ((midi - 69) / 12)


def save(name, x, peak=0.9):
    x = fade(norm(np.asarray(x, dtype=float), peak))
    data = (np.clip(x, -1, 1) * 32767).astype("<i2")
    os.makedirs(OUT, exist_ok=True)
    path = os.path.join(OUT, name + ".bytes")
    with wave.open(path, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes(data.tobytes())
    print(f"  {name:18s} {len(x) / SR:5.2f}s")


# ---------------------------------------------------------------- sound effects

def ui():
    save("ui_click", osc(1400, 0.05) * decay(0.05, 90) + noise(0.05) * decay(0.05, 300) * 0.3, 0.6)
    save("ui_equip", mix(osc(660, 0.08, "tri") * decay(0.08, 30), at(osc(990, 0.14, "tri") * decay(0.14, 20), 0.07)), 0.6)


def show():
    save("count_beep", osc(880, 0.16, "square") * env(0.16, 0.003, 0.12, 0.3, 0.03), 0.6)
    # Air horn: detuned saws with a little wobble
    d = 0.9
    vib = 1 + 0.01 * np.sin(2 * np.pi * 6 * t_(d))
    horn = sum(osc(f * vib, d, "saw") for f in (466, 470, 587, 592, 698))
    horn = drive(lp(horn, 3200), 2.5) * env(d, 0.02, 0.1, 0.9, 0.12)
    save("go_horn", horn)
    buzz = drive(osc(110, 0.8, "square") + osc(113, 0.8, "square"), 3) * env(0.8, 0.005, 0.1, 0.9, 0.08)
    save("end_buzzer", lp(buzz, 2500), 0.75)
    # Fanfare: brassy arpeggio into a held chord
    notes = [("C5", 0.0), ("E5", 0.12), ("G5", 0.24), ("C6", 0.36)]
    brass = lambda f, dur: lp(osc(f, dur, "saw") + 0.5 * osc(f * 1.003, dur, "saw"), 2600) * env(dur, 0.02, 0.1, 0.8, 0.1)
    parts = [at(brass(note(n), 0.14), s) for n, s in notes]
    parts.append(at(sum(brass(note(n), 1.1) for n in ("C5", "E5", "G5", "C6")), 0.5))
    save("fanfare", echo(mix(*parts), 0.11, 0.25, 3))
    # Sad trombone
    tb = []
    seq = [("D4", 0.0, 0.32), ("C#4", 0.38, 0.32), ("C4", 0.76, 0.32), ("B3", 1.14, 1.0)]
    for n, s, dur in seq:
        f = note(n)
        vib = 1 + (0.025 * np.sin(2 * np.pi * 5.5 * t_(dur)) if dur > 0.5 else 0)
        tone = lp(osc(f * vib, dur, "saw") + osc(f * 2 * vib, dur, "saw") * 0.3, 1400) * env(dur, 0.04, 0.1, 0.85, 0.12)
        tb.append(at(tone, s))
    save("sad_trombone", mix(*tb), 0.8)
    # Applause / crowd: lots of tiny claps
    d = 2.2
    clap = np.zeros(int(SR * d))
    shape = np.concatenate([np.linspace(0, 1, int(SR * 0.3)), np.ones(int(SR * 1.0)), np.linspace(1, 0, int(SR * 0.9))])[:len(clap)]
    for _ in range(900):
        p = int(rng.uniform(0, len(clap) - 400))
        if rng.uniform() > shape[p]:
            continue
        c = bp(noise(0.012), 900, 4500) * decay(0.012, 300) * rng.uniform(0.3, 1)
        clap[p:p + len(c)] += c
    save("applause", clap, 0.6)
    save("checkpoint", mix(bell(note("E6"), 0.6, 5), at(bell(note("B6"), 0.7, 5), 0.1)), 0.6)


def weapons():
    # Shove: whoosh + thump
    w = sweep_bp(noise(0.25), 400, 2500, 2) * env(0.25, 0.04, 0.2, 0, 0)
    thump = osc(sweep(180, 60, 0.15), 0.15) * decay(0.15, 25)
    save("shove", mix(w * 1.2, at(thump, 0.06)), 0.8)
    # Pop Pistol: toy pop
    pop = mix(osc(sweep(900, 180, 0.08), 0.08) * decay(0.08, 45), hp(noise(0.05), 1500) * decay(0.05, 120) * 0.7)
    save("pistol", pop, 0.75)
    # Boom Stick
    blast = drive(lp(noise(0.5), 2200) * decay(0.5, 9) + osc(sweep(140, 40, 0.5), 0.5) * decay(0.5, 7) * 1.4, 3)
    save("shotgun", echo(blast, 0.09, 0.2, 2))
    # Long Shot: sharp crack with a long canyon echo
    crack = mix(hp(noise(0.03), 2500) * decay(0.03, 150) * 1.5, osc(sweep(1800, 300, 0.05), 0.05) * decay(0.05, 60))
    body = lp(noise(0.25), 900) * decay(0.25, 18) * 0.5
    save("sniper", echo(mix(crack, body), 0.22, 0.35, 4))
    # Rocket launch
    d = 0.7
    rl = sweep_bp(noise(d), 300, 3000, 3) * env(d, 0.01, 0.6, 0, 0) * 1.4 + osc(sweep(90, 160, d), d) * decay(d, 6) * 0.6
    save("rocket", mix(at(hp(noise(0.04), 1000) * decay(0.04, 100), 0), rl))
    # Explosion
    d = 1.6
    boom = lp(noise(d), 700) * decay(d, 3.5) * 1.3 + osc(sweep(90, 28, d), d) * decay(d, 4) * 1.6
    crackle = hp(noise(d), 2000) * decay(d, 9) * 0.35
    save("explosion", drive(boom + crackle, 2.5))
    # Ammo crate: two metal clicks
    click = lambda: bp(noise(0.03), 2500, 7000) * decay(0.03, 160) + osc(3200, 0.03) * decay(0.03, 120) * 0.3
    save("reload", mix(click(), at(click() * 0.9, 0.14), at(lp(noise(0.06), 600) * decay(0.06, 50), 0.3)), 0.7)
    save("empty_click", bp(noise(0.02), 3000, 8000) * decay(0.02, 200), 0.4)


def hits():
    thwack = bp(noise(0.12), 800, 3500) * decay(0.12, 35) + osc(sweep(260, 120, 0.12), 0.12) * decay(0.12, 30)
    save("hit_light", thwack, 0.7)
    heavy = drive(lp(noise(0.35), 1500) * decay(0.35, 14) + osc(sweep(150, 45, 0.35), 0.35) * decay(0.35, 10) * 1.5, 2.5)
    save("hit_heavy", heavy)
    save("hitmarker", osc(2300, 0.035, "tri") * decay(0.035, 110), 0.45)
    # Cartoon spring
    d = 0.6
    f = sweep(160, 520, d) * (1 + 0.25 * np.sin(2 * np.pi * 18 * t_(d)))
    save("boing", osc(f, d, "tri") * decay(d, 5), 0.7)
    # Slide whistle (falling off)
    d = 1.1
    f = sweep(1500, 260, d) * (1 + 0.012 * np.sin(2 * np.pi * 7 * t_(d)))
    save("fall_whistle", (osc(f, d) + 0.15 * osc(f * 2, d)) * env(d, 0.04, 0.1, 0.9, 0.15), 0.6)
    save("splat", drive(mix(lp(noise(0.3), 1200) * decay(0.3, 16), osc(sweep(120, 50, 0.2), 0.2) * decay(0.2, 18)), 2), 0.8)


def movement():
    save("jump", osc(sweep(260, 620, 0.12), 0.12, "tri") * env(0.12, 0.005, 0.1, 0, 0), 0.4)
    save("double_jump", osc(sweep(500, 1150, 0.14), 0.14, "tri") * env(0.14, 0.005, 0.12, 0, 0), 0.4)
    save("land", mix(lp(noise(0.12), 500) * decay(0.12, 35), osc(sweep(110, 60, 0.1), 0.1) * decay(0.1, 40) * 0.6), 0.55)
    save("dive", sweep_bp(noise(0.3), 1800, 500, 2) * env(0.3, 0.02, 0.25, 0, 0), 0.55)


def cash():
    drawer = lp(noise(0.08), 3000) * decay(0.08, 50) * 0.5
    ching = bell(note("E7"), 0.9, 4) + bell(note("C7"), 0.9, 4) * 0.7
    save("cash", mix(drawer, at(osc(1800, 0.02) * decay(0.02, 200), 0.05), at(ching, 0.08)), 0.7)
    save("coin", bell(note("A6"), 0.25, 14), 0.4)
    womp = osc(sweep(420, 300, 0.22), 0.22, "square") * env(0.22, 0.01, 0.1, 0.7, 0.05)
    womp2 = osc(sweep(300, 170, 0.4), 0.4, "square") * env(0.4, 0.01, 0.2, 0.6, 0.12)
    save("cash_lose", lp(mix(womp, at(womp2, 0.22)), 1800), 0.55)
    jingle = [at(bell(rng.uniform(2200, 4200), 0.3, 12) * rng.uniform(0.4, 1), rng.uniform(0, 0.25)) for _ in range(9)]
    save("bag_grab", mix(*jingle, at(bell(note("C7"), 0.6, 5), 0.05)), 0.7)
    scatter = [at(bell(rng.uniform(1800, 4000), 0.25, 14) * rng.uniform(0.3, 1), rng.uniform(0.05, 0.6)) for _ in range(16)]
    save("bag_drop", mix(lp(noise(0.12), 700) * decay(0.12, 30), *scatter), 0.7)


def gadgets():
    save("throw", sweep_bp(noise(0.22), 600, 2200, 2) * env(0.22, 0.02, 0.18, 0, 0), 0.5)
    save("bounce", (osc(sweep(900, 700, 0.06), 0.06) + bp(noise(0.06), 1000, 3000) * 0.4) * decay(0.06, 70), 0.5)
    save("beep", osc(1600, 0.06, "square") * env(0.06, 0.002, 0.05, 0.5, 0.01), 0.4)
    save("mine_arm", mix(osc(1300, 0.07, "square") * decay(0.07, 30), at(osc(1700, 0.09, "square") * decay(0.09, 25), 0.12)), 0.45)
    save("mine_drop", mix(lp(noise(0.1), 900) * decay(0.1, 40), osc(400, 0.1, "square") * decay(0.1, 50) * 0.3), 0.6)


def streaks():
    arp = [at(osc(note(n), 0.12, "square") * env(0.12, 0.003, 0.1, 0.3, 0.02), i * 0.07)
           for i, n in enumerate(["C5", "E5", "G5", "C6", "E6", "G6"])]
    save("streak_ready", lp(mix(*arp), 4000), 0.55)
    # Seagull squawk (Gary): nasal pitch contour, twice
    def squawk(d):
        x = np.linspace(0, 1, int(SR * d))
        contour = 900 + 700 * np.sin(np.pi * x) - 300 * x
        s = osc(contour, d, "saw") * (0.6 + 0.4 * np.sin(2 * np.pi * 42 * t_(d)))
        s = bp(s, 1200, 3800) + bp(s, 600, 1100) * 0.5
        return drive(s, 2) * env(d, 0.01, 0.1, 0.8, 0.08)
    save("seagull", mix(squawk(0.38), at(squawk(0.5) * 0.9, 0.45)), 0.75)
    d = 1.6
    f = 450 + 450 * np.sin(np.pi * np.linspace(0, 1, int(SR * d))) ** 2
    save("siren", lp(osc(f, d, "saw") + osc(f * 1.01, d, "saw"), 2500) * env(d, 0.05, 0.1, 0.9, 0.2), 0.55)
    d = 1.3
    save("bomb_whistle", osc(sweep(2100, 650, d), d) * env(d, 0.1, 0.1, 0.9, 0.05), 0.5)


def hazards():
    save("bumper", osc(sweep(220, 700, 0.35) * (1 + 0.3 * np.sin(2 * np.pi * 22 * t_(0.35))), 0.35, "tri") * decay(0.35, 9), 0.6)
    ring = sum(osc(f, 0.7) * decay(0.7, 6) * g for f, g in ((180, 1), (433, 0.5), (871, 0.3), (1340, 0.2)))
    slam = drive(mix(lp(noise(0.4), 1800) * decay(0.4, 14) * 1.5, osc(sweep(90, 40, 0.3), 0.3) * decay(0.3, 9) * 1.5, ring * 0.5), 2)
    save("crusher", slam, 0.85)
    crack = np.zeros(int(SR * 0.45))
    for _ in range(26):
        p = int(rng.uniform(0, len(crack) - 300))
        c = bp(noise(0.01), 1500, 6000) * decay(0.01, 400) * rng.uniform(0.3, 1)
        crack[p:p + len(c)] += c
    save("tile_crack", mix(crack, lp(noise(0.45), 300) * decay(0.45, 6) * 0.3), 0.6)
    save("tile_fall", lp(noise(1.0), 260) * env(1.0, 0.03, 0.9, 0, 0) * 1.5, 0.6)


def heckles():
    # Crowd boo: a dozen "ooo" voices sliding down
    d = 1.3
    boo = np.zeros(int(SR * d))
    for _ in range(12):
        f0 = rng.uniform(110, 230)
        start = rng.uniform(0, 0.12)
        dur = d - start
        v = osc(sweep(f0, f0 * 0.82, dur) * (1 + 0.02 * np.sin(2 * np.pi * rng.uniform(4, 7) * t_(dur))), dur, "saw")
        v = bp(v, 220, 420) + bp(v, 700, 1000) * 0.35
        v = v * env(dur, 0.12, 0.2, 0.8, 0.35)
        i = int(SR * start)
        boo[i:i + len(v)] += v[:len(boo) - i]
    boo += bp(noise(d), 200, 900) * env(d, 0.1, 0.2, 0.6, 0.4) * 0.15
    save("boo", boo, 0.75)
    # Fart: buzzy, wobbly, a little wet at the end
    d = 0.75
    x = np.linspace(0, 1, int(SR * d))
    f = 95 + 25 * np.sin(2 * np.pi * 3.2 * x) + rng.normal(0, 6, len(x)) - 30 * x
    buzz = osc(f, d, "saw") * (0.65 + 0.35 * np.sign(np.sin(2 * np.pi * 23 * t_(d))))
    buzz = lp(buzz, 850) + lp(noise(d), 400) * 0.25
    wet = at(bp(noise(0.12), 300, 1500) * decay(0.12, 30) * 0.6, d - 0.1)
    save("fart", mix(drive(buzz * env(d, 0.01, 0.1, 0.9, 0.12), 2), wet), 0.8)
    # Tomato splat (wet)
    sq = bp(noise(0.3), 400, 3500) * decay(0.3, 14) + lp(noise(0.3), 300) * decay(0.3, 20) * 0.8
    save("tomato", mix(sq, at(bp(noise(0.08), 1500, 5000) * decay(0.08, 50) * 0.4, 0.05)), 0.75)


# ---------------------------------------------------------------- music

def drums(bars, bpm, swing=0.0, busy=True):
    beat = 60 / bpm
    total = bars * 4 * beat
    out = np.zeros(int(SR * total))
    kick = osc(sweep(130, 45, 0.25), 0.25) * decay(0.25, 14)
    snare = mix(bp(noise(0.18), 900, 7000) * decay(0.18, 22), osc(190, 0.1) * decay(0.1, 30) * 0.4)
    hat = hp(noise(0.04), 7000) * decay(0.04, 120) * 0.35
    for bar in range(bars):
        for b in range(4):
            s = (bar * 4 + b) * beat
            def put(x, when, g=1.0):
                i = int(SR * when)
                seg = x[:max(0, len(out) - i)]
                out[i:i + len(seg)] += seg * g
            if b in (0, 2) or (busy and b == 3 and bar % 2 == 1):
                put(kick, s if b != 3 else s + beat * 0.5)
            if b in (1, 3):
                put(snare, s, 0.8)
            put(hat, s)
            put(hat, s + beat * (0.5 + swing), 0.7)
    return out


def bass_line(chords, bpm, pattern, kind="saw", cutoff=700):
    beat = 60 / bpm
    out = np.zeros(int(SR * len(chords) * 4 * beat))
    for i, root in enumerate(chords):
        for step, (offset, semis, length) in enumerate(pattern):
            f = note(root) * 2 ** (semis / 12)
            d = length * beat
            tone = lp(osc(f, d, kind) + osc(f / 2, d, "sine") * 0.6, cutoff) * env(d, 0.005, 0.05, 0.8, 0.03)
            s = int(SR * (i * 4 * beat + offset * beat))
            seg = tone[:max(0, len(out) - s)]
            out[s:s + len(seg)] += seg
    return out


def chord_stabs(chords, bpm, offsets, kind="square", cutoff=2400, length=0.35, gain=1.0):
    beat = 60 / bpm
    out = np.zeros(int(SR * len(chords) * 4 * beat))
    for i, ch in enumerate(chords):
        for off in offsets:
            d = length * beat
            tone = sum(osc(note(n), d, kind) for n in ch) / len(ch)
            tone = lp(tone, cutoff) * env(d, 0.005, 0.1, 0.6, 0.03) * gain
            s = int(SR * (i * 4 * beat + off * beat))
            seg = tone[:max(0, len(out) - s)]
            out[s:s + len(seg)] += seg
    return out


def melody(notes, bpm, kind="square", cutoff=3500):
    beat = 60 / bpm
    total = max(s + l for _, s, l in notes) * beat
    out = np.zeros(int(SR * total) + 1)
    for n, start, length in notes:
        if n is None:
            continue
        d = length * beat
        f = note(n) * (1 + 0.006 * np.sin(2 * np.pi * 5 * t_(d)))
        tone = lp(osc(f, d, kind) + osc(f * 1.005, d, kind) * 0.5, cutoff) * env(d, 0.008, 0.08, 0.7, 0.05)
        s = int(SR * start * beat)
        out[s:s + len(tone)] += tone
    return out


def music_game():
    """Fast, punchy game-show funk. 8 bars, 128 bpm, loops."""
    bpm = 128
    roots = ["C2", "C2", "A1", "A1", "F1", "F1", "G1", "G1"]
    pattern = [(0, 0, 0.45), (0.75, 0, 0.2), (1.5, 12, 0.3), (2, 0, 0.45), (2.75, 7, 0.2), (3.5, 10, 0.3)]
    bass = bass_line(roots, bpm, pattern, "saw", 800)
    chords = [["E4", "G4", "C5"], ["E4", "G4", "C5"], ["E4", "A4", "C5"], ["E4", "A4", "C5"],
              ["F4", "A4", "C5"], ["F4", "A4", "C5"], ["D4", "G4", "B4"], ["F4", "G4", "B4"]]
    stabs = chord_stabs(chords, bpm, [0.5, 1.5, 2.5, 3.5], "square", 2600, 0.3, 0.8)
    lead_notes = [
        ("G5", 0, 0.5), ("E5", 0.5, 0.5), ("G5", 1.5, 0.5), ("A5", 2, 1), ("G5", 3, 0.5),
        ("E5", 4, 0.5), ("D5", 4.5, 0.5), ("C5", 5, 1.5), ("D5", 6.5, 0.5), ("E5", 7, 1),
        ("C5", 8, 0.5), ("E5", 8.5, 0.5), ("A5", 9.5, 0.5), ("C6", 10, 1), ("A5", 11, 0.5),
        ("G5", 12, 0.5), ("F5", 12.5, 0.5), ("E5", 13, 1.5), ("C5", 14.5, 0.5), ("A4", 15, 1),
        ("A4", 16, 0.5), ("C5", 16.5, 0.5), ("F5", 17.5, 0.5), ("A5", 18, 1), ("G5", 19, 0.5),
        ("F5", 20, 0.5), ("E5", 20.5, 0.5), ("C5", 21, 1.5), ("D5", 22.5, 0.5), ("F5", 23, 1),
        ("G5", 24, 1), ("B5", 25, 0.5), ("D6", 25.5, 1), ("B5", 26.5, 0.5), ("G5", 27, 1),
        ("F5", 28, 0.5), ("G5", 28.5, 0.5), ("B5", 29, 0.5), ("D6", 29.5, 1.5), ("G5", 31, 0.75),
    ]
    lead = melody(lead_notes, bpm, "square", 3000)
    dr = drums(8, bpm, 0.0, True)
    n = len(dr)
    song = mix(dr[:n] * 0.9, bass[:n] * 0.55, stabs[:n] * 0.3, lead[:n] * 0.32)[:n]
    save("music_game", song, 0.7)


def music_menu():
    """Laid-back lounge loop for menus and loading screens. 8 bars, 98 bpm, swung."""
    bpm = 98
    roots = ["D2", "G1", "C2", "A1", "D2", "G1", "C2", "C2"]
    walk = [(0, 0, 0.9), (1, 7, 0.9), (2, 12, 0.9), (3, 10, 0.9)]
    bass = bass_line(roots, bpm, walk, "tri", 600)
    chords = [["F4", "A4", "C5", "E5"], ["F4", "B4", "D5"], ["E4", "G4", "B4", "D5"], ["E4", "G4", "C#5"],
              ["F4", "A4", "C5", "E5"], ["F4", "B4", "D5"], ["E4", "G4", "B4", "D5"], ["E4", "G4", "B4", "D5"]]
    # Electric-piano-ish: sine + soft harmonic with tremolo
    beat = 60 / bpm
    keys = np.zeros(int(SR * 8 * 4 * beat))
    for i, ch in enumerate(chords):
        for off, length in ((0, 1.4), (1.66, 0.3), (2.5, 1.3)):
            d = length * beat
            trem = 1 + 0.15 * np.sin(2 * np.pi * 4.5 * t_(d))
            tone = sum(osc(note(n), d) + 0.25 * osc(note(n) * 2, d) for n in ch) / len(ch)
            tone = tone * trem * env(d, 0.01, 0.4, 0.5, 0.1)
            s = int(SR * (i * 4 + off) * beat)
            keys[s:s + len(tone)] += tone[:len(keys) - s]
    lead_notes = [("A5", 0.5, 1), ("F5", 1.5, 0.5), ("E5", 2, 1.5), (None, 3.5, 0.5),
                  ("D5", 4.5, 0.5), ("F5", 5, 0.5), ("A5", 5.5, 1), ("G5", 6.5, 1.5),
                  ("E5", 8.5, 1), ("G5", 9.5, 0.5), ("B5", 10, 1.5), ("A5", 11.5, 0.5),
                  ("G5", 12.5, 0.5), ("E5", 13, 1), ("C#5", 14, 1.5),
                  ("D5", 16.5, 1), ("F5", 17.5, 0.5), ("A5", 18, 1.5), ("C6", 19.5, 0.5),
                  ("B5", 20.5, 1), ("G5", 21.5, 0.5), ("F5", 22, 1.5),
                  ("E5", 24.5, 1), ("D5", 25.5, 0.5), ("E5", 26, 1), ("G5", 27, 1),
                  ("E5", 28.5, 2.5)]
    lead = melody(lead_notes, bpm, "tri", 2200)
    dr = drums(8, bpm, 0.17, False) * 0.6
    n = len(dr)
    song = mix(dr, bass[:n] * 0.6, keys[:n] * 0.35, lead[:n] * 0.3)[:n]
    save("music_menu", song, 0.6)


def music_show():
    """In-show loop for loading and results screens: bright, bouncy, a bit of drumroll tension. 8 bars, 112 bpm."""
    bpm = 112
    beat = 60 / bpm
    roots = ["F1", "F1", "D2", "D2", "A#1", "A#1", "C2", "C2"]
    pattern = [(0, 0, 0.4), (0.5, 12, 0.25), (1, 0, 0.4), (1.5, 7, 0.25), (2, 0, 0.4), (2.5, 12, 0.25), (3, 7, 0.4), (3.5, 5, 0.25)]
    bass = bass_line(roots, bpm, pattern, "square", 650)
    chords = [["A4", "C5", "F5"], ["A4", "C5", "F5"], ["A4", "D5", "F5"], ["A4", "D5", "F5"],
              ["A#4", "D5", "F5"], ["A#4", "D5", "F5"], ["G4", "C5", "E5"], ["A#4", "C5", "E5"]]
    stabs = chord_stabs(chords, bpm, [0, 1.5, 3], "saw", 2200, 0.4, 0.9)
    bell_notes = [("C6", 0, 0.5), ("A5", 0.75, 0.5), ("F5", 1.5, 1), ("A5", 3, 0.5),
                  ("D6", 8, 0.5), ("A5", 8.75, 0.5), ("F5", 9.5, 1), ("D6", 11, 0.5),
                  ("D6", 16, 0.5), ("A#5", 16.75, 0.5), ("F5", 17.5, 1), ("A#5", 19, 0.5),
                  ("E6", 24, 0.5), ("C6", 24.75, 0.5), ("G5", 25.5, 1), ("A#5", 27, 0.5), ("C6", 28, 2)]
    lead = np.zeros(int(SR * 32 * beat) + 1)
    for n, st, ln in bell_notes:
        b = bell(note(n), ln * beat + 0.4, 5)
        i = int(SR * st * beat)
        seg = b[:len(lead) - i]
        lead[i:i + len(seg)] += seg
    dr = drums(8, bpm, 0.0, True)
    # Snare roll building into the loop point (last two beats)
    roll = np.zeros_like(dr)
    for k in range(16):
        t0 = (30 + k * 0.125) * beat
        hit = bp(noise(0.06), 1200, 7000) * decay(0.06, 40) * (0.3 + 0.7 * k / 15)
        i = int(SR * t0)
        seg = hit[:len(roll) - i]
        roll[i:i + len(seg)] += seg
    n = len(dr)
    song = mix(dr * 0.8, roll[:n] * 0.6, bass[:n] * 0.5, stabs[:n] * 0.28, lead[:n] * 0.3)[:n]
    save("music_show", song, 0.62)


# ---------------------------------------------------------------- mini-games

def minigames():
    # Bomb tick: a dry wooden click with a tiny metal ring
    tk = bp(noise(0.05), 1800, 6000) * decay(0.05, 120) + osc(2400, 0.05) * decay(0.05, 90) * 0.4
    save("tick", tk, 0.7)
    # Red-light zap: buzzy electric crackle sliding down
    d = 0.45
    buzz = osc(sweep(900, 180, d), d, "saw") * (0.6 + 0.4 * np.sign(np.sin(2 * np.pi * 60 * t_(d))))
    crackle = bp(noise(d), 2000, 8000) * (rng.uniform(0, 1, int(SR * d)) > 0.9) * 1.5
    save("zap", drive(mix(buzz * decay(d, 5), crackle * decay(d, 4)), 2.5), 0.8)
    # Balloon pop: a sharp burst and a rubbery slap
    pop = mix(hp(noise(0.08), 900) * decay(0.08, 60) * 1.4, osc(sweep(500, 140, 0.06), 0.06) * decay(0.06, 50) * 0.6)
    save("pop", pop, 0.85)
    # Toilet flush: a roaring swirl that drains away with a gurgle
    d = 1.8
    swirl = sweep_bp(noise(d), 300, 1400, 2.5) * env(d, 0.08, 0.4, 0.8, 0.9)
    gurgle = np.zeros(int(SR * d))
    for _ in range(14):
        st = rng.uniform(0.7, 1.6)
        f0 = rng.uniform(250, 600)
        g = osc(sweep(f0, f0 * 1.8, 0.07), 0.07) * decay(0.07, 30) * 0.5
        i = int(SR * st)
        gurgle[i:i + len(g)] += g[:len(gurgle) - i]
    save("flush", mix(lp(swirl, 2500), gurgle, lp(noise(d), 200) * env(d, 0.05, 0.3, 0.5, 1.0) * 0.6), 0.8)
    # Record scratch (the music stops!)
    d = 0.5
    x = np.linspace(0, 1, int(SR * d))
    rate = np.where(x < 0.45, 1.0 - 2.2 * x, -0.4 + 1.6 * (x - 0.45))
    scr = bp(noise(d), 600, 3200) * (np.abs(rate) + 0.2) + osc(220 + 400 * np.abs(rate), d, "saw") * 0.25
    save("scratch", scr * env(d, 0.005, 0.1, 0.9, 0.12), 0.8)
    # Porta-potty door slam: a hollow plastic thunk with a latch click
    thunk = lp(noise(0.3), 700) * decay(0.3, 18) * 1.4 + osc(sweep(140, 90, 0.3), 0.3) * decay(0.3, 14)
    latch = at(bp(noise(0.02), 2500, 7000) * decay(0.02, 200) * 0.6, 0.12)
    save("slam", mix(thunk, latch), 0.85)


if __name__ == "__main__":
    print("Writing sounds to", os.path.normpath(OUT))
    ui(); show(); weapons(); hits(); movement(); cash(); gadgets(); streaks(); hazards()
    music_game(); music_menu(); music_show()
    heckles()
    minigames()
    print("Done.")
