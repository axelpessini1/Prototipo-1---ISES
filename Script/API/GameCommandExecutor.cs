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

            string accion = comando["action"].AsString();

            switch (accion)
            {
                case "move":
                    string direccion =
                        comando["direction"].AsString();

                    int cantidad =
                        comando["amount"].AsInt32();

                    await EjecutarMovimiento(
                        direccion,
                        cantidad
                    );
                    break;

                case "collect":
                    EjecutarRecoleccion();
                    break;
            }
        }
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