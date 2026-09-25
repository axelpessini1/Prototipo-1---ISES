using Godot;
using System.Threading.Tasks;

public partial class Editor : Window
{
    private Player player;

    public override void _Ready()
    {
        // Editor es hijo directo de Player
        player = GetParent<Player>();

        if (player == null)
        {
            GD.PrintErr("EDITOR: el padre no es un Player.");
            return;
        }

        GD.Print(
            $"EDITOR: Player encontrado: {player.Name} | " +
            $"Autoridad: {player.IsMultiplayerAuthority()}"
        );

        // Cada Player tiene su propio Editor,
        // pero solamente mostramos el del jugador local.
        if (!player.IsMultiplayerAuthority())
        {
            Hide();
            return;
        }

        Show();

        // NO mover automáticamente al iniciar
        // MoverDerecha();
    }

    private async void MoverDerecha()
    {
        if (player == null)
        {
            GD.PrintErr("EDITOR: Player es null.");
            return;
        }

        GD.Print("EDITOR: moviendo Player a la derecha.");

        await player.MoverCeldas(
            Vector2I.Right,
            5
        );

        GD.Print("EDITOR: movimiento terminado.");
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
                continue;

            string accion =
                comando["action"].AsString();

            if (accion == "move")
            {
                string direccion =
                    comando["direction"].AsString();

                int cantidad =
                    comando["amount"].AsInt32();

                await EjecutarMovimiento(
                    direccion,
                    cantidad
                );
            }
        }
    }

    private async Task EjecutarMovimiento(
        string direccion,
        int cantidad
    )
    {
        if (player == null)
        {
            GD.PrintErr("EDITOR: no hay Player asociado.");
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
                    $"EDITOR: dirección desconocida: {direccion}"
                );
                return;
        }

        if (cantidad <= 0)
        {
            GD.PrintErr(
                "EDITOR: la cantidad debe ser mayor que 0."
            );
            return;
        }

        GD.Print(
            $"EDITOR: ejecutando {direccion} x{cantidad}"
        );

        await player.MoverCeldas(
            direccionVector,
            cantidad
        );
    }
}