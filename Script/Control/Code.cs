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

    private void BuscarPlayer()
    {
        player = null;

        var jugadores = GetTree().GetNodesInGroup("player");

        GD.Print(
            $"CODE: Players encontrados: {jugadores.Count}"
        );

        foreach (Node node in jugadores)
        {
            if (node is Player candidato)
            {
                GD.Print(
                    $"CODE: Player {candidato.Name} | " +
                    $"autoridad = {candidato.IsMultiplayerAuthority()}"
                );

                if (candidato.IsMultiplayerAuthority())
                {
                    player = candidato;

                    GD.Print(
                        $"CODE: Player asignado: {player.Name}"
                    );

                    return;
                }
            }
        }

        GD.PrintErr(
            "CODE: no se encontró un Player con autoridad local."
        );
    }

    public override void _Ready()
    {
        pythonExecutor = new PythonExecutor();

        CallDeferred(nameof(BuscarPlayer));

    }

    private async void _on_ejecutar_button_pressed()
    {
        string codigo = CodeEditor.Text;

        PythonResult resultado =
            await pythonExecutor.EjecutarAsync(codigo);

        if (!resultado.Ok)
        {
            Output.Text = resultado.Error;
            return;
        }

        Output.Text = resultado.Output;

        GD.Print(
            "Código ejecutado correctamente."
        );

        GD.Print(
            "Cantidad de comandos: "
            + resultado.Commands.Count
        );

        foreach (Variant comandoVariant in resultado.Commands)
        {
            GD.Print(
                "Comando recibido: "
                + comandoVariant
            );
        }

        await EjecutarComandos(
            resultado.Commands
        );
    }

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
                GD.PrintErr("CODE: comando sin action.");
                continue;
            }

            string accion =
                comando["action"].AsString();

            switch (accion)
            {
                case "move":
                    {
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

    private Player ObtenerPlayer()
    {
        if (player != null &&
            GodotObject.IsInstanceValid(player))
        {
            return player;
        }

        var jugadores = GetTree().GetNodesInGroup("player");

        foreach (Node node in jugadores)
        {
            if (node is Player candidato &&
                candidato.IsMultiplayerAuthority())
            {
                player = candidato;

                GD.Print(
                    $"CODE: Player asignado: {player.Name}"
                );

                return player;
            }
        }

        return null;
    }

    private async Task EjecutarMovimiento(
        string direccion,
        int cantidad
    )
    {
        ObtenerPlayer();

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
                    $"Dirección desconocida: {direccion}"
                );

                return;
        }

        GD.Print(
            $"CODE: ejecutando {direccion} x{cantidad}"
        );

        await player.MoverCeldas(
            direccionVector,
            cantidad
        );
    }

    private async Task EjecutarDialogo(string texto)
    {
        Player jugador = ObtenerPlayer();

        if (jugador == null)
        {
            GD.PrintErr(
                "CODE: no se encontró el Player para mostrar el diálogo."
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

        // Esperamos para que los comandos Python
        // sigan ejecutándose en orden.
        await ToSignal(
            GetTree().CreateTimer(3f),
            SceneTreeTimer.SignalName.Timeout
        );
    }

    public void _on_text_editor_text_changed()
    {

    }

    private void _on_text_editor_focus_entered()
    {
        if (player != null)
        {
            player.IsWritingCode = true;
        }
    }

    private void _on_text_editor_focus_exited()
    {
        if (player != null)
        {
            player.IsWritingCode = false;
        }
    }
}