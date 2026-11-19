using Unity.Services.Multiplayer;

public static class SharedSession
{
    public static ISession CurrentSession;
    public static bool IsJoiningAsClient;
    public static string PendingRoomCode;
}