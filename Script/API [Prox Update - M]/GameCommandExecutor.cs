//ESTO YA NO SE UTILIZA, QUEDO OBSOLETO.

using Godot;
using System.Threading.Tasks;

public partial class GameCommandExecutor : Node
{
    public async Task EjecutarComandos(
        Godot.Collections.Array comandos
    )
    {
        foreach (Variant comandoVariant in comandos)
        {
            var comando = comandoVariant.AsGodotDictionary();

            if (!comando.ContainsKey("action"))
                continue;

            string accion = comando["action"].AsString();

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

                case "collect":
                {
                    EjecutarRecoleccion();
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
                    GD.PrintErr(
                        $"Acción desconocida: {accion}"
                    );
                    break;
            }
        }
    }

    private async Task EjecutarDialogo(string texto)
    {
        Player player = ObtenerPlayerLocal();

        if (player == null)
        {
            GD.PrintErr(
                "GameCommandExecutor: no se encontró el Player local."
            );

            return;
        }

        GD.Print($"Ejecutando diálogo: {texto}");

        await player.MostrarDialogo(texto);
    }

    private Player ObtenerPlayerLocal()
    {
        foreach (Node node in GetTree().GetNodesInGroup("player"))
        {
            if (node is Player player)
            {
                if (player.IsMultiplayerAuthority())
                {
                    return player;
                }
            }
        }

        return null;
    }

    private async Task EjecutarMovimiento(
        string direccion,
        int cantidad
    )
    {
        for (int i = 0; i < cantidad; i++)
        {
            GD.Print(
                $"Mover una celda hacia {direccion}"
            );

            await ToSignal(
                GetTree().CreateTimer(0.2f),
                SceneTreeTimer.SignalName.Timeout
            );
        }
    }

    private void EjecutarRecoleccion()
    {
        GD.Print("Ejecutar recolección");
    }
}