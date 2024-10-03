namespace eft_dma_radar;

public static class MenuManager
{
    public static bool _menuShown;

    public static void ToggleMenu()
    {
        var menu = InGameMenu.Instance;

        if (menu.InvokeRequired)
            menu.Invoke(new MethodInvoker(() => ToggleMenuVisibility(menu)));
        else
            ToggleMenuVisibility(menu);
    }

    private static void ToggleMenuVisibility(InGameMenu menu)
    {
        if (menu.Visible)
        {
            menu.Hide();
            _menuShown = false;
        }
        else
        {
            menu.Show();
            _menuShown = true;
            menu.BringToFront();
            menu.Focus();
        }
    }

    public static void MenuUp()
    {
        var menu = InGameMenu.Instance;
        if (menu.InvokeRequired)
            menu.Invoke(new MethodInvoker(() => MoveSelection(menu, -1)));
        else
            MoveSelection(menu, -1);
    }

    public static void MenuDown()
    {
        var menu = InGameMenu.Instance;
        if (menu.InvokeRequired)
            menu.Invoke(new MethodInvoker(() => MoveSelection(menu, 1)));
        else
            MoveSelection(menu, 1);
    }

    public static void MenuLeft()
    {
        var menu = InGameMenu.Instance;
        if (menu.InvokeRequired)
            menu.Invoke(new MethodInvoker(() => AdjustSelectionValue(menu, -1)));
        else
            AdjustSelectionValue(menu, -1);
    }

    public static void MenuRight()
    {
        var menu = InGameMenu.Instance;
        if (menu.InvokeRequired)
            menu.Invoke(new MethodInvoker(() => AdjustSelectionValue(menu, 1)));
        else
            AdjustSelectionValue(menu, 1);
    }

    private static void AdjustSelectionValue(InGameMenu menu, int adjustment)
    {
        // Adjust the value by 10 units in the direction specified by 'adjustment'
        int adjustAmount = 10 * adjustment;

        switch (menu.currentSelection)
        {
            // Case is index as set within InGameMenu
            case 5: // Assuming 'Player Distance' is at index 5
                menu.PlayerDistance = Math.Max(0, menu.PlayerDistance + adjustAmount);
                menu.UpdateDistanceMenuItem("Player Distance", menu.PlayerDistance);
                break;
            case 6: // Assuming 'Team Distance' is at index 6
                menu.TeamDistance = Math.Max(0, menu.TeamDistance + adjustAmount);
                menu.UpdateDistanceMenuItem("Team Distance", menu.TeamDistance);
                break;
            case 7: // Assuming 'Scav Distance' is at index 7
                menu.ScavDistance = Math.Max(0, menu.ScavDistance + adjustAmount);
                menu.UpdateDistanceMenuItem("Scav Distance", menu.ScavDistance);
                break;
            case 8: // Assuming 'Loot Distance' is at index 7
                menu.LootDistance = Math.Max(0, menu.LootDistance + adjustAmount);
                menu.UpdateDistanceMenuItem("Loot Distance", menu.LootDistance);
                break;
            // Add cases for other distance settings...
            default:
                // For other menu items that don't have adjustable integer values, do nothing
                break;
        }
    }

    public static void AdjustMenuLeft()
    {
        var menu = InGameMenu.Instance;
        if (menu.InvokeRequired)
            menu.Invoke(new MethodInvoker(() => AdjustSelectionValue(menu, -1)));
        else
            AdjustSelectionValue(menu, -1);
    }

    public static void AdjustMenuRight()
    {
        var menu = InGameMenu.Instance;
        if (menu.InvokeRequired)
            menu.Invoke(new MethodInvoker(() => AdjustSelectionValue(menu, 1)));
        else
            AdjustSelectionValue(menu, 1);
    }

    private static void MoveSelection(InGameMenu menu, int direction)
    {
        // Assuming menuItems and currentSelection are public or have public getters/setters
        if (menu.menuItems == null || menu.menuItems.Count == 0) return;

        menu.currentSelection = (menu.currentSelection + direction + menu.menuItems.Count) % menu.menuItems.Count;
        menu.HighlightSelection();
    }

    public static void SelectMenuItem() // ADD THIS TO GAME
    {
        var menu = InGameMenu.Instance;
        if (menu.InvokeRequired)
            menu.Invoke(new MethodInvoker(() => ToggleSelectedItem(menu)));
        else
            ToggleSelectedItem(menu);
    }

    private static void ToggleSelectedItem(InGameMenu menu)
    {
        // Assuming ToggleMenuItem is a method in InGameMenu that handles the action
        // when a menu item is selected (toggled)
        menu.ToggleMenuItem(menu.currentSelection);
    }
}

public class KeyHandler
{
    private readonly TimeSpan debounceTime = TimeSpan.FromMilliseconds(150);
    private DateTime lastKeyPressTime;

    public KeyHandler()
    {
        lastKeyPressTime = DateTime.MinValue;
    }

    public bool IsDebouncedKeyPress()
    {
        if (DateTime.Now - lastKeyPressTime > debounceTime)
        {
            lastKeyPressTime = DateTime.Now;
            return true;
        }

        return false;
    }

    public void MenuLeft()
    {
        if (IsDebouncedKeyPress())
            MenuManager.AdjustMenuLeft();
    }

    public void MenuRight()
    {
        if (IsDebouncedKeyPress())
            MenuManager.AdjustMenuRight();
    }
}