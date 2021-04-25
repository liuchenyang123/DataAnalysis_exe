using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RelAnalysis3
{
    //主程序
    class Program
    {
        /// <summary>
        /// /数据处理EXE
        /// 版本4.0 用于版本号1.2.1软件（对应2021.2交货小车型号）现场实时处理以及后处理数据解析、断面数据生成等
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            // 测试用例
            //args = new string[15] {
            //    @"H:\测试\Fls",
            //    "上行",
            //    @"H:\测试\上行\Mileage",
            //    @"H:\测试\上行\限界文件.txt",
            //    "0.68",
            //    "2mm",
            //    "LimitAnalysis",
            //    "1",
            //    "0",
            //    "2.7",
            //    "132616879223158835",
            //    "1.6",
            //    @"D:\dist",
            //    //@"D:\TSDALL\testingTSD\TSD202101\TSD\ScanUI\bin\x86\Debug",
            //    "","",
            //};

            DateTime dateTime = DateTime.Now;
            ///检查输入参数，不足则跳出，并记录到"D://Log.txt"
            if (args.Length < 12)
            {
                try
                {
                    string log_exit = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + " 程序参数不足，退出程序！\n参数数目：" + args.Length + "\n";
                    Text_IO.WriteLineToTxt("D://Log.txt", log_exit);//测试用记录输入参数
                    return;
                }
                catch { return; }
            }
            try
            {
                ///逐个参数赋值
                string loadFlsRoad = args[0] + "\\";//扫描文件存储路径
                string baseName = args[1];//扫描基本名（名称前缀）
                string loadMileageHead = args[2];//扫描里程所在路径
                string MileageRoad = args[3];//扫描里程存储路径；限界文件存储路径
                string Accuracy = args[5];//扫描精度
                string doThing = args[6];//解析模式
                double scan_E = Convert.ToDouble(Accuracy.Replace("m", ""));//扫描精度（double）
                string runradius = args[9];//扫描隧道半径
                double Tunnel_Radius = Convert.ToDouble(runradius.Replace("m", ""));//扫描隧道半径（double）
                int cutNum = Convert.ToInt32(args[7]);//解析进程数
                int startNum = Convert.ToInt32(args[8]);//第几个解析进程（从0计）
                string Save_path = Path.GetDirectoryName(loadMileageHead);//扫描解析数据存储路径
                long startTime = Convert.ToInt64(args[10]);//扫描起始时间
                double HuanWidth = Convert.ToDouble(args[11]);//扫描隧道环宽（仅限盾构隧道）
                Process_Solve.EXEPath = args[12];//调用EXE路径
                ///如果参数超过13，则可能包含更多信息（2021.02后版本开始添加该类信息）
                if (args.Length > 13)
                {
                    if (args[13] == "1") Process_Solve.IsCuoFeng = false;
                }

                if (doThing.Contains("SecBuild"))
                {
                    BuildSection(baseName, loadMileageHead, MileageRoad, scan_E);//做断面文件生成
                }
                else
                {
                    string path_3D = Save_path + "\\3D\\";//扫描解析点云数据存储路径
                    string path_sPhoto = Save_path + "\\sPhoto\\";//扫描解析辅助图数据存储路径
                    string path_Photo = Save_path + "\\Photo\\";//扫描解析图片数据存储路径
                    string path_Sec = Save_path + "\\Sec\\";//扫描解析断面数据存储路径
                    string path_M = Save_path + "\\Mileage\\";//扫描里程数据存储路径
                    string path_Progress = Save_path + "\\Message\\";//扫描解析信息数据存储路径
                    string path_limit_jpg = Save_path + "\\Limit\\";   //限界文件保存的路径


                    // 创建文件夹
                    CheckDirectory(path_3D);
                    CheckDirectory(path_sPhoto);
                    CheckDirectory(path_Photo);
                    CheckDirectory(path_Sec);
                    CheckDirectory(path_M);
                    CheckDirectory(path_Progress);
                    CheckDirectory(path_limit_jpg);


                    string datasavehead = Save_path + "\\";//文件保存路径
                    double H_adjust = 0.68;// 扫描仪距离轨道高度
                    try
                    {
                        H_adjust = Convert.ToDouble(args[4]);
                    }

                    catch
                    {
                        H_adjust = 0.68;
                    }

                    int expand = (int)(1000 / scan_E);//分辨率（像素/米）
                    int Height = (int)(Tunnel_Radius * Math.PI * 2 * expand + 1);//高清影像图高度
                    Ellipse_Fitting.R_Tunnel = Tunnel_Radius;//隧道半径
                    if (doThing == "Limit")
                    {
                        //限界数据处理
                    }
                    else if (doThing == "LaterAnalysis")
                    {
                        DoDataAanalysis_Later(MileageRoad, cutNum, startNum, loadFlsRoad, datasavehead, baseName, startTime, expand, Height, H_adjust, Tunnel_Radius, HuanWidth);
                    }
                    else if (doThing == "LimitAnalysis")
                    {
                        DoDataAnalysis_limit(MileageRoad, cutNum, startNum, loadFlsRoad, datasavehead, baseName, startTime, expand, Height, H_adjust, Tunnel_Radius, HuanWidth);
                    }
                    else DoDataAanalysis(cutNum, startNum, loadFlsRoad, datasavehead, baseName, startTime, expand, Height, H_adjust, Tunnel_Radius, HuanWidth);
                }


            }
            catch (Exception theE)
            {
                string theElog = "Error: " + theE.Message + " " + theE.StackTrace;
                Text_IO.WriteLineToTxt("D://Log.txt", theElog);
                System.Windows.Forms.MessageBox.Show(theElog);
            }
        }
        /// <summary>
        /// 检测文件夹是否存在，不存在则创建
        /// </summary>
        /// <param name="dir">文件夹</param>
        static void CheckDirectory(string dir)
        {
            if (Directory.Exists(Path.GetDirectoryName(dir)) == false)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(dir));
            }
        }//若文件夹不存在则创建
        /// <summary>
        /// 得到文件名
        /// </summary>
        /// <param name="head">文件前缀</param>
        /// <param name="num">序号</param>
        /// <param name="endstr">文件后缀</param>
        /// <returns></returns>
        private static string GetFileName(string head, int num, string endstr)
        {
            string Str = head;
            if (num < 10)
            {
                Str = head + "00" + num + endstr;
            }
            else if (num < 100)
            {
                Str = head + "0" + num + endstr;
            }
            else
            {
                Str = head + num + endstr;
            }
            return Str;
        }
        //一般分析（自动分环后生成断面、错台、点云等数据）
        /// <summary>
        /// 现场数据解析
        /// </summary>
        /// <param name="CutNum">解析进程数</param>
        /// <param name="StartNum">第几个解析进程（从0计）</param>
        /// <param name="loadFlsRoad">扫描文件存储路径</param>
        /// <param name="dataSaveRoad">扫描解析数据存储路径</param>
        /// <param name="baseName">扫描基本名</param>
        /// <param name="startTime">扫描起始时间</param>
        /// <param name="Expand">分辨率（像素/米）</param>
        /// <param name="Height">影像高度</param>
        /// <param name="H_adjust">扫描仪离轨面高度</param>
        /// <param name="Tunnel_Radius">扫描隧道半径</param>
        /// <param name="HuanWidth">扫描隧道环宽</param>
        public static void DoDataAanalysis(int CutNum, int StartNum, string loadFlsRoad, string dataSaveRoad, string baseName, long startTime, int Expand, int Height, double H_adjust, double Tunnel_Radius, double HuanWidth)
        {
            ///初始化一些路径...
            string loadMileageHead = dataSaveRoad + "Mileage\\";//里程路径
            string savePhotoHead = dataSaveRoad + "sPhoto\\";//辅助图/深度图等存储路径
            string path_Photo = dataSaveRoad + "Photo\\";//高清影像图路径
            string save3DHead = dataSaveRoad + "3D\\";//解析点云数据存储路径
            string path_Progress = dataSaveRoad + "Message\\";//扫描信息存储路径
            string saveSectionhead = dataSaveRoad + "Sec\\";//扫描解析断面存储路径
            int court_needSolve = StartNum + 1;//当前扫描序号（以1开始计数）
            string theHFmessage = path_Progress + "RingSeam_message.txt";//环缝信息保存路径
            string the3Dmessage = path_Progress + "CloudFile_message.txt";//解析文件里程信息存储路径
            string writeTimePath = path_Progress + StartNum + "程序.txt";//数据解析操作日志路径
            Space_Analysis space_Analysis = new Space_Analysis();
            space_Analysis.WriteMessagePath = path_Progress + "message.txt";
            space_Analysis.WriteLogPath = path_Progress + "LogMessage.txt";
            space_Analysis.writeTimePath = writeTimePath;
            space_Analysis.Hadjust = H_adjust;//扫描距离轨面高度

            List<string> mileage_path = new List<string>(); //扫描中生成的小里程数据路径集合
            List<Mileage> mileage_all = new List<Mileage>();//总里程数据
            bool isLog = true;//是否写日志
            bool isFirst = false;//是否是头个文件
            int preNumRow = 0;//上一个扫描文件行数
            int preNumCol = 0;//上一个扫描文件列数
            int JiShu = 0;//计数标志位

            ///如果是第一次解析则令isFirst = true
            ///否则持续监测 message.txt文件是否存在（存在则说明第一个文件已经解析了）
            ///存在后进一步判断是第二个文件还是之后的文件
            ///（1）如果是第二个文件，则监测_1.config文件再读取扫描仪起始时间和展开椭圆信息进行下一步操作
            ///（2）如果是之后的文件，则监测_2.config文件直到该文件生成，并读取扫描仪起始时间和展开椭圆信息
            if (StartNum == 0) isFirst = true;
            else
            {
                JiShu = 0;
                while (!File.Exists(space_Analysis.WriteMessagePath))
                {
                    Thread.Sleep(2000);
                    JiShu++;
                    if (JiShu > 300)
                    {
                        string log = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + " 无信息文件，请确认！";
                        Text_IO.WriteLineToTxt(space_Analysis.WriteLogPath, log);
                        System.Windows.Forms.MessageBox.Show(log);
                        return;
                    }
                }
                JiShu = 0;
                if (StartNum == 1)
                {
                    string readMessage1 = space_Analysis.WriteMessagePath.Replace(".txt", "_1.config");
                    while (!File.Exists(readMessage1))
                    {
                        Thread.Sleep(500);
                        JiShu++;
                        if (JiShu > 10)
                        {
                            string log = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + " 无信息文件" + readMessage1 + "，请确认！";
                            Text_IO.WriteLineToTxt(space_Analysis.WriteLogPath, log);
                            System.Windows.Forms.MessageBox.Show(log);
                            return;
                        }
                    }
                    while (true)
                    {
                        try
                        {
                            Text_IO.ReadFile(space_Analysis.WriteMessagePath, out Ellipse ellipse, out ulong getAutoTime, out ulong firstTime, out ulong detaTime, out double TimePra, out int numcol, out int numrow);
                            space_Analysis.Get_Autotime = getAutoTime;
                            space_Analysis.Get_FirstAutoTime = firstTime;
                            space_Analysis.DetaScanTime = detaTime;
                            space_Analysis.ellipse = ellipse;
                            //space_Analysis.Autotime_pra = TimePra;
                            preNumCol = numcol;
                            preNumRow = numrow;
                            if (space_Analysis.Get_FirstAutoTime == 0 && CutNum != 2) { Thread.Sleep(10000); continue; }
                            break;
                        }
                        catch
                        {
                            JiShu++;
                            Thread.Sleep(1000);
                            if (JiShu > 5) break;//延迟自动跳出
                        }
                    }
                }
                else
                {
                    string readMessage2 = space_Analysis.WriteMessagePath.Replace(".txt", "_2.config");
                    while (!File.Exists(readMessage2))
                    {
                        Thread.Sleep(5000);
                        JiShu++;
                        if (JiShu > 100)
                        {
                            string log = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + " 无信息文件" + readMessage2 + "，请确认！";
                            Text_IO.WriteLineToTxt(space_Analysis.WriteLogPath, log);
                            System.Windows.Forms.MessageBox.Show(log);
                            return;
                        }
                    }
                    while (true)
                    {
                        try
                        {
                            Text_IO.ReadFile(space_Analysis.WriteMessagePath, out Ellipse ellipse, out ulong getAutoTime, out ulong firstTime, out ulong detaTime, out double TimePra, out int numcol, out int numrow);
                            space_Analysis.Get_Autotime = getAutoTime;
                            space_Analysis.Get_FirstAutoTime = firstTime;
                            space_Analysis.DetaScanTime = detaTime;
                            space_Analysis.ellipse = ellipse;
                            //space_Analysis.Autotime_pra = TimePra;
                            preNumCol = numcol;
                            preNumRow = numrow;
                            if (space_Analysis.Get_Autotime == 0 && CutNum != 2) { Thread.Sleep(10000); continue; }
                            break;
                        }
                        catch
                        {
                            JiShu++;
                            Thread.Sleep(1000);
                            if (JiShu > 5) break;//延迟自动跳出
                        }
                    }
                }
            }
            ///进入循环，按照CutNum间隔逐个解析文件
            while (true)
            {
                string fileName_now = GetFileName(loadFlsRoad + baseName, court_needSolve, ".fls");//当前扫描文件路径
                string mileage_now = loadMileageHead + court_needSolve + ".txt";//当前扫描文件对应里程文件路径
                string mileage2_now = mileage_now.Replace(".txt", "_2.txt");//当前扫描文件对应里程文件对应的配置文件路径，如存在则说明里程文件已生成
                string mileage_pre = GetFileName(loadMileageHead + baseName, court_needSolve - 1, ".txt");//上一个扫描文件对应的里程文件路径

                string saveHighePhoto = GetFileName(path_Photo + baseName, court_needSolve, ".tiff");//当前扫描文件对应高清影像图路径
                string savePhoto_now = GetFileName(savePhotoHead + baseName, court_needSolve, ".tiff");//当前扫描文件对应辅助图路径
                string savePhoto2_now = savePhoto_now.Replace(".tiff", "_huan.tiff");//当前扫描文件对应辅助图 (分环) 路径
                string savePhoto3_now = savePhoto_now.Replace(".tiff", "_mark.tiff");//当前扫描文件对应辅助图（错台）路径
                string savePhoto_pre = GetFileName(savePhotoHead + baseName, court_needSolve - 1, ".tiff");//上一个扫描文件对应辅助图路径
                string savedividePath = savePhoto2_now.Replace(".tiff", ".txt");//当前扫描文件对应辅助图 (分环) 对应配置文件
                string save3DPath = GetFileName(save3DHead + baseName, court_needSolve, ".tsd");//当前扫描文件解析后的点云存储路径
                string saveLmPath = GetFileName(save3DHead + baseName, court_needSolve, ".lm");//当前扫描文件解析后的里程信息存储路径

                long prelen = 0;//文件大小（辅助判断文件是否生成）
                ///先行判断扫描文件是否全部生成完毕，完毕则结束
                ///相对于下一步骤的必要判断，该步骤为非必要判断
                ///此处通过检测文件的逐渐生成来控制具体需等待时间
                ///如无该步骤，在下一步骤需要进行时间相对较长和严谨的判断
                while (!File.Exists(fileName_now))
                {
                    long fileLen1 = Text_IO.GetDirectorySize(loadFlsRoad);
                    Thread.Sleep(500);
                    long fileLen2 = Text_IO.GetDirectorySize(loadFlsRoad);
                    if (fileLen2 - fileLen1 < 10)
                    {
                        prelen++;
                    }
                    if (prelen > 5)
                    {
                        //文件停止生成！
                        string log = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + " " + fileName_now + " 无该文件，停止生成，程序结束！";
                        Text_IO.WriteLineToTxt(space_Analysis.WriteLogPath, log);

                        string theLastFile = GetFileName(save3DHead + baseName, court_needSolve - CutNum, ".tsds");
                        if (File.Exists(theLastFile)) File.Delete(theLastFile);//
                        return;
                    }
                }

                ///已经判断得到文件正生成，通过检测结束标记文件m2判断文件是否生成完毕。
                prelen = 0;
                if (!File.Exists(mileage2_now))
                {
                    while (true)
                    {
                        Thread.Sleep(10);
                        prelen++;
                        if (File.Exists(mileage2_now))
                        {
                            break;//检测文件生成了跳出，检测频率10ms
                        }
                        if (prelen > 12000)
                        {
                            //单个文件生成时间过长（大于2min），认定为结束任务扫描且里程无正常生成！
                            string log = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + " " + mileage_now + " 里程文件不正常生成，无法解析请后处理继续，程序结束！";
                            Text_IO.WriteLineToTxt(space_Analysis.WriteLogPath, log);
                            return;
                        }
                    }
                }
                //文件生成—>里程生成—>加载里程—>解析数据！
                ///逐个追加里程文件到mileage_all
                for (int i = mileage_path.Count + 1; i <= court_needSolve; i++)
                {
                    string m_path = loadMileageHead + i + ".txt";// GetFileName(loadMileageHead + "\\" , i, ".txt");
                    mileage_path.Add(m_path);
                    space_Analysis.LoadMileage(m_path, ref mileage_all);
                }
                ///如果是第二次扫描，则加载信息文件
                if (!isFirst && space_Analysis.Get_Autotime == 0)
                {
                    JiShu = 0;
                    while (true)
                    {
                        if (court_needSolve == 2) break;
                        try
                        {
                            Text_IO.ReadFile(space_Analysis.WriteMessagePath, out Ellipse ellipse, out ulong getAutoTime, out ulong firstTime, out ulong detaTime, out double TimePra, out int numcol, out int numrow);
                            space_Analysis.Get_Autotime = getAutoTime;
                            space_Analysis.Get_FirstAutoTime = firstTime;
                            space_Analysis.DetaScanTime = detaTime;
                            space_Analysis.ellipse = ellipse;
                            space_Analysis.Autotime_pra2 = TimePra;
                            preNumCol = numcol;
                            preNumRow = numrow;
                            if (space_Analysis.Get_Autotime == 0 && CutNum != 2) { Thread.Sleep(10000); continue; }
                            break;
                        }
                        catch
                        {
                            JiShu++;
                            Thread.Sleep(1000);
                            if (JiShu > 5) break;//延迟自动跳出
                        }
                    }
                }

                ///开始解析文件
                if (isLog) Text_IO.WriteLineToTxt(writeTimePath, DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + " 开始解析文件：" + fileName_now);
                if (space_Analysis.ET_NewAnalysis(fileName_now, mileage_all, startTime, Tunnel_Radius))
                {
                    ///如果是首文件，则给初始化 lm_pre 和 ap_pre
                    ///如果不是，则加载上一文件数据
                    if (isLog) Text_IO.WriteLineToTxt(writeTimePath, DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + " 解析完成");
                    if (isFirst) { preNumCol = space_Analysis.numCol; savePhoto_pre = null; space_Analysis.ET_INI(); }
                    int col = 0;//上一文件的截取部分圈数
                    double firstM = 0;//上一文件的截取部分里程

                    if (!isFirst)
                    {
                        string preSaveCloudPath = GetFileName(save3DHead + baseName, court_needSolve - 1, ".tsd");
                        string preSaveLmPath = GetFileName(save3DHead + baseName, court_needSolve - 1, ".lm");
                        string preSaveDividePath = GetFileName(savePhotoHead + baseName, court_needSolve - 1, "_huan.txt");
                        if (isLog) Text_IO.WriteLineToTxt(writeTimePath, DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + " 判断上一点云文件是否生成：" + preSaveCloudPath);
                        while (!File.Exists(preSaveLmPath))
                        {
                            Thread.Sleep(100);
                            col++;
                            if (col > 3000)
                            {
                                return;//程序中止?
                            }
                        }//只需判断LM文件是否生成，因为点云文件在LM文件之前生成，而LM文件生成较快，基本不会读写冲突

                        if (col > 3000) space_Analysis.ET_INI();
                        else
                        {
                            if (isLog) Text_IO.WriteLineToTxt(writeTimePath, DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + " 开始加载上一文件点云：" + preSaveDividePath);
                            Text_IO.ReadDivides(preSaveDividePath, out List<DivideLine> LDiv);//读取获取的分环信息
                            for (int i = LDiv.Count - 1; i >= 0; i--)
                            {
                                if (LDiv[i].attribute == "EndCol" && i != 0)//若无断面则舍去
                                {
                                    col = LDiv[i].ID;//“EndCol”存储的最后环的中间列。
                                    firstM = LDiv[i].point_data[0].x;
                                    if (firstM == 0) firstM += 0.00000001;
                                    break;//找到最后环缝
                                }
                            }
                            ///只加载上一个文件最后一个环的一半到末尾的数据
                            ///避免分环处理时因文件缺失而少分一环、两环
                            space_Analysis.ET_Read3DFileAndLm(preSaveCloudPath, preSaveLmPath, col, preNumCol);
                            if (isLog) Text_IO.WriteLineToTxt(writeTimePath, DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + " 加载上一文件点云完成：" + preSaveDividePath);
                        }
                    }
                    else
                    {
                        preNumCol = space_Analysis.numCol; savePhoto_pre = null; space_Analysis.ET_INI();
                    }
                    if (isLog) Text_IO.WriteLineToTxt(writeTimePath, DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + "开始成图");
                    space_Analysis.ET_GetPhoto_later(saveHighePhoto, saveHighePhoto.Replace(".tiff", ".txt"), Expand, Height);
                    if (isLog) Text_IO.WriteLineToTxt(writeTimePath, DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + " 成图完成：" + saveHighePhoto);
                    if (isLog) Text_IO.WriteLineToTxt(writeTimePath, DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + " 开始成分环图分环：" + savePhoto2_now);
                    List<double> lm_cut = new List<double>();//存储分环里程值
                    space_Analysis.ET_Divide0125(savePhoto2_now, Expand, Height, isFirst, out List<DivideLine> ldivide, HuanWidth);

                    if (isLog) Text_IO.WriteLineToTxt(writeTimePath, DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + " 成图完成，生成断面");
                    List<DivideLine> saveHuanDivide = ldivide.ToList();
                    for (int i = 0; i < ldivide.Count; i++)
                    {
                        if (ldivide[i].attribute == "Ring")
                        {
                            double wsum = 0; double hsum = 0;
                            foreach (var v in ldivide[i].point_data)
                            {
                                wsum += v.x; hsum += v.y;
                            }
                            lm_cut.Add(wsum / ldivide[i].point_data.Count);
                        }
                    }//获取环缝里程值

                    lm_cut = lm_cut.OrderBy(l => l).ToList();
                    if (firstM != 0 && lm_cut.Count != 0)
                    {
                        if (Math.Abs(firstM - lm_cut[0]) < 0.05)
                        {
                            lm_cut[0] = firstM;
                            Text_IO.WriteMileageToTxt(theHFmessage, lm_cut, court_needSolve.ToString());
                        }
                        else
                        {
                            Text_IO.WriteMileageToTxt(theHFmessage, lm_cut, court_needSolve.ToString());
                            lm_cut.Add(firstM);
                        }
                    }
                    else
                    {
                        Text_IO.WriteMileageToTxt(theHFmessage, lm_cut, court_needSolve.ToString());
                        lm_cut.Add(firstM);
                    }
                    Text_IO.WriteToJson(saveHuanDivide, out string jsontext);//写成JSON格式
                    Text_IO.WriteLineToTxt(savedividePath, jsontext, true);
                    space_Analysis.ET_GetSectionFile_test(lm_cut, saveSectionhead);//写出断面文件
                    if (isLog) Text_IO.WriteLineToTxt(writeTimePath, DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + " 断面完成，存储数据");
                    space_Analysis.ET_Save3DFile(save3DPath, the3Dmessage);
                    space_Analysis.ET_SaveLM(saveLmPath);
                    if (isLog) Text_IO.WriteLineToTxt(writeTimePath, DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + " 存储数据完成");
                    Console.WriteLine(fileName_now);
                }
                else
                {
                    string log = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + " " + fileName_now + " 文件解析失败，请后处理继续，程序结束！";
                    Text_IO.WriteLineToTxt(space_Analysis.WriteLogPath, log);
                    if (court_needSolve <= 2)
                    {
                        System.Windows.Forms.MessageBox.Show(log);
                        return;//前两个文件解析失败可能对后面文件有极大影响，需要弹出，其他文件解析失败可以重新解析
                    }

                    space_Analysis.ET_SaveLM(saveLmPath);
                }
                court_needSolve += CutNum;
                isFirst = false;
            }
        }
        /// <summary>
        /// 扫描数据后处理
        /// </summary>
        /// <param name="MILEAGE_Path">里程路径</param>
        /// <param name="CutNum">解析进程数</param>
        /// <param name="StartNum">第几个解析进程（从0计）</param>
        /// <param name="loadFlsRoad">扫描文件存储路径</param>
        /// <param name="dataSaveRoad">扫描解析数据存储路径</param>
        /// <param name="baseName">扫描基本名</param>
        /// <param name="startTime">扫描起始时间</param>
        /// <param name="Expand">分辨率（像素/米）</param>
        /// <param name="Height">影像高度</param>
        /// <param name="H_adjust">扫描仪离轨面高度</param>
        /// <param name="Tunnel_Radius">隧道半径</param>
        /// <param name="HuanWidth">扫描隧道环宽</param>
        public static void DoDataAanalysis_Later(string MILEAGE_Path, int CutNum, int StartNum, string loadFlsRoad, string dataSaveRoad, string baseName, long startTime, int Expand, int Height, double H_adjust, double Tunnel_Radius, double HuanWidth)
        {
            ///请参考DoDataAanalysis注释，除去里程加载不同外其余相同
            string loadMileageHead = dataSaveRoad + "Mileage\\";//里程路径
            string savePhotoHead = dataSaveRoad + "sPhoto\\";//辅助图/深度图等存储路径
            string path_Photo = dataSaveRoad + "Photo\\";//高清影像图路径
            string save3DHead = dataSaveRoad + "3D\\";//解析点云数据存储路径
            string path_Progress = dataSaveRoad + "Message\\";//扫描信息存储路径
            string saveSectionhead = dataSaveRoad + "Sec\\";//扫描解析断面存储路径
            int court_needSolve = StartNum + 1;//当前扫描序号（以1开始计数）
            string theHFmessage = path_Progress + "RingSeam_message.txt";//环缝信息保存路径
            string the3Dmessage = path_Progress + "CloudFile_message.txt";//解析文件里程信息存储路径
            string writeTimePath = path_Progress + StartNum + "程序.txt";//数据解析操作日志路径
            Space_Analysis space_Analysis = new Space_Analysis();
            space_Analysis.WriteMessagePath = path_Progress + "message.txt";
            space_Analysis.WriteLogPath = path_Progress + "LogMessage.txt";
            space_Analysis.writeTimePath = writeTimePath;
            space_Analysis.Hadjust = H_adjust;//扫描距离轨面高度

            List<string> mileage_path = new List<string>(); //扫描中生成的小里程数据路径集合
            List<Mileage> mileage_all = new List<Mileage>();//总里程数据
            bool isLog = true;//是否写日志
            bool isFirst = false;//是否是头个文件
            int preNumRow = 0;//上一个扫描文件行数
            int preNumCol = 0;//上一个扫描文件列数
            int JiShu = 0;//计数标志位

            ///如果是第一次解析则令isFirst = true
            ///否则持续监测 message.txt文件是否存在（存在则说明第一个文件已经解析了）
            ///存在后进一步判断是第二个文件还是之后的文件
            ///（1）如果是第二个文件，则监测_1.config文件再读取扫描仪起始时间和展开椭圆信息进行下一步操作
            ///（2）如果是之后的文件，则监测_2.config文件直到该文件生成，并读取扫描仪起始时间和展开椭圆信息
            if (StartNum == 0)
            {
                isFirst = true;
                try { if (File.Exists(theHFmessage)) File.Delete(theHFmessage); } catch { }
            }
            else
            {
                JiShu = 0;
                while (!File.Exists(space_Analysis.WriteMessagePath))
                {
                    Thread.Sleep(2000);
                    JiShu++;
                    if (JiShu > 300)
                    {
                        string log = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + " 无信息文件，请确认！";
                        Text_IO.WriteLineToTxt(space_Analysis.WriteLogPath, log);
                        System.Windows.Forms.MessageBox.Show(log);
                        return;
                    }
                }
                JiShu = 0;
                if (StartNum == 1)
                {
                    string readMessage1 = space_Analysis.WriteMessagePath.Replace(".txt", "_1.config");
                    while (!File.Exists(readMessage1))
                    {
                        Thread.Sleep(500);
                        JiShu++;
                        if (JiShu > 10)
                        {
                            string log = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + " 无信息文件" + readMessage1 + "，请确认！";
                            Text_IO.WriteLineToTxt(space_Analysis.WriteLogPath, log);
                            System.Windows.Forms.MessageBox.Show(log);
                            return;
                        }
                    }
                    while (true)
                    {
                        try
                        {
                            Text_IO.ReadFile(space_Analysis.WriteMessagePath, out Ellipse ellipse, out ulong getAutoTime, out ulong firstTime, out ulong detaTime, out double TimePra, out int numcol, out int numrow);
                            space_Analysis.Get_Autotime = getAutoTime;
                            space_Analysis.Get_FirstAutoTime = firstTime;
                            space_Analysis.DetaScanTime = detaTime;
                            space_Analysis.ellipse = ellipse;
                            space_Analysis.Autotime_pra2 = TimePra;
                            preNumCol = numcol;
                            preNumRow = numrow;
                            if (space_Analysis.Get_FirstAutoTime == 0 && CutNum != 2) { Thread.Sleep(10000); continue; }
                            break;
                        }
                        catch
                        {
                            JiShu++;
                            Thread.Sleep(1000);
                            if (JiShu > 5) break;//延迟自动跳出
                        }
                    }
                }
                else
                {
                    string readMessage2 = space_Analysis.WriteMessagePath.Replace(".txt", "_2.config");
                    while (!File.Exists(readMessage2))
                    {
                        Thread.Sleep(5000);
                        JiShu++;
                        if (JiShu > 100)
                        {
                            string log = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + " 无信息文件" + readMessage2 + "，请确认！";
                            Text_IO.WriteLineToTxt(space_Analysis.WriteLogPath, log);
                            System.Windows.Forms.MessageBox.Show(log);
                            return;
                        }
                    }
                    while (true)
                    {
                        try
                        {
                            Text_IO.ReadFile(space_Analysis.WriteMessagePath, out Ellipse ellipse, out ulong getAutoTime, out ulong firstTime, out ulong detaTime, out double TimePra, out int numcol, out int numrow);
                            space_Analysis.Get_Autotime = getAutoTime;
                            space_Analysis.Get_FirstAutoTime = firstTime;
                            space_Analysis.DetaScanTime = detaTime;
                            space_Analysis.ellipse = ellipse;
                            space_Analysis.Autotime_pra2 = TimePra;
                            preNumCol = numcol;
                            preNumRow = numrow;
                            if (space_Analysis.Get_Autotime == 0 && CutNum != 2) { Thread.Sleep(10000); continue; }
                            break;
                        }
                        catch
                        {
                            JiShu++;
                            Thread.Sleep(1000);
                            if (JiShu > 5) break;//延迟自动跳出
                        }
                    }
                }

            }//读取扫描仪起始时间和展开椭圆信息

            ///加载里程文件
            JiShu = 0;
            while (true)
            {
                if (StartNum % 3 == 1) Thread.Sleep(2000);
                else if (StartNum % 3 == 2) Thread.Sleep(4000);
                space_Analysis.LoadAllMileage(MILEAGE_Path, out mileage_all);
                if (mileage_all.Count > 0) break;
                else
                {
                    Thread.Sleep(1000);
                }
                JiShu++;
                if (JiShu > 90)
                {
                    string log_m = "里程文件" + MILEAGE_Path + "被占用！程序终止！";
                    Console.WriteLine(log_m);
                    Text_IO.WriteLineToTxt(space_Analysis.WriteLogPath, log_m);
                    return;
                }
            }
            ///进入循环，按照CutNum间隔逐个解析文件
            while (true)
            {
                string fileName_now = GetFileName(loadFlsRoad + baseName, court_needSolve, ".fls");//当前扫描文件路径
                string mileage_now = loadMileageHead + court_needSolve + ".txt";//当前扫描文件对应里程文件路径
                string mileage2_now = mileage_now.Replace(".txt", "_2.txt");//当前扫描文件对应里程文件对应的配置文件路径，如存在则说明里程文件已生成
                string mileage_pre = GetFileName(loadMileageHead + baseName, court_needSolve - 1, ".txt");//上一个扫描文件对应的里程文件路径
                string saveHighePhoto = GetFileName(path_Photo + baseName, court_needSolve, ".tiff");//当前扫描文件对应高清影像图路径
                string savePhoto_now = GetFileName(savePhotoHead + baseName, court_needSolve, ".tiff");//当前扫描文件对应辅助图路径
                string savePhoto2_now = savePhoto_now.Replace(".tiff", "_huan.tiff");//当前扫描文件对应辅助图 (分环) 路径
                string savePhoto3_now = savePhoto_now.Replace(".tiff", "_mark.tiff");//当前扫描文件对应辅助图（错台）路径
                string savePhoto_pre = GetFileName(savePhotoHead + baseName, court_needSolve - 1, ".tiff");//上一个扫描文件对应辅助图路径
                string savedividePath = savePhoto2_now.Replace(".tiff", ".txt");//当前扫描文件对应辅助图 (分环) 对应配置文件
                string save3DPath = GetFileName(save3DHead + baseName, court_needSolve, ".tsd");//当前扫描文件解析后的点云存储路径
                string saveLmPath = GetFileName(save3DHead + baseName, court_needSolve, ".lm");//当前扫描文件解析后的里程信息存储路径

                long prelen = 0;//文件大小（辅助判断文件是否生成）
                ///先行判断扫描文件是否全部生成完毕，完毕则结束
                ///相对于下一步骤的必要判断，该步骤为非必要判断
                ///此处通过检测文件的逐渐生成来控制具体需等待时间
                ///如无该步骤，在下一步骤需要进行时间相对较长和严谨的判断
                while (!File.Exists(fileName_now))
                {
                    long fileLen1 = Text_IO.GetDirectorySize(loadFlsRoad);
                    Thread.Sleep(50);
                    long fileLen2 = Text_IO.GetDirectorySize(loadFlsRoad);
                    if (fileLen2 - fileLen1 < 10)
                    {
                        prelen++;
                    }
                    if (prelen > 5)
                    {
                        string preSaveCloudPathS = GetFileName(save3DHead + baseName, court_needSolve - 1, ".tsds");
                        if (File.Exists(preSaveCloudPathS))
                        {
                            Thread.Sleep(15000);
                            File.Delete(preSaveCloudPathS);
                        }
                        //文件停止生成！
                        string log = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + " " + fileName_now + " 无该文件，停止生成，程序结束！";
                        Text_IO.WriteLineToTxt(space_Analysis.WriteLogPath, log);
                        return;
                    }
                }

                ///已经判断得到文件正生成，通过检测结束标记文件m2判断文件是否生成完毕。
                prelen = 0;
                if (!isFirst && space_Analysis.Get_Autotime == 0)
                {
                    JiShu = 0;
                    while (true)
                    {
                        if (court_needSolve == 2) break;
                        try
                        {
                            Text_IO.ReadFile(space_Analysis.WriteMessagePath, out Ellipse ellipse, out ulong getAutoTime, out ulong firstTime, out ulong detaTime, out double TimePra, out int numcol, out int numrow);
                            space_Analysis.Get_Autotime = getAutoTime;
                            space_Analysis.Get_FirstAutoTime = firstTime;
                            space_Analysis.DetaScanTime = detaTime;
                            space_Analysis.ellipse = ellipse;
                            //space_Analysis.Autotime_pra = TimePra;
                            preNumCol = numcol;
                            preNumRow = numrow;
                            if (space_Analysis.Get_Autotime == 0 && CutNum != 2) { Thread.Sleep(10000); continue; }
                            break;
                        }
                        catch
                        {
                            JiShu++;
                            Thread.Sleep(1000);
                            if (JiShu > 5) break;//延迟自动跳出
                        }
                    }
                }//新扫描仪数据

                if (isLog) Text_IO.WriteLineToTxt(writeTimePath, DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + " 开始解析文件：" + fileName_now);
                if (space_Analysis.ET_NewAnalysis(fileName_now, mileage_all, startTime, Tunnel_Radius))
                {
                    if (isLog) Text_IO.WriteLineToTxt(writeTimePath, DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + " 解析文件完成,开始生成影像图：" + saveHighePhoto);
                    space_Analysis.ET_GetPhoto_later(saveHighePhoto, saveHighePhoto.Replace(".tiff", ".txt"), Expand, Height);
                    if (isLog) Text_IO.WriteLineToTxt(writeTimePath, DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + " 完成生成影像图：" + saveHighePhoto);

                    int col = 0;
                    double firstM = 0;
                    if (!isFirst)
                    {
                        if (isLog) Text_IO.WriteLineToTxt(writeTimePath, DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + " 非首文件，等待上一文件点云生成");
                        string preSaveCloudPath = GetFileName(save3DHead + baseName, court_needSolve - 1, ".tsd");
                        string preSaveLmPath = GetFileName(save3DHead + baseName, court_needSolve - 1, ".lm");
                        string preSaveDividePath = GetFileName(savePhotoHead + baseName, court_needSolve - 1, "_huan.txt");
                        //space_Analysis.ET_INI();
                        while (!File.Exists(preSaveLmPath))
                        {
                            Thread.Sleep(100);
                            col++;
                            if (col > 3000)
                            {

                                break;
                                //return;//程序中止?
                            }
                        }//只需判断LM文件是否生成，因为点云文件在LM文件之前生成
                        if (col > 3000) space_Analysis.ET_INI();
                        else
                        {
                            if (isLog) Text_IO.WriteLineToTxt(writeTimePath, DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + " 开始加载上一文件点云：" + preSaveDividePath);
                            Text_IO.ReadDivides(preSaveDividePath, out List<DivideLine> LDiv);//读取获取的分环信息
                            for (int i = LDiv.Count - 1; i >= 0; i--)
                            {
                                if (LDiv[i].attribute == "EndCol" && i != 0)//若无断面则舍去
                                {
                                    col = LDiv[i].ID;//“EndCol”存储的最后环的中间列。
                                    firstM = LDiv[i].point_data[0].x;
                                    if (firstM == 0) firstM += 0.00000001;
                                    break;//找到最后hua
                                }
                            }

                            space_Analysis.ET_Read3DFileAndLm(preSaveCloudPath, preSaveLmPath, col, preNumCol);
                            //space_Analysis.ET_Read3DFile(preSaveCloudPath, col, preNumCol);//从上一个点云文件中读取需要用到的点云
                            //space_Analysis.ET_ReadLmFile(preSaveLmPath);
                            if (isLog) Text_IO.WriteLineToTxt(writeTimePath, DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + " 加载上一文件点云完成：" + preSaveDividePath);
                        }

                    }
                    else
                    {
                        savePhoto_pre = null;
                        preNumCol = space_Analysis.numCol;
                        space_Analysis.ET_INI();
                    }

                    if (isLog) Text_IO.WriteLineToTxt(writeTimePath, DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + " 开始成图识别环：" + savePhoto2_now);
                    List<double> lm_cut = new List<double>();//存储分环里程值
                    space_Analysis.ET_Divide0125(savePhoto2_now, Expand, Height, isFirst, out List<DivideLine> ldivide, HuanWidth);

                    if (isLog) Text_IO.WriteLineToTxt(writeTimePath, DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + " 完成成图识别环并记录：" + savePhoto2_now);
                    List<DivideLine> saveHuanDivide = ldivide.ToList();
                    for (int i = 0; i < ldivide.Count; i++)
                    {
                        if (ldivide[i].attribute == "Ring")
                        {
                            double wsum = 0; double hsum = 0;
                            foreach (var v in ldivide[i].point_data)
                            {
                                wsum += v.x; hsum += v.y;
                            }
                            lm_cut.Add(wsum / ldivide[i].point_data.Count);
                        }
                    }//获取环缝里程值

                    lm_cut = lm_cut.OrderBy(l => l).ToList();
                    if (firstM != 0 && lm_cut.Count != 0)
                    {
                        if (Math.Abs(firstM - lm_cut[0]) < 0.05)
                        {
                            lm_cut[0] = firstM;
                            Text_IO.WriteMileageToTxt(theHFmessage, lm_cut, court_needSolve.ToString());
                        }
                        else
                        {
                            Text_IO.WriteMileageToTxt(theHFmessage, lm_cut, court_needSolve.ToString());
                            lm_cut.Add(firstM);
                        }
                    }
                    else
                    {
                        Text_IO.WriteMileageToTxt(theHFmessage, lm_cut, court_needSolve.ToString());
                        lm_cut.Add(firstM);
                    }
                    Text_IO.WriteToJson(saveHuanDivide, out string jsontext);//写成JSON格式
                    Text_IO.WriteLineToTxt(savedividePath, jsontext, true);//写出

                    if (isLog) Text_IO.WriteLineToTxt(writeTimePath, DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + " 完成记录识别环缝，开始生成断面文件");

                    space_Analysis.ET_GetSectionFile(firstM, ldivide, saveSectionhead);//写出断面文件
                    if (isLog) Text_IO.WriteLineToTxt(writeTimePath, DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + " 断面文件生成：" + saveSectionhead);
                    if (isLog) Text_IO.WriteLineToTxt(writeTimePath, DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + " 开始存储点云文件：" + save3DPath);
                    space_Analysis.ET_Save3DFile(save3DPath, the3Dmessage);
                    space_Analysis.ET_SaveLM(saveLmPath);
                    if (isLog) Text_IO.WriteLineToTxt(writeTimePath, DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + " 点云文件存储完成：" + save3DPath);
                    Console.WriteLine(court_needSolve);
                }
                else
                {
                    string log = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + " " + fileName_now + " 文件解析失败，请后处理继续，程序结束！";
                    Text_IO.WriteLineToTxt(space_Analysis.WriteLogPath, log);
                    System.Windows.Forms.MessageBox.Show(log);
                    return;
                }
                court_needSolve += CutNum;
                isFirst = false;
            }

        }
        /// <summary>
        /// 断面文件生成
        /// </summary>
        /// <param name="path1">断面信息文件路径</param>
        /// <param name="path2">点云信息文件路径</param>
        /// <param name="secFileHead">断面保存文件夹</param>
        /// <param name="Expand">分辨率（像素/米）</param>
        public static void BuildSection(string path1, string path2, string secFileHead, double Expand)
        {
            Text_IO.ReadDoMessage(path1, out List<Do_Message> ldm1);
            Text_IO.ReadDoMessage(path2, out List<Do_Message> DoFilePath1);
            try
            {
                int Num_huan = ldm1.Count;
                List<Faro_point> LP_all = new List<Faro_point>();
                for (int kk = 0; kk < ldm1.Count; kk++)
                {
                    int i = kk;
                    ldm1[i].y2 = ldm1[i].y0 + 0.05;
                    ldm1[i].y1 = ldm1[i].y0 - 0.05;
                    try
                    {
                        List<Faro_point> lp = new List<Faro_point>();
                        for (int j = 0; j < DoFilePath1.Count; j++)
                        {
                            if ((DoFilePath1[j].y1 < ldm1[i].y2 && DoFilePath1[j].y2 >= ldm1[i].y2) || (DoFilePath1[j].y1 <= ldm1[i].y1 && DoFilePath1[j].y2 > ldm1[i].y1 || (DoFilePath1[j].y1 >= ldm1[i].y1 && DoFilePath1[j].y2 <= ldm1[i].y2)))
                            {
                                string path = DoFilePath1[j].NameOrPoint;
                                Text_IO.ReadS3D_huan(path, ldm1[i].y1, ldm1[i].y2, out List<Faro_point> lp1);
                                lp = lp.Concat(lp1).ToList();
                            }
                        }

                        string savesec = secFileHead + "//" + (int)(ldm1[kk].y0 * 1000) + "mm.txt";
                        SectionAnalysis.Section_Get_0201(lp, savesec, 0.75 * Expand / 1000);

                    }
                    catch (Exception eee)
                    {
                        System.Windows.Forms.MessageBox.Show("提取" + (int)(ldm1[kk].y0 * 1000) + "mm断面失败:" + eee.Message);
                    }

                    Console.WriteLine("+1");
                }
            }
            catch (Exception e)
            {
                System.Windows.Forms.MessageBox.Show("提取断面失败:" + e.Message);
            }
        }
        /// <summary>
        /// 获取磁盘内存（暂无使用）
        /// </summary>
        /// <param name="str_HardDiskName">磁盘名</param>
        /// <returns></returns>
        public static long GetHardDiskSpace(string str_HardDiskName)
        {
            long totalSize = 0;
            str_HardDiskName = str_HardDiskName + ":\\";
            System.IO.DriveInfo[] drives = System.IO.DriveInfo.GetDrives();
            foreach (System.IO.DriveInfo drive in drives)
            {
                if (drive.Name == str_HardDiskName)
                {
                    totalSize = drive.TotalFreeSpace / (1024 * 1024);
                }
            }
            return totalSize;
        }
        /// <summary>
        /// 实时限界处理
        /// </summary>
        /// <param name="LimitRoad">限界文件</param>
        /// <param name="CutNum">解析进程数</param>
        /// <param name="StartNum">第几个解析进程（从0计）</param>
        /// <param name="loadFlsRoad">扫描文件存储路径</param>
        /// <param name="dataSaveRoad">扫描解析数据存储路径</param>
        /// <param name="baseName">扫描基本名</param>
        /// <param name="startTime">扫描起始时间</param>
        /// <param name="Expand">分辨率（像素/米）</param>
        /// <param name="Height">影像高度</param>
        /// <param name="H_adjust">扫描仪离轨面高度</param>
        /// <param name="Tunnel_Radius">扫描隧道半径</param>
        /// <param name="HuanWidth">扫描隧道环宽</param>
        public static void DoDataAnalysis_limit(string LimitRoad, int CutNum, int StartNum, string loadFlsRoad, string dataSaveRoad, string baseName, long startTime, int Expand, int Height, double H_adjust, double Tunnel_Radius, double HuanWidth)
        {
            ///初始化一些路径...
            string loadMileageHead = dataSaveRoad + "Mileage\\";//里程路径
            string savePhotoHead = dataSaveRoad + "sPhoto\\";//辅助图/深度图等存储路径
            string path_Photo = dataSaveRoad + "Photo\\";//高清影像图路径
            string save3DHead = dataSaveRoad + "3D\\";//解析点云数据存储路径
            string path_Progress = dataSaveRoad + "Message\\";//扫描信息存储路径
            string saveSectionhead = dataSaveRoad + "Sec\\";//扫描解析断面存储路径
            string save_limit_jpg = dataSaveRoad + "Limit\\";   // 实时显示断面扫描图片

            int court_needSolve = StartNum + 1;//当前扫描序号（以1开始计数）
            string theHFmessage = path_Progress + "RingSeam_message.txt";//环缝信息保存路径
            string the3Dmessage = path_Progress + "CloudFile_message.txt";//解析文件里程信息存储路径
            string writeTimePath = path_Progress + StartNum + "程序.txt";//数据解析操作日志路径
            Space_Analysis space_Analysis = new Space_Analysis();
            space_Analysis.WriteMessagePath = path_Progress + "message.txt";
            space_Analysis.WriteLogPath = path_Progress + "LogMessage.txt";
            space_Analysis.Write_jpg_log = save_limit_jpg + "limit_log.txt";  // 限界日志输出图片
            space_Analysis.Write_jpgPath = save_limit_jpg;

            space_Analysis.writeTimePath = writeTimePath;
            space_Analysis.Hadjust = H_adjust;//扫描距离轨面高度

            space_Analysis.ReadLimitFile(LimitRoad, out List<Faro_point> limitdata0);
            space_Analysis.GLimitData0 = limitdata0;

            List<string> mileage_path = new List<string>(); //扫描中生成的小里程数据路径集合
            List<Mileage> mileage_all = new List<Mileage>();//总里程数据
            bool isLog = true;//是否写日志
            bool isFirst = false;//是否是头个文件
            int preNumRow = 0;//上一个扫描文件行数
            int preNumCol = 0;//上一个扫描文件列数
            int JiShu = 0;//计数标志位

            ///如果是第一次解析则令isFirst = true
            ///否则持续监测 message.txt文件是否存在（存在则说明第一个文件已经解析了）
            ///存在后进一步判断是第二个文件还是之后的文件
            ///（1）如果是第二个文件，则监测_1.config文件再读取扫描仪起始时间和展开椭圆信息进行下一步操作
            ///（2）如果是之后的文件，则监测_2.config文件直到该文件生成，并读取扫描仪起始时间和展开椭圆信息
            if (StartNum == 0) isFirst = true;
#if true
            else
            {
                JiShu = 0;
                while (!File.Exists(space_Analysis.WriteMessagePath))
                {
                    Thread.Sleep(2000);
                    JiShu++;
                    if (JiShu > 300)
                    {
                        string log = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + " 无信息文件，请确认！";
                        Text_IO.WriteLineToTxt(space_Analysis.WriteLogPath, log);
                        System.Windows.Forms.MessageBox.Show(log);
                        return;
                    }
                }
                JiShu = 0;
                if (StartNum == 1)
                {
                    string readMessage1 = space_Analysis.WriteMessagePath.Replace(".txt", "_1.config");
                    while (!File.Exists(readMessage1))
                    {
                        Thread.Sleep(500);
                        JiShu++;
                        if (JiShu > 10)
                        {
                            string log = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + " 无信息文件" + readMessage1 + "，请确认！";
                            Text_IO.WriteLineToTxt(space_Analysis.WriteLogPath, log);
                            System.Windows.Forms.MessageBox.Show(log);
                            return;
                        }
                    }
                    while (true)
                    {
                        try
                        {
                            Text_IO.ReadFile(space_Analysis.WriteMessagePath, out Ellipse ellipse, out ulong getAutoTime, out ulong firstTime, out ulong detaTime, out double TimePra, out int numcol, out int numrow);
                            space_Analysis.Get_Autotime = getAutoTime;
                            space_Analysis.Get_FirstAutoTime = firstTime;
                            space_Analysis.DetaScanTime = detaTime;
                            space_Analysis.ellipse = ellipse;
                            //space_Analysis.Autotime_pra = TimePra;
                            preNumCol = numcol;
                            preNumRow = numrow;
                            if (space_Analysis.Get_FirstAutoTime == 0 && CutNum != 2) { Thread.Sleep(10000); continue; }
                            break;
                        }
                        catch
                        {
                            JiShu++;
                            Thread.Sleep(1000);
                            if (JiShu > 5) break;//延迟自动跳出
                        }
                    }
                }
                else
                {
                    string readMessage2 = space_Analysis.WriteMessagePath.Replace(".txt", "_2.config");
                    while (!File.Exists(readMessage2))
                    {
                        Thread.Sleep(5000);
                        JiShu++;
                        if (JiShu > 100)
                        {
                            string log = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + " 无信息文件" + readMessage2 + "，请确认！";
                            Text_IO.WriteLineToTxt(space_Analysis.WriteLogPath, log);
                            System.Windows.Forms.MessageBox.Show(log);
                            return;
                        }
                    }
                    while (true)
                    {
                        try
                        {
                            Text_IO.ReadFile(space_Analysis.WriteMessagePath, out Ellipse ellipse, out ulong getAutoTime, out ulong firstTime, out ulong detaTime, out double TimePra, out int numcol, out int numrow);
                            space_Analysis.Get_Autotime = getAutoTime;
                            space_Analysis.Get_FirstAutoTime = firstTime;
                            space_Analysis.DetaScanTime = detaTime;
                            space_Analysis.ellipse = ellipse;
                            //space_Analysis.Autotime_pra = TimePra;
                            preNumCol = numcol;
                            preNumRow = numrow;
                            if (space_Analysis.Get_Autotime == 0 && CutNum != 2) { Thread.Sleep(10000); continue; }
                            break;
                        }
                        catch
                        {
                            JiShu++;
                            Thread.Sleep(1000);
                            if (JiShu > 5) break;//延迟自动跳出
                        }
                    }
                }
            }
#endif
            ///进入循环，按照CutNum间隔逐个解析文件
            while (true)
            {
                string fileName_now = GetFileName(loadFlsRoad + baseName, court_needSolve, ".fls");//当前扫描文件路径
                string mileage_now = loadMileageHead + court_needSolve + ".txt";//当前扫描文件对应里程文件路径
                string mileage2_now = mileage_now.Replace(".txt", "_2.txt");//当前扫描文件对应里程文件对应的配置文件路径，如存在则说明里程文件已生成
                string mileage_pre = GetFileName(loadMileageHead + baseName, court_needSolve - 1, ".txt");//上一个扫描文件对应的里程文件路径

                string saveHighePhoto = GetFileName(path_Photo + baseName, court_needSolve, ".tiff");//当前扫描文件对应高清影像图路径
                string savePhoto_now = GetFileName(savePhotoHead + baseName, court_needSolve, ".tiff");//当前扫描文件对应辅助图路径
                string savePhoto2_now = savePhoto_now.Replace(".tiff", "_huan.tiff");//当前扫描文件对应辅助图 (分环) 路径
                string savePhoto3_now = savePhoto_now.Replace(".tiff", "_mark.tiff");//当前扫描文件对应辅助图（错台）路径
                string savePhoto_pre = GetFileName(savePhotoHead + baseName, court_needSolve - 1, ".tiff");//上一个扫描文件对应辅助图路径
                string savedividePath = savePhoto2_now.Replace(".tiff", ".txt");//当前扫描文件对应辅助图 (分环) 对应配置文件
                string save3DPath = GetFileName(save3DHead + baseName, court_needSolve, ".tsd");//当前扫描文件解析后的点云存储路径
                string saveLmPath = GetFileName(save3DHead + baseName, court_needSolve, ".lm");//当前扫描文件解析后的里程信息存储路径

                long prelen = 0;//文件大小（辅助判断文件是否生成）
                ///先行判断扫描文件是否全部生成完毕，完毕则结束
                ///相对于下一步骤的必要判断，该步骤为非必要判断
                ///此处通过检测文件的逐渐生成来控制具体需等待时间
                ///如无该步骤，在下一步骤需要进行时间相对较长和严谨的判断
                while (!File.Exists(fileName_now))
                {
                    long fileLen1 = Text_IO.GetDirectorySize(loadFlsRoad);
                    Thread.Sleep(500);
                    long fileLen2 = Text_IO.GetDirectorySize(loadFlsRoad);
                    if (fileLen2 - fileLen1 < 10)
                    {
                        prelen++;
                    }
                    if (prelen > 5)
                    {
                        //文件停止生成！
                        string log = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + " " + fileName_now + " 无该文件，停止生成，程序结束！";
                        Text_IO.WriteLineToTxt(space_Analysis.WriteLogPath, log);

                        string theLastFile = GetFileName(save3DHead + baseName, court_needSolve - CutNum, ".tsds");
                        if (File.Exists(theLastFile)) File.Delete(theLastFile);//
                        return;
                    }
                }

                ///已经判断得到文件正生成，通过检测结束标记文件m2判断文件是否生成完毕。
                prelen = 0;
                if (!File.Exists(mileage2_now))
                {
                    while (true)
                    {
                        Thread.Sleep(10);
                        prelen++;
                        if (File.Exists(mileage2_now))
                        {
                            break;//检测文件生成了跳出，检测频率10ms
                        }
                        if (prelen > 12000)
                        {
                            //单个文件生成时间过长（大于2min），认定为结束任务扫描且里程无正常生成！
                            string log = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + " " + mileage_now + " 里程文件不正常生成，无法解析请后处理继续，程序结束！";
                            Text_IO.WriteLineToTxt(space_Analysis.WriteLogPath, log);
                            return;
                        }
                    }
                }
                //文件生成—>里程生成—>加载里程—>解析数据！
                ///逐个追加里程文件到mileage_all
                for (int i = mileage_path.Count + 1; i <= court_needSolve; i++)
                {
                    string m_path = loadMileageHead + i + ".txt";// GetFileName(loadMileageHead + "\\" , i, ".txt");
                    mileage_path.Add(m_path);
                    space_Analysis.LoadMileage(m_path, ref mileage_all);
                }
                ///如果是第二次扫描，则加载信息文件
                if (!isFirst && space_Analysis.Get_Autotime == 0)
                {
                    JiShu = 0;
                    while (true)
                    {
                        if (court_needSolve == 2) break;
                        try
                        {
                            Text_IO.ReadFile(space_Analysis.WriteMessagePath, out Ellipse ellipse, out ulong getAutoTime, out ulong firstTime, out ulong detaTime, out double TimePra, out int numcol, out int numrow);
                            space_Analysis.Get_Autotime = getAutoTime;
                            space_Analysis.Get_FirstAutoTime = firstTime;
                            space_Analysis.DetaScanTime = detaTime;
                            space_Analysis.ellipse = ellipse;
                            space_Analysis.Autotime_pra2 = TimePra;
                            preNumCol = numcol;
                            preNumRow = numrow;
                            if (space_Analysis.Get_Autotime == 0 && CutNum != 2) { Thread.Sleep(10000); continue; }
                            break;
                        }
                        catch
                        {
                            JiShu++;
                            Thread.Sleep(1000);
                            if (JiShu > 5) break;//延迟自动跳出
                        }
                    }
                }
                //startTime = mileage_all[0].Time;
                ///开始解析文件
                if (isLog) Text_IO.WriteLineToTxt(writeTimePath, DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + " 开始解析文件：" + fileName_now);
                if (space_Analysis.ET_Analysis_Limit(fileName_now, mileage_all, startTime, Tunnel_Radius))
                {
                    ///如果是首文件，则给初始化 lm_pre 和 ap_pre
                    ///如果不是，则加载上一文件数据
                    if (isLog) Text_IO.WriteLineToTxt(writeTimePath, DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + " 解析完成");
                    if (isFirst) { preNumCol = space_Analysis.numCol; savePhoto_pre = null; space_Analysis.ET_INI(); }
                    int col = 0;//上一文件的截取部分圈数
                    double firstM = 0;//上一文件的截取部分里程

                    if (isLog) Text_IO.WriteLineToTxt(writeTimePath, DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + "开始成图");
                    space_Analysis.ET_GetPhoto_later(saveHighePhoto, saveHighePhoto.Replace(".tiff", ".txt"), Expand, Height);
                    if (isLog) Text_IO.WriteLineToTxt(writeTimePath, DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + " 成图完成：" + saveHighePhoto);



                    space_Analysis.ET_Save3DFile(save3DPath, the3Dmessage);
                    space_Analysis.ET_SaveLM(saveLmPath);
                    if (isLog) Text_IO.WriteLineToTxt(writeTimePath, DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + " 存储数据完成");
                    Console.WriteLine(fileName_now);
                }
                else
                {
                    string log = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + " " + fileName_now + " 文件解析失败，请后处理继续，程序结束！";
                    Text_IO.WriteLineToTxt(space_Analysis.WriteLogPath, log);
                    if (court_needSolve <= 2)
                    {
                        System.Windows.Forms.MessageBox.Show(log);
                        return;//前两个文件解析失败可能对后面文件有极大影响，需要弹出，其他文件解析失败可以重新解析
                    }

                    space_Analysis.ET_SaveLM(saveLmPath);
                }
                court_needSolve += CutNum;
                isFirst = false;
            }
        }
    }
}
