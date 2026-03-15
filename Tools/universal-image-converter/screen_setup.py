#!/usr/bin/env python3
"""
screen_setup.py — Setup & Requirements screen for SE Tools.

Covers the one optional dependency (texconv.exe) and nothing else —
the exe bundles everything else users need.
"""

import webbrowser
import tkinter as tk
from tkinter import ttk

import se_theme as T


# External download URLs
_URL_DIRECTXTEX = "https://github.com/microsoft/DirectXTex/releases/latest"


class SetupScreen(ttk.Frame):

    def __init__(self, parent, app):
        super().__init__(parent, style="TFrame")
        self._app = app
        self._build()

    # -----------------------------------------------------------------------

    def _build(self):
        T.build_header(
            self,
            title="SETUP  &  REQUIREMENTS",
            subtitle="No installation required — just the exe and one optional tool.",
            back_cb=lambda: self._app.show_screen("home"),
        )
        T.separator(self, pady=(10, 0))

        # Scrollable content area
        canvas    = tk.Canvas(self, bg=T.BG, bd=0, highlightthickness=0)
        scrollbar = ttk.Scrollbar(self, orient="vertical",
                                  command=canvas.yview,
                                  style="SE.Vertical.TScrollbar")
        canvas.configure(yscrollcommand=scrollbar.set)

        scrollbar.pack(side="right", fill="y")
        canvas.pack(side="left", fill="both", expand=True)

        content = ttk.Frame(canvas, style="TFrame")
        win_id  = canvas.create_window((0, 0), window=content, anchor="nw")

        def _on_resize(e):
            canvas.itemconfig(win_id, width=e.width)

        def _on_frame_configure(_e):
            canvas.configure(scrollregion=canvas.bbox("all"))

        canvas.bind("<Configure>", _on_resize)
        content.bind("<Configure>", _on_frame_configure)

        def _on_wheel(e):
            canvas.yview_scroll(int(-1 * (e.delta / 120)), "units")

        canvas.bind_all("<MouseWheel>", _on_wheel)

        pad = dict(padx=24)

        # ── You're ready ─────────────────────────────────────────────────────
        self._section(content, "YOU'RE READY TO GO", pad)
        self._body(content,
                   "SE Image Converter is a standalone executable.\n"
                   "Python, Pillow, and all other dependencies are bundled inside.\n"
                   "Nothing to install — just run the exe.", pad)

        self._rule(content)

        # ── texconv (optional) ───────────────────────────────────────────────
        self._section(content, "OPTIONAL  ·  TEXCONV  (IMAGE TO DDS ONLY)", pad)
        self._body(content,
                   "texconv.exe is Microsoft's DirectXTex encoder.\n"
                   "When present, it produces BC7_UNORM DDS files —\n"
                   "higher quality than the built-in DXT5 encoder.\n\n"
                   "To use it, place  texconv.exe  in either:\n"
                   "  •  the same folder as  SE Image Converter.exe, or\n"
                   "  •  anywhere on your system PATH.\n\n"
                   "If texconv is not found, the tool falls back to\n"
                   "the built-in DXT5 encoder automatically.", pad)
        self._link_btn(content, "  ⬇  Download DirectXTex (texconv.exe)",
                       _URL_DIRECTXTEX, pad)

        ttk.Frame(content, style="TFrame", height=20).pack()

    # -----------------------------------------------------------------------
    # Content helpers
    # -----------------------------------------------------------------------

    def _section(self, parent, text: str, pad: dict) -> None:
        ttk.Label(parent, text=f"▣  {text}",
                  style="Section.TLabel").pack(anchor="w", pady=(14, 2), **pad)

    def _body(self, parent, text: str, pad: dict) -> None:
        tk.Label(parent, text=text,
                 bg=T.BG, fg=T.TEXT,
                 font=("Courier New", 9),
                 justify="left", anchor="w").pack(anchor="w", pady=(2, 0), **pad)

    def _link_btn(self, parent, text: str, url: str, pad: dict) -> None:
        btn = tk.Label(parent, text=text,
                       bg=T.BG, fg=T.BLUE,
                       font=("Courier New", 9, "underline"),
                       cursor="hand2", anchor="w")
        btn.pack(anchor="w", pady=(4, 2), **pad)
        btn.bind("<Button-1>", lambda _e: webbrowser.open(url))
        btn.bind("<Enter>",    lambda _e: btn.config(fg=T.CYAN))
        btn.bind("<Leave>",    lambda _e: btn.config(fg=T.BLUE))

    def _rule(self, parent) -> None:
        tk.Frame(parent, bg=T.BORDER, height=1).pack(
            fill="x", padx=24, pady=(14, 0))
