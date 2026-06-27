using Godot;

namespace LastWord.Enemy;

/// <summary>
/// Singleton Node that holds a cached Array of all ListenerAI instances.
/// Refreshes when players join/leave. Replaces per-frame GetNodesInGroup calls.
/// </summary>
public partial class ListenerGroupCache : Node
{
    public static ListenerGroupCache Instance { get; private set; }

    private Godot.Collections.Array<ListenerAI> _listeners = new();

    public Godot.Collections.Array<ListenerAI> Listeners => _listeners;

    public override void _EnterTree()
    {
        Instance = this;
    }

    public override void _ExitTree()
    {
        if (Instance == this) Instance = null;
    }

    public override void _Ready()
    {
        // Initial population
        Refresh();

        // Subscribe to scene tree changes
        var tree = GetTree();
        if (tree != null)
        {
            tree.NodeAdded += OnNodeAdded;
            tree.NodeRemoved += OnNodeRemoved;
        }
    }

    private void OnNodeAdded(Node node)
    {
        if (node is ListenerAI && !_listeners.Contains((ListenerAI)node))
        {
            _listeners.Add((ListenerAI)node);
        }
    }

    private void OnNodeRemoved(Node node)
    {
        if (node is ListenerAI)
        {
            _listeners.Remove((ListenerAI)node);
        }
    }

    public void Refresh()
    {
        _listeners.Clear();
        var tree = GetTree();
        if (tree == null) return;
        foreach (var node in tree.GetNodesInGroup("Listener"))
        {
            if (node is ListenerAI listener)
                _listeners.Add(listener);
        }
    }
}
