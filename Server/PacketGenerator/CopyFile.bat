@echo off
copy .\bin\Debug\net8.0\ClientPacketManager.cs ..\DummyClient\Packet\
copy .\bin\Debug\net8.0\GenPackets.cs ..\DummyClient\Packet\

copy .\bin\Debug\net8.0\ServerPacketManager.cs ..\Server\Packet\
copy .\bin\Debug\net8.0\GenPackets.cs ..\Server\Packet\