using Godot;
using System.Threading.Tasks;

public partial class Code : Control
{
	[Export]
	public TextEdit CodeEditor { get; set; }

	[Export]
	public RichTextLabel Output { get; set; }

	[Export]
	public GameCommandExecutor CommandExecutor { get; set; }

	private PythonExecutor pythonExecutor;

	public override void _Ready()
	{
		pythonExecutor = new PythonExecutor();
	}

	private async void _on_ejecutar_button_pressed()
	{
		string codigo = CodeEditor.Text;

		PythonResult resultado =
			await pythonExecutor.EjecutarAsync(codigo);

		if (resultado.Ok)
		{
			Output.Text = resultado.Output;

			GD.Print("Código ejecutado correctamente.");

			GD.Print("Cantidad de comandos: " + resultado.Commands.Count);

			foreach (Variant comandoVariant in resultado.Commands)
			{
				GD.Print("Comando recibido: " + comandoVariant);
			}

			if (CommandExecutor == null)
			{
				GD.PrintErr("ERROR: CommandExecutor no está asignado.");
				return;
			}

			await CommandExecutor.EjecutarComandos(
				resultado.Commands
			);
		}
	}
}