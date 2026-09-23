using Godot;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

public class PythonExecutor
{
    private string rutaPython;

    public PythonExecutor()
    {
        rutaPython = ProjectSettings.GlobalizePath(
            "res://Python/ejecutar.py"
        );
    }

    public async Task<PythonResult> EjecutarAsync(string codigo)
    {
        ProcessStartInfo psi = new ProcessStartInfo
        {
            FileName = "python",
            Arguments = $"\"{rutaPython}\"",

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

            await proceso.StandardInput.WriteAsync(codigo);
            proceso.StandardInput.Close();

            string salida =
                await proceso.StandardOutput.ReadToEndAsync();

            string errores =
                await proceso.StandardError.ReadToEndAsync();

            await proceso.WaitForExitAsync();

            if (!string.IsNullOrEmpty(errores))
            {
                return new PythonResult
                {
                    Ok = false,
                    Error = errores
                };
            }

            Variant json = Json.ParseString(salida);

            if (json.VariantType != Variant.Type.Dictionary)
            {
                return new PythonResult
                {
                    Ok = false,
                    Error = "Python devolvió un JSON inválido."
                };
            }

            var datos = json.AsGodotDictionary();

            return new PythonResult
            {
                Ok = datos["ok"].AsBool(),
                Output = datos["output"].AsString(),
                Error = datos["error"].AsString(),
                Variables = datos["variables"].AsGodotDictionary(),
                Commands = datos["commands"].AsGodotArray()
            };
        }
        catch (Exception e)
        {
            return new PythonResult
            {
                Ok = false,
                Error = e.Message
            };
        }
    }
}