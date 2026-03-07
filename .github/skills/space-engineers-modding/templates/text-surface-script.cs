using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI;
using VRageMath;
using System;
using System.Collections.Generic;
using System.Text;

namespace YourModNamespace
{
    /// <summary>
    /// Text Surface Script for LCD displays.
    /// Shows [describe what your script does]
    /// </summary>
    public class TSS_YourScriptName : MyTextSurfaceScriptBase
    {
        // Surface and viewport
        private IMyTextSurface m_surface;
        private RectangleF m_viewport;
        
        // Configuration
        private bool m_initialized = false;
        
        // Update tracking
        private int m_frameCount = 0;
        private const int UPDATE_FREQUENCY = 60; // Update every 60 frames (~1 second)
        
        public TSS_YourScriptName(IMyTextSurface surface, IMyCubeBlock block, Vector2 size) 
            : base(surface, block, size)
        {
            m_surface = surface;
            
            // Don't run on dedicated servers
            if (MyAPIGateway.Utilities?.IsDedicated ?? false)
                return;
            
            // Calculate viewport (accounts for texture padding)
            m_viewport = new RectangleF(
                (m_surface.TextureSize - m_surface.SurfaceSize) / 2f,
                m_surface.SurfaceSize
            );
            
            // Set to script mode
            m_surface.ContentType = ContentType.SCRIPT;
            m_surface.Script = "";
        }

        public override void Run()
        {
            // Don't run on dedicated servers
            if (MyAPIGateway.Utilities?.IsDedicated ?? false)
                return;

            try
            {
                // Initialize on first run
                if (!m_initialized)
                {
                    Initialize();
                    m_initialized = true;
                }
                
                // Update at specified frequency
                m_frameCount++;
                if (m_frameCount >= UPDATE_FREQUENCY)
                {
                    m_frameCount = 0;
                    Update();
                }
                
                // Draw every frame
                Draw();
            }
            catch (Exception ex)
            {
                // Log errors
                MyLog.Default.WriteLine($"[YourScriptName] Error in Run: {ex.Message}");
            }
        }

        private void Initialize()
        {
            // Get terminal block
            var terminalBlock = m_surface.Entity as IMyTerminalBlock;
            if (terminalBlock == null)
                return;
            
            // Parse CustomData configuration
            ParseConfiguration(terminalBlock.CustomData);
        }

        private void Update()
        {
            // Get terminal block
            var terminalBlock = m_surface.Entity as IMyTerminalBlock;
            if (terminalBlock == null)
                return;
            
            // Get grid
            var grid = terminalBlock.CubeGrid as IMyCubeGrid;
            if (grid == null)
                return;
            
            // TODO: Gather data from blocks, inventories, etc.
            // Example: Get all blocks of a type
            // List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
            // MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(grid)
            //     .GetBlocksOfType<IMyPowerProducer>(blocks);
        }

        private void Draw()
        {
            // Begin drawing frame
            using (var frame = m_surface.DrawFrame())
            {
                // Draw background
                var background = new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = "SquareSimple",
                    Position = m_viewport.Center,
                    Size = m_viewport.Size,
                    Color = m_surface.ScriptBackgroundColor,
                    Alignment = TextAlignment.CENTER
                };
                frame.Add(background);
                
                // Draw title
                var title = MySprite.CreateText(
                    "Your Script Name",
                    "White",
                    m_surface.ScriptForegroundColor,
                    1.0f,
                    TextAlignment.CENTER
                );
                title.Position = new Vector2(m_viewport.Center.X, m_viewport.Position.Y + 20);
                frame.Add(title);
                
                // TODO: Draw your content
                // Example: Draw text
                // var text = MySprite.CreateText(
                //     "Content here",
                //     "White",
                //     m_surface.ScriptForegroundColor,
                //     0.8f,
                //     TextAlignment.LEFT
                // );
                // text.Position = new Vector2(m_viewport.Position.X + 10, m_viewport.Position.Y + 60);
                // frame.Add(text);
            }
        }

        private void ParseConfiguration(string customData)
        {
            // Parse CustomData using MyIni
            MyIni config = new MyIni();
            MyIniParseResult result;
            
            if (!config.TryParse(customData, out result))
            {
                // Invalid format - use defaults
                MyLog.Default.WriteLine($"[YourScriptName] Failed to parse CustomData: {result}");
                return;
            }
            
            // Read configuration values with defaults
            // Example:
            // bool enabled = config.Get("YourSection", "Enabled").ToBoolean(true);
            // string text = config.Get("YourSection", "DisplayText").ToString("Default");
            // int number = config.Get("YourSection", "UpdateFrequency").ToInt32(60);
        }

        public override void Dispose()
        {
            // Cleanup when script is disposed
            base.Dispose();
            
            m_initialized = false;
        }
    }
}
