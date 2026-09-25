using Godot;
using System.Threading.Tasks;

public partial class Code : Control
{
    [Export]
    public TextEdit CodeEditor { get; set; }

    [Export]
    public RichTextLabel Output { get; set; }

    private PythonExecutor pythonExecutor;
    private Player player;

    private bool buscandoPlayer = false;


    // ============================================================
    // READY
    // ============================================================

    public override void _Ready()
    {
        pythonExecutor = new PythonExecutor();

        // El Player puede todavía no existir porque
        // el servidor lo crea mediante RPC.
        CallDeferred(nameof(BuscarPlayer));
    }


    // ============================================================
    // BUSCAR PLAYER LOCAL
    // ============================================================

    private void BuscarPlayer()
    {
        if (buscandoPlayer)
            return;

        buscandoPlayer = true;

        player = null;

        var jugadores =
            GetTree().GetNodesInGroup("player");

        GD.Print(
            $"CODE: Players encontrados: {jugadores.Count}"
        );

        foreach (Node node in jugadores)
        {
            if (node is not Player candidato)
                continue;

            GD.Print(
                $"CODE: Player {candidato.Name} | " +
                $"autoridad = {candidato.IsMultiplayerAuthority()}"
            );

            // Queremos solamente el Player
            // controlado por este cliente.
            if (!candidato.IsMultiplayerAuthority())
                continue;

            player = candidato;

            GD.Print(
                $"CODE: Player local asignado: {player.Name}"
            );

            buscandoPlayer = false;

            return;
        }

        buscandoPlayer = false;

        // El Player todavía puede no existir.
        // Volvemos a intentar más adelante.
        GetTree()
            .CreateTimer(0.2f)
            .Timeout += BuscarPlayer;
    }


    // ============================================================
    // OBTENER PLAYER
    // ============================================================

    private Player ObtenerPlayer()
    {
        // Si ya tenemos un Player válido,
        // usamos ese mismo.
        if (player != null &&
            GodotObject.IsInstanceValid(player))
        {
            return player;
        }

        player = null;

        var jugadores =
            GetTree().GetNodesInGroup("player");

        foreach (Node node in jugadores)
        {
            if (node is not Player candidato)
                continue;

            if (!candidato.IsMultiplayerAuthority())
                continue;

            player = candidato;

            GD.Print(
                $"CODE: Player local asignado: {player.Name}"
            );

            return player;
        }

        return null;
    }


    // ============================================================
    // EJECUTAR CÓDIGO
    // ============================================================

    private async void _on_ejecutar_button_pressed()
    {
        if (CodeEditor == null)
        {
            GD.PrintErr(
                "CODE: CodeEditor no está asignado."
            );

            return;
        }

        if (Output == null)
        {
            GD.PrintErr(
                "CODE: Output no está asignado."
            );

            return;
        }

        string codigo = CodeEditor.Text;

        if (string.IsNullOrWhiteSpace(codigo))
        {
            Output.Text = "No se ingresó código.";

            return;
        }

        PythonResult resultado =
            await pythonExecutor.EjecutarAsync(codigo);

        if (!resultado.Ok)
        {
            Output.Text = resultado.Error;

            GD.PrintErr(
                $"CODE: error de Python: {resultado.Error}"
            );

            return;
        }

        Output.Text = resultado.Output;

        GD.Print(
            "Código ejecutado correctamente."
        );

        GD.Print(
            $"Cantidad de comandos: " +
            $"{resultado.Commands.Count}"
        );

        foreach (Variant comandoVariant in resultado.Commands)
        {
            GD.Print(
                $"Comando recibido: {comandoVariant}"
            );
        }

        await EjecutarComandos(
            resultado.Commands
        );
    }


    // ============================================================
    // EJECUTAR COMANDOS
    // ============================================================

    private async Task EjecutarComandos(
        Godot.Collections.Array comandos
    )
    {
        foreach (Variant comandoVariant in comandos)
        {
            Godot.Collections.Dictionary comando =
                comandoVariant.AsGodotDictionary();

            if (!comando.ContainsKey("action"))
            {
                GD.PrintErr(
                    "CODE: comando sin action."
                );

                continue;
            }

            string accion =
                comando["action"].AsString();

            switch (accion)
            {
                case "move":
                {
                    if (!comando.ContainsKey("direction") ||
                        !comando.ContainsKey("amount"))
                    {
                        GD.PrintErr(
                            "CODE: comando move incompleto."
                        );

                        continue;
                    }

                    string direccion =
                        comando["direction"].AsString();

                    int cantidad =
                        comando["amount"].AsInt32();

                    await EjecutarMovimiento(
                        direccion,
                        cantidad
                    );

                    break;
                }

                case "say":
                {
                    if (!comando.ContainsKey("text"))
                    {
                        GD.PrintErr(
                            "CODE: comando say sin text."
                        );

                        continue;
                    }

                    string texto =
                        comando["text"].AsString();

                    await EjecutarDialogo(texto);

                    break;
                }

                default:
                {
                    GD.PrintErr(
                        $"CODE: acción desconocida: {accion}"
                    );

                    break;
                }
            }
        }
    }


    // ============================================================
    // MOVIMIENTO
    // ============================================================

    private async Task EjecutarMovimiento(
        string direccion,
        int cantidad
    )
    {
        Player jugador =
            ObtenerPlayer();

        if (jugador == null)
        {
            GD.PrintErr(
                "CODE: no se encontró el Player local para mover."
            );

            return;
        }

        Vector2I direccionVector;

        switch (direccion)
        {
            case "right":
                direccionVector = Vector2I.Right;
                break;

            case "left":
                direccionVector = Vector2I.Left;
                break;

            case "up":
                direccionVector = Vector2I.Up;
                break;

            case "down":
                direccionVector = Vector2I.Down;
                break;

            default:
                GD.PrintErr(
                    $"CODE: dirección desconocida: {direccion}"
                );

                return;
        }

        if (cantidad <= 0)
        {
            GD.PrintErr(
                "CODE: la cantidad de movimiento debe ser mayor a 0."
            );

            return;
        }

        GD.Print(
            $"CODE: ejecutando {direccion} x{cantidad}"
        );

        await jugador.MoverCeldas(
            direccionVector,
            cantidad
        );
    }


    // ============================================================
    // DIÁLOGO
    // ============================================================

    private async Task EjecutarDialogo(
        string texto
    )
    {
        Player jugador =
            ObtenerPlayer();

        if (jugador == null)
        {
            GD.PrintErr(
                "CODE: no se encontró el Player local " +
                "para mostrar el diálogo."
            );

            return;
        }

        if (string.IsNullOrWhiteSpace(texto))
        {
            GD.PrintErr(
                "CODE: diálogo vacío."
            );

            return;
        }

        GD.Print(
            $"CODE: mostrando diálogo: {texto}"
        );

        jugador.Rpc(
            nameof(Player.RpcMostrarDialogo),
            texto,
            3f
        );

        await ToSignal(
            GetTree().CreateTimer(3f),
            SceneTreeTimer.SignalName.Timeout
        );
    }


    // ============================================================
    // TEXT EDIT
    // ============================================================

    public void _on_text_editor_text_changed()
    {
        // Actualmente no necesitamos hacer nada.
    }


    // ============================================================
    // EDITOR RECIBE FOCO
    // ============================================================

    private void _on_text_editor_focus_entered()
    {
        Player jugador =
            ObtenerPlayer();

        if (jugador == null)
            return;

        jugador.IsWritingCode = true;

        GD.Print(
            $"CODE: Player {jugador.Name} comenzó a escribir."
        );
    }


    // ============================================================
    // EDITOR PIERDE FOCO
    // ============================================================

    private void _on_text_editor_focus_exited()
    {
        Player jugador =
            ObtenerPlayer();

        if (jugador == null)
            return;

        jugador.IsWritingCode = false;

        GD.Print(
            $"CODE: Player {jugador.Name} dejó de escribir."
        );
    }
}
