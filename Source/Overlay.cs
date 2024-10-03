using System.Collections.ObjectModel;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;
using SharpDX.DXGI;
using SharpDX.Mathematics.Interop;
using AlphaMode = SharpDX.Direct2D1.AlphaMode;
using Color = System.Drawing.Color;
using Factory = SharpDX.Direct2D1.Factory;
using Font = System.Drawing.Font;
using FontFactory = SharpDX.DirectWrite.Factory;
using Point = System.Drawing.Point;
using TextAntialiasMode = SharpDX.Direct2D1.TextAntialiasMode;
using TextRenderer = System.Windows.Forms.TextRenderer;
using Vector3 = System.Numerics.Vector3;
using Numerics = System.Numerics;
using System.Xml.Linq;
using System.Drawing;
using System.Diagnostics;

namespace eft_dma_radar;

public partial class Overlay : Form
{
    private frmMain _frmMain;

    public static bool isMenuShown = false;

    public static bool isESPOn = true;
    public static bool isPMCOn = true;
    public static bool isTeamOn = true;
    public static bool isScavOn = true;
    public static bool isLootOn = true;
    public static int npcLimit = 350;
    public static int playerLimit = 750;
    public static int teamLimit = 750;
    public static int lootLimit = 250;

    private CameraManager _cameraManager;

    public static ulong FPSCamera
    {
        get => CameraManager._staticfpsCamera;  // Access static field directly
    }


    // Loot ESP
    private readonly Config _config;
    private LootManager Loot
    {
        get => Memory.Loot;
    }


    //private InGameMenu menu;

    private void CreateMenu()
    {
        //var menu = InGameMenu.Instance;
        //menu.Show();
        //menu.BringToFront();
        //menu.Focus();
    }

private void DirectXThread(object sender)
    {
        var isReady = Ready; // cache bool
        var inGame = InGame; // cache bool
        var localPlayer = LocalPlayer; // cache ref to current player
        var lastInGameState = inGame; // cache last known in-game state
        var lastUpdateTime = DateTime.Now; // to track the last time we updated the text

        while (_running && !token.IsCancellationRequested)
            try
            {
                _device.BeginDraw();
                _device.Clear(SharpDX.Color.Transparent);
                _device.TextAntialiasMode = TextAntialiasMode.Aliased;

                WriteTopLeftText("Tarkov Overlay", Brushes.WHITE);


                while (localPlayer is null)
                {
                    localPlayer = LocalPlayer;
                    WriteTopLeftText("NOT IN RAID", Brushes.RED, 13, "Arial Unicode MS", 10, 30);
                    _device.Flush();
                    _device.EndDraw();
                    Thread.Sleep(5);
                } 

                var strBuild = new StringBuilder();
                if (InGame && localPlayer is not null)
                {
                    WriteTopLeftText("IN RAID", Brushes.GREEN, 13, "Arial Unicode MS", 10, 30);
                    var allPlayers = AllPlayers?.Select(x => x.Value);
                    if (allPlayers is not null)
                    {
                        foreach (var player in allPlayers)
                        {
                            var playerHeadPos = new Vector3(player.HeadPosition.X, player.HeadPosition.Z, player.HeadPosition.Y);
                            var playerBasePos = new Vector3(player.Position.X, player.Position.Z, player.Position.Y);

                            var localPlayerPos = localPlayer.Position;
                            var dist = Vector3.Distance(localPlayerPos, player.Position);

                            // Check if player is valid for ESP drawing
                            if ((player.IsAlive && player.Type is not PlayerType.LocalPlayer && dist <= playerLimit && isESPOn)
                                || (player.Type is not PlayerType.LocalPlayer && !player.IsHuman && player.IsAlive && dist <= npcLimit && isESPOn)
                                || (player.Type is not PlayerType.LocalPlayer && player.Type is PlayerType.Teammate && player.IsAlive && dist <= teamLimit && isESPOn))
                            {
                                List<Vector3> enemyPositions = new List<Vector3>
                                {
                                    playerBasePos, // Base position (foot)
                                    playerHeadPos // Head position
                                };

                                List<Vector3> coords = new List<Vector3>();
                                WorldToScreenCombined(player, enemyPositions, coords);

                                Vector3 baseCoords = coords[0]; // Foot position
                                Vector3 headCoords = coords[1]; // Head position


                                // Calculate the height of the bounding box
                                float boxHeight = headCoords.Y - baseCoords.Y; // Height from foot to head
                                float boxWidth = boxHeight * 0.6f; // Width is now 60% of the height

                                // Calculate padding based on box dimensions
                                float paddingHeight = boxHeight * 0.1f; // 10% of the height for height padding
                                float paddingWidth = boxWidth * 0.05f;   // 5% of the width for width padding

                                // Set the box dimensions with padding
                                boxHeight += paddingHeight; // Add height padding
                                boxWidth += paddingWidth;   // Add width padding

                                // Draw the rectangle based on foot and head coordinates
                                if (baseCoords.X > 0 || baseCoords.Y > 0 || baseCoords.Z > 0)
                                {
                                    #region PMC
                                    if (player.Type is PlayerType.PMC && isPMCOn && dist <= playerLimit)
                                    {
                                        // Set the top and bottom coordinates for the bounding box
                                        _device.DrawRectangle(
                                            new RawRectangleF(baseCoords.X - (boxWidth / 2), headCoords.Y + paddingHeight, // Top line at head position + padding
                                                              baseCoords.X + (boxWidth / 2), baseCoords.Y - paddingHeight), // Bottom line at foot position - padding
                                            Brushes.RED); 

                                        // Calculate the head dot size as 30% of the bounding box width
                                        float headDotSize = boxWidth * 0.2f; // Set head ellipse size to 20% of the bounding box width
                                        _device.DrawEllipse(new Ellipse(new RawVector2(headCoords.X - (headDotSize / 2), headCoords.Y - (headDotSize / 2)), headDotSize, headDotSize), Brushes.WHITE); // Head ellipse outline

                                        // Display text information
                                        WriteText(player.Name + Environment.NewLine + Math.Round(dist, 0) + "m",
                                                  baseCoords.X + 5, baseCoords.Y - 25, Brushes.WHITE);
                                    }
                                    #endregion
                                    #region Scav
                                    if (player.Type is PlayerType.Scav && isScavOn && dist <= npcLimit)
                                    {
                                        // Set the top and bottom coordinates for the bounding box
                                        _device.DrawRectangle(
                                            new RawRectangleF(baseCoords.X - (boxWidth / 2), headCoords.Y + paddingHeight, // Top line at head position + padding
                                                              baseCoords.X + (boxWidth / 2), baseCoords.Y - paddingHeight), // Bottom line at foot position - padding
                                            Brushes.YELLOW); 

                                        // Calculate the head dot size as 30% of the bounding box width
                                        float headDotSize = boxWidth * 0.2f; // Set head ellipse size to 20% of the bounding box width
                                        _device.DrawEllipse(new Ellipse(new RawVector2(headCoords.X - (headDotSize / 2), headCoords.Y - (headDotSize / 2)), headDotSize, headDotSize), Brushes.WHITE); // Head ellipse outline

                                        // Display text information
                                        WriteText("Scav" + Environment.NewLine + Math.Round(dist, 0) + "m",
                                                  baseCoords.X + 5, baseCoords.Y - 25, Brushes.WHITE);

                                    }
                                    #endregion
                                    #region Boss
                                    if (player.Type is PlayerType.Boss && isScavOn && dist <= npcLimit)
                                    {
                                        // Set the top and bottom coordinates for the bounding box
                                        _device.DrawRectangle(
                                            new RawRectangleF(baseCoords.X - (boxWidth / 2), headCoords.Y + paddingHeight, // Top line at head position + padding
                                                              baseCoords.X + (boxWidth / 2), baseCoords.Y - paddingHeight), // Bottom line at foot position - padding
                                            Brushes.MAGENTA); 

                                        // Calculate the head dot size as 30% of the bounding box width
                                        float headDotSize = boxWidth * 0.2f; // Set head ellipse size to 20% of the bounding box width
                                        _device.DrawEllipse(new Ellipse(new RawVector2(headCoords.X - (headDotSize / 2), headCoords.Y - (headDotSize / 2)), headDotSize, headDotSize), Brushes.WHITE); // Head ellipse outline

                                        // Display text information
                                        WriteText(player.Name + Environment.NewLine + Math.Round(dist, 0) + "m",
                                                  baseCoords.X + 5, baseCoords.Y - 25, Brushes.WHITE);

                                    }
                                    #endregion
                                    #region BossFollower
                                    if ((player.Type is PlayerType.BossFollower || player.Type is PlayerType.BossGuard) && isScavOn && dist <= npcLimit)
                                    {
                                        // Set the top and bottom coordinates for the bounding box
                                        _device.DrawRectangle(
                                            new RawRectangleF(baseCoords.X - (boxWidth / 2), headCoords.Y + paddingHeight, // Top line at head position + padding
                                                              baseCoords.X + (boxWidth / 2), baseCoords.Y - paddingHeight), // Bottom line at foot position - padding
                                            Brushes.PURPLE); 

                                        // Calculate the head dot size as 30% of the bounding box width
                                        float headDotSize = boxWidth * 0.2f; // Set head ellipse size to 20% of the bounding box width
                                        _device.DrawEllipse(new Ellipse(new RawVector2(headCoords.X - (headDotSize / 2), headCoords.Y - (headDotSize / 2)), headDotSize, headDotSize), Brushes.WHITE); // Head ellipse outline

                                        // Display text information
                                        WriteText("Follower" + Environment.NewLine + Math.Round(dist, 0) + "m",
                                                  baseCoords.X + 5, baseCoords.Y - 25, Brushes.WHITE);

                                    }
                                    #endregion
                                    #region Teammate
                                    if (player.Type is PlayerType.Teammate && isTeamOn && dist <= teamLimit)
                                    {
                                        // Set the top and bottom coordinates for the bounding box
                                        _device.DrawRectangle(
                                            new RawRectangleF(baseCoords.X - (boxWidth / 2), headCoords.Y + paddingHeight, // Top line at head position + padding
                                                              baseCoords.X + (boxWidth / 2), baseCoords.Y - paddingHeight), // Bottom line at foot position - padding
                                            Brushes.GREEN); 

                                        // Calculate the head dot size as 30% of the bounding box width
                                        float headDotSize = boxWidth * 0.2f; // Set head ellipse size to 20% of the bounding box width
                                        _device.DrawEllipse(new Ellipse(new RawVector2(headCoords.X - (headDotSize / 2), headCoords.Y - (headDotSize / 2)), headDotSize, headDotSize), Brushes.WHITE); // Head ellipse outline

                                        // Display text information
                                        WriteText(player.Name + Environment.NewLine + Math.Round(dist, 0) + "m",
                                                  baseCoords.X + 5, baseCoords.Y - 25, Brushes.WHITE);
                                    }
                                    #endregion
                                    #region Cultist
                                    if (player.Type is PlayerType.Cultist && isScavOn && dist <= npcLimit)
                                    {
                                        // Set the top and bottom coordinates for the bounding box
                                        _device.DrawRectangle(
                                            new RawRectangleF(baseCoords.X - (boxWidth / 2), headCoords.Y + paddingHeight, // Top line at head position + padding
                                                              baseCoords.X + (boxWidth / 2), baseCoords.Y - paddingHeight), // Bottom line at foot position - padding
                                            Brushes.ORANGE); 

                                        // Calculate the head dot size as 30% of the bounding box width
                                        float headDotSize = boxWidth * 0.2f; // Set head ellipse size to 20% of the bounding box width
                                        _device.DrawEllipse(new Ellipse(new RawVector2(headCoords.X - (headDotSize / 2), headCoords.Y - (headDotSize / 2)), headDotSize, headDotSize), Brushes.WHITE); // Head ellipse outline

                                        // Display text information
                                        WriteText("Cultist" + Environment.NewLine + Math.Round(dist, 0) + "m",
                                                  baseCoords.X + 5, baseCoords.Y - 25, Brushes.WHITE);
                                    }
                                    #endregion
                                    #region PlayerScav
                                    if (player.Type is PlayerType.PlayerScav && isPMCOn && dist <= playerLimit)
                                    {
                                        // Set the top and bottom coordinates for the bounding box
                                        _device.DrawRectangle(
                                            new RawRectangleF(baseCoords.X - (boxWidth / 2), headCoords.Y + paddingHeight, // Top line at head position + padding
                                                              baseCoords.X + (boxWidth / 2), baseCoords.Y - paddingHeight), // Bottom line at foot position - padding
                                            Brushes.BLUE); 

                                        // Calculate the head dot size as 30% of the bounding box width
                                        float headDotSize = boxWidth * 0.2f; // Set head ellipse size to 20% of the bounding box width
                                        _device.DrawEllipse(new Ellipse(new RawVector2(headCoords.X - (headDotSize / 2), headCoords.Y - (headDotSize / 2)), headDotSize, headDotSize), Brushes.WHITE); // Head ellipse outline

                                        // Display text information
                                        WriteText("Player Scav" + Environment.NewLine + Math.Round(dist, 0) + "m",
                                                  baseCoords.X + 5, baseCoords.Y - 25, Brushes.WHITE);
                                    }
                                    #endregion
                                    #region Raider
                                    if (player.Type is PlayerType.Raider && isScavOn && dist <= npcLimit)
                                    {
                                        // Set the top and bottom coordinates for the bounding box
                                        _device.DrawRectangle(
                                            new RawRectangleF(baseCoords.X - (boxWidth / 2), headCoords.Y + paddingHeight, // Top line at head position + padding
                                                              baseCoords.X + (boxWidth / 2), baseCoords.Y - paddingHeight), // Bottom line at foot position - padding
                                            Brushes.ORANGE); 

                                        // Calculate the head dot size as 30% of the bounding box width
                                        float headDotSize = boxWidth * 0.2f; // Set head ellipse size to 20% of the bounding box width
                                        _device.DrawEllipse(new Ellipse(new RawVector2(headCoords.X - (headDotSize / 2), headCoords.Y - (headDotSize / 2)), headDotSize, headDotSize), Brushes.WHITE); // Head ellipse outline

                                        // Display text information
                                        WriteText("Raider" + Environment.NewLine + Math.Round(dist, 0) + "m",
                                                  baseCoords.X + 5, baseCoords.Y - 25, Brushes.WHITE);
                                    }
                                    #endregion
                                    #region SniperScav
                                    if (player.Type is PlayerType.SniperScav && isScavOn && dist <= npcLimit)
                                    {
                                        // Set the top and bottom coordinates for the bounding box
                                        _device.DrawRectangle(
                                            new RawRectangleF(baseCoords.X - (boxWidth / 2), headCoords.Y + paddingHeight, // Top line at head position + padding
                                                              baseCoords.X + (boxWidth / 2), baseCoords.Y - paddingHeight), // Bottom line at foot position - padding
                                            Brushes.YELLOW); 

                                        // Calculate the head dot size as 30% of the bounding box width
                                        float headDotSize = boxWidth * 0.2f; // Set head ellipse size to 20% of the bounding box width
                                        _device.DrawEllipse(new Ellipse(new RawVector2(headCoords.X - (headDotSize / 2), headCoords.Y - (headDotSize / 2)), headDotSize, headDotSize), Brushes.WHITE); // Head ellipse outline

                                        // Display text information
                                        WriteText("Sniper Scav" + Environment.NewLine + Math.Round(dist, 0) + "m",
                                                  baseCoords.X + 5, baseCoords.Y - 25, Brushes.WHITE);
                                    }
                                    #endregion

                                }
                            }
                        }

                        // THIS LOOT FILTER IS USING AN OLD BUGGY METHOD OF SCALING. IT IS NOT 100% ACCURATE

                        #region Item ESP
                        var loot = this.Loot; // cache ref
                        var lootplayer = this.LocalPlayer;
                        if (loot is not null)
                        {
                            var filter = Loot.Filter; // Get ref to collection
                            if (filter is not null) foreach (var item in filter)
                                {
                                    var localPlayerPos = localPlayer.Position;

                                    var lootPos = new System.Numerics.Vector3(item.Position.X, item.Position.Z, item.Position.Y);
                                    var lootdist = System.Numerics.Vector3.Distance(localPlayerPos, item.Position);

                                    float distfact = 0.5f; // Distance factor (How much the rectangle will grow or shrink)
                                    float lootheight = 500; // Height of rectangle when the player is 1m away from localplayer
                                    float lootwidth = 300; // Width of rectangle when the player is 1m away from localplayer
                                    lootheight = lootheight / lootdist * distfact; // Height of box = pheight / distance to local player * distance factor
                                    lootwidth = lootwidth / lootdist * distfact; // Width of box = pheight / distance to local player * distance factor                                        

                                    if (isESPOn == true && isLootOn == true)
                                    {
                                        // Loot ESP
                                        WorldToScreenLootTest(lootplayer, lootPos, out var lootcoords);
                                        if (lootcoords.X > 0 || lootcoords.Y > 0 || lootcoords.Z > 0)
                                        {
                                            if (lootdist <= lootLimit)
                                            {
                                                _device.DrawRectangle(new RawRectangleF(lootcoords.X - lootwidth, lootcoords.Y + (lootheight / 4), lootcoords.X + lootwidth, lootcoords.Y - lootheight), Brushes.WHITE);
                                                WriteText(item.Name + Environment.NewLine + Math.Round(lootdist, 0) + "m", lootcoords.X + 5, lootcoords.Y - 25, Brushes.WHITE);
                                            }
                                        }
                                    }
                                }
                        }
                        #endregion

                        // Crosshair
                        WriteCenterText("+", Height / 2, Brushes.WHITE);
                        _device.Flush();
                        _device.EndDraw();
                    }
                }
                else if (InGame is false)
                {
                    WriteTopLeftText("NOT IN RAID", Brushes.RED, 13, "Arial Unicode MS", 10, 30); // Update text for not in-game
                    _device.Flush();
                    _device.EndDraw();
                }
                Console.WriteLine("Local player is " + LocalPlayer);
            }
            catch (SharpDXException e)
            {
                try
                {
                    Console.WriteLine(e);
                    _device.Flush();
                    _device.EndDraw();
                }
                catch
                {
                }
            }

        Thread.Sleep(10);
    }
    #region Declaration

    internal struct Margins
    {
        public int Left, Right, Top, Bottom;
    }

    private Margins marg;

    private ReadOnlyDictionary<string, Player> AllPlayers => Memory.Players;

    /// <summary>
    ///     Radar has found Escape From Tarkov process and is ready.
    /// </summary>
    private bool Ready => Memory.Ready;

    /// <summary>
    ///     Radar has found Local Game World.
    /// </summary>
    private bool InGame => Memory.InGame;

    /// <summary>
    ///     LocalPlayer (who is running Radar) 'Player' object.
    /// </summary>
    private Player LocalPlayer => Memory.Players?.FirstOrDefault(x => x.Value.Type is PlayerType.LocalPlayer).Value;

    [DllImport("dwmapi.dll")]
    private static extern void DwmExtendFrameIntoClientArea(IntPtr hWnd, ref Margins pMargins);

    private static WindowRenderTarget _device;
    private HwndRenderTargetProperties _renderProperties;
    private Factory _factory;

    public static bool ingame = false;

    // Fonts
    private readonly FontFactory _fontFactory = new();

    private IntPtr _handle;
    private Thread _threadDx;

    private readonly float[] _viewMatrix = new float[16];
    private Vector3 _worldToScreenPos;
    private CancellationTokenSource _tokenSource;
    private CancellationToken token;

    public bool toggleESP = true;
    public bool togglePMCESP = true;
    public bool toggleTeamESP = true;
    public bool toggleScavESP = true;

    private bool _running;

    public static class Colors
    {
        public static RawColor4 WHITE = new(Color.White.R, Color.White.G, Color.White.B, Color.White.A);
        public static RawColor4 BLACK = new(Color.Black.R, Color.Black.G, Color.Black.B, Color.Black.A);
        public static RawColor4 RED = new(Color.Red.R, Color.Red.G, Color.Red.B, Color.Red.A);
        public static RawColor4 GREEN = new(Color.Green.R, Color.Green.G, Color.Green.B, Color.Green.A);
        public static RawColor4 BLUE = new(Color.Blue.R, Color.Blue.G, Color.Blue.B, Color.Blue.A);
        public static RawColor4 TRANSPARENCY = new(Color.Black.R, Color.Black.G, Color.Black.B, 255);

        // Add more colors
        public static RawColor4 YELLOW = new(255, 255, 0, 255); // Yellow
        public static RawColor4 CYAN = new(0, 255, 255, 255); // Cyan
        public static RawColor4 MAGENTA = new(255, 0, 255, 255); // Magenta
        public static RawColor4 ORANGE = new(255, 165, 0, 255); // Orange
        public static RawColor4 PURPLE = new(128, 0, 128, 255); // Purple
        public static RawColor4 GRAY = new(128, 128, 128, 255); // Gray
        public static RawColor4 LIGHT_GRAY = new(211, 211, 211, 255); // Light Gray

        // Additional colors
        public static RawColor4 LIGHT_BLUE = new(173, 216, 230, 255); // Light Blue
        public static RawColor4 DARK_BLUE = new(0, 0, 139, 255); // Dark Blue
        public static RawColor4 LIGHT_GREEN = new(144, 238, 144, 255); // Light Green
        public static RawColor4 DARK_GREEN = new(0, 100, 0, 255); // Dark Green
        public static RawColor4 BROWN = new(165, 42, 42, 255); // Brown
        public static RawColor4 TEAL = new(0, 128, 128, 255); // Teal
        public static RawColor4 OLIVE = new(128, 128, 0, 255); // Olive
        public static RawColor4 MAROON = new(128, 0, 0, 255); // Maroon
        public static RawColor4 NAVY = new(0, 0, 128, 255); // Navy
        public static RawColor4 SILVER = new(192, 192, 192, 255); // Silver
        public static RawColor4 GOLD = new(255, 215, 0, 255); // Gold

        // Custom Colors using RGBA values
        public static RawColor4 CUSTOM_COLOR_1 = new(255, 128, 0, 128); // Custom Purple
        public static RawColor4 CUSTOM_COLOR_2 = new(255, 255, 165, 0); // Custom Orange
        public static RawColor4 CUSTOM_COLOR_3 = new(255, 0, 255, 255); // Custom Aqua
    }


    public class Brushes
    {
        public static SolidColorBrush WHITE = new(_device, Colors.WHITE);
        public static SolidColorBrush BLACK = new(_device, Colors.BLACK);
        public static SolidColorBrush RED = new(_device, Colors.RED);
        public static SolidColorBrush GREEN = new(_device, Colors.GREEN);
        public static SolidColorBrush BLUE = new(_device, Colors.BLUE);
        public static SolidColorBrush TRANSPARENCY = new(_device, Colors.TRANSPARENCY);

        // New colors from the Colors class
        public static SolidColorBrush YELLOW = new(_device, Colors.YELLOW);
        public static SolidColorBrush CYAN = new(_device, Colors.CYAN);
        public static SolidColorBrush MAGENTA = new(_device, Colors.MAGENTA);
        public static SolidColorBrush ORANGE = new(_device, Colors.ORANGE);
        public static SolidColorBrush PURPLE = new(_device, Colors.PURPLE);
        public static SolidColorBrush GRAY = new(_device, Colors.GRAY);
        public static SolidColorBrush LIGHT_GRAY = new(_device, Colors.LIGHT_GRAY);
        public static SolidColorBrush LIGHT_BLUE = new(_device, Colors.LIGHT_BLUE);
        public static SolidColorBrush DARK_BLUE = new(_device, Colors.DARK_BLUE);
        public static SolidColorBrush LIGHT_GREEN = new(_device, Colors.LIGHT_GREEN);
        public static SolidColorBrush DARK_GREEN = new(_device, Colors.DARK_GREEN);
        public static SolidColorBrush BROWN = new(_device, Colors.BROWN);
        public static SolidColorBrush TEAL = new(_device, Colors.TEAL);
        public static SolidColorBrush OLIVE = new(_device, Colors.OLIVE);
        public static SolidColorBrush MAROON = new(_device, Colors.MAROON);
        public static SolidColorBrush NAVY = new(_device, Colors.NAVY);
        public static SolidColorBrush SILVER = new(_device, Colors.SILVER);
        public static SolidColorBrush GOLD = new(_device, Colors.GOLD);
        public static SolidColorBrush CUSTOM_COLOR_1 = new(_device, Colors.CUSTOM_COLOR_1);
        public static SolidColorBrush CUSTOM_COLOR_2 = new(_device, Colors.CUSTOM_COLOR_2);
        public static SolidColorBrush CUSTOM_COLOR_3 = new(_device, Colors.CUSTOM_COLOR_3);
    }


    #endregion

    #region Start

    public Overlay()
    {
        _handle = Handle;
        InitializeComponent();
        ApplicationManager.CloseOverlayRequested += CloseOverlay;
        Move += OverlayForm_Move;
    }

    private Factory factory = new();

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == Keys.F7)
        {
            Hide();
            return true;
        }

        if (keyData == Keys.Insert)
        {
            //MenuManager.ToggleMenu();
            return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

private void LoadOverlay(object sender, EventArgs e)
{
    // Dispose of the existing overlay if it is already running
    if (_threadDx != null && _threadDx.IsAlive)
    {
        _running = false; // Stop the thread
        _tokenSource.Cancel(); // Signal cancellation

        // Wait for the thread to finish
        _threadDx.Join();
        
        // Dispose of the DirectX device and factory
        _device?.Dispose();
        _factory?.Dispose();

        _device = null;
        _factory = null;
    }

    DoubleBuffered = true;
    SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
             ControlStyles.Opaque | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);

    _factory = new Factory();
    _renderProperties = new HwndRenderTargetProperties
    {
        Hwnd = Handle,
        PixelSize = new Size2(Size.Width, Size.Height),
        PresentOptions = PresentOptions.None
    };

    marg.Left = 0;
    marg.Top = 0;
    marg.Right = Width;
    marg.Bottom = Height;

    // Initialize DirectX
    _device = new WindowRenderTarget(_factory,
        new RenderTargetProperties(new PixelFormat(Format.B8G8R8A8_UNorm, AlphaMode.Premultiplied)),
        _renderProperties);
    
    _tokenSource = new CancellationTokenSource();
    token = _tokenSource.Token;
    
    // Start the DirectX thread
    _threadDx = new Thread(DirectXThread)
    {
        Priority = ThreadPriority.Highest,
        IsBackground = true
    };
    
    _running = true;
    TopMost = true;
    _threadDx.Start(token);

    CreateMenu();
}


    #endregion

    #region DrawFunctions

    private SolidColorBrush CreateBrush(RawColor4 color)
    {
        return new SolidColorBrush(_device, color);
    }

    private void DrawLine(int x, int y, int xTo, int yTo, SolidColorBrush color)
    {
        _device.DrawLine(new RawVector2(x, y), new RawVector2(xTo, yTo), color);
    }

    private void WriteText(string msg, float x, float y, SolidColorBrush color, float fontSize = 13,
        string fontFamily = "Arial Unicode MS")
    {
        var measure = PredictSize(msg, fontSize, fontFamily);
        _device.DrawText(msg, new TextFormat(_fontFactory, fontFamily, fontSize),
            new RawRectangleF(x, y, x + measure.Width, y + measure.Height), color);
    }

    private void WriteTextExact(string msg, float x, float y, SolidColorBrush color, float fontSize = 13,
        string fontFamily = "Arial Unicode MS")
    {
        var measure = PredictSize(msg, fontSize, fontFamily);
        _device.DrawText(msg, new TextFormat(_fontFactory, fontFamily, fontSize),
            new RawRectangleF(x, y, x + measure.Width, y + measure.Height), color);
    }

    private void WriteCenterText(string msg, float y, SolidColorBrush color, float fontSize = 13,
        string fontFamily = "Arial Unicode MS")
    {
        var measure = PredictSize(msg, fontSize, fontFamily);
        var x = Width / 2 - measure.Width / 2;
        WriteText(msg, x, y, color, fontSize, fontFamily);
    }

    private void WriteTopLeftText(string msg, SolidColorBrush color, float fontSize = 13,
    string fontFamily = "Arial Unicode MS", float xOffset = 10, float yOffset = 10)
    {
        // xOffset sets the distance from the left side (default is 10 pixels)
        var x = xOffset;
        // yOffset sets the distance from the top side (default is 10 pixels)
        var y = yOffset;

        // Write text at the calculated position
        WriteText(msg, x, y, color, fontSize, fontFamily);
    }

    private void WriteBottomText(string msg, float x, SolidColorBrush color, float fontSize = 13,
        string fontFamily = "Arial Unicode MS")
    {
        var measure = PredictSize(msg, fontSize, fontFamily);
        var y = Height - measure.Height;
        WriteTextExact(msg, x, y, color, fontSize, fontFamily);
    }

    private Size PredictSize(string msg, float fontSize = 13, string fontFamily = "Arial Unicode MS")
    {
        return TextRenderer.MeasureText(msg, new Font(fontFamily, fontSize - 3));
    }

    #endregion

    #region Quit

    private void ClosedOverlay(object sender, FormClosingEventArgs e)
    {
        try
        {
            _running = false;

            _device.Flush();
            _device.EndDraw();
            _factory.Dispose();
            _device.Dispose();
            _device = null;
        }
        catch
        {
        }
    }

    private void manualClose()
    {
        try
        {
            _running = false;

            _device.Flush();
            _device.EndDraw();
            _factory.Dispose();
            _device.Dispose();
            _device = null;
        }
        catch
        {
        }
    }

    #endregion

    #region Functions

    private Vector3 prevScreenPos = new Vector3(0, 0, 0);

    private bool WorldToScreen(Player player, Vector3 _Enemy, out Vector3 _Screen)
    {
        _Screen = new Vector3(0, 0, 0);

        var stopwatch = new Stopwatch();
        stopwatch.Start();

        ulong tempMatrixPtr = Memory.ReadPtrChain(FPSCamera, Offsets.CameraShit.viewmatrix);
        Numerics.Matrix4x4 temp = Numerics.Matrix4x4.Transpose(Memory.ReadValue<Numerics.Matrix4x4>(tempMatrixPtr + 0xDC));


        var translationVector = new Vector3(temp.M41, temp.M42, temp.M43);
        var up = new Vector3(temp.M21, temp.M22, temp.M23);
        var right = new Vector3(temp.M11, temp.M12, temp.M13);

        var w = D3DXVec3Dot(translationVector, _Enemy) + temp.M44;

        if (w < 0.098f)
            return false;

        var y = D3DXVec3Dot(up, _Enemy) + temp.M24;
        var x = D3DXVec3Dot(right, _Enemy) + temp.M14;

        _Screen.X = Width / 2 * (1f + x / w);
        _Screen.Y = Height / 2 * (1f - y / w);
        _Screen.Z = w;
        stopwatch.Stop();
        Console.WriteLine($"Head Position Draw Time: {stopwatch.ElapsedMilliseconds} ms");

        return true;
    }

    private bool WorldToScreenCombined(Player player, List<Vector3> enemyPositions, List<Vector3> screenCoords)
    {
        screenCoords.Clear(); // Clear previous results

        var stopwatch = new Stopwatch();
        stopwatch.Start();

        ulong tempMatrixPtr = Memory.ReadPtrChain(FPSCamera, Offsets.CameraShit.viewmatrix);
        Numerics.Matrix4x4 temp = Numerics.Matrix4x4.Transpose(Memory.ReadValue<Numerics.Matrix4x4>(tempMatrixPtr + 0xDC));

        var translationVector = new Vector3(temp.M41, temp.M42, temp.M43);
        var up = new Vector3(temp.M21, temp.M22, temp.M23);
        var right = new Vector3(temp.M11, temp.M12, temp.M13);

        foreach (var _Enemy in enemyPositions)
        {
            var w = D3DXVec3Dot(translationVector, _Enemy) + temp.M44;

            if (w < 0.098f)
            {
                // If the point is behind the camera, we cannot project it
                screenCoords.Add(new Vector3(0, 0, 0)); // Add a default value (or handle it differently if needed)
                continue;
            }

            var y = D3DXVec3Dot(up, _Enemy) + temp.M24;
            var x = D3DXVec3Dot(right, _Enemy) + temp.M14;

            var screenX = Width / 2 * (1f + x / w);
            var screenY = Height / 2 * (1f - y / w);

            // Add the calculated screen coordinates
            screenCoords.Add(new Vector3(screenX, screenY, w));
        }

        stopwatch.Stop();
        Console.WriteLine($"Combined Position Draw Time: {stopwatch.ElapsedMilliseconds} ms");

        return true;
    }


    private float Lerp(float start, float end, float amount)
    {
        return start + (end - start) * amount;
    }

    private bool WorldToScreenLootTest(Player player, System.Numerics.Vector3 _Item, out System.Numerics.Vector3 _Screen)
    {
        _Screen = new System.Numerics.Vector3(0, 0, 0);

        ulong tempMatrixPtr = Memory.ReadPtrChain(FPSCamera, Offsets.CameraShit.viewmatrix);
        Numerics.Matrix4x4 temp = Numerics.Matrix4x4.Transpose(Memory.ReadValue<Numerics.Matrix4x4>(tempMatrixPtr + 0xDC));


        System.Numerics.Vector3 translationVector = new System.Numerics.Vector3(temp.M41, temp.M42, temp.M43);
        System.Numerics.Vector3 up = new System.Numerics.Vector3(temp.M21, temp.M22, temp.M23);
        System.Numerics.Vector3 right = new System.Numerics.Vector3(temp.M11, temp.M12, temp.M13);

        float w = D3DXVec3Dot(translationVector, _Item) + temp.M44;

        if (w < 0.098f)
            return false;

        float y = D3DXVec3Dot(up, _Item) + temp.M24;
        float x = D3DXVec3Dot(right, _Item) + temp.M14;

        _Screen.X = (this.Width / 2) * (1f + x / w);
        _Screen.Y = (this.Height / 2) * (1f - y / w);
        _Screen.Z = w;

        return true;

    }

    private float D3DXVec3Dot(Vector3 a, Vector3 b)
    {
        return a.X * b.X +
               a.Y * b.Y +
               a.Z * b.Z;
    }

    private bool WorldToScreen(Vector3 from, Vector3 to)
    {
        var w = 0.0f;

        to.Y = _viewMatrix[0] * from.X + _viewMatrix[1] * from.Y + _viewMatrix[2] * from.Z + _viewMatrix[3];
        to.Y = _viewMatrix[4] * from.X + _viewMatrix[5] * from.Y + _viewMatrix[6] * from.Z + _viewMatrix[7];

        w = _viewMatrix[12] * from.X + _viewMatrix[13] * from.Y + _viewMatrix[14] * from.Z + _viewMatrix[15];

        if (w < 0.01f)
            return false;

        to.X *= 1.0f / w;
        to.Y *= 1.0f / w;

        var width = Size.Width;
        var height = Size.Height;

        float x = width / 2;
        float y = height / 2;

        x += 0.5f * to.X * width + 0.5f;
        y -= 0.5f * to.Y * height + 0.5f;

        to.X = x;
        to.Y = y;

        _worldToScreenPos.X = to.X;
        _worldToScreenPos.Y = to.Y;

        return true;
    }

    private void CloseOverlay()
    {
        Hide();
        //MainForm.isOverlayShown = false;
    }

    private void OverlayForm_Move(object sender, EventArgs e)
    {
        //if (menu != null && !menu.IsDisposed)
            // Update the position of the MenuForm to follow the OverlayForm
            //menu.Location = new Point(Location.X + 20, Location.Y + 20); // Adjust the offsets as needed
    }

    #endregion
}