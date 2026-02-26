using Sandbox.Game.GameSystems.TextSurfaceScripts;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRageMath;

namespace MahrianeIndustries.LCDInfo
{
    [MyTextSurfaceScript("LCDInfoScreenDetailedInfo", "$IOS LCD - Detailed Info")]
    public class LCDDetailedInfo : MyTextSurfaceScriptBase
    {
        MyIni config = new MyIni();

        public static string CONFIG_SECTION_ID = "SettingsDetailedInfoStatus";

        string searchId = "";
        bool showHeader = true;

        IMyTextSurface mySurface;
        IMyTerminalBlock myTerminalBlock;

        SurfaceDrawer.SurfaceData surfaceData;
        List<IMyTerminalBlock> allBlocks = new List<IMyTerminalBlock>();

        void TryCreateSurfaceData()
        {
            if (surfaceData != null)
                return;

            var textSize = 0.5f;

            surfaceData = new SurfaceDrawer.SurfaceData
            {
                surface = mySurface,
                textSize = textSize,
                titleOffset = 128,
                ratioOffset = 96,
                viewPortOffsetX = 12,
                viewPortOffsetY = 12,
                newLine = new Vector2(0, 30 * textSize),
                showHeader = true,
                showSubgrids = false,
                showDocked = false,
                useColors = false
            };
        }

        void CreateConfig()
        {
            TryCreateSurfaceData();

            // Build custom formatted config with INI section headers
            StringBuilder sb = new StringBuilder();

            // Always preserve existing CustomData (from other mods/apps)
            string existing = myTerminalBlock.CustomData ?? "";
            if (!string.IsNullOrWhiteSpace(existing))
            {
                sb.Append(existing);
                if (!existing.EndsWith("\n"))
                    sb.AppendLine();
            }

            // Write config with INI section header for proper parsing
            sb.AppendLine($"[{CONFIG_SECTION_ID}]");
            sb.AppendLine();
            sb.AppendLine("; [ DETAILEDINFO - GENERAL OPTIONS ]");
            ConfigHelpers.AppendSearchIdConfig(sb, searchId);
            ConfigHelpers.AppendShowHeaderConfig(sb, showHeader);
            sb.AppendLine();
            sb.AppendLine("; [ DETAILEDINFO - LAYOUT OPTIONS ]");
            sb.AppendLine($"TextSize={surfaceData.textSize}");
            sb.AppendLine($"ViewPortOffsetX={surfaceData.viewPortOffsetX}");
            sb.AppendLine($"ViewPortOffsetY={surfaceData.viewPortOffsetY}");
            sb.AppendLine($"TitleFieldWidth={surfaceData.titleOffset}");
            sb.AppendLine($"RatioFieldWidth={surfaceData.ratioOffset}");

            myTerminalBlock.CustomData = sb.ToString();
        }

        void ReadConfig()
        {
            TryCreateSurfaceData();

            var customData = myTerminalBlock.CustomData ?? "";
            if (string.IsNullOrWhiteSpace(customData))
            {
                CreateConfig();
                return;
            }

            MyIniParseResult result;
            if (!config.TryParse(customData, CONFIG_SECTION_ID, out result))
            {
                CreateConfig();
                return;
            }

            // Read general options
            searchId = config.Get(CONFIG_SECTION_ID, "SearchId").ToString(searchId);
            showHeader = config.Get(CONFIG_SECTION_ID, "ShowHeader").ToBoolean(showHeader);

            // Read layout options
            surfaceData.textSize = config.Get(CONFIG_SECTION_ID, "TextSize").ToSingle(surfaceData.textSize);
            surfaceData.viewPortOffsetX = (int)config.Get(CONFIG_SECTION_ID, "ViewPortOffsetX").ToSingle(surfaceData.viewPortOffsetX);
            surfaceData.viewPortOffsetY = (int)config.Get(CONFIG_SECTION_ID, "ViewPortOffsetY").ToSingle(surfaceData.viewPortOffsetY);
            surfaceData.titleOffset = (int)config.Get(CONFIG_SECTION_ID, "TitleFieldWidth").ToSingle(surfaceData.titleOffset);
            surfaceData.ratioOffset = (int)config.Get(CONFIG_SECTION_ID, "RatioFieldWidth").ToSingle(surfaceData.ratioOffset);

            // Update newLine based on textSize
            surfaceData.newLine = new Vector2(0, 30 * surfaceData.textSize);
            surfaceData.showHeader = showHeader;
        }

        public override void Run()
        {
            try
            {
                base.Run();

                TryCreateSurfaceData();
                ReadConfig();

                var frame = mySurface.DrawFrame();
                Vector2 position = new Vector2(surfaceData.viewPortOffsetX, surfaceData.viewPortOffsetY);

                // Draw header
                if (surfaceData.showHeader)
                {
                    DrawHeader(ref frame, ref position);
                    position += surfaceData.newLine;
                }

                // Check if SearchId is empty
                if (string.IsNullOrWhiteSpace(searchId))
                {
                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, "Add block name to SearchId", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                    frame.Dispose();
                    return;
                }

                // Find the block
                IMyTerminalBlock targetBlock = FindBlock();

                if (targetBlock == null)
                {
                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, "Block not found", TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                    frame.Dispose();
                    return;
                }

                // Get and display the detailed info (even if empty to avoid flickering)
                string detailedInfo = targetBlock.DetailedInfo ?? "";
                DrawDetailedInfo(ref frame, ref position, detailedInfo);
                frame.Dispose();
            }
            catch (Exception e)
            {
                if (mySurface != null)
                {
                    using (var frame = mySurface.DrawFrame())
                    {
                        Vector2 position = new Vector2(surfaceData?.viewPortOffsetX ?? 12, surfaceData?.viewPortOffsetY ?? 12);
                        var sprite = MySprite.CreateText($"Error: {e.Message}", "DEBUG", Color.Red, 0.5f, TextAlignment.LEFT);
                        sprite.Position = position;
                        frame.Add(sprite);
                    }
                }
            }
        }

        void DrawHeader(ref MySpriteDrawFrame frame, ref Vector2 position)
        {
            string blockName = string.IsNullOrWhiteSpace(searchId) ? "" : searchId;
            string headerLeft = $"Detailed Info [{blockName}]";
            string headerRight = DateTime.Now.ToString("HH:mm:ss");

            SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, headerLeft, TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
            SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, headerRight, TextAlignment.RIGHT, surfaceData.surface.ScriptForegroundColor);
        }

        IMyTerminalBlock FindBlock()
        {
            allBlocks.Clear();

            // Get terminal system
            var grid = myTerminalBlock.CubeGrid;
            var gridTerminalSystem = MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(grid);

            if (gridTerminalSystem == null)
                return null;

            // Search only on the same grid
            gridTerminalSystem.GetBlocks(allBlocks);

            // Find exact match (case-insensitive)
            return allBlocks.FirstOrDefault(b => b.CustomName.Equals(searchId, StringComparison.OrdinalIgnoreCase));
        }

        void DrawDetailedInfo(ref MySpriteDrawFrame frame, ref Vector2 position, string detailedInfo)
        {
            // Split by lines
            string[] lines = detailedInfo.Split(new[] { '\r', '\n' }, StringSplitOptions.None);

            // Calculate max characters per line based on surface width and text size
            float availableWidth = mySurface.SurfaceSize.X - (surfaceData.viewPortOffsetX * 2);
            int maxCharsPerLine = (int)(availableWidth / (surfaceData.textSize * 19)); // Approximate character width

            foreach (string line in lines)
            {
                if (string.IsNullOrEmpty(line))
                {
                    // Empty line - just advance position
                    position += surfaceData.newLine;
                    continue;
                }

                // Word wrap if line is too long
                if (line.Length <= maxCharsPerLine)
                {
                    SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, line, TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                    position += surfaceData.newLine;
                }
                else
                {
                    // Wrap long lines
                    List<string> wrappedLines = WrapText(line, maxCharsPerLine);
                    foreach (string wrappedLine in wrappedLines)
                    {
                        SurfaceDrawer.WriteTextSprite(ref frame, position, surfaceData, wrappedLine, TextAlignment.LEFT, surfaceData.surface.ScriptForegroundColor);
                        position += surfaceData.newLine;
                    }
                }
            }
        }

        List<string> WrapText(string text, int maxLength)
        {
            List<string> result = new List<string>();

            if (string.IsNullOrEmpty(text) || maxLength <= 0)
                return result;

            int currentIndex = 0;

            while (currentIndex < text.Length)
            {
                int length = Math.Min(maxLength, text.Length - currentIndex);

                // If we're not at the end, try to break at a space
                if (currentIndex + length < text.Length)
                {
                    int lastSpace = text.LastIndexOf(' ', currentIndex + length, length);
                    if (lastSpace > currentIndex)
                    {
                        length = lastSpace - currentIndex;
                    }
                }

                result.Add(text.Substring(currentIndex, length).TrimEnd());
                currentIndex += length;

                // Skip the space we broke on
                if (currentIndex < text.Length && text[currentIndex] == ' ')
                    currentIndex++;
            }

            return result;
        }

        public LCDDetailedInfo(IMyTextSurface surface, IMyCubeBlock block, Vector2 size) : base(surface, block, size)
        {
            mySurface = surface;
            myTerminalBlock = block as IMyTerminalBlock;
        }

        public override ScriptUpdate NeedsUpdate => ScriptUpdate.Update100;

        public override void Dispose()
        {
            base.Dispose();
        }
    }
}
