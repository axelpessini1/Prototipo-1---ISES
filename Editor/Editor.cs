using Godot;
using System.Threading.Tasks;

public partial class Editor : Window
{
	private Player player;

	public override void _Ready()
	{
		player = GetNodeOrNull<Player>("../Player");

		if (player == null)
		{
			GD.PrintErr("EDITOR: no se encontró Player.");
			return;
		}

		GD.Print(
			$"EDITOR: Player encontrado: {player.Name}"
		);

		MoverDerecha();
	}

	private async void MoverDerecha()
	{
		if (player == null)
		{
			GD.PrintErr("EDITOR: Player es null.");
			return;
		}

		GD.Print(
			"EDITOR: moviendo Player a la derecha."
		);

		await player.MoverCeldas(
			Vector2I.Right,
			5
		);

		GD.Print(
			"EDITOR: movimiento terminado."
		);
	}

	public override void _Process(double delta)
	{
	}

	private async Task EjecutarComandos(
	Godot.Collections.Array comandos
)
	{
		foreach (Variant comandoVariant in comandos)
		{
			var comando =
				comandoVariant.AsGodotDictionary();

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
			$"EDITOR: ejecutando {direccion} x{cantidad}"
		);

		await player.MoverCeldas(
			direccionVector,
			cantidad
		);
	}
}