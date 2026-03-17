#!/usr/bin/env python3
"""
screen_audio_editor.py — WAV Audio Editor for SE Audio Converter.

Opens a WAV file, displays a waveform on a tkinter Canvas, and provides
non-destructive editing operations via numpy.

Edit operations (all numpy-only, no scipy):
  Trim         — discard audio outside the selection handles
  Fade In      — linear ramp up from silence at the start of selection
  Fade Out     — linear ramp down to silence at the end of selection
  Normalize    — scale peak to 0 dBFS (or a user target)
  Volume       — multiply by a constant gain factor
  Silence      — replace selection with silence
  Reverse      — flip selected region backwards
  DC Offset    — subtract mean (fixes DC bias recording issues)
  Speed        — resample to change duration (0.5× – 2×) via np.interp
  Mono→Stereo  — duplicate single channel to L and R
  Stereo→Mono  — average L and R channels
  Swap         — swap left and right channels
  Extract L/R  — keep only one channel as mono

Playback via sounddevice (optional — editor is fully functional without it).
"""

import struct
import wave as wave_module
from pathlib import Path

import tkinter as tk
from tkinter import filedialog, ttk

import se_audio_theme as T

# ---------------------------------------------------------------------------
# Optional dependencies
# ---------------------------------------------------------------------------
try:
    import numpy as np
    _HAS_NUMPY = True
except ImportError:
    _HAS_NUMPY = False

try:
    import sounddevice as _sd
    _HAS_SD = True
except Exception:
    _HAS_SD = False

# ---------------------------------------------------------------------------
# Constants
# ---------------------------------------------------------------------------
WAVEFORM_HEIGHT  = 160       # canvas height in pixels
WAVEFORM_BG      = T.PANEL
WAVEFORM_COLOR   = T.CYAN
SELECTION_COLOR  = "#ff8c0033"   # semi-transparent orange (drawn as overlay)
PLAYHEAD_COLOR   = T.TEXT
HANDLE_COLOR     = T.ORANGE

SPEED_OPTIONS    = ["0.50×", "0.75×", "1.25×", "1.50×", "2.00×"]
SPEED_MAP        = {
    "0.50×": 0.50, "0.75×": 0.75,
    "1.25×": 1.25, "1.50×": 1.50, "2.00×": 2.00,
}


# ---------------------------------------------------------------------------
# WAV helpers
# ---------------------------------------------------------------------------

def load_wav(path: Path) -> tuple:
    """
    Load a WAV file.
    Returns (samples: np.ndarray shape [frames, channels], sample_rate: int).
    samples dtype is float32 in range [-1, 1].
    """
    if not _HAS_NUMPY:
        raise RuntimeError("numpy is required for the Audio Editor.")
    with wave_module.open(str(path), "rb") as w:
        n_channels  = w.getnchannels()
        sample_rate = w.getframerate()
        n_frames    = w.getnframes()
        samp_width  = w.getsampwidth()
        raw         = w.readframes(n_frames)

    if samp_width == 1:
        dtype = np.uint8
        scale = 128.0
        offset = -1.0
    elif samp_width == 2:
        dtype = np.int16
        scale = 32768.0
        offset = 0.0
    elif samp_width == 3:
        # 24-bit: unpack manually
        raw_arr = np.frombuffer(raw, dtype=np.uint8).reshape(-1, 3)
        raw32   = (raw_arr[:, 2].astype(np.int32) << 16 |
                   raw_arr[:, 1].astype(np.int32) << 8  |
                   raw_arr[:, 0].astype(np.int32))
        raw32[raw32 >= 2**23] -= 2**24
        samples = raw32.astype(np.float32) / (2**23)
        return samples.reshape(-1, n_channels), sample_rate
    elif samp_width == 4:
        dtype = np.int32
        scale = 2147483648.0
        offset = 0.0
    else:
        raise RuntimeError(f"Unsupported sample width: {samp_width} bytes")

    samples = np.frombuffer(raw, dtype=dtype).astype(np.float32)
    samples = samples / scale + offset
    return samples.reshape(-1, n_channels), sample_rate


def save_wav(path: Path, samples: "np.ndarray", sample_rate: int) -> None:
    """Save float32 [-1,1] samples as 16-bit PCM WAV."""
    clipped = np.clip(samples, -1.0, 1.0)
    pcm     = (clipped * 32767).astype(np.int16)
    n_channels = pcm.shape[1] if pcm.ndim == 2 else 1
    raw = pcm.tobytes()
    with wave_module.open(str(path), "wb") as w:
        w.setnchannels(n_channels)
        w.setsampwidth(2)
        w.setframerate(sample_rate)
        w.writeframes(raw)


# ---------------------------------------------------------------------------
# DSP operations  (all numpy-only)
# ---------------------------------------------------------------------------

def op_trim(samples, sel_start, sel_end):
    return samples[sel_start:sel_end]

def op_fade_in(samples, sel_start, sel_end):
    out   = samples.copy()
    ln    = sel_end - sel_start
    ramp  = np.linspace(0.0, 1.0, ln, dtype=np.float32).reshape(-1, 1)
    out[sel_start:sel_end] *= ramp
    return out

def op_fade_out(samples, sel_start, sel_end):
    out   = samples.copy()
    ln    = sel_end - sel_start
    ramp  = np.linspace(1.0, 0.0, ln, dtype=np.float32).reshape(-1, 1)
    out[sel_start:sel_end] *= ramp
    return out

def op_normalize(samples, target_db=0.0):
    peak = np.max(np.abs(samples))
    if peak < 1e-9:
        return samples
    target_linear = 10 ** (target_db / 20.0)
    return np.clip(samples * (target_linear / peak), -1.0, 1.0)

def op_gain(samples, factor):
    return np.clip(samples * factor, -1.0, 1.0)

def op_silence(samples, sel_start, sel_end):
    out = samples.copy()
    out[sel_start:sel_end] = 0.0
    return out

def op_reverse(samples, sel_start, sel_end):
    out = samples.copy()
    out[sel_start:sel_end] = samples[sel_start:sel_end][::-1]
    return out

def op_dc_offset(samples):
    return samples - np.mean(samples, axis=0)

def op_speed(samples, factor, sample_rate):
    """Resample to change playback speed. factor > 1 = faster, < 1 = slower."""
    n_in  = len(samples)
    n_out = max(1, int(n_in / factor))
    x_in  = np.arange(n_in,  dtype=np.float64)
    x_out = np.linspace(0, n_in - 1, n_out, dtype=np.float64)
    n_ch  = samples.shape[1] if samples.ndim == 2 else 1
    if n_ch == 1:
        return np.interp(x_out, x_in, samples[:, 0]).astype(np.float32).reshape(-1, 1)
    result = np.empty((n_out, n_ch), dtype=np.float32)
    for c in range(n_ch):
        result[:, c] = np.interp(x_out, x_in, samples[:, c])
    return result

def op_mono_to_stereo(samples):
    if samples.shape[1] == 2:
        return samples
    return np.hstack([samples, samples])

def op_stereo_to_mono(samples):
    if samples.shape[1] == 1:
        return samples
    return np.mean(samples, axis=1, keepdims=True).astype(np.float32)

def op_swap_channels(samples):
    if samples.shape[1] < 2:
        return samples
    out = samples.copy()
    out[:, 0] = samples[:, 1]
    out[:, 1] = samples[:, 0]
    return out

def op_extract_channel(samples, channel: int):
    ch = min(channel, samples.shape[1] - 1)
    return samples[:, ch:ch+1].copy()


# ---------------------------------------------------------------------------
# Waveform Canvas
# ---------------------------------------------------------------------------

class WaveformCanvas(tk.Canvas):
    """
    Canvas that draws a stereo/mono waveform, a draggable selection region,
    and an optional playhead cursor.
    """

    def __init__(self, parent, height=WAVEFORM_HEIGHT, **kw):
        super().__init__(parent,
                         bg=WAVEFORM_BG,
                         height=height,
                         highlightthickness=1,
                         highlightbackground=T.BORDER,
                         cursor="crosshair",
                         **kw)
        self._samples    = None   # np.ndarray [N, ch]
        self._n_frames   = 0
        self._width      = 1

        self._sel_start_px  = 0    # selection in pixels
        self._sel_end_px    = 0
        self._dragging      = None  # "start", "end", "region", or None
        self._drag_origin   = 0

        self._playhead_px   = -1

        self.bind("<Configure>",       self._on_resize)
        self.bind("<ButtonPress-1>",   self._on_press)
        self.bind("<B1-Motion>",       self._on_drag)
        self.bind("<ButtonRelease-1>", self._on_release)

        # Callbacks
        self.on_selection_change = None   # called with (start_frame, end_frame)

    # -----------------------------------------------------------------------

    def load(self, samples: "np.ndarray") -> None:
        self._samples  = samples
        self._n_frames = len(samples)
        self._sel_start_px = 0
        self._sel_end_px   = max(0, self.winfo_width() - 1)
        self._playhead_px  = -1
        self._redraw()

    def clear(self) -> None:
        self._samples  = None
        self._n_frames = 0
        self.delete("all")
        self.create_text(self.winfo_width() // 2, WAVEFORM_HEIGHT // 2,
                         text="No file loaded — open a WAV file to begin",
                         fill=T.MUTED, font=("Courier New", 9))

    def set_playhead(self, frame: int) -> None:
        if self._n_frames <= 0:
            return
        self._playhead_px = int(frame / self._n_frames * self._width)
        self._redraw()

    def get_selection_frames(self) -> tuple[int, int]:
        if self._n_frames <= 0:
            return 0, 0
        w   = max(1, self._width)
        s   = int(self._sel_start_px / w * self._n_frames)
        e   = int(self._sel_end_px   / w * self._n_frames)
        return min(s, e), max(s, e)

    def select_all(self) -> None:
        self._sel_start_px = 0
        self._sel_end_px   = self._width
        self._redraw()
        self._fire_selection()

    # -----------------------------------------------------------------------

    def _on_resize(self, event) -> None:
        self._width = event.width
        if self._sel_end_px == 0 or self._sel_end_px > self._width:
            self._sel_end_px = self._width
        self._redraw()

    def _on_press(self, event) -> None:
        x = event.x
        HANDLE_TOL = 8
        if abs(x - self._sel_start_px) < HANDLE_TOL:
            self._dragging    = "start"
        elif abs(x - self._sel_end_px) < HANDLE_TOL:
            self._dragging    = "end"
        elif self._sel_start_px < x < self._sel_end_px:
            self._dragging    = "region"
            self._drag_origin = x
        else:
            self._dragging       = "new"
            self._sel_start_px   = x
            self._sel_end_px     = x

    def _on_drag(self, event) -> None:
        x = max(0, min(event.x, self._width))
        if self._dragging == "start":
            self._sel_start_px = x
        elif self._dragging == "end":
            self._sel_end_px = x
        elif self._dragging == "region":
            delta = x - self._drag_origin
            self._drag_origin  = x
            span = self._sel_end_px - self._sel_start_px
            self._sel_start_px = max(0, self._sel_start_px + delta)
            self._sel_end_px   = min(self._width, self._sel_start_px + span)
        elif self._dragging == "new":
            self._sel_end_px = x
        self._redraw()

    def _on_release(self, _event) -> None:
        # Normalise so start < end
        if self._sel_start_px > self._sel_end_px:
            self._sel_start_px, self._sel_end_px = \
                self._sel_end_px, self._sel_start_px
        self._dragging = None
        self._fire_selection()

    def _fire_selection(self) -> None:
        if self.on_selection_change:
            self.on_selection_change(*self.get_selection_frames())

    # -----------------------------------------------------------------------

    def _redraw(self) -> None:
        self.delete("all")
        w = self._width
        h = WAVEFORM_HEIGHT

        if self._samples is None or self._n_frames == 0:
            self.create_text(w // 2, h // 2,
                             text="No file loaded",
                             fill=T.MUTED, font=("Courier New", 9))
            return

        # Centre line
        self.create_line(0, h // 2, w, h // 2, fill=T.BORDER)

        # Waveform — downsample to canvas pixel width
        n_ch   = self._samples.shape[1] if self._samples.ndim == 2 else 1
        mono   = np.mean(self._samples, axis=1) if n_ch > 1 else self._samples[:, 0]
        bucket = max(1, self._n_frames // w)

        pts = []
        for px in range(w):
            lo = px * bucket
            hi = min(lo + bucket, self._n_frames)
            chunk = mono[lo:hi]
            if len(chunk) == 0:
                continue
            peak_pos = float(np.max(chunk))
            peak_neg = float(np.min(chunk))
            cy       = h // 2
            y_top    = cy - int(peak_pos * cy)
            y_bot    = cy - int(peak_neg * cy)
            pts += [px, y_top, px, y_bot]

        if pts:
            self.create_line(pts, fill=WAVEFORM_COLOR, width=1)

        # Selection overlay
        sx = min(self._sel_start_px, self._sel_end_px)
        ex = max(self._sel_start_px, self._sel_end_px)
        if ex > sx:
            self.create_rectangle(sx, 0, ex, h,
                                  fill=T.BLUE, stipple="gray25",
                                  outline="", tags="sel")
        # Selection handles
        for hx in (sx, ex):
            self.create_line(hx, 0, hx, h, fill=HANDLE_COLOR, width=2)

        # Playhead
        if 0 <= self._playhead_px <= w:
            self.create_line(self._playhead_px, 0,
                             self._playhead_px, h,
                             fill=PLAYHEAD_COLOR, width=1, dash=(4, 2))


# ---------------------------------------------------------------------------
# Editor Screen
# ---------------------------------------------------------------------------

class EditorScreen(ttk.Frame):

    def __init__(self, parent, app):
        super().__init__(parent, style="TFrame")
        self._app = app

        self._path: Path | None       = None
        self._samples: "np.ndarray | None" = None
        self._sample_rate: int        = 44100
        self._history: list           = []   # undo stack
        self._sel_start: int          = 0
        self._sel_end:   int          = 0
        self._playing:   bool         = False
        self._play_start_frame: int   = 0
        self._play_start_time:  float = 0.0  # time.time() at play start
        self._play_region_frames: int = 0    # length of playing region

        self._build_ui()

        if not _HAS_NUMPY:
            self._log("numpy not installed \u2014 install it to use the Audio Editor.", "error")
            self._log("Run:  pip install numpy sounddevice", "warn")
        if not _HAS_SD:
            self._log("sounddevice not installed \u2014 playback unavailable.", "warn")

    # -----------------------------------------------------------------------
    # UI
    # -----------------------------------------------------------------------

    def _build_ui(self):
        T.build_header(
            self,
            title="AUDIO EDITOR",
            subtitle="Open a WAV file, edit it, and save.",
            back_cb=self._on_back,
        )
        T.separator(self, pady=(8, 6))

        # ── File bar ─────────────────────────────────────────────────────────
        file_bar = ttk.Frame(self, style="TFrame")
        file_bar.pack(fill="x", padx=16, pady=(0, 6))

        self._se_btn(file_bar, "OPEN WAV", self._on_open).pack(side="left", padx=(0, 8))
        self._se_btn(file_bar, "SAVE", self._on_save).pack(side="left", padx=(0, 4))
        self._se_btn(file_bar, "SAVE AS...", self._on_save_as).pack(side="left", padx=(0, 16))
        self._se_btn(file_bar, "UNDO", self._on_undo).pack(side="left", padx=(0, 4))
        self._se_btn(file_bar, "SELECT ALL", self._on_select_all).pack(side="left")

        self._file_label_var = tk.StringVar(value="No file loaded")
        ttk.Label(file_bar, textvariable=self._file_label_var,
                  style="Muted.TLabel").pack(side="right")

        # ── Waveform canvas ───────────────────────────────────────────────────
        canvas_frame = tk.Frame(self, bg=T.BG)
        canvas_frame.pack(fill="x", padx=16, pady=(0, 4))

        self._waveform = WaveformCanvas(canvas_frame, height=WAVEFORM_HEIGHT)
        self._waveform.pack(fill="x")
        self._waveform.on_selection_change = self._on_selection_change
        self._waveform.clear()

        # Selection / file info bar
        info_bar = ttk.Frame(self, style="TFrame")
        info_bar.pack(fill="x", padx=16, pady=(2, 0))

        self._info_var = tk.StringVar(value="")
        ttk.Label(info_bar, textvariable=self._info_var,
                  style="Muted.TLabel").pack(side="left")

        self._sel_var = tk.StringVar(value="")
        ttk.Label(info_bar, textvariable=self._sel_var,
                  style="Muted.TLabel").pack(side="right")

        T.separator(self, pady=(6, 6))

        # ── Playback bar ──────────────────────────────────────────────────────
        if _HAS_SD:
            play_bar = ttk.Frame(self, style="TFrame")
            play_bar.pack(fill="x", padx=16, pady=(0, 6))

            self._btn_play = self._se_btn(play_bar, "\u25b6  PLAY",  self._on_play)
            self._btn_play.pack(side="left", padx=(0, 6))
            self._se_btn(play_bar, "\u25a0  STOP", self._on_stop).pack(side="left", padx=(0, 16))
            self._se_btn(play_bar, "PLAY SELECTION", self._on_play_selection).pack(side="left")

            self._playback_var = tk.StringVar(value="")
            ttk.Label(play_bar, textvariable=self._playback_var,
                      style="Muted.TLabel").pack(side="right")

            T.separator(self, pady=(6, 6))

        # ── Edit toolbar (two rows) ───────────────────────────────────────────
        self._build_edit_toolbar()

        T.separator(self, pady=(6, 6))

        # ── Log ───────────────────────────────────────────────────────────────
        log_frame = ttk.Frame(self, style="Panel.TFrame")
        log_frame.pack(fill="both", expand=True, padx=16, pady=(0, 14))

        log_hdr = ttk.Frame(log_frame, style="Panel.TFrame")
        log_hdr.pack(fill="x", padx=6, pady=(4, 2))
        ttk.Label(log_hdr, text="\u25a3  EDIT LOG",
                  style="Section.TLabel", background=T.PANEL).pack(side="left")
        tk.Button(log_hdr, text="CLEAR",
                  command=self._clear_log,
                  bg=T.PANEL, fg=T.MUTED,
                  activebackground=T.HOVER, activeforeground=T.TEXT,
                  font=("Courier New", 8), relief="flat", bd=0,
                  cursor="hand2").pack(side="right")

        self._log_text = T.log_text_widget(log_frame)
        log_sb = ttk.Scrollbar(log_frame, orient="vertical",
                               command=self._log_text.yview,
                               style="SE.Vertical.TScrollbar")
        self._log_text.config(yscrollcommand=log_sb.set)
        log_sb.pack(side="right", fill="y")
        self._log_text.pack(fill="both", expand=True, padx=6, pady=(0, 6))

    def _build_edit_toolbar(self):
        # Row 1 — clip & volume
        row1 = ttk.Frame(self, style="TFrame")
        row1.pack(fill="x", padx=16, pady=(0, 4))

        self._se_btn(row1, "TRIM TO SELECTION", self._op_trim).pack(side="left", padx=(0, 6))
        self._se_btn(row1, "SILENCE",           self._op_silence).pack(side="left", padx=(0, 6))
        self._se_btn(row1, "REVERSE",           self._op_reverse).pack(side="left", padx=(0, 6))
        self._se_btn(row1, "NORMALIZE",         self._op_normalize).pack(side="left", padx=(0, 6))
        self._se_btn(row1, "DC OFFSET",         self._op_dc_offset).pack(side="left", padx=(0, 16))

        ttk.Label(row1, text="Gain:", style="TLabel").pack(side="left")
        self._gain_var = tk.StringVar(value="1.0")
        tk.Entry(row1, textvariable=self._gain_var,
                 bg=T.PANEL, fg=T.TEXT, insertbackground=T.CYAN,
                 font=("Courier New", 9), relief="flat",
                 highlightthickness=1, highlightbackground=T.BORDER,
                 highlightcolor=T.CYAN, width=6).pack(side="left", padx=(4, 4), ipady=2)
        self._se_btn(row1, "APPLY", self._op_gain).pack(side="left")

        # Row 2 — fades, speed, channels
        row2 = ttk.Frame(self, style="TFrame")
        row2.pack(fill="x", padx=16, pady=(0, 4))

        self._se_btn(row2, "FADE IN",  self._op_fade_in).pack(side="left", padx=(0, 6))
        self._se_btn(row2, "FADE OUT", self._op_fade_out).pack(side="left", padx=(0, 16))

        ttk.Label(row2, text="Speed:", style="TLabel").pack(side="left")
        self._speed_var = tk.StringVar(value="1.25×")
        ttk.Combobox(row2, textvariable=self._speed_var,
                     values=SPEED_OPTIONS,
                     state="readonly", width=7,
                     style="SE.TCombobox").pack(side="left", padx=(4, 4))
        self._se_btn(row2, "APPLY", self._op_speed).pack(side="left", padx=(0, 16))

        ttk.Label(row2, text="Channels:", style="TLabel").pack(side="left")
        for label, cmd in [
            ("MONO\u2192STEREO", self._op_mono_to_stereo),
            ("STEREO\u2192MONO", self._op_stereo_to_mono),
            ("SWAP L/R",         self._op_swap),
            ("EXTRACT L",        lambda: self._op_extract(0)),
            ("EXTRACT R",        lambda: self._op_extract(1)),
        ]:
            self._se_btn(row2, label, cmd).pack(side="left", padx=(4, 0))

    # -----------------------------------------------------------------------
    # Widget helpers
    # -----------------------------------------------------------------------

    def _se_btn(self, parent, text, cmd, width=None):
        kw = {"width": width} if width else {}
        return ttk.Button(parent, text=text, command=cmd,
                          style="SE.TButton", **kw)

    def _log(self, msg, tag="info"):
        T.append_log(self._log_text, msg, tag)

    def _log_sep(self):
        self._log("\u2501" * 38, "sep")

    def _clear_log(self):
        self._log_text.config(state="normal")
        self._log_text.delete("1.0", "end")
        self._log_text.config(state="disabled")

    def _update_info(self):
        if self._samples is None:
            self._info_var.set("")
            self._sel_var.set("")
            return
        n  = len(self._samples)
        sr = self._sample_rate
        ch = self._samples.shape[1] if self._samples.ndim == 2 else 1
        dur = n / sr
        ch_label = "Stereo" if ch == 2 else "Mono"
        self._info_var.set(
            f"{self._path.name if self._path else ''}  \u2014  "
            f"{dur:.2f}s  \u00b7  {sr} Hz  \u00b7  {ch_label}")
        # Selection
        ss, se = self._sel_start, self._sel_end
        sel_dur = (se - ss) / sr
        self._sel_var.set(
            f"Selection: {ss/sr:.3f}s \u2013 {se/sr:.3f}s  ({sel_dur:.3f}s)")

    # -----------------------------------------------------------------------
    # File operations
    # -----------------------------------------------------------------------

    def _on_back(self):
        self._on_stop()
        self._app.show_screen("home")

    def _on_open(self):
        path = filedialog.askopenfilename(
            title="Open WAV file",
            filetypes=[("WAV files", "*.wav"), ("All files", "*.*")],
        )
        if not path:
            return
        self._load_file(Path(path))

    def _load_file(self, path: Path):
        if not _HAS_NUMPY:
            T.themed_showinfo(self.winfo_toplevel(), "numpy Missing",
                              "Install numpy to use the Audio Editor:\n\npip install numpy sounddevice")
            return
        try:
            samples, sr = load_wav(path)
            self._path        = path
            self._samples     = samples
            self._sample_rate = sr
            self._history.clear()
            self._sel_start   = 0
            self._sel_end     = len(samples)
            self._waveform.load(samples)
            self._waveform.select_all()
            self._file_label_var.set(path.name)
            self._update_info()
            self._log_sep()
            self._log(f"Opened: {path.name}", "cyan")
            ch = samples.shape[1] if samples.ndim == 2 else 1
            self._log(f"  {len(samples)/sr:.2f}s  \u00b7  {sr} Hz  \u00b7  "
                      f"{'Stereo' if ch == 2 else 'Mono'}  \u00b7  "
                      f"{len(samples)} frames", "muted")
        except Exception as exc:
            T.themed_showinfo(self.winfo_toplevel(), "Load Error", str(exc))

    def _on_save(self):
        if self._samples is None or self._path is None:
            return
        try:
            save_wav(self._path, self._samples, self._sample_rate)
            self._log(f"Saved: {self._path.name}", "success")
        except Exception as exc:
            T.themed_showinfo(self.winfo_toplevel(), "Save Error", str(exc))

    def _on_save_as(self):
        if self._samples is None:
            return
        path = filedialog.asksaveasfilename(
            title="Save WAV as",
            defaultextension=".wav",
            initialfile=self._path.stem + "_edited.wav" if self._path else "output.wav",
            filetypes=[("WAV files", "*.wav"), ("All files", "*.*")],
        )
        if not path:
            return
        try:
            save_wav(Path(path), self._samples, self._sample_rate)
            self._path = Path(path)
            self._file_label_var.set(self._path.name)
            self._log(f"Saved as: {self._path.name}", "success")
        except Exception as exc:
            T.themed_showinfo(self.winfo_toplevel(), "Save Error", str(exc))

    # -----------------------------------------------------------------------
    # Selection
    # -----------------------------------------------------------------------

    def _on_selection_change(self, start_frame, end_frame):
        self._sel_start = start_frame
        self._sel_end   = end_frame
        self._update_info()

    def _on_select_all(self):
        if self._samples is None:
            return
        self._sel_start = 0
        self._sel_end   = len(self._samples)
        self._waveform.select_all()
        self._update_info()

    # -----------------------------------------------------------------------
    # Undo
    # -----------------------------------------------------------------------

    def _push_history(self):
        if self._samples is not None:
            self._history.append((self._samples.copy(), self._sample_rate))
            if len(self._history) > 20:
                self._history.pop(0)

    def _on_undo(self):
        if not self._history:
            self._log("Nothing to undo.", "warn")
            return
        self._samples, self._sample_rate = self._history.pop()
        self._waveform.load(self._samples)
        self._sel_start = 0
        self._sel_end   = len(self._samples)
        self._waveform.select_all()
        self._update_info()
        self._log("Undo.", "muted")

    # -----------------------------------------------------------------------
    # Playback
    # -----------------------------------------------------------------------

    def _on_play(self):
        self._play_region(0, len(self._samples) if self._samples is not None else 0)

    def _on_play_selection(self):
        self._play_region(self._sel_start, self._sel_end)

    def _play_region(self, start, end):
        if not _HAS_SD or self._samples is None:
            return
        import time
        region = self._samples[start:end]
        if len(region) == 0:
            return
        self._on_stop()
        try:
            _sd.play(region, self._sample_rate)
            self._playing              = True
            self._play_start_frame     = start
            self._play_start_time      = time.time()
            self._play_region_frames   = len(region)
            self._tick_playhead()
        except Exception as exc:
            self._log(f"Playback error: {exc}", "error")

    def _on_stop(self):
        if _HAS_SD:
            try:
                _sd.stop()
            except Exception:
                pass
        self._playing = False
        self._waveform.set_playhead(-1)

    def _tick_playhead(self):
        import time
        if not self._playing:
            return
        elapsed_s      = time.time() - self._play_start_time
        elapsed_frames = int(elapsed_s * self._sample_rate)
        if elapsed_frames >= self._play_region_frames:
            self._playing = False
            self._waveform.set_playhead(-1)
            return
        self._waveform.set_playhead(self._play_start_frame + elapsed_frames)
        self.after(33, self._tick_playhead)

    # -----------------------------------------------------------------------
    # Edit operations
    # -----------------------------------------------------------------------

    def _require_loaded(self) -> bool:
        if self._samples is None:
            self._log("Open a WAV file first.", "warn")
            return False
        return True

    def _apply(self, new_samples, label: str):
        self._push_history()
        self._samples   = new_samples
        self._sel_start = 0
        self._sel_end   = len(new_samples)
        self._waveform.load(new_samples)
        self._waveform.select_all()
        self._update_info()
        self._log(f"\u2713 {label}", "success")

    def _op_trim(self):
        if not self._require_loaded():
            return
        s, e = self._sel_start, self._sel_end
        if e <= s:
            self._log("No selection to trim to.", "warn")
            return
        self._apply(op_trim(self._samples, s, e),
                    f"Trim: kept {s/self._sample_rate:.3f}s \u2013 {e/self._sample_rate:.3f}s")

    def _op_fade_in(self):
        if not self._require_loaded():
            return
        self._apply(op_fade_in(self._samples, self._sel_start, self._sel_end),
                    f"Fade In on selection ({(self._sel_end-self._sel_start)/self._sample_rate:.3f}s)")

    def _op_fade_out(self):
        if not self._require_loaded():
            return
        self._apply(op_fade_out(self._samples, self._sel_start, self._sel_end),
                    f"Fade Out on selection ({(self._sel_end-self._sel_start)/self._sample_rate:.3f}s)")

    def _op_normalize(self):
        if not self._require_loaded():
            return
        self._apply(op_normalize(self._samples), "Normalize to 0 dBFS")

    def _op_gain(self):
        if not self._require_loaded():
            return
        try:
            factor = float(self._gain_var.get())
        except ValueError:
            self._log("Invalid gain value.", "error")
            return
        self._apply(op_gain(self._samples, factor), f"Gain ×{factor}")

    def _op_silence(self):
        if not self._require_loaded():
            return
        self._apply(op_silence(self._samples, self._sel_start, self._sel_end),
                    "Silence selection")

    def _op_reverse(self):
        if not self._require_loaded():
            return
        self._apply(op_reverse(self._samples, self._sel_start, self._sel_end),
                    "Reverse selection")

    def _op_dc_offset(self):
        if not self._require_loaded():
            return
        self._apply(op_dc_offset(self._samples), "DC Offset removal")

    def _op_speed(self):
        if not self._require_loaded():
            return
        factor = SPEED_MAP.get(self._speed_var.get(), 1.25)
        self._apply(op_speed(self._samples, factor, self._sample_rate),
                    f"Speed ×{factor}  (new duration: "
                    f"{len(self._samples)/factor/self._sample_rate:.2f}s)")

    def _op_mono_to_stereo(self):
        if not self._require_loaded():
            return
        if self._samples.shape[1] == 2:
            self._log("Already stereo.", "warn")
            return
        self._apply(op_mono_to_stereo(self._samples), "Mono \u2192 Stereo")

    def _op_stereo_to_mono(self):
        if not self._require_loaded():
            return
        if self._samples.shape[1] == 1:
            self._log("Already mono.", "warn")
            return
        self._apply(op_stereo_to_mono(self._samples), "Stereo \u2192 Mono")

    def _op_swap(self):
        if not self._require_loaded():
            return
        if self._samples.shape[1] < 2:
            self._log("Need stereo to swap channels.", "warn")
            return
        self._apply(op_swap_channels(self._samples), "Swap L/R channels")

    def _op_extract(self, channel: int):
        if not self._require_loaded():
            return
        label = "L" if channel == 0 else "R"
        self._apply(op_extract_channel(self._samples, channel),
                    f"Extract channel {label} as Mono")


# ---------------------------------------------------------------------------
# Playback buffer helper
# ---------------------------------------------------------------------------

def save_wav_to_buffer(buf, samples: "np.ndarray", sample_rate: int) -> None:
    """Write WAV data to a file-like buffer (for pygame in-memory playback)."""
    import io
    clipped    = np.clip(samples, -1.0, 1.0)
    pcm        = (clipped * 32767).astype(np.int16)
    n_channels = pcm.shape[1] if pcm.ndim == 2 else 1
    with wave_module.open(buf, "wb") as w:
        w.setnchannels(n_channels)
        w.setsampwidth(2)
        w.setframerate(sample_rate)
        w.writeframes(pcm.tobytes())
