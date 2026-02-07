using System.Collections.Generic;
using UnityEngine;

// Trạng thái của một hành động
public enum Nhan_NodeState { RUNNING, SUCCESS, FAILURE }

// Class cha của mọi Node
public abstract class Nhan_Node
{
    protected Nhan_NodeState _state;
    public Nhan_NodeState State => _state;
    public abstract Nhan_NodeState Evaluate();
}

// Selector (Logic HOẶC): Chạy các node con, chỉ cần 1 cái thành công là trả về SUCCESS ngay
public class Nhan_Selector : Nhan_Node
{
    private List<Nhan_Node> _nodes = new List<Nhan_Node>();
    public Nhan_Selector(List<Nhan_Node> nodes) => _nodes = nodes;

    public override Nhan_NodeState Evaluate()
    {
        foreach (var node in _nodes)
        {
            switch (node.Evaluate())
            {
                case Nhan_NodeState.FAILURE: continue;
                case Nhan_NodeState.SUCCESS: _state = Nhan_NodeState.SUCCESS; return _state;
                case Nhan_NodeState.RUNNING: _state = Nhan_NodeState.RUNNING; return _state;
            }
        }
        _state = Nhan_NodeState.FAILURE;
        return _state;
    }
}

// Sequence (Logic VÀ): Chạy các node con, TẤT CẢ phải thành công mới trả về SUCCESS
public class Nhan_Sequence : Nhan_Node
{
    private List<Nhan_Node> _nodes = new List<Nhan_Node>();
    public Nhan_Sequence(List<Nhan_Node> nodes) => _nodes = nodes;

    public override Nhan_NodeState Evaluate()
    {
        bool anyChildRunning = false;
        foreach (var node in _nodes)
        {
            switch (node.Evaluate())
            {
                case Nhan_NodeState.FAILURE: _state = Nhan_NodeState.FAILURE; return _state;
                case Nhan_NodeState.SUCCESS: continue;
                case Nhan_NodeState.RUNNING: anyChildRunning = true; continue;
            }
        }
        _state = anyChildRunning ? Nhan_NodeState.RUNNING : Nhan_NodeState.SUCCESS;
        return _state;
    }
}