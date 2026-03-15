#!/usr/bin/env python3
"""
screen_home.py — SE Tools title / home screen.

Three nav-card buttons route the user to:
  • Setup & Requirements
  • Image Converter  (for modders)
  • LCD Text Converter  (for players)
"""

import tkinter as tk
from tkinter import ttk
import webbrowser

import se_theme as T


class HomeScreen(ttk.Frame):

    def __init__(self, parent, app):
        super().__init__(parent, style="TFrame")
        self._app = app
        self._build()

    # -----------------------------------------------------------------------

    def _build(self):
        # ── Header ──────────────────────────────────────────────────────────
        T.build_header(
            self,
            title="UNIVERSAL IMAGE CONVERTER",
            subtitle="Space Engineers modding and player utilities.",
        )
        T.separator(self, pady=(10, 14))

        # ── Nav cards ────────────────────────────────────────────────────────
        nav = ttk.Frame(self, style="TFrame")
        nav.pack(fill="x", expand=False, padx=24)

        self._nav_card(
            nav,
            icon="◈",
            title="SETUP  &  REQUIREMENTS",
            desc=("No install required — just the exe.\n"
                  "One optional tool (texconv) for best DDS quality."),
            command=lambda: self._app.show_screen("setup"),
        ).pack(fill="x", pady=(0, 10))

        self._nav_card(
            nav,
            icon="▣",
            title="IMAGE TO DDS  ·  FOR MODDERS",
            desc=("Convert images to DDS / BC7_UNORM format for\n"
                  "Space Engineers LCD texture mods."),
            command=lambda: self._app.show_screen("image_converter"),
        ).pack(fill="x", pady=(0, 10))

        self._nav_card(
            nav,
            icon="▶",
            title="IMAGE TO LCD  ·  FOR PLAYERS",
            desc=("Convert images to pasteable LCD text strings.\n"
                  "No modding or files required — copy, paste, and play."),
            command=lambda: self._app.show_screen("text_converter"),
        ).pack(fill="x")

        # ── Footer ───────────────────────────────────────────────────────────
        T.separator(self, pady=(18, 0))

        ttk.Label(self, text="Made with \u2665 by Godimas and Claude",
                  style="Muted.TLabel").pack(anchor="center", pady=(6, 0))

        credit_row = ttk.Frame(self, style="TFrame")
        credit_row.pack(anchor="center", pady=(2, 0))
        ttk.Label(credit_row,
                  text="Image To LCD reverse engineered from\u00a0",
                  style="Muted.TLabel").pack(side="left")
        link = tk.Label(credit_row,
                        text="Whiplash's Image Converter",
                        bg=T.BG, fg=T.BLUE,
                        font=("Courier New", 9, "underline"),
                        cursor="hand2")
        link.pack(side="left")
        link.bind("<Button-1>",
                  lambda _e: webbrowser.open(
                      "https://github.com/Whiplash141/Whips-Image-Converter"))
        link.bind("<Enter>", lambda _e: link.config(fg=T.CYAN))
        link.bind("<Leave>", lambda _e: link.config(fg=T.BLUE))

        ttk.Label(self, text="v1.0  \u00b7  SE Image Converter",
                  style="Muted.TLabel").pack(anchor="center", pady=(6, 10))

    # -----------------------------------------------------------------------

    def _nav_card(self, parent, icon: str, title: str,
                  desc: str, command) -> tk.Frame:
        """
        A dark panel card with icon, title, description.
        Highlights CYAN on hover; fires *command* on click anywhere.
        """
        outer = tk.Frame(
            parent, bg=T.PANEL, cursor="hand2",
            highlightthickness=1,
            highlightbackground=T.BORDER,
            highlightcolor=T.CYAN,
        )
        inner = tk.Frame(outer, bg=T.PANEL)
        inner.pack(fill="both", expand=True, padx=16, pady=14)

        # Fixed-size container keeps the icon column the same width on all cards
        # regardless of how each character renders at font size.
        icon_box = tk.Frame(inner, bg=T.PANEL, width=48, height=48)
        icon_box.pack_propagate(False)
        icon_box.pack(side="left", padx=(0, 16))

        icon_lbl = tk.Label(icon_box, text=icon,
                            bg=T.PANEL, fg=T.CYAN,
                            font=("Courier New", 22, "bold"))
        icon_lbl.place(relx=0.5, rely=0.5, anchor="center")

        text_frame = tk.Frame(inner, bg=T.PANEL)
        text_frame.pack(side="left", fill="both", expand=True)

        title_lbl = tk.Label(text_frame, text=title,
                             bg=T.PANEL, fg=T.CYAN,
                             font=("Courier New", 11, "bold"),
                             anchor="w")
        title_lbl.pack(anchor="w")

        desc_lbl = tk.Label(text_frame, text=desc,
                            bg=T.PANEL, fg=T.MUTED,
                            font=("Courier New", 9),
                            anchor="w", justify="left")
        desc_lbl.pack(anchor="w", pady=(2, 0))

        # Collect every widget for consistent hover/click behaviour
        all_w = [outer, inner, icon_box, text_frame, icon_lbl, title_lbl, desc_lbl]

        def _enter(_e):
            outer.config(highlightbackground=T.CYAN)
            for w in all_w:
                try:
                    w.config(bg=T.HOVER)
                except tk.TclError:
                    pass

        def _leave(_e):
            outer.config(highlightbackground=T.BORDER)
            for w in all_w:
                try:
                    w.config(bg=T.PANEL)
                except tk.TclError:
                    pass

        for w in all_w:
            w.bind("<Enter>",    _enter)
            w.bind("<Leave>",    _leave)
            w.bind("<Button-1>", lambda _e: command())

        return outer
