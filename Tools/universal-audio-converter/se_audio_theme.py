#!/usr/bin/env python3
"""
se_audio_theme.py — Shared theme for the SE Audio Converter tool.

All colour constants, font specs, ttk style configuration,
and reusable widget helpers.  Import this module in every screen.

Orange accent variant of se_theme.py (CYAN="#ff8c00", BLUE="#c45c00").
"""

import base64
import io
import math
import tkinter as tk
from tkinter import ttk


# ===========================================================================
# Colour Palette
# ===========================================================================
BG      = "#0d1117"
PANEL   = "#161b22"
CYAN    = "#ff8c00"   # Orange accent (replaces blue/cyan of the image tool)
BLUE    = "#c45c00"   # Darker orange for buttons / selection highlights
TEXT    = "#e6edf3"
MUTED   = "#8b949e"
GREEN   = "#3fb950"
ORANGE  = "#d29922"
RED     = "#f85149"
HOVER   = "#21262d"
BORDER  = "#30363d"

# ===========================================================================
# Typography  (all Courier New — monospace keeps column alignment intact)
# ===========================================================================
FONT_MONO  = "Courier New"
FONT_BODY  = ("Courier New", 10)
FONT_LABEL = ("Courier New", 10)
FONT_TITLE = ("Courier New", 16, "bold")
FONT_SMALL = ("Courier New", 8)


# ===========================================================================
# ttk Style Configuration  — call once from the launcher at startup
# ===========================================================================

def configure_styles(style: ttk.Style) -> None:
    style.theme_use("clam")

    style.configure("TFrame",        background=BG)
    style.configure("Panel.TFrame",  background=PANEL)

    style.configure("TLabel",         background=BG,    foreground=TEXT,  font=FONT_LABEL)
    style.configure("Panel.TLabel",   background=PANEL, foreground=TEXT,  font=FONT_LABEL)
    style.configure("Muted.TLabel",   background=BG,    foreground=MUTED, font=("Courier New", 9))
    style.configure("Title.TLabel",   background=BG,    foreground=CYAN,  font=FONT_TITLE)
    style.configure("Section.TLabel", background=BG,    foreground=CYAN,  font=("Courier New", 10, "bold"))

    # Standard action button
    style.configure("SE.TButton",
        background=PANEL, foreground=CYAN,
        bordercolor=BORDER, darkcolor=PANEL, lightcolor=PANEL,
        relief="flat", font=FONT_LABEL, padding=(10, 4))
    style.map("SE.TButton",
        background=[("active", HOVER), ("disabled", BG)],
        foreground=[("disabled", BORDER)])

    # Back navigation button — muted at rest, orange on hover
    style.configure("Back.TButton",
        background=PANEL, foreground=MUTED,
        bordercolor=BORDER, darkcolor=PANEL, lightcolor=PANEL,
        relief="flat", font=("Courier New", 10), padding=(10, 4))
    style.map("Back.TButton",
        background=[("active", HOVER)],
        foreground=[("active", CYAN)])

    # Small info / secondary button
    style.configure("Info.TButton",
        background=PANEL, foreground=MUTED,
        bordercolor=BORDER, darkcolor=PANEL, lightcolor=PANEL,
        relief="flat", font=("Courier New", 9), padding=(4, 2))
    style.map("Info.TButton",
        background=[("active", HOVER)],
        foreground=[("active", CYAN)])

    style.configure("SE.Horizontal.TProgressbar",
        troughcolor=PANEL, background=CYAN,
        bordercolor=BORDER, darkcolor=CYAN, lightcolor=CYAN,
        thickness=18)

    style.configure("SE.TCheckbutton",
        background=BG, foreground=TEXT, font=FONT_LABEL,
        indicatorcolor=PANEL, indicatordiameter=14)
    style.map("SE.TCheckbutton",
        indicatorcolor=[("selected", CYAN), ("!selected", PANEL)],
        background=[("active", BG)],
        foreground=[("active", TEXT), ("disabled", BORDER)])

    style.configure("SE.TCombobox",
        fieldbackground=PANEL, background=PANEL,
        foreground=TEXT, selectforeground=CYAN, selectbackground=PANEL,
        bordercolor=BORDER, darkcolor=PANEL, lightcolor=PANEL,
        arrowcolor=CYAN, font=FONT_LABEL)
    style.map("SE.TCombobox",
        fieldbackground=[("readonly", PANEL)],
        selectbackground=[("readonly", PANEL)],
        selectforeground=[("readonly", CYAN)])

    style.configure("SE.Vertical.TScrollbar",
        background=PANEL, troughcolor=BG,
        bordercolor=BORDER, arrowcolor=CYAN,
        darkcolor=PANEL, lightcolor=PANEL)
    style.map("SE.Vertical.TScrollbar", background=[("active", HOVER)])

    style.configure("SE.Horizontal.TScrollbar",
        background=PANEL, troughcolor=BG,
        bordercolor=BORDER, arrowcolor=CYAN,
        darkcolor=PANEL, lightcolor=PANEL)
    style.map("SE.Horizontal.TScrollbar", background=[("active", HOVER)])

    style.configure("SE.Treeview",
        background=BG, foreground=TEXT, fieldbackground=BG,
        bordercolor=BORDER, font=("Courier New", 9), rowheight=22)
    style.map("SE.Treeview",
        background=[("selected", BLUE)],
        foreground=[("selected", TEXT)])
    style.configure("SE.Treeview.Heading",
        background=PANEL, foreground=CYAN,
        font=("Courier New", 9, "bold"),
        bordercolor=BORDER, relief="flat")
    style.map("SE.Treeview.Heading",
        background=[("active", HOVER)])


# ===========================================================================
# Hex Canvas Icon
# ===========================================================================

def draw_hex_canvas(parent, bg: str = BG, size: int = 54) -> tk.Canvas:
    c  = tk.Canvas(parent, width=size, height=size, bg=bg, bd=0, highlightthickness=0)
    cx = cy = size // 2
    r  = int(size * 24 / 54)
    ri = int(size * 16 / 54)

    def _hex_pts(radius):
        pts = []
        for a in range(0, 360, 60):
            pts += [cx + radius * math.cos(math.radians(a - 30)),
                    cy + radius * math.sin(math.radians(a - 30))]
        return pts

    c.create_polygon(_hex_pts(r),  fill=PANEL, outline=CYAN, width=2)
    c.create_polygon(_hex_pts(ri), fill=BG,    outline=CYAN, width=1)
    c.create_text(cx, cy, text="SE", fill=CYAN,
                  font=("Courier New", max(8, int(size * 12 / 54)), "bold"))
    return c


def build_icon_photoimage(root_widget) -> "tk.PhotoImage | None":
    try:
        from PIL import Image as PILImage, ImageFont
        from PIL import ImageDraw
        size = 64
        img  = PILImage.new("RGBA", (size, size), (0, 0, 0, 0))
        draw = ImageDraw.Draw(img)
        cx = cy = size // 2
        r  = 28
        pts = [(cx + r * math.cos(math.radians(a)),
                cy + r * math.sin(math.radians(a))) for a in range(0, 360, 60)]
        draw.polygon(pts, fill="#0d1117")
        for offset in range(3):
            ro   = r - offset
            pts2 = [(cx + ro * math.cos(math.radians(a)),
                     cy + ro * math.sin(math.radians(a))) for a in range(0, 360, 60)]
            draw.polygon(pts2, outline="#ff8c00")
        draw.text((cx - 12, cy - 9), "SE", fill="#ff8c00",
                  font=ImageFont.load_default())
        buf = io.BytesIO()
        img.save(buf, format="PNG")
        buf.seek(0)
        return tk.PhotoImage(data=base64.b64encode(buf.read()).decode())
    except Exception:
        return None


# ===========================================================================
# Standard Screen Header
# ===========================================================================

def build_header(parent, title: str, subtitle: str,
                 back_cb=None, note: str = "") -> ttk.Frame:
    """
    Attach a consistent header to *parent*.
    If back_cb is provided a ◀ BACK button appears on the right.
    If note is provided it appears as a third muted line below subtitle.
    Returns the outer header frame.
    """
    outer = ttk.Frame(parent, style="TFrame")
    outer.pack(fill="x", padx=16, pady=(14, 0))

    # Back button pinned to the right — packed first so it takes priority
    if back_cb:
        ttk.Button(outer, text="◀  BACK", command=back_cb,
                   style="Back.TButton", width=10).pack(side="right", anchor="n")

    icon_canvas = draw_hex_canvas(outer)
    icon_canvas.pack(side="left", padx=(0, 12))

    text_block = ttk.Frame(outer, style="TFrame")
    text_block.pack(side="left", fill="both", expand=True)
    ttk.Label(text_block, text=title,    style="Title.TLabel").pack(anchor="w")
    ttk.Label(text_block, text=subtitle, style="Muted.TLabel").pack(anchor="w")
    if note:
        ttk.Label(text_block, text=note, style="Muted.TLabel").pack(anchor="w")

    return outer


# ===========================================================================
# Layout Helpers
# ===========================================================================

def separator(parent, pady: tuple = (8, 8)) -> None:
    """Thin 1 px horizontal rule in BORDER colour."""
    tk.Frame(parent, bg=BORDER, height=1).pack(fill="x", padx=16, pady=pady)


def hero_button(parent, text: str, command) -> tk.Button:
    """The large orange-bordered action button."""
    btn = tk.Button(
        parent, text=text, command=command,
        bg=PANEL, fg=CYAN,
        activebackground=HOVER, activeforeground=CYAN,
        font=("Courier New", 13, "bold"),
        relief="flat", bd=0,
        padx=24, pady=8,
        cursor="hand2",
        highlightthickness=2,
        highlightbackground=CYAN, highlightcolor=CYAN,
    )
    btn.bind("<Enter>", lambda e: btn.config(bg=HOVER))
    btn.bind("<Leave>", lambda e: btn.config(bg=PANEL))
    return btn


def log_text_widget(parent) -> tk.Text:
    """Standard read-only log Text widget."""
    widget = tk.Text(
        parent,
        bg=BG, fg=TEXT,
        font=("Courier New", 9),
        relief="flat", bd=0,
        state="disabled", height=10,
        wrap="word", highlightthickness=0,
        selectbackground=BLUE,
    )
    widget.tag_configure("info",    foreground=TEXT)
    widget.tag_configure("success", foreground=GREEN)
    widget.tag_configure("warn",    foreground=ORANGE)
    widget.tag_configure("error",   foreground=RED)
    widget.tag_configure("cyan",    foreground=CYAN)
    widget.tag_configure("muted",   foreground=MUTED)
    widget.tag_configure("sep",     foreground=MUTED, justify="center")
    return widget


def append_log(widget: tk.Text, message: str, tag: str = "info") -> None:
    """Thread-safe log append (call from main thread via after())."""
    widget.config(state="normal")
    widget.insert("end", message + "\n", tag)
    widget.see("end")
    widget.config(state="disabled")


# ===========================================================================
# Audio Editor Reference Window
# ===========================================================================

class AudioEditorReferenceWindow(tk.Toplevel):
    """Dark-themed resizable reference window for Audio Editor controls."""

    _CONTENT = [
        ("section", "CLIP"),
        ("op", "✂  TRIM",
         "Cuts the audio down to just your selection.\n"
         "Everything outside the selection is removed."),
        ("op", "⊘  SILENCE",
         "Replaces the selected region with silence.\n"
         "The total length of the file does not change."),
        ("section", "TRANSFORM"),
        ("op", "⇄  REVERSE",
         "Permanently reverses the selected region in the file.\n"
         "Requires a selection."),
        ("op", "▲  NORMALIZE",
         "Boosts (or lowers) the whole file so its peak volume\n"
         "hits 0 dB — the maximum before clipping."),
        ("op", "≈  DC OFFSET",
         "Removes a DC bias from the waveform. Useful if the waveform\n"
         "is shifted above or below the centre line, which can cause\n"
         "clicks or reduce headroom."),
        ("section", "VOLUME"),
        ("op", "Gain  /  ✓ APPLY",
         "Multiplies the selected region by the gain value.\n"
         "With no selection, applies to the whole file.\n"
         "1.0 = no change  ·  0.5 = half volume  ·  2.0 = double.\n"
         "Values above ~1.0 may cause clipping."),
        ("section", "FADES"),
        ("op", "↗  FADE IN",
         "Smoothly ramps volume from silence up to full level\n"
         "across the selected region."),
        ("op", "↘  FADE OUT",
         "Smoothly ramps volume from full level down to silence\n"
         "across the selected region."),
        ("section", "SPEED"),
        ("op", "✓  APPLY",
         "Resamples the audio to change playback speed.\n"
         "Faster speeds raise pitch; slower speeds lower it.\n"
         "The file length changes to match."),
        ("section", "CHANNELS"),
        ("op", "R  /  L  (waveform toggle buttons)",
         "Stereo files show two waveform lanes — R on top, L on bottom.\n"
         "Click R or L to toggle which channel is active (lit orange = on).\n"
         "EXTRACT and SOLO read these to know which channel to target."),
        ("op", "⊕  MONO → STEREO",  "Duplicates a mono track into both L and R channels."),
        ("op", "⊖  STEREO → MONO",  "Mixes L and R channels down to a single mono track."),
        ("op", "↔  SWAP L/R",        "Swaps the left and right channels."),
        ("op", "⊟  EXTRACT",
         "Keeps only the active channel as mono — discards the other.\n"
         "Toggle one channel button on (and the other off) before pressing.\n"
         "Both on or both off: operation is blocked with a log message."),
        ("op", "◎  SOLO",
         "Mutes the inactive channel so only the active one plays.\n"
         "File stays stereo throughout.\n"
         "With a time selection active, only that region is affected.\n"
         "Toggle one channel button on (and the other off) before pressing."),
    ]

    def __init__(self, parent):
        super().__init__(parent)
        self.title("Audio Editor — Controls Reference")
        self.configure(bg=BG)
        self.resizable(True, True)
        self.geometry("560x580")
        self.minsize(420, 360)
        self.transient(parent)
        self._build()

    def _build(self):
        # Header
        hdr = ttk.Frame(self, style="TFrame")
        hdr.pack(fill="x", padx=14, pady=(12, 0))
        ttk.Label(hdr, text="▣  AUDIO EDITOR — CONTROLS REFERENCE",
                  style="Section.TLabel").pack(side="left")

        tk.Frame(self, bg=BORDER, height=1).pack(fill="x", padx=14, pady=(8, 0))
        ttk.Label(self,
                  text="Time-based ops apply to the waveform selection  ·  Channel ops (Extract, Solo) use the L / R toggle buttons.",
                  style="Muted.TLabel").pack(anchor="w", padx=14, pady=(4, 8))

        # Scrollable content
        content_frame = ttk.Frame(self, style="Panel.TFrame")
        content_frame.pack(fill="both", expand=True, padx=14)

        txt = tk.Text(
            content_frame,
            bg=PANEL, fg=TEXT,
            font=("Courier New", 9),
            relief="flat", bd=0,
            highlightthickness=0,
            wrap="word",
            padx=12, pady=8,
            cursor="arrow",
            state="normal",
        )
        vsb = ttk.Scrollbar(content_frame, orient="vertical",
                            command=txt.yview,
                            style="SE.Vertical.TScrollbar")
        txt.configure(yscrollcommand=vsb.set)
        vsb.pack(side="right", fill="y")
        txt.pack(side="left", fill="both", expand=True)

        txt.tag_configure("section",
                          foreground=CYAN,
                          font=("Courier New", 10, "bold"),
                          spacing1=12, spacing3=2)
        txt.tag_configure("op_name",
                          foreground=TEXT,
                          font=("Courier New", 9, "bold"),
                          lmargin1=16, lmargin2=16,
                          spacing1=4)
        txt.tag_configure("op_desc",
                          foreground=MUTED,
                          font=("Courier New", 9),
                          lmargin1=32, lmargin2=32,
                          spacing3=6)

        for item in self._CONTENT:
            if item[0] == "section":
                txt.insert("end", item[1] + "\n", "section")
            else:
                _, name, desc = item
                txt.insert("end", name + "\n", "op_name")
                for line in desc.split("\n"):
                    txt.insert("end", line + "\n", "op_desc")

        txt.config(state="disabled")

        # Footer
        foot = ttk.Frame(self, style="TFrame")
        foot.pack(fill="x", padx=14, pady=(8, 12))
        ttk.Button(foot, text="CLOSE", command=self.destroy,
                   style="SE.TButton").pack(side="right")


# ===========================================================================
# Themed Dialogs
# ===========================================================================

def _base_dialog(parent, title: str, message: str,
                 width: int = 440) -> tuple:
    """
    Build a dark themed Toplevel.  Returns (dlg, btn_row).
    Caller adds buttons to btn_row.
    """
    dlg = tk.Toplevel(parent)
    dlg.title(title)
    dlg.configure(bg=BG)
    dlg.resizable(False, False)
    dlg.transient(parent)
    dlg.grab_set()

    hdr = ttk.Frame(dlg, style="TFrame")
    hdr.pack(fill="x", padx=16, pady=(14, 0))
    ttk.Label(hdr, text=f"▣  {title.upper()}",
              style="Section.TLabel").pack(anchor="w")
    tk.Frame(dlg, bg=BORDER, height=1).pack(fill="x", padx=16, pady=(8, 0))

    tk.Label(dlg, text=message,
             bg=BG, fg=TEXT,
             font=("Courier New", 9),
             justify="left", wraplength=width, anchor="w",
             padx=16, pady=12).pack(fill="x")

    tk.Frame(dlg, bg=BORDER, height=1).pack(fill="x", padx=16)

    btn_row = ttk.Frame(dlg, style="TFrame")
    btn_row.pack(fill="x", padx=16, pady=(10, 14))

    parent.update_idletasks()
    dlg.update_idletasks()
    x = parent.winfo_x() + (parent.winfo_width()  - dlg.winfo_reqwidth())  // 2
    y = parent.winfo_y() + (parent.winfo_height() - dlg.winfo_reqheight()) // 2
    dlg.geometry(f"+{x}+{y}")

    return dlg, btn_row


def themed_askokcancel(parent, title: str, message: str) -> bool:
    """Dark-themed OK / Cancel dialog.  Returns True if OK clicked."""
    result = tk.BooleanVar(value=False)
    dlg, btn_row = _base_dialog(parent, title, message)

    def _ok():
        result.set(True)
        dlg.destroy()

    def _cancel():
        result.set(False)
        dlg.destroy()

    dlg.bind("<Return>", lambda _: _ok())
    dlg.bind("<Escape>", lambda _: _cancel())
    ttk.Button(btn_row, text="CANCEL", command=_cancel,
               style="SE.TButton", width=8).pack(side="right", padx=(6, 0))
    ttk.Button(btn_row, text="OK",     command=_ok,
               style="SE.TButton", width=8).pack(side="right")

    dlg.wait_window()
    return result.get()


def themed_showinfo(parent, title: str, message: str,
                    width: int = 460) -> None:
    """Dark-themed single-button info dialog."""
    dlg, btn_row = _base_dialog(parent, title, message, width=width)
    dlg.bind("<Return>", lambda _: dlg.destroy())
    dlg.bind("<Escape>", lambda _: dlg.destroy())
    ttk.Button(btn_row, text="OK", command=dlg.destroy,
               style="SE.TButton", width=8).pack(side="right")
    dlg.wait_window()
