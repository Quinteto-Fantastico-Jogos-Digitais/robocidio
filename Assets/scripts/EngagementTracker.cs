using UnityEngine;

public class EngagementTracker : MonoBehaviour
{
    public bool IsTargeted { get; private set; } = false;

    [HideInInspector] public Transform CurrentTracker = null;

    public void StartTracking(Transform tracker)
    {
        IsTargeted = true;
        CurrentTracker = tracker;
    }

    public void StopTracking(Transform potentialTracker)
    {
        if (CurrentTracker == potentialTracker)
        {
            IsTargeted = false;
            CurrentTracker = null;
        }
    }
}