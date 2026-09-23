using Godot;
using System.Collections.Generic;
using System.Threading.Tasks;

public partial class Player : CharacterBody2D
{
    [Export]
    public float Speed { get; set; } = 100.0f;

    [Export]
    public AnimationPlayer AnimationPlayer { get; set; }

    [Export]
    public int PlayerIndex { get; set; } = 1;

    

    private const int CELL_SIZE = 16;

    // =========================
    // ESTADO
    // =========================

    private enum Direction
    {
        Up,
        Down,
        Left,
        Right
    }

    private Direction lastDirection = Direction.Down;

    public bool Moving { get; private set; }
    public bool CodeMoving { get; private set; }

    // Este valor debe ser puesto en true
    // cuando el usuario está escribiendo código.
    public bool IsWritingCode { get; set; }

    // =========================
    // MOVIMIENTO
    // =========================

    private Vector2 _targetPosition;
    private bool _isMovingToCell = false;

    // =========================
    // COLA DE MOVIMIENTO
    // =========================

    private readonly Queue<Vector2I> _moveQueue = new();

    // =========================
    // ANIMACIÓN
    // =========================

    private string _currentAnimation = "";

    public override void _EnterTree()
    {
        AddToGroup("player");

        if (int.TryParse(Name, out int peerId))
        {
            SetMultiplayerAuthority(peerId);

            GD.Print(
                $"Player {Name}: autoridad = {peerId}, " +
                $"soy autoridad? {IsMultiplayerAuthority()}"
            );
        }
        else
        {
            GD.PushWarning(
                $"Player: no se pudo parsear el nombre '{Name}' como peer ID."
            );
        }
    }

    public override void _Ready()
    {
        MsjPanel.Visible = false;

        AddToGroup("player");

        Position = SnapToGrid(Position);
        _targetPosition = Position;

        PlayIdleAnimation();
    }

    // ============================================================
    // INPUT
    // ============================================================

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!IsMultiplayerAuthority()) return;

        // NO permitir movimiento mientras se escribe código
        if (IsWritingCode) return;

        // NO permitir input manual mientras el código mueve al jugador
        if (CodeMoving) return;

        // NO aceptar otro movimiento mientras termina el actual
        if (_isMovingToCell) return;


        // NO permitir movimiento mientras se escribe código
        if (IsWritingCode)
            return;

        // NO permitir input manual mientras el código mueve al jugador
        if (CodeMoving)
            return;

        // NO aceptar otro movimiento mientras termina el actual
        if (_isMovingToCell)
            return;

        // Solo reaccionamos a teclas presionadas
        if (@event is not InputEventKey keyEvent)
            return;

        if (!keyEvent.Pressed || keyEvent.Echo)
            return;

        Vector2 direction = Vector2.Zero;

        switch (keyEvent.Keycode)
        {
            case Key.W:
            case Key.Up:
                direction = Vector2.Up;
                break;

            case Key.S:
            case Key.Down:
                direction = Vector2.Down;
                break;

            case Key.A:
            case Key.Left:
                direction = Vector2.Left;
                break;

            case Key.D:
            case Key.Right:
                direction = Vector2.Right;
                break;
        }

        if (direction != Vector2.Zero)
        {
            TryMoveToCell(direction);
        }
    }

    // ============================================================
    // PHYSICS
    // ============================================================

    public override void _PhysicsProcess(double delta)
    {
        if (!IsMultiplayerAuthority()) return;

        // -----------------------------------------
        // Movimiento controlado por código
        // -----------------------------------------

        if (CodeMoving)
        {
            // Si no estamos moviéndonos actualmente,
            // buscamos el siguiente movimiento.
            if (!_isMovingToCell)
            {
                StartNextCodeMovement();

                // Puede que no haya más movimientos
                if (!CodeMoving)
                    return;
            }

            MoveTowardsTarget(delta);

            return;
        }

        // -----------------------------------------
        // Movimiento manual
        // -----------------------------------------

        if (_isMovingToCell)
        {
            MoveTowardsTarget(delta);
        }
    }

    // ============================================================
    // MOVIMIENTO POR CÓDIGO
    // ============================================================

    private void StartNextCodeMovement()
    {
        // No quedan movimientos
        if (_moveQueue.Count == 0)
        {
            FinishCodeMovement();
            return;
        }

        Vector2I direction = _moveQueue.Dequeue();

        Vector2 dir = new Vector2(direction.X, direction.Y);

        // Guardar dirección
        SetDirection(dir);

        Vector2 movement = dir * CELL_SIZE;

        // -----------------------------------------
        // Comprobar colisión
        // -----------------------------------------

        KinematicCollision2D collision =
            MoveAndCollide(
                movement,
                testOnly: true
            );

        if (collision != null)
        {
            GD.Print("Movimiento bloqueado por una colisión.");

            _moveQueue.Clear();

            FinishCodeMovement();

            return;
        }

        // -----------------------------------------
        // Preparar movimiento
        // -----------------------------------------

        _targetPosition = Position + movement;

        _isMovingToCell = true;
        Moving = true;

        PlayWalkAnimation(dir);
    }

    private void FinishCodeMovement()
    {
        CodeMoving = false;
        Moving = false;
        _isMovingToCell = false;

        Velocity = Vector2.Zero;

        PlayIdleAnimation();
    }

    // ============================================================
    // MOVIMIENTO MANUAL
    // ============================================================

    private void TryMoveToCell(Vector2 direction)
    {
        if (IsWritingCode)
            return;

        if (CodeMoving)
            return;

        if (_isMovingToCell)
            return;

        // Guardar dirección
        SetDirection(direction);

        // -----------------------------------------
        // Comprobar colisión
        // -----------------------------------------

        Vector2 movement = direction * CELL_SIZE;

        KinematicCollision2D collision =
            MoveAndCollide(
                movement,
                testOnly: true
            );

        if (collision != null)
        {
            // No mover y mantener idle
            PlayIdleAnimation();
            return;
        }

        // -----------------------------------------
        // Preparar movimiento
        // -----------------------------------------

        _targetPosition = Position + movement;

        _isMovingToCell = true;
        Moving = true;

        PlayWalkAnimation(direction);
    }

    // ============================================================
    // EJECUTAR MOVIMIENTO
    // ============================================================

    private void MoveTowardsTarget(double delta)
    {
        float movementSpeed = Speed * (float)delta;

        Position = Position.MoveToward(
            _targetPosition,
            movementSpeed
        );

        // Llegamos al destino
        if (Position.DistanceTo(_targetPosition) <= 0.01f)
        {
            Position = _targetPosition;

            _isMovingToCell = false;
            Moving = false;

            // -----------------------------------------
            // Si hay más movimientos de código,
            // NO cambiar a idle.
            // -----------------------------------------

            if (CodeMoving && _moveQueue.Count > 0)
            {
                return;
            }

            // Terminó el movimiento
            PlayIdleAnimation();
        }
    }

    // ============================================================
    // DIRECCIÓN
    // ============================================================

    private void SetDirection(Vector2 direction)
    {
        if (direction == Vector2.Up)
            lastDirection = Direction.Up;

        else if (direction == Vector2.Down)
            lastDirection = Direction.Down;

        else if (direction == Vector2.Left)
            lastDirection = Direction.Left;

        else if (direction == Vector2.Right)
            lastDirection = Direction.Right;
    }

    // ============================================================
    // ANIMACIONES
    // ============================================================

    private void PlayWalkAnimation(Vector2 direction)
    {
        string animation = direction switch
        {
            var d when d == Vector2.Up => "walk_up",
            var d when d == Vector2.Down => "walk_down",
            var d when d == Vector2.Left => "walk_left",
            var d when d == Vector2.Right => "walk_right",
            _ => "walk_down"
        };

        PlayAnimation(animation);
    }

    private void PlayIdleAnimation()
    {
        string animation = lastDirection switch
        {
            Direction.Up => "idle_up",
            Direction.Down => "idle_down",
            Direction.Left => "idle_left",
            Direction.Right => "idle_right",
            _ => "idle_down"
        };

        PlayAnimation(animation);
    }

    private void PlayAnimation(string animationName)
    {
        if (AnimationPlayer == null)
            return;

        if (!AnimationPlayer.HasAnimation(animationName))
            return;

        // MUY IMPORTANTE:
        // No reiniciar la animación si ya está reproduciéndose.
        if (_currentAnimation == animationName &&
            AnimationPlayer.IsPlaying())
        {
            return;
        }

        _currentAnimation = animationName;

        AnimationPlayer.Play(animationName);
    }

    // ============================================================
    // GRID
    // ============================================================

    private Vector2 SnapToGrid(Vector2 position)
    {
        return new Vector2(
            Mathf.Round(position.X / CELL_SIZE) * CELL_SIZE,
            Mathf.Round(position.Y / CELL_SIZE) * CELL_SIZE
        );
    }

    // ============================================================
    // API PARA EL CÓDIGO
    // ============================================================

    public async Task MoverCeldas(Vector2I direccion, int cantidad)
    {
        if (CodeMoving)
            return;

        if (cantidad <= 0)
            return;

        // -----------------------------------------
        // Agregar movimientos a la cola
        // -----------------------------------------

        for (int i = 0; i < cantidad; i++)
        {
            _moveQueue.Enqueue(direccion);
        }

        CodeMoving = true;

        // -----------------------------------------
        // Esperar hasta terminar
        // -----------------------------------------

        while (CodeMoving)
        {
            await ToSignal(
                GetTree(),
                SceneTree.SignalName.PhysicsFrame
            );
        }
    }

    // ============================================================
    // ESTADO
    // ============================================================

    public bool IsBusy()
    {
        return CodeMoving || _isMovingToCell;
    }

    [Export]
    public Label DialogoText { get; set; }

    [Export]
    public Panel MsjPanel { get; set; }

    private int dialogoId = 0;

    public async Task MostrarDialogo(string texto, float segundos = 3f)
    {
        // Evita que un diálogo viejo oculte uno nuevo
        dialogoId++;
        int idActual = dialogoId;

        DialogoText.Text = texto;
        MsjPanel.Visible = true;

        await ToSignal(
            GetTree().CreateTimer(segundos),
            SceneTreeTimer.SignalName.Timeout
        );

        if (idActual == dialogoId)
        {
            MsjPanel.Visible = false;
        }
    }
}
