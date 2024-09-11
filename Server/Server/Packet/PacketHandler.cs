using Server;
using ServerCore;

/// <summary>
/// 패킷 수신 시 처리 로직을 담당하는 클래스
/// </summary>
class PacketHandler
{
    public static void C_LeaveGameHandler(PacketSession session, IPacket packet)
    {
        ClientSession clientSession = session as ClientSession;

        if(clientSession.Room == null)  // 방에 입장하지 않은 경우
            return;

        // jobQueue를 통해 실행 되므로 null 레퍼런스 예외가 발생할 수 있어
        // clientSession.Room을 room이라는 지역 변수에 저장하고 이를 통해 Leave() 메서드를 호출
        // clientSession.Room은 변수가 null이 되는거지 실제 객체는 살아있으므로 이가 가능함
        GameRoom room = clientSession.Room;
        room.Push(
            () => room.Leave(clientSession)
            ); // 행동을 잡큐에 액션으로 전달
    }

    public static void C_MoveHandler(PacketSession session, IPacket packet)
    {
        ClientSession clientSession = session as ClientSession;
        C_Move movePacket = packet as C_Move;

        if (clientSession.Room == null)  // 방에 입장하지 않은 경우
            return;

        // jobQueue를 통해 실행 되므로 null 레퍼런스 예외가 발생할 수 있어
        // clientSession.Room을 room이라는 지역 변수에 저장하고 이를 통해 Move() 메서드를 호출
        // clientSession.Room은 변수가 null이 되는거지 실제 객체는 살아있으므로 이가 가능함
        GameRoom room = clientSession.Room;
        room.Push(
            () => room.Move(clientSession, movePacket)
            ); // 행동을 잡큐에 액션으로 전달
    }
}