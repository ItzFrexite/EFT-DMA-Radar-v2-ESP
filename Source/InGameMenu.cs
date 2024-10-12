using System.Diagnostics;


namespace eft_dma_radar;

public partial class InGameMenu : Form
{
    private const int WM_NCHITTEST = 0x84;
    private const int HTCLIENT = 0x1;
    private const int HTCAPTION = 0x2;

    private static InGameMenu _instance;

    private readonly Stopwatch _sw = new();
    public int currentSelection;
    public bool isMenuOpen;


    public bool isNoRecoilOn;

    public List<Label> menuItems;
    private Dictionary<string, Label> statusLabels;


    public InGameMenu()
    {
        InitializeComponent();
        SetupMenu();
        Width = 250;
        KeyPreview = true;
        KeyDown += MenuForm_KeyDown;

        TopMost = true;
        ShowInTaskbar = false;
    }

    public InputManager Inputs => Memory._inputManager;

    public static InGameMenu Instance
    {
        get
        {
            if (_instance == null || _instance.IsDisposed) _instance = new InGameMenu();
            return _instance;
        }
    }

    protected override void WndProc(ref Message m)
    {
        base.WndProc(ref m);

        if (m.Msg == WM_NCHITTEST && (int)m.Result == HTCLIENT)
            m.Result = (IntPtr)HTCAPTION;
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        Activate();
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == Keys.F7)
        {
            Hide();
            Overlay.isMenuShown = false;
            isMenuOpen = false;
            // Request to close the overlay
            ApplicationManager.RequestOverlayClose();

            return true;
        }

        if (keyData == Keys.Insert)
        {
            Hide();
            Overlay.isMenuShown = false;
            isMenuOpen = false;
            return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    private void SetupMenu()
    {
        menuItems = new List<Label>();
        statusLabels = new Dictionary<string, Label>();

        // When adding more remember to add here (SetupMenu()), MenuForm_KeyDown() (If Incremental), ToggleMenuItem() and AdjustSelectionValue() within MenuManager if Incremental

        CreateMenuItem("ESP", isESPOn, new Point(10, 50));
        CreateMenuItem("Bone ESP", isPlayerESPOn, new Point(10, 70));
        CreateMenuItem("Player ESP", isPlayerESPOn, new Point(10, 90));
        CreateMenuItem("Team ESP", isTeamESPOn, new Point(10, 110));
        CreateMenuItem("Scav ESP", isScavESPOn, new Point(10, 130));
        CreateMenuItem("Loot ESP", isLootESPOn, new Point(10, 150));
        CreateMenuItem("Bounding Box", isBoxESPOn, new Point(10, 170));
        CreateMenuItem("Head Dot", isHeadDotOn, new Point(10, 190));

        CreateMenuItem("Bone Distance", BoneDistance, new Point(10, 210));
        CreateMenuItem("Player Distance", PlayerDistance, new Point(10, 230)); 
        CreateMenuItem("Team Distance", TeamDistance, new Point(10, 250)); 
        CreateMenuItem("Scav Distance", ScavDistance, new Point(10, 270));
        CreateMenuItem("Loot Distance", LootDistance, new Point(10, 290));

        //CreateMenuItem("Aimbot", isAimbotOn, new Point(10, 230));
        //CreateMenuItem("No Recoil", isNoRecoilOn, new Point(10, 250));

        HighlightSelection();
    }

    private void CreateMenuItem(string text, object status, Point location)
    {
        int statusLabelOffset = 120; // Adjust this value as needed for better alignment

        var statusLabel = new Label
        {
            Text = FormatStatus(status),
            Location = new Point(location.X + statusLabelOffset, location.Y),
            AutoSize = true,
            Font = new Font("Arial", 10),
            ForeColor = GetStatusColor(status)
        };
        Controls.Add(statusLabel);

        var menuItemLabel = new Label
        {
            Text = text,
            Location = location,
            AutoSize = true,
            Font = new Font("Arial", 10)
        };
        Controls.Add(menuItemLabel);

        menuItems.Add(menuItemLabel);
        statusLabels.Add(text, statusLabel);
    }

    private string FormatStatus(object status)
    {
        return status switch
        {
            bool b => $"[{(b ? "ON" : "OFF")}]",
            int i => $"[{i}]",
            _ => "[UNKNOWN]"
        };
    }

    private Color GetStatusColor(object status)
    {
        return status switch
        {
            bool b => b ? Color.Green : Color.Red,
            int _ => Color.Blue,
            _ => Color.Gray
        };
    }

    private void MenuForm_KeyDown(object sender, KeyEventArgs e)
    {
        if (menuItems == null || menuItems.Count == 0)
            return;

        switch (e.KeyCode)
        {
            case Keys.Up:
                currentSelection = (currentSelection - 1 + menuItems.Count) % menuItems.Count;
                break;
            case Keys.Down:
                currentSelection = (currentSelection + 1) % menuItems.Count;
                break;
            case Keys.Enter:
                ToggleMenuItem(currentSelection);
                break;

            case Keys.Left:
                if (currentSelection == 8) // Bone Distance
                {
                    BoneDistance = Math.Max(0, BoneDistance - 10); // Decrement
                    UpdateMenuItem("Bone Distance", BoneDistance);
                }
                else if (currentSelection == 9) // Player Distance
                {
                    PlayerDistance = Math.Max(0, PlayerDistance - 10); // Decrement
                    UpdateMenuItem("Player Distance", PlayerDistance);
                }
                else if (currentSelection == 10) // Team Distance
                {
                    TeamDistance = Math.Max(0, TeamDistance - 10); // Decrement
                    UpdateMenuItem("Team Distance", TeamDistance);
                }
                else if (currentSelection == 11) // Scav Distance
                {
                    ScavDistance = Math.Max(0, ScavDistance - 10); // Decrement
                    UpdateMenuItem("Scav Distance", ScavDistance);
                }
                else if (currentSelection == 12) // Loot Distance
                {
                    LootDistance = Math.Max(0, LootDistance - 10); // Decrement
                    UpdateMenuItem("Loot Distance", LootDistance);
                }
                break;
            case Keys.Right:
                if (currentSelection == 8) // Bone Distance Distance
                {
                    BoneDistance += 10; // Increment
                    UpdateMenuItem("Bone Distance", BoneDistance);
                }
                else if (currentSelection == 9) // Player Distance
                {
                    PlayerDistance += 10; // Increment
                    UpdateMenuItem("Player Distance", PlayerDistance);
                }
                else if (currentSelection == 10) // Team Distance
                {
                    TeamDistance += 10; // Increment
                    UpdateMenuItem("Team Distance", TeamDistance);
                }
                else if (currentSelection == 11) // Scav Distance
                {
                    ScavDistance += 10; // Increment
                    UpdateMenuItem("Scav Distance", ScavDistance);
                }
                else if (currentSelection == 12) // Loot Distance
                {
                    LootDistance += 10; // Increment
                    UpdateMenuItem("Loot Distance", LootDistance);
                }
                break;
        }

        HighlightSelection();
    }

    public void UpdateDistanceMenuItem(string menuItemText, int value)
    {
        var statusLabel = FindStatusLabelForMenuItem(menuItemText);
        if (statusLabel == null) return;
        statusLabel.Text = $"[{value}]";
    }

    public void HighlightSelection()
    {
        if (menuItems == null || menuItems.Count == 0 || currentSelection < 0 ||
            currentSelection >= menuItems.Count) return;

        foreach (var item in menuItems)
        {
            item.BackColor = Color.Transparent;
            item.ForeColor = Color.White;
        }

        menuItems[currentSelection].BackColor = Color.FromArgb(128, Color.Gray);
    }

    public void ToggleMenuItem(int index)
    {
        switch (index)
        {
            case 0:
                isESPOn = !isESPOn;
                UpdateMenuItem("ESP", isESPOn);
                break;
            case 1:
                isBoneESPOn = !isBoneESPOn;
                UpdateMenuItem("Bone ESP", isBoneESPOn);
                break;
            case 2:
                isPlayerESPOn = !isPlayerESPOn;
                UpdateMenuItem("Player ESP", isPlayerESPOn);
                break;
            case 3:
                isTeamESPOn = !isTeamESPOn;
                UpdateMenuItem("Team ESP", isTeamESPOn);
                break;
            case 4:
                isScavESPOn = !isScavESPOn;
                UpdateMenuItem("Scav ESP", isScavESPOn);
                break;
            case 5:
                isLootESPOn = !isLootESPOn;
                UpdateMenuItem("Loot ESP", isLootESPOn);
                break;
            case 6:
                isBoxESPOn = !isBoxESPOn;
                UpdateMenuItem("Bounding Box", isBoxESPOn);
                break;
            case 7:
                isHeadDotOn = !isHeadDotOn;
                UpdateMenuItem("Head Dot", isHeadDotOn);
                break;
            case 8: // Bone Distance
                BoneDistance += 10; // Adjust the increment as needed
                UpdateMenuItem("Bone Distance", BoneDistance);
                break;
            case 9: // Player Distance
                PlayerDistance += 10; // Adjust the increment as needed
                UpdateMenuItem("Player Distance", PlayerDistance);
                break;
            case 10: // Team Distance
                TeamDistance += 10; // Adjust the increment as needed
                UpdateMenuItem("Team Distance", TeamDistance);
                break;
            case 11: // Scav Distance
                ScavDistance += 10; // Adjust the increment as needed
                UpdateMenuItem("Scav Distance", ScavDistance);
                break;
            case 12: // Loot Distance
                LootDistance += 10; // Adjust the increment as needed
                UpdateMenuItem("Loot Distance", LootDistance);
                break;
           // case 9:
                //isAimbotOn = !isAimbotOn;
                //UpdateMenuItem("Aimbot", isAimbotOn);
                //break;
            //case 10:
                //isNoRecoilOn = !isNoRecoilOn;
                //UpdateMenuItem("No Recoil", isNoRecoilOn);
                //break;
        }
    }

    private Label FindStatusLabelForMenuItem(string menuItemText)
    {
        return (statusLabels.GetValueOrDefault(menuItemText));
    }

    private void UpdateMenuItem(string menuItemText, object value)
    {
        var statusLabel = FindStatusLabelForMenuItem(menuItemText);
        if (statusLabel == null) return;
        statusLabel.Text = FormatStatus(value);
        statusLabel.ForeColor = GetStatusColor(value);
    }

    #region Getters and Setters

    private bool isESPOn
    {
        get => Overlay.isESPOn;
        set => Overlay.isESPOn = value;
    }

    private bool isBoneESPOn
    {
        get => Overlay.isBoneESPOn;
        set => Overlay.isBoneESPOn = value;
    }

    private bool isPlayerESPOn
    {
        get => Overlay.isPMCOn;
        set => Overlay.isPMCOn = value;
    }

    private bool isTeamESPOn
    {
        get => Overlay.isTeamOn;
        set => Overlay.isTeamOn = value;
    }

    private bool isScavESPOn
    {
        get => Overlay.isScavOn;
        set => Overlay.isScavOn = value;
    }

    private bool isLootESPOn
    {
        get => Overlay.isLootOn;
        set => Overlay.isLootOn = value;
    }

    private bool isBoxESPOn
    {
        get => Overlay.isBoxOn;
        set => Overlay.isBoxOn = value;
    }

    private bool isHeadDotOn
    {
        get => Overlay.isHeadDotOn;
        set => Overlay.isHeadDotOn = value;
    }

    public int BoneDistance
    {
        get => Overlay.boneLimit;
        set => Overlay.boneLimit = value;
    }

    public int PlayerDistance
    {
        get => Overlay.playerLimit;
        set => Overlay.playerLimit = value;
    }

    public int ScavDistance
    {
        get => Overlay.npcLimit;
        set => Overlay.npcLimit = value;
    }
    
    public int TeamDistance
    {
        get => Overlay.teamLimit;
        set => Overlay.teamLimit = value;
    }

    public int LootDistance
    {
        get => Overlay.lootLimit;
        set => Overlay.lootLimit = value;
    }

    //private bool isAimbotOn
    //{
        //get => AimbotManager.aimbotOn;
        //set => AimbotManager.aimbotOn = value;
    //}

    #endregion
}