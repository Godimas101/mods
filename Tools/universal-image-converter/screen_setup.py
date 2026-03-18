#!/usr/bin/env python3
"""
screen_setup.py — Setup & Requirements screen for SE Tools.

Covers the one optional dependency (texconv.exe) and nothing else —
the exe bundles everything else users need.
"""

import shutil
import webbrowser
import tkinter as tk
from tkinter import ttk
from pathlib import Path

import se_theme as T


# External download URLs
_URL_DIRECTXTEX = "https://github.com/microsoft/DirectXTex/releases/latest"


# ---------------------------------------------------------------------------
# Detection helpers
# ---------------------------------------------------------------------------

def _find_tool(name: str) -> str | None:
    """Return path to an exe if found on PATH or next to this script."""
    found = shutil.which(name)
    if found:
        return found
    for ext in ("", ".exe"):
        candidate = Path(__file__).parent / (name + ext)
        if candidate.exists():
            return str(candidate)
    return None


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

        def _on_wheel(e):
            canvas.yview_scroll(int(-1 * (e.delta / 120)), "units")

        canvas.bind("<Configure>", _on_resize)
        content.bind("<Configure>", _on_frame_configure)
        canvas.bind_all("<MouseWheel>", _on_wheel)

        self._build_body(content)

    def _build_body(self, parent):
        pad = dict(padx=24)

        # ── You're ready ─────────────────────────────────────────────────────
        self._section(parent, "YOU'RE READY TO GO")
        self._body(parent,
                   "SE Image Converter is a standalone executable.\n"
                   "Python, Pillow, and all other dependencies are bundled inside.\n"
                   "Nothing to install — just run the exe.")

        self._rule(parent)

        # ── External tools ───────────────────────────────────────────────────
        self._section(parent, "EXTERNAL TOOLS")

        texconv_path = _find_tool("texconv")
        self._tool_row(
            parent,
            name="texconv",
            status_ok=texconv_path is not None,
            status_text="found" if texconv_path else "NOT FOUND",
            required_by="Image to DDS  (optional — improves quality)",
            desc=("texconv.exe is Microsoft's DirectXTex encoder.\n"
                  "When present, it produces BC7_UNORM DDS files —\n"
                  "higher quality than the built-in DXT5 encoder.\n"
                  "If not found, the tool falls back to DXT5 automatically."),
            install_note=("Place  texconv.exe  next to  SE Image Converter.exe,\n"
                          "or add the folder containing it to your system PATH."),
            download_url=_URL_DIRECTXTEX,
        )

        self._rule(parent)

        # ── How to add to PATH ────────────────────────────────────────────────
        self._section(parent, "HOW TO ADD A TOOL TO PATH  (Windows)")

        path_card = tk.Frame(parent, bg=T.PANEL,
                             highlightthickness=1,
                             highlightbackground=T.BORDER)
        path_card.pack(fill="x", **pad, pady=(0, 8))

        path_inner = tk.Frame(path_card, bg=T.PANEL)
        path_inner.pack(fill="x", padx=12, pady=10)

        tk.Label(path_inner,
                 text=("Adding a tool to PATH means Windows can find it from anywhere,\n"
                       "so you don't need to place it next to the exe."),
                 bg=T.PANEL, fg=T.TEXT,
                 font=("Courier New", 9), justify="left", anchor="w").pack(anchor="w")

        tk.Frame(path_inner, bg=T.BORDER, height=1).pack(fill="x", pady=(8, 6))

        steps_text = (
            "1.  Press  Win + S  (opens the search bar)\n"
            "2.  Type:  environment variables  — click the result that says\n"
            "      'Edit the system environment variables'\n"
            "3.  A window called System Properties opens.\n"
            "      Click the  'Environment Variables...'  button at the bottom.\n"
            "4.  A new window opens with two lists.\n"
            "      Look at the BOTTOM list labelled  'System variables'.\n"
            "5.  Scroll down that list until you see a variable named  Path.\n"
            "      Click on  Path  to select it  (it will highlight blue).\n"
            "6.  Click the  Edit  button below the System variables list.\n"
            "      (Do NOT click New — that creates a different variable.)\n"
            "7.  Another window opens showing a list of folder paths.\n"
            "      Click the  New  button on the RIGHT side of this window.\n"
            "8.  A blank line appears at the bottom of the list.\n"
            "      Type or paste the full path to the FOLDER containing your tool.\n"
            "      Example:  C:\\Tools\\texconv\n"
            "9.  Click  OK  on all three windows to save your changes.\n"
            "10. Restart any open programs for the change to take effect."
        )
        tk.Label(path_inner, text=steps_text,
                 bg=T.PANEL, fg=T.MUTED,
                 font=("Courier New", 8), justify="left", anchor="w").pack(anchor="w")

        tk.Frame(parent, bg=T.BG, height=20).pack()

    # -----------------------------------------------------------------------
    # Content helpers
    # -----------------------------------------------------------------------

    def _section(self, parent, text: str) -> None:
        ttk.Label(parent, text=f"▣  {text}",
                  style="Section.TLabel").pack(anchor="w", padx=24, pady=(14, 8))

    def _body(self, parent, text: str) -> None:
        tk.Label(parent, text=text,
                 bg=T.BG, fg=T.TEXT,
                 font=("Courier New", 9),
                 justify="left", anchor="w").pack(anchor="w", padx=24, pady=(2, 0))

    def _rule(self, parent) -> None:
        tk.Frame(parent, bg=T.BORDER, height=1).pack(
            fill="x", padx=24, pady=(14, 0))

    def _tool_row(self, parent, name: str, status_ok: bool,
                  status_text: str, required_by: str,
                  desc: str, install_note: str, download_url: str) -> None:

        card = tk.Frame(parent, bg=T.PANEL,
                        highlightthickness=1,
                        highlightbackground=T.BORDER)
        card.pack(fill="x", padx=24, pady=(0, 8))

        inner = tk.Frame(card, bg=T.PANEL)
        inner.pack(fill="x", padx=12, pady=10)

        # Name + status badge
        top = tk.Frame(inner, bg=T.PANEL)
        top.pack(fill="x")

        tk.Label(top, text=name,
                 bg=T.PANEL, fg=T.CYAN,
                 font=("Courier New", 11, "bold")).pack(side="left")

        badge_color = T.GREEN if status_ok else T.RED
        badge_text  = f"  \u2713 {status_text}  " if status_ok else f"  \u2717 {status_text}  "
        tk.Label(top, text=badge_text,
                 bg=badge_color, fg=T.BG,
                 font=("Courier New", 8, "bold")).pack(side="left", padx=(10, 0))

        tk.Label(top, text=f"Required by: {required_by}",
                 bg=T.PANEL, fg=T.MUTED,
                 font=("Courier New", 8)).pack(side="right")

        # Description
        tk.Label(inner, text=desc,
                 bg=T.PANEL, fg=T.TEXT,
                 font=("Courier New", 9),
                 justify="left", anchor="w").pack(anchor="w", pady=(6, 0))

        if not status_ok:
            tk.Frame(inner, bg=T.BORDER, height=1).pack(fill="x", pady=(8, 6))

            tk.Label(inner, text=install_note,
                     bg=T.PANEL, fg=T.MUTED,
                     font=("Courier New", 8),
                     justify="left", anchor="w").pack(anchor="w")

            link_row = tk.Frame(inner, bg=T.PANEL)
            link_row.pack(anchor="w", pady=(6, 0))
            tk.Label(link_row, text="Download: ",
                     bg=T.PANEL, fg=T.MUTED,
                     font=("Courier New", 8)).pack(side="left")
            link = tk.Label(link_row, text=download_url,
                            bg=T.PANEL, fg=T.BLUE,
                            font=("Courier New", 8, "underline"),
                            cursor="hand2")
            link.pack(side="left")
            link.bind("<Button-1>", lambda _e, u=download_url: webbrowser.open(u))
            link.bind("<Enter>", lambda _e: link.config(fg=T.CYAN))
            link.bind("<Leave>", lambda _e: link.config(fg=T.BLUE))
