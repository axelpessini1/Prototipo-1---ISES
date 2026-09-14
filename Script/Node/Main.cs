using Godot;
using System;

public partial class Main : Node
{
	// Called when the node enters the scene tree for the first time.
	
	[Export]
	public PackedScene CodeScene { get; set; }
	private Window ventana;

    public override void _Ready()
    {
		ventana = new Window();

        ventana.Title = "Ventana externa";
        ventana.Size = new Vector2I(800, 600);
        ventana.Position = new Vector2I(100, 100);
        ventana.Unresizable = false;

        Node editor = CodeScene.Instantiate();

        ventana.AddChild(editor);

        // IMPORTANTE
        AddChild(ventana);

        ventana.Show();
    }

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
