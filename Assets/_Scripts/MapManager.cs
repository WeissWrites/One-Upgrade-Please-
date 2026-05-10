using UnityEngine;
using UnityEngine.Events;

// Place this on a persistent scene object.
// Call BeginMapChange() to start swapping the layout.
// Call MarkMapFinished() from an animation event or your map-swap coroutine when done.
public class MapManager : MonoBehaviour
{
    public static MapManager Instance { get; private set; }

    public UnityEvent onMapChangeBegin;

    void Awake() { Instance = this; }

    public void BeginMapChange()
    {
        onMapChangeBegin?.Invoke();
    }

    // Call this from your animation event / map-swap script when the new layout is fully in place
    public void MarkMapFinished()
    {
        if (ShopkeeperSequence.Instance == null) return;
        ShopkeeperSequence.Instance.isMapFinished = true;
        ShopkeeperSequence.Instance.OnMapFinished();
    }
}
