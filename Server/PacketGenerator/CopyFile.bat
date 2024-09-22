@echo off

xcopy /y .\bin\ClientPacketManager.cs "../../Common/protoc-28.2-win64/bin"
xcopy /y .\bin\ServerPacketManager.cs "../../Common/protoc-28.2-win64/bin"