#!/usr/bin/env python3
"""
screen_audio_sbc.py — SBC Generator screen for SE Audio Converter.

Converts .wav and .xwm audio files to Space Engineers AudioDefinition
and SoundBlock SBC (XML) mod files.  No external dependencies required —
uses stdlib only (wave, xml.etree.ElementTree, tkinter, pathlib).

Layout:
  [Header: AUDIO TO SBC  ◀ BACK]
  [separator]
  [LEFT PANEL 280px file list] | [RIGHT PANEL scrollable settings]
  [separator]
  [▶▶ GENERATE SBC ▶▶  hero button]
  [separator]
  [Output tabs: Audio.sbc | SoundBlock.sbc]
    [Text widget with syntax-highlighted XML]
    [COPY TO CLIPBOARD] [SAVE AS...]
"""

import re
import wave as wave_module
import xml.etree.ElementTree as ET
from pathlib import Path

import tkinter as tk
from tkinter import filedialog, ttk

import se_audio_theme as T

# ---------------------------------------------------------------------------
# Optional drag-and-drop support via tkinterdnd2
# ---------------------------------------------------------------------------
try:
    from tkinterdnd2 import DND_FILES  # noqa: F401
    _HAS_DND = True
except ImportError:
    _HAS_DND = False

# ---------------------------------------------------------------------------
# Constants
# ---------------------------------------------------------------------------
WARN_EXTS = {".mp3", ".ogg", ".flac", ".aac", ".m4a", ".wma", ".aiff", ".aif", ".opus"}

CATEGORIES = [
    "Sb", "Music", "HUD", "BLOCK", "SHIP_JET", "SHOT", "AMB",
    "BOT", "CHAR", "TOOL", "SHPWPN", "STAT", "VOXEL", "PHYSICS",
    "GUI", "NPC",
]

WAVE_TYPES = ["D2 (Stereo/No position)", "D3 (3D positional)"]

LOOP_TYPES = [
    "None (single play)",
    "Simple loop (Loop only)",
    "Start/Loop/End",
]

MUSIC_CATEGORIES = [
    "Space", "Planet", "Battle", "Danger", "Credits",
    "Menu", "Tutorial", "Custom",
]

_WAVE_TYPE_MAP = {
    "D2 (Stereo/No position)": "D2",
    "D3 (3D positional)":      "D3",
}
_LOOP_TYPE_MAP = {
    "None (single play)":      "none",
    "Simple loop (Loop only)": "simple",
    "Start/Loop/End":          "start_loop_end",
}


# ---------------------------------------------------------------------------
# File data model
# ---------------------------------------------------------------------------

def _default_file_entry(path: Path) -> dict:
    stem = path.stem
    return {
        "path":               path,
        "filename":           path.name,
        "format":             path.suffix.lstrip(".").lower(),
        "duration":           0.0,
        "channels":           0,
        "sample_rate":        0,
        "bit_depth":          0,
        # Per-file settings
        "subtype_id":         stem,
        "display_name":       stem,
        "file_path_in_mod":   f"Audio\\{stem}{path.suffix}",
        "category":           "Sb",
        "wave_type":          "D2",
        "volume":             1.0,
        "max_distance":       200,
        "loop_type":          "none",
        "loop_path":          "",
        "start_path":         "",
        "end_path":           "",
        "pitch_variation":    0.0,
        "volume_variation":   0.0,
        "sound_limit":        10,
        "stream_sound":       False,
        "prevent_sync":       False,
        "music_category":     "Space",
        "transition_category": "",
        "sb_category_id":     "MyModSounds",
    }


def _probe_wav(entry: dict) -> None:
    """Fill audio metadata for .wav files using stdlib wave module."""
    try:
        with wave_module.open(str(entry["path"]), "rb") as w:
            entry["channels"]    = w.getnchannels()
            entry["sample_rate"] = w.getframerate()
            entry["bit_depth"]   = w.getsampwidth() * 8
            fr                   = w.getframerate()
            entry["duration"]    = w.getnframes() / fr if fr else 0.0
    except Exception:
        pass


# ---------------------------------------------------------------------------
# XML Generation
# ---------------------------------------------------------------------------

def _indent_xml(elem, level: int = 0) -> None:
    """Pretty-print indent an ElementTree in-place."""
    indent = "\n" + "  " * level
    if len(elem):
        if not elem.text or not elem.text.strip():
            elem.text = indent + "  "
        if not elem.tail or not elem.tail.strip():
            elem.tail = indent
        last = None
        for child in elem:
            _indent_xml(child, level + 1)
            last = child
        if last is not None and (not last.tail or not last.tail.strip()):
            last.tail = indent
    else:
        if level and (not elem.tail or not elem.tail.strip()):
            elem.tail = indent


def generate_audio_sbc(entries: list) -> str:
    """Return Audio.sbc XML string from *entries*."""
    root = ET.Element("Definitions")
    root.set("xmlns:xsi", "http://www.w3.org/2001/XMLSchema-instance")
    sounds_el = ET.SubElement(root, "Sounds")

    for e in entries:
        sound = ET.SubElement(sounds_el, "Sound")

        id_el = ET.SubElement(sound, "Id")
        ET.SubElement(id_el, "TypeId").text    = "MyObjectBuilder_AudioDefinition"
        ET.SubElement(id_el, "SubtypeId").text = e["subtype_id"]

        ET.SubElement(sound, "Category").text       = e["category"]
        ET.SubElement(sound, "MaxDistance").text     = str(e["max_distance"])
        ET.SubElement(sound, "Volume").text          = str(e["volume"])
        ET.SubElement(sound, "Pitch").text           = "0"
        ET.SubElement(sound, "VolumeVariation").text = str(e["volume_variation"])
        ET.SubElement(sound, "PitchVariation").text  = str(e["pitch_variation"])
        ET.SubElement(sound, "SoundLimit").text      = str(e["sound_limit"])

        waves_el = ET.SubElement(sound, "Waves")
        wave_el  = ET.SubElement(waves_el, "Wave")
        wave_el.set("Type", e["wave_type"])

        lt        = e["loop_type"]
        file_path = e["file_path_in_mod"]

        if lt == "start_loop_end":
            ET.SubElement(wave_el, "Start").text = e["start_path"] or file_path
            ET.SubElement(wave_el, "Loop").text  = e["loop_path"]  or file_path
            ET.SubElement(wave_el, "End").text   = e["end_path"]   or file_path
        elif lt == "simple":
            ET.SubElement(wave_el, "Loop").text = e["loop_path"] or file_path
        else:
            ET.SubElement(wave_el, "Loop").text = file_path

        ET.SubElement(sound, "StreamSound").text = "true" if e["stream_sound"] else "false"

        if e["prevent_sync"]:
            ET.SubElement(sound, "PreventSynchronization").text = "true"

        if e["category"] == "Music":
            if e["music_category"]:
                ET.SubElement(sound, "MusicCategory").text = e["music_category"]
            if e["transition_category"]:
                ET.SubElement(sound, "TransitionCategory").text = e["transition_category"]

    _indent_xml(root)
    return '<?xml version="1.0"?>\n' + ET.tostring(root, encoding="unicode")


def generate_soundblock_sbc(entries: list) -> str:
    """Return SoundBlock.sbc XML for Category=Sb entries, or '' if none."""
    sb_entries = [e for e in entries if e["category"] == "Sb"]
    if not sb_entries:
        return ""

    root = ET.Element("Definitions")
    root.set("xmlns:xsi", "http://www.w3.org/2001/XMLSchema-instance")
    blocks_el = ET.SubElement(root, "SoundBlocks")

    # Group by sb_category_id
    cats: dict = {}
    for e in sb_entries:
        cats.setdefault(e["sb_category_id"], []).append(e)

    for cat_id, sounds in cats.items():
        cat_el = ET.SubElement(blocks_el, "SoundCategory")

        id_el = ET.SubElement(cat_el, "Id")
        ET.SubElement(id_el, "TypeId").text    = "MySoundCategoryDefinition"
        ET.SubElement(id_el, "SubtypeId").text = cat_id

        sounds_el = ET.SubElement(cat_el, "Sounds")
        for e in sounds:
            s = ET.SubElement(sounds_el, "Sound")
            ET.SubElement(s, "Id").text   = e["subtype_id"]
            ET.SubElement(s, "Name").text = e["display_name"]

    _indent_xml(root)
    return '<?xml version="1.0"?>\n' + ET.tostring(root, encoding="unicode")


# ---------------------------------------------------------------------------
# XML Syntax Highlighter
# ---------------------------------------------------------------------------

def apply_xml_highlighting(widget: tk.Text) -> None:
    """Apply syntax-colour tags to XML in *widget* (widget must be in normal state)."""
    for tag in ("xml_tag", "xml_attr_val", "xml_decl", "xml_comment"):
        widget.tag_remove(tag, "1.0", "end")

    content = widget.get("1.0", "end")

    def _tag_all(pattern: str, tag: str):
        for m in re.finditer(pattern, content, re.DOTALL):
            widget.tag_add(tag, f"1.0+{m.start()}c", f"1.0+{m.end()}c")

    _tag_all(r'<\?xml[^?]*\?>', "xml_decl")
    _tag_all(r'<!--.*?-->', "xml_comment")
    _tag_all(r'</?\w[\w.:]*', "xml_tag")
    _tag_all(r'"[^"]*"', "xml_attr_val")

    widget.tag_configure("xml_tag",      foreground=T.CYAN)
    widget.tag_configure("xml_attr_val", foreground=T.GREEN)
    widget.tag_configure("xml_decl",     foreground=T.MUTED)
    widget.tag_configure("xml_comment",  foreground=T.MUTED)


# ---------------------------------------------------------------------------
# Main Screen
# ---------------------------------------------------------------------------

class SBCScreen(ttk.Frame):

    def __init__(self, parent, app):
        super().__init__(parent, style="TFrame")
        self._app = app

        self._entries: list = []   # list of file dicts
        self._selected_idx  = None # int | None

        self._build_ui()

    # =======================================================================
    # UI Construction
    # =======================================================================

    def _build_ui(self):
        T.build_header(
            self,
            title="AUDIO TO SBC",
            subtitle="Generate AudioDefinition and SoundBlock SBC files for SE mods.",
            back_cb=lambda: self._app.show_screen("home"),
        )
        T.separator(self, pady=(8, 8))

        # Main body row
        body = ttk.Frame(self, style="TFrame")
        body.pack(fill="both", expand=False, padx=16)

        self._build_left_panel(body)
        self._build_right_panel(body)

        T.separator(self, pady=(10, 8))

        # Hero generate button
        gen_frame = ttk.Frame(self, style="TFrame")
        gen_frame.pack(pady=(0, 8))
        self._btn_generate = T.hero_button(
            gen_frame,
            "  \u25b6\u25b6  GENERATE SBC  \u25b6\u25b6  ",
            self._on_generate,
        )
        self._btn_generate.pack()

        T.separator(self, pady=(8, 6))

        self._build_output_area()

    # -----------------------------------------------------------------------
    # Left panel — file list
    # -----------------------------------------------------------------------

    def _build_left_panel(self, parent):
        left = tk.Frame(parent, bg=T.BG, width=280)
        left.pack(side="left", fill="y", padx=(0, 8))
        left.pack_propagate(False)

        tk.Label(left, text="\u25a3  AUDIO FILES",
                 bg=T.BG, fg=T.CYAN,
                 font=("Courier New", 10, "bold")).pack(anchor="w", pady=(0, 4))

        # Drag-drop zone
        dnd_text = (
            "Drop audio files here  (.wav  .xwm  .mp3  .ogg  .flac  \u2026)"
            if _HAS_DND else
            "drag-drop not available \u2014 use ADD FILES button"
        )
        self._dnd_zone = tk.Label(
            left,
            text=dnd_text,
            bg=T.PANEL, fg=T.MUTED,
            font=("Courier New", 8),
            relief="flat", bd=0, pady=10,
            wraplength=256, justify="center",
            highlightthickness=1,
            highlightbackground=T.BORDER,
        )
        self._dnd_zone.pack(fill="x", pady=(0, 4))

        if _HAS_DND:
            self._dnd_zone.drop_target_register(DND_FILES)  # type: ignore[name-defined]
            self._dnd_zone.dnd_bind("<<Drop>>", self._on_dnd_drop)
            self._dnd_zone.bind(
                "<Enter>",
                lambda _e: self._dnd_zone.config(highlightbackground=T.CYAN))
            self._dnd_zone.bind(
                "<Leave>",
                lambda _e: self._dnd_zone.config(highlightbackground=T.BORDER))

        # Treeview: File | Format | Duration
        tree_frame = tk.Frame(left, bg=T.BG)
        tree_frame.pack(fill="both", expand=True)

        cols = ("name", "fmt", "dur")
        self._tree = ttk.Treeview(
            tree_frame, columns=cols, show="headings",
            style="SE.Treeview", selectmode="browse", height=8)
        self._tree.heading("name", text="File")
        self._tree.heading("fmt",  text="Fmt")
        self._tree.heading("dur",  text="Duration")
        self._tree.column("name", width=146, minwidth=80,  stretch=True)
        self._tree.column("fmt",  width=44,  minwidth=36,  stretch=False, anchor="center")
        self._tree.column("dur",  width=70,  minwidth=56,  stretch=False, anchor="e")

        # Format badge colours
        self._tree.tag_configure("wav",  foreground=T.GREEN)
        self._tree.tag_configure("xwm",  foreground=T.CYAN)
        self._tree.tag_configure("warn", foreground=T.ORANGE)
        self._tree.tag_configure("bad",  foreground=T.RED)

        tree_vsb = ttk.Scrollbar(tree_frame, orient="vertical",
                                 command=self._tree.yview,
                                 style="SE.Vertical.TScrollbar")
        self._tree.configure(yscrollcommand=tree_vsb.set)
        self._tree.pack(side="left", fill="both", expand=True)
        tree_vsb.pack(side="right", fill="y")

        self._tree.bind("<<TreeviewSelect>>", self._on_tree_select)

        # Buttons
        btn_row = tk.Frame(left, bg=T.BG)
        btn_row.pack(fill="x", pady=(6, 0))
        for txt, cmd in [("ADD FILES",  self._on_add_files),
                         ("REMOVE",     self._on_remove_file),
                         ("CLEAR ALL",  self._on_clear_all)]:
            ttk.Button(btn_row, text=txt, command=cmd,
                       style="SE.TButton").pack(side="left", padx=(0, 4))

        self._file_count_var = tk.StringVar(value="No files loaded")
        tk.Label(left, textvariable=self._file_count_var,
                 bg=T.BG, fg=T.MUTED,
                 font=("Courier New", 8)).pack(anchor="w", pady=(4, 0))

    # -----------------------------------------------------------------------
    # Right panel — scrollable settings
    # -----------------------------------------------------------------------

    def _build_right_panel(self, parent):
        right_outer = tk.Frame(parent, bg=T.BG)
        right_outer.pack(side="left", fill="both", expand=True)

        tk.Label(right_outer, text="\u25a3  SETTINGS  (selected file)",
                 bg=T.BG, fg=T.CYAN,
                 font=("Courier New", 10, "bold")).pack(anchor="w", pady=(0, 4))

        canvas = tk.Canvas(right_outer, bg=T.BG, bd=0, highlightthickness=0,
                           height=310)
        vsb = ttk.Scrollbar(right_outer, orient="vertical",
                            command=canvas.yview,
                            style="SE.Vertical.TScrollbar")
        canvas.configure(yscrollcommand=vsb.set)

        vsb.pack(side="right", fill="y")
        canvas.pack(side="left", fill="both", expand=True)

        self._settings_frame = ttk.Frame(canvas, style="TFrame")
        win_id = canvas.create_window((0, 0), window=self._settings_frame, anchor="nw")

        def _on_resize(e):
            canvas.itemconfig(win_id, width=e.width)

        def _on_frame_configure(_e):
            canvas.configure(scrollregion=canvas.bbox("all"))

        def _on_wheel(e):
            canvas.yview_scroll(int(-1 * (e.delta / 120)), "units")

        canvas.bind("<Configure>", _on_resize)
        self._settings_frame.bind("<Configure>", _on_frame_configure)
        canvas.bind("<Enter>", lambda _e: canvas.bind_all("<MouseWheel>", _on_wheel))
        canvas.bind("<Leave>", lambda _e: canvas.unbind_all("<MouseWheel>"))

        self._build_settings_widgets(self._settings_frame)

    def _build_settings_widgets(self, parent):
        # Placeholder shown when no file is selected
        self._settings_placeholder = tk.Label(
            parent,
            text="Select a file from the list to edit its settings.",
            bg=T.BG, fg=T.MUTED, font=("Courier New", 9))
        self._settings_placeholder.pack(anchor="w", pady=(8, 0), padx=8)

        # ── IDENTITY ────────────────────────────────────────────────────────
        self._sect_identity = self._section_frame(parent, "IDENTITY")
        self._var_subtype   = tk.StringVar()
        self._var_display   = tk.StringVar()
        self._var_file_path = tk.StringVar()
        self._make_entry_row(self._sect_identity, "SubtypeId:",
                             self._var_subtype)
        self._make_entry_row(self._sect_identity, "Display Name:",
                             self._var_display)
        self._make_entry_row(self._sect_identity, "File Path in Mod:",
                             self._var_file_path,
                             note="e.g. Audio\\MyMod\\sound.wav")

        # ── PLAYBACK ────────────────────────────────────────────────────────
        self._sect_playback = self._section_frame(parent, "PLAYBACK")
        self._var_category  = tk.StringVar(value="Sb")
        self._var_wave_type = tk.StringVar(value="D2 (Stereo/No position)")
        self._var_volume    = tk.DoubleVar(value=1.0)
        self._var_max_dist  = tk.StringVar(value="200")
        self._var_loop_type = tk.StringVar(value="None (single play)")
        self._var_loop_path  = tk.StringVar()
        self._var_start_path = tk.StringVar()
        self._var_end_path   = tk.StringVar()

        self._make_combo_row(self._sect_playback, "Category:",
                             self._var_category, CATEGORIES,
                             self._on_category_change)
        self._make_combo_row(self._sect_playback, "Wave Type:",
                             self._var_wave_type, WAVE_TYPES)

        # Volume slider + numeric entry
        vol_frame = tk.Frame(self._sect_playback, bg=T.BG)
        vol_frame.pack(fill="x", pady=(2, 4), padx=8)
        tk.Label(vol_frame, text="Volume:", bg=T.BG, fg=T.TEXT,
                 font=("Courier New", 9), width=16, anchor="e").pack(side="left")
        tk.Scale(
            vol_frame, variable=self._var_volume,
            from_=0.0, to=2.0, resolution=0.05, orient="horizontal",
            length=155, bg=T.BG, fg=T.TEXT,
            activebackground=T.HOVER, troughcolor=T.PANEL,
            highlightthickness=0, font=("Courier New", 8),
            showvalue=False,
        ).pack(side="left", padx=(4, 4))
        tk.Entry(
            vol_frame, textvariable=self._var_volume,
            bg=T.PANEL, fg=T.TEXT, insertbackground=T.CYAN,
            font=("Courier New", 9), relief="flat",
            highlightthickness=1, highlightbackground=T.BORDER,
            highlightcolor=T.CYAN, width=6,
        ).pack(side="left", ipady=2)

        self._make_entry_row(self._sect_playback, "Max Distance:",
                             self._var_max_dist, note="relevant for D3/3D sounds")
        self._make_combo_row(self._sect_playback, "Loop Type:",
                             self._var_loop_type, LOOP_TYPES,
                             self._on_loop_type_change)

        # Conditional loop path rows (packed/unpacked by _on_loop_type_change)
        self._row_loop_path  = self._make_entry_row(
            self._sect_playback, "Loop File:", self._var_loop_path)
        self._row_start_path = self._make_entry_row(
            self._sect_playback, "Start File:", self._var_start_path)
        self._row_end_path   = self._make_entry_row(
            self._sect_playback, "End File:", self._var_end_path)
        self._row_loop_path.pack_forget()
        self._row_start_path.pack_forget()
        self._row_end_path.pack_forget()

        # ── ADVANCED ────────────────────────────────────────────────────────
        self._sect_advanced = self._section_frame(parent, "ADVANCED")
        self._var_pitch_var = tk.StringVar(value="0")
        self._var_vol_var   = tk.StringVar(value="0")
        self._var_snd_limit = tk.StringVar(value="10")
        self._var_stream    = tk.BooleanVar(value=False)
        self._var_prev_sync = tk.BooleanVar(value=False)
        self._make_entry_row(self._sect_advanced, "Pitch Variation:",  self._var_pitch_var)
        self._make_entry_row(self._sect_advanced, "Volume Variation:", self._var_vol_var)
        self._make_entry_row(self._sect_advanced, "Sound Limit:",      self._var_snd_limit)
        self._make_check_row(self._sect_advanced, "Stream Sound:",     self._var_stream)
        self._make_check_row(self._sect_advanced, "Prevent Sync:",     self._var_prev_sync)

        # ── MUSIC (shown only when Category=Music) ───────────────────────────
        self._sect_music    = self._section_frame(parent, "MUSIC")
        self._var_music_cat = tk.StringVar(value="Space")
        self._var_trans_cat = tk.StringVar()
        self._make_combo_row(self._sect_music, "Music Category:",
                             self._var_music_cat, MUSIC_CATEGORIES)
        self._make_entry_row(self._sect_music, "Transition Cat.:", self._var_trans_cat)
        self._sect_music.pack_forget()

        # ── SOUNDBLOCK (shown only when Category=Sb) ─────────────────────────
        self._sect_sb       = self._section_frame(parent, "SOUNDBLOCK")
        self._var_sb_cat_id = tk.StringVar(value="MyModSounds")
        self._make_entry_row(self._sect_sb, "SB Category ID:", self._var_sb_cat_id,
                             note="SoundCategoryDefinition SubtypeId")
        # Sb is the default; section starts visible once a file is loaded

        # ── APPLY TO ALL ────────────────────────────────────────────────────
        apply_row = tk.Frame(parent, bg=T.BG)
        apply_row.pack(fill="x", padx=8, pady=(10, 4))
        ttk.Button(apply_row, text="APPLY SETTINGS TO ALL FILES",
                   command=self._on_apply_to_all,
                   style="SE.TButton").pack(anchor="w")
        tk.Label(apply_row,
                 text="Copies category, volume, wave type, loop and other\n"
                      "non-identity settings to every file in the list.",
                 bg=T.BG, fg=T.MUTED,
                 font=("Courier New", 7),
                 justify="left").pack(anchor="w", pady=(2, 0))

        # Start with settings hidden (no file selected)
        self._set_settings_visible(False)

    # -----------------------------------------------------------------------
    # Settings widget helpers
    # -----------------------------------------------------------------------

    def _section_frame(self, parent, title: str) -> tk.Frame:
        frame = tk.Frame(parent, bg=T.BG)
        frame.pack(fill="x", pady=(8, 0), padx=4)
        tk.Label(frame, text=f"  {title}",
                 bg=T.BORDER, fg=T.CYAN,
                 font=("Courier New", 8, "bold"),
                 anchor="w").pack(fill="x", pady=(0, 4))
        return frame

    def _make_entry_row(self, parent, label: str, var,
                        note: str = "") -> tk.Frame:
        row = tk.Frame(parent, bg=T.BG)
        row.pack(fill="x", pady=(1, 2), padx=8)
        tk.Label(row, text=label, bg=T.BG, fg=T.TEXT,
                 font=("Courier New", 9), width=16, anchor="e").pack(side="left")
        tk.Entry(
            row, textvariable=var,
            bg=T.PANEL, fg=T.TEXT, insertbackground=T.CYAN,
            font=("Courier New", 9), relief="flat",
            highlightthickness=1, highlightbackground=T.BORDER,
            highlightcolor=T.CYAN, width=26,
        ).pack(side="left", padx=(4, 0), ipady=2)
        if note:
            tk.Label(row, text=note, bg=T.BG, fg=T.MUTED,
                     font=("Courier New", 7)).pack(side="left", padx=(4, 0))
        return row

    def _make_combo_row(self, parent, label: str, var,
                        values: list, callback=None) -> tk.Frame:
        row = tk.Frame(parent, bg=T.BG)
        row.pack(fill="x", pady=(1, 2), padx=8)
        tk.Label(row, text=label, bg=T.BG, fg=T.TEXT,
                 font=("Courier New", 9), width=16, anchor="e").pack(side="left")
        combo = ttk.Combobox(
            row, textvariable=var, values=values,
            state="readonly", width=24, style="SE.TCombobox",
            font=("Courier New", 9))
        combo.pack(side="left", padx=(4, 0))
        if callback:
            combo.bind("<<ComboboxSelected>>", lambda _e: callback())
        return row

    def _make_check_row(self, parent, label: str, var) -> tk.Frame:
        row = tk.Frame(parent, bg=T.BG)
        row.pack(fill="x", pady=(1, 2), padx=8)
        tk.Label(row, text=label, bg=T.BG, fg=T.TEXT,
                 font=("Courier New", 9), width=16, anchor="e").pack(side="left")
        ttk.Checkbutton(row, variable=var,
                        style="SE.TCheckbutton").pack(side="left", padx=(4, 0))
        return row

    def _set_settings_visible(self, visible: bool) -> None:
        if visible:
            self._settings_placeholder.pack_forget()
            self._sect_identity.pack(fill="x", pady=(8, 0), padx=4)
            self._sect_playback.pack(fill="x", pady=(8, 0), padx=4)
            self._sect_advanced.pack(fill="x", pady=(8, 0), padx=4)
            self._on_category_change()
            self._on_loop_type_change()
        else:
            self._settings_placeholder.pack(anchor="w", pady=(8, 0), padx=8)
            for s in (self._sect_identity, self._sect_playback,
                      self._sect_advanced, self._sect_music, self._sect_sb):
                s.pack_forget()

    # -----------------------------------------------------------------------
    # Output area
    # -----------------------------------------------------------------------

    def _build_output_area(self):
        out_frame = ttk.Frame(self, style="TFrame")
        out_frame.pack(fill="both", expand=True, padx=16, pady=(0, 10))

        tk.Label(out_frame, text="\u25a3  OUTPUT",
                 bg=T.BG, fg=T.CYAN,
                 font=("Courier New", 10, "bold")).pack(anchor="w", pady=(0, 4))

        # Custom tab strip
        tab_strip = tk.Frame(out_frame, bg=T.PANEL)
        tab_strip.pack(fill="x")

        self._tab_audio_btn = self._make_tab_btn(
            tab_strip, "Audio.sbc",       lambda: self._switch_tab("audio"))
        self._tab_sb_btn    = self._make_tab_btn(
            tab_strip, "SoundBlock.sbc",  lambda: self._switch_tab("soundblock"))
        self._tab_audio_btn.pack(side="left")
        self._tab_sb_btn.pack(side="left")

        self._out_audio_frame = self._build_output_tab(out_frame, "audio")
        self._out_sb_frame    = self._build_output_tab(out_frame, "soundblock")

        self._switch_tab("audio")

    def _make_tab_btn(self, parent, text: str, command) -> tk.Button:
        return tk.Button(
            parent, text=text, command=command,
            bg=T.PANEL, fg=T.MUTED,
            activebackground=T.HOVER, activeforeground=T.CYAN,
            font=("Courier New", 9), relief="flat", bd=0,
            padx=12, pady=4, cursor="hand2",
        )

    def _build_output_tab(self, parent, tab_id: str) -> tk.Frame:
        frame = tk.Frame(parent, bg=T.BG)

        txt = tk.Text(
            frame,
            bg=T.PANEL, fg=T.TEXT,
            font=("Courier New", 9),
            relief="flat", bd=0,
            state="disabled", height=11,
            wrap="none", highlightthickness=1,
            highlightbackground=T.BORDER,
            selectbackground=T.BLUE,
        )
        vsb = ttk.Scrollbar(frame, orient="vertical",
                            command=txt.yview,
                            style="SE.Vertical.TScrollbar")
        hsb = ttk.Scrollbar(frame, orient="horizontal", command=txt.xview)
        txt.configure(yscrollcommand=vsb.set, xscrollcommand=hsb.set)
        vsb.pack(side="right", fill="y")
        txt.pack(side="top", fill="both", expand=True)
        hsb.pack(side="bottom", fill="x")

        btn_row = tk.Frame(frame, bg=T.BG)
        btn_row.pack(fill="x", pady=(4, 0))
        ttk.Button(btn_row, text="COPY TO CLIPBOARD",
                   command=lambda: self._on_copy(txt),
                   style="SE.TButton").pack(side="left", padx=(0, 8))
        ttk.Button(btn_row, text="SAVE AS...",
                   command=lambda: self._on_save(txt, tab_id),
                   style="SE.TButton").pack(side="left")

        if tab_id == "audio":
            self._out_audio_txt = txt
        else:
            self._out_sb_txt = txt

        return frame

    def _switch_tab(self, tab: str) -> None:
        if tab == "audio":
            self._out_audio_frame.pack(fill="both", expand=True)
            self._out_sb_frame.pack_forget()
            self._tab_audio_btn.config(fg=T.CYAN, bg=T.HOVER)
            self._tab_sb_btn.config(fg=T.MUTED,   bg=T.PANEL)
        else:
            self._out_sb_frame.pack(fill="both", expand=True)
            self._out_audio_frame.pack_forget()
            self._tab_audio_btn.config(fg=T.MUTED,  bg=T.PANEL)
            self._tab_sb_btn.config(fg=T.CYAN,   bg=T.HOVER)

    # =======================================================================
    # File list helpers
    # =======================================================================

    def _refresh_tree(self) -> None:
        self._tree.delete(*self._tree.get_children())
        for e in self._entries:
            dur = e["duration"]
            dur_str = f"{dur:.1f}s" if dur > 0 else "\u2014"
            fmt = e["format"]

            if fmt == "wav":
                tag = "wav"
            elif fmt == "xwm":
                tag = "xwm"
            elif ("." + fmt) in WARN_EXTS:
                tag = "warn"
            else:
                tag = "bad"

            name = e["filename"]
            if len(name) > 22:
                name = name[:19] + "..."

            self._tree.insert("", "end",
                              values=(name, fmt.upper(), dur_str),
                              tags=(tag,))

        count = len(self._entries)
        self._file_count_var.set(
            "No files loaded" if count == 0 else
            f"{count} file{'s' if count != 1 else ''} loaded"
        )

    def _add_paths(self, paths: list) -> None:
        added = 0
        for p in paths:
            path = Path(p)
            if any(e["path"] == path for e in self._entries):
                continue
            entry = _default_file_entry(path)
            if path.suffix.lower() == ".wav":
                _probe_wav(entry)
            self._entries.append(entry)
            added += 1

        self._refresh_tree()

        if added and self._selected_idx is None and self._entries:
            children = self._tree.get_children()
            if children:
                self._tree.selection_set(children[0])
                self._on_tree_select()

    # =======================================================================
    # Settings load / save
    # =======================================================================

    def _load_settings_from_entry(self, idx: int) -> None:
        e = self._entries[idx]
        self._var_subtype.set(e["subtype_id"])
        self._var_display.set(e["display_name"])
        self._var_file_path.set(e["file_path_in_mod"])

        wt = next((k for k, v in _WAVE_TYPE_MAP.items() if v == e["wave_type"]),
                  WAVE_TYPES[0])
        lt = next((k for k, v in _LOOP_TYPE_MAP.items() if v == e["loop_type"]),
                  LOOP_TYPES[0])

        self._var_category.set(e["category"])
        self._var_wave_type.set(wt)
        self._var_volume.set(e["volume"])
        self._var_max_dist.set(str(e["max_distance"]))
        self._var_loop_type.set(lt)
        self._var_loop_path.set(e["loop_path"])
        self._var_start_path.set(e["start_path"])
        self._var_end_path.set(e["end_path"])
        self._var_pitch_var.set(str(e["pitch_variation"]))
        self._var_vol_var.set(str(e["volume_variation"]))
        self._var_snd_limit.set(str(e["sound_limit"]))
        self._var_stream.set(e["stream_sound"])
        self._var_prev_sync.set(e["prevent_sync"])
        self._var_music_cat.set(e["music_category"])
        self._var_trans_cat.set(e["transition_category"])
        self._var_sb_cat_id.set(e["sb_category_id"])

        self._on_category_change()
        self._on_loop_type_change()

    def _save_settings_to_entry(self, idx: int) -> None:
        e = self._entries[idx]
        e["subtype_id"]       = self._var_subtype.get().strip()
        e["display_name"]     = self._var_display.get().strip()
        e["file_path_in_mod"] = self._var_file_path.get().strip()
        e["category"]         = self._var_category.get()
        e["wave_type"]        = _WAVE_TYPE_MAP.get(self._var_wave_type.get(), "D2")

        try:    e["volume"] = float(self._var_volume.get())
        except: e["volume"] = 1.0  # noqa: E722

        try:    e["max_distance"] = int(self._var_max_dist.get())
        except: e["max_distance"] = 200  # noqa: E722

        e["loop_type"]  = _LOOP_TYPE_MAP.get(self._var_loop_type.get(), "none")
        e["loop_path"]  = self._var_loop_path.get().strip()
        e["start_path"] = self._var_start_path.get().strip()
        e["end_path"]   = self._var_end_path.get().strip()

        try:    e["pitch_variation"] = float(self._var_pitch_var.get())
        except: e["pitch_variation"] = 0.0  # noqa: E722

        try:    e["volume_variation"] = float(self._var_vol_var.get())
        except: e["volume_variation"] = 0.0  # noqa: E722

        try:    e["sound_limit"] = int(self._var_snd_limit.get())
        except: e["sound_limit"] = 10  # noqa: E722

        e["stream_sound"]        = bool(self._var_stream.get())
        e["prevent_sync"]        = bool(self._var_prev_sync.get())
        e["music_category"]      = self._var_music_cat.get()
        e["transition_category"] = self._var_trans_cat.get().strip()
        e["sb_category_id"]      = self._var_sb_cat_id.get().strip()

    def _collect_non_identity(self) -> dict:
        def _f(v, d):
            try:    return float(v)
            except: return d  # noqa: E722

        def _i(v, d):
            try:    return int(v)
            except: return d  # noqa: E722

        return {
            "category":          self._var_category.get(),
            "wave_type":         _WAVE_TYPE_MAP.get(self._var_wave_type.get(), "D2"),
            "volume":            _f(self._var_volume.get(), 1.0),
            "max_distance":      _i(self._var_max_dist.get(), 200),
            "loop_type":         _LOOP_TYPE_MAP.get(self._var_loop_type.get(), "none"),
            "loop_path":         self._var_loop_path.get().strip(),
            "start_path":        self._var_start_path.get().strip(),
            "end_path":          self._var_end_path.get().strip(),
            "pitch_variation":   _f(self._var_pitch_var.get(), 0.0),
            "volume_variation":  _f(self._var_vol_var.get(), 0.0),
            "sound_limit":       _i(self._var_snd_limit.get(), 10),
            "stream_sound":      bool(self._var_stream.get()),
            "prevent_sync":      bool(self._var_prev_sync.get()),
            "music_category":    self._var_music_cat.get(),
            "transition_category": self._var_trans_cat.get().strip(),
            "sb_category_id":    self._var_sb_cat_id.get().strip(),
        }

    # =======================================================================
    # Event handlers
    # =======================================================================

    def _on_add_files(self) -> None:
        paths = filedialog.askopenfilenames(
            title="Select audio files",
            filetypes=[
                ("Audio files", "*.wav *.xwm *.mp3 *.ogg *.flac *.aac *.m4a *.wma *.aiff *.aif *.opus"),
                ("WAV / XWM (SE native)", "*.wav *.xwm"),
                ("MP3 / OGG / FLAC",      "*.mp3 *.ogg *.flac"),
                ("All files",             "*.*"),
            ],
        )
        if paths:
            self._add_paths(list(paths))

    def _on_remove_file(self) -> None:
        sel = self._tree.selection()
        if not sel:
            return
        if self._selected_idx is not None:
            self._save_settings_to_entry(self._selected_idx)
        idx = self._tree.index(sel[0])
        self._entries.pop(idx)
        self._selected_idx = None
        self._refresh_tree()
        self._set_settings_visible(False)

    def _on_clear_all(self) -> None:
        if not self._entries:
            return
        if T.themed_askokcancel(self.winfo_toplevel(),
                                "Clear All Files", "Remove all loaded files?"):
            self._entries.clear()
            self._selected_idx = None
            self._refresh_tree()
            self._set_settings_visible(False)

    def _on_dnd_drop(self, event) -> None:
        import shlex
        raw = event.data
        try:
            paths = shlex.split(raw)
        except ValueError:
            raw   = raw.replace("{", "").replace("}", "")
            paths = raw.split()
        self._add_paths(paths)

    def _on_tree_select(self, _event=None) -> None:
        sel = self._tree.selection()
        if not sel:
            return
        if self._selected_idx is not None:
            self._save_settings_to_entry(self._selected_idx)
        idx = self._tree.index(sel[0])
        self._selected_idx = idx
        self._set_settings_visible(True)
        self._load_settings_from_entry(idx)

    def _on_category_change(self) -> None:
        cat = self._var_category.get()
        if cat == "Music":
            self._var_stream.set(True)
            self._sect_music.pack(fill="x", pady=(8, 0), padx=4)
            self._sect_sb.pack_forget()
        elif cat == "Sb":
            self._sect_sb.pack(fill="x", pady=(8, 0), padx=4)
            self._sect_music.pack_forget()
        else:
            self._sect_music.pack_forget()
            self._sect_sb.pack_forget()

    def _on_loop_type_change(self) -> None:
        lt = self._var_loop_type.get()
        self._row_loop_path.pack_forget()
        self._row_start_path.pack_forget()
        self._row_end_path.pack_forget()
        if lt == "Simple loop (Loop only)":
            self._row_loop_path.pack(fill="x", pady=(1, 2), padx=8)
        elif lt == "Start/Loop/End":
            self._row_start_path.pack(fill="x", pady=(1, 2), padx=8)
            self._row_loop_path.pack(fill="x",  pady=(1, 2), padx=8)
            self._row_end_path.pack(fill="x",   pady=(1, 2), padx=8)

    def _on_apply_to_all(self) -> None:
        if not self._entries:
            return
        if self._selected_idx is not None:
            self._save_settings_to_entry(self._selected_idx)
        settings = self._collect_non_identity()
        for e in self._entries:
            e.update(settings)
        T.themed_showinfo(
            self.winfo_toplevel(), "Applied",
            f"Non-identity settings applied to all {len(self._entries)} file(s).")

    def _on_generate(self) -> None:
        if self._selected_idx is not None:
            self._save_settings_to_entry(self._selected_idx)

        if not self._entries:
            T.themed_showinfo(self.winfo_toplevel(), "No Files",
                              "Add at least one audio file before generating.")
            return

        missing = [e["filename"] for e in self._entries
                   if not e["subtype_id"].strip()]
        if missing:
            names = "\n".join(f"  \u2022 {n}" for n in missing[:5])
            if len(missing) > 5:
                names += f"\n  \u2026 and {len(missing) - 5} more"
            T.themed_showinfo(
                self.winfo_toplevel(), "Missing SubtypeId",
                f"The following files have no SubtypeId:\n\n{names}\n\n"
                "Set a SubtypeId for each file before generating.")
            return

        audio_xml = generate_audio_sbc(self._entries)
        sb_xml    = generate_soundblock_sbc(self._entries)

        self._set_output_text(self._out_audio_txt, audio_xml)
        self._set_output_text(
            self._out_sb_txt,
            sb_xml if sb_xml else
            "<!-- No Sb-category files \u2014 SoundBlock.sbc not needed -->")

        self._switch_tab("audio")

    # -----------------------------------------------------------------------

    def _set_output_text(self, widget: tk.Text, content: str) -> None:
        widget.config(state="normal")
        widget.delete("1.0", "end")
        widget.insert("1.0", content)
        apply_xml_highlighting(widget)
        widget.config(state="disabled")

    def _on_copy(self, widget: tk.Text) -> None:
        widget.config(state="normal")
        content = widget.get("1.0", "end-1c")
        widget.config(state="disabled")
        self.clipboard_clear()
        self.clipboard_append(content)

    def _on_save(self, widget: tk.Text, tab_id: str) -> None:
        widget.config(state="normal")
        content = widget.get("1.0", "end-1c")
        widget.config(state="disabled")
        if not content.strip() or content.strip().startswith("<!--"):
            T.themed_showinfo(self.winfo_toplevel(), "Nothing to Save",
                              "Generate SBC output first.")
            return
        default_name = "Audio.sbc" if tab_id == "audio" else "SoundBlock.sbc"
        path = filedialog.asksaveasfilename(
            title="Save SBC file",
            defaultextension=".sbc",
            initialfile=default_name,
            filetypes=[("SBC files", "*.sbc"),
                       ("XML files", "*.xml"),
                       ("All files", "*.*")],
        )
        if path:
            try:
                Path(path).write_text(content, encoding="utf-8")
            except OSError as exc:
                T.themed_showinfo(self.winfo_toplevel(), "Save Error",
                                  f"Could not save file:\n{exc}")
