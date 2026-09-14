using Godot;

public partial class Player : CharacterBody2D
{
    [Export]
    public float Speed { get; set; } = 100.0f;

    [Export]
    public AnimationPlayer AnimationPlayer { get; set; }

    private enum Direction
    {
        Up,
        Down,
        Left,
        Right
    }

    private Direction lastDirection = Direction.Down;

    public bool Moving { get; private set; }

    public override void _PhysicsProcess(double delta)
    {
        Vector2 direction = Input.GetVector(
            "ui_left",
            "ui_right",
            "ui_up",
            "ui_down"
        );

        // No se está presionando ninguna tecla
        if (direction == Vector2.Zero)
        {
            Velocity = Vector2.Zero;
            Moving = false;
        }
        else
        {
            // Se está moviendo
            Velocity = direction * Speed;
            Moving = true;

            UpdateDirection(direction);
        }

        MoveAndSlide();

        UpdateAnimation();
    }

    private void UpdateDirection(Vector2 direction)
    {
        // Movimiento horizontal
        if (Mathf.Abs(direction.X) > Mathf.Abs(direction.Y))
        {
            if (direction.X > 0)
                lastDirection = Direction.Right;
            else
                lastDirection = Direction.Left;
        }
        // Movimiento vertical
        else
        {
            if (direction.Y > 0)
                lastDirection = Direction.Down;
            else
                lastDirection = Direction.Up;
        }
    }

    private void UpdateAnimation()
    {
        if (AnimationPlayer == null)
            return;

        string animationName;

        if (Moving)
        {
            animationName = lastDirection switch
            {
                Direction.Up => "walk_up",
                Direction.Down => "walk_down",
                Direction.Left => "walk_left",
                Direction.Right => "walk_right",
                _ => "walk_down"
            };
        }
        else
        {
            animationName = lastDirection switch
            {
                Direction.Up => "idle_up",
                Direction.Down => "idle_down",
                Direction.Left => "idle_left",
                Direction.Right => "idle_right",
                _ => "idle_down"
            };
        }

        if (AnimationPlayer.CurrentAnimation != animationName)
        {
            AnimationPlayer.Play(animationName);
        }
    }
}
