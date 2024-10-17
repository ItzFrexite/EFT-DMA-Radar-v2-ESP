using ReaLTaiizor.Controls;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace eft_dma_radar
{
    public partial class GUI : Form
    {
        private bool dragging = false; // To track if the window is being dragged
        private Point dragCursorPoint; // The current cursor point when dragging starts
        private Point dragFormPoint; // The initial point of the form when dragging starts

        private frmMain _frmMain; // Reference to the main form

        private List<Control> espControls = new List<Control>();
        private List<Control> aimControls = new List<Control>();
        private List<Control> writesControls = new List<Control>();
        private List<Control> currentControls; // Active control list

        private int currentIndex = 0; // Track currently selected index
        private bool adjustingTrackBar; // Track if we are adjusting a trackbar
        private bool isAimingTab; // Track if the Aim tab is currently active
        private bool isWriteTab; // Track if the Write tab is currently active
        private int espIndex; // Track selected index for ESP tab
        private int aimIndex; // Track selected index for Aim tab
        private int writeIndex; // Track selected index for Write tab

        private bool ismMemoryWritesEnabled;
        private bool isAimEnabled;
        private bool isESPEnabled;

        public event Action<bool> ThirdPersonToggled;

        private CancellationTokenSource _cancellationTokenSource; // To control the cancellation of the thread

        private static Config _config; // Configuration object to store settings

        private static GUI guiInstance;

        #region Left and Right Arrow Key Handling for Game PC

        // For setting the foreground window
        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        public static class NativeMethods
        {
            // For getting the foreground window
            [DllImport("user32.dll")]
            public static extern IntPtr GetForegroundWindow();

            // For setting the foreground window
            [DllImport("user32.dll")]
            public static extern bool SetForegroundWindow(IntPtr hWnd);

            // For simulating key presses
            [DllImport("user32.dll")]
            public static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

            public const int KEYEVENTF_KEYUP = 0x0002;
            public const int VK_LEFT = 0x25; // Virtual key code for the left arrow key
            public const int VK_RIGHT = 0x27; // Virtual key code for the right arrow key
        }
        #endregion


        public GUI(Config config)
        {
            InitializeComponent();
            InitializeControlLists();
            HighlightCurrentControl();

            this.FormBorderStyle = FormBorderStyle.None; // No title bar
            this.StartPosition = FormStartPosition.Manual; // Manual positioning

            _config = config;

            #region Updating GUI from Config
            #region ESP
            cbESP.Checked = _config.ToggleESP;
            cbPlayers.Checked = _config.PlayerESP;
            cbTeam.Checked = _config.TeamESP;
            cbScavs.Checked = _config.ScavESP;
            cbBosses.Checked = _config.BossESP;
            cbItems.Checked = _config.ItemESP;
            cbSkeletons.Checked = _config.BoneESP;
            cbBoundingBox.Checked = _config.BoxESP;
            cbHeadDot.Checked = _config.HeadDotESP;
            tbBoneDist.Value = _config.BoneLimit;
            tbPlayerDist.Value = _config.PlayerDist;
            tbTeamDist.Value = _config.TeamDist;
            tbScavDist.Value = _config.ScavDist;
            tbItemDist.Value = _config.ItemDist;
            #endregion
            #region Aim
            cbAim.Checked = _config.EnableAimbot;
            cbPMC.Checked = _config.EnablePMC;
            cbScav.Checked = _config.EnableTargetScavs;
            cbHead.Checked = _config.AimbotHead;
            cbNeck.Checked = _config.AimbotNeck;
            cbChest.Checked = _config.AimbotChest;
            cbPelvis.Checked = _config.AimbotPelvis;
            cbRightLeg.Checked = _config.AimbotRightLeg;
            cbLeftLeg.Checked = _config.AimbotLeftLeg;
            cbClosest.Checked = _config.AimbotClosest;
            cbShowFOV.Checked = _config.ShowFOV;
            tbAimFOV.Value = _config.AimbotFOV;
            tbAimSmoothness.Value = _config.AimbotSmoothness;
            tbAimDistance.Value = _config.AimbotMaxDistance;
            #endregion
            #region Writes
            cbMemoryWrites.Checked = _config.MasterSwitch;
            cbInfiniteStamina.Checked = _config.InfiniteStamina;
            cbThirdPerson.Checked = _config.Thirdperson;
            cbInventoryBlur.Checked = _config.InventoryBlur;
            cbFreezeTime.Checked = _config.FreezeTimeOfDay;
            tbTimeOfDay.Value = (int)_config.TimeOfDay;
            cbTimeScale.Checked = _config.TimeScale;
            tbTimeFactor.Value = frmMain.sldrTimeScaleFactor.Value;
            lblTimeScaleFactorX.Text = frmMain.lblSettingsMemoryWritingTimeScaleFactor.Text;
            cbLootThroughWalls.Checked = _config.LootThroughWalls;
            tbLootThroughWallsDistance.Value = (int)_config.LootThroughWallsDistance;
            cbExtendedReach.Checked = _config.ExtendedReach;
            tbReachDistance.Value = (int)_config.ExtendedReachDistance;
            cbNoRecoil.Checked = _config.NoRecoil;
            cbNoSway.Checked = _config.NoSway;
            cbInstantADS.Checked = _config.InstantADS;
            cbThermalVision.Checked = _config.ThermalVision;
            cbOpticalThermal.Checked = _config.OpticThermalVision;
            cbNightVision.Checked = _config.NightVision;
            cbNoWeaponMalfunctions.Checked = _config.NoWeaponMalfunctions;
            cbNoVisor.Checked = _config.NoVisor;
            #endregion
            #endregion

            StartGuiInputThread(); // Start the input handling thread
        }

        public static GUI Instance
        {
            get
            {
                if (guiInstance == null || guiInstance.IsDisposed)
                {
                    guiInstance = new GUI(_config);
                }
                return guiInstance;
            }
        }

        private void InitializeControlLists()
        {
            this.MouseDown += new MouseEventHandler(GUI_MouseDown);
            this.MouseMove += new MouseEventHandler(GUI_MouseMove);
            this.MouseUp += new MouseEventHandler(GUI_MouseUp);

            #region Populating List with ESP Controls
            espControls = new List<Control>
            {
                cbESP,
                cbPlayers,
                cbTeam,
                cbScavs,
                cbBosses,
                cbItems,
                cbSkeletons,
                cbBoundingBox,
                cbHeadDot,
                cbRestart,
                tbBoneDist,
                tbPlayerDist,
                tbTeamDist,
                tbScavDist,
                tbItemDist
            };
            #endregion
            #region Populating list with Aim Controls
            aimControls = new List<Control>
            {
                cbAim,
                cbPMC,
                cbScav,
                cbHead,
                cbNeck,
                cbChest,
                cbPelvis,
                cbRightLeg,
                cbLeftLeg,
                cbClosest,
                cbShowFOV,
                tbAimFOV,
                tbAimSmoothness,
                tbAimDistance
            };
            #endregion
            #region Populating list with Write Controls
            writesControls = new List<Control>
            {
                cbMemoryWrites,
                cbInfiniteStamina,
                cbThirdPerson,
                cbInventoryBlur,
                cbFreezeTime,
                tbTimeOfDay,
                cbTimeScale,
                tbTimeFactor,
                cbLootThroughWalls,
                tbLootThroughWallsDistance,
                cbExtendedReach,
                tbReachDistance,
                cbNoRecoil,
                cbNoSway,
                cbInstantADS,
                cbThermalVision,
                cbOpticalThermal,
                cbNightVision,
                cbNoWeaponMalfunctions,
                cbNoVisor
            };
            #endregion

            currentIndex = 0; // Start at the first control
            adjustingTrackBar = false; // Initially not adjusting any trackbar
            isAimingTab = false; // Start on ESP tab
            espIndex = 0; // Start at the first control for ESP tab
            aimIndex = 0; // Start at the first control for Aim tab
            writeIndex = 0; // Start at the first control for Write tab
        }

        #region Dragging Window
        // Implement the MouseDown event handler
        private void GUI_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && e.Y < 33) // Check if the click is within the top light bar
            {
                dragging = true; // Start dragging
                dragCursorPoint = Cursor.Position; // Get the current cursor position
                dragFormPoint = this.Location; // Get the current form location
            }
        }

        // Implement the MouseMove event handler
        private void GUI_MouseMove(object sender, MouseEventArgs e)
        {
            if (dragging)
            {
                // Calculate the new location based on cursor movement
                Point dif = Point.Subtract(Cursor.Position, new Size(dragCursorPoint));
                this.Location = Point.Add(dragFormPoint, new Size(dif)); // Update the form location
            }
        }

        // Implement the MouseUp event handler
        private void GUI_MouseUp(object sender, MouseEventArgs e)
        {
            dragging = false; // Stop dragging
        }
        #endregion

        #region DMA PC Controls

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            #region GUI Controls
            /*
            // Ensure only the active tab processes inputs
            if (isWriteTab)
            {
                if (adjustingTrackBar)
                {
                    // In adjusting mode for trackbars
                    if (keyData == Keys.Left)
                    {
                        AdjustTrackBarValue(-1);
                        return true; // Indicate the key was handled
                    }
                    else if (keyData == Keys.Right)
                    {
                        AdjustTrackBarValue(1);
                        return true; // Indicate the key was handled
                    }
                    else if (keyData == Keys.Enter)
                    {
                        // Confirm adjustments and return to navigation mode
                        adjustingTrackBar = false; // Exit adjusting mode
                        return true; // Indicate the key was handled
                    }
                }

                // Handle tab navigation when not adjusting
                if (!adjustingTrackBar)
                {
                    if (keyData == Keys.Up)
                    {
                        MoveSelection(-1, writesControls); // Move selection in Writes tab
                        return true; // Indicate the key was handled
                    }
                    else if (keyData == Keys.Down)
                    {
                        MoveSelection(1, writesControls); // Move selection in Writes tab
                        return true; // Indicate the key was handled
                    }
                    else if (keyData == Keys.Enter)
                    {
                        // Check if the current control is a ForeverTrackBar
                        if (writesControls[currentIndex] is ReaLTaiizor.Controls.ForeverTrackBar)
                        {
                            adjustingTrackBar = true; // Enter adjusting mode
                            return true; // Indicate the key was handled
                        }

                        ToggleControl(currentIndex, writesControls); // If not a trackbar, toggle the control
                        return true; // Indicate the key was handled
                    }
                }
            }
            else if (isAimingTab)
            {
                if (adjustingTrackBar)
                {
                    // In adjusting mode for trackbars
                    if (keyData == Keys.Left)
                    {
                        if (aimControls[currentIndex] is ReaLTaiizor.Controls.ForeverTrackBar trackBar)
                        {
                            // Adjust based on trackbar name
                            if (trackBar.Name == "tbAimFOV" || trackBar.Name == "tbAimSmoothness")
                            {
                                AdjustTrackBarValue(-1); // Decrease by 1 for these trackbars
                            }
                            else if (trackBar.Name == "tbAimDistance")
                            {
                                AdjustTrackBarValue(-10); // Decrease by 10 for tbAimDistance
                            }
                        }
                    }
                    else if (keyData == Keys.Right)
                    {
                        if (aimControls[currentIndex] is ReaLTaiizor.Controls.ForeverTrackBar trackBar)
                        {
                            // Adjust based on trackbar name
                            if (trackBar.Name == "tbAimFOV" || trackBar.Name == "tbAimSmoothness")
                            {
                                AdjustTrackBarValue(1); // Decrease by 1 for these trackbars
                            }
                            else if (trackBar.Name == "tbAimDistance")
                            {
                                AdjustTrackBarValue(10); // Decrease by 10 for tbAimDistance
                            }
                        }
                    }
                    else if (keyData == Keys.Enter)
                    {
                        // Confirm adjustments and return to navigation mode
                        adjustingTrackBar = false; // Exit adjusting mode
                        return true; // Indicate the key was handled
                    }
                }

                // Handle tab navigation when not adjusting
                if (!adjustingTrackBar)
                {
                    if (keyData == Keys.Up)
                    {
                        MoveSelection(-1, aimControls); // Move selection in Aim tab
                        return true; // Indicate the key was handled
                    }
                    else if (keyData == Keys.Down)
                    {
                        MoveSelection(1, aimControls); // Move selection in Aim tab
                        return true; // Indicate the key was handled
                    }
                    else if (keyData == Keys.Enter)
                    {
                        // Check if the current control is a ForeverTrackBar
                        if (aimControls[currentIndex] is ReaLTaiizor.Controls.ForeverTrackBar)
                        {
                            adjustingTrackBar = true; // Enter adjusting mode
                            return true; // Indicate the key was handled
                        }

                        ToggleControl(currentIndex, aimControls); // If not a trackbar, toggle the control
                        return true; // Indicate the key was handled
                    }
                }
            }
            else
            {
                // ESP tab handling
                if (adjustingTrackBar)
                {
                    // In adjusting mode for trackbars
                    if (keyData == Keys.Left)
                    {
                        AdjustTrackBarValue(-10);
                        return true; // Indicate the key was handled
                    }
                    else if (keyData == Keys.Right)
                    {
                        AdjustTrackBarValue(10);
                        return true; // Indicate the key was handled
                    }
                    else if (keyData == Keys.Enter)
                    {
                        // Confirm adjustments and return to navigation mode
                        adjustingTrackBar = false; // Exit adjusting mode
                        return true; // Indicate the key was handled
                    }
                }

                // Handle tab navigation when not adjusting
                if (!adjustingTrackBar)
                {
                    if (keyData == Keys.Up)
                    {
                        MoveSelection(-1, espControls); // Move selection in ESP tab
                        return true; // Indicate the key was handled
                    }
                    else if (keyData == Keys.Down)
                    {
                        MoveSelection(1, espControls); // Move selection in ESP tab
                        return true; // Indicate the key was handled
                    }
                    else if (keyData == Keys.Enter)
                    {
                        // Check if the current control is a ForeverTrackBar
                        if (espControls[currentIndex] is ReaLTaiizor.Controls.ForeverTrackBar)
                        {
                            adjustingTrackBar = true; // Enter adjusting mode
                            return true; // Indicate the key was handled
                        }

                        ToggleControl(currentIndex, espControls); // If not a trackbar, toggle the control
                        return true; // Indicate the key was handled
                    }
                }
            }*/
            #endregion

            // Close GUI with Insert key
            if (keyData == Keys.Insert)
            {
                if (this.Visible)
                {
                    this.Hide();
                }
                else
                {
                    this.Show();
                    this.TopMost = true;
                }
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        #endregion

        #region Game PC Controls

        private void StartGuiInputThread()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = _cancellationTokenSource.Token;

            Task.Run(() => GuiInputWorkerThread(cancellationToken));
        }

        private void GuiInputWorkerThread(CancellationToken cancellationToken)
        {
            if (this.Visible)
            {
                if (Overlay.ActiveForm != null && Overlay.ActiveForm.Visible)
                {
                    MessageBox.Show("Overlay is active, please close it first.");
                    // Bring the GUI to the front
                    SetForegroundWindow(this.Handle);
                }
            }
            // Boolean flags to track the key states
            bool isUpPressedLast = false;
            bool isDownPressedLast = false;
            bool isLeftPressedLast = false;
            bool isRightPressedLast = false;
            bool isEnterPressedLast = false;
            bool isInsertPressedLast = false;

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // Check the current state of the keys
                    bool isUpPressed = Memory.inputHandler.IsKeyDown(0x26);    // Up Arrow
                    bool isDownPressed = Memory.inputHandler.IsKeyDown(0x28);  // Down Arrow
                    bool isLeftPressed = Memory.inputHandler.IsKeyDown(0x25);  // Left Arrow
                    bool isRightPressed = Memory.inputHandler.IsKeyDown(0x27); // Right Arrow
                    bool isEnterPressed = Memory.inputHandler.IsKeyDown(0x0D); // Enter
                    bool isInsertPressed = Memory.inputHandler.IsKeyDown(0x2D); // Insert

                    // Hide GUI with Insert key
                    if (isInsertPressed && !isInsertPressedLast)
                    {
                        if (this.Visible)
                        {
                            this.Hide();
                        }
                        else
                        {
                            this.Show();
                            this.TopMost = true;
                        }
                    }

                    #region Writes Tab InGame Keybinds
                    if (isWriteTab)
                    {
                        if (adjustingTrackBar)
                        {
                            // In adjusting mode for trackbars
                            if (isLeftPressed && !isLeftPressedLast)
                            {
                                AdjustTrackBarValue(-1);
                                isLeftPressedLast = true; // Prevent spam
                            }
                            else if (isRightPressed && !isRightPressedLast)
                            {
                                AdjustTrackBarValue(1);
                                isRightPressedLast = true; // Prevent spam
                            }
                            else if (isEnterPressed && !isEnterPressedLast)
                            {
                                // Confirm adjustments and return to navigation mode
                                adjustingTrackBar = false; // Exit adjusting mode
                                isEnterPressedLast = true; // Prevent spam
                            }
                        }

                        // Handle tab navigation when not adjusting
                        if (!adjustingTrackBar)
                        {
                            if (isUpPressed && !isUpPressedLast)
                            {
                                MoveSelection(-1, writesControls); // Move selection in Writes tab
                                isUpPressedLast = true; // Prevent spam
                            }
                            else if (isDownPressed && !isDownPressedLast)
                            {
                                MoveSelection(1, writesControls); // Move selection in Writes tab
                                isDownPressedLast = true; // Prevent spam
                            }
                            else if (isEnterPressed && !isEnterPressedLast)
                            {
                                // Check if the current control is a ForeverTrackBar
                                if (writesControls[currentIndex] is ReaLTaiizor.Controls.ForeverTrackBar)
                                {
                                    adjustingTrackBar = true; // Enter adjusting mode
                                    isEnterPressedLast = true; // Prevent spam
                                }

                                ToggleControl(currentIndex, writesControls); // If not a trackbar, toggle the control
                            }
                            // Handling left and right arrow keys to switch between tabs
                            else if (isLeftPressed && !isLeftPressedLast)
                            {
                                SwitchTabWithEmulatedKeys(-1); // Move left to the previous tab
                                isLeftPressedLast = true; // Prevent spam
                            }
                            else if (isRightPressed && !isRightPressedLast)
                            {
                                SwitchTabWithEmulatedKeys(1); // Move right to the next tab
                                isRightPressedLast = true; // Prevent spam
                            }
                        }
                    }
                    #endregion
                    #region Aim Tab InGame Keybinds
                    else if (isAimingTab)
                    {
                        if (adjustingTrackBar)
                        {
                            // Determine which controls to access based on the active tab
                            if (isLeftPressed && !isLeftPressedLast)
                            {

                                // Check which trackbar is currently focused based on its name
                                if (aimControls[currentIndex] is ReaLTaiizor.Controls.ForeverTrackBar trackBar)
                                {
                                    // Adjust based on trackbar name
                                    if (trackBar.Name == "tbAimFOV" || trackBar.Name == "tbAimSmoothness")
                                    {
                                        AdjustTrackBarValue(-1); // Decrease by 1 for these trackbars
                                    }
                                    else if (trackBar.Name == "tbAimDistance")
                                    {
                                        AdjustTrackBarValue(-10); // Decrease by 10 for tbAimDistance
                                    }
                                }

                                isLeftPressedLast = true; // Prevent spam
                            }
                            else if (isRightPressed && !isRightPressedLast)
                            {
                                if (aimControls[currentIndex] is ReaLTaiizor.Controls.ForeverTrackBar trackBar)
                                {
                                    // Adjust based on trackbar name
                                    if (trackBar.Name == "tbAimFOV" || trackBar.Name == "tbAimSmoothness")
                                    {
                                        AdjustTrackBarValue(1); // Increase by 1 for these trackbars
                                    }
                                    else if (trackBar.Name == "tbAimDistance")
                                    {
                                        AdjustTrackBarValue(10); // Increase by 10 for tbAimDistance
                                    }
                                }

                                isRightPressedLast = true; // Prevent spam
                            }
                            else if (isEnterPressed && !isEnterPressedLast)
                            {
                                adjustingTrackBar = false; // Exit adjusting mode
                                isEnterPressedLast = true; // Prevent spam
                            }
                        }

                        // Handle tab navigation when not adjusting
                        if (!adjustingTrackBar)
                        {
                            if (isUpPressed && !isUpPressedLast)
                            {
                                MoveSelection(-1, aimControls); // Move selection in Aim tab
                                isUpPressedLast = true; // Prevent spam
                            }
                            else if (isDownPressed && !isDownPressedLast)
                            {
                                MoveSelection(1, aimControls); // Move selection in Aim tab
                                isDownPressedLast = true; // Prevent spam
                            }
                            else if (isEnterPressed && !isEnterPressedLast)
                            {
                                // Check if the current control is a ForeverTrackBar
                                if (aimControls[currentIndex] is ReaLTaiizor.Controls.ForeverTrackBar)
                                {
                                    adjustingTrackBar = true; // Enter adjusting mode
                                    isEnterPressedLast = true; // Prevent spam
                                }

                                ToggleControl(currentIndex, aimControls); // If not a trackbar, toggle the control
                            }
                            // Handling left and right arrow keys to switch between tabs
                            else if (isLeftPressed && !isLeftPressedLast)
                            {
                                SwitchTabWithEmulatedKeys(-1); // Move left to the previous tab
                                isLeftPressedLast = true; // Prevent spam
                            }
                            else if (isRightPressed && !isRightPressedLast)
                            {
                                SwitchTabWithEmulatedKeys(1); // Move right to the next tab
                                isRightPressedLast = true; // Prevent spam
                            }
                        }
                    }
                    #endregion
                    #region ESP Tab InGame Keybinds
                    else
                    {
                        // ESP tab handling
                        if (adjustingTrackBar)
                        {
                            // In adjusting mode for trackbars
                            if (isLeftPressed && !isLeftPressedLast)
                            {
                                AdjustTrackBarValue(-10);
                                isLeftPressedLast = true; // Prevent spam
                            }
                            else if (isRightPressed && !isRightPressedLast)
                            {
                                AdjustTrackBarValue(10);
                                isRightPressedLast = true; // Prevent spam
                            }
                            else if (isEnterPressed && !isEnterPressedLast)
                            {
                                // Confirm adjustments and return to navigation mode
                                adjustingTrackBar = false; // Exit adjusting mode
                                isEnterPressedLast = true; // Prevent spam
                            }
                        }

                        // Handle tab navigation when not adjusting
                        if (!adjustingTrackBar)
                        {
                            if (isUpPressed && !isUpPressedLast)
                            {
                                MoveSelection(-1, espControls); // Move selection in ESP tab
                                isUpPressedLast = true; // Prevent spam
                            }
                            else if (isDownPressed && !isDownPressedLast)
                            {
                                MoveSelection(1, espControls); // Move selection in ESP tab
                                isDownPressedLast = true; // Prevent spam
                            }
                            else if (isEnterPressed && !isEnterPressedLast)
                            {
                                // Check if the current control is a ForeverTrackBar
                                if (espControls[currentIndex] is ReaLTaiizor.Controls.ForeverTrackBar)
                                {
                                    adjustingTrackBar = true; // Enter adjusting mode
                                    isEnterPressedLast = true; // Prevent spam
                                }

                                ToggleControl(currentIndex, espControls); // If not a trackbar, toggle the control
                            }
                            // Handling left and right arrow keys to switch between tabs
                            else if (isLeftPressed && !isLeftPressedLast)
                            {
                                SwitchTabWithEmulatedKeys(-1); // Move left to the previous tab
                                isLeftPressedLast = true; // Prevent spam
                            }
                            else if (isRightPressed && !isRightPressedLast)
                            {
                                SwitchTabWithEmulatedKeys(1); // Move right to the next tab
                                isRightPressedLast = true; // Prevent spam
                            }
                        }
                    }
                    #endregion

                    // Update the last pressed states
                    isUpPressedLast = isUpPressed;
                    isDownPressedLast = isDownPressed;
                    isLeftPressedLast = isLeftPressed;
                    isRightPressedLast = isRightPressed;
                    isEnterPressedLast = isEnterPressed;
                    isInsertPressedLast = isInsertPressed;

                    // Rate-limit the loop for performance
                    Thread.Sleep(10); // Adjust this delay based on performance needs
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in GUI input thread: {ex.Message}");
                }
            }
        }

        private void SwitchTabWithEmulatedKeys(int direction)
        {
            if (this != GUI.ActiveForm)
            {
                // Bring the GUI to the front
                SetForegroundWindow(this.Handle);
                Thread.Sleep(100); // Wait for the GUI to come to the front
            }
            // Get the handle of the currently active window
            IntPtr currentWindowHandle = NativeMethods.GetForegroundWindow();

            // Check if the GUI is now the focused window
            if (NativeMethods.GetForegroundWindow() == this.Handle)
            {
                // Simulate key press for left or right arrow
                if (direction == -1) // Left arrow
                {
                    // Key down
                    NativeMethods.keybd_event(NativeMethods.VK_LEFT, 0, 0, UIntPtr.Zero);
                    // Key up
                    NativeMethods.keybd_event(NativeMethods.VK_LEFT, 0, NativeMethods.KEYEVENTF_KEYUP, UIntPtr.Zero);
                }
                else if (direction == 1) // Right arrow
                {
                    // Key down
                    NativeMethods.keybd_event(NativeMethods.VK_RIGHT, 0, 0, UIntPtr.Zero);
                    // Key up
                    NativeMethods.keybd_event(NativeMethods.VK_RIGHT, 0, NativeMethods.KEYEVENTF_KEYUP, UIntPtr.Zero);
                }
            }

            // Restore focus to the original window
            NativeMethods.SetForegroundWindow(currentWindowHandle);
        }

        #endregion

        #region Trackbar Adjustments

        public void AdjustTrackBarValue(int change)
        {
            // Determine which controls to access based on the active tab
            Control currentControl = isWriteTab ? writesControls[currentIndex] :
                                    isAimingTab ? aimControls[currentIndex] :
                                    espControls[currentIndex]; // Default to ESP tab if none matched

            if (currentControl is ReaLTaiizor.Controls.ForeverTrackBar trackBar)
            {
                int newValue = Math.Clamp(trackBar.Value + change, trackBar.Minimum, trackBar.Maximum);
                trackBar.Value = newValue; // Set the new value directly

                // Update the radar configuration based on the specific trackbar
                #region Memory Writes
                if (trackBar == tbTimeOfDay)
                {
                    _config.TimeOfDay = (float)newValue; // Update radar configuration
                }
                else if (trackBar == tbTimeFactor)
                {
                    _config.TimeScaleFactor = (float)newValue / 10; // Update radar configuration
                    if (_config.TimeScaleFactor < 1)
                    {
                        _config.TimeScaleFactor = 1;
                    }
                    lblTimeScaleFactorX.Text = $"x{(_config.TimeScaleFactor)}"; // Update the label text
                }
                else if (trackBar == tbLootThroughWallsDistance)
                {
                    var pveMode = _config.PvEMode;
                    var distance = (float)newValue;

                    if (pveMode)
                        _config.LootThroughWallsDistancePvE = distance;
                    else
                    {
                        if (distance > 3)
                            distance = 3;

                        _config.LootThroughWallsDistance = distance;
                    }

                    lblLootThroughWallsX.Text = $"x{(distance)}"; // Update the label text
                }
                else if (trackBar == tbReachDistance)
                {
                    var pveMode = _config.PvEMode;
                    var distance = (float)newValue / 10;

                    if (pveMode)
                        _config.ExtendedReachDistancePvE = distance;
                    else
                    {
                        if (distance > 4f)
                            distance = 4f;

                        _config.ExtendedReachDistance = distance;
                    }
                    lblReachX.Text = $"x{(distance)}"; // Update the label text
                }
                #endregion

                #region Aim

                else if (trackBar == tbAimFOV)
                {
                    _config.AimbotFOV = newValue; // Update radar configuration
                }
                else if (trackBar == tbAimSmoothness)
                {
                    _config.AimbotSmoothness = newValue; // Update radar configuration
                }
                else if (trackBar == tbAimDistance)
                {
                    _config.AimbotMaxDistance = newValue; // Update radar configuration
                }

                #endregion

                #region ESP
                else if (trackBar == tbPlayerDist)
                {
                    _config.PlayerDist = newValue; // Update radar configuration
                }
                else if (trackBar == tbTeamDist)
                {
                    _config.TeamDist = newValue; // Update radar configuration
                }
                else if (trackBar == tbScavDist)
                {
                    _config.ScavDist = newValue; // Update radar configuration
                }
                else if (trackBar == tbItemDist)
                {
                    _config.ItemDist = newValue; // Update radar configuration
                }
                else if (trackBar == tbBoneDist)
                {
                    _config.BossDist = newValue; // Update radar configuration
                }
                #endregion

                // Optionally, synchronize the GUI if needed
                SynchronizeWithRadarConfig(trackBar, newValue);
            }
        }
        // Optional synchronization method if you want to reflect changes in GUI
        private void SynchronizeWithRadarConfig(ReaLTaiizor.Controls.ForeverTrackBar trackBar, int newValue)
        {
            // Update the corresponding trackbar in the GUI

            #region Memory Writes
            if (trackBar == tbTimeOfDay)
            {
                tbTimeOfDay.Value = newValue; // Ensure GUI reflects the new value
                frmMain.sldrTimeOfDay.Value = newValue; // Synchronize the GUI with the Main Form
            }
            else if (trackBar == tbTimeFactor)
            {
                tbTimeFactor.Value = newValue; // Ensure GUI reflects the new value
                frmMain.sldrTimeScaleFactor.Value = newValue; // Synchronize the GUI with the Main Form
                frmMain.lblSettingsMemoryWritingTimeScaleFactor.Text = $"x{(_config.TimeScaleFactor)}";
            }
            else if (trackBar == tbLootThroughWallsDistance)
            {
                tbLootThroughWallsDistance.Value = newValue; // Ensure GUI reflects the new value
                frmMain.sldrLootThroughWallsDistance.Value = (newValue * 10); // Synchronize the GUI with the Main Form
                frmMain.lblSettingsMemoryWritingLootThroughWallsDistance.Text = $"x{(_config.LootThroughWallsDistance)}";
            }
            else if (trackBar == tbReachDistance)
            {
                tbReachDistance.Value = newValue; // Ensure GUI reflects the new value
                frmMain.sldrExtendedReachDistance.Value = newValue; // Synchronize the GUI with the Main Form
                frmMain.lblSettingsMemoryWritingExtendedReachDistance.Text = $"x{(_config.ExtendedReachDistance)}";
            }
            #endregion

            #region Aim
            else if (trackBar == tbAimFOV)
            {
                tbAimFOV.Value = newValue; // Ensure GUI reflects the new value
                frmMain.sldrAimbotFOV.Value = newValue; // Synchronize the GUI with the Main Form
            }
            else if (trackBar == tbAimSmoothness)
            {
                tbAimSmoothness.Value = newValue; // Ensure GUI reflects the new value
                frmMain.sldrAimbotSmoothness.Value = newValue; // Synchronize the GUI with the Main Form
            }
            else if (trackBar == tbAimDistance)
            {
                tbAimDistance.Value = newValue; // Ensure GUI reflects the new value
                frmMain.sldrAimDistance.Value = newValue; // Synchronize the GUI with the Main Form
            }
            #endregion

            #region ESP
            else if (trackBar == tbPlayerDist)
            {
                tbPlayerDist.Value = newValue; // Ensure GUI reflects the new value
                frmMain.sldrPlayerDist.Value = newValue; // Synchronize the GUI with the Main Form
            }
            else if (trackBar == tbTeamDist)
            {
                tbTeamDist.Value = newValue; // Ensure GUI reflects the new value
                frmMain.sldrTeamDist.Value = newValue; // Synchronize the GUI with the Main Form
            }
            else if (trackBar == tbScavDist)
            {
                tbScavDist.Value = newValue; // Ensure GUI reflects the new value
                frmMain.sldrScavDist.Value = newValue; // Synchronize the GUI with the Main Form
            }
            else if (trackBar == tbItemDist)
            {
                tbItemDist.Value = newValue; // Ensure GUI reflects the new value
                frmMain.sldrItemDist.Value = newValue; // Synchronize the GUI with the Main Form
            }
            else if (trackBar == tbBoneDist)
            {
                tbBoneDist.Value = newValue; // Ensure GUI reflects the new value
                frmMain.sldrBoneDist.Value = newValue; // Synchronize the GUI with the Main Form
            }
            #endregion
        }


        private int RoundToNearestTen(int value)
        {
            return (int)(Math.Round(value / 10.0) * 10);
        }

        private void MoveSelection(int direction, List<Control> controls)
        {
            // Clear previous highlight
            controls[currentIndex].BackColor = Color.FromArgb(60, 70, 73);

            // Update index
            currentIndex += direction;

            // Wrap around the list
            if (currentIndex < 0) currentIndex = controls.Count - 1;
            else if (currentIndex >= controls.Count) currentIndex = 0;

            // Highlight new selection
            HighlightCurrentControl();
        }

        #endregion

        #region Highlighting Current Selection
        private void HighlightCurrentControl()
        {
            if (isWriteTab)
            {
                writesControls[currentIndex].BackColor = Color.Red; // Change color to indicate selection
            }
            else if (isAimingTab)
            {
                aimControls[currentIndex].BackColor = Color.Red; // Change color to indicate selection
            }
            else
            {
                espControls[currentIndex].BackColor = Color.Red; // Change color to indicate selection
            }
        }
        #endregion

        #region Toggling Controls
        private void ToggleControl(int index, List<Control> controls)
        {
            Control currentControl = controls[index];
            if (currentControl is ReaLTaiizor.Controls.ForeverCheckBox checkBox)
            {
                // Toggle checkbox
                checkBox.Checked = !checkBox.Checked;

                // Manually invoke the CheckedChanged event
                CheckBox_CheckedChanged(checkBox, EventArgs.Empty); // Pass EventArgs.Empty as we are not using specific event data
            }
        }

        // CheckBox CheckedChanged event handler
        private void CheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (sender is ReaLTaiizor.Controls.ForeverCheckBox checkBox)
            {
                string controlName = checkBox.Name; // Get the name of the checkbox

                // Perform actions based on the checkbox name
                switch (controlName)
                {
                    #region ESP
                    case "cbESP":
                        //Console.WriteLine("ESP toggled!");
                        ToggleESP();
                        break;
                    case "cbPlayers":
                        //Console.WriteLine("Players toggled!");
                        TogglePlayersESP();
                        break;
                    case "cbTeam":
                        //Console.WriteLine("Team toggled!");
                        ToggleTeamESP();
                        break;
                    case "cbScavs":
                        //Console.WriteLine("Scavs toggled!");
                        ToggleScavESP();
                        break;
                    case "cbBosses":
                        //Console.WriteLine("Bosses toggled!");
                        ToggleBossESP();
                        break;
                    case "cbItems":
                        //Console.WriteLine("Items toggled!");
                        ToggleItemESP();
                        break;
                    case "cbSkeletons":
                        //Console.WriteLine("Skeletons toggled!");
                        ToggleSkeletonsESP();
                        break;
                    case "cbBoundingBox":
                        //Console.WriteLine("Bounding Box toggled!");
                        ToggleBoxESP();
                        break;
                    case "cbHeadDot":
                        //Console.WriteLine("Head Dot toggled!");
                        ToggleHeadDotESP();
                        break;
                    case "cbRestart":
                        RestartRadar();
                        break;

                    #endregion
                    #region Aim
                    case "cbAim":
                        //Console.WriteLine("Aim toggled!");
                        ToggleAim();
                        break;
                    case "cbPMC":
                        //Console.WriteLine("PMC toggled!");
                        TogglePMC();
                        break;
                    case "cbScav":
                        //Console.WriteLine("Scav toggled!");
                        ToggleScav();
                        break;
                    case "cbHead":
                        //Console.WriteLine("Head toggled!");
                        ToggleHead();
                        break;
                    case "cbNeck":
                        //Console.WriteLine("Neck toggled!");
                        ToggleNeck();
                        break;
                    case "cbChest":
                        //Console.WriteLine("Chest toggled!");
                        ToggleChest();
                        break;
                    case "cbPelvis":
                        //Console.WriteLine("Pelvis toggled!");
                        TogglePelvis();
                        break;
                    case "cbRightLeg":
                        //Console.WriteLine("Right Leg toggled!");
                        ToggleRightLeg();
                        break;
                    case "cbLeftLeg":
                        //Console.WriteLine("Left Leg toggled!");
                        ToggleLeftLeg();
                        break;
                    case "cbClosest":
                        //Console.WriteLine("Closest toggled!");
                        ToggleClosest();
                        break;
                    case "cbShowFOV":
                        //Console.WriteLine("Aim FOV toggled!");
                        ToggleShowFOV();
                        break;
                    #endregion
                    #region Writes
                    case "cbMemoryWrites":
                        //Console.WriteLine("Memory Writes toggled!");
                        ToggleMemoryWrites();
                        break;
                    case "cbInfiniteStamina":
                        //Console.WriteLine("Infinite Stamina toggled!");
                        ToggleInfiniteStamina();
                        break;
                    case "cbThirdPerson":
                        //Console.WriteLine("Third Person toggled!");
                        ToggleThirdPerson();
                        break;
                    case "cbInventoryBlur":
                        //Console.WriteLine("Inventory Blur toggled!");
                        ToggleInventoryBlur();
                        break;
                    case "cbFreezeTime":
                        //Console.WriteLine("Freeze Time toggled!");
                        ToggleFreezeTime();
                        break;
                    case "cbExtendedReach":
                        //Console.WriteLine("Extended Reach toggled!");
                        ToggleExtendedReach();
                        break;
                    case "cbTimeScale":
                        //Console.WriteLine("Time Scale toggled!");
                        ToggleTimeScale();
                        break;
                    case "cbNoWeaponMalfunctions":
                        //Console.WriteLine("No Weapon Malfunctions toggled!");
                        ToggleNoWeaponMalfunctions();
                        break;
                    case "cbNightVision":
                        //Console.WriteLine("Night Vision toggled!");
                        ToggleNightVision();
                        break;
                    case "cbOpticalThermal":
                        //Console.WriteLine("Optical Thermal toggled!");
                        ToggleOpticalThermal();
                        break;
                    case "cbThermalVision":
                        //Console.WriteLine("Thermal Vision toggled!");
                        ToggleThermalVision();
                        break;
                    case "cbInstantADS":
                        //Console.WriteLine("Instant ADS toggled!");
                        ToggleInstantADS();
                        break;
                    case "cbNoSway":
                        //Console.WriteLine("No Sway toggled!");
                        ToggleNoSway();
                        break;
                    case "cbNoRecoil":
                        //Console.WriteLine("No Recoil toggled!");
                        ToggleNoRecoil();
                        break;
                    case "cbLootThroughWalls":
                        //Console.WriteLine("Loot Through Walls toggled!");
                        ToggleLootThroughWalls();
                        break;
                    case "cbNoVisor":
                        //Console.WriteLine("No Visor toggled!");
                        ToggleNoVisor();
                        break;
                    #endregion
                    default:
                        break;
                }
            }
        }
        #endregion

        private void tabPageGUI_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Store the currently selected index for the active tab before switching
            if (isWriteTab)
            {
                writeIndex = currentIndex; // Save the index for Writes tab
            }
            else if (isAimingTab)
            {
                aimIndex = currentIndex; // Save the index for Aim tab
            }
            else
            {
                espIndex = currentIndex; // Save the index for ESP tab
            }

            // Switch tabs and update the current index
            isAimingTab = tabPageGUI.SelectedTab == tabAim;
            isWriteTab = tabPageGUI.SelectedTab == tabWrites; // Check if the Writes tab is selected

            // Restore the selected index for the newly activated tab
            if (isAimingTab)
            {
                currentIndex = aimIndex; // Restore Aim tab index
            }
            else if (isWriteTab)
            {
                currentIndex = writeIndex; // Restore Writes tab index
            }
            else
            {
                currentIndex = espIndex; // Restore ESP tab index
            }

            // Ensure the index is within bounds for the selected tab
            if (isAimingTab)
            {
                currentIndex = Math.Clamp(currentIndex, 0, aimControls.Count - 1);
            }
            else if (isWriteTab)
            {
                currentIndex = Math.Clamp(currentIndex, 0, writesControls.Count - 1); // Ensure the index for Writes tab
            }
            else
            {
                currentIndex = Math.Clamp(currentIndex, 0, espControls.Count - 1);
            }

            // Highlight the current control
            HighlightCurrentControl();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Cancel(); // Cancel the input thread
                _cancellationTokenSource.Dispose();
            }
        }

        #region Methods for Functions
        #region Memory Writes
        private void ToggleMemoryWrites()
        {
            this.BeginInvoke((MethodInvoker)delegate
            {
                // Get the current state of the GUI checkbox
                ismMemoryWritesEnabled = frmMain.swMasterSwitch.Checked;

                // Update the radar configuration
                _config.MasterSwitch = !ismMemoryWritesEnabled;

                // Synchronize the GUI checkbox with the radar setting
                frmMain.swMasterSwitch.Checked = !ismMemoryWritesEnabled; // Toggle the switch in the GUI
            });
        }
        private void ToggleInfiniteStamina()
        {
            this.BeginInvoke((MethodInvoker)delegate
            {
                // Get the current state of the GUI checkbox
                bool isInfiniteStaminaEnabled = frmMain.swInfiniteStamina.Checked;

                // Update the radar configuration
                _config.InfiniteStamina = !isInfiniteStaminaEnabled;

                // Synchronize the GUI checkbox with the radar setting
                frmMain.swInfiniteStamina.Checked = !isInfiniteStaminaEnabled; // Toggle the switch in the GUI
            });
        }
        private void ToggleThirdPerson()
        {
            this.BeginInvoke((MethodInvoker)delegate
            {
                // Get the current state of the GUI checkbox
                bool isThirdPersonEnabled = frmMain.swThirdperson.Checked;

                // Update the radar configuration
                _config.Thirdperson = !isThirdPersonEnabled;

                // Synchronize the GUI checkbox with the radar setting
                frmMain.swThirdperson.Checked = !isThirdPersonEnabled; // Toggle the switch in the GUI
            });
        }
        private void ToggleInventoryBlur()
        {
            this.BeginInvoke((MethodInvoker)delegate
            {
                // Get the current state of the GUI checkbox
                bool IsInventoryBlurEnabled = frmMain.swInventoryBlur.Checked;

                // Update the radar configuration
                _config.InventoryBlur = !IsInventoryBlurEnabled;

                // Synchronize the GUI checkbox with the radar setting
                frmMain.swInventoryBlur.Checked = !IsInventoryBlurEnabled; // Toggle the switch in the GUI
            });
        }
        private void ToggleFreezeTime()
        {
            this.BeginInvoke((MethodInvoker)delegate
            {
                // Get the current state of the GUI checkbox
                bool isFreezeTimeEnabled = frmMain.swFreezeTime.Checked;

                // Update the radar configuration
                _config.FreezeTimeOfDay = !isFreezeTimeEnabled;

                // Synchronize the GUI checkbox with the radar setting
                frmMain.swFreezeTime.Checked = !isFreezeTimeEnabled; // Toggle the switch in the GUI
            });
        }
        private void ToggleExtendedReach()
        {
            this.BeginInvoke((MethodInvoker)delegate
            {
                // Get the current state of the GUI checkbox
                bool isExtendedReachEnabled = frmMain.swExtendedReach.Checked;

                // Update the radar configuration
                _config.ExtendedReach = !isExtendedReachEnabled;

                // Synchronize the GUI checkbox with the radar setting
                frmMain.swExtendedReach.Checked = !isExtendedReachEnabled; // Toggle the switch in the GUI
            });
        }
        private void ToggleTimeScale()
        {
            this.BeginInvoke((MethodInvoker)delegate
            {
                // Get the current state of the GUI checkbox
                bool isTimeScaleEnabled = frmMain.swTimeScale.Checked;

                // Update the radar configuration
                _config.TimeScale = !isTimeScaleEnabled;

                // Synchronize the GUI checkbox with the radar setting
                frmMain.swTimeScale.Checked = !isTimeScaleEnabled; // Toggle the switch in the GUI
            });
        }
        private void ToggleNoWeaponMalfunctions()
        {
            this.BeginInvoke((MethodInvoker)delegate
            {
                // Get the current state of the GUI checkbox
                bool isNoWeaponMalfunctionsEnabled = frmMain.swNoWeaponMalfunctions.Checked;

                // Update the radar configuration
                _config.NoWeaponMalfunctions = !isNoWeaponMalfunctionsEnabled;

                // Synchronize the GUI checkbox with the radar setting
                frmMain.swNoWeaponMalfunctions.Checked = !isNoWeaponMalfunctionsEnabled; // Toggle the switch in the GUI
            });
        }
        private void ToggleNightVision()
        {
            this.BeginInvoke((MethodInvoker)delegate
            {
                // Get the current state of the GUI checkbox
                bool isNightVisionEnabled = frmMain.swNightVision.Checked;

                // Update the radar configuration
                _config.NightVision = !isNightVisionEnabled;

                // Synchronize the GUI checkbox with the radar setting
                frmMain.swNightVision.Checked = !isNightVisionEnabled; // Toggle the switch in the GUI
            });
        }
        private void ToggleOpticalThermal()
        {
            this.BeginInvoke((MethodInvoker)delegate
            {
                // Get the current state of the GUI checkbox
                bool isOpticalThermalEnabled = frmMain.swOpticalThermal.Checked;

                // Update the radar configuration
                _config.OpticThermalVision = !isOpticalThermalEnabled;

                // Synchronize the GUI checkbox with the radar setting
                frmMain.swOpticalThermal.Checked = !isOpticalThermalEnabled; // Toggle the switch in the GUI
            });
        }
        private void ToggleThermalVision()
        {
            this.BeginInvoke((MethodInvoker)delegate
            {
                // Get the current state of the GUI checkbox
                bool isThermalVisionEnabled = frmMain.swThermalVision.Checked;

                // Update the radar configuration
                _config.ThermalVision = !isThermalVisionEnabled;

                // Synchronize the GUI checkbox with the radar setting
                frmMain.swThermalVision.Checked = !isThermalVisionEnabled; // Toggle the switch in the GUI
            });
        }
        private void ToggleInstantADS()
        {
            this.BeginInvoke((MethodInvoker)delegate
            {
                // Get the current state of the GUI checkbox
                bool isInstantADSEnabled = frmMain.swInstantADS.Checked;

                // Update the radar configuration
                _config.InstantADS = !isInstantADSEnabled;

                // Synchronize the GUI checkbox with the radar setting
                frmMain.swInstantADS.Checked = !isInstantADSEnabled; // Toggle the switch in the GUI
            });
        }
        private void ToggleNoSway()
        {
            this.BeginInvoke((MethodInvoker)delegate
            {
                // Get the current state of the GUI checkbox
                bool isNoSwayEnabled = frmMain.swNoSway.Checked;

                // Update the radar configuration
                _config.NoSway = !isNoSwayEnabled;

                // Synchronize the GUI checkbox with the radar setting
                frmMain.swNoSway.Checked = !isNoSwayEnabled; // Toggle the switch in the GUI
            });
        }
        private void ToggleNoRecoil()
        {
            this.BeginInvoke((MethodInvoker)delegate
            {
                // Get the current state of the GUI checkbox
                bool isNoRecoilEnabled = frmMain.swNoRecoil.Checked;

                // Update the radar configuration
                _config.NoRecoil = !isNoRecoilEnabled;

                // Synchronize the GUI checkbox with the radar setting
                frmMain.swNoRecoil.Checked = !isNoRecoilEnabled; // Toggle the switch in the GUI
            });
        }
        private void ToggleLootThroughWalls()
        {
            this.BeginInvoke((MethodInvoker)delegate
            {
                // Get the current state of the GUI checkbox
                bool isLootThroughWallsEnabled = frmMain.swLootThroughWalls.Checked;

                // Update the radar configuration
                _config.LootThroughWalls = !isLootThroughWallsEnabled;

                // Synchronize the GUI checkbox with the radar setting
                frmMain.swLootThroughWalls.Checked = !isLootThroughWallsEnabled; // Toggle the switch in the GUI
            });
        }
        private void ToggleNoVisor()
        {
            this.BeginInvoke((MethodInvoker)delegate
            {
                // Get the current state of the GUI checkbox
                bool isNoVisorEnabled = frmMain.swNoVisor.Checked;

                // Update the radar configuration
                _config.NoVisor = !isNoVisorEnabled;

                // Synchronize the GUI checkbox with the radar setting
                frmMain.swNoVisor.Checked = !isNoVisorEnabled; // Toggle the switch in the GUI
            });
        }

        #endregion
        #region Aim
        private void ToggleAim()
        {
            this.BeginInvoke((MethodInvoker)delegate
            {
                // Get the current state of the GUI checkbox
                isAimEnabled = frmMain.swEnableAimBot.Checked;

                // Update the radar configuration
                _config.EnableAimbot = !isAimEnabled;

                // Synchronize the GUI checkbox with the radar setting
                frmMain.swEnableAimBot.Checked = !isAimEnabled; // Toggle the switch in the GUI
            });
        }
        private void TogglePMC()
        {
            this.BeginInvoke((MethodInvoker)delegate
            {
                // Get the current state of the GUI checkbox
                bool isPMCEnabled = frmMain.swEnablePMC.Checked;

                // Update the radar configuration
                _config.EnablePMC = !isPMCEnabled;

                // Synchronize the GUI checkbox with the radar setting
                frmMain.swEnablePMC.Checked = !isPMCEnabled; // Toggle the switch in the GUI
            });
        }
        private void ToggleScav()
        {
            this.BeginInvoke((MethodInvoker)delegate
            {
                // Get the current state of the GUI checkbox
                bool isScavEnabled = frmMain.swEnableTargetScavs.Checked;

                // Update the radar configuration
                _config.EnableTargetScavs = !isScavEnabled;

                // Synchronize the GUI checkbox with the radar setting
                frmMain.swEnableTargetScavs.Checked = !isScavEnabled; // Toggle the switch in the GUI
            });
        }
        private void ToggleHead()
        {
            this.BeginInvoke((MethodInvoker)delegate
            {
                // Get the current state of the GUI checkbox
                bool isHeadEnabled = frmMain.swHeadAim.Checked;

                // Update the radar configuration
                _config.AimbotHead = !isHeadEnabled;

                // Synchronize the GUI checkbox with the radar setting
                frmMain.swHeadAim.Checked = !isHeadEnabled; // Toggle the switch in the GUI
            });
        }
        private void ToggleNeck()
        {
            this.BeginInvoke((MethodInvoker)delegate
            {
                // Get the current state of the GUI checkbox
                bool isNeckEnabled = frmMain.swAimNeck.Checked;

                // Update the radar configuration
                _config.AimbotNeck = !isNeckEnabled;

                // Synchronize the GUI checkbox with the radar setting
                frmMain.swAimNeck.Checked = !isNeckEnabled; // Toggle the switch in the GUI
            });
        }
        private void ToggleChest()
        {
            this.BeginInvoke((MethodInvoker)delegate
            {
                // Get the current state of the GUI checkbox
                bool isChestEnabled = frmMain.swAimChest.Checked;

                // Update the radar configuration
                _config.AimbotChest = !isChestEnabled;

                // Synchronize the GUI checkbox with the radar setting
                frmMain.swAimChest.Checked = !isChestEnabled; // Toggle the switch in the GUI
            });
        }
        private void TogglePelvis()
        {
            this.BeginInvoke((MethodInvoker)delegate
            {
                // Get the current state of the GUI checkbox
                bool isPelvisEnabled = frmMain.swAimPelvis.Checked;

                // Update the radar configuration
                _config.AimbotPelvis = !isPelvisEnabled;

                // Synchronize the GUI checkbox with the radar setting
                frmMain.swAimPelvis.Checked = !isPelvisEnabled; // Toggle the switch in the GUI
            });
        }
        private void ToggleRightLeg()
        {
            this.BeginInvoke((MethodInvoker)delegate
            {
                // Get the current state of the GUI checkbox
                bool isRightLegEnabled = frmMain.swAimRLeg.Checked;

                // Update the radar configuration
                _config.AimbotRightLeg = !isRightLegEnabled;

                // Synchronize the GUI checkbox with the radar setting
                frmMain.swAimRLeg.Checked = !isRightLegEnabled; // Toggle the switch in the GUI
            });
        }
        private void ToggleLeftLeg()
        {
            this.BeginInvoke((MethodInvoker)delegate
            {
                // Get the current state of the GUI checkbox
                bool isLeftLegEnabled = frmMain.swAimLLeg.Checked;

                // Update the radar configuration
                _config.AimbotLeftLeg = !isLeftLegEnabled;

                // Synchronize the GUI checkbox with the radar setting
                frmMain.swAimLLeg.Checked = !isLeftLegEnabled; // Toggle the switch in the GUI
            });
        }
        private void ToggleClosest()
        {
            this.BeginInvoke((MethodInvoker)delegate
            {
                // Get the current state of the GUI checkbox
                bool isClosestEnabled = frmMain.swAimClosest.Checked;

                // Update the radar configuration
                _config.AimbotClosest = !isClosestEnabled;

                // Synchronize the GUI checkbox with the radar setting
                frmMain.swAimClosest.Checked = !isClosestEnabled; // Toggle the switch in the GUI
            });
        }
        private void ToggleShowFOV()
        {
            this.BeginInvoke((MethodInvoker)delegate
            {
                // Get the current state of the GUI checkbox
                bool isFOVEnabled = frmMain.swShowFOV.Checked;

                // Update the radar configuration
                _config.ShowFOV = !isFOVEnabled;

                // Synchronize the GUI checkbox with the radar setting
                frmMain.swShowFOV.Checked = !isFOVEnabled; // Toggle the switch in the GUI
            });
        }
        #endregion
        #region ESP
        private void ToggleESP()
        {
            this.BeginInvoke((MethodInvoker)delegate
            {
                // Get the current state of the GUI checkbox
                isESPEnabled = frmMain.swToggleESP.Checked;

                // Update the radar configuration
                _config.ToggleESP = !isESPEnabled;

                // Synchronize the GUI checkbox with the radar setting
                frmMain.swToggleESP.Checked = !isESPEnabled; // Toggle the switch in the GUI
            });
        }
        private void TogglePlayersESP()
        {
            this.BeginInvoke((MethodInvoker)delegate
            {
                // Get the current state of the GUI checkbox
                bool isPlayersEnabled = frmMain.swTogglePlayers.Checked;

                // Update the radar configuration
                _config.PlayerESP = !isPlayersEnabled;

                // Synchronize the GUI checkbox with the radar setting
                frmMain.swTogglePlayers.Checked = !isPlayersEnabled; // Toggle the switch in the GUI
            });
        }
        private void ToggleTeamESP()
        {
            this.BeginInvoke((MethodInvoker)delegate
            {
                // Get the current state of the GUI checkbox
                bool isTeamEnabled = frmMain.swToggleTeam.Checked;

                // Update the radar configuration
                _config.TeamESP = !isTeamEnabled;

                // Synchronize the GUI checkbox with the radar setting
                frmMain.swToggleTeam.Checked = !isTeamEnabled; // Toggle the switch in the GUI
            });
        }
        private void ToggleScavESP()
        {
            this.BeginInvoke((MethodInvoker)delegate
            {
                // Get the current state of the GUI checkbox
                bool isScavEnabled = frmMain.swToggleScavs.Checked;

                // Update the radar configuration
                _config.ScavESP = !isScavEnabled;

                // Synchronize the GUI checkbox with the radar setting
                frmMain.swToggleScavs.Checked = !isScavEnabled; // Toggle the switch in the GUI
            });
        }
        private void ToggleBossESP()
        {
            this.BeginInvoke((MethodInvoker)delegate
            {
                // Get the current state of the GUI checkbox
                bool isBossEnabled = frmMain.swToggleBosses.Checked;

                // Update the radar configuration
                _config.BossESP = !isBossEnabled;

                // Synchronize the GUI checkbox with the radar setting
                frmMain.swToggleBosses.Checked = !isBossEnabled; // Toggle the switch in the GUI
            });
        }
        private void ToggleItemESP()
        {
            this.BeginInvoke((MethodInvoker)delegate
            {
                // Get the current state of the GUI checkbox
                bool isItemEnabled = frmMain.swToggleItems.Checked;

                // Update the radar configuration
                _config.ItemESP = !isItemEnabled;

                // Synchronize the GUI checkbox with the radar setting
                frmMain.swToggleItems.Checked = !isItemEnabled; // Toggle the switch in the GUI
            });
        }
        private void ToggleSkeletonsESP()
        {
            this.BeginInvoke((MethodInvoker)delegate
            {
                // Get the current state of the GUI checkbox
                bool isBonesEnabled = frmMain.swToggleBones.Checked;

                // Update the radar configuration
                _config.BoneESP = !isBonesEnabled;

                // Synchronize the GUI checkbox with the radar setting
                frmMain.swToggleBones.Checked = !isBonesEnabled; // Toggle the switch in the GUI
            });
        }
        private void ToggleBoxESP()
        {
            this.BeginInvoke((MethodInvoker)delegate
            {
                // Get the current state of the GUI checkbox
                bool isBoxEnabled = frmMain.swBox.Checked;

                // Update the radar configuration
                _config.BoxESP = !isBoxEnabled;

                // Synchronize the GUI checkbox with the radar setting
                frmMain.swBox.Checked = !isBoxEnabled; // Toggle the switch in the GUI
            });
        }
        private void ToggleHeadDotESP()
        {
            this.BeginInvoke((MethodInvoker)delegate
            {
                // Get the current state of the GUI checkbox
                bool isHeadDotEnabled = frmMain.swHeadDot.Checked;

                // Update the radar configuration
                _config.HeadDotESP = !isHeadDotEnabled;

                // Synchronize the GUI checkbox with the radar setting
                frmMain.swHeadDot.Checked = !isHeadDotEnabled; // Toggle the switch in the GUI
            });
        }
        private void RestartRadar()
        {
            Memory.Restart();
            cbRestart.Checked = false;
        }
        #endregion
        #endregion

        protected override void OnLocationChanged(EventArgs e)
        {
            base.OnLocationChanged(e);

            // Ensure the GUI stays within the bounds of the overlay
            if (Owner != null)
            {
                var overlayBounds = Owner.ClientRectangle;

                // Clamp the GUI position within the overlay bounds
                if (this.Left < overlayBounds.Left)
                {
                    this.Left = overlayBounds.Left;
                }
                else if (this.Right > overlayBounds.Right)
                {
                    this.Left = overlayBounds.Right - this.Width;
                }

                if (this.Top < overlayBounds.Top)
                {
                    this.Top = overlayBounds.Top;
                }
                else if (this.Bottom > overlayBounds.Bottom)
                {
                    this.Top = overlayBounds.Bottom - this.Height;
                }
            }
        }

        #region ESP GUI Event Handlers
        #region Check Boxes
        private void cbESP_CheckedChanged(object sender)
        {
            ToggleESP();
        }

        private void cbPlayers_CheckedChanged(object sender)
        {
            TogglePlayersESP();
        }

        private void cbTeam_CheckedChanged(object sender)
        {
            ToggleTeamESP();
        }

        private void cbScavs_CheckedChanged(object sender)
        {
            ToggleScavESP();
        }

        private void cbBosses_CheckedChanged(object sender)
        {
            ToggleBossESP();
        }

        private void cbItems_CheckedChanged(object sender)
        {
            ToggleItemESP();
        }

        private void cbSkeletons_CheckedChanged(object sender)
        {
            ToggleSkeletonsESP();
        }

        private void cbBoundingBox_CheckedChanged(object sender)
        {
            ToggleBoxESP();
        }

        private void cbHeadDot_CheckedChanged(object sender)
        {
            ToggleHeadDotESP();
        }

        private void cbRestart_CheckedChanged(object sender)
        {
            RestartRadar();
        }
        #endregion
        #region Track Bars
        private void tbBoneDist_Scroll(object sender)
        {
            if (sender is ReaLTaiizor.Controls.ForeverTrackBar trackBar)
            {
                // Get the new value from the trackbar
                int newValue = trackBar.Value;

                // Update radar configuration with the new trackbar value
                _config.BoneLimit = newValue;

                frmMain.sldrBoneDist.Value = newValue; // Synchronize the GUI with the Main Form

            }
        }

        private void tbPlayerDist_Scroll(object sender)
        {
            if (sender is ReaLTaiizor.Controls.ForeverTrackBar trackBar)
            {
                // Get the new value from the trackbar
                int newValue = trackBar.Value;

                // Update radar configuration with the new trackbar value
                _config.PlayerDist = newValue;

                frmMain.sldrPlayerDist.Value = newValue; // Synchronize the GUI with the Main Form

            }
        }

        private void tbTeamDist_Scroll(object sender)
        {
            if (sender is ReaLTaiizor.Controls.ForeverTrackBar trackBar)
            {
                // Get the new value from the trackbar
                int newValue = trackBar.Value;

                // Update radar configuration with the new trackbar value
                _config.TeamDist = newValue;

                frmMain.sldrTeamDist.Value = newValue; // Synchronize the GUI with the Main Form

            }
        }

        private void tbScavDist_Scroll(object sender)
        {
            if (sender is ReaLTaiizor.Controls.ForeverTrackBar trackBar)
            {
                // Get the new value from the trackbar
                int newValue = trackBar.Value;

                // Update radar configuration with the new trackbar value
                _config.ScavDist = newValue;

                frmMain.sldrScavDist.Value = newValue; // Synchronize the GUI with the Main Form

            }
        }

        private void tbItemDist_Scroll(object sender)
        {
            if (sender is ReaLTaiizor.Controls.ForeverTrackBar trackBar)
            {
                // Get the new value from the trackbar
                int newValue = trackBar.Value;

                // Update radar configuration with the new trackbar value
                _config.ItemDist = newValue;

                frmMain.sldrItemDist.Value = newValue; // Synchronize the GUI with the Main Form

            }
        }
        #endregion
        #endregion
        #region Aim GUI Event Handlers
        #region Check Boxes
        private void cbAim_CheckedChanged(object sender)
        {
            ToggleAim();
        }

        private void cbPMC_CheckedChanged(object sender)
        {
            TogglePMC();
        }

        private void cbScav_CheckedChanged(object sender)
        {
            ToggleScav();
        }

        private void cbHead_CheckedChanged(object sender)
        {
            ToggleHead();
        }

        private void cbNeck_CheckedChanged(object sender)
        {
            ToggleNeck();
        }

        private void cbChest_CheckedChanged(object sender)
        {
            ToggleChest();
        }

        private void cbPelvis_CheckedChanged(object sender)
        {
            TogglePelvis();
        }

        private void cbRightLeg_CheckedChanged(object sender)
        {
            ToggleRightLeg();
        }

        private void cbLeftLeg_CheckedChanged(object sender)
        {
            ToggleLeftLeg();
        }

        private void cbClosest_CheckedChanged(object sender)
        {
            ToggleClosest();
        }

        private void cbShowFOV_CheckedChanged(object sender)
        {
            ToggleShowFOV();
        }
        #endregion
        #region Track Bars
        private void tbAimFOV_Scroll(object sender)
        {
            if (sender is ReaLTaiizor.Controls.ForeverTrackBar trackBar)
            {
                // Get the new value from the trackbar
                int newValue = trackBar.Value;

                // Update radar configuration with the new trackbar value
                _config.AimbotFOV = newValue;

                frmMain.sldrAimbotFOV.Value = newValue; // Synchronize the GUI with the Main Form

            }
        }

        private void tbAimSmoothness_Scroll(object sender)
        {
            if (sender is ReaLTaiizor.Controls.ForeverTrackBar trackBar)
            {
                // Get the new value from the trackbar
                int newValue = trackBar.Value;

                // Update radar configuration with the new trackbar value
                _config.AimbotSmoothness = newValue;

                frmMain.sldrAimbotSmoothness.Value = newValue; // Synchronize the GUI with the Main Form

            }
        }

        private void tbAimDistance_Scroll(object sender)
        {
            if (sender is ReaLTaiizor.Controls.ForeverTrackBar trackBar)
            {
                // Get the new value from the trackbar
                int newValue = trackBar.Value;

                // Update radar configuration with the new trackbar value
                _config.AimbotMaxDistance = newValue;

                frmMain.sldrAimDistance.Value = newValue; // Synchronize the GUI with the Main Form

            }
        }
        #endregion
        #endregion
        #region Writes GUI Event Handlers
        #region Check Boxes
        private void cbMemoryWrites_CheckedChanged(object sender)
        {
            ToggleMemoryWrites();
        }

        private void cbInfiniteStamina_CheckedChanged(object sender)
        {
            ToggleInfiniteStamina();
        }

        private void cbThirdPerson_CheckedChanged(object sender)
        {
            ToggleThirdPerson();
        }

        private void cbInventoryBlur_CheckedChanged(object sender)
        {
            ToggleInfiniteStamina();
        }

        private void cbFreezeTime_CheckedChanged(object sender)
        {
            ToggleFreezeTime();
        }

        private void cbTimeScale_CheckedChanged(object sender)
        {
            ToggleTimeScale();
        }

        private void cbLootThroughWalls_CheckedChanged(object sender)
        {
            ToggleLootThroughWalls();
        }

        private void cbExtendedReach_CheckedChanged(object sender)
        {
            ToggleExtendedReach();
        }

        private void cbNoRecoil_CheckedChanged(object sender)
        {
            ToggleNoRecoil();
        }

        private void cbNoSway_CheckedChanged(object sender)
        {
            ToggleNoSway();
        }

        private void cbInstantADS_CheckedChanged(object sender)
        {
            ToggleInstantADS();
        }

        private void cbThermalVision_CheckedChanged(object sender)
        {
            ToggleThermalVision();
        }

        private void cbOpticalThermal_CheckedChanged(object sender)
        {
            ToggleOpticalThermal();
        }

        private void cbNightVision_CheckedChanged(object sender)
        {
            ToggleNightVision();
        }

        private void cbNoWeaponMalfunctions_CheckedChanged(object sender)
        {
            ToggleNoWeaponMalfunctions();
        }

        private void cbNoVisor_CheckedChanged(object sender)
        {
            ToggleNoVisor();
        }
        #endregion
        #region Track Bars
        private void tbTimeOfDay_Scroll(object sender)
        {
            if (sender is ReaLTaiizor.Controls.ForeverTrackBar trackBar)
            {
                // Get the new value from the trackbar
                int newValue = trackBar.Value;

                // Update radar configuration with the new trackbar value
                _config.TimeOfDay = newValue;

                frmMain.sldrTimeOfDay.Value = newValue; // Synchronize the GUI with the Main Form

            }
        }

        private void tbTimeFactor_Scroll(object sender)
        {
            if (sender is ReaLTaiizor.Controls.ForeverTrackBar trackBar)
            {
                // Get the new value from the trackbar
                int newValue = trackBar.Value;

                _config.TimeScaleFactor = (float)newValue / 10; // Update radar configuration
                if (_config.TimeScaleFactor < 1)
                {
                    _config.TimeScaleFactor = 1;
                }
                lblTimeScaleFactorX.Text = $"x{(_config.TimeScaleFactor)}"; // Update the label text


                frmMain.sldrExtendedReachDistance.Value = newValue; // Synchronize the GUI with the Main Form

            }
        }

        private void tbLootThroughWallsDistance_Scroll(object sender)
        {
            if (sender is ReaLTaiizor.Controls.ForeverTrackBar trackBar)
            {
                // Get the new value from the trackbar
                int newValue = trackBar.Value;

                var pveMode = _config.PvEMode;
                var distance = (float)newValue;

                if (pveMode)
                    _config.LootThroughWallsDistancePvE = distance;
                else
                {
                    if (distance > 3)
                        distance = 3;

                    // Update radar configuration with the new trackbar value
                    _config.LootThroughWallsDistance = distance;
                }

                lblLootThroughWallsX.Text = $"x{(distance)}"; // Update the label text


                frmMain.sldrLootThroughWallsDistance.Value = newValue; // Synchronize the GUI with the Main Form

            }
        }

        private void tbReachDistance_Scroll(object sender)
        {
            if (sender is ReaLTaiizor.Controls.ForeverTrackBar trackBar)
            {
                // Get the new value from the trackbar
                int newValue = trackBar.Value;

                var pveMode = _config.PvEMode;
                var distance = (float)newValue;

                if (pveMode)
                    _config.ExtendedReachDistancePvE = distance;
                else
                {
                    if (distance > 3)
                        distance = 3;

                    // Update radar configuration with the new trackbar value
                    _config.ExtendedReachDistance = distance;
                }

                lblReachX.Text = $"x{(distance)}"; // Update the label text


                frmMain.sldrExtendedReachDistance.Value = newValue; // Synchronize the GUI with the Main Form
            }
        }
        #endregion
        #endregion
    }
}
