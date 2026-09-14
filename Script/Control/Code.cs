using Godot;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

public partial class Code : Control
{
	[Export]
	public TextEdit CodeEditor { get; set; }

	[Export]
	public RichTextLabel ErrorPanel { get; set; }

	private Player player;

	private readonly HashSet<string> comandos = new()
	{
		"moveLeft",
		"moveRight",
		"moveUp",
		"moveDown"
	};

	public void SetPlayer(Player player)
	{
		this.player = player;
	}

	public async void _on_ejecutar_pressed()
	{
		ErrorPanel.Clear();

		if (player == null)
		{
			MostrarError(0, "No se encontró el Player.");
			return;
		}

		string codigo = CodeEditor.Text;

		List<string> instrucciones = ValidarCodigo(codigo);

		if (instrucciones == null)
		{
			GD.PrintErr("El código contiene errores. No se ejecutó nada.");
			return;
		}

		// Ejecutar UNA por UNA y esperar a que termine cada una
		foreach (string instruccion in instrucciones)
		{
			await EjecutarInstruccion(instruccion);
		}
	}

	//=========================================================
	// VALIDAR TODO EL CÓDIGO
	// =========================================================

	private List<string> ValidarCodigo(string codigo)
	{
		List<string> instruccionesValidas = new();

		string[] lineas = codigo.Split('\n');

		bool hayErrores = false;

		for (int i = 0; i < lineas.Length; i++)
		{
			string linea = lineas[i].Trim();

			if (string.IsNullOrWhiteSpace(linea))
				continue;

			// Permitir varias instrucciones por línea
			string[] partes = linea.Split(';');

			for (int j = 0; j < partes.Length; j++)
			{
				string instruccion = partes[j].Trim();

				if (string.IsNullOrWhiteSpace(instruccion))
					continue;

				// ==========================================
				// COMPROBAR ;
				// ==========================================

				bool tienePuntoYComa =
					j < partes.Length - 1;

				if (!tienePuntoYComa)
				{
					MostrarError(
						i + 1,
						"Falta ';' al final de la instrucción."
					);

					hayErrores = true;

					continue;
				}

				// ==========================================
				// VALIDAR SINTAXIS
				// ==========================================

				Match match = Regex.Match(
					instruccion,
					@"^([a-zA-Z_][a-zA-Z0-9_]*)\s*\((.*)\)$"
				);

				if (!match.Success)
				{
					MostrarError(
						i + 1,
						"Sintaxis incorrecta."
					);

					hayErrores = true;

					continue;
				}

				string nombreComando =
					match.Groups[1].Value;

				string argumentos =
					match.Groups[2].Value.Trim();

				// ==========================================
				// COMPROBAR COMANDO
				// ==========================================

				if (!comandos.Contains(nombreComando))
				{
					MostrarError(
						i + 1,
						$"Comando desconocido: '{nombreComando}'."
					);

					hayErrores = true;

					continue;
				}

				// ==========================================
				// VALIDAR ARGUMENTOS
				// ==========================================

				if (!ValidarArgumentos(
					nombreComando,
					argumentos,
					i + 1))
				{
					hayErrores = true;
					continue;
				}

				// La instrucción es válida
				instruccionesValidas.Add(
					instruccion + ";"
				);
			}
		}

		// ==========================================
		// SI EXISTE CUALQUIER ERROR
		// ==========================================

		if (hayErrores)
		{
			return null;
		}

		return instruccionesValidas;
	}

	// =========================================================
	// VALIDAR ARGUMENTOS
	// =========================================================

	private bool ValidarArgumentos(
		string comando,
		string argumentos,
		int numeroLinea)
	{
		// Todos nuestros movimientos necesitan
		// exactamente un número.

		if (!int.TryParse(
			argumentos,
			out int cantidad))
		{
			MostrarError(
				numeroLinea,
				$"{comando}() necesita un número."
			);

			return false;
		}

		if (cantidad <= 0)
		{
			MostrarError(
				numeroLinea,
				"La cantidad debe ser mayor que 0."
			);

			return false;
		}

		return true;
	}

	// =========================================================
	// EJECUTAR INSTRUCCIÓN
	// =========================================================

	private async Task EjecutarInstruccion(string linea)
	{
		string instruccion = linea
			.Substring(0, linea.Length - 1)
			.Trim();

		Match match = Regex.Match(
			instruccion,
			@"^([a-zA-Z_][a-zA-Z0-9_]*)\s*\((.*)\)$"
		);

		string nombreComando = match.Groups[1].Value;
		string argumentos = match.Groups[2].Value.Trim();

		int cantidad = int.Parse(argumentos);

		switch (nombreComando)
		{
			case "moveLeft":
				await player.MoverCeldas(
					new Vector2I(-1, 0),
					cantidad
				);
				break;

			case "moveRight":
				await player.MoverCeldas(
					new Vector2I(1, 0),
					cantidad
				);
				break;

			case "moveUp":
				await player.MoverCeldas(
					new Vector2I(0, -1),
					cantidad
				);
				break;

			case "moveDown":
				await player.MoverCeldas(
					new Vector2I(0, 1),
					cantidad
				);
				break;
		}
	}
	// =========================================================
	// MOVIMIENTO DEL JUGADOR
	// =========================================================

	private void MoverJugador(Vector2 movimiento)
	{
		if (player == null)
		{
			GD.PrintErr(
				"No se encontró el Player."
			);

			return;
		}

		KinematicCollision2D collision =
			player.MoveAndCollide(movimiento);

		if (collision != null)
		{
			GD.Print(
				"El jugador chocó contra un obstáculo."
			);
		}
		else
		{
			GD.Print(
				$"Jugador movido: {movimiento}"
			);
		}
	}

	// =========================================================
	// ERROR
	// =========================================================

	private void MostrarError(
		int linea,
		string mensaje)
	{
		if (linea > 0)
		{
			ErrorPanel.AppendText(
				$"❌ Línea {linea}: {mensaje}\n"
			);

			GD.PrintErr(
				$"Error en línea {linea}: {mensaje}"
			);
		}
		else
		{
			ErrorPanel.AppendText(
				$"❌ {mensaje}\n"
			);

			GD.PrintErr(mensaje);
		}
	}
}