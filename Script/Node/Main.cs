using Godot;

public partial class Main : Node
{
    private const int Port = 7777;
    private const int MaxClients = 4;

    [Export]
    public Control UI { get; set; }

    [Export]
    public PackedScene PlayerScene { get; set; }

    [Export]
    public Node2D PlayersContainer { get; set; }

    [Export]
    public Godot.Collections.Array<Vector2> SpawnPoints { get; set; } = new()
    {
        new Vector2(160, 160),
        new Vector2(320, 160),
        new Vector2(160, 320),
        new Vector2(320, 320)
    };

    private ENetMultiplayerPeer _peer;


    // ============================================================
    // READY
    // ============================================================

    public override void _Ready()
    {
        GD.Print("================================");
        GD.Print("MAIN READY");
        GD.Print($"Main Path: {GetPath()}");
        GD.Print("================================");

        // Si no asignaste PlayersContainer desde el Inspector,
        // lo creamos automáticamente.
        if (PlayersContainer == null)
        {
            PlayersContainer = new Node2D
            {
                Name = "Players"
            };

            AddChild(PlayersContainer);

            GD.Print(
                "PlayersContainer creado automáticamente."
            );
        }

        // Mostrar los SpawnPoints reales que tiene Godot.
        if (SpawnPoints == null || SpawnPoints.Count == 0)
        {
            GD.PushWarning(
                "SpawnPoints está vacío. " +
                "Los jugadores aparecerán en (0,0)."
            );
        }
        else
        {
            GD.Print(
                $"SpawnPoints encontrados: {SpawnPoints.Count}"
            );

            for (int i = 0; i < SpawnPoints.Count; i++)
            {
                GD.Print(
                    $"SpawnPoint {i}: {SpawnPoints[i]}"
                );
            }
        }
    }


    // ============================================================
    // HOST
    // ============================================================

    public void HostGame()
    {
        if (_peer != null)
        {
            GD.Print(
                "Ya existe una conexión."
            );

            return;
        }

        _peer = new ENetMultiplayerPeer();

        Error error = _peer.CreateServer(
            Port,
            MaxClients
        );

        if (error != Error.Ok)
        {
            GD.PushError(
                $"CreateServer falló: {error}"
            );

            _peer = null;

            return;
        }

        Multiplayer.MultiplayerPeer = _peer;

        // Eventos del servidor.
        Multiplayer.PeerConnected += OnPeerConnected;
        Multiplayer.PeerDisconnected += OnPeerDisconnected;

        GD.Print("================================");
        GD.Print("HOST INICIADO");
        GD.Print($"ID: {Multiplayer.GetUniqueId()}");
        GD.Print($"Puerto: {Port}");
        GD.Print("================================");

        // El servidor también es un jugador.
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
            GD.Print(
                "Ya existe una conexión."
            );

            return;
        }

        _peer = new ENetMultiplayerPeer();

        Error error = _peer.CreateClient(
            address,
            Port
        );

        if (error != Error.Ok)
        {
            GD.PushError(
                $"CreateClient falló: {error}"
            );

            _peer = null;

            return;
        }

        Multiplayer.MultiplayerPeer = _peer;

        // Eventos del cliente.
        Multiplayer.ConnectedToServer +=
            OnConnectedToServer;

        Multiplayer.ConnectionFailed +=
            OnConnectionFailed;

        Multiplayer.ServerDisconnected +=
            OnServerDisconnected;

        GD.Print("================================");
        GD.Print("JOIN");
        GD.Print($"Servidor: {address}:{Port}");
        GD.Print("================================");
    }


    // ============================================================
    // PEER CONNECTED
    // ============================================================

    private void OnPeerConnected(long id)
    {
        GD.Print("================================");
        GD.Print($"PEER CONECTADO: {id}");
        GD.Print(
            $"¿Soy servidor?: {Multiplayer.IsServer()}"
        );
        GD.Print("================================");

        // Solamente el servidor administra los jugadores.
        if (!Multiplayer.IsServer())
            return;


        // --------------------------------------------------------
        // 1. Crear el jugador nuevo
        // --------------------------------------------------------

        SpawnPlayer(id);


        // --------------------------------------------------------
        // 2. Enviar los jugadores existentes
        //    a todos los clientes.
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

            // El jugador nuevo ya fue enviado arriba.
            if (existingPeerId == id)
                continue;

            GD.Print(
                $"Enviando Player existente " +
                $"{existingPeerId} " +
                $"a los clientes."
            );

            Rpc(
                nameof(SpawnPlayerRpc),
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

        // IMPORTANTE:
        // Esto sí es un RPC.
        Rpc(
            nameof(RemovePlayerRpc),
            id
        );
    }


    // ============================================================
    // CONNECTED TO SERVER
    // ============================================================

    private void OnConnectedToServer()
    {
        GD.Print("================================");
        GD.Print("CONECTADO AL SERVIDOR");
        GD.Print(
            $"Mi ID: {Multiplayer.GetUniqueId()}"
        );
        GD.Print(
            $"Main Path: {GetPath()}"
        );
        GD.Print("================================");
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
    // SPAWN PLAYER
    // ============================================================

    private void SpawnPlayer(long peerId)
    {
        Vector2 position =
            GetSpawnPoint(peerId);

        GD.Print("================================");
        GD.Print(
            $"Servidor: enviando RPC de spawn"
        );
        GD.Print(
            $"Peer: {peerId}"
        );
        GD.Print(
            $"Posición: {position}"
        );
        GD.Print("================================");

        // IMPORTANTE:
        //
        // NO hacemos:
        //
        // SpawnPlayerRpc(peerId, position);
        //
        // porque eso sería una llamada local.
        //
        // Rpc() envía la llamada a los demás peers
        // y como CallLocal = true también la ejecuta
        // en el servidor.

        Rpc(
            nameof(SpawnPlayerRpc),
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
        Vector2 position)
    {
        GD.Print("================================");
        GD.Print("SpawnPlayerRpc RECIBIDO");
        GD.Print(
            $"Peer: {peerId}"
        );
        GD.Print(
            $"Posición: {position}"
        );
        GD.Print(
            $"Mi ID: {Multiplayer.GetUniqueId()}"
        );
        GD.Print(
            $"Main Path: {GetPath()}"
        );
        GD.Print("================================");


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
        // Verificar PlayerScene
        // --------------------------------------------------------

        if (PlayerScene == null)
        {
            GD.PushError(
                "PlayerScene NO está asignado."
            );

            return;
        }


        // --------------------------------------------------------
        // Crear Player
        // --------------------------------------------------------

        Player player =
            PlayerScene.Instantiate<Player>();


        // --------------------------------------------------------
        // Nombre
        // --------------------------------------------------------

        player.Name =
            peerId.ToString();


        // --------------------------------------------------------
        // Posición
        // --------------------------------------------------------

        player.Position =
            position;


        // --------------------------------------------------------
        // Authority
        //
        // IMPORTANTE:
        // Se establece ANTES de AddChild().
        // --------------------------------------------------------

        player.SetMultiplayerAuthority(
            (int)peerId
        );


        // --------------------------------------------------------
        // Grupo
        // --------------------------------------------------------

        player.AddToGroup(
            "player"
        );


        // --------------------------------------------------------
        // Agregar al árbol
        // --------------------------------------------------------

        PlayersContainer.AddChild(
            player
        );


        // --------------------------------------------------------
        // Debug
        // --------------------------------------------------------

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
        long peerId)
    {
        GD.Print(
            $"RemovePlayerRpc → {peerId}"
        );

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
        long peerId)
    {
        if (SpawnPoints == null ||
            SpawnPoints.Count == 0)
        {
            GD.PushWarning(
                "No hay SpawnPoints. " +
                "Usando Vector2.Zero."
            );

            return Vector2.Zero;
        }

        int index =
            (int)((peerId - 1) %
            SpawnPoints.Count);

        Vector2 position =
            SpawnPoints[index];

        GD.Print(
            $"SpawnPoint para peer {peerId}: " +
            $"{position}"
        );

        return position;
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