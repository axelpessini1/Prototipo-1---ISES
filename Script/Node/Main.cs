using Godot;
using System.Collections.Generic;

public partial class Main : Node
{
	private const int Port = 7777;
	private const int MaxClients = 4;

	[Export] public Control UI;
	[Export] public PackedScene PlayerScene { get; set; }
	[Export] public Node2D PlayersContainer { get; set; }
	[Export] public MultiplayerSpawner PlayerSpawner { get; set; }

	[Export]

	public Godot.Collections.Array<Vector2> SpawnPoints { get; set; } = new()
	{
		new Vector2(160, 160),
		new Vector2(320, 160),
		new Vector2(160, 320),
		new Vector2(320, 320),
	};

	private ENetMultiplayerPeer _peer;

	public override void _Ready()
	{
		// Referencias por defecto si no se asignan en el inspector
		if (PlayersContainer == null)
		{
			PlayersContainer = new Node2D { Name = "Players" };
			AddChild(PlayersContainer);
		}

		// ✅ CRUCIAL: Asignar la función de spawn personalizada al Spawner
		if (PlayerSpawner != null)
		{
			PlayerSpawner.SpawnFunction = new Callable(this, MethodName.CustomSpawnFunction);
		}
		else
		{
			GD.PushError("Main: PlayerSpawner no asignado en el inspector.");
		}
	}

	// Esta función se ejecutará en TODOS los peers cuando se llame a Spawn()
	private Node CustomSpawnFunction(Variant data)
	{
		var dict = data.AsGodotDictionary();

		long peerId = (long)dict["peer_id"];
		Vector2 position = dict["position"].AsVector2();

		if (PlayerScene == null)
		{
			GD.PushError("Main: PlayerScene no asignada.");
			return null;
		}

		var player = PlayerScene.Instantiate<Player>();

		player.Name = peerId.ToString();
		player.Position = position;

		// Autoridad del jugador
		player.SetMultiplayerAuthority((int)peerId);

		// Registrar jugador
		player.AddToGroup("player");

		GD.Print(
			$"Main: creando Player {player.Name} " +
			$"con autoridad {peerId}"
		);

		return player;
	}

	private void SpawnPlayerForPeer(long peerId)
	{
		if (PlayerSpawner == null) return;
		if (PlayersContainer.HasNode(peerId.ToString())) return; // Evitar duplicados

		// ✅ Preparar los datos que se enviarán a todos los peers
		var data = new Godot.Collections.Dictionary
		{
			["peer_id"] = peerId,
			["position"] = GetSpawnPoint(peerId)
		};

		// ✅ El Spawner se encarga de crear el nodo localmente y replicarlo
		PlayerSpawner.Spawn(data);

		GD.Print($"Spawning player for peer {peerId}");
	}

	// ============================================================
	// HOST / JOIN
	// ============================================================

	public void HostGame()
	{
		_peer = new ENetMultiplayerPeer();
		var err = _peer.CreateServer(Port, MaxClients);
		if (err != Error.Ok)
		{
			GD.PushError($"Main: CreateServer failed — {err}");
			return;
		}

		Multiplayer.MultiplayerPeer = _peer;
		ConnectSignals();

		// En el host, spawneamos nuestro propio jugador (peer id = 1)
		SpawnPlayerForPeer(1);

		GD.Print($"Main: hosting on port {Port}, my id = {Multiplayer.GetUniqueId()}");
	}

	public void JoinGame(string address = "127.0.0.1")
	{
		_peer = new ENetMultiplayerPeer();
		var err = _peer.CreateClient(address, Port);
		if (err != Error.Ok)
		{
			GD.PushError($"Main: CreateClient failed — {err}");
			return;
		}

		Multiplayer.MultiplayerPeer = _peer;
		ConnectSignals();

		GD.Print($"Main: connecting to {address}:{Port}");
	}

	private void ConnectSignals()
	{
		Multiplayer.PeerConnected += OnPeerConnected;
		Multiplayer.PeerDisconnected += OnPeerDisconnected;
		Multiplayer.ConnectedToServer += OnConnectedToServer;
		Multiplayer.ConnectionFailed += OnConnectionFailed;
		Multiplayer.ServerDisconnected += OnServerDisconnected;
	}

	private void OnPeerConnected(long id)
	{
		GD.Print($"Peer connected: {id}");

		// Solo el servidor spawnea jugadores para los que se conectan
		if (Multiplayer.IsServer())
			SpawnPlayerForPeer(id);
	}

	private void OnPeerDisconnected(long id)
	{
		GD.Print($"Peer disconnected: {id}");

		// Solo el servidor elimina el jugador del peer que se fue
		if (Multiplayer.IsServer())
		{
			var node = PlayersContainer.GetNodeOrNull(id.ToString());
			node?.QueueFree();
		}
	}

	private void OnConnectedToServer()
	{
		GD.Print($"Connected — my id = {Multiplayer.GetUniqueId()}");
	}

	private void OnConnectionFailed()
	{
		GD.PushError("Connection failed");
	}

	private void OnServerDisconnected()
	{
		GD.Print("Server disconnected");
	}

	// ============================================================
	// SPAWN
	// ============================================================


	private Vector2 GetSpawnPoint(long peerId)
	{
		int index = (int)(peerId - 1) % SpawnPoints.Count;
		return SpawnPoints[index];
	}

	// ============================================================
	// BOTONES (conectados desde el editor)
	// ============================================================

	public void _on_host_pressed()
	{
		HostGame();
		UI.Visible = false;
	}

	public void _on_join_pressed()
	{
		JoinGame();
		UI.Visible = false;
	}
}