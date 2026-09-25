using Godot;

public partial class Main : Node
{
    // ============================================================
    // CONFIGURACIÓN
    // ============================================================

    [Export]
    public PackedScene PlayerScene { get; set; }

    [Export]
    public Node2D PlayersContainer { get; set; }

    [Export]
    public MultiplayerSpawner PlayerSpawner { get; set; }

    [Export]
    public Godot.Collections.Array<Vector2> SpawnPoints { get; set; }

    // ============================================================
    // ESTADO
    // ============================================================

    // Evita ejecutar dos veces la lógica de volver al menú.
    private bool _returningToMenu = false;

    // Indica si conectamos las señales correctamente.
    private bool _signalsConnected = false;

    // ============================================================
    // READY
    // ============================================================

    public override void _Ready()
    {
        GD.Print("================================");
        GD.Print("MAIN INICIADO");
        GD.Print($"Mi Peer ID: {Multiplayer.GetUniqueId()}");
        GD.Print($"¿Soy servidor?: {Multiplayer.IsServer()}");
        GD.Print("================================");

        // ========================================================
        // CONECTAR SEÑALES
        // ========================================================

        Multiplayer.PeerConnected += OnPeerConnected;
        Multiplayer.PeerDisconnected += OnPeerDisconnected;
        Multiplayer.ServerDisconnected += OnServerDisconnected;

        _signalsConnected = true;

        // ========================================================
        // CREAR AL HOST
        // ========================================================

        if (Multiplayer.IsServer())
        {
            GD.Print("MAIN: soy el servidor.");

            SpawnPlayer(
                Multiplayer.GetUniqueId()
            );
        }
        else
        {
            GD.Print("MAIN: soy un cliente.");
        }
    }

    // ============================================================
    // PEER CONECTADO
    // ============================================================

    private void OnPeerConnected(long peerId)
    {
        GD.Print(
            $"MAIN: se conectó el Peer {peerId}"
        );

        // Solo el servidor administra los jugadores.
        if (!Multiplayer.IsServer())
            return;

        // Crear al jugador nuevo.
        SpawnPlayer(peerId);

        // ========================================================
        // ENVIAR LOS JUGADORES EXISTENTES AL NUEVO CLIENTE
        // ========================================================

        if (PlayersContainer == null)
        {
            GD.PushError(
                "MAIN: PlayersContainer no está asignado."
            );

            return;
        }

        foreach (Node child in PlayersContainer.GetChildren())
        {
            if (!long.TryParse(
                    child.Name,
                    out long existingPeerId))
            {
                continue;
            }

            // No mandarnos a nosotros mismos.
            if (existingPeerId == peerId)
                continue;

            if (child is not Node2D playerNode)
                continue;

            RpcId(
                peerId,
                nameof(SpawnPlayerRpc),
                existingPeerId,
                playerNode.Position
            );
        }
    }

    // ============================================================
    // PEER DESCONECTADO
    // ============================================================

    private void OnPeerDisconnected(long peerId)
    {
        GD.Print(
            $"MAIN: se desconectó el Peer {peerId}"
        );

        // Si este Main está cerrándose, no hacer nada.
        if (_returningToMenu)
            return;

        // Solo el servidor elimina jugadores.
        if (!Multiplayer.IsServer())
            return;

        Rpc(
            nameof(RemovePlayerRpc),
            peerId
        );
    }

    // ============================================================
    // HOST DESCONECTADO
    // ============================================================

    private void OnServerDisconnected()
    {
        // Evitar ejecutar esto más de una vez.
        if (_returningToMenu)
            return;

        _returningToMenu = true;

        GD.Print("================================");
        GD.Print("EL HOST SE DESCONECTÓ");
        GD.Print("VOLVIENDO AL MENÚ...");
        GD.Print("================================");

        // IMPORTANTE:
        //
        // NO hacemos:
        //
        // Multiplayer.PeerConnected -= ...
        //
        // aquí.
        //
        // _ExitTree() será el encargado de limpiar
        // las señales.

        // Cerramos el peer.
        if (Multiplayer.HasMultiplayerPeer())
        {
            Multiplayer.MultiplayerPeer = null;
        }

        // Volver al menú.
        Error error = GetTree().ChangeSceneToFile(
            "res://Scenes/Control/menu.tscn"
        );

        if (error != Error.Ok)
        {
            GD.PushError(
                $"No se pudo volver al menú: {error}"
            );
        }
    }

    // ============================================================
    // CREAR PLAYER
    // ============================================================

    private void SpawnPlayer(long peerId)
    {
        // Solo el servidor crea jugadores.
        if (!Multiplayer.IsServer())
            return;

        if (PlayersContainer == null)
        {
            GD.PushError(
                "MAIN: PlayersContainer no está asignado."
            );

            return;
        }

        if (PlayerScene == null)
        {
            GD.PushError(
                "MAIN: PlayerScene no está asignado."
            );

            return;
        }

        Vector2 position = GetSpawnPoint(peerId);

        GD.Print(
            $"MAIN: creando Player {peerId} " +
            $"en {position}"
        );

        // IMPORTANTE:
        // RPC y no llamada directa.
        Rpc(
            nameof(SpawnPlayerRpc),
            peerId,
            position
        );
    }

    // ============================================================
    // RPC CREAR PLAYER
    // ============================================================

    [Rpc(
        MultiplayerApi.RpcMode.Authority,
        CallLocal = true,
        TransferMode = MultiplayerPeer.TransferModeEnum.Reliable
    )]
    private void SpawnPlayerRpc(
        long peerId,
        Vector2 position
    )
    {
        if (PlayersContainer == null)
        {
            GD.PushError(
                "MAIN: PlayersContainer no está asignado."
            );

            return;
        }

        if (PlayerScene == null)
        {
            GD.PushError(
                "MAIN: PlayerScene no está asignado."
            );

            return;
        }

        // ========================================================
        // EVITAR DUPLICADOS
        // ========================================================

        if (PlayersContainer.HasNode(
                peerId.ToString()))
        {
            return;
        }

        // ========================================================
        // INSTANCIAR PLAYER
        // ========================================================

        Player player =
            PlayerScene.Instantiate<Player>();

        player.Name = peerId.ToString();

        player.Position = position;

        // La autoridad del Player es el peer correspondiente.
        player.SetMultiplayerAuthority(
            (int)peerId
        );

        player.AddToGroup("player");

        PlayersContainer.AddChild(player);

        GD.Print(
            $"MAIN: Player {peerId} creado."
        );
    }

    // ============================================================
    // RPC ELIMINAR PLAYER
    // ============================================================

    [Rpc(
        MultiplayerApi.RpcMode.Authority,
        CallLocal = true,
        TransferMode = MultiplayerPeer.TransferModeEnum.Reliable
    )]
    private void RemovePlayerRpc(long peerId)
    {
        if (PlayersContainer == null)
            return;

        Node player =
            PlayersContainer.GetNodeOrNull(
                peerId.ToString()
            );

        if (player == null)
            return;

        GD.Print(
            $"MAIN: eliminando Player {peerId}"
        );

        player.QueueFree();
    }

    // ============================================================
    // OBTENER SPAWN
    // ============================================================

    private Vector2 GetSpawnPoint(long peerId)
    {
        if (SpawnPoints == null ||
            SpawnPoints.Count == 0)
        {
            return Vector2.Zero;
        }

        int index =
            (int)((peerId - 1) % SpawnPoints.Count);

        return SpawnPoints[index];
    }

    // ============================================================
    // EXIT TREE
    // ============================================================

    public override void _ExitTree()
    {
        // ========================================================
        // IMPORTANTE
        // ========================================================
        //
        // Este es EL ÚNICO lugar donde desconectamos
        // las señales.
        //
        // Ya no las desconectamos dentro de
        // OnServerDisconnected().
        //
        // ========================================================

        if (!_signalsConnected)
            return;

        _signalsConnected = false;

        if (Multiplayer != null)
        {
            Multiplayer.PeerConnected -= OnPeerConnected;
            Multiplayer.PeerDisconnected -= OnPeerDisconnected;
            Multiplayer.ServerDisconnected -= OnServerDisconnected;
        }

        GD.Print(
            "MAIN: señales desconectadas correctamente."
        );
    }
}