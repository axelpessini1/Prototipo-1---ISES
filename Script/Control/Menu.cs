using Godot;

public partial class Menu : Control
{
    private const int Port = 7777;
    private const int MaxClients = 4;

    [Export] public VBoxContainer Home { get; set; }

    [Export] public VBoxContainer Join { get; set; }

    [Export] public TextEdit TextIP { get; set; }


    // ============================================================
    // READY
    // ============================================================

    public override void _Ready()
    {
        if (Home != null)
            Home.Visible = true;

        if (Join != null)
            Join.Visible = false;
    }


    // ============================================================
    // MOSTRAR MENU JOIN
    // ============================================================

    public void _on_join_pressed()
    {
        if (Home != null)
            Home.Visible = false;

        if (Join != null)
            Join.Visible = true;
    }


    // ============================================================
    // CREAR SERVIDOR
    // ============================================================

    public void CreateServer()
    {
        GD.Print("Creando servidor...");

        ENetMultiplayerPeer peer =
            new ENetMultiplayerPeer();

        Error error =
            peer.CreateServer(
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
        GD.Print(
            $"Peer ID: {Multiplayer.GetUniqueId()}"
        );
        GD.Print("================================");

        GetTree().ChangeSceneToFile(
            "Scenes/2D/Maps/map.tscn"
        );
    }


    // ============================================================
    // UNIRSE A SERVIDOR
    // ============================================================

    public void JoinServer()
    {
        if (TextIP == null)
        {
            GD.PushError(
                "TextIP no está asignado."
            );

            return;
        }

        // Obtener IP escrita por el usuario.
        string address =
            TextIP.Text.Trim();

        // Verificar que no esté vacío.
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


        // Asignar la conexión a Godot.
        Multiplayer.MultiplayerPeer = peer;


        // Cuando la conexión tenga éxito.
        Multiplayer.ConnectedToServer +=
            OnConnectedToServer;


        // Cuando falle.
        Multiplayer.ConnectionFailed +=
            OnConnectionFailed;


        // Cuando el servidor se desconecte.
        Multiplayer.ServerDisconnected +=
            OnServerDisconnected;


        GD.Print(
            "Conexión iniciada. Esperando servidor..."
        );
    }


    // ============================================================
    // CONECTADO AL SERVIDOR
    // ============================================================

    private void OnConnectedToServer()
    {
        GD.Print("================================");
        GD.Print("CONECTADO AL SERVIDOR");
        GD.Print(
            $"Mi ID: {Multiplayer.GetUniqueId()}"
        );
        GD.Print("================================");


        // Ahora podemos entrar al mapa.
        GetTree().ChangeSceneToFile(
            "Scenes/2D/Maps/map.tscn"
        );
    }

    // ============================================================
    // CONEXIÓN FALLIDA
    // ============================================================

    private void OnConnectionFailed()
    {
        GD.Print("================================");
        GD.Print("ERROR: NO SE PUDO CONECTAR");
        GD.Print("================================");


        // Liberar la conexión.
        Multiplayer.MultiplayerPeer = null;


        // Volver al menú principal.
        if (Home != null)
            Home.Visible = true;

        if (Join != null)
            Join.Visible = false;
    }


    // ============================================================
    // SERVIDOR DESCONECTADO
    // ============================================================

    private void OnServerDisconnected()
    {
        GD.Print("================================");
        GD.Print("SERVIDOR DESCONECTADO");
        GD.Print("================================");


        Multiplayer.MultiplayerPeer = null;


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
            // Ignorar IPv6.
            if (!address.Contains("."))
                continue;

            // Ignorar localhost.
            if (address.StartsWith("127."))
                continue;

            // Ignorar algunas interfaces virtuales comunes.
            if (address.StartsWith("169.254."))
                continue;

            return address;
        }

        return "127.0.0.1";
    }


    // ============================================================
    // BOTÓN JUGAR
    // ============================================================

    public void _on_play_pressed()
    {
        GD.Print(
            "Botón JUGAR presionado."
        );

        CreateServer();
    }


    // ============================================================
    // VOLVER
    // ============================================================

    public void _on_exit_pressed()
    {
        if (Home != null)
            Home.Visible = true;

        if (Join != null)
            Join.Visible = false;
    }


    // ============================================================
    // BOTÓN UNIRSE AL SERVIDOR
    // ============================================================

    public void _on_join_server_pressed()
    {
        GD.Print(
            "Botón UNIRSE presionado."
        );

        JoinServer();
    }
}