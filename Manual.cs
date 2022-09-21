using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
//using System.Text.Encoding.CodePages;

namespace cchess_con
{
    internal class Manual
    {
        public Manual()
        {
            _info = new();
            _manualMove = new(new RootMove());
        }

        public Manual(string fileName) : this()
        {
            Read(fileName);
        }
        public Manual(Dictionary<string, string> info) : this()
        {
        }

        public void Read(string fileName)
        {
            ReadXQF(fileName);
            //ReadCM(fileName);
        }
        public void Write(string fileName)
        {
            WriteCM(fileName);
        }

        private void WriteCM(string fileName)
        {

        }

        private void ReadXQF(string fileName)
        {
            using(FileStream stream = File.OpenRead(fileName))
            {
                //文件标记'XQ'=$5158/版本/加密掩码/ProductId[4], 产品(厂商的产品号)
                const int PIECENUM = 32;
                byte[] Signature = new byte[3], Version = new byte[1], headKeyMask = new byte[1],
                    ProductId = new byte[4], headKeyOrA = new byte[1],
                    headKeyOrB = new byte[1], headKeyOrC = new byte[1], headKeyOrD = new byte[1],
                    headKeysSum = new byte[1], headKeyXY = new byte[1], headKeyXYf = new byte[1], headKeyXYt = new byte[1],
                    headQiziXY = new byte[PIECENUM],
                    PlayStepNo = new byte[2], headWhoPlay = new byte[1], headPlayResult = new byte[1], PlayNodes = new byte[4],
                    PTreePos = new byte[4], Reserved1 = new byte[4],
                    headCodeA_H = new byte[16], TitleA = new byte[64], TitleB = new byte[64],
                    Event = new byte[64], Date = new byte[16], Site = new byte[16], Red = new byte[16], Black = new byte[16],
                    Opening = new byte[64], Redtime = new byte[16], Blktime = new byte[16], Reservedh = new byte[32],
                    RMKWriter = new byte[16], Author = new byte[16]; //, Other[528]{}; 
                // 棋谱评论员/文件的作者
                // 32个棋子的原始位置
                // 加密的钥匙和/棋子布局位置钥匙/棋谱起点钥匙/棋谱终点钥匙
                // 用单字节坐标表示, 将字节变为十进制, 十位数为X(0-8)个位数为Y(0-9),
                // 棋盘的左下角为原点(0, 0). 32个棋子的位置从1到32依次为:
                // 红: 车马相士帅士相马车炮炮兵兵兵兵兵 (位置从右到左, 从下到上)
                // 黑: 车马象士将士象马车炮炮卒卒卒卒卒 (位置从右到左,
                // 该谁下 0-红先, 1-黑先/最终结果 0-未知, 1-红胜 2-黑胜, 3-和棋
                // 从下到上)PlayStepNo[2],
                // 对局类型(开,中,残等)

                stream.Read(Signature, 0, 2);
                stream.Read(Version, 0, 1);
                stream.Read(headKeyMask, 0, 1);
                stream.Read(ProductId, 0, 4); // = 8 bytes
                stream.Read(headKeyOrA, 0, 1);
                stream.Read(headKeyOrB, 0, 1);
                stream.Read(headKeyOrC, 0, 1);
                stream.Read(headKeyOrD, 0, 1);
                stream.Read(headKeysSum, 0, 1);
                stream.Read(headKeyXY, 0, 1);
                stream.Read(headKeyXYf, 0, 1);
                stream.Read(headKeyXYt, 0, 1); // = 16 bytes
                stream.Read(headQiziXY, 0, PIECENUM); // = 48 bytes
                stream.Read(PlayStepNo, 0, 2);
                stream.Read(headWhoPlay, 0, 1);
                stream.Read(headPlayResult, 0, 1);
                stream.Read(PlayNodes, 0, 4);
                stream.Read(PTreePos, 0, 4);
                stream.Read(Reserved1, 0, 4); // = 64 bytes
                stream.Read(headCodeA_H, 0, 16);
                stream.Read(TitleA, 0, 64);
                stream.Read(TitleB, 0, 64);
                stream.Read(Event, 0, 64);
                stream.Read(Date, 0, 16);
                stream.Read(Site, 0, 16);
                stream.Read(Red, 0, 16); // = 320 bytes
                stream.Read(Black, 0, 16);
                stream.Read(Opening, 0, 64);
                stream.Read(Redtime, 0, 16);
                stream.Read(Blktime, 0, 16);
                stream.Read(Reservedh, 0, 32);
                stream.Read(RMKWriter, 0, 16); // = 480 bytes
                stream.Read(Author, 0, 16); // = 496 bytes

                if(Signature[0] != 0x58 || Signature[1] != 0x51)
                    throw new Exception("文件标记不符。");
                if((headKeysSum[0] + headKeyXY[0] + headKeyXYf[0] + headKeyXYt[0]) % 256 != 0)
                    throw new Exception("检查密码校验和不对，不等于0。");
                if(Version[0] > 18)
                    throw new Exception("这是一个高版本的XQF文件，您需要更高版本的XQStudio来读取这个文件。");

                byte[] KeyXY = new byte[1], KeyXYf = new byte[1], KeyXYt = new byte[1],
                    F32Keys = new byte[PIECENUM], head_QiziXY = new byte[PIECENUM];
                uint KeyRMKSize = 0;

                headQiziXY.CopyTo(head_QiziXY, 0);
                if(Version[0] <= 10)
                {   // version <= 10 兼容1.0以前的版本
                    KeyRMKSize = 0;
                    KeyXYf[0] = KeyXYt[0] = 0;
                }
                else
                {
                    byte __calkey(byte bKey, byte cKey)
                    {
                        // % 256; // 保持为<256
                        return (byte)((((((bKey * bKey) * 3 + 9) * 3 + 8) * 2 + 1) * 3 + 8) * cKey);
                    }

                    KeyXY[0] = __calkey(headKeyXY[0], headKeyXY[0]);
                    KeyXYf[0] = __calkey(headKeyXYf[0], KeyXY[0]);
                    KeyXYt[0] = __calkey(headKeyXYt[0], KeyXYf[0]);
                    KeyRMKSize = (uint)(((headKeysSum[0] * 256 + headKeyXY[0]) % 32000) + 767); // % 65536
                    if(Version[0] >= 12)
                    {   // 棋子位置循环移动
                        byte[] Qixy = new byte[PIECENUM];
                        headQiziXY.CopyTo(Qixy, 0);
                        for(int i = 0;i != PIECENUM;++i)
                            head_QiziXY[(i + KeyXY[0] + 1) % PIECENUM] = Qixy[i];
                    }
                    for(int i = 0;i != PIECENUM;++i)
                        head_QiziXY[i] -= KeyXY[0]; // 保持为8位无符号整数，<256
                }
                int[] KeyBytes = new int[]{
                    (headKeysSum[0] & headKeyMask[0]) | headKeyOrA[0],
                    (headKeyXY[0] & headKeyMask[0]) | headKeyOrB[0],
                    (headKeyXYf[0] & headKeyMask[0]) | headKeyOrC[0],
                    (headKeyXYt[0] & headKeyMask[0]) | headKeyOrD[0] };
                string copyright = "[(C) Copyright Mr. Dong Shiwei.]";
                for(int i = 0;i != PIECENUM;++i)
                    F32Keys[i] = (byte)(copyright[i] & KeyBytes[i % 4]); // ord(c)

                // 取得棋子字符串
                StringBuilder pieceChars = new(new string('_', 90));
                string pieChars = "RNBAKABNRCCPPPPPrnbakabnrccppppp"; // QiziXY设定的棋子顺序
                for(int i = 0;i != PIECENUM;++i)
                {
                    int xy = head_QiziXY[i];
                    if(xy <= 89)
                        // 用单字节坐标表示, 将字节变为十进制,
                        // 十位数为X(0-8),个位数为Y(0-9),棋盘的左下角为原点(0, 0)
                        pieceChars[xy % 10 * 9 + xy / 10] = pieChars[i];
                }

                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                Encoding codec = Encoding.GetEncoding("gb18030"); // "gb2312"
                string[] result = { "未知", "红胜", "黑胜", "和棋" };
                string[] typestr = { "全局", "开局", "中局", "残局" };
                SetInfoValue("VERSION", string.Format($"{Version[0]}"));
                SetInfoValue("RESULT", result[headPlayResult[0]]);
                SetInfoValue("TYPE", typestr[headCodeA_H[0]]);
                SetInfoValue("TITLE", codec.GetString(TitleA).Replace('\0', ' '));
                SetInfoValue("EVENT", codec.GetString(Event).Replace('\0', ' '));
                SetInfoValue("DATE", codec.GetString(Date).Replace('\0', ' '));
                SetInfoValue("SITE", codec.GetString(Site).Replace('\0', ' '));
                SetInfoValue("RED", codec.GetString(Red).Replace('\0', ' '));
                SetInfoValue("BLACK", codec.GetString(Black).Replace('\0', ' '));
                SetInfoValue("OPENING", codec.GetString(Opening).Replace('\0', ' '));
                SetInfoValue("WRITER", codec.GetString(RMKWriter).Replace('\0', ' '));
                SetInfoValue("AUTHOR", codec.GetString(Author).Replace('\0', ' '));
                SetInfoFEN(Board.GetFEN(pieceChars.ToString()), PieceColor.RED); // 可能存在不是红棋先走的情况？

                byte __sub(byte a, byte b) { return (byte)(a - b); }; // 保持为<256

                void __readBytes(byte[] bytes, int size)
                {
                    int pos = (int)stream.Position;
                    stream.Read(bytes, 0, size);
                    if(Version[0] > 10) // '字节解密'
                        for(uint i = 0;i != size;++i)
                            bytes[i] = __sub(bytes[i], F32Keys[(pos + i) % 32]);
                }

                uint __getRemarksize()
                {
                    byte[] clen = new byte[4];
                    __readBytes(clen, 4);
                    return (uint)(clen[0] + (clen[1] << 8) + (clen[2] << 16) + (clen[3] << 24)) - KeyRMKSize;
                };

                byte[] data = new byte[4];
                byte frc = data[0], trc = data[1], tag = data[2];
                string? __readDataAndGetRemark()
                {
                    __readBytes(data, 4);
                    uint RemarkSize = 0;
                    frc = data[0];
                    trc = data[1];
                    tag = data[2];
                    if(Version[0] <= 10)
                    {
                        tag = (byte)((((tag & 0xF0) != 0) ? 0x80 : 0) | (((tag & 0x0F) != 0) ? 0x40 : 0));
                        RemarkSize = __getRemarksize();
                    }
                    else
                    {
                        tag &= 0xE0;
                        if((tag & 0x20) != 0)
                            RemarkSize = __getRemarksize();
                    }
                    if(RemarkSize > 0)
                    { // # 如果有注解
                        byte[] rem = new byte[2048 * 2];
                        __readBytes(rem, (int)RemarkSize);
                        return codec.GetString(rem).Replace('\0', ' ').Trim();
                    }

                    return null;
                }

                stream.Seek(1024, SeekOrigin.Begin);
                _manualMove.CurRemark = __readDataAndGetRemark();
                _manualMove.SetBoard(InfoValue("FEN"));

                if((tag & 0x80) == 0) // 无左子树
                    return;

                // 有左子树
                //ManualMoveAppendIterator appendIter(manual->manualMove());
                //while(stream.status() == QDataStream::Status::Ok && !appendIter.over())
                Stack<Move> preMoves = new();
                preMoves.Push(_manualMove.CurMove);
                bool isOther = false;
                // 当前棋子为根，且有后继棋子时，表明深度搜索已经回退到根，已经没有后续棋子了
                while(!_manualMove.CurMove.IsRoot || !_manualMove.CurMove.HasAfter(false))
                {
                    var remark = __readDataAndGetRemark();
                    //# 一步棋的起点和终点有简单的加密计算，读入时需要还原

                    int fcolrow = __sub(frc, (byte)(0X18 + KeyXYf[0])),
                        tcolrow = __sub(trc, (byte)(0X20 + KeyXYt[0]));
                    if(fcolrow > 89 || tcolrow > 89)
                        throw new Exception("fcolrow > 89 || tcolrow > 89 ? ");

                    int frow = fcolrow % 10, fcol = fcolrow / 10, trow = tcolrow % 10,
                        tcol = tcolrow / 10;

                    CoordPair coordPair = new(new(frow, fcol), new(trow, tcol));
                    bool hasNext = (tag & 0x80) != 0, hasOther = (tag & 0x40) != 0;

                    var curCoordPair = _manualMove.CurMove.CoordPair;
                    if(curCoordPair.FromCoord.row == frow && curCoordPair.FromCoord.col == fcol
                        && curCoordPair.ToCoord.row == trow && curCoordPair.ToCoord.col == tcol)
                    {
                        Console.WriteLine("Error: " + fileName); //+ coordPair << remark;
                        _manualMove.CurRemark = remark;
                    }
                    else
                    {
                        _manualMove.AddMove(coordPair, remark, isOther);
                        //Console.WriteLine("_manualMove.CurMove: " + _manualMove.CurMove.ToString());

                        if(hasNext && hasOther)
                            preMoves.Push(_manualMove.CurMove);

                        isOther = !hasNext && !hasOther && preMoves.Count > 0;
                        if(isOther)
                            _manualMove.CurMove = preMoves.Pop(); // 最后时，将回退到根
                    }
                }
            }
        }

        private void ReadCM(string fileName)
        {

        }

        public string InfoValue(string key) { return _info[key]; }
        public void SetInfoValue(string key, string value) { _info[key] = value.Trim(); }

        public void SetInfoFEN(string fen, PieceColor firstColor)
        {
            char color = firstColor == PieceColor.RED ? 'r' : 'b';
            SetInfoValue("FEN", string.Format($"{fen} {color} - - 0 1"));
        }

        new public string ToString()
        {
            string result = "";
            foreach(var kv in _info)
                result += string.Format($"[{kv.Key} \"{kv.Value}\"]\n");

            return result + _manualMove.ToString() + '\n';
        }

        private readonly Dictionary<string, string> _info;
        private readonly ManualMove _manualMove;
    }

    internal class ManualMove
    {
        public ManualMove(RootMove rootMove)
        {
            _board = new();
            _rootMove = rootMove;
            CurMove = rootMove;
        }

        //public List<Coord> GetCanPutCoords(Piece piece)
        //{ return new(); }
        //public List<Coord> GetCanMoveCoords(Coord fromCoord)
        //{ return new(); }

        public void AddMove(CoordPair coordPair, string? remark, bool isOther)
        {
            CurMove = isOther ? CurMove.AddOtherMove(coordPair, remark) : CurMove.AddAfterMove(coordPair, remark);
        }

        //public bool GoNext() // 前进
        //{ return true; }
        //public bool BackNext() // 本着非变着，则回退一着
        //{ return true; }
        //public bool GoOther() // 前进变着
        //{ return true; }
        //public bool BackOther() // 回退变着
        //{ return true; }
        //public bool GoTo(Move move) // 前进至指定move
        //{ return true; }

        //private void done(Move move) { }
        //private void undo(Move move) { }

        public bool SetBoard(string fen)
        {
            return _board.SetFEN(fen.Split(' ')[0]);
        }

        public Move CurMove { get; set; }
        public string? CurRemark { get { return CurMove.Remark; } set { CurMove.Remark = value?.Trim(); } }

        new public string ToString()
        {
            string result = _board.ToString();
            _rootMove.EnumVisible = false;
            int count = -1; // 去掉根节点
            foreach(var move in _rootMove)
            {
                result += move.ToString() + '\n';
                count++;
            }

            return result + string.Format($"着法数量：【{count}】\n");
        }

        private readonly Board _board;
        private readonly RootMove _rootMove;
    }
}
