
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using System.Xml;

namespace PacketGenerator
{
    /// <summary>
    /// 패킷을 정의한 XML 파일을 읽어서 C# 클래스 파일로 만들어주는 프로그램
    /// </summary>
    class PacketGenerator
    {
        static string genPackets;
        static ushort packetId;
        static string packetEnums;

        static string clientRegister;
        static string serverRegister;

        static void Main(string[] args)
        {
            string pdlPath = "../../../PDL.xml";

            // XmlReaderSettings : XmlReader의 동작을 설정하는데 사용
            XmlReaderSettings settings = new XmlReaderSettings()
            {
                IgnoreWhitespace = true,
                IgnoreComments = true
            };

            // 인자가 있으면 첫번째 인자로 경로 설정
            if(args.Length >= 1)
                pdlPath = args[0];

            // using 구문 사용 시 범위를 벗어나면 자동으로 Dispose() 호출
            using (XmlReader reader = XmlReader.Create(pdlPath, settings))
            {
                reader.MoveToContent(); // 바로 내부 요소로 이동

                while(reader.Read())
                {
                    if (reader.Depth == 1 && reader.NodeType == XmlNodeType.Element)
                        ParsePacket(reader);
                }

                string fileText = string.Format(PacketFormat.fileFormat, packetEnums,genPackets);
                File.WriteAllText("GenPackets.cs", fileText);

                string clientManagerText = string.Format(PacketFormat.managerFormat, clientRegister);
                File.WriteAllText("ClientPacketManager.cs", clientManagerText);

                string serverManagerText = string.Format(PacketFormat.managerFormat, serverRegister);
                File.WriteAllText("ServerPacketManager.cs", serverManagerText);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="reader"></param>
        public static void ParsePacket(XmlReader reader)
        {
            if (reader.NodeType == XmlNodeType.EndElement)
                return;

            if (reader.Name.ToLower() != "packet")  // packet 노드가 아니면 리턴
            {
                Console.WriteLine("Invalid packet node");
                return;
            }

            string packetName = reader["name"]; // packet의 name 속성 값 가져오기
            if (string.IsNullOrEmpty(packetName))
            {
                Console.WriteLine("Packet without name");
                return;
            }

            Tuple<string, string, string> t = ParseMembers(reader);
            genPackets += string.Format(PacketFormat.packetFormat, packetName, t.Item1, t.Item2, t.Item3);
            packetEnums += string.Format(PacketFormat.packetEnumFormat, packetName, ++packetId) + Environment.NewLine + "\t";
            
            if(packetName.StartsWith("S_") || packetName.StartsWith("s_"))  // 서버로 보내는 패킷
                clientRegister += string.Format(PacketFormat.managerRegisterFormat, packetName) + Environment.NewLine;
            else    // 클라이언트로 보내는 패킷
                serverRegister += string.Format(PacketFormat.managerRegisterFormat, packetName) + Environment.NewLine;
        }

        /// <summary>
        /// 1. 맴버 변수들
        /// 2. 멤버 변수 Read
        /// 3. 멤버 변수 Write
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        public static Tuple<string, string, string> ParseMembers(XmlReader reader)
        {
            string memberCode = "";
            string readCode = "";
            string writeCode = "";

            int depth = reader.Depth + 1;   
            while(reader.Read())    // 자식 노드 파싱
            {
                if(reader.Depth != depth)   // 같은 depth로 돌아오면 종료
                    break;

                string memberName = reader["name"];
                if (string.IsNullOrEmpty(memberName))
                {
                    Console.WriteLine("Member without name");
                    return null;
                }

                // 다음 줄로 이동
                if (string.IsNullOrEmpty(memberCode) == false)
                    memberCode += Environment.NewLine;
                if (string.IsNullOrEmpty(readCode) == false)
                    readCode += Environment.NewLine;
                if (string.IsNullOrEmpty(writeCode) == false)
                    writeCode += Environment.NewLine;

                // 맴버 변수 타입 체크
                string memberType = reader.Name.ToLower();
                switch (memberType)
                {
                    case "byte":
                    case "sbyte":
                        memberCode += string.Format(PacketFormat.memberFormat, memberType, memberName);
                        readCode += string.Format(PacketFormat.readByteFormat, memberName, memberType);
                        writeCode += string.Format(PacketFormat.writeByteFormat, memberName, memberType);
                        break;
                    case "bool":
                    case "short":
                    case "ushort":
                    case "int":
                    case "long":
                    case "float":
                    case "double":
                        memberCode += string.Format(PacketFormat.memberFormat, memberType, memberName);
                        readCode += string.Format(PacketFormat.readFormat, memberName, ToMemberType(memberType), memberType);
                        writeCode += string.Format(PacketFormat.writeFormat, memberName, memberType);
                        break;
                    case "string":
                        memberCode += string.Format(PacketFormat.memberFormat, memberType, memberName);
                        readCode += string.Format(PacketFormat.readStringFormat, memberName);
                        writeCode += string.Format(PacketFormat.writeStringFormat, memberName);
                        break;
                    case "list":
                        // List는 내부에 또 다른 멤버가 있을 수 있으므로 추가 처리 필요
                        Tuple<string, string, string> t = ParseList(reader);
                        memberCode += t.Item1;
                        readCode += t.Item2;
                        writeCode += t.Item3;
                        break;
                    default:
                        Console.WriteLine("Invalid type");
                        break;
                }
            }

            // 들여쓰기 처리
            memberCode = memberCode.Replace("\n", "\n\t");
            readCode = readCode.Replace("\n", "\n\t\t");
            writeCode = writeCode.Replace("\n", "\n\t\t");

            return new Tuple<string, string, string>(memberCode, readCode, writeCode);
        }

        /// <summary>
        /// List 파싱 함수
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        public static Tuple<string, string, string> ParseList(XmlReader reader)
        {
            string listName = reader["name"];
            if (string.IsNullOrEmpty(listName))
            {
                Console.WriteLine("List without name");
                return null;
            }
            
            // List 내부의 멤버 변수 파싱
            Tuple<string, string, string> t = ParseMembers(reader);

            string memberCode = string.Format(PacketFormat.memberListFormat,
                FirstCharToUpper(listName),
                FirstCharToLower(listName),
                t.Item1,
                t.Item2,
                t.Item3);

            string readCode = string.Format(PacketFormat.readListFormat,
                FirstCharToUpper(listName),
                FirstCharToLower(listName));
        
            string writeCode = string.Format(PacketFormat.writeListFormat,
                FirstCharToUpper(listName),
                FirstCharToLower(listName));
        
            return new Tuple<string, string, string>(memberCode, readCode, writeCode);
        }

        /// <summary>
        /// 변수 형식에 따른 변환 함수
        /// </summary>
        /// <param name="memberType"></param>
        /// <returns></returns>
        public static string ToMemberType(string memberType)
        {
            switch (memberType)
            {
                case "bool":
                    return "ToBoolean";
                case "short":
                    return "ToInt16";
                case "ushort":
                    return "ToUInt16";
                case "int":
                    return "ToInt32";
                case "long":
                    return "ToInt64";
                case "float":
                    return "ToSingle";
                case "double":
                    return "ToDouble";
                default:
                    return string.Empty;
            }
        }

        public static string FirstCharToUpper(string str)
        {
            if (string.IsNullOrEmpty(str))
                return string.Empty;

            char[] a = str.ToCharArray();
            a[0] = char.ToUpper(a[0]);
            return new string(a);
        }

        public static string FirstCharToLower(string str)
        {
            if (string.IsNullOrEmpty(str))
                return string.Empty;

            char[] a = str.ToCharArray();
            a[0] = char.ToLower(a[0]);
            return new string(a);
        }
    }
}