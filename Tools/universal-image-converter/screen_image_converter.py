#!/usr/bin/env python3
"""
screen_image_converter.py — Image Converter screen for the SE Tools launcher.

Ported from universal-image-converter/se_lcd_gui.py into a ttk.Frame so it
can be embedded in the launcher while se_lcd_gui.py continues to work
standalone.
"""

import queue
import threading
from pathlib import Path

import tkinter as tk
from tkinter import filedialog, ttk

import se_theme as T

# ---------------------------------------------------------------------------
# Import the CLI conversion module (same folder)
# ---------------------------------------------------------------------------
try:
    from se_lcd_convert import (
        convert_image,
        SUPPORTED_EXTS,
        DEFAULT_MAX_SIZE,
        SCREEN_PRESETS,
        PRESET_NAMES,
        PRESET_DISPLAY_NAMES,
        PRESET_DISPLAY_MAP,
        SCREEN_REFERENCE_DATA,
        get_preset,
        mip_count,
        _detect_texconv,
        _detect_wand,
        _check_pillow,
    )
    _IMPORT_OK = True
    _IMPORT_ERR = ""
except ImportError as e:
    _IMPORT_OK = False
    _IMPORT_ERR = str(e)

_PRESET_DISPLAY_NAMES = PRESET_DISPLAY_NAMES if _IMPORT_OK else []
_PRESET_DISPLAY_MAP   = PRESET_DISPLAY_MAP   if _IMPORT_OK else {}


# ---------------------------------------------------------------------------
# ---------------------------------------------------------------------------
# Main screen frame
# ---------------------------------------------------------------------------

DEFAULT_WIDTH  = "1024"
DEFAULT_HEIGHT = "1024"


class ImageConverterScreen(ttk.Frame):

    def __init__(self, parent, app):
        super().__init__(parent, style="TFrame")
        self._app = app

        # State
        self._files: list[Path]          = []
        self._out_folder: Path | None    = None
        self._converting                 = False
        self._q: queue.Queue             = queue.Queue()
        self._ref_window                 = None

        if _IMPORT_OK:
            self._has_pillow  = _check_pillow()
            self._use_texconv = _detect_texconv() if self._has_pillow else None
            self._use_wand    = _detect_wand()    if self._has_pillow else False
        else:
            self._has_pillow  = False
            self._use_texconv = None
            self._use_wand    = False

        self._build_ui()
        self._log_startup_info()

    # -----------------------------------------------------------------------
    # UI Construction
    # -----------------------------------------------------------------------

    def _build_ui(self):
        pad = dict(padx=16, pady=0)

        T.build_header(
            self,
            title="IMAGE TO DDS",
            subtitle="Convert images to DDS format for Space Engineers LCD mods.",
            back_cb=lambda: self._app.show_screen("home"),
            note=f"Supported: {', '.join(sorted(e.lstrip('.').upper() for e in SUPPORTED_EXTS))}" if _IMPORT_OK else "",
        )
        T.separator(self, pady=(8, 8))

        if not _IMPORT_OK:
            tk.Label(self,
                     text=f"⚠  Could not load se_lcd_convert.py\n\n{_IMPORT_ERR}",
                     bg=T.BG, fg=T.RED,
                     font=("Courier New", 10),
                     justify="left", wraplength=620).pack(padx=24, pady=24, anchor="w")
            return

        # ── Input Images ────────────────────────────────────────────────────
        ttk.Label(self, text="▣  INPUT IMAGES",
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
            height=6, activestyle="none",
        )
        self._lb_scrollbar = ttk.Scrollbar(lb_container, orient="vertical",
                                           command=self._listbox.yview,
                                           style="SE.Vertical.TScrollbar")
        self._listbox.config(yscrollcommand=self._lb_scrollbar.set)
        self._listbox.pack(side="left", fill="both", expand=True)

        self._list_placeholder = tk.Label(
            lb_container, text="No images selected",
            bg=T.PANEL, fg=T.MUTED, font=("Courier New", 9))
        self._list_placeholder.place(relx=0.5, rely=0.5, anchor="center")

        btn_frame = ttk.Frame(list_frame, style="Panel.TFrame")
        btn_frame.pack(side="right", fill="y", padx=8, pady=6)
        self._btn_select = self._se_button(btn_frame, "SELECT", self._on_select, width=8)
        self._btn_select.pack(pady=(0, 6))
        self._btn_clear  = self._se_button(btn_frame, "CLEAR",  self._on_clear,  width=8)
        self._btn_clear.pack()

        self._file_count_var = tk.StringVar(value="")
        ttk.Label(self, textvariable=self._file_count_var,
                  style="Muted.TLabel").pack(anchor="e", padx=18, pady=(2, 0))

        T.separator(self, pady=(8, 8))

        # ── Output Settings ──────────────────────────────────────────────────
        ttk.Label(self, text="▣  OUTPUT SETTINGS",
                  style="Section.TLabel").pack(anchor="w", **pad)

        settings_frame = ttk.Frame(self, style="TFrame")
        settings_frame.pack(fill="x", padx=16, pady=(6, 0))
        settings_frame.columnconfigure(0, minsize=115)
        settings_frame.columnconfigure(1, weight=1)

        # Row 0: File Rename
        ttk.Label(settings_frame, text="File Rename:", style="TLabel").grid(
            row=0, column=0, sticky="w", pady=(0, 6))
        rename_ctrl = ttk.Frame(settings_frame, style="TFrame")
        rename_ctrl.grid(row=0, column=1, sticky="w", pady=(0, 6))

        self._affix_mode_var = tk.StringVar(value="None")
        affix_combo = ttk.Combobox(
            rename_ctrl, textvariable=self._affix_mode_var,
            values=["None", "Add Prefix", "Add Suffix"],
            state="readonly", width=12, style="SE.TCombobox",
        )
        affix_combo.pack(side="left", padx=(0, 8))
        affix_combo.bind("<<ComboboxSelected>>", self._on_affix_change)

        self._affix_text_var = tk.StringVar(value="")
        self._affix_entry = tk.Entry(
            rename_ctrl,
            textvariable=self._affix_text_var,
            bg=T.PANEL, fg=T.TEXT,
            insertbackground=T.CYAN,
            disabledbackground=T.BG, disabledforeground=T.MUTED,
            font=("Courier New", 9),
            relief="flat", bd=1,
            highlightthickness=1,
            highlightbackground=T.BORDER, highlightcolor=T.CYAN,
            width=16, state="disabled",
        )
        self._affix_entry.pack(side="left", ipady=3)

        # Row 1: Screen Target
        ttk.Label(settings_frame, text="Screen Target:", style="TLabel").grid(
            row=1, column=0, sticky="e", padx=(0, 8), pady=(0, 6))
        screen_ctrl = ttk.Frame(settings_frame, style="TFrame")
        screen_ctrl.grid(row=1, column=1, sticky="w", pady=(0, 6))

        self._screen_var = tk.StringVar(value=_PRESET_DISPLAY_NAMES[0])
        screen_combo = ttk.Combobox(
            screen_ctrl, textvariable=self._screen_var,
            values=_PRESET_DISPLAY_NAMES, state="readonly",
            width=30, style="SE.TCombobox", font=T.FONT_LABEL,
        )
        screen_combo.pack(side="left", padx=(0, 6))
        screen_combo.bind("<<ComboboxSelected>>", self._on_screen_change)

        self._btn_info = ttk.Button(screen_ctrl, text="ⓘ",
                                    command=self._on_open_reference,
                                    style="Info.TButton")
        self._btn_info.pack(side="left")

        # Row 2: Custom controls (hidden unless "Custom" selected)
        self._custom_row = ttk.Frame(settings_frame, style="TFrame")

        ttk.Label(self._custom_row, text="Width:", style="TLabel").pack(side="left")
        self._width_var = tk.StringVar(value=DEFAULT_WIDTH)
        self._width_entry = tk.Entry(
            self._custom_row, textvariable=self._width_var,
            bg=T.PANEL, fg=T.TEXT,
            insertbackground=T.CYAN,
            font=("Courier New", 9),
            relief="flat", bd=1,
            highlightthickness=1,
            highlightbackground=T.BORDER, highlightcolor=T.CYAN,
            width=6,
        )
        self._width_entry.pack(side="left", padx=(6, 2), ipady=3)
        ttk.Label(self._custom_row, text="px", style="Muted.TLabel").pack(side="left", padx=(0, 16))

        ttk.Label(self._custom_row, text="Height:", style="TLabel").pack(side="left")
        self._height_var = tk.StringVar(value=DEFAULT_HEIGHT)
        self._height_entry = tk.Entry(
            self._custom_row, textvariable=self._height_var,
            bg=T.PANEL, fg=T.TEXT,
            insertbackground=T.CYAN,
            font=("Courier New", 9),
            relief="flat", bd=1,
            highlightthickness=1,
            highlightbackground=T.BORDER, highlightcolor=T.CYAN,
            width=6,
        )
        self._height_entry.pack(side="left", padx=(6, 2), ipady=3)
        ttk.Label(self._custom_row, text="px", style="Muted.TLabel").pack(side="left", padx=(0, 20))

        self._aspect_var = tk.BooleanVar(value=True)
        self._aspect_cb = ttk.Checkbutton(
            self._custom_row, text="Preserve Aspect Ratio",
            variable=self._aspect_var,
            command=self._on_aspect_toggle,
            style="SE.TCheckbutton",
        )
        self._aspect_cb.pack(side="left")

        # Lock height to width when aspect ratio is preserved
        self._width_var.trace_add("write", self._on_width_changed)

        # Row 3: Output Folder
        ttk.Label(settings_frame, text="Output Folder:", style="TLabel").grid(
            row=3, column=0, sticky="e", padx=(0, 8))
        outfolder_ctrl = ttk.Frame(settings_frame, style="TFrame")
        outfolder_ctrl.grid(row=3, column=1, sticky="ew")

        self._outfolder_var = tk.StringVar(value="same as source file")
        outfolder_entry = tk.Entry(
            outfolder_ctrl,
            textvariable=self._outfolder_var,
            state="readonly",
            readonlybackground=T.PANEL,
            fg=T.MUTED, font=("Courier New", 9),
            relief="flat", bd=1,
            highlightthickness=1,
            highlightbackground=T.BORDER, highlightcolor=T.CYAN,
            width=33,
        )
        outfolder_entry.pack(side="left", padx=(0, 8), ipady=3)

        self._btn_browse    = self._se_button(outfolder_ctrl, "BROWSE...", self._on_browse, width=10)
        self._btn_browse.pack(side="left", padx=(0, 6))
        self._btn_clear_out = self._se_button(outfolder_ctrl, "RESET", self._on_reset_outfolder, width=7)
        self._btn_clear_out.pack(side="left")

        T.separator(self, pady=(14, 10))

        # ── Convert Button ────────────────────────────────────────────────────
        convert_frame = ttk.Frame(self, style="TFrame")
        convert_frame.pack(pady=(0, 10))
        self._btn_convert = T.hero_button(convert_frame,
                                          "  ▶  CONVERT  ▶  ",
                                          self._on_convert)
        self._btn_convert.pack()

        T.separator(self, pady=(10, 8))

        # ── Progress ──────────────────────────────────────────────────────────
        progress_frame = ttk.Frame(self, style="TFrame")
        progress_frame.pack(fill="x", padx=16, pady=(0, 4))

        self._progress_var = tk.DoubleVar(value=0.0)
        self._progressbar  = ttk.Progressbar(
            progress_frame, variable=self._progress_var,
            maximum=100.0, mode="determinate",
            style="SE.Horizontal.TProgressbar", length=520,
        )
        self._progressbar.pack(side="left", fill="x", expand=True, pady=2)

        self._pct_var = tk.StringVar(value="  0%")
        ttk.Label(progress_frame, textvariable=self._pct_var,
                  style="Muted.TLabel", width=5).pack(side="left", padx=(8, 0))

        self._status_var = tk.StringVar(value="")
        ttk.Label(self, textvariable=self._status_var,
                  style="Muted.TLabel").pack(anchor="w", padx=18, pady=(0, 6))

        # ── Log ───────────────────────────────────────────────────────────────
        log_frame = ttk.Frame(self, style="Panel.TFrame")
        log_frame.pack(fill="both", expand=True, padx=16, pady=(0, 14))

        log_header = ttk.Frame(log_frame, style="Panel.TFrame")
        log_header.pack(fill="x", padx=6, pady=(4, 2))
        ttk.Label(log_header, text="▣  CONVERSION LOG",
                  style="Section.TLabel", background=T.PANEL).pack(side="left")
        tk.Button(
            log_header, text="CLEAR LOG",
            command=self._clear_log,
            bg=T.PANEL, fg=T.MUTED,
            activebackground=T.HOVER, activeforeground=T.TEXT,
            font=("Courier New", 8), relief="flat", bd=0, cursor="hand2",
        ).pack(side="right")

        self._log_text = T.log_text_widget(log_frame)
        log_sb = ttk.Scrollbar(log_frame, orient="vertical",
                               command=self._log_text.yview,
                               style="SE.Vertical.TScrollbar")
        self._log_text.config(yscrollcommand=log_sb.set)
        log_sb.pack(side="right", fill="y")
        self._log_text.pack(fill="both", expand=True, padx=6, pady=(0, 6))

    # -----------------------------------------------------------------------
    # Widget helpers
    # -----------------------------------------------------------------------

    def _se_button(self, parent, text, command, width=None):
        kw = {"width": width} if width else {}
        return ttk.Button(parent, text=text, command=command,
                          style="SE.TButton", **kw)

    # -----------------------------------------------------------------------
    # Logging
    # -----------------------------------------------------------------------

    def _log(self, msg: str, tag: str = "info") -> None:
        T.append_log(self._log_text, msg, tag)

    def _log_sep(self) -> None:
        self._log("━" * 38, "sep")

    def _clear_log(self) -> None:
        self._log_text.config(state="normal")
        self._log_text.delete("1.0", "end")
        self._log_text.config(state="disabled")

    def _log_startup_info(self) -> None:
        self._log("  Universal Image Converter", "cyan")
        self._log_sep()
        if not _IMPORT_OK:
            self._log(f"Import error: {_IMPORT_ERR}", "error")
            return
        if self._has_pillow:
            self._log("Pillow ready.", "success")
        else:
            self._log("Pillow not found — run: pip install Pillow", "error")
        if self._use_texconv:
            self._log(f"texconv found: {self._use_texconv}", "success")
        else:
            self._log("texconv not found — BC7 fallback will be used.", "warn")
        if self._use_wand:
            self._log("ImageMagick (Wand) ready.", "success")
        self._log_sep()

    # -----------------------------------------------------------------------
    # Event handlers
    # -----------------------------------------------------------------------

    def _on_select(self) -> None:
        exts = " ".join(f"*{e}" for e in SUPPORTED_EXTS)
        paths = filedialog.askopenfilenames(
            title="Select images",
            filetypes=[("Image files", exts), ("All files", "*.*")],
        )
        if paths:
            for p in paths:
                path = Path(p)
                if path not in self._files:
                    self._files.append(path)
            self._refresh_listbox()

    def _on_clear(self) -> None:
        self._files.clear()
        self._refresh_listbox()

    def _refresh_listbox(self) -> None:
        self._listbox.delete(0, "end")
        if self._files:
            for f in self._files:
                self._listbox.insert("end", f.name)
            self._lb_scrollbar.pack(side="right", fill="y")
            self._list_placeholder.place_forget()
            self._file_count_var.set(
                f"{len(self._files)} file{'s' if len(self._files) != 1 else ''} selected")
        else:
            self._lb_scrollbar.pack_forget()
            self._list_placeholder.place(relx=0.5, rely=0.5, anchor="center")
            self._file_count_var.set("")

    def _on_affix_change(self, _e=None) -> None:
        mode = self._affix_mode_var.get()
        if mode == "None":
            self._affix_entry.config(state="disabled")
        elif mode == "Add Prefix":
            self._affix_entry.config(state="normal")
            self._affix_text_var.set("converted_")
        else:  # Add Suffix
            self._affix_entry.config(state="normal")
            self._affix_text_var.set("_converted")

    def _on_aspect_toggle(self) -> None:
        pass  # aspect ratio is handled by convert_image; both width and height remain editable

    def _on_width_changed(self, *_args) -> None:
        pass  # no height-locking; width and height are always independent

    def _on_screen_change(self, _e=None) -> None:
        display = self._screen_var.get()
        name    = _PRESET_DISPLAY_MAP.get(display, display)
        if name.strip().lower() == "custom":
            self._custom_row.grid(row=2, column=1, sticky="w", pady=(0, 6))
            self._height_entry.config(state="normal")
        else:
            self._custom_row.grid_remove()

    def _on_browse(self) -> None:
        folder = filedialog.askdirectory(title="Select output folder")
        if folder:
            self._out_folder = Path(folder)
            self._outfolder_var.set(str(self._out_folder))

    def _on_reset_outfolder(self) -> None:
        self._out_folder = None
        self._outfolder_var.set("same as source file")

    def _on_open_reference(self) -> None:
        if self._ref_window and self._ref_window.winfo_exists():
            self._ref_window.lift()
        else:
            self._ref_window = T.LCDReferenceWindow(self.winfo_toplevel())

    def _on_convert(self) -> None:
        if self._converting:
            return
        if not self._files:
            T.themed_showinfo(self.winfo_toplevel(), "No Images",
                              "Please select at least one image to convert.")
            return
        if not self._has_pillow:
            T.themed_showinfo(self.winfo_toplevel(), "Pillow Missing",
                              "Pillow is required.\n\nRun: pip install Pillow")
            return

        # Overwrite check
        screen_display = self._screen_var.get()
        screen_name    = _PRESET_DISPLAY_MAP.get(screen_display, screen_display)
        gen_mipmaps    = True
        affix_mode     = self._affix_mode_var.get()
        affix_text     = self._affix_text_var.get() if affix_mode != "None" else ""
        prefix         = affix_text if affix_mode == "Add Prefix" else ""
        suffix         = affix_text if affix_mode == "Add Suffix" else ""
        out_dir        = self._out_folder

        # Check for existing output files
        existing = []
        for f in self._files:
            od  = out_dir or f.parent
            out = od / (prefix + f.stem + suffix + ".dds")
            if out.exists():
                existing.append(out.name)

        if existing:
            names = "\n".join(f"  • {n}" for n in existing[:5])
            if len(existing) > 5:
                names += f"\n  … and {len(existing)-5} more"
            ok = T.themed_askokcancel(
                self.winfo_toplevel(),
                "Overwrite Files",
                f"The following output files already exist:\n\n{names}\n\nOverwrite them?",
            )
            if not ok:
                return

        self._start_conversion(screen_name, gen_mipmaps, prefix, suffix, out_dir)

    def _start_conversion(self, screen_name, gen_mipmaps, prefix, suffix, out_dir):
        self._converting = True
        self._btn_convert.config(state="disabled")
        self._progress_var.set(0)
        self._pct_var.set("  0%")
        self._status_var.set("")

        args = (screen_name, gen_mipmaps, prefix, suffix, out_dir)
        t = threading.Thread(target=self._worker, args=args, daemon=True)
        t.start()
        self._poll_queue()

    def _worker(self, screen_name, gen_mipmaps, prefix, suffix, out_dir):
        files   = list(self._files)
        total   = len(files)
        preset   = get_preset(screen_name)
        c_width  = int(self._width_var.get())  if self._width_var.get().isdigit()  else DEFAULT_MAX_SIZE
        c_height = int(self._height_var.get()) if self._height_var.get().isdigit() else DEFAULT_MAX_SIZE

        for i, f in enumerate(files, 1):
            od = out_dir or f.parent
            self._q.put(("status", f"[{i}/{total}] {f.name}"))
            self._q.put(("log", f"Converting: {f.name}", "cyan"))
            try:
                result = convert_image(
                    f, od, preset, gen_mipmaps,
                    self._use_texconv, self._use_wand,
                    prefix=prefix, suffix=suffix,
                    custom_max_size=c_width,
                    custom_max_height=c_height,
                    custom_preserve_aspect=self._aspect_var.get(),
                )
                self._q.put(("log", f"  ✓ Saved: {f.stem}.dds", "success"))
            except Exception as exc:
                self._q.put(("log", f"  ✗ Error: {exc}", "error"))

            pct = int(i / total * 100)
            self._q.put(("progress", pct))

        self._q.put(("done", total))

    def _poll_queue(self) -> None:
        try:
            while True:
                msg = self._q.get_nowait()
                kind = msg[0]
                if kind == "log":
                    self._log(msg[1], msg[2])
                elif kind == "status":
                    self._status_var.set(msg[1])
                elif kind == "progress":
                    self._progress_var.set(msg[1])
                    self._pct_var.set(f"{msg[1]:3d}%")
                elif kind == "done":
                    self._progress_var.set(100)
                    self._pct_var.set("100%")
                    self._status_var.set(f"Done — {msg[1]} file(s) converted.")
                    self._log_sep()
                    self._log(f"Done — {msg[1]} file(s) converted.", "success")
                    self._log_sep()
                    self._converting = False
                    self._btn_convert.config(state="normal")
                    return
        except queue.Empty:
            pass
        self.after(50, self._poll_queue)
