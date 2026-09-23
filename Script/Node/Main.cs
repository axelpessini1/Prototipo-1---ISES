using Godot;

public partial class Main : Node
{
    private const int Port = 7777;
    private const int MaxClients = 4;

    [Export] public Control UI;
    [Export] public PackedScene PlayerScene;
    [Export] public Node2D PlayersContainer;

    [Export]
    public Godot.Collections.Array<Vector2> SpawnPoints { get; set; } = new()
    {
        new Vector2(160, 160),
        new Vector2(320, 160),
        new Vector2(160, 320),
        new Vector2(320, 320)
    };

    private ENetMultiplayerPeer _peer;

    public override void _Ready()
    {
        // Crear Players si no fue asignado desde el Inspector
        if (PlayersContainer == null)
        {
            PlayersContainer = new Node2D
            {
                Name = "Players"
            };

            AddChild(PlayersContainer);
        }

        GD.Print("Main listo.");
    }

    // ============================================================
    // HOST
    // ============================================================

    public void HostGame()
    {
        if (_peer != null)
        {
            GD.Print("Ya existe una conexión.");
            return;
        }

        _peer = new ENetMultiplayerPeer();

        Error err = _peer.CreateServer(
            Port,
            MaxClients
        );

        if (err != Error.Ok)
        {
            GD.PushError(
                $"CreateServer failed: {err}"
            );

            _peer = null;
            return;
        }

        Multiplayer.MultiplayerPeer = _peer;

        // Señales del servidor
        Multiplayer.PeerConnected += OnPeerConnected;
        Multiplayer.PeerDisconnected += OnPeerDisconnected;

        GD.Print(
            $"================================"
        );

        GD.Print(
            $"HOST iniciado"
        );

        GD.Print(
            $"ID: {Multiplayer.GetUniqueId()}"
        );

        GD.Print(
            $"Puerto: {Port}"
        );

        GD.Print(
            $"================================"
        );

        // El servidor es peer 1
        SpawnPlayer(1);
    }

    // ============================================================
    // JOIN
    // ============================================================

    public void JoinGame(
        string address = "192.168.88.29"
    )
    {
        if (_peer != null)
        {
            GD.Print("Ya existe una conexión.");
            return;
        }

        _peer = new ENetMultiplayerPeer();

        Error err = _peer.CreateClient(
            address,
            Port
        );

        if (err != Error.Ok)
        {
            GD.PushError(
                $"CreateClient failed: {err}"
            );

            _peer = null;
            return;
        }

        Multiplayer.MultiplayerPeer = _peer;

        // Señales del cliente
        Multiplayer.ConnectedToServer +=
            OnConnectedToServer;

        Multiplayer.ConnectionFailed +=
            OnConnectionFailed;

        Multiplayer.ServerDisconnected +=
            OnServerDisconnected;

        GD.Print(
            $"================================"
        );

        GD.Print(
            $"JOIN"
        );

        GD.Print(
            $"Servidor: {address}:{Port}"
        );

        GD.Print(
            $"================================"
        );
    }

    // ============================================================
    // PEER CONNECTED
    // ============================================================

    private void OnPeerConnected(long id)
    {
        GD.Print(
            $"PEER CONECTADO: {id}"
        );

        GD.Print(
            $"¿Soy servidor?: {Multiplayer.IsServer()}"
        );

        // Solamente el servidor controla los jugadores
        if (!Multiplayer.IsServer())
            return;

        // Crear jugador del nuevo cliente
        SpawnPlayer(id);

        // --------------------------------------------------------
        // Enviar al nuevo cliente los jugadores que ya existían
        // --------------------------------------------------------

        foreach (Node child in PlayersContainer.GetChildren())
        {
            if (!long.TryParse(
                child.Name,
                out long existingPeerId))
            {
                continue;
            }

            Player existingPlayer =
                child as Player;

            if (existingPlayer == null)
                continue;

            // No necesitamos volver a crear el jugador
            // que acabamos de crear.
            if (existingPeerId == id)
                continue;

            GD.Print(
                $"Enviando Player existente " +
                $"{existingPeerId} al nuevo peer {id}"
            );

            SpawnPlayerRpc(
                existingPeerId,
                existingPlayer.Position
            );
        }
    }

    // ============================================================
    // PEER DISCONNECTED
    // ============================================================

    private void OnPeerDisconnected(long id)
    {
        GD.Print(
            $"PEER DESCONECTADO: {id}"
        );

        if (!Multiplayer.IsServer())
            return;

        RemovePlayerRpc(id);
    }

    // ============================================================
    // CONNECTED TO SERVER
    // ============================================================

    private void OnConnectedToServer()
    {
        GD.Print(
            $"================================"
        );

        GD.Print(
            $"CONECTADO AL SERVIDOR"
        );

        GD.Print(
            $"Mi ID: {Multiplayer.GetUniqueId()}"
        );

        GD.Print(
            $"================================"
        );
    }

    // ============================================================
    // CONNECTION FAILED
    // ============================================================

    private void OnConnectionFailed()
    {
        GD.PushError(
            "No se pudo conectar al servidor."
        );

        _peer = null;
    }

    // ============================================================
    // SERVER DISCONNECTED
    // ============================================================

    private void OnServerDisconnected()
    {
        GD.Print(
            "Servidor desconectado."
        );

        _peer = null;
    }

    // ============================================================
    // SPAWN
    // ============================================================

    private void SpawnPlayer(long peerId)
    {
        Vector2 position =
            GetSpawnPoint(peerId);

        GD.Print(
            $"Servidor: solicitando spawn " +
            $"para peer {peerId}"
        );

        SpawnPlayerRpc(
            peerId,
            position
        );
    }

    // ============================================================
    // SPAWN RPC
    // ============================================================

    [Rpc(
        MultiplayerApi.RpcMode.Authority,
        CallLocal = true
    )]
    private void SpawnPlayerRpc(
        long peerId,
        Vector2 position
    )
    {
        GD.Print(
            $"SpawnPlayerRpc → " +
            $"peer {peerId} " +
            $"posición {position}"
        );

        // --------------------------------------------------------
        // Evitar duplicados
        // --------------------------------------------------------

        if (PlayersContainer.HasNode(
            peerId.ToString()))
        {
            GD.Print(
                $"Player {peerId} ya existe."
            );

            return;
        }

        // --------------------------------------------------------
        // Comprobar escena
        // --------------------------------------------------------

        if (PlayerScene == null)
        {
            GD.PushError(
                "PlayerScene no está asignado."
            );

            return;
        }

        // --------------------------------------------------------
        // Crear Player
        // --------------------------------------------------------

        Player player =
            PlayerScene.Instantiate<Player>();

        player.Name =
            peerId.ToString();

        player.Position =
            position;

        // --------------------------------------------------------
        // Autoridad
        // --------------------------------------------------------

        player.SetMultiplayerAuthority(
            (int)peerId
        );

        player.AddToGroup(
            "player"
        );

        // --------------------------------------------------------
        // Agregar al árbol
        // --------------------------------------------------------

        PlayersContainer.AddChild(
            player
        );

        GD.Print(
            $"Player creado: {peerId}"
        );

        GD.Print(
            $"Authority: {peerId}"
        );

        GD.Print(
            $"Mi ID: {Multiplayer.GetUniqueId()}"
        );

        GD.Print(
            $"¿Soy autoridad?: " +
            $"{player.IsMultiplayerAuthority()}"
        );
    }

    // ============================================================
    // REMOVE PLAYER RPC
    // ============================================================

    [Rpc(
        MultiplayerApi.RpcMode.Authority,
        CallLocal = true
    )]
    private void RemovePlayerRpc(
        long peerId
    )
    {
        Node player =
            PlayersContainer.GetNodeOrNull(
                peerId.ToString()
            );

        if (player == null)
        {
            GD.Print(
                $"Player {peerId} no existe."
            );

            return;
        }

        player.QueueFree();

        GD.Print(
            $"Player {peerId} eliminado."
        );
    }

    // ============================================================
    // SPAWN POINT
    // ============================================================

    private Vector2 GetSpawnPoint(
        long peerId
    )
    {
        if (SpawnPoints == null ||
            SpawnPoints.Count == 0)
        {
            return Vector2.Zero;
        }

        int index =
            (int)((peerId - 1) %
            SpawnPoints.Count);

        return SpawnPoints[index];
    }

    // ============================================================
    // HOST BUTTON
    // ============================================================

    public void _on_host_pressed()
    {
        GD.Print(
            "HOST presionado."
        );

        HostGame();

        if (UI != null)
            UI.Visible = false;
    }

    // ============================================================
    // JOIN BUTTON
    // ============================================================

    public void _on_join_pressed()
    {
        GD.Print(
            "JOIN presionado."
        );

        JoinGame();

        if (UI != null)
            UI.Visible = false;
    }
}
