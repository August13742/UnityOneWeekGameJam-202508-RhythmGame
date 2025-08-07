import argparse, enum, json, numpy as np, librosa

class Difficulty(enum.Enum):
    EASY   = 2  
    NORMAL = 2  # eighth notes
    HARD   = 4  # sixteenth notes

# Minimum onset-strength (normalised 0-1) required for a note to survive
ENERGY_GATE = {
    Difficulty.EASY:   0.2,
    Difficulty.NORMAL: 0.15,
    Difficulty.HARD:   0.2,
}

def analyse(path: str, diff: Difficulty):
    # ── 1. load
    y, sr = librosa.load(path, sr=None)

    # ── 2. global tempo + beat positions
    tempo, beat_frames = librosa.beat.beat_track(y=y, sr=sr, units='frames')
    beat_times = librosa.frames_to_time(beat_frames, sr=sr)

    # ── 3. onset envelope (normalised)
    onset_env = librosa.onset.onset_strength(y=y, sr=sr)
    onset_env /= np.max(onset_env) + 1e-9

    # helper: fast lookup of envelope value at arbitrary seconds
    onset_time = librosa.frames_to_time(np.arange(len(onset_env)), sr=sr)
    strength_at = np.interp  # linear interpolation alias

    # ── 4. build subdivision grid
    subdiv = diff.value            #
    grid_times = []
    for b1, b2 in zip(beat_times[:-1], beat_times[1:]):
        step = (b2 - b1) / subdiv
        grid_times.extend(b1 + step * np.arange(subdiv))
    grid_times.append(beat_times[-1])               # include last downbeat
    grid_times = np.array(grid_times)

    # ── 5. energy gating
    min_e = ENERGY_GATE[diff]
    keep_mask = strength_at(grid_times, onset_time, onset_env) >= min_e
    kept = grid_times[keep_mask]

    # ── 6. emit
    return [
        {"hitTime": round(float(t), 6), "type": 0, "spawnPointIndex": -1}
        for t in kept
    ]

# ──────────────────────────────────────────────
if __name__ == "__main__":
    ap = argparse.ArgumentParser(description="Auto-generate beatmap JSON")
    ap.add_argument("in_wav")
    ap.add_argument("out_json")
    ap.add_argument("--difficulty",
                    choices=["EASY", "NORMAL", "HARD"],
                    default="NORMAL")
    ap.add_argument("--approach", type=float, default=1.0,
                    help="approachTime to write into JSON (sec)")
    args = ap.parse_args()

    diff = Difficulty[args.difficulty]
    notes = analyse(args.in_wav, diff)

    bm = {
        "musicTrack": args.in_wav,
        "approachTime": args.approach,
        "notes": notes
    }
    with open(args.out_json, "w", encoding="utf-8") as f:
        json.dump(bm, f, indent=2)

    print(f"Generated {len(notes)} notes "
          f"({diff.name}, tempo≈{len(notes)/ (librosa.get_duration(filename=args.in_wav)/60):.0f} notes/min)")
