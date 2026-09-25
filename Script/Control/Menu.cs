using Godot;

public partial class Menu : Control
{
    private const int Port = 7777;
    private const int MaxClients = 4;

    [Export]
    public VBoxContainer Home { get; set; }

    [Export]
    public VBoxContainer Join { get; set; }

    [Export]
    public TextEdit TextIP { get; set; }

    // ============================================================
    // ESTADO DE LAS SEÑALES
    // ============================================================

    private bool connectedToServerSignalConnected = false;
    private bool connectionFailedSignalConnected = false;

    // Evita cambiar de escena más de una vez.
    private bool changingScene = false;

    public override void _Ready()
    {
        if (Home != null)
            Home.Visible = true;

        if (Join != null)
            Join.Visible = false;

        // Por seguridad, limpiamos cualquier peer anterior.
        if (Multiplayer.HasMultiplayerPeer())
        {
            Multiplayer.MultiplayerPeer = null;
        }
    }

    // ============================================================
    // CONECTAR SEÑALES
    // ============================================================

    private void ConnectMultiplayerSignals()
    {
        // Evitamos conectar dos veces la misma señal.

        if (!connectedToServerSignalConnected)
        {
            Multiplayer.ConnectedToServer += OnConnectedToServer;
            connectedToServerSignalConnected = true;
        }

        if (!connectionFailedSignalConnected)
        {
            Multiplayer.ConnectionFailed += OnConnectionFailed;
            connectionFailedSignalConnected = true;
        }
    }

    // ============================================================
    // DESCONECTAR SEÑALES
    // ============================================================

    private void DisconnectMultiplayerSignals()
    {
        if (connectedToServerSignalConnected)
        {
            Multiplayer.ConnectedToServer -= OnConnectedToServer;
            connectedToServerSignalConnected = false;
        }

        if (connectionFailedSignalConnected)
        {
            Multiplayer.ConnectionFailed -= OnConnectionFailed;
            connectionFailedSignalConnected = false;
        }
    }

    // ============================================================
    // BOTÓN: JUGAR / CREAR SERVIDOR
    // ============================================================

    public void _on_play_pressed()
    {
        CreateServer();
    }

    // ============================================================
    // CREAR SERVIDOR
    // ============================================================

    public void CreateServer()
    {
        if (changingScene)
            return;

        GD.Print("================================");
        GD.Print("CREANDO SERVIDOR...");
        GD.Print("================================");

        ENetMultiplayerPeer peer = new ENetMultiplayerPeer();

        Error error = peer.CreateServer(
            Port,
            MaxClients
        );

        if (error != Error.Ok)
        {
            GD.PushError(
                $"No se pudo crear el servidor: {error}"
            );

            return;
        }

        Multiplayer.MultiplayerPeer = peer;

        string localIp = GetLocalIPv4();

        GD.Print("================================");
        GD.Print("SERVIDOR CREADO");
        GD.Print($"IP: {localIp}");
        GD.Print($"Puerto: {Port}");
        GD.Print($"Jugadores máximos: {MaxClients}");
        GD.Print($"Peer ID: {Multiplayer.GetUniqueId()}");
        GD.Print("================================");

        ChangeToMap();
    }

    // ============================================================
    // BOTÓN: UNIRSE
    // ============================================================

    public void _on_join_pressed()
    {
        if (Home != null)
            Home.Visible = false;

        if (Join != null)
            Join.Visible = true;
    }

    // ============================================================
    // CONECTARSE AL SERVIDOR
    // ============================================================

    public void _on_join_server_pressed()
    {
        JoinServer();
    }

    public void JoinServer()
    {
        if (changingScene)
            return;

        if (TextIP == null)
        {
            GD.PushError(
                "TextIP no está asignado."
            );

            return;
        }

        string address = TextIP.Text.Trim();

        if (string.IsNullOrWhiteSpace(address))
        {
            GD.PushError(
                "No se ingresó una dirección IP."
            );

            return;
        }

        GD.Print("================================");
        GD.Print("INTENTANDO CONECTARSE");
        GD.Print($"Servidor: {address}");
        GD.Print($"Puerto: {Port}");
        GD.Print("================================");

        // Si ya existe un peer, lo eliminamos antes
        // de crear una nueva conexión.
        if (Multiplayer.HasMultiplayerPeer())
        {
            Multiplayer.MultiplayerPeer = null;
        }

        ENetMultiplayerPeer peer =
            new ENetMultiplayerPeer();

        Error error =
            peer.CreateClient(
                address,
                Port
            );

        if (error != Error.Ok)
        {
            GD.PushError(
                $"No se pudo iniciar la conexión: {error}"
            );

            return;
        }

        // IMPORTANTE:
        // Conectamos las señales ANTES de asignar el peer.
        ConnectMultiplayerSignals();

        Multiplayer.MultiplayerPeer = peer;

        GD.Print(
            "Conexión iniciada. Esperando servidor..."
        );
    }

    // ============================================================
    // CONECTADO AL SERVIDOR
    // ============================================================

    private void OnConnectedToServer()
    {
        if (changingScene)
            return;

        GD.Print("================================");
        GD.Print("CONECTADO AL SERVIDOR");
        GD.Print($"Mi ID: {Multiplayer.GetUniqueId()}");
        GD.Print("================================");

        ChangeToMap();
    }

    // ============================================================
    // CONEXIÓN FALLIDA
    // ============================================================

    private void OnConnectionFailed()
    {
        GD.Print("================================");
        GD.Print("ERROR: NO SE PUDO CONECTAR");
        GD.Print("================================");

        // Limpiamos el peer.
        Multiplayer.MultiplayerPeer = null;

        if (Home != null)
            Home.Visible = true;

        if (Join != null)
            Join.Visible = false;
    }

    // ============================================================
    // CAMBIAR AL MAPA
    // ============================================================

    private void ChangeToMap()
    {
        if (changingScene)
            return;

        changingScene = true;

        GD.Print("Cambiando al mapa...");

        Error error = GetTree().ChangeSceneToFile(
            "res://Scenes/2D/Maps/map.tscn"
        );

        if (error != Error.Ok)
        {
            GD.PushError(
                $"No se pudo cambiar al mapa: {error}"
            );

            changingScene = false;
        }
    }

    // ============================================================
    // BOTÓN VOLVER / SALIR
    // ============================================================

    public void _on_exit_pressed()
    {
        if (Home != null)
            Home.Visible = true;

        if (Join != null)
            Join.Visible = false;
    }

    // ============================================================
    // OBTENER IP LOCAL
    // ============================================================

    private string GetLocalIPv4()
    {
        foreach (string address in IP.GetLocalAddresses())
        {
            if (!address.Contains("."))
                continue;

            if (address.StartsWith("127."))
                continue;

            if (address.StartsWith("169.254."))
                continue;

            return address;
        }

        return "127.0.0.1";
    }

    // ============================================================
    // LIMPIEZA
    // ============================================================

    public override void _ExitTree()
    {
        DisconnectMultiplayerSignals();
    }
}