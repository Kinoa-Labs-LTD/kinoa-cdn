using System;
using UnityEngine;

public class KinoaOverlay : IDisposable
{
    private readonly GameObject overlayInstance;

    public KinoaOverlay(GameObject overlay)
    {
        if (overlay == null) return;
        overlayInstance = UnityEngine.Object.Instantiate(overlay);
    }

    public void Dispose()
    {
        if (overlayInstance == null) return;
        UnityEngine.Object.Destroy(overlayInstance);
    }
}