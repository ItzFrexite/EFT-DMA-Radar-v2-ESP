namespace eft_dma_radar;

public static class ApplicationManager
{
    public static event Action CloseOverlayRequested;

    public static void RequestOverlayClose()
    {
        CloseOverlayRequested?.Invoke();
    }
}