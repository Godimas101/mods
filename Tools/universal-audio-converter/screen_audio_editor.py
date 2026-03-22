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
  Extract      — keep the active channel as mono (toggle L or R button to choose)
  Solo         — mute the inactive channel (selection or whole file; toggle L or R to choose)

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
WAVEFORM_COLOR   = T.MUTED
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

def op_solo_channel(samples, channel: int, sel_start: int, sel_end: int):
    """Mute the other channel within [sel_start:sel_end] (whole file if no selection)."""
    if samples.shape[1] < 2:
        return samples
    out   = samples.copy()
    other = 1 - channel
    if sel_end > sel_start:
        out[sel_start:sel_end, other] = 0.0
    else:
        out[:, other] = 0.0
    return out


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

        self._sel_start_px  = 0    # selection in viewport pixels
        self._sel_end_px    = 0
        self._dragging      = None  # "start", "end", "region", or None
        self._drag_origin   = 0

        self._playhead_px   = -1
        self._ch_active     = [True, True]   # [L_active, R_active]

        self._zoom          = 1.0  # 1.0 = all frames visible
        self._view_start    = 0.0  # fraction of total duration at left edge

        self.bind("<Configure>",       self._on_resize)
        self.bind("<ButtonPress-1>",   self._on_press)
        self.bind("<B1-Motion>",       self._on_drag)
        self.bind("<ButtonRelease-1>", self._on_release)
        self.bind("<Motion>",          self._on_hover)

        # Callbacks
        self.on_selection_change = None   # called with (start_frame, end_frame)
        self.on_zoom_change      = None   # called with (zoom, view_start)

    # -----------------------------------------------------------------------

    def load(self, samples: "np.ndarray") -> None:
        self._samples  = samples
        self._n_frames = len(samples)
        self._sel_start_px = 0
        self._sel_end_px   = max(0, self.winfo_width() - 1)
        self._playhead_px  = -1
        self._zoom         = 1.0
        self._view_start   = 0.0
        self._redraw()

    def clear(self) -> None:
        self._samples  = None
        self._n_frames = 0
        self.delete("all")
        ch = max(WAVEFORM_HEIGHT, self.winfo_height())
        self.create_text(self.winfo_width() // 2, ch // 2,
                         text="No file loaded — open a WAV file to begin",
                         fill=T.MUTED, font=("Courier New", 9))

    def set_playhead(self, frame: int) -> None:
        if self._n_frames <= 0:
            return
        visible = max(1, int(self._n_frames / self._zoom))
        start_f = int(self._view_start * self._n_frames)
        self._playhead_px = int((frame - start_f) / visible * self._width)
        self._redraw()

    def get_selection_frames(self) -> tuple[int, int]:
        if self._n_frames <= 0:
            return 0, 0
        w       = max(1, self._width)
        visible = max(1, int(self._n_frames / self._zoom))
        start_f = int(self._view_start * self._n_frames)
        s = max(0, min(start_f + int(self._sel_start_px / w * visible), self._n_frames))
        e = max(0, min(start_f + int(self._sel_end_px   / w * visible), self._n_frames))
        return min(s, e), max(s, e)

    def select_all(self) -> None:
        self._sel_start_px = 0
        self._sel_end_px   = self._width
        self._redraw()
        self._fire_selection()

    def clear_selection(self) -> None:
        self._sel_start_px = 0
        self._sel_end_px   = 0
        self._redraw()
        self._fire_selection()

    def set_channel_active(self, active: list) -> None:
        self._ch_active = list(active)
        self._redraw()

    def set_selection_frames(self, start: int, end: int) -> None:
        if self._n_frames <= 0:
            return
        w       = max(1, self._width)
        visible = max(1, int(self._n_frames / self._zoom))
        start_f = int(self._view_start * self._n_frames)
        self._sel_start_px = int((start - start_f) / visible * w)
        self._sel_end_px   = int((end   - start_f) / visible * w)
        self._redraw()
        self._fire_selection()

    def set_zoom(self, zoom: float, view_start: float = None) -> None:
        # Preserve selection in frame-space so it represents the same time range
        sel_start_f, sel_end_f = self.get_selection_frames()

        self._zoom = max(1.0, min(float(zoom), 64.0))
        max_start  = max(0.0, 1.0 - 1.0 / self._zoom)
        if view_start is not None:
            self._view_start = max(0.0, min(float(view_start), max_start))
        else:
            self._view_start = max(0.0, min(self._view_start, max_start))

        # Re-map selection pixels to the new zoom/scroll coordinates
        if self._n_frames > 0:
            w       = max(1, self._width)
            visible = max(1, int(self._n_frames / self._zoom))
            start_f = int(self._view_start * self._n_frames)
            self._sel_start_px = int((sel_start_f - start_f) / visible * w)
            self._sel_end_px   = int((sel_end_f   - start_f) / visible * w)

        self._redraw()
        if self.on_zoom_change:
            self.on_zoom_change(self._zoom, self._view_start)

    def get_zoom(self) -> tuple:
        return self._zoom, self._view_start

    def xview(self, *args) -> None:
        """Scrollbar-compatible xview for ttk.Scrollbar integration."""
        # Preserve selection in frame-space so scrolling doesn't shift the time range
        sel_start_f, sel_end_f = self.get_selection_frames()

        max_start = max(0.0, 1.0 - 1.0 / self._zoom)
        if args[0] == "moveto":
            self._view_start = max(0.0, min(float(args[1]), max_start))
        elif args[0] == "scroll":
            step = (1.0 / self._zoom) * (0.1 if args[2] == "units" else 1.0)
            self._view_start = max(0.0, min(self._view_start + int(args[1]) * step, max_start))

        # Re-map selection pixels to the new scroll position
        if self._n_frames > 0:
            w       = max(1, self._width)
            visible = max(1, int(self._n_frames / self._zoom))
            start_f = int(self._view_start * self._n_frames)
            self._sel_start_px = int((sel_start_f - start_f) / visible * w)
            self._sel_end_px   = int((sel_end_f   - start_f) / visible * w)

        self._redraw()
        if self.on_zoom_change:
            self.on_zoom_change(self._zoom, self._view_start)

    # -----------------------------------------------------------------------

    def _on_resize(self, event) -> None:
        new_w = max(1, event.width)
        if self._width > 0 and self._width != new_w:
            # Proportionally rescale selection so it survives transient
            # intermediate resize events (e.g. when the channel btn frame
            # is packed/unpacked and tkinter fires multiple Configure events)
            scale = new_w / self._width
            self._sel_start_px = min(new_w, round(self._sel_start_px * scale))
            self._sel_end_px   = min(new_w, round(self._sel_end_px   * scale))
        self._width = new_w
        self._redraw()

    def _on_hover(self, event) -> None:
        HANDLE_TOL = 8
        x = event.x
        if (abs(x - self._sel_start_px) < HANDLE_TOL or
                abs(x - self._sel_end_px) < HANDLE_TOL):
            self.config(cursor="sb_h_double_arrow")
        else:
            self.config(cursor="crosshair")

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
        h = max(WAVEFORM_HEIGHT, self.winfo_height())

        if self._samples is None or self._n_frames == 0:
            self.create_text(w // 2, h // 2,
                             text="No file loaded",
                             fill=T.MUTED, font=("Courier New", 9))
            return

        # Compute visible frame range based on zoom/scroll
        visible    = max(1, int(self._n_frames / self._zoom))
        start_frame = int(self._view_start * self._n_frames)
        end_frame   = min(self._n_frames, start_frame + visible)

        n_ch   = self._samples.shape[1] if self._samples.ndim == 2 else 1
        bucket = max(1, (end_frame - start_frame) // max(1, w))

        sx = min(self._sel_start_px, self._sel_end_px)
        ex = max(self._sel_start_px, self._sel_end_px)

        if n_ch == 2:
            # Stereo: R on top half, L on bottom half
            slot = h // 2
            for ch_idx, y_off in [(1, 0), (0, slot)]:
                active  = self._ch_active[ch_idx]
                cy      = y_off + slot // 2
                half    = slot // 2
                ch_data = self._samples[:, ch_idx]
                self.create_line(0, cy, w, cy, fill=T.BORDER)
                # Tint only on active channels
                if ex > sx and active:
                    self.create_rectangle(sx, y_off, ex, y_off + slot,
                                          fill=T.ORANGE, stipple="gray25",
                                          outline="", tags="sel")
                pts_grey = []; pts_sel = []
                for px in range(w):
                    lo    = start_frame + px * bucket
                    hi    = min(lo + bucket, end_frame)
                    chunk = ch_data[lo:hi]
                    if len(chunk) == 0:
                        continue
                    y_top = cy - int(float(np.max(chunk)) * half)
                    y_bot = cy - int(float(np.min(chunk)) * half)
                    pt = [px, y_top, px, y_bot]
                    if active and sx < ex and sx <= px < ex:
                        pts_sel  += pt
                    else:
                        pts_grey += pt
                if pts_grey: self.create_line(pts_grey, fill=WAVEFORM_COLOR, width=1)
                if pts_sel:  self.create_line(pts_sel,  fill=T.ORANGE,       width=1)
            # Divider between R and L
            self.create_line(0, slot, w, slot, fill=T.BORDER)
        else:
            # Mono: single waveform across full height
            mono = self._samples[:, 0]
            cy   = h // 2
            self.create_line(0, cy, w, cy, fill=T.BORDER)
            if ex > sx:
                self.create_rectangle(sx, 0, ex, h,
                                      fill=T.ORANGE, stipple="gray25",
                                      outline="", tags="sel")
            pts_left = []; pts_sel = []; pts_right = []
            for px in range(w):
                lo    = start_frame + px * bucket
                hi    = min(lo + bucket, end_frame)
                chunk = mono[lo:hi]
                if len(chunk) == 0:
                    continue
                y_top = cy - int(float(np.max(chunk)) * cy)
                y_bot = cy - int(float(np.min(chunk)) * cy)
                pt = [px, y_top, px, y_bot]
                if sx < ex and sx <= px < ex:
                    pts_sel  += pt
                elif px < sx:
                    pts_left += pt
                else:
                    pts_right += pt
            if pts_left:  self.create_line(pts_left,  fill=WAVEFORM_COLOR, width=1)
            if pts_sel:   self.create_line(pts_sel,   fill=T.ORANGE,       width=1)
            if pts_right: self.create_line(pts_right, fill=WAVEFORM_COLOR, width=1)

        # Selection handles and playhead span full height
        for hx in (sx, ex):
            self.create_line(hx, 0, hx, h, fill=HANDLE_COLOR, width=2)

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
        self._ref_window              = None

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

        self._file_label_var = tk.StringVar(value="No file loaded")
        ttk.Label(file_bar, textvariable=self._file_label_var,
                  style="Muted.TLabel").pack(side="right")

        # ── Waveform canvas ───────────────────────────────────────────────────
        canvas_frame = tk.Frame(self, bg=T.BG)
        canvas_frame.pack(fill="both", expand=True, padx=16, pady=(0, 4))

        # Channel toggle buttons — shown for stereo, hidden for mono
        BTN_W = 32
        self._ch_btn_frame = tk.Frame(canvas_frame, bg=T.PANEL, width=BTN_W)
        self._ch_btn_frame.pack_propagate(False)
        # (packed/forgotten in _update_channel_ui — not packed here)

        _btn_kw = dict(font=("Courier New", 10, "bold"), bd=0, relief="flat",
                       cursor="hand2", activeforeground=T.TEXT)
        self._btn_ch_r = tk.Button(self._ch_btn_frame, text="R",
                                   command=lambda: self._toggle_channel(1), **_btn_kw)
        self._btn_ch_r.pack(fill="both", expand=True)
        self._btn_ch_l = tk.Button(self._ch_btn_frame, text="L",
                                   command=lambda: self._toggle_channel(0), **_btn_kw)
        self._btn_ch_l.pack(fill="both", expand=True)
        self._ch_active = [True, True]   # [L_active, R_active]

        self._waveform = WaveformCanvas(canvas_frame, height=WAVEFORM_HEIGHT)
        self._waveform.pack(side="left", fill="both", expand=True)
        self._waveform.on_selection_change = self._on_selection_change
        self._waveform.clear()

        # File / selection info bar
        info_bar = ttk.Frame(self, style="TFrame")
        info_bar.pack(fill="x", padx=16, pady=(2, 0))

        self._info_var = tk.StringVar(value="")
        ttk.Label(info_bar, textvariable=self._info_var,
                  style="Muted.TLabel").pack(side="left")

        self._sel_var = tk.StringVar(value="")
        ttk.Label(info_bar, textvariable=self._sel_var,
                  style="Muted.TLabel").pack(side="right")

        # Zoom controls + horizontal scroll bar
        zoom_bar = ttk.Frame(self, style="TFrame")
        zoom_bar.pack(fill="x", padx=16, pady=(2, 0))

        zoom_btns = ttk.Frame(zoom_bar, style="TFrame")
        zoom_btns.pack(side="left")
        ttk.Label(zoom_btns, text="ZOOM:", style="Muted.TLabel").pack(side="left", padx=(0, 4))
        self._se_btn(zoom_btns, "−", self._zoom_out, width=2).pack(side="left", padx=(0, 2))
        self._se_btn(zoom_btns, "+", self._zoom_in,  width=2).pack(side="left", padx=(0, 2))
        self._se_btn(zoom_btns, "RESET", self._zoom_reset).pack(side="left", padx=(0, 8))

        self._h_scroll = ttk.Scrollbar(zoom_bar, orient="horizontal",
                                       command=self._waveform.xview,
                                       style="SE.Horizontal.TScrollbar")
        self._h_scroll.pack(side="left", fill="x", expand=True)
        self._h_scroll.set(0.0, 1.0)
        self._waveform.on_zoom_change = self._on_waveform_zoom_change

        T.separator(self, pady=(6, 6))

        # ── Playback bar ──────────────────────────────────────────────────────
        if _HAS_SD:
            play_bar = ttk.Frame(self, style="TFrame")
            play_bar.pack(fill="x", padx=16, pady=(0, 0))

            self._btn_play = self._se_btn(play_bar, "\u25b6  PLAY",  self._on_play)
            self._btn_play.pack(side="left", padx=(0, 6))
            self._se_btn(play_bar, "\u25a0  STOP", self._on_stop).pack(side="left", padx=(0, 16))
            self._se_btn(play_bar, "|\u25b6|  PLAY SELECTION", self._on_play_selection).pack(side="left", padx=(0, 6))
            self._se_btn(play_bar, "|\u229e|  SELECT ALL", self._on_select_all).pack(side="left", padx=(0, 6))
            self._se_btn(play_bar, "|\u2715|  CLEAR SELECTION", self._on_clear_selection).pack(side="left")

            self._playback_var = tk.StringVar(value="")
            ttk.Label(play_bar, textvariable=self._playback_var,
                      style="Muted.TLabel").pack(side="right")

            T.separator(self, pady=(6, 6))

        # ── Edit toolbar (two rows) ───────────────────────────────────────────
        self._build_edit_toolbar()

        T.separator(self, pady=(6, 6))

        # ── Log ───────────────────────────────────────────────────────────────
        log_frame = ttk.Frame(self, style="Panel.TFrame")
        log_frame.pack(fill="x", padx=16, pady=(0, 14))

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
        self._log_text.config(height=5)
        log_sb = ttk.Scrollbar(log_frame, orient="vertical",
                               command=self._log_text.yview,
                               style="SE.Vertical.TScrollbar")
        self._log_text.config(yscrollcommand=log_sb.set)
        log_sb.pack(side="right", fill="y")
        self._log_text.pack(fill="both", expand=True, padx=6, pady=(0, 6))

    def _build_edit_toolbar(self):
        toolbar = tk.Frame(self, bg=T.BG)
        toolbar.pack(fill="x", padx=16, pady=(0, 0))

        # Info button — far right, top-anchored to sit level with the Clip row
        ttk.Button(toolbar, text="\u24d8",
                   command=self._on_edit_info,
                   style="Info.TButton").pack(side="right", anchor="n", pady=2)

        grid = tk.Frame(toolbar, bg=T.BG)
        grid.pack(side="left", fill="x", expand=True)
        grid.columnconfigure(0, minsize=80)   # label column — fixed width
        grid.columnconfigure(1, weight=1)     # buttons column — fills remaining

        def _lbl(row, text):
            tk.Label(grid, text=text,
                     bg=T.BG, fg=T.MUTED,
                     font=("Courier New", 7, "bold"),
                     anchor="e").grid(row=row, column=0,
                                      sticky="e", padx=(0, 8), pady=2)

        def _btn_cell(row):
            cell = tk.Frame(grid, bg=T.BG)
            cell.grid(row=row, column=1, sticky="w", pady=2)
            return cell

        # ── Clip ──────────────────────────────────────────────────────────────
        _lbl(0, "CLIP")
        cell = _btn_cell(0)
        self._se_btn(cell, "\u2702 TRIM",    self._op_trim).pack(side="left", padx=(0, 4))
        self._se_btn(cell, "\u2298 SILENCE", self._op_silence).pack(side="left")

        # ── Transform ─────────────────────────────────────────────────────────
        _lbl(1, "TRANSFORM")
        cell = _btn_cell(1)
        self._se_btn(cell, "\u21c4 REVERSE",   self._op_reverse).pack(side="left", padx=(0, 4))
        self._se_btn(cell, "\u25b2 NORMALIZE", self._op_normalize).pack(side="left", padx=(0, 4))
        self._se_btn(cell, "\u2248 DC OFFSET", self._op_dc_offset).pack(side="left")

        # ── Volume ────────────────────────────────────────────────────────────
        _lbl(2, "VOLUME")
        cell = _btn_cell(2)
        ttk.Label(cell, text="Gain:", style="TLabel").pack(side="left")
        self._gain_var = tk.StringVar(value="1.0")
        tk.Entry(cell, textvariable=self._gain_var,
                 bg=T.PANEL, fg=T.TEXT, insertbackground=T.CYAN,
                 font=("Courier New", 9), relief="flat",
                 highlightthickness=1, highlightbackground=T.BORDER,
                 highlightcolor=T.CYAN, width=6).pack(side="left", padx=(4, 4), ipady=2)
        self._se_btn(cell, "\u2713 APPLY", self._op_gain).pack(side="left")

        # ── Fades ─────────────────────────────────────────────────────────────
        _lbl(3, "FADES")
        cell = _btn_cell(3)
        self._se_btn(cell, "\u2197 FADE IN",  self._op_fade_in).pack(side="left", padx=(0, 4))
        self._se_btn(cell, "\u2198 FADE OUT", self._op_fade_out).pack(side="left")

        # ── Speed ─────────────────────────────────────────────────────────────
        _lbl(4, "SPEED")
        cell = _btn_cell(4)
        self._speed_var = tk.StringVar(value="1.25\u00d7")
        ttk.Combobox(cell, textvariable=self._speed_var,
                     values=SPEED_OPTIONS,
                     state="readonly", width=7,
                     style="SE.TCombobox").pack(side="left", padx=(0, 4))
        self._se_btn(cell, "\u2713 APPLY", self._op_speed).pack(side="left")

        # ── Channels ──────────────────────────────────────────────────────────
        _lbl(5, "CHANNELS")
        cell = _btn_cell(5)
        self._se_btn(cell, "\u2295 MONO\u2192STEREO", self._op_mono_to_stereo).pack(side="left", padx=(0, 4))
        self._se_btn(cell, "\u2296 STEREO\u2192MONO", self._op_stereo_to_mono).pack(side="left", padx=(0, 4))
        self._btn_swap    = self._se_btn(cell, "\u2194 SWAP L/R", self._op_swap)
        self._btn_swap.pack(side="left", padx=(0, 4))
        self._btn_extract = self._se_btn(cell, "\u229f EXTRACT", self._op_extract_active)
        self._btn_extract.pack(side="left", padx=(0, 4))
        self._btn_solo    = self._se_btn(cell, "\u25ce SOLO", self._op_solo_active)
        self._btn_solo.pack(side="left", padx=(0, 4))

    # -----------------------------------------------------------------------
    # Widget helpers
    # -----------------------------------------------------------------------

    def _zoom_in(self):
        z, vs = self._waveform.get_zoom()
        self._waveform.set_zoom(z * 2.0)

    def _zoom_out(self):
        z, vs = self._waveform.get_zoom()
        self._waveform.set_zoom(max(1.0, z / 2.0))

    def _zoom_reset(self):
        self._waveform.set_zoom(1.0, 0.0)

    def _on_waveform_zoom_change(self, zoom, view_start):
        if zoom <= 1.0:
            self._h_scroll.set(0.0, 1.0)
        else:
            self._h_scroll.set(view_start, view_start + 1.0 / zoom)

    def _on_edit_info(self):
        if self._ref_window and self._ref_window.winfo_exists():
            self._ref_window.lift()
        else:
            self._ref_window = T.AudioEditorReferenceWindow(self.winfo_toplevel())

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
            self._sel_end     = 0
            self._waveform.load(samples)
            self._waveform.clear_selection()
            self._h_scroll.set(0.0, 1.0)
            self._update_channel_ui(samples.shape[1], reset_channels=True)
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

    # -----------------------------------------------------------------------
    # Channel toggle buttons
    # -----------------------------------------------------------------------

    def _toggle_channel(self, ch_idx: int) -> None:
        self._ch_active[ch_idx] = not self._ch_active[ch_idx]
        self._update_ch_buttons()
        self._waveform.set_channel_active(self._ch_active)

    def _update_ch_buttons(self) -> None:
        for btn, ch_idx in [(self._btn_ch_r, 1), (self._btn_ch_l, 0)]:
            on = self._ch_active[ch_idx]
            btn.config(
                bg=T.ORANGE  if on else T.PANEL,
                fg=T.BG      if on else T.MUTED,
                activebackground=T.ORANGE if on else T.PANEL,
            )

    def _update_channel_ui(self, n_ch: int, reset_channels: bool = False) -> None:
        """Show/hide channel toggle buttons and enable/disable stereo-only ops."""
        if n_ch == 2:
            if reset_channels:
                self._ch_active = [True, True]
            self._ch_btn_frame.pack(side="left", fill="y",
                                    before=self._waveform)
            self._update_ch_buttons()
        else:
            self._ch_btn_frame.pack_forget()
        self._waveform.set_channel_active(self._ch_active)
        state = "normal" if n_ch == 2 else "disabled"
        for btn in (self._btn_swap, self._btn_extract, self._btn_solo):
            btn.config(state=state)

    def _on_select_all(self):
        if self._samples is None:
            return
        self._sel_start = 0
        self._sel_end   = len(self._samples)
        self._waveform.select_all()
        self._update_info()

    def _on_clear_selection(self):
        self._sel_start = 0
        self._sel_end   = 0
        self._waveform.clear_selection()
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
        prev_zoom, prev_view_start = self._waveform.get_zoom()
        prev_ch    = self._samples.shape[1] if self._samples is not None else 0
        prev_start = self._sel_start
        prev_end   = self._sel_end
        self._samples, self._sample_rate = self._history.pop()
        new_len = len(self._samples)
        self._waveform.load(self._samples)
        if prev_zoom > 1.0:
            self._waveform.set_zoom(prev_zoom, prev_view_start)
        # Restore selection clamped to the restored length
        new_start = min(prev_start, new_len)
        new_end   = min(prev_end,   new_len)
        if new_end > new_start:
            self._sel_start = new_start
            self._sel_end   = new_end
            self._waveform.set_selection_frames(new_start, new_end)
        else:
            self._sel_start = 0
            self._sel_end   = 0
            self._waveform.clear_selection()
        self._update_channel_ui(self._samples.shape[1],
                                reset_channels=(self._samples.shape[1] != prev_ch))
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
        prev_start = self._sel_start
        prev_end   = self._sel_end
        prev_len   = len(self._samples) if self._samples is not None else 0
        prev_ch    = self._samples.shape[1] if self._samples is not None else 0
        had_selection = prev_end > prev_start

        # Save zoom — load() unconditionally resets it to 1.0
        prev_zoom, prev_view_start = self._waveform.get_zoom()

        self._push_history()
        self._samples   = new_samples
        new_len = len(new_samples)
        self._sel_start = 0
        self._sel_end   = 0
        self._waveform.load(new_samples)

        # Restore zoom after load() reset it
        if prev_zoom > 1.0:
            self._waveform.set_zoom(prev_zoom, prev_view_start)

        if had_selection and new_len == prev_len:
            # Length unchanged — restore clamped selection
            new_start = min(prev_start, new_len)
            new_end   = min(prev_end,   new_len)
            if new_end > new_start:
                self._sel_start = new_start
                self._sel_end   = new_end
                self._waveform.set_selection_frames(new_start, new_end)
            else:
                self._waveform.clear_selection()
        else:
            self._waveform.clear_selection()

        self._update_channel_ui(new_samples.shape[1],
                                reset_channels=(new_samples.shape[1] != prev_ch))
        self._update_info()
        self._log(f"\u2713 {label}", "success")

    def _ch_mask(self) -> list:
        """Active channel indices. Returns [0] for mono; subset of [0,1] for stereo."""
        if self._samples is None or self._samples.shape[1] < 2:
            return [0]
        return [ch for ch in range(2) if self._ch_active[ch]]

    def _require_active_channel(self) -> bool:
        """Return False (with warning) if stereo and both channels are deactivated."""
        if (self._samples is not None and
                self._samples.shape[1] == 2 and
                not any(self._ch_active)):
            self._log("No channels active — toggle L or R first.", "warn")
            return False
        return True

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
        if not self._require_loaded() or not self._require_active_channel():
            return
        s, e = self._sel_start, self._sel_end
        if e <= s:
            self._log("Make a selection first.", "warn")
            return
        ln   = e - s
        out  = self._samples.copy()
        ramp = np.linspace(0.0, 1.0, ln, dtype=np.float32)
        for ch in self._ch_mask():
            out[s:e, ch] *= ramp
        self._apply(out, f"Fade In on selection ({ln/self._sample_rate:.3f}s)")

    def _op_fade_out(self):
        if not self._require_loaded() or not self._require_active_channel():
            return
        s, e = self._sel_start, self._sel_end
        if e <= s:
            self._log("Make a selection first.", "warn")
            return
        ln   = e - s
        out  = self._samples.copy()
        ramp = np.linspace(1.0, 0.0, ln, dtype=np.float32)
        for ch in self._ch_mask():
            out[s:e, ch] *= ramp
        self._apply(out, f"Fade Out on selection ({ln/self._sample_rate:.3f}s)")

    def _op_normalize(self):
        if not self._require_loaded() or not self._require_active_channel():
            return
        out = self._samples.copy()
        for ch in self._ch_mask():
            peak = np.max(np.abs(out[:, ch]))
            if peak > 1e-9:
                out[:, ch] = np.clip(out[:, ch] / peak, -1.0, 1.0)
        self._apply(out, "Normalize to 0 dBFS")

    def _op_gain(self):
        if not self._require_loaded() or not self._require_active_channel():
            return
        try:
            factor = float(self._gain_var.get())
        except ValueError:
            self._log("Invalid gain value.", "error")
            return
        s, e = self._sel_start, self._sel_end
        out  = self._samples.copy()
        if e > s:
            for ch in self._ch_mask():
                out[s:e, ch] = np.clip(out[s:e, ch] * factor, -1.0, 1.0)
            self._apply(out, f"Gain ×{factor} on selection ({(e-s)/self._sample_rate:.3f}s)")
        else:
            for ch in self._ch_mask():
                out[:, ch] = np.clip(out[:, ch] * factor, -1.0, 1.0)
            self._apply(out, f"Gain ×{factor} (whole file)")

    def _op_silence(self):
        if not self._require_loaded() or not self._require_active_channel():
            return
        s, e = self._sel_start, self._sel_end
        if e <= s:
            self._log("Make a selection first.", "warn")
            return
        out  = self._samples.copy()
        for ch in self._ch_mask():
            out[s:e, ch] = 0.0
        self._apply(out, f"Silence selection ({(e-s)/self._sample_rate:.3f}s)")

    def _op_reverse(self):
        if not self._require_loaded() or not self._require_active_channel():
            return
        s, e = self._sel_start, self._sel_end
        if e <= s:
            self._log("Make a selection first.", "warn")
            return
        out  = self._samples.copy()
        for ch in self._ch_mask():
            out[s:e, ch] = self._samples[s:e, ch][::-1]
        self._apply(out, f"Reverse selection ({(e-s)/self._sample_rate:.3f}s)")

    def _op_dc_offset(self):
        if not self._require_loaded() or not self._require_active_channel():
            return
        ch_names = ["L", "R"]
        reports = []
        for ch in self._ch_mask():
            mean = float(np.mean(self._samples[:, ch]))
            label = ch_names[ch] if self._samples.shape[1] == 2 else "mono"
            reports.append((ch, mean, label))
        offset_str = ", ".join(f"{lbl}={m:+.4f}" for _, m, lbl in reports)
        if all(abs(m) < 0.0001 for _, m, _ in reports):
            self._log(f"DC offset: no significant offset detected ({offset_str})", "info")
            return
        out = self._samples.copy()
        for ch, mean, _ in reports:
            out[:, ch] = np.clip(self._samples[:, ch] - mean, -1.0, 1.0)
        self._apply(out, f"DC Offset removal ({offset_str})")

    def _op_speed(self):
        if not self._require_loaded():
            return
        factor = SPEED_MAP.get(self._speed_var.get(), 1.25)
        result = op_speed(self._samples, factor, self._sample_rate)
        self._apply(result, f"Speed ×{factor}  (new duration: {len(result)/self._sample_rate:.2f}s)")

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

    def _op_solo(self, channel: int):
        if not self._require_loaded():
            return
        if self._samples.shape[1] < 2:
            self._log("Need stereo for Solo.", "warn")
            return
        label = "L" if channel == 0 else "R"
        s, e  = self._sel_start, self._sel_end
        scope = f" on selection ({(e-s)/self._sample_rate:.3f}s)" if e > s else " (whole file)"
        self._apply(op_solo_channel(self._samples, channel, s, e),
                    f"Solo {label}{scope}")

    def _op_extract_active(self):
        if not self._require_loaded():
            return
        if self._samples.shape[1] < 2:
            self._log("File is already mono — nothing to extract.", "warn")
            return
        l_on, r_on = self._ch_active[0], self._ch_active[1]
        if l_on and not r_on:
            self._op_extract(0)
        elif r_on and not l_on:
            self._op_extract(1)
        elif not l_on and not r_on:
            self._log("No channel selected — toggle L or R first.", "warn")
        else:
            self._log("Both channels active — deselect one to extract a single channel.", "warn")

    def _op_solo_active(self):
        if not self._require_loaded():
            return
        if self._samples.shape[1] < 2:
            self._log("File is mono — nothing to solo.", "warn")
            return
        l_on, r_on = self._ch_active[0], self._ch_active[1]
        if l_on and not r_on:
            self._op_solo(0)
        elif r_on and not l_on:
            self._op_solo(1)
        elif not l_on and not r_on:
            self._log("No channel selected — toggle L or R first.", "warn")
        else:
            self._log("Both channels active — deselect one to target a single channel.", "warn")


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
