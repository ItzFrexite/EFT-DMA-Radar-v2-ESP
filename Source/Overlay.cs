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
using static eft_dma_radar.Watchlist;
using System.Windows.Forms;
using Offsets;
using System.Globalization;
using System.Collections.Generic;

namespace eft_dma_radar;

public partial class Overlay : Form
{

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

    private bool _running;



    private frmMain _frmMain;
    private Config _config { get => Program.Config; }

    private ExfilManager exfilManager;

    public static bool isMenuShown = false;

    private float AimFOV
    {
        get => Aimbot._aimbotFOV;
        set => Aimbot._aimbotFOV = value;
    }

    private CameraManager _cameraManager;
    private Game _game;

    public static ulong FPSCamera
    {
        get => CameraManager._staticfpsCamera;  // Access static field directly
    }

    private LootManager Loot
    {
        get => Memory.Loot;
    }

    private List<Exfil> Exfils
    {
        get => Memory.Exfils;
    }

    private List<Grenade> Grenades
    {
        get => Memory.Grenades;
    }

    private List<Tripwire> Tripwires
    {
        get => Memory.Tripwires;
    }



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
            if (frmMain.guiInstance.Visible)
            {
                frmMain.guiInstance.Hide();
            }
            else
            {
                frmMain.guiInstance.Show();
                frmMain.guiInstance.TopMost = true;
            }
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

    private void CreateMenu()
    {
        frmMain.guiInstance.Show();
        frmMain.guiInstance.Owner = this;
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
                if (frmMain.guiInstance.Visible)
                {
                    if (frmMain.guiInstance.InvokeRequired)
                    {
                        frmMain.guiInstance.Invoke((MethodInvoker)delegate
                        {
                            frmMain.guiInstance.TopMost = true;
                            frmMain.guiInstance.BringToFront();
                            frmMain.guiInstance.Focus();
                        });
                    }
                    else
                    {
                        // This block runs if we're already on the UI thread
                        frmMain.guiInstance.TopMost = true;
                        frmMain.guiInstance.BringToFront();
                        frmMain.guiInstance.Focus();
                    }
                }
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

                    while (FPSCamera == 0)
                    {
                        Thread.Sleep(5);
                    }

                    if (allPlayers is not null)
                    {
                        var localplayer = this.LocalPlayer;
                        var localPlayerPos = localPlayer.Position;

                        #region Player ESP
                        foreach (var player in allPlayers)
                            {
                                var playerHeadPos = new Vector3(player.HeadPosition.X, player.HeadPosition.Z, player.HeadPosition.Y);
                                var playerSpine3Pos = new Vector3(player.Spine3Position.X, player.Spine3Position.Z, player.Spine3Position.Y);
                                var playerLPalmPos = new Vector3(player.LPalmPosition.X, player.LPalmPosition.Z, player.LPalmPosition.Y);
                                var playerRPalmPos = new Vector3(player.RPalmPosition.X, player.RPalmPosition.Z, player.RPalmPosition.Y);
                                var playerPelvisPos = new Vector3(player.PelvisPosition.X, player.PelvisPosition.Z, player.PelvisPosition.Y);
                                var playerLFootPos = new Vector3(player.LFootPosition.X, player.LFootPosition.Z, player.LFootPosition.Y);
                                var playerRFootPos = new Vector3(player.RFootPosition.X, player.RFootPosition.Z, player.RFootPosition.Y);
                                var playerBasePos = new Vector3(player.Position.X, player.Position.Z, player.Position.Y);
                                var playerLForearm1Pos = new Vector3(player.LForearm1Position.X, player.LForearm1Position.Z, player.LForearm1Position.Y);
                                var playerRForearm1Pos = new Vector3(player.RForearm1Position.X, player.RForearm1Position.Z, player.RForearm1Position.Y);
                                var playerLCalfPos = new Vector3(player.LCalfPosition.X, player.LCalfPosition.Z, player.LCalfPosition.Y);
                                var playerRCalfPos = new Vector3(player.RCalfPosition.X, player.RCalfPosition.Z, player.RCalfPosition.Y);

                                var dist = Vector3.Distance(localPlayerPos, player.Position);

                                // Check if player is valid for ESP drawing
                                if ((player.IsAlive && player.Type is not PlayerType.LocalPlayer && dist <= _config.PlayerDist && _config.ToggleESP)
                                    || (player.Type is not PlayerType.LocalPlayer && !player.IsHuman && player.IsAlive && dist <= _config.ScavDist && _config.ToggleESP)
                                    || (player.Type is not PlayerType.LocalPlayer && player.Type is PlayerType.Teammate && player.IsAlive && dist <= _config.TeamDist && _config.ToggleESP))
                                {
                                    List<Vector3> enemyPositions = new List<Vector3>
                                    {
                                        playerBasePos, // Base position (foot)
                                        playerHeadPos, // Head position
                                        playerSpine3Pos, // Spine position
                                        playerLPalmPos, // Left palm position
                                        playerRPalmPos, // Right palm position
                                        playerPelvisPos, // Pelvis position
                                        playerLFootPos, // Left foot position
                                        playerRFootPos,
                                        playerLForearm1Pos,
                                        playerRForearm1Pos,
                                        playerLCalfPos,
                                        playerLCalfPos,
                                    };

                                    List<Vector3> coords = new List<Vector3>();
                                    WorldToScreenCombined(player, enemyPositions, coords);

                                    Vector3 baseCoords = coords[0]; // Foot position
                                    Vector3 headCoords = coords[1]; // Head position
                                    Vector3 spine3Coords = coords[2]; // Spine position
                                    Vector3 lPalmCoords = coords[3]; // Left palm position
                                    Vector3 rPalmCoords = coords[4]; // Right palm position
                                    Vector3 pelvisCoords = coords[5]; // Pelvis position
                                    Vector3 lFootCoords = coords[6]; // Left foot position
                                    Vector3 rFootCoords = coords[7]; // Right foot position
                                    Vector3 lForearm1Coords = coords[8];
                                    Vector3 rForearm1Coords = coords[9];
                                    Vector3 lCalfCoords = coords[10];
                                    Vector3 rCalfCoords = coords[11];

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
                                        if ((player.Type is PlayerType.BEAR || player.Type is PlayerType.USEC) && _config.PlayerESP && dist <= _config.PlayerDist)
                                        {
                                            if (_config.BoneESP == false || dist > _config.BoneLimit)
                                            {
                                                if (_config.BoxESP == true)
                                                {
                                                    // Set the top and bottom coordinates for the bounding box
                                                    _device.DrawRectangle(
                                                        new RawRectangleF(baseCoords.X - (boxWidth / 2), headCoords.Y + paddingHeight, // Top line at head position + padding
                                                                          baseCoords.X + (boxWidth / 2), baseCoords.Y - paddingHeight), // Bottom line at foot position - padding
                                                        Brushes.RED);
                                                }
                                                if (_config.HeadDotESP == true)
                                                {
                                                    // Calculate the head dot size as 30% of the bounding box width
                                                    float headDotSize = boxWidth * 0.2f; // Set head ellipse size to 20% of the bounding box width
                                                    _device.DrawEllipse(new Ellipse(new RawVector2(headCoords.X - (headDotSize / 2), headCoords.Y - (headDotSize / 2)), headDotSize, headDotSize), Brushes.WHITE); // Head ellipse outline
                                                }
                                            }
                                            else if (dist <= _config.BoneLimit)
                                            {
                                                if (_config.BoxESP == true)
                                                {
                                                    //Console.WriteLine("Player " + player.Name);
                                                    _device.DrawRectangle(
                                                    new RawRectangleF(baseCoords.X - (boxWidth / 2), headCoords.Y + paddingHeight, // Top line at head position + padding
                                                                      baseCoords.X + (boxWidth / 2), baseCoords.Y - paddingHeight), // Bottom line at foot position - padding
                                                    Brushes.RED);
                                                }
                                                // Head to Spine3
                                                _device.DrawLine(new RawVector2(headCoords.X, headCoords.Y),
                                                                 new RawVector2(spine3Coords.X, spine3Coords.Y),
                                                                 Brushes.WHITE, 2.0f);

                                                // Spine3 to Pelvis
                                                _device.DrawLine(new RawVector2(spine3Coords.X, spine3Coords.Y),
                                                                 new RawVector2(pelvisCoords.X, pelvisCoords.Y),
                                                                 Brushes.WHITE, 2.0f);

                                                // Spine3 to Left Forearm1
                                                _device.DrawLine(new RawVector2(spine3Coords.X, spine3Coords.Y),
                                                                 new RawVector2(lForearm1Coords.X, lForearm1Coords.Y),
                                                                 Brushes.WHITE, 2.0f);
                                                // Left Forearm1 to Left Palm
                                                _device.DrawLine(new RawVector2(lForearm1Coords.X, lForearm1Coords.Y),
                                                                 new RawVector2(lPalmCoords.X, lPalmCoords.Y),
                                                                 Brushes.WHITE, 2.0f);

                                                // Spine3 to Right Forearm1
                                                _device.DrawLine(new RawVector2(spine3Coords.X, spine3Coords.Y),
                                                                 new RawVector2(rForearm1Coords.X, rForearm1Coords.Y),
                                                                 Brushes.WHITE, 2.0f);

                                                // Right Forearm1 to Right Palm
                                                _device.DrawLine(new RawVector2(rForearm1Coords.X, rForearm1Coords.Y),
                                                                 new RawVector2(rPalmCoords.X, rPalmCoords.Y),
                                                                 Brushes.WHITE, 2.0f);

                                                // Pelvis to Left Calf
                                                _device.DrawLine(new RawVector2(pelvisCoords.X, pelvisCoords.Y),
                                                                 new RawVector2(lCalfCoords.X, lCalfCoords.Y),
                                                                 Brushes.WHITE, 2.0f);

                                                // Left Calf to Left Foot
                                                _device.DrawLine(new RawVector2(lCalfCoords.X, lCalfCoords.Y),
                                                                 new RawVector2(lFootCoords.X, lFootCoords.Y),
                                                                 Brushes.WHITE, 2.0f);

                                                // Pelvis to Right Calf
                                                _device.DrawLine(new RawVector2(pelvisCoords.X, pelvisCoords.Y),
                                                                 new RawVector2(rCalfCoords.X, rCalfCoords.Y),
                                                                 Brushes.WHITE, 2.0f);

                                                // Right Calf to Right Foot
                                                _device.DrawLine(new RawVector2(rCalfCoords.X, rCalfCoords.Y),
                                                                 new RawVector2(rFootCoords.X, rFootCoords.Y),
                                                                 Brushes.WHITE, 2.0f);


                                            }

                                            // Display text information
                                            WriteText(player.Name + Environment.NewLine + Math.Round(dist, 0) + "m", baseCoords.X - (paddingWidth * 15), headCoords.Y - 5, Brushes.WHITE);
                                        }
                                        #endregion
                                        #region Scav
                                        if (player.Type is PlayerType.Scav && _config.ScavESP && dist <= _config.ScavDist)
                                        {
                                            if (_config.BoneESP == false || dist > _config.BoneLimit)
                                            {
                                                if (_config.BoxESP == true)
                                                {
                                                    // Set the top and bottom coordinates for the bounding box
                                                    _device.DrawRectangle(
                                                        new RawRectangleF(baseCoords.X - (boxWidth / 2), headCoords.Y + paddingHeight, // Top line at head position + padding
                                                                          baseCoords.X + (boxWidth / 2), baseCoords.Y - paddingHeight), // Bottom line at foot position - padding
                                                        Brushes.YELLOW);
                                                }
                                                if (_config.HeadDotESP == true)
                                                {
                                                    // Calculate the head dot size as 30% of the bounding box width
                                                    float headDotSize = boxWidth * 0.2f; // Set head ellipse size to 20% of the bounding box width
                                                    _device.DrawEllipse(new Ellipse(new RawVector2(headCoords.X - (headDotSize / 2), headCoords.Y - (headDotSize / 2)), headDotSize, headDotSize), Brushes.WHITE); // Head ellipse outline
                                                }
                                            }
                                            else if (dist <= _config.BoneLimit)
                                            {
                                                if (_config.BoxESP == true)
                                                {
                                                    //Console.WriteLine("Player " + player.Name);
                                                    _device.DrawRectangle(
                                                    new RawRectangleF(baseCoords.X - (boxWidth / 2), headCoords.Y + paddingHeight, // Top line at head position + padding
                                                                      baseCoords.X + (boxWidth / 2), baseCoords.Y - paddingHeight), // Bottom line at foot position - padding
                                                    Brushes.YELLOW);
                                                }
                                                // Head to Spine3
                                                _device.DrawLine(new RawVector2(headCoords.X, headCoords.Y),
                                                                 new RawVector2(spine3Coords.X, spine3Coords.Y),
                                                                 Brushes.WHITE, 2.0f);

                                                // Spine3 to Pelvis
                                                _device.DrawLine(new RawVector2(spine3Coords.X, spine3Coords.Y),
                                                                 new RawVector2(pelvisCoords.X, pelvisCoords.Y),
                                                                 Brushes.WHITE, 2.0f);

                                                // Spine3 to Left Forearm1
                                                _device.DrawLine(new RawVector2(spine3Coords.X, spine3Coords.Y),
                                                                 new RawVector2(lForearm1Coords.X, lForearm1Coords.Y),
                                                                 Brushes.WHITE, 2.0f);
                                                // Left Forearm1 to Left Palm
                                                _device.DrawLine(new RawVector2(lForearm1Coords.X, lForearm1Coords.Y),
                                                                 new RawVector2(lPalmCoords.X, lPalmCoords.Y),
                                                                 Brushes.WHITE, 2.0f);

                                                // Spine3 to Right Forearm1
                                                _device.DrawLine(new RawVector2(spine3Coords.X, spine3Coords.Y),
                                                                 new RawVector2(rForearm1Coords.X, rForearm1Coords.Y),
                                                                 Brushes.WHITE, 2.0f);

                                                // Right Forearm1 to Right Palm
                                                _device.DrawLine(new RawVector2(rForearm1Coords.X, rForearm1Coords.Y),
                                                                 new RawVector2(rPalmCoords.X, rPalmCoords.Y),
                                                                 Brushes.WHITE, 2.0f);

                                                // Pelvis to Left Calf
                                                _device.DrawLine(new RawVector2(pelvisCoords.X, pelvisCoords.Y),
                                                                 new RawVector2(lCalfCoords.X, lCalfCoords.Y),
                                                                 Brushes.WHITE, 2.0f);

                                                // Left Calf to Left Foot
                                                _device.DrawLine(new RawVector2(lCalfCoords.X, lCalfCoords.Y),
                                                                 new RawVector2(lFootCoords.X, lFootCoords.Y),
                                                                 Brushes.WHITE, 2.0f);

                                                // Pelvis to Right Calf
                                                _device.DrawLine(new RawVector2(pelvisCoords.X, pelvisCoords.Y),
                                                                 new RawVector2(rCalfCoords.X, rCalfCoords.Y),
                                                                 Brushes.WHITE, 2.0f);

                                                // Right Calf to Right Foot
                                                _device.DrawLine(new RawVector2(rCalfCoords.X, rCalfCoords.Y),
                                                                 new RawVector2(rFootCoords.X, rFootCoords.Y),
                                                                 Brushes.WHITE, 2.0f);


                                            }

                                            // Display text information
                                            //WriteText("Scav" + Environment.NewLine + Math.Round(dist, 0) + "m", baseCoords.X - (paddingWidth * 15), headCoords.Y - 5, Brushes.WHITE);

                                            WriteText("Scav" + Environment.NewLine + Math.Round(dist, 0) + "m", baseCoords.X - (paddingWidth * 15), headCoords.Y - 5, Brushes.WHITE);

                                        }
                                        #endregion
                                        #region Boss
                                        if (player.Type is PlayerType.Boss && _config.BossESP && dist <= _config.ScavDist)
                                        {
                                            if (_config.BoneESP == false || dist > _config.BoneLimit)
                                            {
                                                if (_config.BoxESP == true)
                                                {
                                                    // Set the top and bottom coordinates for the bounding box
                                                    _device.DrawRectangle(
                                                        new RawRectangleF(baseCoords.X - (boxWidth / 2), headCoords.Y + paddingHeight, // Top line at head position + padding
                                                                          baseCoords.X + (boxWidth / 2), baseCoords.Y - paddingHeight), // Bottom line at foot position - padding
                                                        Brushes.MAGENTA);
                                                }
                                                if (_config.HeadDotESP == true)
                                                {
                                                    // Calculate the head dot size as 30% of the bounding box width
                                                    float headDotSize = boxWidth * 0.2f; // Set head ellipse size to 20% of the bounding box width
                                                    _device.DrawEllipse(new Ellipse(new RawVector2(headCoords.X - (headDotSize / 2), headCoords.Y - (headDotSize / 2)), headDotSize, headDotSize), Brushes.WHITE); // Head ellipse outline
                                                }
                                            }
                                            else if (dist <= _config.BoneLimit)
                                            {
                                                if (_config.BoxESP == true)
                                                {
                                                    //Console.WriteLine("Player " + player.Name);
                                                    _device.DrawRectangle(
                                                    new RawRectangleF(baseCoords.X - (boxWidth / 2), headCoords.Y + paddingHeight, // Top line at head position + padding
                                                                      baseCoords.X + (boxWidth / 2), baseCoords.Y - paddingHeight), // Bottom line at foot position - padding
                                                    Brushes.MAGENTA);
                                                }
                                                // Head to Spine3
                                                _device.DrawLine(new RawVector2(headCoords.X, headCoords.Y),
                                                                 new RawVector2(spine3Coords.X, spine3Coords.Y),
                                                                 Brushes.WHITE, 2.0f);

                                                // Spine3 to Pelvis
                                                _device.DrawLine(new RawVector2(spine3Coords.X, spine3Coords.Y),
                                                                 new RawVector2(pelvisCoords.X, pelvisCoords.Y),
                                                                 Brushes.WHITE, 2.0f);

                                                // Spine3 to Left Forearm1
                                                _device.DrawLine(new RawVector2(spine3Coords.X, spine3Coords.Y),
                                                                 new RawVector2(lForearm1Coords.X, lForearm1Coords.Y),
                                                                 Brushes.WHITE, 2.0f);
                                                // Left Forearm1 to Left Palm
                                                _device.DrawLine(new RawVector2(lForearm1Coords.X, lForearm1Coords.Y),
                                                                 new RawVector2(lPalmCoords.X, lPalmCoords.Y),
                                                                 Brushes.WHITE, 2.0f);

                                                // Spine3 to Right Forearm1
                                                _device.DrawLine(new RawVector2(spine3Coords.X, spine3Coords.Y),
                                                                 new RawVector2(rForearm1Coords.X, rForearm1Coords.Y),
                                                                 Brushes.WHITE, 2.0f);

                                                // Right Forearm1 to Right Palm
                                                _device.DrawLine(new RawVector2(rForearm1Coords.X, rForearm1Coords.Y),
                                                                 new RawVector2(rPalmCoords.X, rPalmCoords.Y),
                                                                 Brushes.WHITE, 2.0f);

                                                // Pelvis to Left Calf
                                                _device.DrawLine(new RawVector2(pelvisCoords.X, pelvisCoords.Y),
                                                                 new RawVector2(lCalfCoords.X, lCalfCoords.Y),
                                                                 Brushes.WHITE, 2.0f);

                                                // Left Calf to Left Foot
                                                _device.DrawLine(new RawVector2(lCalfCoords.X, lCalfCoords.Y),
                                                                 new RawVector2(lFootCoords.X, lFootCoords.Y),
                                                                 Brushes.WHITE, 2.0f);

                                                // Pelvis to Right Calf
                                                _device.DrawLine(new RawVector2(pelvisCoords.X, pelvisCoords.Y),
                                                                 new RawVector2(rCalfCoords.X, rCalfCoords.Y),
                                                                 Brushes.WHITE, 2.0f);

                                                // Right Calf to Right Foot
                                                _device.DrawLine(new RawVector2(rCalfCoords.X, rCalfCoords.Y),
                                                                 new RawVector2(rFootCoords.X, rFootCoords.Y),
                                                                 Brushes.WHITE, 2.0f);


                                            }

                                            // Display text information
                                            WriteText(player.Name + Environment.NewLine + Math.Round(dist, 0) + "m", baseCoords.X - (paddingWidth * 15), headCoords.Y - 5, Brushes.WHITE);


                                        }
                                        #endregion
                                        #region BossFollower
                                        if ((player.Type is PlayerType.BossFollower || player.Type is PlayerType.BossGuard) && _config.ScavESP && dist <= _config.ScavDist)
                                        {
                                            if (_config.BoneESP == false || dist > _config.BoneLimit)
                                            {
                                                if (_config.BoxESP == true)
                                                {
                                                    // Set the top and bottom coordinates for the bounding box
                                                    _device.DrawRectangle(
                                                        new RawRectangleF(baseCoords.X - (boxWidth / 2), headCoords.Y + paddingHeight, // Top line at head position + padding
                                                                          baseCoords.X + (boxWidth / 2), baseCoords.Y - paddingHeight), // Bottom line at foot position - padding
                                                        Brushes.PURPLE);
                                                }
                                                if (_config.HeadDotESP == true)
                                                {
                                                    // Calculate the head dot size as 30% of the bounding box width
                                                    float headDotSize = boxWidth * 0.2f; // Set head ellipse size to 20% of the bounding box width
                                                    _device.DrawEllipse(new Ellipse(new RawVector2(headCoords.X - (headDotSize / 2), headCoords.Y - (headDotSize / 2)), headDotSize, headDotSize), Brushes.WHITE); // Head ellipse outline
                                                }
                                            }
                                            else if (dist <= _config.BoneLimit)
                                            {
                                                if (_config.BoxESP == true)
                                                {
                                                    //Console.WriteLine("Player " + player.Name);
                                                    _device.DrawRectangle(
                                                    new RawRectangleF(baseCoords.X - (boxWidth / 2), headCoords.Y + paddingHeight, // Top line at head position + padding
                                                                      baseCoords.X + (boxWidth / 2), baseCoords.Y - paddingHeight), // Bottom line at foot position - padding
                                                    Brushes.PURPLE);
                                                }
                                                // Head to Spine3
                                                _device.DrawLine(new RawVector2(headCoords.X, headCoords.Y),
                                                                 new RawVector2(spine3Coords.X, spine3Coords.Y),
                                                                 Brushes.WHITE, 2.0f);

                                                // Spine3 to Pelvis
                                                _device.DrawLine(new RawVector2(spine3Coords.X, spine3Coords.Y),
                                                                 new RawVector2(pelvisCoords.X, pelvisCoords.Y),
                                                                 Brushes.WHITE, 2.0f);

                                                // Spine3 to Left Forearm1
                                                _device.DrawLine(new RawVector2(spine3Coords.X, spine3Coords.Y),
                                                                 new RawVector2(lForearm1Coords.X, lForearm1Coords.Y),
                                                                 Brushes.WHITE, 2.0f);
                                                // Left Forearm1 to Left Palm
                                                _device.DrawLine(new RawVector2(lForearm1Coords.X, lForearm1Coords.Y),
                                                                 new RawVector2(lPalmCoords.X, lPalmCoords.Y),
                                                                 Brushes.WHITE, 2.0f);

                                                // Spine3 to Right Forearm1
                                                _device.DrawLine(new RawVector2(spine3Coords.X, spine3Coords.Y),
                                                                 new RawVector2(rForearm1Coords.X, rForearm1Coords.Y),
                                                                 Brushes.WHITE, 2.0f);

                                                // Right Forearm1 to Right Palm
                                                _device.DrawLine(new RawVector2(rForearm1Coords.X, rForearm1Coords.Y),
                                                                 new RawVector2(rPalmCoords.X, rPalmCoords.Y),
                                                                 Brushes.WHITE, 2.0f);

                                                // Pelvis to Left Calf
                                                _device.DrawLine(new RawVector2(pelvisCoords.X, pelvisCoords.Y),
                                                                 new RawVector2(lCalfCoords.X, lCalfCoords.Y),
                                                                 Brushes.WHITE, 2.0f);

                                                // Left Calf to Left Foot
                                                _device.DrawLine(new RawVector2(lCalfCoords.X, lCalfCoords.Y),
                                                                 new RawVector2(lFootCoords.X, lFootCoords.Y),
                                                                 Brushes.WHITE, 2.0f);

                                                // Pelvis to Right Calf
                                                _device.DrawLine(new RawVector2(pelvisCoords.X, pelvisCoords.Y),
                                                                 new RawVector2(rCalfCoords.X, rCalfCoords.Y),
                                                                 Brushes.WHITE, 2.0f);

                                                // Right Calf to Right Foot
                                                _device.DrawLine(new RawVector2(rCalfCoords.X, rCalfCoords.Y),
                                                                 new RawVector2(rFootCoords.X, rFootCoords.Y),
                                                                 Brushes.WHITE, 2.0f);


                                            }

                                            // Display text information
                                            WriteText("Follower" + Environment.NewLine + Math.Round(dist, 0) + "m", baseCoords.X - (paddingWidth * 15), headCoords.Y - 5, Brushes.WHITE);

                                        }
                                        #endregion
                                        #region Teammate
                                        if (player.Type is PlayerType.Teammate && _config.TeamESP && dist <= _config.TeamDist)
                                        {
                                            if (_config.BoneESP == false || dist > _config.BoneLimit)
                                            {
                                                if (_config.BoxESP == true)
                                                {
                                                    // Set the top and bottom coordinates for the bounding box
                                                    _device.DrawRectangle(
                                                        new RawRectangleF(baseCoords.X - (boxWidth / 2), headCoords.Y + paddingHeight, // Top line at head position + padding
                                                                          baseCoords.X + (boxWidth / 2), baseCoords.Y - paddingHeight), // Bottom line at foot position - padding
                                                        Brushes.GREEN);
                                                }
                                                if (_config.HeadDotESP == true)
                                                {
                                                    // Calculate the head dot size as 30% of the bounding box width
                                                    float headDotSize = boxWidth * 0.2f; // Set head ellipse size to 20% of the bounding box width
                                                    _device.DrawEllipse(new Ellipse(new RawVector2(headCoords.X - (headDotSize / 2), headCoords.Y - (headDotSize / 2)), headDotSize, headDotSize), Brushes.WHITE); // Head ellipse outline
                                                }
                                            }
                                            else if (dist <= _config.BoneLimit)
                                            {
                                                if (_config.BoxESP == true)
                                                {
                                                    //Console.WriteLine("Player " + player.Name);
                                                    _device.DrawRectangle(
                                                    new RawRectangleF(baseCoords.X - (boxWidth / 2), headCoords.Y + paddingHeight, // Top line at head position + padding
                                                                      baseCoords.X + (boxWidth / 2), baseCoords.Y - paddingHeight), // Bottom line at foot position - padding
                                                    Brushes.GREEN);
                                                }
                                                // Head to Spine3
                                                _device.DrawLine(new RawVector2(headCoords.X, headCoords.Y),
                                                                 new RawVector2(spine3Coords.X, spine3Coords.Y),
                                                                 Brushes.WHITE, 2.0f);

                                                // Spine3 to Pelvis
                                                _device.DrawLine(new RawVector2(spine3Coords.X, spine3Coords.Y),
                                                                 new RawVector2(pelvisCoords.X, pelvisCoords.Y),
                                                                 Brushes.WHITE, 2.0f);

                                                // Spine3 to Left Forearm1
                                                _device.DrawLine(new RawVector2(spine3Coords.X, spine3Coords.Y),
                                                                 new RawVector2(lForearm1Coords.X, lForearm1Coords.Y),
                                                                 Brushes.WHITE, 2.0f);
                                                // Left Forearm1 to Left Palm
                                                _device.DrawLine(new RawVector2(lForearm1Coords.X, lForearm1Coords.Y),
                                                                 new RawVector2(lPalmCoords.X, lPalmCoords.Y),
                                                                 Brushes.WHITE, 2.0f);

                                                // Spine3 to Right Forearm1
                                                _device.DrawLine(new RawVector2(spine3Coords.X, spine3Coords.Y),
                                                                 new RawVector2(rForearm1Coords.X, rForearm1Coords.Y),
                                                                 Brushes.WHITE, 2.0f);

                                                // Right Forearm1 to Right Palm
                                                _device.DrawLine(new RawVector2(rForearm1Coords.X, rForearm1Coords.Y),
                                                                 new RawVector2(rPalmCoords.X, rPalmCoords.Y),
                                                                 Brushes.WHITE, 2.0f);

                                                // Pelvis to Left Calf
                                                _device.DrawLine(new RawVector2(pelvisCoords.X, pelvisCoords.Y),
                                                                 new RawVector2(lCalfCoords.X, lCalfCoords.Y),
                                                                 Brushes.WHITE, 2.0f);

                                                // Left Calf to Left Foot
                                                _device.DrawLine(new RawVector2(lCalfCoords.X, lCalfCoords.Y),
                                                                 new RawVector2(lFootCoords.X, lFootCoords.Y),
                                                                 Brushes.WHITE, 2.0f);

                                                // Pelvis to Right Calf
                                                _device.DrawLine(new RawVector2(pelvisCoords.X, pelvisCoords.Y),
                                                                 new RawVector2(rCalfCoords.X, rCalfCoords.Y),
                                                                 Brushes.WHITE, 2.0f);

                                                // Right Calf to Right Foot
                                                _device.DrawLine(new RawVector2(rCalfCoords.X, rCalfCoords.Y),
                                                                 new RawVector2(rFootCoords.X, rFootCoords.Y),
                                                                 Brushes.WHITE, 2.0f);


                                            }

                                            // Display text information
                                            WriteText(player.Name + Environment.NewLine + Math.Round(dist, 0) + "m", baseCoords.X - (paddingWidth * 15), headCoords.Y - 5, Brushes.WHITE);
                                        }
                                        #endregion
                                        #region Cultist
                                        if (player.Type is PlayerType.Cultist && _config.ScavESP && dist <= _config.ScavDist)
                                        {
                                            if (_config.BoneESP == false || dist > _config.BoneLimit)
                                            {
                                                if (_config.BoxESP == true)
                                                {
                                                    // Set the top and bottom coordinates for the bounding box
                                                    _device.DrawRectangle(
                                                        new RawRectangleF(baseCoords.X - (boxWidth / 2), headCoords.Y + paddingHeight, // Top line at head position + padding
                                                                          baseCoords.X + (boxWidth / 2), baseCoords.Y - paddingHeight), // Bottom line at foot position - padding
                                                        Brushes.ORANGE);
                                                }
                                                if (_config.HeadDotESP == true)
                                                {
                                                    // Calculate the head dot size as 30% of the bounding box width
                                                    float headDotSize = boxWidth * 0.2f; // Set head ellipse size to 20% of the bounding box width
                                                    _device.DrawEllipse(new Ellipse(new RawVector2(headCoords.X - (headDotSize / 2), headCoords.Y - (headDotSize / 2)), headDotSize, headDotSize), Brushes.WHITE); // Head ellipse outline
                                                }
                                            }
                                            else if (dist <= _config.BoneLimit)
                                            {
                                                if (_config.BoxESP == true)
                                                {
                                                    //Console.WriteLine("Player " + player.Name);
                                                    _device.DrawRectangle(
                                                    new RawRectangleF(baseCoords.X - (boxWidth / 2), headCoords.Y + paddingHeight, // Top line at head position + padding
                                                                      baseCoords.X + (boxWidth / 2), baseCoords.Y - paddingHeight), // Bottom line at foot position - padding
                                                    Brushes.ORANGE);
                                                }
                                                // Head to Spine3
                                                _device.DrawLine(new RawVector2(headCoords.X, headCoords.Y),
                                                                 new RawVector2(spine3Coords.X, spine3Coords.Y),
                                                                 Brushes.WHITE, 2.0f);

                                                // Spine3 to Pelvis
                                                _device.DrawLine(new RawVector2(spine3Coords.X, spine3Coords.Y),
                                                                 new RawVector2(pelvisCoords.X, pelvisCoords.Y),
                                                                 Brushes.WHITE, 2.0f);

                                                // Spine3 to Left Forearm1
                                                _device.DrawLine(new RawVector2(spine3Coords.X, spine3Coords.Y),
                                                                 new RawVector2(lForearm1Coords.X, lForearm1Coords.Y),
                                                                 Brushes.WHITE, 2.0f);
                                                // Left Forearm1 to Left Palm
                                                _device.DrawLine(new RawVector2(lForearm1Coords.X, lForearm1Coords.Y),
                                                                 new RawVector2(lPalmCoords.X, lPalmCoords.Y),
                                                                 Brushes.WHITE, 2.0f);

                                                // Spine3 to Right Forearm1
                                                _device.DrawLine(new RawVector2(spine3Coords.X, spine3Coords.Y),
                                                                 new RawVector2(rForearm1Coords.X, rForearm1Coords.Y),
                                                                 Brushes.WHITE, 2.0f);

                                                // Right Forearm1 to Right Palm
                                                _device.DrawLine(new RawVector2(rForearm1Coords.X, rForearm1Coords.Y),
                                                                 new RawVector2(rPalmCoords.X, rPalmCoords.Y),
                                                                 Brushes.WHITE, 2.0f);

                                                // Pelvis to Left Calf
                                                _device.DrawLine(new RawVector2(pelvisCoords.X, pelvisCoords.Y),
                                                                 new RawVector2(lCalfCoords.X, lCalfCoords.Y),
                                                                 Brushes.WHITE, 2.0f);

                                                // Left Calf to Left Foot
                                                _device.DrawLine(new RawVector2(lCalfCoords.X, lCalfCoords.Y),
                                                                 new RawVector2(lFootCoords.X, lFootCoords.Y),
                                                                 Brushes.WHITE, 2.0f);

                                                // Pelvis to Right Calf
                                                _device.DrawLine(new RawVector2(pelvisCoords.X, pelvisCoords.Y),
                                                                 new RawVector2(rCalfCoords.X, rCalfCoords.Y),
                                                                 Brushes.WHITE, 2.0f);

                                                // Right Calf to Right Foot
                                                _device.DrawLine(new RawVector2(rCalfCoords.X, rCalfCoords.Y),
                                                                 new RawVector2(rFootCoords.X, rFootCoords.Y),
                                                                 Brushes.WHITE, 2.0f);


                                            }

                                            // Display text information
                                            WriteText("Cultist" + Environment.NewLine + Math.Round(dist, 0) + "m", baseCoords.X - (paddingWidth * 15), headCoords.Y - 5, Brushes.WHITE);
                                        }
                                        #endregion
                                        #region PlayerScav
                                        if (player.Type is PlayerType.PlayerScav && _config.PlayerESP && dist <= _config.PlayerDist)
                                        {
                                            if (_config.BoneESP == false || dist > _config.BoneLimit)
                                            {
                                                if (_config.BoxESP == true)
                                                {
                                                    // Set the top and bottom coordinates for the bounding box
                                                    _device.DrawRectangle(
                                                        new RawRectangleF(baseCoords.X - (boxWidth / 2), headCoords.Y + paddingHeight, // Top line at head position + padding
                                                                          baseCoords.X + (boxWidth / 2), baseCoords.Y - paddingHeight), // Bottom line at foot position - padding
                                                        Brushes.BLUE);
                                                }
                                                if (_config.HeadDotESP == true)
                                                {
                                                    // Calculate the head dot size as 30% of the bounding box width
                                                    float headDotSize = boxWidth * 0.2f; // Set head ellipse size to 20% of the bounding box width
                                                    _device.DrawEllipse(new Ellipse(new RawVector2(headCoords.X - (headDotSize / 2), headCoords.Y - (headDotSize / 2)), headDotSize, headDotSize), Brushes.WHITE); // Head ellipse outline
                                                }
                                            }
                                            else if (dist <= _config.BoneLimit)
                                            {
                                                if (_config.BoxESP == true)
                                                {
                                                    //Console.WriteLine("Player " + player.Name);
                                                    _device.DrawRectangle(
                                                    new RawRectangleF(baseCoords.X - (boxWidth / 2), headCoords.Y + paddingHeight, // Top line at head position + padding
                                                                      baseCoords.X + (boxWidth / 2), baseCoords.Y - paddingHeight), // Bottom line at foot position - padding
                                                    Brushes.BLUE);
                                                }
                                                // Head to Spine3
                                                _device.DrawLine(new RawVector2(headCoords.X, headCoords.Y),
                                                                 new RawVector2(spine3Coords.X, spine3Coords.Y),
                                                                 Brushes.WHITE, 2.0f);

                                                // Spine3 to Pelvis
                                                _device.DrawLine(new RawVector2(spine3Coords.X, spine3Coords.Y),
                                                                 new RawVector2(pelvisCoords.X, pelvisCoords.Y),
                                                                 Brushes.WHITE, 2.0f);

                                                // Spine3 to Left Forearm1
                                                _device.DrawLine(new RawVector2(spine3Coords.X, spine3Coords.Y),
                                                                 new RawVector2(lForearm1Coords.X, lForearm1Coords.Y),
                                                                 Brushes.WHITE, 2.0f);
                                                // Left Forearm1 to Left Palm
                                                _device.DrawLine(new RawVector2(lForearm1Coords.X, lForearm1Coords.Y),
                                                                 new RawVector2(lPalmCoords.X, lPalmCoords.Y),
                                                                 Brushes.WHITE, 2.0f);

                                                // Spine3 to Right Forearm1
                                                _device.DrawLine(new RawVector2(spine3Coords.X, spine3Coords.Y),
                                                                 new RawVector2(rForearm1Coords.X, rForearm1Coords.Y),
                                                                 Brushes.WHITE, 2.0f);

                                                // Right Forearm1 to Right Palm
                                                _device.DrawLine(new RawVector2(rForearm1Coords.X, rForearm1Coords.Y),
                                                                 new RawVector2(rPalmCoords.X, rPalmCoords.Y),
                                                                 Brushes.WHITE, 2.0f);

                                                // Pelvis to Left Calf
                                                _device.DrawLine(new RawVector2(pelvisCoords.X, pelvisCoords.Y),
                                                                 new RawVector2(lCalfCoords.X, lCalfCoords.Y),
                                                                 Brushes.WHITE, 2.0f);

                                                // Left Calf to Left Foot
                                                _device.DrawLine(new RawVector2(lCalfCoords.X, lCalfCoords.Y),
                                                                 new RawVector2(lFootCoords.X, lFootCoords.Y),
                                                                 Brushes.WHITE, 2.0f);

                                                // Pelvis to Right Calf
                                                _device.DrawLine(new RawVector2(pelvisCoords.X, pelvisCoords.Y),
                                                                 new RawVector2(rCalfCoords.X, rCalfCoords.Y),
                                                                 Brushes.WHITE, 2.0f);

                                                // Right Calf to Right Foot
                                                _device.DrawLine(new RawVector2(rCalfCoords.X, rCalfCoords.Y),
                                                                 new RawVector2(rFootCoords.X, rFootCoords.Y),
                                                                 Brushes.WHITE, 2.0f);


                                            }

                                            // Display text information
                                            WriteText("Player Scav" + Environment.NewLine + Math.Round(dist, 0) + "m", baseCoords.X - (paddingWidth * 15), headCoords.Y - 5, Brushes.WHITE);
                                        }
                                        #endregion
                                        #region Raider
                                        if (player.Type is PlayerType.Raider && _config.ScavESP && dist <= _config.ScavDist)
                                        {
                                            if (_config.BoneESP == false || dist > _config.BoneLimit)
                                            {
                                                if (_config.BoxESP == true)
                                                {
                                                    // Set the top and bottom coordinates for the bounding box
                                                    _device.DrawRectangle(
                                                        new RawRectangleF(baseCoords.X - (boxWidth / 2), headCoords.Y + paddingHeight, // Top line at head position + padding
                                                                          baseCoords.X + (boxWidth / 2), baseCoords.Y - paddingHeight), // Bottom line at foot position - padding
                                                        Brushes.ORANGE);
                                                }
                                                if (_config.HeadDotESP == true)
                                                {
                                                    // Calculate the head dot size as 30% of the bounding box width
                                                    float headDotSize = boxWidth * 0.2f; // Set head ellipse size to 20% of the bounding box width
                                                    _device.DrawEllipse(new Ellipse(new RawVector2(headCoords.X - (headDotSize / 2), headCoords.Y - (headDotSize / 2)), headDotSize, headDotSize), Brushes.WHITE); // Head ellipse outline
                                                }
                                            }
                                            else if (dist <= _config.BoneLimit)
                                            {
                                                if (_config.BoxESP == true)
                                                {
                                                    //Console.WriteLine("Player " + player.Name);
                                                    _device.DrawRectangle(
                                                    new RawRectangleF(baseCoords.X - (boxWidth / 2), headCoords.Y + paddingHeight, // Top line at head position + padding
                                                                      baseCoords.X + (boxWidth / 2), baseCoords.Y - paddingHeight), // Bottom line at foot position - padding
                                                    Brushes.ORANGE);
                                                }
                                                // Head to Spine3
                                                _device.DrawLine(new RawVector2(headCoords.X, headCoords.Y),
                                                                 new RawVector2(spine3Coords.X, spine3Coords.Y),
                                                                 Brushes.WHITE, 2.0f);

                                                // Spine3 to Pelvis
                                                _device.DrawLine(new RawVector2(spine3Coords.X, spine3Coords.Y),
                                                                 new RawVector2(pelvisCoords.X, pelvisCoords.Y),
                                                                 Brushes.WHITE, 2.0f);

                                                // Spine3 to Left Forearm1
                                                _device.DrawLine(new RawVector2(spine3Coords.X, spine3Coords.Y),
                                                                 new RawVector2(lForearm1Coords.X, lForearm1Coords.Y),
                                                                 Brushes.WHITE, 2.0f);
                                                // Left Forearm1 to Left Palm
                                                _device.DrawLine(new RawVector2(lForearm1Coords.X, lForearm1Coords.Y),
                                                                 new RawVector2(lPalmCoords.X, lPalmCoords.Y),
                                                                 Brushes.WHITE, 2.0f);

                                                // Spine3 to Right Forearm1
                                                _device.DrawLine(new RawVector2(spine3Coords.X, spine3Coords.Y),
                                                                 new RawVector2(rForearm1Coords.X, rForearm1Coords.Y),
                                                                 Brushes.WHITE, 2.0f);

                                                // Right Forearm1 to Right Palm
                                                _device.DrawLine(new RawVector2(rForearm1Coords.X, rForearm1Coords.Y),
                                                                 new RawVector2(rPalmCoords.X, rPalmCoords.Y),
                                                                 Brushes.WHITE, 2.0f);

                                                // Pelvis to Left Calf
                                                _device.DrawLine(new RawVector2(pelvisCoords.X, pelvisCoords.Y),
                                                                 new RawVector2(lCalfCoords.X, lCalfCoords.Y),
                                                                 Brushes.WHITE, 2.0f);

                                                // Left Calf to Left Foot
                                                _device.DrawLine(new RawVector2(lCalfCoords.X, lCalfCoords.Y),
                                                                 new RawVector2(lFootCoords.X, lFootCoords.Y),
                                                                 Brushes.WHITE, 2.0f);

                                                // Pelvis to Right Calf
                                                _device.DrawLine(new RawVector2(pelvisCoords.X, pelvisCoords.Y),
                                                                 new RawVector2(rCalfCoords.X, rCalfCoords.Y),
                                                                 Brushes.WHITE, 2.0f);

                                                // Right Calf to Right Foot
                                                _device.DrawLine(new RawVector2(rCalfCoords.X, rCalfCoords.Y),
                                                                 new RawVector2(rFootCoords.X, rFootCoords.Y),
                                                                 Brushes.WHITE, 2.0f);


                                            }

                                            // Display text information
                                            WriteText("Raider" + Environment.NewLine + Math.Round(dist, 0) + "m", baseCoords.X - (paddingWidth * 15), headCoords.Y - 5, Brushes.WHITE);
                                        }
                                        #endregion
                                        #region SniperScav
                                        if (player.Type is PlayerType.SniperScav && _config.ScavESP && dist <= _config.ScavDist)
                                        {
                                            if (_config.BoneESP == false || dist > _config.BoneLimit)
                                            {
                                                if (_config.BoxESP == true)
                                                {
                                                    // Set the top and bottom coordinates for the bounding box
                                                    _device.DrawRectangle(
                                                        new RawRectangleF(baseCoords.X - (boxWidth / 2), headCoords.Y + paddingHeight, // Top line at head position + padding
                                                                          baseCoords.X + (boxWidth / 2), baseCoords.Y - paddingHeight), // Bottom line at foot position - padding
                                                        Brushes.YELLOW);
                                                }
                                                if (_config.HeadDotESP == true)
                                                {
                                                    // Calculate the head dot size as 30% of the bounding box width
                                                    float headDotSize = boxWidth * 0.2f; // Set head ellipse size to 20% of the bounding box width
                                                    _device.DrawEllipse(new Ellipse(new RawVector2(headCoords.X - (headDotSize / 2), headCoords.Y - (headDotSize / 2)), headDotSize, headDotSize), Brushes.WHITE); // Head ellipse outline
                                                }
                                            }
                                            else if (dist <= _config.BoneLimit)
                                            {
                                                if (_config.BoxESP == true)
                                                {
                                                    //Console.WriteLine("Player " + player.Name);
                                                    _device.DrawRectangle(
                                                    new RawRectangleF(baseCoords.X - (boxWidth / 2), headCoords.Y + paddingHeight, // Top line at head position + padding
                                                                      baseCoords.X + (boxWidth / 2), baseCoords.Y - paddingHeight), // Bottom line at foot position - padding
                                                    Brushes.YELLOW);
                                                }
                                                // Head to Spine3
                                                _device.DrawLine(new RawVector2(headCoords.X, headCoords.Y),
                                                                 new RawVector2(spine3Coords.X, spine3Coords.Y),
                                                                 Brushes.WHITE, 2.0f);

                                                // Spine3 to Pelvis
                                                _device.DrawLine(new RawVector2(spine3Coords.X, spine3Coords.Y),
                                                                 new RawVector2(pelvisCoords.X, pelvisCoords.Y),
                                                                 Brushes.WHITE, 2.0f);

                                                // Spine3 to Left Forearm1
                                                _device.DrawLine(new RawVector2(spine3Coords.X, spine3Coords.Y),
                                                                 new RawVector2(lForearm1Coords.X, lForearm1Coords.Y),
                                                                 Brushes.WHITE, 2.0f);
                                                // Left Forearm1 to Left Palm
                                                _device.DrawLine(new RawVector2(lForearm1Coords.X, lForearm1Coords.Y),
                                                                 new RawVector2(lPalmCoords.X, lPalmCoords.Y),
                                                                 Brushes.WHITE, 2.0f);

                                                // Spine3 to Right Forearm1
                                                _device.DrawLine(new RawVector2(spine3Coords.X, spine3Coords.Y),
                                                                 new RawVector2(rForearm1Coords.X, rForearm1Coords.Y),
                                                                 Brushes.WHITE, 2.0f);

                                                // Right Forearm1 to Right Palm
                                                _device.DrawLine(new RawVector2(rForearm1Coords.X, rForearm1Coords.Y),
                                                                 new RawVector2(rPalmCoords.X, rPalmCoords.Y),
                                                                 Brushes.WHITE, 2.0f);

                                                // Pelvis to Left Calf
                                                _device.DrawLine(new RawVector2(pelvisCoords.X, pelvisCoords.Y),
                                                                 new RawVector2(lCalfCoords.X, lCalfCoords.Y),
                                                                 Brushes.WHITE, 2.0f);

                                                // Left Calf to Left Foot
                                                _device.DrawLine(new RawVector2(lCalfCoords.X, lCalfCoords.Y),
                                                                 new RawVector2(lFootCoords.X, lFootCoords.Y),
                                                                 Brushes.WHITE, 2.0f);

                                                // Pelvis to Right Calf
                                                _device.DrawLine(new RawVector2(pelvisCoords.X, pelvisCoords.Y),
                                                                 new RawVector2(rCalfCoords.X, rCalfCoords.Y),
                                                                 Brushes.WHITE, 2.0f);

                                                // Right Calf to Right Foot
                                                _device.DrawLine(new RawVector2(rCalfCoords.X, rCalfCoords.Y),
                                                                 new RawVector2(rFootCoords.X, rFootCoords.Y),
                                                                 Brushes.WHITE, 2.0f);


                                            }
                                            // Display text information
                                            WriteText("Sniper Scav" + Environment.NewLine + Math.Round(dist, 0) + "m", baseCoords.X - (paddingWidth * 15), headCoords.Y - 5, Brushes.WHITE);
                                        }
                                        #endregion
                                    }
                                }
                        }
                        #endregion

                        #region Old Loot, Exfil and Grenade Method
                        /*#region Item ESP
                        
                        var loot = this.Loot; // cache ref
                        if (loot is not null)
                        {
                            var filter = Loot.Filter; // Get ref to collection
                            if (filter is not null) foreach (var item in filter)
                                {
                                    var lootPos = new System.Numerics.Vector3(item.Position.X, item.Position.Z, item.Position.Y);
                                    var lootdist = System.Numerics.Vector3.Distance(localPlayerPos, item.Position);

                                    float distfact = 0.5f; // Distance factor (How much the rectangle will grow or shrink)
                                    float lootheight = 500; // Height of rectangle when the player is 1m away from localplayer
                                    float lootwidth = 300; // Width of rectangle when the player is 1m away from localplayer
                                    lootheight = lootheight / lootdist * distfact; // Height of box = pheight / distance to local player * distance factor
                                    lootwidth = lootwidth / lootdist * distfact; // Width of box = pheight / distance to local player * distance factor                                        

                                    if (_config.ToggleESP == true && _config.ItemESP == true)
                                    {
                                        // Loot ESP
                                        WorldToScreenObjects(localplayer, lootPos, out var lootcoords);
                                        if (lootcoords.X > 0 || lootcoords.Y > 0 || lootcoords.Z > 0)
                                        {
                                            if (lootdist <= _config.ItemDist)
                                            {
                                                _device.DrawRectangle(new RawRectangleF(lootcoords.X - lootwidth, lootcoords.Y + (lootheight / 4), lootcoords.X + lootwidth, lootcoords.Y - lootheight), Brushes.WHITE);
                                                WriteText(item.Name + Environment.NewLine + Math.Round(lootdist, 0) + "m", lootcoords.X + 5, lootcoords.Y - 25, Brushes.WHITE);
                                            }
                                        }
                                    }
                                }
                        }
                        #endregion

                        #region Exfil ESP
                        var exfils = this.Exfils; // cache ref
                        if (exfils is not null)
                        {
                            foreach (var exfil in exfils)
                            {
                                var exfilPos = new System.Numerics.Vector3(exfil.Position.X, exfil.Position.Z, exfil.Position.Y);
                                var exfilDist = System.Numerics.Vector3.Distance(localPlayerPos, exfil.Position);


                                if (_config.ToggleESP == true)
                                {
                                    WorldToScreenObjects(localplayer, exfilPos, out var exfilcoords);
                                    if (exfilcoords.X > 0 || exfilcoords.Y > 0 || exfilcoords.Z > 0)
                                    {
                                        if (exfil.Status == ExfilStatus.Open)
                                        {
                                            WriteText(exfil.Name, exfilcoords.X + 5, exfilcoords.Y - 25, Brushes.LIGHT_GREEN);
                                        }
                                        if (exfil.Status == ExfilStatus.Pending)
                                        {
                                            WriteText(exfil.Name, exfilcoords.X + 5, exfilcoords.Y - 25, Brushes.ORANGE);
                                        }
                                    }
                                }
                            }
                        }
                        #endregion

                        #region Grenade ESP
                        var grenades = this.Grenades; // cache ref
                        if (grenades is not null)
                        {
                            foreach (var grenade in grenades)
                            {
                                var grenadePos = new System.Numerics.Vector3(grenade.Position.X, grenade.Position.Z, grenade.Position.Y);
                                var grenadeDist = System.Numerics.Vector3.Distance(localPlayerPos, grenade.Position);


                                if (_config.ToggleESP == true)
                                {
                                    WorldToScreenObjects(localplayer, grenadePos, out var grenadecoords);
                                    if (grenadecoords.X > 0 || grenadecoords.Y > 0 || grenadecoords.Z > 0)
                                    {
                                        // Define minimum and maximum dot sizes
                                        float minGrenadeDotSize = 0.15f; // Minimum size
                                        float maxGrenadeDotSize = 15.0f; // Maximum size

                                        // Determine a scaling factor based on the distance
                                        float scalingFactor = Math.Clamp(1.0f - (grenadeDist / 100.0f), 0.0f, 1.0f); // Scale between 0 and 1

                                        // Calculate the grenade dot size based on distance
                                        float grenadeDotSize = minGrenadeDotSize + (scalingFactor * (maxGrenadeDotSize - minGrenadeDotSize));

                                        // Draw the grenade dot (assuming grenadeCoords are the screen coordinates for the grenade)
                                        _device.FillEllipse(new Ellipse(new RawVector2(grenadecoords.X - (grenadeDotSize / 2), grenadecoords.Y - (grenadeDotSize / 2)), grenadeDotSize, grenadeDotSize), Brushes.RED); // Grenade ellipse
                                        WriteText("Grenade", grenadecoords.X + 5, grenadecoords.Y - 25, Brushes.WHITE);
                                    }
                                }
                            }
                        }
                        
                        #endregion*/
                        #endregion

                        #region Combined Loot, Exfil, Tripwire and Grenade ESP

                        // Initialize lists for storing positions and screen coordinates
                        List<Vector3> objectPositions = new List<Vector3>();
                        List<Vector3> screenCoords = new List<Vector3>();

                        objectPositions.Clear();

                        #region Gather Loot ESP Positions
                        // Gather Loot positions
                        var loot = this.Loot;
                        if (loot is not null)
                        {
                            var filter = Loot.Filter;
                            if (filter is not null)
                            {
                                foreach (var item in filter)
                                {
                                    objectPositions.Add(new Vector3(item.Position.X, item.Position.Z, item.Position.Y));
                                }
                            }
                        }
                        #endregion

                        #region Gather Exfil ESP Positions
                        // Gather Exfil positions
                        var exfils = this.Exfils;
                        if (exfils is not null)
                        {
                            foreach (var exfil in exfils)
                            {
                                objectPositions.Add(new Vector3(exfil.Position.X, exfil.Position.Z, exfil.Position.Y));
                            }
                        }
                        #endregion

                        #region Gather Grenade ESP Positions
                        // Gather Grenade positions
                        var grenades = this.Grenades;
                        if (grenades is not null)
                        {
                            foreach (var grenade in grenades)
                            {
                                objectPositions.Add(new Vector3(grenade.Position.X, grenade.Position.Z, grenade.Position.Y));
                            }
                        }
                        #endregion

                        #region Gather Tripwire ESP Positions
                        // Gather Tripwire positions
                        var tripwires = this.Tripwires;
                        if (tripwires is not null)
                        {
                            foreach (var tripwire in tripwires)
                            {
                                // Add both positions to the objectPositions list
                                objectPositions.Add(new Vector3(tripwire.FromPos.X, tripwire.FromPos.Z, tripwire.FromPos.Y));
                                objectPositions.Add(new Vector3(tripwire.ToPos.X, tripwire.ToPos.Z, tripwire.ToPos.Y));
                            }
                        }
                        #endregion

                        // Call WorldToScreenCombined once for all objects
                        WorldToScreenCombined(localplayer, objectPositions, screenCoords);

                        // Track index for screen coordinates
                        int screenIndex = 0;

                        #region Loot ESP
                        // THIS LOOT FILTER IS USING AN OLD BUGGY METHOD OF SCALING. IT IS NOT 100% ACCURATE
                        // Process Loot ESP
                        if (loot is not null && loot.Filter is not null)
                        {
                            foreach (var item in loot.Filter)
                            {
                                var lootDist = Vector3.Distance(localPlayerPos, item.Position);
                                var lootCoords = screenCoords[screenIndex++]; // Use the current screen coordinate

                                float distFact = 0.5f;
                                float lootHeight = 500 / lootDist * distFact;
                                float lootWidth = 300 / lootDist * distFact;

                                if (lootCoords.X > 0 || lootCoords.Y > 0 || lootCoords.Z > 0)
                                {
                                    if (lootDist <= _config.ItemDist)
                                    {
                                        _device.DrawRectangle(new RawRectangleF(lootCoords.X - lootWidth, lootCoords.Y + (lootHeight / 4), lootCoords.X + lootWidth, lootCoords.Y - lootHeight), Brushes.WHITE);
                                        WriteText(item.Name + Environment.NewLine + Math.Round(lootDist, 0) + "m", lootCoords.X + 5, lootCoords.Y - 25, Brushes.WHITE);
                                    }
                                }
                            }
                        }
                        #endregion

                        #region Exfil ESP
                        // Process Exfil ESP
                        if (exfils is not null)
                        {
                            foreach (var exfil in exfils)
                            {
                                var exfilCoords = screenCoords[screenIndex++]; // Use the current screen coordinate
                                var exfilDist = Vector3.Distance(localPlayerPos, exfil.Position);

                                if (exfilCoords.X > 0 || exfilCoords.Y > 0 || exfilCoords.Z > 0)
                                {
                                    WriteText(exfil.Name, exfilCoords.X + 5, exfilCoords.Y - 25, Brushes.TEAL);
                                }
                            }
                        }
                        #endregion

                        #region Grenade ESP
                        // Process Grenade ESP
                        if (grenades is not null)
                        {
                            foreach (var grenade in grenades)
                            {
                                var grenadeCoords = screenCoords[screenIndex++]; // Use the current screen coordinate
                                var grenadeDist = Vector3.Distance(localPlayerPos, grenade.Position);

                                if (grenadeCoords.X > 0 || grenadeCoords.Y > 0 || grenadeCoords.Z > 0)
                                {
                                    float minGrenadeDotSize = 0.15f;
                                    float maxGrenadeDotSize = 15.0f;
                                    float scalingFactor = Math.Clamp(1.0f - (grenadeDist / 100.0f), 0.0f, 1.0f);
                                    float grenadeDotSize = minGrenadeDotSize + (scalingFactor * (maxGrenadeDotSize - minGrenadeDotSize));

                                    _device.FillEllipse(new Ellipse(new RawVector2(grenadeCoords.X - (grenadeDotSize / 2), grenadeCoords.Y - (grenadeDotSize / 2)), grenadeDotSize, grenadeDotSize), Brushes.RED);
                                    WriteText("Grenade", grenadeCoords.X + 5, grenadeCoords.Y - 25, Brushes.WHITE);
                                }
                            }
                        }
                        #endregion

                        #region Tripwire ESP
                        // Process Tripwire ESP
                        if (tripwires is not null)
                        {
                            foreach (var tripwire in tripwires)
                            {
                                // Get screen coordinates for both ends of the tripwire
                                var fromCoords = screenCoords[screenIndex++]; // First position
                                var toCoords = screenCoords[screenIndex++];   // Second position

                                if (fromCoords.X > 0 && fromCoords.Y > 0 && toCoords.X > 0 && toCoords.Y > 0)
                                {
                                    // Draw a line between the two points
                                    _device.DrawLine(new RawVector2(fromCoords.X, fromCoords.Y), new RawVector2(toCoords.X, toCoords.Y), Brushes.RED);

                                    // Optionally, write text or additional details near the tripwire
                                    WriteText("Tripwire", (fromCoords.X + toCoords.X) / 2, (fromCoords.Y + toCoords.Y) / 2 - 25, Brushes.WHITE);
                                }
                            }
                        }
                        #endregion

                        #endregion

                        #region Crosshair
                        // Crosshair
                        // Draw the horizontal line
                        _device.DrawLine(
                            new RawVector2((Width / 2) - _config.CrosshairLength, (Height / 2)),
                            new RawVector2((Width / 2) + _config.CrosshairLength, (Height / 2)),
                            Brushes.WHITE, 1.0f // Set the color and thickness
                        );

                        // Draw the vertical line
                        _device.DrawLine(
                            new RawVector2((Width / 2), (Height / 2) - _config.CrosshairLength),
                            new RawVector2((Width / 2), (Height / 2) + _config.CrosshairLength),
                            Brushes.WHITE, 1.0f // Set the color and thickness
                        );

                        #endregion

                        if (_config.ShowFOV)
                        {
                            // Draw the FOV circle centered on the crosshair
                            _device.DrawEllipse(
                            new Ellipse(new RawVector2((Width / 2), (Height / 2)), AimFOV, AimFOV), // Center and radius
                            Brushes.WHITE,  // The color of the circle
                            2.0f);          // Thickness of the circle border
                        }

                        if (LocalPlayer != null && LocalPlayer.ItemInHands.Item != null)
                        {
                            WriteBottomRightText("Ammo: " + LocalPlayer.ItemInHands.Item.GearInfo.AmmoCount, Brushes.WHITE, 16, "Arial Unicode MS"); // Ammo Count
                        }

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

    private void WriteBottomRightText(string msg, SolidColorBrush color, float fontSize = 20,
        string fontFamily = "Arial Unicode MS")
    {
        // Calculate the width and height of the drawing area
        float width = this.Width; // Get the width of the control
        float height = this.Height; // Get the height of the control

        // Measure the size of the text
        using (var graphics = this.CreateGraphics())
        {
            var textSize = graphics.MeasureString(msg, new Font(fontFamily, fontSize));

            // Calculate x and y positions for bottom right placement, adjusted to 1/16th of the screen size
            var x = width - textSize.Width - (width / 16); // x position
            var y = height - textSize.Height - (height / 16); // y position

            // Write text at the calculated position
            WriteText(msg, x, y, color, fontSize, fontFamily);
        }
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

        if (FPSCamera == 0)
        {
            screenCoords.Add(new Vector3(0, 0, 0));
            return true;
        }

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

        return true;
    }


    private float Lerp(float start, float end, float amount)
    {
        return start + (end - start) * amount;
    }

    private bool WorldToScreenObjects(Player player, System.Numerics.Vector3 _Item, out System.Numerics.Vector3 _Screen)
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
        frmMain.isOverlayShown = false;
    }

    private void OverlayForm_Move(object sender, EventArgs e)
    {
        if (GUI.Instance != null && GUI.Instance.Visible)
        {
            // Update the position of the MenuForm to follow the OverlayForm
            frmMain.guiInstance.Location = new Point(Location.X + 20, Location.Y + 20); // Adjust the offsets as needed
        }
    }

    #endregion
}