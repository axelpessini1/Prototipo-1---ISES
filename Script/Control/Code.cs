using Godot;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

public partial class Code : Control
{
    [Export]
    public TextEdit CodeEditor { get; set; }

    [Export]
    public RichTextLabel Output { get; set; }

    private string RutaPython;

    public override void _Ready()
    {
        // Ruta del archivo Python dentro del proyecto
        RutaPython = ProjectSettings.GlobalizePath(
            "res://Python/ejecutar.py"
        );
    }

    private async void _on_ejecutar_button_pressed()
    {
        await EjecutarPython();
    }

    private async Task EjecutarPython()
    {
        string codigo = CodeEditor.Text;

        ProcessStartInfo psi = new ProcessStartInfo
        {
            FileName = "python",
            Arguments = $"\"{RutaPython}\"",

            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,

            UseShellExecute = false,
            CreateNoWindow = true
        };

        try
        {
            using Process proceso = new Process();

            proceso.StartInfo = psi;

            proceso.Start();

            // Enviar el código escrito por el jugador
            await proceso.StandardInput.WriteAsync(codigo);
            proceso.StandardInput.Close();

            // Leer la salida de Python
            string salida = await proceso.StandardOutput.ReadToEndAsync();

            // Leer errores del proceso
            string errores = await proceso.StandardError.ReadToEndAsync();

            await proceso.WaitForExitAsync();

            if (!string.IsNullOrEmpty(errores))
            {
                Output.Text = errores;
                return;
            }

            Output.Text = salida;
        }
        catch (Exception e)
        {
            Output.Text = "Error al ejecutar Python:\n" + e.Message;
        }
    }
}