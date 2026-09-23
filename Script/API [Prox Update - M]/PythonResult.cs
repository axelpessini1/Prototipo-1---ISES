using Godot;
using System.Collections.Generic;

public class PythonResult
{
    public bool Ok { get; set; }

    public string Output { get; set; } = "";

    public string Error { get; set; }

    public Godot.Collections.Dictionary Variables { get; set; }

    public Godot.Collections.Array Commands { get; set; }
}