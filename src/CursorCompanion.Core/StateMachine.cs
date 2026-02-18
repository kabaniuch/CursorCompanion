namespace CursorCompanion.Core;

public enum PetState
{
    Idle,
    Falling,
    Landing,
    Dragging,
    ActionPlaying,
    Sleeping
}

public interface IState
{
    void Enter();
    void Update(float dt);
    void Exit();
}

public class StateMachine<TState> where TState : struct, Enum
{
    private readonly Dictionary<TState, IState> _states = new();
    private IState? _currentStateImpl;

    public TState CurrentState { get; private set; }
    public event Action<TState, TState>? OnStateChanged;

    public void RegisterState(TState key, IState state)
    {
        _states[key] = state;
    }

    public void SetState(TState newState)
    {
        if (EqualityComparer<TState>.Default.Equals(CurrentState, newState) && _currentStateImpl != null)
            return;

        var oldState = CurrentState;
        _currentStateImpl?.Exit();

        CurrentState = newState;
        if (_states.TryGetValue(newState, out var stateImpl))
        {
            _currentStateImpl = stateImpl;
            _currentStateImpl.Enter();
        }
        else
        {
            _currentStateImpl = null;
        }

        OnStateChanged?.Invoke(oldState, newState);
    }

    public void Update(float dt)
    {
        _currentStateImpl?.Update(dt);
    }
}
