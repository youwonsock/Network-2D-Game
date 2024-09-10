@echo off

start /d .\bin\Debug\net8.0 PacketGenerator.exe

xcopy /y .\bin\Debug\net8.0\ClientPacketManager.cs ..\DummyClient\Packet\
xcopy /y .\bin\Debug\net8.0\GenPackets.cs ..\DummyClient\Packet\

xcopy /y .\bin\Debug\net8.0\ServerPacketManager.cs ..\Server\Packet\ 
xcopy /y .\bin\Debug\net8.0\GenPackets.cs ..\Server\Packet\