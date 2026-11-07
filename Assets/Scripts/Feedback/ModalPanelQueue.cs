using System;
using System.Collections.Generic;
using UnityEngine;

public enum ModalPriority
{
    TileOrTurret = 0,
    ExtraPick = 1,
    Modifier = 2,
    Legendary = 3
}

[DefaultExecutionOrder(-50)]
public class ModalPanelQueue : MonoBehaviour
{
    private class Request
    {
        public Action open;
        public Func<bool> isOpen;
        public ModalPriority priority;
        public int order;
        public string label;
    }

    private static ModalPanelQueue instance;

    [Header("Debug")]
    [SerializeField] private bool logTransitions = true;

    private readonly List<Request> pending = new();
    private Request current;
    private int orderCounter;
    private int openedFrame = -1;

    public static ModalPanelQueue Instance
    {
        get
        {
            if (instance != null) return instance;
            instance = FindFirstObjectByType<ModalPanelQueue>();
            if (instance == null && Application.isPlaying)
                instance = new GameObject("ModalPanelQueue").AddComponent<ModalPanelQueue>();
            return instance;
        }
    }

    public bool HasOpenPanel => current != null;
    public int PendingCount => pending.Count;
    public ModalPriority? CurrentPriority => current?.priority;
    public bool IsModifierOpen => current != null && (current.priority == ModalPriority.Modifier || current.priority == ModalPriority.Legendary);

    public static bool ModifierPanelActive => instance != null && instance.IsModifierOpen;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this);
            return;
        }
        instance = this;
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    public void Enqueue(Action open, Func<bool> isOpen, ModalPriority priority = ModalPriority.TileOrTurret, string label = null)
    {
        if (open == null) return;

        pending.Add(new Request
        {
            open = open,
            isOpen = isOpen,
            priority = priority,
            order = orderCounter++,
            label = string.IsNullOrEmpty(label) ? priority.ToString() : label
        });

        TryOpenNext();
    }

    private void Update()
    {
        if (current != null)
        {
            if (Time.frameCount == openedFrame) return;
            bool stillOpen = current.isOpen != null && SafeIsOpen(current);
            if (stillOpen) return;

            if (logTransitions) Log.Info(nameof(ModalPanelQueue), $"Closed {current.label}");
            current = null;
        }

        TryOpenNext();
    }

    private void TryOpenNext()
    {
        if (current != null || pending.Count == 0) return;

        int best = 0;
        for (int i = 1; i < pending.Count; i++)
        {
            var a = pending[i];
            var b = pending[best];
            if (a.priority > b.priority || (a.priority == b.priority && a.order < b.order))
                best = i;
        }

        current = pending[best];
        pending.RemoveAt(best);
        openedFrame = Time.frameCount;

        if (logTransitions) Log.Info(nameof(ModalPanelQueue), $"Opening {current.label} ({pending.Count} waiting)");

        try
        {
            current.open();
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            current = null;
        }
    }

    private static bool SafeIsOpen(Request request)
    {
        try
        {
            return request.isOpen();
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            return false;
        }
    }
}
