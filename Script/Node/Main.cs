using Godot;
using System;

public partial class Main : Node
{
	// Called when the node enters the scene tree for the first time.
	
	[Export]
	public PackedScene CodeScene { get; set; }
	private Window ventana;

    private Player GetPlayerReference()
    {
        Player player = GetNodeOrNull<Player>("../Player");

        if (player == null)
            player = GetNodeOrNull<Player>("/root/Map/Player");

        if (player == null)
            player = GetNodeOrNull<Player>("Player");

        return player;
    }

    public override void _Ready()
	{
		Player player = GetPlayerReference();

        if (player == null)
        {
            GD.PrintErr("No se encontró el Player en la escena actual.");
            return;
        }

		Window ventana = new Window();

		ventana.Title = "Editor de código";
		ventana.Size = new Vector2I(800, 600);
		ventana.Position = new Vector2I(100, 100);
		ventana.Unresizable = true;

		Code editor = CodeScene.Instantiate<Code>();

		// Le pasamos el Player de Main a Code
		//editor.SetPlayer(player);

		ventana.AddChild(editor);

		AddChild(ventana);

		ventana.Show();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
