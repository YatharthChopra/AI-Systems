using UnityEngine;

// Base class for all AI states — every state must implement Enter, Execute, and Exit
public abstract class State
{
    public abstract void Enter();
    public abstract void Execute();
    public abstract void Exit();
}
