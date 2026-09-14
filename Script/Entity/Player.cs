using Godot;
using System.Threading.Tasks;

public partial class Player : CharacterBody2D
{
    [Export]
    public float Speed { get; set; } = 100.0f;

    [Export]
    public AnimationPlayer AnimationPlayer { get; set; }

    // Tamaño de cada celda del RPG
    private const int CELL_SIZE = 16;

    // Velocidad del movimiento por código
    private const float CODE_MOVE_SPEED = 100.0f;

    private enum Direction
    {
        Up,
        Down,
        Left,
        Right
    }

    private Direction lastDirection = Direction.Down;

    public bool Moving { get; private set; }

    // Indica si el jugador está siendo controlado por el código
    public bool CodeMoving { get; private set; }

    // =========================================================
    // MOVIMIENTO NORMAL DEL JUGADOR
    // =========================================================

    public override void _PhysicsProcess(double delta)
    {
        // Si el código está moviendo al jugador,
        // no permitir movimiento con teclado.
        if (CodeMoving)
        {
            Velocity = Vector2.Zero;
            Moving = true;

            UpdateAnimation();

            return;
        }

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
            Velocity = direction * Speed;
            Moving = true;

            UpdateDirection(direction);
        }

        MoveAndSlide();

        UpdateAnimation();
    }

    // =========================================================
    // MOVIMIENTO DESDE EL CÓDIGO
    // =========================================================

    public async Task MoverCeldas(
        Vector2I direccion,
        int cantidad)
    {
        if (CodeMoving)
            return;

        CodeMoving = true;
        Moving = true;

        // Dirección de la animación
        Vector2 direccionVector =
            new Vector2(
                direccion.X,
                direccion.Y
            );

        UpdateDirection(direccionVector);

        for (int i = 0; i < cantidad; i++)
        {
            // ==========================================
            // DISTANCIA DE UNA CELDA
            // ==========================================

            Vector2 movimiento =
                new Vector2(
                    direccion.X * CELL_SIZE,
                    direccion.Y * CELL_SIZE
                );

            Vector2 posicionInicial =
                GlobalPosition;

            Vector2 posicionObjetivo =
                posicionInicial + movimiento;

            // ==========================================
            // COMPROBAR COLISIÓN
            // ==========================================

            KinematicCollision2D collision =
                MoveAndCollide(
                    movimiento,
                    testOnly: true
                );

            if (collision != null)
            {
                GD.Print(
                    "Movimiento bloqueado por una colisión."
                );

                break;
            }

            // ==========================================
            // ANIMAR LA CELDA
            // ==========================================

            float distancia =
                posicionInicial.DistanceTo(
                    posicionObjetivo
                );

            float duracion =
                distancia / CODE_MOVE_SPEED;

            float tiempo = 0.0f;

            while (tiempo < duracion)
            {
                tiempo += (float)GetProcessDeltaTime();

                float progreso =
                    Mathf.Clamp(
                        tiempo / duracion,
                        0.0f,
                        1.0f
                    );

                GlobalPosition =
                    posicionInicial.Lerp(
                        posicionObjetivo,
                        progreso
                    );

                await ToSignal(
                    GetTree(),
                    SceneTree.SignalName.ProcessFrame
                );
            }

            GlobalPosition = posicionObjetivo;
        }

        // ==========================================
        // TERMINÓ EL MOVIMIENTO
        // ==========================================

        GlobalPosition =
            new Vector2(
                Mathf.Round(GlobalPosition.X / CELL_SIZE) * CELL_SIZE,
                Mathf.Round(GlobalPosition.Y / CELL_SIZE) * CELL_SIZE
            );

        CodeMoving = false;
        Moving = false;

        Velocity = Vector2.Zero;

        UpdateAnimation();
    }

    // =========================================================
    // ACTUALIZAR DIRECCIÓN
    // =========================================================

    private void UpdateDirection(Vector2 direction)
    {
        if (Mathf.Abs(direction.X) > Mathf.Abs(direction.Y))
        {
            if (direction.X > 0)
                lastDirection = Direction.Right;
            else
                lastDirection = Direction.Left;
        }
        else
        {
            if (direction.Y > 0)
                lastDirection = Direction.Down;
            else
                lastDirection = Direction.Up;
        }
    }

    // =========================================================
    // ANIMACIÓN
    // =========================================================

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