import argparse, enum, json, numpy as np, librosa

'''
Auto-generates rhythm game beatmaps (note timing data) from a music file using adaptive, musically-informed heuristics.

Features:
- Supports three difficulty levels (EASY, NORMAL, HARD) with configurable note density and rhythmic complexity.
- Uses onset detection and beat tracking (via librosa) to identify musically salient moments.
- Places notes on a dense rhythmic grid (12 tatums per beat), with adaptive selection to match target note density and spacing.
- Applies local median normalization and RMS weighting to favor percussive and prominent events.
- Outputs a JSON file containing note timings and metadata for use in rhythm games.


Technically made via Vibe Coding. Functions Surprisingly well 
(at least for this (My Unity 1 Week GameJam game))


Pair with GenerateBeatmapWindow.cs tool file in Unity, but You can use CLI if you want.
- by August, 2025/08/08
'''
TOOL_VERSION = "auto-map v0.6"
# ---------------- Difficulty: density + spacing per BEAT ----------------
class Difficulty(enum.Enum):
    EASY   = 0
    NORMAL = 1
    HARD   = 2

# target_npb: notes per beat; space_frac: min spacing as fraction of median beat period
DIFF_CFG = {
    Difficulty.EASY:   dict(target_npb=0.50, space_frac=0.60, phases={0, 6}),            # downbeat + half
    Difficulty.NORMAL: dict(target_npb=1.00, space_frac=0.35, phases={0, 3, 6, 9}),      # quarters
    Difficulty.HARD:   dict(target_npb=2.00, space_frac=0.20, phases=set(range(12))),    # all tatums
}

# windows (seconds)
SNAP_WIN_SEC  = 0.040   # snap-to-peak half-window
SCORE_WIN_SEC = 0.040   # local max pooling window

# mild RMS weighting to favor percussive events without killing quiet parts
RMS_EXP = 0.30          # 0 = off, 0.3 is mild & safe

def _window_argmax_time(arr, times, t0, halfw):
    lo = np.searchsorted(times, t0 - halfw, 'left')
    hi = np.searchsorted(times, t0 + halfw, 'right')
    if hi <= lo: return t0
    k = lo + np.argmax(arr[lo:hi])
    return times[k]

def _window_max(arr, times, t0, halfw):
    lo = np.searchsorted(times, t0 - halfw, 'left')
    hi = np.searchsorted(times, t0 + halfw, 'right')
    if hi <= lo: return 0.0
    return float(np.max(arr[lo:hi]))

def _phase_weight(phase_idx, tatum=12):
    x = 1.0 + 0.5*np.cos(2*np.pi*phase_idx/tatum)   # smooth beat accent
    if phase_idx % tatum == 0:       x *= 1.4       # downbeat
    elif phase_idx % (tatum//2) == 0:x *= 1.2       # half
    elif phase_idx % (tatum//4) == 0:x *= 1.1       # quarter
    return x

def _build_tatum_grid(beat_times, tatum=12):
    times, phases = [], []
    for b1, b2 in zip(beat_times[:-1], beat_times[1:]):
        step = (b2 - b1) / tatum
        slots = b1 + step * np.arange(tatum)
        times.append(slots)
        phases.append(np.arange(tatum, dtype=int))
    if len(beat_times):
        times.append(np.array([beat_times[-1]])); phases.append(np.array([0], dtype=int))
    return np.concatenate(times), np.concatenate(phases)

import numpy as np
from numpy.lib import stride_tricks

def _sliding_window_1d(a, w):
    # fallback for older NumPy that lacks sliding_window_view
    if w > a.size:
        # trivial: pad to ensure at least one window
        pad = (w - a.size + 1) // 2
        ap = np.pad(a, (pad, pad), mode='edge')
        a = ap
    shape = (a.size - w + 1, w)
    strides = (a.strides[0], a.strides[0])
    return stride_tricks.as_strided(a, shape=shape, strides=strides)

def _local_median_norm(x, win_frames):
    win = max(1, int(round(win_frames)))
    if win % 2 == 0:
        win += 1
    if len(x) == 0:
        return x

    pad = win // 2
    xpad = np.pad(x, (pad, pad), mode='edge')

    # try modern API
    sw = None
    try:
        from numpy.lib.stride_tricks import sliding_window_view
        # positional arg to support older numpy signatures
        sw = sliding_window_view(xpad, win)     # shape: (len(x), win)
    except Exception:
        sw = _sliding_window_1d(xpad, win)      # fallback

    # If a fallback produced more than len(x) rows, crop to len(x)
    if sw.shape[0] != len(x):
        sw = sw[:len(x), :]

    med = np.median(sw, axis=-1)
    return x / (1e-6 + med)



def _compute_scores(y, sr):
    # Onset envelope (0..1) with time axis
    onset = librosa.onset.onset_strength(y=y, sr=sr)
    onset = onset / (np.max(onset) + 1e-9)
    t_env = librosa.frames_to_time(np.arange(len(onset)), sr=sr)

    # Local median normalization (~0.5 s) for section robustness
    hop = 512
    med_win = 0.5 * sr / hop
    novelty = _local_median_norm(onset, med_win)
    novelty = np.clip(novelty, 0, None)

    # Mild RMS up-weighting
    if RMS_EXP > 1e-9:
        rms = librosa.feature.rms(y=y, frame_length=2048, hop_length=hop, center=True)[0]
        rms = rms / (np.max(rms) + 1e-9)
        # align RMS timeline to onset timeline
        t_rms = librosa.frames_to_time(np.arange(len(rms)), sr=sr, hop_length=hop)
        rms_interp = np.interp(t_env, t_rms, rms)
        novelty = novelty * np.power(rms_interp, RMS_EXP)

    return novelty, t_env

def _select_with_relaxation(grid_times, grid_phases, raw_score, beat_times, novelty, t_env, diff, diag=False):
    # tempo baselines
    beat_periods = np.diff(beat_times)
    B = float(np.median(beat_periods)) if len(beat_periods) else 0.5
    n_beats = max(len(beat_times) - 1, 1)
    cfg = DIFF_CFG[diff]
    target_n = int(round(cfg["target_npb"] * n_beats))
    min_space = cfg["space_frac"] * B

    base_q = {Difficulty.EASY:0.80, Difficulty.NORMAL:0.65, Difficulty.HARD:0.45}[diff]
    q = base_q
    phases = set(cfg["phases"])   # copy

    expand_order = [0,6,3,9,2,4,8,10,1,5,7,11]  # strong→weak; items already in phases will be skipped
    expand_order = [p for p in expand_order if p not in phases]

    def run_once(phs, quant):
        mask = np.array([p in phs for p in grid_phases])
        t = grid_times[mask]; s = raw_score[mask]
        if t.size == 0: return np.array([]), np.array([])
        thr = np.quantile(s, quant) if len(s) > 8 else 0.0
        keep = s >= thr
        return t[keep], s[keep]

    best_snap = []
    # conservative relaxation: expand phases first, then lower quantile, never shrink spacing below 80% of initial
    min_space_floor = min_space * 0.80

    for step in range(10):
        cand_t, cand_s = run_once(phases, q)

        if cand_t.size:
            order = np.argsort(-cand_s)
            chosen = []
            for i in order:
                ti = cand_t[i]
                if all(abs(ti - tj) >= min_space for tj in chosen):
                    chosen.append(ti)
                    if len(chosen) >= target_n:
                        break

            if chosen:
                snapped = [ _window_argmax_time(novelty, t_env, t, SNAP_WIN_SEC) for t in chosen ]
                snapped.sort()
                best_snap = snapped
                if len(snapped) >= target_n:
                    if diag:
                        print(f"[ok] step={step} count={len(snapped)} target={target_n} q={q:.2f} phases={len(phases)} min_space={min_space:.3f}")
                    return snapped

        # need more: relax carefully (NORMAL/HARD only)
        if diff != Difficulty.EASY:
            if expand_order:
                phases.add(expand_order.pop(0))
            elif q > 0.10:
                q = max(0.10, q - 0.10)
            elif min_space > min_space_floor:
                min_space = max(min_space_floor, min_space * 0.95)
            else:
                break
        else:
            break

    if diag:
        print(f"[end] count={len(best_snap)} target={target_n} q={q:.2f} phases={len(phases)} min_space={min_space:.3f}")
    return best_snap

def analyse(path: str, diff: Difficulty, diagnostics=False):
    # 1) load
    y, sr = librosa.load(path, sr=None)

    # 2) tempo + beats
    tempo, beat_frames = librosa.beat.beat_track(y=y, sr=sr, units='frames')
    beat_times = librosa.frames_to_time(beat_frames, sr=sr)

    if len(beat_times) < 2:
        dur = librosa.get_duration(y=y, sr=sr)
        return [] if dur == 0 else [{"hitTime": 0.0, "type": 0, "spawnPointIndex": -1}]

    # 3) scoring signals
    novelty, t_env = _compute_scores(y, sr)

    # 4) dense 12-tatum grid
    tatum = 12
    grid_times, grid_phases = _build_tatum_grid(beat_times, tatum=tatum)

    # 5) per-slot score = local max novelty × phase accent
    local_max = np.array([_window_max(novelty, t_env, t, SCORE_WIN_SEC) for t in grid_times])
    phase_w   = np.array([_phase_weight(p, tatum=tatum) for p in grid_phases])
    raw_score = local_max * phase_w

    # 6) selection with safe relaxation
    snapped = _select_with_relaxation(grid_times, grid_phases, raw_score, beat_times, novelty, t_env, diff, diag=diagnostics)

    return [{"hitTime": round(float(t), 6), "type": 0, "spawnPointIndex": -1} for t in snapped]

# -------------------------------- CLI ------------------------------------
if __name__ == "__main__":
    print(f"[tool] {TOOL_VERSION}")
    ap = argparse.ArgumentParser(description="Auto-generate beatmap JSON (safe adaptive)")
    ap.add_argument("in_wav")
    ap.add_argument("out_json")
    ap.add_argument("--difficulty", choices=["EASY","NORMAL","HARD"], default="NORMAL")
    ap.add_argument("--approach", type=float, default=1.0, help="approachTime in seconds")
    ap.add_argument("--diag", action="store_true", help="print diagnostics")
    args = ap.parse_args()

    diff = Difficulty[args.difficulty]
    notes = analyse(args.in_wav, diff, diagnostics=args.diag)

    bm = {"musicTrack": args.in_wav, "approachTime": args.approach, "notes": notes}
    with open(args.out_json, "w", encoding="utf-8") as f:
        json.dump(bm, f, indent=2)

    dur = librosa.get_duration(path=args.in_wav)
    nps = len(notes) / max(dur, 1e-9)
    print(f"Generated {len(notes)} notes ({diff.name}, {nps:.2f} notes/sec, total={len(notes)})")
    print(f"[tool] using script at: {__file__}")
    print(f"[tool] difficulty arg: {args.difficulty}")
    print(f"[tool] DIFF_CFG: {DIFF_CFG[Difficulty[args.difficulty]]}")

    


