#!/usr/bin/env python3
"""
screen_audio_setup.py — Setup & Requirements screen for SE Audio Converter.

Displays tool detection status and download/install instructions for:
  • ffmpeg        (required for Audio Converter)
  • xWMAEncode    (required for XWM output)
  • numpy         (required for Audio Editor)
  • sounddevice   (required for Audio Editor playback)
"""

import shutil
import subprocess
from pathlib import Path

import tkinter as tk
from tkinter import ttk

import se_audio_theme as T


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


def _check_python_package(pkg: str) -> bool:
    try:
        __import__(pkg)
        return True
    except ImportError:
        return False


def _get_ffmpeg_version(path: str) -> str:
    try:
        r = subprocess.run([path, "-version"], capture_output=True,
                           text=True, timeout=5)
        first = r.stdout.splitlines()[0] if r.stdout else ""
        return first.split("version")[-1].strip().split(" ")[0] if "version" in first else "found"
    except Exception:
        return "found"


# ---------------------------------------------------------------------------
# Screen
# ---------------------------------------------------------------------------

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
            subtitle="Tool detection and download instructions.",
            back_cb=lambda: self._app.show_screen("home"),
        )
        T.separator(self, pady=(8, 10))

        # Scrollable body
        canvas = tk.Canvas(self, bg=T.BG, bd=0, highlightthickness=0)
        vsb = ttk.Scrollbar(self, orient="vertical", command=canvas.yview,
                            style="SE.Vertical.TScrollbar")
        canvas.configure(yscrollcommand=vsb.set)
        vsb.pack(side="right", fill="y")
        canvas.pack(side="left", fill="both", expand=True)

        body = ttk.Frame(canvas, style="TFrame")
        win_id = canvas.create_window((0, 0), window=body, anchor="nw")

        def _resize(e):
            canvas.itemconfig(win_id, width=e.width)
        def _scroll_configure(_e):
            canvas.configure(scrollregion=canvas.bbox("all"))
        def _on_wheel(e):
            canvas.yview_scroll(int(-1 * (e.delta / 120)), "units")

        canvas.bind("<Configure>", _resize)
        body.bind("<Configure>", _scroll_configure)
        canvas.bind_all("<MouseWheel>", _on_wheel)

        self._build_body(body)

    def _build_body(self, parent):
        pad = dict(padx=24)

        # ── EXTERNAL TOOLS ───────────────────────────────────────────────────
        self._section(parent, "EXTERNAL TOOLS")

        # ffmpeg
        ffmpeg_path = _find_tool("ffmpeg")
        self._tool_row(
            parent,
            name="ffmpeg",
            status_ok=ffmpeg_path is not None,
            status_text=_get_ffmpeg_version(ffmpeg_path) if ffmpeg_path else "NOT FOUND",
            required_by="Audio Converter",
            desc=("Open-source audio/video converter. Required to convert MP3, OGG,\n"
                  "FLAC and other formats to WAV."),
            download_url="https://ffmpeg.org/download.html",
            install_note=("Download the Windows build. Extract and place ffmpeg.exe\n"
                          "next to SE Audio Converter.exe  — or add it to your PATH."),
        )

        T.separator(parent, pady=(6, 6))

        # xWMAEncode
        xwma_path = _find_tool("xWMAEncode")
        self._tool_row(
            parent,
            name="xWMAEncode",
            status_ok=xwma_path is not None,
            status_text="found" if xwma_path else "NOT FOUND",
            required_by="Audio Converter  (XWM output only)",
            desc=("Microsoft's official XWM encoder. Required only if you want\n"
                  "to export audio as .xwm for Space Engineers mods."),
            download_url="https://store.steampowered.com/app/244860/Space_Engineers_Mod_SDK/",
            install_note=("Already included in the Space Engineers Mod SDK!\n"
                          "Find it at:  [ModSDK]\\Tools\\xWMAEncode.exe\n"
                          "Copy it next to SE Audio Converter.exe  — or add the Tools folder to PATH.\n"
                          "Don't have the ModSDK? Add it free via Steam (App ID 244860)."),
        )

        T.separator(parent, pady=(6, 14))

        # ── HOW TO ADD TO PATH ────────────────────────────────────────────────
        self._section(parent, "HOW TO ADD A TOOL TO PATH  (Windows)")

        path_card = tk.Frame(parent, bg=T.PANEL,
                             highlightthickness=1,
                             highlightbackground=T.BORDER)
        path_card.pack(fill="x", padx=24, pady=(0, 8))

        path_inner = tk.Frame(path_card, bg=T.PANEL)
        path_inner.pack(fill="x", padx=12, pady=10)

        tk.Label(path_inner,
                 text=("Adding a tool to PATH means Windows can find it from anywhere,\n"
                       "so you don't need to place it next to the exe every time."),
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
            "      Example:  C:\\SpaceEngineersModSDK\\Tools\n"
            "      Example:  C:\\Tools\\ffmpeg\n"
            "9.  Click  OK  on all three windows to save your changes.\n"
            "10. Restart any open programs for the change to take effect."
        )
        tk.Label(path_inner, text=steps_text,
                 bg=T.PANEL, fg=T.MUTED,
                 font=("Courier New", 8), justify="left", anchor="w").pack(anchor="w")

        T.separator(parent, pady=(6, 14))

        # ── PYTHON PACKAGES ──────────────────────────────────────────────────
        self._section(parent, "PYTHON PACKAGES  (Audio Editor)")

        numpy_ok  = _check_python_package("numpy")
        sd_ok     = _check_python_package("sounddevice")

        self._package_row(parent, "numpy",  numpy_ok,
                          "Audio processing — fade, trim, normalize, resample, channel mix.")
        self._package_row(parent, "sounddevice", sd_ok,
                          "Audio playback with real-time position tracking for the waveform playhead.")

        if not (numpy_ok and sd_ok):
            ttk.Label(parent,
                      text="Install missing packages by running:",
                      style="Muted.TLabel").pack(anchor="w", padx=24, pady=(10, 2))
            cmd_frame = tk.Frame(parent, bg=T.PANEL)
            cmd_frame.pack(fill="x", padx=24, pady=(0, 4))
            tk.Label(cmd_frame,
                     text="  pip install numpy sounddevice  ",
                     bg=T.PANEL, fg=T.CYAN,
                     font=("Courier New", 10)).pack(anchor="w", padx=8, pady=6)

        T.separator(parent, pady=(10, 14))

        # ── WORKFLOW OVERVIEW ─────────────────────────────────────────────────
        self._section(parent, "RECOMMENDED WORKFLOW")

        steps = [
            ("1", "CONVERT",  "Use the Audio Converter to bring in any audio format \u2192 WAV"),
            ("2", "EDIT",     "Use the Audio Editor to trim, fade, and clean up your WAV"),
            ("3", "GENERATE", "Use the SBC Generator to create AudioDefinition + SoundBlock SBC files"),
            ("4", "ENCODE",   "Use the Audio Converter (XWM output) to produce the final .xwm file"),
            ("5", "MOD",      "Place .xwm files and .sbc files in your mod's Data and Audio folders"),
        ]

        for num, label, text in steps:
            row = tk.Frame(parent, bg=T.BG)
            row.pack(fill="x", padx=24, pady=(0, 6))
            tk.Label(row, text=f" {num} ", bg=T.BLUE, fg=T.TEXT,
                     font=("Courier New", 9, "bold"),
                     width=3).pack(side="left")
            tk.Label(row, text=f"  {label:<10}",
                     bg=T.BG, fg=T.CYAN,
                     font=("Courier New", 9, "bold")).pack(side="left")
            tk.Label(row, text=text,
                     bg=T.BG, fg=T.TEXT,
                     font=("Courier New", 9)).pack(side="left")

        tk.Frame(parent, bg=T.BG, height=16).pack()

    # -----------------------------------------------------------------------
    # Widget helpers
    # -----------------------------------------------------------------------

    def _section(self, parent, title: str) -> None:
        ttk.Label(parent, text=f"\u25a3  {title}",
                  style="Section.TLabel").pack(anchor="w", padx=24, pady=(0, 8))

    def _tool_row(self, parent, name: str, status_ok: bool,
                  status_text: str, required_by: str,
                  desc: str, download_url: str, install_note: str) -> None:

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
            import webbrowser
            link.bind("<Button-1>", lambda _e, u=download_url: webbrowser.open(u))
            link.bind("<Enter>", lambda _e: link.config(fg=T.CYAN))
            link.bind("<Leave>", lambda _e: link.config(fg=T.BLUE))

    def _package_row(self, parent, name: str, ok: bool, desc: str) -> None:
        row = tk.Frame(parent, bg=T.BG)
        row.pack(fill="x", padx=24, pady=(0, 4))

        color = T.GREEN if ok else T.RED
        mark  = "\u2713" if ok else "\u2717"
        tk.Label(row, text=f" {mark} ",
                 bg=color, fg=T.BG,
                 font=("Courier New", 9, "bold")).pack(side="left")
        tk.Label(row, text=f"  {name:<12}",
                 bg=T.BG, fg=T.CYAN,
                 font=("Courier New", 9, "bold")).pack(side="left")
        tk.Label(row, text=desc,
                 bg=T.BG, fg=T.MUTED,
                 font=("Courier New", 9)).pack(side="left")
