#!/usr/bin/env python3
"""
screen_text_converter.py — LCD Text Converter screen for SE Tools.

Converts any image to a Space Engineers LCD text string that players can
paste directly into an in-game LCD panel.  No modding tools required.

Uses the SE monospace font's \uE100-\uE1FF coloured-pixel character range
(512 colours, 9-bit palette) with optional dithering.
"""

import queue
import threading
from pathlib import Path

import tkinter as tk
from tkinter import filedialog, ttk

import se_theme as T
import se_text_convert as C
from se_lcd_convert import (
    SCREEN_PRESETS,
    PRESET_DISPLAY_NAMES,
    PRESET_DISPLAY_MAP,
    get_preset,
)


# ---------------------------------------------------------------------------
# "How it works" popup content
# ---------------------------------------------------------------------------

_HOW_TO_APPLY = """\
  1.  Build or find an LCD Panel in-game and open its
      Control Panel  (K by default).

  2.  In the  Content  dropdown, select  Text and Images.

  3.  Click  Edit Text,  select all existing content,
      and paste the string copied from this tool.

  4.  In the  Font  dropdown, select  Monospaced.

  5.  Set  Font Size  to the value shown in the converter
      (usually 0.1 for most panels).

  6.  Set  Text Padding  to  0.

  7.  Close the control panel \u2014 your image should appear.

\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500

TIPS

  \u2022 Larger resolution = more characters = larger paste string.
  \u2022 LCD Panel (178\u00d7178) is the most common target.
  \u2022 Floyd-Steinberg gives the best results for photos.
  \u2022 None dithering is fastest \u2014 great for logos and flat art.

\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500

HOW IT WORKS

  \u2022 SE's monospace font has special characters (U+E100\u2013U+E1FF),
    each rendering as a solid 1-pixel colour block on an LCD.

  \u2022 Every pixel is mapped to SE's 9-bit palette (512 colours \u2014
    8 levels each for R, G, B) and encoded as one character.

  \u2022 Dithering spreads colour rounding errors to neighbouring
    pixels, producing smoother gradients at a small compute cost."""


# ---------------------------------------------------------------------------
# Dithering info popup content
# ---------------------------------------------------------------------------

_DITHERING_INFO = """\
WHAT IS DITHERING?

  SE's LCD palette has only 512 colours (8 shades each for R, G and B).
  Most images contain far more colours than that, so each pixel must be
  rounded to the nearest available colour — which causes banding and
  loss of detail in gradients.

  Dithering distributes the rounding error from each pixel to its
  neighbours, so the eye blends them into smoother tones.  Think of it
  like a newspaper halftone pattern — up close it's dots, but from a
  distance it reads as a gradient.

──────────────────────────────────────

MODES

  None
    No dithering.  Every pixel snaps to the nearest palette colour.
    Fastest conversion.  Best for logos, pixel art, and flat-colour
    images where banding is not a concern.

  Floyd-Steinberg
    The classic algorithm.  Error is spread to 4 neighbours using a
    7-1-5-3 weight pattern.  Excellent all-round choice for photos and
    artwork.  Recommended default.

  Ju-Ji-Ni  (Jarvis-Judice-Ninke)
    Error spreads across 12 neighbours over 3 rows.  Produces a finer,
    more uniform grain than Floyd-Steinberg at the cost of slower
    conversion.  Good for large images with subtle gradients.

  Stucci
    A variant of Ju-Ji-Ni with adjusted weights that adds a slight
    texture to the diffusion pattern.  Reduces the "worm" artefacts
    that can appear in flat areas with Floyd-Steinberg.

  Sierra 3
    Spreads error to 10 neighbours over 3 rows using a softer pattern.
    Sits between Floyd-Steinberg and Ju-Ji-Ni — smoother grain without
    the full compute cost of Ju-Ji-Ni.

  Sierra 2
    A lighter two-row version of Sierra 3.  Faster than Sierra 3 with
    slightly less smoothing.  Good compromise for medium-sized images.

  Sierra Lite
    The lightest Sierra variant — only 3 neighbours.  Nearly as fast
    as None but adds a touch of dithering to reduce harsh banding.
    Good for simple graphics where you want minimal grain.

──────────────────────────────────────

TIPS

  \u2022 Photos and gradients → Floyd-Steinberg or Sierra 3
  \u2022 Logos and flat art   → None
  \u2022 Maximum quality      → Ju-Ji-Ni (slower)
  \u2022 Fastest with grain   → Sierra Lite"""


# ---------------------------------------------------------------------------
# Helpers: convert a ScreenPreset to an SETextSurface-compatible object
# ---------------------------------------------------------------------------

_CUSTOM_FONT_SIZES = ["0.1", "0.2", "0.4", "0.5", "1.0"]


def _preset_to_surface(preset, font_size: float | None = None) -> C.SETextSurface:
    """
    Derive an SETextSurface from a ScreenPreset.

    The preset's dds dimensions act as the texture reference; the formula
    matches what Whip's converter uses for each named LCD block type.
    """
    scale = 512.0 / min(preset.dds_w, preset.dds_h)
    cw = round(preset.surface_w * C.PIXELS_TO_CHARS * scale)
    ch = round(preset.surface_h * C.PIXELS_TO_CHARS * scale)
    fs = font_size if font_size is not None else preset.font_size
    # Build a surface that yields exactly cw × ch chars
    return C.SETextSurface(
        preset.name, "Screen Area",
        512, 512,
        cw / C.PIXELS_TO_CHARS,
        ch / C.PIXELS_TO_CHARS,
        fs,
    )


def _custom_surface(char_w: int, char_h: int, font_size: float) -> C.SETextSurface:
    return C.SETextSurface(
        "Custom", "Custom",
        512, 512,
        char_w / C.PIXELS_TO_CHARS,
        char_h / C.PIXELS_TO_CHARS,
        font_size,
    )


# ---------------------------------------------------------------------------
# Screen
# ---------------------------------------------------------------------------

class TextConverterScreen(ttk.Frame):

    def __init__(self, parent, app):
        super().__init__(parent, style="TFrame")
        self._app = app

        # State
        self._file:       Path | None = None
        self._output_str: str         = ""
        self._converting: bool        = False
        self._q:          queue.Queue = queue.Queue()

        self._surface: C.SETextSurface = _preset_to_surface(SCREEN_PRESETS[0])
        self._preview_photo = None      # tk.PhotoImage kept alive here
        self._ref_window    = None      # LCD reference Toplevel
        self._bg_color      = (0, 0, 0) # background / letterbox fill colour

        self._build_ui()

    # -----------------------------------------------------------------------
    # UI Construction
    # -----------------------------------------------------------------------

    def _build_ui(self):
        pad = dict(padx=16, pady=0)

        T.build_header(
            self,
            title="IMAGE TO LCD",
            subtitle="Convert images to pasteable SE LCD text strings.  No modding required.",
            back_cb=lambda: self._app.show_screen("home"),
        )
        T.separator(self, pady=(8, 8))

        # ── Input Image ──────────────────────────────────────────────────────
        ttk.Label(self, text="\u25a3  INPUT IMAGE",
                  style="Section.TLabel").pack(anchor="w", **pad)

        list_frame = ttk.Frame(self, style="Panel.TFrame")
        list_frame.pack(fill="x", padx=16, pady=(4, 0))

        lb_container = tk.Frame(list_frame, bg=T.PANEL)
        lb_container.pack(side="left", fill="both", expand=True, padx=(6, 0), pady=6)

        self._listbox = tk.Listbox(
            lb_container,
            bg=T.PANEL, fg=T.TEXT,
            selectbackground=T.BLUE, selectforeground=T.TEXT,
            highlightthickness=1, highlightcolor=T.BORDER,
            highlightbackground=T.BORDER,
            relief="flat", bd=0,
            font=("Courier New", 9),
            height=2, activestyle="none",
        )
        self._listbox.pack(side="left", fill="both", expand=True)

        self._list_placeholder = tk.Label(
            lb_container, text="No image selected",
            bg=T.PANEL, fg=T.MUTED, font=("Courier New", 9))
        self._list_placeholder.place(relx=0.5, rely=0.5, anchor="center")

        btn_frame = ttk.Frame(list_frame, style="Panel.TFrame")
        btn_frame.pack(side="right", fill="y", padx=8, pady=6)
        self._se_button(btn_frame, "SELECT", self._on_select, width=8).pack(pady=(0, 6))
        self._se_button(btn_frame, "CLEAR",  self._on_clear,  width=8).pack()

        T.separator(self, pady=(10, 8))

        # ── Output Settings ──────────────────────────────────────────────────
        ttk.Label(self, text="\u25a3  OUTPUT SETTINGS",
                  style="Section.TLabel").pack(anchor="w", **pad)

        sf = ttk.Frame(self, style="TFrame")
        sf.pack(fill="x", padx=16, pady=(6, 0))
        sf.columnconfigure(0, minsize=115)
        sf.columnconfigure(1, weight=1)

        # Row 0: Screen Target
        ttk.Label(sf, text="Screen Target:", style="TLabel").grid(
            row=0, column=0, sticky="w", pady=(0, 6))
        target_ctrl = ttk.Frame(sf, style="TFrame")
        target_ctrl.grid(row=0, column=1, sticky="w", pady=(0, 6))

        self._screen_var = tk.StringVar(value=PRESET_DISPLAY_NAMES[0])
        screen_combo = ttk.Combobox(
            target_ctrl, textvariable=self._screen_var,
            values=PRESET_DISPLAY_NAMES, state="readonly",
            width=30, style="SE.TCombobox", font=T.FONT_LABEL,
        )
        screen_combo.pack(side="left", padx=(0, 6))
        screen_combo.bind("<<ComboboxSelected>>", self._on_screen_change)

        ttk.Button(target_ctrl, text="\u24d8",
                   command=self._on_open_reference,
                   style="Info.TButton").pack(side="left", padx=(0, 8))

        self._font_size_var = tk.StringVar(
            value=f"font size: {SCREEN_PRESETS[0].font_size}")
        ttk.Label(target_ctrl, textvariable=self._font_size_var,
                  style="Muted.TLabel").pack(side="left")

        # Row 1: Custom dimensions (hidden unless "Custom" selected)
        self._custom_row = ttk.Frame(sf, style="TFrame")

        ttk.Label(self._custom_row, text="Char W:", style="TLabel").pack(side="left")
        self._custom_w_var = tk.StringVar(value="178")
        tk.Entry(
            self._custom_row, textvariable=self._custom_w_var,
            bg=T.PANEL, fg=T.TEXT, insertbackground=T.CYAN,
            font=("Courier New", 9), relief="flat", bd=1,
            highlightthickness=1, highlightbackground=T.BORDER,
            highlightcolor=T.CYAN, width=5,
        ).pack(side="left", padx=(4, 12), ipady=3)

        ttk.Label(self._custom_row, text="Char H:", style="TLabel").pack(side="left")
        self._custom_h_var = tk.StringVar(value="178")
        tk.Entry(
            self._custom_row, textvariable=self._custom_h_var,
            bg=T.PANEL, fg=T.TEXT, insertbackground=T.CYAN,
            font=("Courier New", 9), relief="flat", bd=1,
            highlightthickness=1, highlightbackground=T.BORDER,
            highlightcolor=T.CYAN, width=5,
        ).pack(side="left", padx=(4, 12), ipady=3)

        ttk.Label(self._custom_row, text="Font:", style="TLabel").pack(side="left")
        self._custom_font_var = tk.StringVar(value="0.1")
        ttk.Combobox(
            self._custom_row, textvariable=self._custom_font_var,
            values=_CUSTOM_FONT_SIZES, state="readonly",
            width=5, style="SE.TCombobox",
        ).pack(side="left", padx=(4, 0))

        # Row 2: Dithering
        ttk.Label(sf, text="Dithering:", style="TLabel").grid(
            row=2, column=0, sticky="w", pady=(0, 6))
        self._dither_var = tk.StringVar(value="Floyd-Steinberg")
        dither_ctrl = ttk.Frame(sf, style="TFrame")
        dither_ctrl.grid(row=2, column=1, sticky="w", pady=(0, 6))
        ttk.Combobox(
            dither_ctrl, textvariable=self._dither_var,
            values=C.DITHER_MODES, state="readonly",
            width=20, style="SE.TCombobox",
        ).pack(side="left", padx=(0, 6))
        ttk.Button(dither_ctrl, text="\u24d8",
                   command=self._on_dither_info,
                   style="Info.TButton").pack(side="left")

        # Row 3: Options
        ttk.Label(sf, text="Options:", style="TLabel").grid(
            row=3, column=0, sticky="w", pady=(0, 6))
        opts_ctrl = ttk.Frame(sf, style="TFrame")
        opts_ctrl.grid(row=3, column=1, sticky="w", pady=(0, 6))

        self._aspect_var = tk.BooleanVar(value=True)
        ttk.Checkbutton(opts_ctrl, text="Preserve aspect ratio",
                        variable=self._aspect_var,
                        style="SE.TCheckbutton").pack(side="left", padx=(0, 20))

        self._transp_var = tk.BooleanVar(value=False)
        ttk.Checkbutton(opts_ctrl, text="Transparency",
                        variable=self._transp_var,
                        style="SE.TCheckbutton").pack(side="left")
        self._transp_var.trace_add("write", self._on_transp_change)

        # Row 4: Background colour
        ttk.Label(sf, text="Background:", style="TLabel").grid(
            row=4, column=0, sticky="w", pady=(0, 6))
        bg_ctrl = ttk.Frame(sf, style="TFrame")
        bg_ctrl.grid(row=4, column=1, sticky="w", pady=(0, 6))

        self._bg_swatch = tk.Frame(
            bg_ctrl, width=22, height=22,
            bg="#000000", cursor="hand2",
            highlightthickness=1, highlightbackground=T.BORDER,
        )
        self._bg_swatch.pack_propagate(False)
        self._bg_swatch.pack(side="left", padx=(0, 8))
        self._bg_swatch.bind("<Button-1>", lambda _e: self._on_pick_bg())

        self._bg_hex_var = tk.StringVar(value="#000000")
        self._bg_hex_label = ttk.Label(bg_ctrl, textvariable=self._bg_hex_var,
                                       style="Muted.TLabel")
        self._bg_hex_label.pack(side="left")

        T.separator(self, pady=(12, 10))

        # ── Convert Button ────────────────────────────────────────────────────
        convert_frame = ttk.Frame(self, style="TFrame")
        convert_frame.pack(pady=(0, 10))
        self._btn_convert = T.hero_button(
            convert_frame, "  \u25b6  CONVERT TO TEXT  \u25b6  ", self._on_convert)
        self._btn_convert.pack()

        T.separator(self, pady=(10, 8))

        # ── Progress ──────────────────────────────────────────────────────────
        progress_frame = ttk.Frame(self, style="TFrame")
        progress_frame.pack(fill="x", padx=16, pady=(0, 4))

        self._progress_var = tk.DoubleVar(value=0.0)
        ttk.Progressbar(
            progress_frame, variable=self._progress_var,
            maximum=100.0, mode="determinate",
            style="SE.Horizontal.TProgressbar",
        ).pack(side="left", fill="x", expand=True, pady=2)

        self._pct_var = tk.StringVar(value="  0%")
        ttk.Label(progress_frame, textvariable=self._pct_var,
                  style="Muted.TLabel", width=5).pack(side="left", padx=(8, 0))

        self._status_var = tk.StringVar(value="")
        ttk.Label(self, textvariable=self._status_var,
                  style="Muted.TLabel").pack(anchor="w", padx=18, pady=(0, 4))

        # ── Output ────────────────────────────────────────────────────────────
        self._output_frame = ttk.Frame(self, style="TFrame")
        self._output_frame.pack(fill="x", padx=16, pady=(0, 4))

        ttk.Label(self._output_frame, text="\u25a3  OUTPUT",
                  style="Section.TLabel").pack(anchor="w")

        # Preview canvas
        self._preview_canvas = tk.Canvas(
            self._output_frame,
            bg=T.PANEL, bd=0, highlightthickness=1,
            highlightbackground=T.BORDER,
            height=160,
        )
        self._preview_canvas.pack(fill="x", pady=(6, 6))

        # Two equal-width action buttons below the preview
        bottom_row = ttk.Frame(self._output_frame, style="TFrame")
        bottom_row.pack(fill="x", pady=(0, 0))
        bottom_row.columnconfigure(0, weight=1)
        bottom_row.columnconfigure(1, weight=1)

        self._btn_how = T.hero_button(
            bottom_row, "  \u24d8  HOW TO APPLY  ", self._on_how_it_works)
        self._btn_how.grid(row=0, column=0, sticky="ew", padx=(0, 4))

        self._btn_copy = T.hero_button(
            bottom_row, "  \u29c9  COPY TO CLIPBOARD  ", self._on_copy)
        self._btn_copy.grid(row=0, column=1, sticky="ew", padx=(4, 0))
        self._btn_copy.config(state="disabled",
                              highlightbackground=T.BORDER,
                              fg=T.MUTED)

        # Raw text box
        raw_frame = ttk.Frame(self._output_frame, style="Panel.TFrame")
        raw_frame.pack(fill="x", pady=(8, 0))
        ttk.Label(raw_frame, text="\u25a3  RAW STRING  (read-only)",
                  style="Section.TLabel", background=T.PANEL).pack(
                      anchor="w", padx=6, pady=(4, 2))
        self._raw_text = T.log_text_widget(raw_frame)
        self._raw_text.config(height=8)
        raw_sb = ttk.Scrollbar(raw_frame, orient="vertical",
                               command=self._raw_text.yview,
                               style="SE.Vertical.TScrollbar")
        self._raw_text.config(yscrollcommand=raw_sb.set)
        raw_sb.pack(side="right", fill="y")
        self._raw_text.pack(fill="both", expand=True, padx=6, pady=(0, 6))

    # -----------------------------------------------------------------------
    # Widget helpers
    # -----------------------------------------------------------------------

    def _se_button(self, parent, text, command, width=None):
        kw = {"width": width} if width else {}
        return ttk.Button(parent, text=text, command=command,
                          style="SE.TButton", **kw)

    # -----------------------------------------------------------------------
    # Dropdown / settings logic
    # -----------------------------------------------------------------------

    def _on_transp_change(self, *_) -> None:
        """Disable the background colour picker when Transparency is active."""
        enabled = not self._transp_var.get()
        if enabled:
            self._bg_swatch.config(cursor="hand2",
                                   highlightbackground=T.BORDER)
            self._bg_swatch.bind("<Button-1>", lambda _e: self._on_pick_bg())
            self._bg_hex_label.config(foreground=T.MUTED)
        else:
            self._bg_swatch.config(cursor="",
                                   highlightbackground=T.BG)
            self._bg_swatch.bind("<Button-1>", lambda _e: None)
            self._bg_hex_label.config(foreground=T.BORDER)

    def _on_pick_bg(self) -> None:
        from tkinter import colorchooser
        current = "#{:02x}{:02x}{:02x}".format(*self._bg_color)
        result = colorchooser.askcolor(
            color=current, title="Background Color",
            parent=self.winfo_toplevel())
        if result and result[0]:
            r, g, b = (int(x) for x in result[0])
            self._bg_color = (r, g, b)
            hex_str = "#{:02x}{:02x}{:02x}".format(r, g, b)
            self._bg_swatch.config(bg=hex_str)
            self._bg_hex_var.set(hex_str)

    def _on_screen_change(self, _e=None) -> None:
        raw_name = PRESET_DISPLAY_MAP.get(self._screen_var.get(), "Custom")
        if raw_name == "Custom":
            self._custom_row.grid(row=1, column=0, columnspan=2,
                                  sticky="w", padx=(0, 0), pady=(0, 6))
            self._font_size_var.set("")
            self._surface = self._resolve_custom_surface()
        else:
            self._custom_row.grid_remove()
            preset = get_preset(raw_name)
            self._surface = _preset_to_surface(preset)
            self._font_size_var.set(f"font size: {preset.font_size}")

    def _resolve_custom_surface(self) -> C.SETextSurface:
        try:
            cw = max(1, int(self._custom_w_var.get()))
        except ValueError:
            cw = 178
        try:
            ch = max(1, int(self._custom_h_var.get()))
        except ValueError:
            ch = 178
        try:
            fs = float(self._custom_font_var.get())
        except ValueError:
            fs = 0.1
        return _custom_surface(cw, ch, fs)

    # -----------------------------------------------------------------------
    # File list
    # -----------------------------------------------------------------------

    def _on_select(self) -> None:
        path = filedialog.askopenfilename(
            title="Select image",
            filetypes=[
                ("Image files", "*.png *.jpg *.jpeg *.bmp *.gif *.tif *.tiff *.webp"),
                ("All files", "*.*"),
            ],
        )
        if path:
            self._file = Path(path)
            self._refresh_listbox()

    def _on_clear(self) -> None:
        self._file = None
        self._output_str = ""
        self._refresh_listbox()
        self._clear_output()

    def _refresh_listbox(self) -> None:
        self._listbox.delete(0, "end")
        if self._file:
            self._listbox.insert("end", self._file.name)
            self._list_placeholder.place_forget()
        else:
            self._list_placeholder.place(relx=0.5, rely=0.5, anchor="center")

    def _clear_output(self) -> None:
        self._preview_canvas.delete("all")
        self._preview_canvas.config(height=160)
        self._raw_text.config(state="normal")
        self._raw_text.delete("1.0", "end")
        self._raw_text.config(state="disabled")
        self._btn_copy.config(state="disabled",
                              highlightbackground=T.BORDER,
                              fg=T.MUTED,
                              text="  \u29c9  COPY TO CLIPBOARD  ")

    # -----------------------------------------------------------------------
    # Conversion
    # -----------------------------------------------------------------------

    def _on_convert(self) -> None:
        if self._converting:
            return
        if not self._file:
            T.themed_showinfo(self.winfo_toplevel(), "No Image",
                              "Please select an image to convert.")
            return

        # Resolve Custom surface at convert time (catches manual edits)
        raw_name = PRESET_DISPLAY_MAP.get(self._screen_var.get(), "Custom")
        if raw_name == "Custom":
            self._surface = self._resolve_custom_surface()

        self._converting = True
        self._btn_convert.config(state="disabled")
        self._progress_var.set(0)
        self._pct_var.set("  0%")
        self._status_var.set("Converting\u2026")
        self._clear_output()

        t = threading.Thread(target=self._worker, daemon=True)
        t.start()
        self._poll_queue()

    def _worker(self) -> None:
        try:
            output_str, preview_img = C.convert_to_text(
                img_path        = self._file,
                surface         = self._surface,
                dither_mode     = self._dither_var.get(),
                preserve_aspect = self._aspect_var.get(),
                bg_color        = self._bg_color,
                transparency    = self._transp_var.get(),
                progress_cb     = lambda pct: self._q.put(("progress", pct)),
            )
            self._q.put(("done", output_str, preview_img))
        except Exception as exc:
            self._q.put(("error", str(exc)))

    def _poll_queue(self) -> None:
        try:
            while True:
                msg = self._q.get_nowait()
                kind = msg[0]
                if kind == "progress":
                    pct = msg[1]
                    self._progress_var.set(pct)
                    self._pct_var.set(f"{pct:3d}%")
                elif kind == "done":
                    self._on_conversion_done(msg[1], msg[2])
                    return
                elif kind == "error":
                    self._on_conversion_error(msg[1])
                    return
        except queue.Empty:
            pass
        self.after(50, self._poll_queue)

    def _on_conversion_done(self, output_str: str, preview_img) -> None:
        self._output_str  = output_str
        self._converting  = False
        self._progress_var.set(100)
        self._pct_var.set("100%")
        self._status_var.set(
            f"Done  \u00b7  {self._surface.char_w}\u00d7{self._surface.char_h} chars  "
            f"\u00b7  {len(output_str):,} characters total"
        )
        self._btn_convert.config(state="normal")

        self._btn_copy.config(
            state="normal",
            highlightbackground=T.CYAN,
            fg=T.CYAN,
        )

        self._update_preview(preview_img)

        self._raw_text.config(state="normal")
        self._raw_text.delete("1.0", "end")
        self._raw_text.insert("end", output_str, "muted")
        self._raw_text.config(state="disabled")

    def _on_conversion_error(self, err: str) -> None:
        self._converting = False
        self._btn_convert.config(state="normal")
        self._status_var.set(f"Error: {err}")
        T.themed_showinfo(self.winfo_toplevel(), "Conversion Error",
                          f"Conversion failed:\n\n{err}")

    # -----------------------------------------------------------------------
    # Preview
    # -----------------------------------------------------------------------

    def _update_preview(self, pil_img) -> None:
        try:
            from PIL import ImageTk, Image

            canvas_w = self._preview_canvas.winfo_width() or 640
            canvas_h = 160

            img_w, img_h = pil_img.size
            scale = min(canvas_w / img_w, canvas_h / img_h, 1.0)
            disp_w = max(1, int(img_w * scale))
            disp_h = max(1, int(img_h * scale))

            display = pil_img.resize((disp_w, disp_h), Image.NEAREST)
            self._preview_photo = ImageTk.PhotoImage(display)
            self._preview_canvas.config(height=disp_h + 8)
            self._preview_canvas.delete("all")
            cx = canvas_w // 2
            self._preview_canvas.create_image(cx, (disp_h + 8) // 2,
                                              image=self._preview_photo,
                                              anchor="center")
        except Exception:
            pass   # preview is non-critical

    # -----------------------------------------------------------------------
    # Clipboard
    # -----------------------------------------------------------------------

    def _on_copy(self) -> None:
        if not self._output_str:
            return
        root = self.winfo_toplevel()
        root.clipboard_clear()
        root.clipboard_append(self._output_str)
        root.update()

        self._btn_copy.config(text="  \u2713  COPIED!  ")
        self.after(1800, lambda: self._btn_copy.config(
            text="  \u29c9  COPY TO CLIPBOARD  "))

    # -----------------------------------------------------------------------
    # Info / reference popups
    # -----------------------------------------------------------------------

    def _on_open_reference(self) -> None:
        if self._ref_window and self._ref_window.winfo_exists():
            self._ref_window.lift()
        else:
            self._ref_window = T.LCDReferenceWindow(self.winfo_toplevel())

    def _on_how_it_works(self) -> None:
        T.themed_showinfo(
            self.winfo_toplevel(),
            "How To Apply",
            _HOW_TO_APPLY,
            width=480,
        )

    def _on_dither_info(self) -> None:
        T.themed_showinfo(
            self.winfo_toplevel(),
            "Dithering Modes",
            _DITHERING_INFO,
            width=520,
        )
