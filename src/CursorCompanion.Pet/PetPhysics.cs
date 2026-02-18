using CursorCompanion.Windowing;

namespace CursorCompanion.Pet;

public class PetPhysics
{
    public float X { get; set; }
    public float Y { get; set; }
    public float VelocityY { get; set; }
    public bool IsGrounded { get; private set; }
    public bool Enabled { get; set; } = true;

    public float Gravity { get; set; } = 4000f;    // px/s²
    public float TerminalVelocity { get; set; } = 3000f; // px/s

    private int _spriteWidth;
    private int _spriteHeight;

    public void Initialize(int spriteWidth, int spriteHeight)
    {
        _spriteWidth = spriteWidth;
        _spriteHeight = spriteHeight;
    }

    public void Update(float dt, List<WindowRect> windows, int taskbarFloorY)
    {
        if (!Enabled)
        {
            IsGrounded = false;
            return;
        }

        // Apply gravity
        VelocityY += Gravity * dt;
        if (VelocityY > TerminalVelocity)
            VelocityY = TerminalVelocity;

        float newY = Y + VelocityY * dt;

        // Find support surface
        float supportY = taskbarFloorY; // taskbar as fallback floor
        bool foundSupport = false;

        // Pet's feet position and horizontal bounds
        float petLeft = X;
        float petRight = X + _spriteWidth;
        float petBottom = newY + _spriteHeight;

        foreach (var win in windows)
        {
            // Check horizontal overlap
            if (petRight <= win.Left || petLeft >= win.Right)
                continue;

            // Window top must be below current feet and above new feet (or at same level)
            float winTop = win.Top;
            if (winTop >= Y + _spriteHeight && winTop <= petBottom)
            {
                if (winTop < supportY)
                {
                    supportY = winTop;
                    foundSupport = true;
                }
            }
        }

        // Also check: are we currently resting on a window?
        if (!foundSupport && VelocityY >= 0)
        {
            float currentBottom = Y + _spriteHeight;
            foreach (var win in windows)
            {
                if (petRight <= win.Left || petLeft >= win.Right)
                    continue;

                float winTop = win.Top;
                // Standing on this window (within 2px tolerance)
                if (Math.Abs(currentBottom - winTop) < 2f)
                {
                    supportY = winTop;
                    foundSupport = true;
                    break;
                }
            }
        }

        // Check taskbar
        if (petBottom >= taskbarFloorY)
        {
            supportY = taskbarFloorY;
            foundSupport = true;
        }

        // Land or fall
        if (VelocityY >= 0 && newY + _spriteHeight >= supportY)
        {
            newY = supportY - _spriteHeight;
            VelocityY = 0;
            IsGrounded = true;
        }
        else
        {
            IsGrounded = false;
        }

        Y = newY;
    }

    public bool HasSupportBelow(List<WindowRect> windows, int taskbarFloorY)
    {
        float petLeft = X;
        float petRight = X + _spriteWidth;
        float petBottom = Y + _spriteHeight;

        // Check taskbar
        if (Math.Abs(petBottom - taskbarFloorY) < 4f)
            return true;

        foreach (var win in windows)
        {
            if (petRight <= win.Left || petLeft >= win.Right)
                continue;

            if (Math.Abs(petBottom - win.Top) < 4f)
                return true;
        }

        return false;
    }
}
