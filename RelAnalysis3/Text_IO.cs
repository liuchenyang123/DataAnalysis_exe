using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RelAnalysis3
{
    public class Text_IO
    {
        public delegate void WriteToTxtDelegate1(List<BaseXY> l, string path);
        /// <summary>
        /// 输出断面文件
        /// </summary>
        /// <param name="l">断面点集</param>
        /// <param name="path">保存路径</param>
        public static void WriteToTxt(List<BaseXY> l, string path)
        {
            //if (!File.Exists(path))
            //{
            var fsc = new FileStream(path, FileMode.Create, FileAccess.Write);//若文件不存在，创建TXT文件
            fsc.Close();
            //}
            FileStream fs = new FileStream(path, FileMode.Append, FileAccess.Write);
            TextWriter tw = new StreamWriter(fs, Encoding.Default);
            StringBuilder builder = new StringBuilder();
            foreach (var p in l)
            {
                string row = p.X.ToString() + " " + p.Y.ToString();// +" "+p.Error.ToString().Replace(",","");
                //string row = p.X.ToString() + " " + p.Y.ToString() + " " + p.Color.ToString() + " " + p.Color.ToString() + " " + p.Color.ToString();
                if (builder.Length + row.Length < 1024 * 1024 * 100)
                {
                    builder.AppendLine(row);//换行
                }
                else
                {
                    tw.Write(builder.ToString());
                    tw.Flush();

                    builder.Remove(0, builder.Length);
                    builder.AppendLine(row);
                }
            }
            tw.Write(builder.ToString());
            tw.Flush();
            tw.Close();
            fs.Close();
        }
        public static void Write3DToTxt(List<BaseXY> l, string path)
        {
            //if (!File.Exists(path))
            //{
            var fsc = new FileStream(path, FileMode.Create, FileAccess.Write);//若文件不存在，创建TXT文件
            fsc.Close();
            //}
            FileStream fs = new FileStream(path, FileMode.Append, FileAccess.Write);
            TextWriter tw = new StreamWriter(fs, Encoding.Default);
            StringBuilder builder = new StringBuilder();
            foreach (var p in l)
            {
                string row = p.X.ToString() + " " + p.Y.ToString()+" "+p.angle+" "+p.Error+" "+(p.Color/10000.0);
                //string row = p.X.ToString() + " " + p.Y.ToString() + " " + p.Color.ToString() + " " + p.Color.ToString() + " " + p.Color.ToString();
                if (builder.Length + row.Length < 1024 * 1024 * 100)
                {
                    builder.AppendLine(row);//换行
                }
                else
                {
                    tw.Write(builder.ToString());
                    tw.Flush();

                    builder.Remove(0, builder.Length);
                    builder.AppendLine(row);
                }
            }
            tw.Write(builder.ToString());
            tw.Flush();
            tw.Close();
            fs.Close();
        }
        public static void WriteS3D_bit2(Faro_point[][] l, string path)
        {
            var fsc = new FileStream(path, FileMode.Create, FileAccess.Write);//清空TXT文件
            fsc.Close();
            string path2 = path.Replace(".tsd",".tsds");
            var fsc2 = new FileStream(path2, FileMode.Create, FileAccess.Write);//清空TXT文件
            fsc2.Close();
            /////////////////////////////////////////////////存储
            FileStream fs = new FileStream(path, FileMode.Append, FileAccess.Write);//文件流
            BinaryWriter bw = new BinaryWriter(fs, Encoding.ASCII);
            FileStream fs2 = new FileStream(path2, FileMode.Append, FileAccess.Write);//文件流
            BinaryWriter bw2 = new BinaryWriter(fs2, Encoding.ASCII);

            int col = l.Length;
            int row = l[0].Length;
            for (int i = 0; i < col; i++)
            {
                for (int j = 0; j < row; j++)
                {
                    Faro_point v = l[i][j];
                    bw.Write(v.X);
                    bw.Write(v.Y);
                    bw.Write(v.Z);
                    bw.Write(v.Color);
                    byte b = 0;
                    if (v.tag == 1) b = 1;
                    bw.Write(b);

                    bw2.Write(v.xe);
                    bw2.Write(v.H);
                }
            }
            bw.Close();
            fs.Close();

            bw2.Close();
            fs2.Close();

        }//保存点云文件（新待改##***********）
        public delegate void WriteTo3dTxtDelegate(Faro_point[][] l, string path);
        /// <summary>
        /// 向指定文件添加一行
        /// </summary>
        /// <param name="path">文件路径</param>
        /// <param name="l">写入字符串</param>
        /// <param name="isCover">是否覆盖原有内容</param>
        public static void WriteLineToTxt(string path, string l,bool isCover=false)
        {
            if (!File.Exists(path))
            {
                var fsc = new FileStream(path, FileMode.Create, FileAccess.Write);//若文件不存在，创建TXT文件
                fsc.Close();
            }
            else if (isCover) {
                var fsc = new FileStream(path, FileMode.Create, FileAccess.Write);//若文件不存在，创建TXT文件
                fsc.Close();
            }
            FileStream fs = new FileStream(path, FileMode.Append, FileAccess.Write);
            TextWriter tw = new StreamWriter(fs, Encoding.Default);
            StringBuilder builder = new StringBuilder();
            //foreach (var p in l)
            {
                string row = l;
                //string row = p.X.ToString() + " " + p.Y.ToString() + " " + p.Color.ToString() + " " + p.Color.ToString() + " " + p.Color.ToString();
                if (builder.Length + row.Length < 1024 * 1024 * 100)
                {
                    builder.AppendLine(row);//换行
                }
                else
                {
                    tw.Write(builder.ToString());
                    tw.Flush();

                    builder.Remove(0, builder.Length);
                    builder.AppendLine(row);
                }
            }
            tw.Write(builder.ToString());
            tw.Flush();
            tw.Close();
            fs.Close();
        }
        /// <summary>
        /// 向指定文件添加多行字符信息
        /// </summary>
        /// <param name="path">文件路径</param>
        /// <param name="ls">写入字符串集合</param>
        /// <param name="isCover">是否覆盖原有内容</param>
        public static void WriteLineToTxt(string path, List<string> ls, bool isCover = false)
        {
            if (!File.Exists(path))
            {
                var fsc = new FileStream(path, FileMode.Create, FileAccess.Write);//若文件不存在，创建TXT文件
                fsc.Close();
            }
            else if (isCover)
            {
                var fsc = new FileStream(path, FileMode.Create, FileAccess.Write);//若文件不存在，创建TXT文件
                fsc.Close();
            }
            FileStream fs = new FileStream(path, FileMode.Append, FileAccess.Write);
            TextWriter tw = new StreamWriter(fs, Encoding.Default);
            StringBuilder builder = new StringBuilder();
            foreach (var l in ls)
            {
                string row = l;
                if (builder.Length + row.Length < 1024 * 1024 * 100)
                {
                    builder.AppendLine(row);//换行
                }
                else
                {
                    tw.Write(builder.ToString());
                    tw.Flush();

                    builder.Remove(0, builder.Length);
                    builder.AppendLine(row);
                }
            }
            tw.Write(builder.ToString());
            tw.Flush();
            tw.Close();
            fs.Close();
        }
        /// <summary>
        /// 保存环缝信息
        /// </summary>
        /// <param name="path">文件路径</param>
        /// <param name="lm">环缝信息</param>
        /// <param name="file">类型名</param>
        /// <param name="isCover">是否覆盖原有内容</param>
        public static void WriteMileageToTxt(string path,List<double>lm,string file,bool isCover=false) {
            if (!File.Exists(path))
            {
                var fsc = new FileStream(path, FileMode.Create, FileAccess.Write);//若文件不存在，创建TXT文件
                fsc.Close();
            }
            else if (isCover)
            {
                var fsc = new FileStream(path, FileMode.Create, FileAccess.Write);//若文件不存在，创建TXT文件
                fsc.Close();
            }
            FileStream fs = new FileStream(path, FileMode.Append, FileAccess.Write);
            TextWriter tw = new StreamWriter(fs, Encoding.Default);
            StringBuilder builder = new StringBuilder();
            string time = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss");
            for (int i = 0; i < lm.Count; i++) {
                string row = time + " " + file +" "+ lm[i];
                if (builder.Length + row.Length < 1024 * 1024 * 100)
                {
                    builder.AppendLine(row);//换行
                }
                else
                {
                    tw.Write(builder.ToString());
                    tw.Flush();

                    builder.Remove(0, builder.Length);
                    builder.AppendLine(row);
                }
            }
            tw.Write(builder.ToString());
            tw.Flush();
            tw.Close();
            fs.Close();
        }
        /// <summary>
        /// 读取扫描数据
        /// </summary>
        /// <param name="readpath">文件路径</param>
        /// <param name="startCol">开始读取圈数</param>
        /// <param name="endCol">终止读取圈数</param>
        /// <param name="NumRow">每圈点数</param>
        /// <param name="l">点云数据</param>
        /// <returns></returns>
        public static bool ReadS3DFile(string readpath,int startCol,int endCol,int NumRow,out Faro_point[][] l) {
            l = new Faro_point[endCol - startCol][];
            FileStream fs1 = new FileStream(readpath, FileMode.Open);
            BinaryReader br = new BinaryReader(fs1);
            fs1.Seek(startCol * NumRow * 29, SeekOrigin.Current);
            int N = 0;
            int Col = 0;
            while (true)
            {
                try
                {
                    double x = br.ReadDouble();
                    double y = br.ReadDouble();
                    double z = br.ReadDouble();
                    int color = br.ReadInt32();
                    byte tag = br.ReadByte();
                    if (N == 0) l[Col] = new Faro_point[NumRow];//暂定为2500
                    l[Col][N] = new Faro_point() { X = x, Y = y, Z = z, Color = color, tag = tag };
                    N++;
                    if (N == NumRow) { N = 0; Col++; }
                }
                catch { break; }
            }
            br.Close();
            fs1.Close();
            return true;
        }
        /// <summary>
        /// 读取扫描数据的xe和H
        /// </summary>
        /// <param name="readpath">文件路径</param>
        /// <param name="startCol">开始读取圈数</param>
        /// <param name="endCol">终止读取圈数</param>
        /// <param name="NumRow">每圈点数</param>
        /// <param name="l">点云数据</param>
        /// <returns></returns>
        public static bool ReadS3D_xeH(string readpath, int startCol, int endCol, int NumRow,ref Faro_point[][] l) {
            try {
                FileStream fs1 = new FileStream(readpath, FileMode.Open);
                BinaryReader br = new BinaryReader(fs1);
                fs1.Seek(startCol * NumRow * 16, SeekOrigin.Current);//两个double 即是16个字节
                for (int i = 0; i < l.Length; i++)
                {
                    for (int j = 0; j < l[i].Length; j++)
                    {
                        l[i][j].xe = br.ReadDouble();
                        l[i][j].H = br.ReadDouble();
                    }
                }
                br.Close();
                fs1.Close();
                return true;
            } catch { return false; }
        }
        
        public static void ReadLmFile(string readpath,out List<MileageEllipse>lm) {
            lm = new List<MileageEllipse>();
            if (!File.Exists(readpath)) return;
            string[] lines;
            char[] split1 = new char[] { '\r', '\n' };//分行标识
            using (System.IO.StreamReader sr = new System.IO.StreamReader(readpath, Encoding.Default))
            {
                lines = sr.ReadToEnd().Split(split1, StringSplitOptions.RemoveEmptyEntries);
            }

            for (int i = 0; i < lines.Length; i++) {
                string[] items = lines[i].Split(' ');
                if (items.Length == 8)
                {
                    MileageEllipse d = new MileageEllipse();
                    d.mileage = Convert.ToDouble(items[0]);
                    d.dm = Convert.ToDouble(items[1]);
                    d.col = Convert.ToInt32(items[2]);
                    d.ep = new Ellipse();
                    d.ep.A = Convert.ToDouble(items[3]);
                    d.ep.B = Convert.ToDouble(items[4]);
                    d.ep.X0 = Convert.ToDouble(items[5]);
                    d.ep.Y0 = Convert.ToDouble(items[6]);
                    d.ep.Angle = Convert.ToDouble(items[7]);
                    lm.Add(d);
                }
                else {
                    MileageEllipse d = new MileageEllipse();
                    d.mileage = Convert.ToDouble(items[0]);
                    d.dm = Convert.ToDouble(items[1]);
                    d.col = Convert.ToInt32(items[2]);
                    d.ep = new Ellipse();
                    lm.Add(d);
                }
                
            }
        }//读取里程文件
       
        public static void ReadFile(string readpath, out Ellipse ellipse, out ulong Get_Autotime,out ulong FirstScanFileHeadTime,out ulong DetaScanTime,out double AutoTimePra, out int numcol, out int numrow)
        {
            ellipse = null; Get_Autotime = 0; FirstScanFileHeadTime = 0; DetaScanTime = 0;
            numcol = 5000; numrow = 4990; AutoTimePra = 10;
            if (!File.Exists(readpath)) return;
            string[] lines;
            char[] split1 = new char[] { '\r', '\n' };//分行标识
            using (System.IO.StreamReader sr = new System.IO.StreamReader(readpath, Encoding.Default))
            {
                lines = sr.ReadToEnd().Split(split1, StringSplitOptions.RemoveEmptyEntries);
            }
            for (int i = 0; i < lines.Length; i++)
            {
                string[] items = lines[i].Split(' ');
                //long time = Convert.ToInt64(items[0]);
                string type = items[1];
                if (type == "ScanHeadTime")
                {
                    Get_Autotime = Convert.ToUInt64(items[2]);
                }
                else if (type == "FirstScanFileHeadTime") {
                    FirstScanFileHeadTime = Convert.ToUInt64(items[2]);
                }
                else if (type == "DetaScanTime")
                {
                    DetaScanTime = Convert.ToUInt64(items[2]);
                }
                else if (type == "AutoTimePra")
                {
                    AutoTimePra = Convert.ToDouble(items[2]);
                }
                else if (type == "Ellipse")
                {
                    ellipse = new Ellipse()
                    {
                        A = Convert.ToDouble(items[2]),
                        B = Convert.ToDouble(items[3]),
                        X0 = Convert.ToDouble(items[4]),
                        Y0 = Convert.ToDouble(items[5]),
                        Angle = Convert.ToDouble(items[6]),
                    };
                }
                else if (type == "NumCol")
                {
                    numcol = Convert.ToInt32(items[2]);
                }
                else if (type == "NumRow")
                {
                    numrow = Convert.ToInt32(items[2]);
                }
            }
        }
        public static void WriteToJson<T>(T YuanXing, out string str)
        {
            str = Newtonsoft.Json.JsonConvert.SerializeObject(YuanXing);
        }
        public static void WriteToJson(List<DivideLine>LDivide, out string str)
        {
            str = Newtonsoft.Json.JsonConvert.SerializeObject(LDivide);
        }
        public static void ReadDivides(string path,out List<DivideLine> ldivide) {
            ldivide = new List<DivideLine>();
            if (!File.Exists(path)) return;
            Encoding encode = System.Text.Encoding.Default;
            string jsonText = System.IO.File.ReadAllText(path, encode);
            JToken jo = (JToken)JsonConvert.DeserializeObject(jsonText);
            foreach (var v in jo) {
                DivideLine divideLine = new DivideLine();
                divideLine.ID=Convert.ToInt32(v["ID"].ToString());
                divideLine.left_ID= Convert.ToInt32(v["left_ID"].ToString());
                divideLine.right_ID = Convert.ToInt32(v["right_ID"].ToString());
                divideLine.attribute = v["attribute"].ToString();
                divideLine.point_data = new List<point_data>();
                foreach (var vv in v["point_data"]) {
                    point_data point_Data = new point_data();
                    point_Data.w= Convert.ToInt32(vv["w"].ToString());
                    point_Data.h = Convert.ToInt32(vv["h"].ToString());
                    divideLine.point_data.Add(point_Data);
                }
                ldivide.Add(divideLine);
            }
        }
        public static void ReadDoMessage(string readpath, out List<Do_Message>doM) {
            doM = new List<Do_Message>();
            if (!File.Exists(readpath)) return;
            string[] lines;
            char[] split1 = new char[] { '\r', '\n' };//分行标识
            using (System.IO.StreamReader sr = new System.IO.StreamReader(readpath, Encoding.Default))
            {
                lines = sr.ReadToEnd().Split(split1, StringSplitOptions.RemoveEmptyEntries);
            }
            for (int i = 0; i < lines.Length; i++) {
                string[] items = lines[i].Split(' ');
                if (items.Length > 3) {
                    Do_Message do_Message = new Do_Message();
                    do_Message.NameOrPoint = items[0];
                    do_Message.y0 = Convert.ToDouble(items[1]);
                    do_Message.y1 = Convert.ToDouble(items[2]);
                    do_Message.y2 = Convert.ToDouble(items[3]);
                    do_Message.expand = Convert.ToInt32(items[4]);
                    doM.Add(do_Message);
                }
            }

        }
        public static void ReadS3D_huan(string path, double startM, double endM, out List<Faro_point> l)
        {
            try
            {
                long len = GetFileSize(path);
                if (len < 800 * 1024 * 1024)
                {
                    ReadS3D_huan2(path, startM, endM, out l);
                    return;
                }
                else if (len < 1000 * 1024 * 1024) {
                    ReadS3D_huan1(path, startM, endM, out l);
                    return;
                }
                FileStream fs1 = new FileStream(path, FileMode.Open);
                BinaryReader br = new BinaryReader(fs1);
                l = new List<Faro_point>();
                int isNow = 0;
                len = fs1.Length;
                while (true)
                {
                    try
                    {
                        Faro_point p = new Faro_point();

                        double x = br.ReadDouble();
                        double y = br.ReadDouble();
                        double z = br.ReadDouble();
                        int color = br.ReadInt32();
                        double xe = br.ReadDouble();
                        double H = br.ReadDouble();
                        double Hs = br.ReadDouble();
                        byte tag0 = br.ReadByte();
                        p.X = x;
                        p.Y = y;
                        p.Z = z;
                        p.Color = color;
                        if (tag0 == 1) continue;
                        if (p.Y < startM) continue;
                        if (p.Y > endM) continue;
                        if (p.Y < startM || p.Y > endM)
                        {
                            if (isNow == 0)
                            {
                                if (fs1.Position + 53 * 1000 >= len) fs1.Position = len - 1;
                                else fs1.Position += 53 * 1000;
                                continue;
                            }
                            else if (isNow >= 1)
                            {
                                if (fs1.Position + 53 * 1000 >= len) fs1.Position = len - 1;
                                else fs1.Position += 53 * 1000;
                                isNow++;
                                continue;
                            }
                        }
                        else if (isNow == 0)
                        {
                            if (fs1.Position < 53 * 1000) fs1.Position = 0;
                            else fs1.Position -= 53 * 1000;

                            isNow++;
                            continue;
                        }
                        else if (isNow > 1)
                        {
                            isNow = -1;
                            if (fs1.Position < 53 * 1000) fs1.Position = 0;
                            else fs1.Position -= 53 * 1000;
                            continue;
                        }
                        l.Add(p);
                    }
                    catch
                    {
                        break;
                    }
                }
                br.Close();
                fs1.Close();

            }
            catch
            {
                l = new List<Faro_point>();
            }
        }
        public static void ReadS3D_huan1(string path, double startM, double endM, out List<Faro_point> l)
        {
            try
            {
                FileStream fs1 = new FileStream(path, FileMode.Open);
                BinaryReader br = new BinaryReader(fs1);
                l = new List<Faro_point>();
                int isNow = 0;
                long len = fs1.Length;
                while (true)
                {
                    try
                    {
                        Faro_point p = new Faro_point();

                        double x = br.ReadDouble();
                        double y = br.ReadDouble();
                        double z = br.ReadDouble();
                        int color = br.ReadInt32();
                        double xe = br.ReadDouble();
                        double H = br.ReadDouble();
                        byte tag0 = br.ReadByte();
                        p.X = x;
                        p.Y = y;
                        p.Z = z;
                        p.Color = color;
                        if (tag0 == 1) continue;
                        if (p.Y < startM) continue;
                        if (p.Y > endM) continue;
                        if (p.Y < startM || p.Y > endM)
                        {
                            if (isNow == 0)
                            {
                                if (fs1.Position + 45 * 1000 >= len) fs1.Position = len - 1;
                                else fs1.Position += 45 * 1000;
                                continue;
                            }
                            else if (isNow >= 1)
                            {
                                if (fs1.Position + 45 * 1000 >= len) fs1.Position = len - 1;
                                else fs1.Position += 45 * 1000;
                                isNow++;
                                continue;
                            }
                        }
                        else if (isNow == 0)
                        {
                            if (fs1.Position < 45 * 1000) fs1.Position = 0;
                            else fs1.Position -= 45 * 1000;

                            isNow++;
                            continue;
                        }
                        else if (isNow > 1)
                        {
                            isNow = -1;
                            if (fs1.Position < 45 * 1000) fs1.Position = 0;
                            else fs1.Position -= 45 * 1000;
                            continue;
                        }
                        l.Add(p);
                    }
                    catch
                    {
                        break;
                    }
                }
                br.Close();
                fs1.Close();
            }
            catch
            {
                l = new List<Faro_point>();
            }
        }
        public static void ReadS3D_huan2(string path, double startM, double endM, out List<Faro_point> l)
        {
            try
            {
                FileStream fs1 = new FileStream(path, FileMode.Open);
                BinaryReader br = new BinaryReader(fs1);
                l = new List<Faro_point>();
                int isNow = 0;
                long len = fs1.Length;
                while (true)
                {
                    try
                    {
                        Faro_point p = new Faro_point();

                        double x = br.ReadDouble();
                        double y = br.ReadDouble();
                        double z = br.ReadDouble();
                        int color = br.ReadInt32();
                        byte tag0 = br.ReadByte();
                        p.X = x;
                        p.Y = y;
                        p.Z = z;
                        p.Color = color;

                        if (tag0 == 1) continue;
                        if (p.Y < startM) continue;
                        if (p.Y > endM) continue;
                        if (p.Y < startM || p.Y > endM)
                        {
                            if (isNow == 0)
                            {
                                if (fs1.Position + 29 * 1000 >= len) fs1.Position = len - 1;
                                else fs1.Position += 29 * 1000;
                                continue;
                            }
                            else if (isNow >= 1)
                            {
                                if (fs1.Position + 29 * 1000 >= len) fs1.Position = len - 1;
                                else fs1.Position += 29 * 1000;
                                isNow++;
                                continue;
                            }
                        }
                        else if (isNow == 0)
                        {
                            if (fs1.Position < 29 * 1000) fs1.Position = 0;
                            else fs1.Position -= 29 * 1000;

                            isNow++;
                            continue;
                        }
                        else if (isNow > 1)
                        {
                            isNow = -1;
                            if (fs1.Position < 29 * 1000) fs1.Position = 0;
                            else fs1.Position -= 29 * 1000;
                            continue;
                        }
                        l.Add(p);
                    }
                    catch
                    {
                        break;
                    }
                }
                br.Close();
                fs1.Close();

            }
            catch
            {
                l = new List<Faro_point>();
            }
        }
       
        static public long GetDirectorySize(string dirPath)
        {
            long len = 0;
            //判断该路径是否存在（是否为文件夹）
            if (!Directory.Exists(dirPath))
            {
                //查询文件的大小
                len = GetFileSize(dirPath);
            }
            else
            {
                //定义一个DirectoryInfo对象
                DirectoryInfo di = new DirectoryInfo(dirPath);
                //通过GetFiles方法，获取di目录中的所有文件的大小
                foreach (FileInfo fi in di.GetFiles())
                {
                    len += fi.Length;
                }
                //获取di中所有的文件夹，并存到一个新的对象数组中，以进行递归
                DirectoryInfo[] dis = di.GetDirectories();
                if (dis.Length > 0)
                {
                    for (int i = 0; i < dis.Length; i++)
                    {
                        len += GetDirectorySize(dis[i].FullName);
                    }
                }
            }
            return len;
        }//计算文件夹大小
        static public long GetFileSize(string filePath)
        {
            //定义一个FileInfo对象，是指与filePath所指向的文件相关联，以获取其大小
            FileInfo fileInfo = new FileInfo(filePath);
            return fileInfo.Length;
        }//计算文件大小
       
        public static void WriteXeH(string path, List<Faro_point> lfp, bool isCover = true)
        {
            if (!File.Exists(path))
            {
                var fsc = new FileStream(path, FileMode.Create, FileAccess.Write);//若文件不存在，创建TXT文件
                fsc.Close();
            }
            else if (isCover)
            {
                var fsc = new FileStream(path, FileMode.Create, FileAccess.Write);//若文件不存在，创建TXT文件
                fsc.Close();
            }
            FileStream fs = new FileStream(path, FileMode.Append, FileAccess.Write);
            TextWriter tw = new StreamWriter(fs, Encoding.Default);
            StringBuilder builder = new StringBuilder();
            foreach (var v in lfp)
            {
                string str = string.Format("{0:N4}",v.xe)  + " " + string.Format("{0:N4}", v.H);
                if (builder.Length + str.Length < 1024 * 1024 * 100)
                {
                    builder.AppendLine(str);//换行
                }
                else
                {
                    tw.Write(builder.ToString());
                    tw.Flush();
                    builder.Remove(0, builder.Length);
                    builder.AppendLine(str);
                }
            }

            tw.Write(builder.ToString());
            tw.Flush();
            tw.Close();
            fs.Close();


        }
        public static void WriteSlabMessage(string path, List<List<Faro_point>> llfp, bool isCover = false)
        {
            if (!File.Exists(path))
            {
                var fsc = new FileStream(path, FileMode.Create, FileAccess.Write);//若文件不存在，创建TXT文件
                fsc.Close();
            }
            else if (isCover)
            {
                var fsc = new FileStream(path, FileMode.Create, FileAccess.Write);//若文件不存在，创建TXT文件
                fsc.Close();
            }
            
            FileStream fs = new FileStream(path, FileMode.Append, FileAccess.Write);
            BinaryWriter bw = new BinaryWriter(fs, Encoding.ASCII);
            foreach (var lfp in llfp) {
                int len = lfp.Count;
                if (len == 0) continue;
                bw.Write(len);
                bw.Write(lfp[0].tag);
                foreach (var v in lfp)
                {
                    bw.Write(v.X);
                    bw.Write(v.Y);
                    bw.Write(v.H);
                    bw.Write(v.Hs);
                    bw.Write(v.Z);
                    bw.Write(v.R);
                }
            }
           
            bw.Close();
            fs.Close();
        }
        public static void WriteSlabMessage2(string path, List<Faro_point> Rank_test_ALL, List<List<Faro_point>> llf, bool isCover = true)
        {
            if (!File.Exists(path))
            {
                var fsc = new FileStream(path, FileMode.Create, FileAccess.Write);//若文件不存在，创建TXT文件
                fsc.Close();
            }
            else if (isCover)
            {
                var fsc = new FileStream(path, FileMode.Create, FileAccess.Write);//若文件不存在，创建TXT文件
                fsc.Close();
            }

            FileStream fs = new FileStream(path, FileMode.Append, FileAccess.Write);
            BinaryWriter bw = new BinaryWriter(fs, Encoding.ASCII);
            bw.Write(llf.Count);//先生成llf（环线）
            foreach (var lfp in llf)
            {
                int len = lfp.Count;
                if (len == 0) continue;
                bw.Write(len);
                bw.Write(lfp[0].tag);
                foreach (var v in lfp)
                {
                    bw.Write(v.X);
                    bw.Write(v.Y);
                    bw.Write(v.H);
                    bw.Write(v.Hs);
                    bw.Write(v.Z);
                    bw.Write(v.R);
                }
            }
            bw.Write(Rank_test_ALL.Count);
            foreach (var v in Rank_test_ALL) {
                bw.Write(v.tag);
                bw.Write(v.X);
                bw.Write(v.Y);
                bw.Write(v.H);
                bw.Write(v.Hs);
                bw.Write(v.Z);
                bw.Write(v.R);
            }

            bw.Close();
            fs.Close();
        }
       
    }
}
