using CursorCompanion.Core;

namespace CursorCompanion.Pet;

public class IdleState : IState
{
    private readonly PetController _pet;
    public IdleState(PetController pet) => _pet = pet;

    public void Enter() => _pet.PlayAnimation("Idle");
    public void Update(float dt) { }
    public void Exit() { }
}

public class FallingState : IState
{
    private readonly PetController _pet;
    public FallingState(PetController pet) => _pet = pet;

    public void Enter() => _pet.PlayAnimation("Falling");
    public void Update(float dt) { }
    public void Exit() { }
}

public class LandingState : IState
{
    private readonly PetController _pet;
    private float _timer;
    private const float LandDuration = 0.3f;

    public LandingState(PetController pet) => _pet = pet;

    public void Enter()
    {
        _timer = 0;
        _pet.PlayAnimation("Landing");
    }

    public void Update(float dt)
    {
        _timer += dt;
        if (_timer >= LandDuration || _pet.AnimPlayer.IsFinished)
            _pet.SetState(PetState.Idle);
    }

    public void Exit() { }
}

public class DraggingState : IState
{
    private readonly PetController _pet;
    public DraggingState(PetController pet) => _pet = pet;

    public void Enter()
    {
        _pet.Physics.Enabled = false;
        _pet.PlayAnimation("Dragging");
    }

    public void Update(float dt) { }

    public void Exit()
    {
        _pet.Physics.Enabled = true;
        _pet.Physics.VelocityY = 0;
    }
}

public class ActionPlayingState : IState
{
    private readonly PetController _pet;
    public ActionPlayingState(PetController pet) => _pet = pet;

    public void Enter() { } // Animation set by caller
    public void Update(float dt)
    {
        if (_pet.AnimPlayer.IsFinished)
            _pet.SetState(PetState.Idle);
    }
    public void Exit() { }
}

public class SleepingState : IState
{
    private readonly PetController _pet;
    public SleepingState(PetController pet) => _pet = pet;

    public void Enter() => _pet.PlayAnimation("Sleeping");
    public void Update(float dt) { }
    public void Exit() { }
}
