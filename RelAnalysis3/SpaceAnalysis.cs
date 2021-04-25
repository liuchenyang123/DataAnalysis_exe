using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DLLForFls;
namespace RelAnalysis3
{
    /// <summary>
    /// 空间数据解析
    /// 注：调用DLLForFls.dll而不是FARO自带的SDKdll是
    /// 由于faro的sdk只支持.net3.5以下的版本，
    /// 所以将一些用到的方法封装成dll方便在高版本.net中调用
    /// dll具体打包项目可查看文档，调用的一些函数在项目中有注释
    /// 因此当前页无每个都注释，关于SDK更多资料可查看文档/FARO_LS_SDK_6.0_Manual_EN.pdf
    /// </summary>
    public class Space_Analysis
    {
        /// <summary>
        /// 初始化FARO类
        /// </summary>
        public Space_Analysis()
        {
            //初始化FARO类
            if (!OpenFls.IsSuccess)
                OpenFls.Authorize();
        }

        #region 公共变量
        /// <summary>
        /// 椭圆断面结构信息（默认为null）
        /// </summary>
        public Ellipse ellipse;
        /// <summary>
        /// 用于同步实际时间扫描数据自动化时间（第二个文件生成时予以矫正）
        /// </summary>
        public ulong Get_Autotime = 0;
        /// <summary>
        /// 首文件起始时间
        /// </summary>
        public ulong Get_FirstAutoTime = 0;
        /// <summary>
        /// 用于辅助计算新扫描仪时间（详见文档）
        /// 对新Faro扫描仪S150APlus型号的螺旋扫描数据进行解析后发现第一个文件的时间和之后文件的时间不连续
        /// 因此需要对第一个文件和后面文件分开处理，设置DetaScanTime一个时间差
        /// </summary>
        public ulong DetaScanTime = 0;
        /// <summary>
        /// 写出日志路径
        /// </summary>
        public string WriteLogPath = "";

        /// <summary>
        /// 实时生成限界图片路径
        /// </summary>
        public string Write_jpgPath = "";

        /// <summary>
        /// 实时生成限界日志路径
        /// </summary>
        public string Write_jpg_log = "";

        /// <summary>
        /// 写出信息文件路径
        /// </summary>
        public string WriteMessagePath = "";
        /// <summary>
        /// 数据解析操作日志
        /// </summary>
        public string writeTimePath = "";
        /// <summary>
        /// 是否为椭圆（暂无使用）
        /// </summary>
        bool IsEllipse = false;
        /// <summary>
        /// 扫描列数
        /// </summary>
        public int numCol = 0;
        /// <summary>
        /// 扫描行数
        /// </summary>
        public int numRow = 0;
        /// <summary>
        /// 扫描仪时间单位（）
        /// </summary>
        public double Autotime_pra2 = 10;
        public static double Autotime_pra = 10;
        //int HY_index=0;//记录当前管片序号
        /// <summary>
        /// 单个文件数据 
        /// </summary>
        Faro_point[][] ap;

        /// <summary>
        /// 一圈限界数据是一个链表，数组保存2500个链表 下标用圈号对应
        /// </summary>
        List<Faro_point>[] limit_quan;

        /// <summary>
        /// 中间变量 负责记录侵界里程信息
        /// </summary>
        double km = 0;
        

        /// <summary>
        /// 保存
        /// </summary>
        List<Faro_point>[] LimitData_arr;

        /// <summary>
        /// 单个文件里程及点间距信息
        /// </summary>
        List<MileageEllipse> lme;
        /// <summary>
        /// 判断已完成线程数量
        /// </summary>
        int over;
        /// <summary>
        /// 当前文件起始里程
        /// </summary>
        double m1 = 0;
        /// <summary>
        /// 当前文件终止里程
        /// </summary>
        double m2 = 0;
        /// <summary>
        /// 上个文件点云数据
        /// </summary>
        Faro_point[][] ap_pre;
        /// <summary>
        /// 上个文件里程及点间距信息
        /// </summary>
        List<MileageEllipse> lm_pre;
        /// <summary>
        /// 椭圆断面结构信息
        /// </summary>
        public static Ellipse ep_0;
        #endregion

        #region 解析

        public double Hadjust = 0.68;//扫描仪中心距离轨面高度
        /// <summary>
        /// 数据解析
        /// </summary>
        /// <param name="Fls_path">扫描文件路径</param>
        /// <param name="mileage">里程数据</param>
        /// <param name="startTime">扫描起始时间</param>
        /// <param name="TunnelRadius">隧道半径（用于椭圆拟合失败时生成一个预设展开椭圆）</param>
        /// <returns></returns>
        public bool ET_NewAnalysis(string Fls_path, List<Mileage> mileage, long startTime, double TunnelRadius = 2.7)
        {
            try
            {
                ///统计每次解析时间并记录（可去除）
                if (writeTimePath != "") Text_IO.WriteLineToTxt(writeTimePath, DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + " StartLoad：" + Fls_path);
                OpenFls.libRef.load(Fls_path);//加载扫描文件到内存（时间较长）
                if (writeTimePath != "") Text_IO.WriteLineToTxt(writeTimePath, DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + " EndLoad：" + Fls_path);
                OpenFls.libRef.scanReflectionMode = 1;
                numCol = OpenFls.libRef.getScanNumCols(0);//扫描列数
                numRow = OpenFls.libRef.getScanNumRows(0);//扫描行数
                Array position = Array.CreateInstance(typeof(double), numRow * 3);
                Array color = Array.CreateInstance(typeof(int), numRow);
                Array polar = Array.CreateInstance(typeof(double), numRow * 3);
                Array refl = Array.CreateInstance(typeof(int), numRow);
                ulong ScanHeadTime = Get_Autotime;
                //扫描文件按一定列数分割分配里程，每部分相关参数
                int numSplit = 50;//分割列数*0.5
                int splitMap = numSplit;//计算标志位，当前设定为50圈
                ulong at_s;//起始自动化时间
                double Mileage_s = 0;//起始里程
                ulong at_Max;//终止自动化时间
                double Mileage_e;//终止里程
                double dy = 0;
                double dm = 0;
                double ddm = 0;
                int num = 0;
                int num_1 = numRow / 150;//每度含点数
                int gap = Math.Max(1, num_1 * 2 / 10);//大概保证两度内取10个点时的间隔
                int ind_d1 = num_1 * 5, ind_d2 = num_1 * 12;//取5度和12 度处点做判断
                m1 = 0; m2 = 0;
                List<BaseXY> lb = new List<BaseXY>();
                ///加载后获取最后一圈点云数据拟合椭圆，作为起算椭圆
                ///考虑扫描时前面可能是站台，而最后一圈是站台的可能性相对较低，因而选择最后一圈作为椭圆拟合起算点云
                for (int i = 0; i < 2; i++)
                {
                    OpenFls.libRef.getXYZScanPoints2(0, 0, numCol / 2 - 1 + i * numCol / 2, numRow, out position, out color);
                    for (int j = 0; j < numRow; j++)
                    {
                        BaseXY b = new BaseXY();
                        b.X = (double)position.GetValue(3 * j);
                        b.Y = (double)position.GetValue(3 * j + 2);
                        if (Math.Abs(b.X) < 30 && Math.Abs(b.Y) < 30)
                        {
                            if (b.X == 0 && b.Y == 0) continue;//扫描仪有时会出现较多（0，0）点，影响椭圆拟合效果
                            lb.Add(b);
                        }
                    }
                }
                ///
                Ellipse_Fitting.Robust_fitting(lb, out Ellipse ep);
                ep_0 = new Ellipse() { X0 = ep.X0, Y0 = ep.Y0, A = ep.A, B = ep.B, Angle = ep.Angle, };
                if (Get_FirstAutoTime == 0)
                {
                    ///首个文件解析时：Get_FirstAutoTime == 0
                    ///（1）获取起始的扫描点时间 FirstScanFileHeadTime
                    ///（2）扫描点理论每列间隔时间 DetaScanTime
                    ///（3）扫描点时间系数 Autotime_pra
                    DLLForFls.OpenFls.libRef.getAutomationTimeOfScanPoint(0, numRow - 1, 0, out Get_FirstAutoTime);
                    ScanHeadTime = Get_FirstAutoTime;
                    OpenFls.libRef.getAutomationTimeOfScanPoint(0, numRow - 1, 1, out ulong atRound);
                    OpenFls.libRef.getAutomationTimeOfScanPoint(0, numRow - 1, numCol / 2 - 1, out ulong at2);
                    CheckAuto_pra();
                    Autotime_pra2 = Autotime_pra;
                    ulong time = 2 * Get_FirstAutoTime - atRound - at2;
                    ///记录各种信息
                    string message = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + " FirstScanFileHeadTime " + Get_FirstAutoTime;
                    Text_IO.WriteLineToTxt(WriteMessagePath, message);
                    string message2 = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + " DetaScanTime " + time;
                    Text_IO.WriteLineToTxt(WriteMessagePath, message2);
                    DetaScanTime = time;
                    string message3 = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + " Autotime_pra " + Autotime_pra;
                    Text_IO.WriteLineToTxt(WriteMessagePath, message3);
                    ///生成_1.config文件，用于判断是否经过了第一次数据解析
                    Text_IO.WriteLineToTxt(WriteMessagePath.Replace(".txt", "_1.config"), message);
                }
                else if (Get_Autotime == 0)
                {
                    ///第二个文件解析时 Get_Autotime == 0
                    ///（1）将从第一个文件获取的扫描点时间系数Autotime_pra2赋予Autotime_pra
                    ///（2）根据从第一个文件获取的FirstScanFileHeadTime和DetaScanTime重新计算第二个及之后文件对应的起始的扫描点时间
                    ///（3）生成_2.config文件，之后的文件检测到该文件直接调用第二个文件解析的一些参数，eg:ScanHeadTime
                    Autotime_pra = Autotime_pra2;
                    //若Get_Autotime为零表明尚未初始化
                    //获取初始化文件最小自动化时间，用以与现实时间匹配
                    DLLForFls.OpenFls.libRef.getAutomationTimeOfScanPoint(0, numRow - 1, 0, out ulong Get_TwoTime);
                    ulong time = Get_TwoTime + DetaScanTime;
                    if ((time - Get_FirstAutoTime < 10) || (Get_FirstAutoTime - time < 10)) Get_Autotime = Get_FirstAutoTime;
                    else Get_Autotime = time;
                    ScanHeadTime = Get_Autotime;
                    //记录时间+扫描起始时间点
                    string message = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + " ScanHeadTime " + Get_Autotime;
                    Text_IO.WriteLineToTxt(WriteMessagePath, message);
                    Text_IO.WriteLineToTxt(WriteMessagePath.Replace(".txt", "_2.config"), message);
                }
                else
                {
                    ///第三个文件开始的文件直接调用第二个文件解析的一些参数，ScanHeadTime和Autotime_pra
                    Autotime_pra = Autotime_pra2;
                }

                ///第一次加载，获取展开图统一采用的椭圆信息
                if (ellipse == null)
                {
                    ///判断拟合椭圆是否正常，如正常则采用，如不正常则使用预设椭圆
                    if (!double.IsNaN(ep.X0))
                    {
                        double theE = (ep.A - ep.B) / TunnelRadius;
                        if (theE > 0.1 || ep.B > TunnelRadius || ep.A < TunnelRadius)
                        {
                            GetEllipse(out ellipse, TunnelRadius, Hadjust);
                            IsEllipse = false;
                        }
                        else ellipse = ep;
                    }
                    else
                    {
                        GetEllipse(out ellipse, TunnelRadius, Hadjust);
                        IsEllipse = false;
                    }
                    ///记录展开图统一采用的椭圆信息和扫描列数、扫描行数
                    ellipse.A = (ellipse.A + ellipse.B) / 2;
                    string message1 = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + " Ellipse " + string.Format("{0:N6}", ellipse.A) + " " + string.Format("{0:N6}", ellipse.B) + " " + string.Format("{0:N6}", ellipse.X0) + " " + string.Format("{0:N6}", ellipse.Y0) + " " + string.Format("{0:N6}", ellipse.Angle);
                    Text_IO.WriteLineToTxt(WriteMessagePath, message1);
                    string message2 = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + " NumCol " + numCol;
                    Text_IO.WriteLineToTxt(WriteMessagePath, message2);
                    string message3 = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + " NumRow " + numRow;
                    Text_IO.WriteLineToTxt(WriteMessagePath, message3);
                }

                double r = ellipse.A;//半径
                List<List<Faro_point>> llp = new List<List<Faro_point>>();
                ap = new Faro_point[numCol / 2][];//初始化点云数据存储数组
                over = 0;//统计数据解析完成的线程数
                lme = new List<MileageEllipse>();//列间里程数据

                ///逐圈将扫描数据添加到线程中运行，每numSplit（此处是50）圈计算一次拟合椭圆用于计算深度等数据
                for (int i = 0; i < numCol / 2; i++)
                {
                    ThreadInfo tf = new ThreadInfo();//声明单圈数据
                    #region tf属性计算
                    if (splitMap == numSplit)
                    {
                        //更新ep
                        List<BaseXY> l = new List<BaseXY>();
                        ///获取第i圈，前面一列（第i列）的数据
                        OpenFls.libRef.getXYZScanPoints2(0, 0, i, numRow, out position, out color);
                        tf.p1 = position; tf.c1 = color;//赋给tf变量
                        for (int j = 0; j < numRow; j++)
                        {
                            BaseXY b = new BaseXY();
                            b.X = (double)position.GetValue(3 * j);
                            b.Y = (double)position.GetValue(3 * j + 2);
                            if (Math.Abs(b.X) < 30 && Math.Abs(b.Y) < 30)
                            {
                                if (b.X == 0 && b.Y == 0) continue;//扫描仪有时会出现较多（0，0）点，影响椭圆拟合效果
                                l.Add(b);
                            }
                        }
                        ///获取第i圈，后面一列（第i+numCol / 2列）的数据
                        OpenFls.libRef.getXYZScanPoints2(0, 0, i + numCol / 2, numRow, out position, out color);
                        tf.p2 = position; tf.c2 = color;//赋给tf变量
                        for (int j = 0; j < numRow; j++)
                        {
                            BaseXY b = new BaseXY();

                            b.X = (double)position.GetValue(3 * j);
                            b.Y = (double)position.GetValue(3 * j + 2);
                            if (Math.Abs(b.X) < 30 && Math.Abs(b.Y) < 30)
                            {
                                if (b.X == 0 && b.Y == 0) continue;//扫描仪有时会出现较多（0，0）点，影响椭圆拟合效果
                                l.Add(b);
                            }
                        }
                        ///有原先拟合椭圆，根据现有断面数据进一步拟合得到ep
                        Ellipse_Fitting.Robust_fitting1(l, ref ep);

                        ///获取起始里程
                        OpenFls.libRef.getAutomationTimeOfScanPoint(0, numRow - 1, i, out at_s);
                        at_s = at_s - ScanHeadTime;
                        if (!Get_Mileage(mileage, startTime, at_s, out Mileage_s))
                        {
                            string log = Fls_path + "：" + "第" + i + "列未正常获取起始里程！";
                            //Text_IO.WriteLog(log);//暂无记录，如出现该错误一般是里程数据问题或者匹配问题
                        }
                        else
                        {
                            string log = Fls_path + "：" + "第" + i + "列正常获取起始里程！";
                            //Text_IO.WriteLog(log);//暂无记录
                        }

                        ///获取终止里程
                        int endCol = i + numCol / 2 + numSplit - 1;
                        if (endCol > numCol - 1)
                        {
                            endCol = numCol - 1;
                        }
                        OpenFls.libRef.getAutomationTimeOfScanPoint(0, numRow - 1, endCol, out at_Max);
                        at_Max = at_Max - ScanHeadTime;
                        if (!Get_Mileage(mileage, startTime, at_Max, out Mileage_e))
                        {
                            string log = Fls_path + "：" + "第" + endCol + "列未正常获取终止里程！";
                            //Text_IO.WriteLog(log);//暂无记录，如出现该错误一般是里程数据问题或者匹配问题
                        }
                        else
                        {
                            string log = Fls_path + "：" + "第" + endCol + "列正常获取终止里程！";
                            //Text_IO.WriteLog(log);//暂无记录
                        }

                        ///记录当前numSplit列内的起始里程、扫描点里程间距、拟合椭圆
                        int lines = endCol - (i + numCol / 2) + 1;
                        double rate = lines * 6.0 / (lines * 6.0 - 1);
                        dm = (Mileage_e - Mileage_s) * rate / lines;
                        ddm = dm / (numRow * 2);
                        if (dm != 0) { lme.Add(new MileageEllipse { mileage = Mileage_s, dm = dm, col = i, ep = new Ellipse(ep) }); }
                        if (endCol == numCol - 1) { lme.Add(new MileageEllipse { mileage = Mileage_e, dm = 1, col = i, ep = new Ellipse(ep) }); }

                        splitMap = 0;//标志位重置为0

                        ///获取整个文件的起始里程和终止里程
                        double ddmmmm = (Mileage_e - Mileage_s);
                        if (m1 == m2)
                        {
                            if (ddmmmm > 0) { m1 = Mileage_s; m2 = Mileage_s + ddmmmm; }
                            else { m2 = Mileage_s; m1 = Mileage_s + ddmmmm; }
                        }
                        else
                        {
                            if (ddmmmm > 0)
                            {
                                if (m1 > Mileage_s) m1 = Mileage_s;
                                if (m2 < Mileage_s + ddmmmm) m2 = Mileage_s + ddmmmm;
                            }
                            else
                            {
                                if (m1 > Mileage_s + ddmmmm) m1 = Mileage_s + ddmmmm;
                                if (m2 < Mileage_s) m2 = Mileage_s;
                            }
                        }
                    }
                    else
                    {
                        //第i圈前面一列数据赋给tf变量
                        OpenFls.libRef.getXYZScanPoints2(0, 0, i, numRow, out position, out color);
                        tf.p1 = position; tf.c1 = color;
                        //第i圈后面一列数据赋给tf变量
                        OpenFls.libRef.getXYZScanPoints2(0, 0, i + numCol / 2, numRow, out position, out color);
                        tf.p2 = position; tf.c2 = color;

                    }
                    splitMap++;
                    #endregion
                    tf.ep2 = ep;
                    tf.numRow = numRow; tf.ep1 = ellipse;
                    tf.mileage_s = Mileage_s; tf.ddm = ddm; tf.round = i;
                    ThreadPool.QueueUserWorkItem(new WaitCallback(OneLapCalculation), tf);//多线程运行
                    //OneLapCalculation(tf.p1, tf.p2, tf.c1, tf.c2, tf.numRow, tf.mileage_s, tf.ddm, tf.ep1, tf.ep2, tf.round);//主线程运行
                    Mileage_s += dm;
                }
                int Nsleep = 0;
                while (over < numCol / 2)
                {
                    Thread.Sleep(100); //延时判断所有数据是否都解析完成
                    //test-测试
                    Nsleep++;
                    if (Nsleep == 1000)
                    {
                        if (writeTimePath != "") Text_IO.WriteLineToTxt(writeTimePath, DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + " 程序未完成解析，over is：" + over);
                    }
                    else if (Nsleep == 1500)
                    {
                        if (writeTimePath != "") Text_IO.WriteLineToTxt(writeTimePath, DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + " 程序未完成解析，over is：" + over);
                        break;//解析时间过长自动退出并记录
                    }
                }

                return true;
            }
            catch (Exception e)
            {
                string log = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + " 解析过程发生错误：" + e.StackTrace;
                Text_IO.WriteLineToTxt(WriteLogPath, log);
                return false;
            }
        }

        public bool ET_Analysis_Limit(string Fls_path, List<Mileage> mileage, long startTime, double TunnelRadius = 2.7)
        {
            try
            {
                GLimitData = GLimitData0;
                ///统计每次解析时间并记录（可去除）
                if (writeTimePath != "") Text_IO.WriteLineToTxt(writeTimePath, DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + " StartLoad：" + Fls_path);
                OpenFls.libRef.load(Fls_path);//加载扫描文件到内存（时间较长）
                if (writeTimePath != "") Text_IO.WriteLineToTxt(writeTimePath, DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + " EndLoad：" + Fls_path);
                OpenFls.libRef.scanReflectionMode = 1;
                numCol = OpenFls.libRef.getScanNumCols(0);//扫描列数
                numRow = OpenFls.libRef.getScanNumRows(0);//扫描行数
                Array position = Array.CreateInstance(typeof(double), numRow * 3);
                Array color = Array.CreateInstance(typeof(int), numRow);
                Array polar = Array.CreateInstance(typeof(double), numRow * 3);
                Array refl = Array.CreateInstance(typeof(int), numRow);
                ulong ScanHeadTime = Get_Autotime;
                //扫描文件按一定列数分割分配里程，每部分相关参数
                int numSplit = 50;//分割列数*0.5，当前设定为50圈
                int splitMap = numSplit;//计算标志位
                ulong at_s;//起始自动化时间
                double Mileage_s = 0;//起始里程
                ulong at_Max;//终止自动化时间
                double Mileage_e;//终止里程
                double dy = 0;
                double dm = 0;
                double ddm = 0;
                int num = 0;
                int num_1 = numRow / 150;//每度含点数
                int gap = Math.Max(1, num_1 * 2 / 10);//大概保证两度内取10个点时的间隔
                int ind_d1 = num_1 * 5, ind_d2 = num_1 * 12;//取5度和12 度处点做判断
                
                m1 = 0; m2 = 0;
                List<BaseXY> lb = new List<BaseXY>();
                ///加载后获取最后一圈点云数据拟合椭圆，作为起算椭圆
                ///考虑扫描时前面可能是站台，而最后一圈是站台的可能性相对较低，因而选择最后一圈作为椭圆拟合起算点云
                for (int i = 0; i < 2; i++)
                {
                    OpenFls.libRef.getXYZScanPoints2(0, 0, numCol / 2 - 1 + i * numCol / 2, numRow, out position, out color);
                    for (int j = 0; j < numRow; j++)
                    {
                        BaseXY b = new BaseXY();
                        b.X = (double)position.GetValue(3 * j);
                        b.Y = (double)position.GetValue(3 * j + 2);
                        if (Math.Abs(b.X) < 30 && Math.Abs(b.Y) < 30)
                        {
                            if (b.X == 0 && b.Y == 0) continue;//扫描仪有时会出现较多（0，0）点，影响椭圆拟合效果
                            lb.Add(b);
                        }
                    }
                }
                ///
                Ellipse_Fitting.Robust_fitting(lb, out Ellipse ep);
                ep_0 = new Ellipse() { X0 = ep.X0, Y0 = ep.Y0, A = ep.A, B = ep.B, Angle = ep.Angle, };
                if (Get_FirstAutoTime == 0)
                {
                    ///首个文件解析时：Get_FirstAutoTime == 0
                    ///（1）获取起始的扫描点时间 FirstScanFileHeadTime
                    ///（2）扫描点理论每列间隔时间 DetaScanTime
                    ///（3）扫描点时间系数 Autotime_pra
                    DLLForFls.OpenFls.libRef.getAutomationTimeOfScanPoint(0, numRow - 1, 0, out Get_FirstAutoTime);
                    ScanHeadTime = Get_FirstAutoTime;
                    OpenFls.libRef.getAutomationTimeOfScanPoint(0, numRow - 1, 1, out ulong atRound);
                    OpenFls.libRef.getAutomationTimeOfScanPoint(0, numRow - 1, numCol / 2 - 1, out ulong at2);
                    CheckAuto_pra();
                    Autotime_pra2 = Autotime_pra;
                    ulong time = 2 * Get_FirstAutoTime - atRound - at2;
                    ///记录各种信息
                    string message = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + " FirstScanFileHeadTime " + Get_FirstAutoTime;
                    Text_IO.WriteLineToTxt(WriteMessagePath, message);
                    string message2 = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + " DetaScanTime " + time;
                    Text_IO.WriteLineToTxt(WriteMessagePath, message2);
                    DetaScanTime = time;
                    string message3 = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + " Autotime_pra " + Autotime_pra;
                    Text_IO.WriteLineToTxt(WriteMessagePath, message3);
                    ///生成_1.config文件，用于判断是否经过了第一次数据解析
                    Text_IO.WriteLineToTxt(WriteMessagePath.Replace(".txt", "_1.config"), message);
                }
                else if (Get_Autotime == 0)
                {
                    ///第二个文件解析时 Get_Autotime == 0
                    ///（1）将从第一个文件获取的扫描点时间系数Autotime_pra2赋予Autotime_pra
                    ///（2）根据从第一个文件获取的FirstScanFileHeadTime和DetaScanTime重新计算第二个及之后文件对应的起始的扫描点时间
                    ///（3）生成_2.config文件，之后的文件检测到该文件直接调用第二个文件解析的一些参数，eg:ScanHeadTime
                    Autotime_pra = Autotime_pra2;
                    //若Get_Autotime为零表明尚未初始化
                    //获取初始化文件最小自动化时间，用以与现实时间匹配
                    DLLForFls.OpenFls.libRef.getAutomationTimeOfScanPoint(0, numRow - 1, 0, out ulong Get_TwoTime);
                    ulong time = Get_TwoTime + DetaScanTime;
                    if ((time - Get_FirstAutoTime < 10) || (Get_FirstAutoTime - time < 10)) Get_Autotime = Get_FirstAutoTime;
                    else Get_Autotime = time;
                    ScanHeadTime = Get_Autotime;
                    //记录时间+扫描起始时间点
                    string message = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + " ScanHeadTime " + Get_Autotime;
                    Text_IO.WriteLineToTxt(WriteMessagePath, message);
                    Text_IO.WriteLineToTxt(WriteMessagePath.Replace(".txt", "_2.config"), message);
                }
                else
                {
                    ///第三个文件开始的文件直接调用第二个文件解析的一些参数，ScanHeadTime和Autotime_pra
                    Autotime_pra = Autotime_pra2;
                }
                ///第一次加载，获取展开图统一采用的椭圆信息
                GetEllipse(out ellipse, TunnelRadius, Hadjust);

                ap = new Faro_point[numCol / 2][];//初始化点云数据存储数组
                limit_quan = new List<Faro_point>[numCol / 2];
                LimitData_arr = new List<Faro_point>[numCol / 2];

                over = 0;//统计数据解析完成的线程数
                lme = new List<MileageEllipse>();//列间里程数据
                //ThreadPool.SetMaxThreads(60, 60);
                for (int i = 0; i < numCol / 2; i++)   // 2500次循环
                {
                    ThreadInfo tf = new ThreadInfo();//声明单圈数据
                    #region tf属性计算
                    if (splitMap == numSplit)
                    {
                        ///获取起始里程
                        OpenFls.libRef.getAutomationTimeOfScanPoint(0, numRow - 1, i, out at_s);
                        at_s = at_s - ScanHeadTime;
                        if (!Get_Mileage(mileage, startTime, at_s, out Mileage_s))
                        {
                            string log = Fls_path + "：" + "第" + i + "列未正常获取起始里程！";
                            //Text_IO.WriteLog(log);//暂无记录，如出现该错误一般是里程数据问题或者匹配问题
                        }
                        else
                        {
                            string log = Fls_path + "：" + "第" + i + "列正常获取起始里程！";
                            //Text_IO.WriteLog(log);//暂无记录
                        }
                        ///获取终止里程
                        int endCol = i + numSplit - 1 + numCol / 2;
                        if (endCol > numCol - 1)
                        {
                            endCol = numCol - 1;
                        }
                        OpenFls.libRef.getAutomationTimeOfScanPoint(0, numRow - 1, endCol, out at_Max);
                        at_Max = at_Max - ScanHeadTime;
                        if (!Get_Mileage(mileage, startTime, at_Max, out Mileage_e))
                        {
                            string log = Fls_path + "：" + "第" + endCol + "列未正常获取终止里程！";
                            //Text_IO.WriteLog(log);//暂无记录，如出现该错误一般是里程数据问题或者匹配问题
                        }
                        else
                        {
                            string log = Fls_path + "：" + "第" + endCol + "列正常获取终止里程！";
                            //Text_IO.WriteLog(log);//暂无记录
                        }

                        ///记录当前numSplit列内的起始里程、扫描点里程间距、拟合椭圆
                        int lines = numSplit;// endCol - (i + numCol / 2) + 1;//
                        double rate = lines * 6.0 / (lines * 6.0 - 1);
                        dm = (Mileage_e - Mileage_s) * rate / lines;
                        ddm = dm / (numRow * 2);
                        if (dm != 0) { lme.Add(new MileageEllipse { mileage = Mileage_s, dm = dm, col = i, ep = new Ellipse(ep) }); }
                        if (endCol == numCol - 1) { lme.Add(new MileageEllipse { mileage = Mileage_e, dm = 1, col = i, ep = new Ellipse(ep) }); }

                        splitMap = 0;//标志位重置为0

                        ///计算整个文件的起始里程和终止里程
                        double ddmmmm = (Mileage_e - Mileage_s);//当前numSplit列内的里程
                        if (m1 == m2)
                        {
                            if (ddmmmm > 0) { m1 = Mileage_s; m2 = Mileage_s + ddmmmm; }
                            else { m2 = Mileage_s; m1 = Mileage_s + ddmmmm; }
                        }
                        else
                        {
                            if (ddmmmm > 0)
                            {
                                if (m1 > Mileage_s) m1 = Mileage_s;
                                if (m2 < Mileage_s + ddmmmm) m2 = Mileage_s + ddmmmm;
                            }
                            else
                            {
                                if (m1 > Mileage_s + ddmmmm) m1 = Mileage_s + ddmmmm;
                                if (m2 < Mileage_s) m2 = Mileage_s;
                            }
                        }
                    }

                    //第i圈前面一列数据赋给tf变量
                    OpenFls.libRef.getXYZScanPoints2(0, 0, i, numRow, out position, out color);
                    tf.p1 = position; tf.c1 = color;
                    //第i圈后面一列数据赋给tf变量
                    OpenFls.libRef.getXYZScanPoints2(0, 0, i + numCol / 2, numRow, out position, out color);
                    tf.p2 = position; tf.c2 = color;

                    splitMap++;
                    #endregion
                    tf.ep2 = ep;
                    tf.numRow = numRow; tf.ep1 = ellipse;
                    tf.mileage_s = Mileage_s; tf.ddm = ddm; tf.round = i;
                    ThreadPool.QueueUserWorkItem(new WaitCallback(OneLapCal_Limit), tf);//多线程运行
                    //ap[i]=OneLapCal_Limit(tf.p1, tf.p2, tf.c1, tf.c2, tf.numRow, tf.mileage_s, tf.ddm, tf.ep1, tf.round);//主线程运行
                    Mileage_s += dm;
                }




                int Nsleep = 0;
                while (over < numCol / 2)
                {
                    Thread.Sleep(100); //延时判断所有数据是否都解析完成
                    ////test-测试
                    //Nsleep++;
                    //if (Nsleep == 1000)
                    //{
                    //    if (writeTimePath != "") Text_IO.WriteLineToTxt(writeTimePath, DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + " 程序未完成解析，over is：" + over);
                    //}
                    //else if (Nsleep == 1500)
                    //{
                    //    if (writeTimePath != "") Text_IO.WriteLineToTxt(writeTimePath, DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + " 程序未完成解析，over is：" + over);
                    //    break;//解析时间过长自动退出并记录
                    //}
                }


                // if (writeTimePath != "") Text_IO.WriteLineToTxt(writeTimePath, DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + " StartLoad：" + Fls_path);

#if true
                if (writeTimePath != "") Text_IO.WriteLineToTxt(writeTimePath, DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") +  Fls_path+ " 开始生成限界图" );

                for (int i = 0; i < numCol / 2; i++)
                {
                    if (limit_quan[i].Count != 0)
                    {
                        List<Faro_point> lp = new List<Faro_point>();
                        string log_path = Write_jpg_log;
                        if (limit_quan[i].First().tag == 0)
                        {
                            if (ap[i][0].Y - km >= 0.05)
                            {
                                // 侵界直接出图   
                                string jpg_path = Write_jpgPath + i + "+" + ap[i][0].Y + ".jpg";
                             
                                foreach (var v in ap[i]) {
                                    if (v.tag != 1) lp.Add(v);
                                }
                                SectionAnalysis.DrawSection(LimitData_arr[i], lp, limit_quan[i], jpg_path, Path.GetFileNameWithoutExtension(jpg_path));
                                // 将每一环的信息写成日志记录
                                if (log_path != "") Text_IO.WriteLineToTxt(log_path, DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + " 当前里程:" + ap[i][0].Y + "当前里程有侵界");
                                km = ap[i][0].Y;
                            }
                        }
                        else  //没有侵界按对应里程出图
                        {
                            // 将每一环的信息写成日志记录
                            if (log_path != "") Text_IO.WriteLineToTxt(log_path, DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + " 当前里程:" + ap[i][0].Y);
                            // 按照一定的里程出图  500圈
                            if ((i + 1) % 500 == 0)
                            {
                                foreach (var v in ap[i])
                                {
                                    if (v.tag != 1) lp.Add(v);
                                }
                                string jpg_path = Write_jpgPath + i + "+" + ap[i][0].Y + ".jpg";
                                SectionAnalysis.DrawSection(LimitData_arr[i], lp, limit_quan[i], jpg_path, Path.GetFileNameWithoutExtension(jpg_path));
                            }
                        }
                    }


                }
                if (writeTimePath != "") Text_IO.WriteLineToTxt(writeTimePath, DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + Fls_path + " 结束生成限界图");
#endif
                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }
        /// <summary>
        /// 单圈扫描数据计算
        /// </summary>
        /// <param name="a">用于传递参数</param>
        void OneLapCalculation(object a)
        {
            try
            {
                ThreadInfo tf = a as ThreadInfo;
                //分别为半圈点数量，单圈的序号，
                int numRow = tf.numRow, round = tf.round;
                //分别为单圈起始里程，点间里程差
                double mileage_s = tf.mileage_s, ddm = tf.ddm;
                //分别为椭圆展开时用到的椭圆参数，计算单点到椭圆的误差（深度）时用到的椭圆参数
                Ellipse ep1 = tf.ep1, ep2 = tf.ep2;
                //分别为前半圈数据，后半圈数据，前半圈数据灰度，后半圈数据灰度
                Array pos1 = tf.p1, pos2 = tf.p2, clr1 = tf.c1, clr2 = tf.c2;
                ap[round] = OneLapCalculation(pos1, pos2, clr1, clr2, numRow, mileage_s, ddm, ep1, ep2, round);
                Interlocked.Increment(ref over);//用于多线程的变量递增，记录每圈数据是否已经解算
            }
            catch
            {
                Interlocked.Increment(ref over);
            }
        }
        /// <summary>
        /// 单圈扫描数据计算
        /// </summary>
        /// <param name="pos1">前半圈数据</param>
        /// <param name="pos2">后半圈数据</param>
        /// <param name="color1">前半圈数据灰度</param>
        /// <param name="color2">后半圈数据灰度</param>
        /// <param name="numRow">行数，单圈点数量</param>
        /// <param name="Mileage_s">单圈起始里程</param>
        /// <param name="ddm">点间里程差</param>
        /// <param name="ellipse">椭圆展开时用到的椭圆参数</param>
        /// <param name="ep">计算单点到椭圆的误差（深度）时用到的椭圆参数</param>
        /// <param name="round">单圈的序号</param>
        /// <returns></returns>
        static Faro_point[] OneLapCalculation(Array pos1, Array pos2, Array color1, Array color2, int numRow, double Mileage_s, double ddm, Ellipse ellipse, Ellipse ep, int round = 0)
        {
            try
            {
                Faro_point[] a_p = new Faro_point[numRow * 2];
                List<Faro_point> lp = new List<Faro_point>();
                double[] pos = (double[])pos1; int[] clr = (int[])color1;
                double r = ellipse.A;//A,B都等于r
                int num = -1;
                //每圈第一列
                for (int j = numRow - 1; j > -1; j--)
                {
                    num = numRow - 1 - j;
                    Faro_point p = new Faro_point();
                    //获取每点xyz坐标及灰度color
                    p.X = pos[3 * j];
                    p.Y =/* pos[3 * j + 1] +*/ Mileage_s + num * ddm;
                    p.Z = pos[3 * j + 2];
                    p.Color = clr[j];
                    double Error = p.X * p.X + p.Z * p.Z;
                    if (Error > 900 || Error < 0.04)
                    { p.tag = 1; a_p[num] = p; continue; }
                    double Xr, Yr;//每点x，z映射到以椭圆中心坐标为中心的坐标系下圆心为（0，0），半径为r的圆上对应某点的坐标
                    if (p.X == 0)
                    {
                        Xr = 0;
                        Yr = ellipse.Y0 + Math.Sqrt(r * r - ellipse.X0 * ellipse.X0);
                    }
                    else
                    {
                        //联立Yr=ta*Xr和（Xr-ellipse.X0)^2+(Yr-ellipse.Y0)^2=r^2可求解Xr，Yr
                        double ta = p.Z / p.X;
                        double yX_1 = ellipse.Y0 * ta + ellipse.X0;
                        double yX_2 = 1 + ta * ta;
                        if (p.X >= 0)
                        {
                            Xr = (yX_1 + Math.Sqrt(yX_1 * yX_1 + yX_2 * (r * r - ellipse.X0 * ellipse.X0 - ellipse.Y0 * ellipse.Y0))) / yX_2;
                        }
                        else
                        {
                            Xr = (yX_1 - Math.Sqrt(yX_1 * yX_1 + yX_2 * (r * r - ellipse.X0 * ellipse.X0 - ellipse.Y0 * ellipse.Y0))) / yX_2;
                        }
                        Yr = Xr * ta;
                    }
                    p.xe = (Math.PI - Math.Atan2(Xr - ellipse.X0, Yr - ellipse.Y0)) * r;//每点基于椭圆展开
                    double ae = Math.Atan2(p.Z - ep.Y0, p.X - ep.X0) - ep.Angle;
                    double re = ep.A * ep.B / Math.Sqrt(Math.Pow(ep.A * Math.Sin(ae), 2) + Math.Pow(Math.Cos(ae) * ep.B, 2));//点与圆心连线方向上椭圆对应点到圆心的距离
                    p.H = Math.Sqrt(Math.Pow(p.Z - ep.Y0, 2) + Math.Pow(p.X - ep.X0, 2)) - re;//每点基于圆心方向到椭圆的距离
                    a_p[num] = p;
                    lp.Add(p);
                }
                //每圈第二列
                pos = (double[])pos2; clr = (int[])color2;
                for (int j = 0; j < numRow; j++)
                {
                    num = numRow + j;
                    Faro_point p = new Faro_point();
                    p.X = pos[3 * j];
                    p.Y = /*pos[3 * j + 1] +*/ Mileage_s + num * ddm;
                    p.Z = pos[3 * j + 2];
                    p.Color = clr[j];

                    double Error = p.X * p.X + p.Z * p.Z;
                    if (Error > 900 || Error < 0.04)
                    { p.tag = 1; a_p[num] = p; continue; }
                    double Xr, Yr;
                    if (p.X == 0)
                    {
                        Xr = 0;
                        Yr = ellipse.Y0 + Math.Sqrt(r * r - ellipse.X0 * ellipse.X0);
                    }
                    else
                    {
                        //联立Yr=ta*Xr和（Xr-ellipse.X0)^2+(Yr-ellipse.Y0)^2=r^2可求解Xr，Yr
                        double ta = p.Z / p.X;
                        double yX_1 = ellipse.Y0 * ta + ellipse.X0, yX_2 = 1 + ta * ta;
                        if (p.X < 0)
                        {
                            Xr = (yX_1 - Math.Sqrt(yX_1 * yX_1 + yX_2 * (r * r - ellipse.X0 * ellipse.X0 - ellipse.Y0 * ellipse.Y0))) / yX_2;
                        }
                        else
                        {
                            Xr = (yX_1 + Math.Sqrt(yX_1 * yX_1 + yX_2 * (r * r - ellipse.X0 * ellipse.X0 - ellipse.Y0 * ellipse.Y0))) / yX_2;
                        }
                        Yr = Xr * ta;
                    }
                    p.xe = (Math.PI - Math.Atan2(Xr - ellipse.X0, Yr - ellipse.Y0)) * r;//每点基于椭圆展开
                    double ae = Math.Atan2(p.Z - ep.Y0, p.X - ep.X0) - ep.Angle;
                    double re = ep.A * ep.B / Math.Sqrt(Math.Pow(ep.A * Math.Sin(ae), 2) + Math.Pow(Math.Cos(ae) * ep.B, 2));//点与圆心连线方向上椭圆对应点到圆心的距离
                    p.H = Math.Sqrt(Math.Pow(p.Z - ep.Y0, 2) + Math.Pow(p.X - ep.X0, 2)) - re;//每点基于圆心方向到椭圆的距离
                    a_p[num] = p;
                    lp.Add(p);
                }

                double[] s = TunnelClassify(lp);//提取管壁
                for (int i = 0, j = 0; i < a_p.Length; i++)
                {
                    if (a_p[i].tag != 1)
                    {
                        //a_p[i].Hs = a_p[i].H;
                        a_p[i].H -= s[j];//计算每点到管壁的深度
                        //lp[j].H = a_p[i].H;
                        j++;
                    }
                    else//给一些要过滤（距离过大）的点赋同列邻近点的值，保证数据原始行列完整性，方便后续相关计算
                    {
                        for (int k = 1; k < a_p.Length; k++)
                        {
                            int k1 = i - k, k2 = i + k;
                            if (k1 > 0) { if (a_p[k1].tag != 1) { a_p[i].getValue(a_p[k1]); break; } }
                            else if (k2 >= a_p.Length) { break; }
                            if (k2 < a_p.Length) { if (a_p[k2].tag != 1) { a_p[i].getValue(a_p[k2]); break; } }
                            else if (k1 <= 0) { break; }
                        }
                    }
                }
                return a_p;
            }
            catch (Exception e) { return new Faro_point[0]; }
        }

        void OneLapCal_Limit(object a)
        {
            try
            {
               
                ThreadInfo tf = a as ThreadInfo;
                //分别为单圈点数量，单圈的序号，
                int numRow = tf.numRow, round = tf.round;
                //分别为单圈起始里程，点间里程差
                double mileage_s = tf.mileage_s, ddm = tf.ddm;
                //分别为椭圆展开时用到的椭圆参数，计算单点到椭圆的误差（深度）时用到的椭圆参数
                Ellipse ep1 = tf.ep1, ep2 = tf.ep2;
                //分别为前半圈数据，后半圈数据，前半圈数据灰度，后半圈数据灰度
                Array pos1 = tf.p1, pos2 = tf.p2, clr1 = tf.c1, clr2 = tf.c2;
                ap[round] = OneLapCal_Limit(pos1, pos2, clr1, clr2, numRow, mileage_s, ddm, ep1, round, out List <Faro_point> AllMark, out List<Faro_point> lp, out List<Faro_point> LimitData);
                //ap[round][0].Y   里程信息


              
                // 1. 将每一圈的list装在数组的对应位置里
                limit_quan[round] = AllMark; // 得到单圈点云数据
                LimitData_arr[round] = LimitData;
#if false
                // 2. 取出list的第一个元素判断是否侵界

                if (AllMark.Count != 0)
                {
                    string log_path = "H://测试//Limit//limit_log.txt";
                    if (AllMark[0].tag == 0)
                    {
                        if (ap[round][0].Y - km >= 0.05)
                        {
                            // 侵界直接出图   
                            string jpg_path = "H://测试//Limit//" + round + "+" + ap[round][0].Y + ".jpg";
                            SectionAnalysis.DrawSection(LimitData, lp, AllMark, jpg_path, Path.GetFileNameWithoutExtension(jpg_path));
                            // 将每一环的信息写成日志记录
                            if (log_path != "") Text_IO.WriteLineToTxt(log_path, DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + " 当前里程:" + ap[round][0].Y + "当前里程有侵界");
                            km = ap[round][0].Y;
                        }
                    }
                    else  //没有侵界按对应里程出图
                    {
                        // 将每一环的信息写成日志记录
                        if (log_path != "") Text_IO.WriteLineToTxt(log_path, DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + " 当前里程:" + ap[round][0].Y);
                        // 按照一定的里程出图  500圈
                        if ((round + 1) % 500 == 0)
                        {
                            string jpg_path = "H://测试//Limit//" + round + "+" + ap[round][0].Y + ".jpg";
                            SectionAnalysis.DrawSection(LimitData, lp, AllMark, jpg_path, Path.GetFileNameWithoutExtension(jpg_path));
                        }
                    }
                     Interlocked.Increment(ref over);//用于多线程的变量递增，记录每圈数据是否已经解算
                }
#endif

                 Interlocked.Increment(ref over);//用于多线程的变量递增，记录每圈数据是否已经解算
            }
            catch
            {
                Interlocked.Increment(ref over);
            }
        }
        static Faro_point[] OneLapCal_Limit(Array pos1, Array pos2, Array color1, Array color2, int numRow, double Mileage_s, double ddm, Ellipse ellipse, int round , out List<Faro_point> AllMark , out List<Faro_point> lp , out List<Faro_point> LimitData)
        {
            AllMark = new List<Faro_point>();
            LimitData = new List<Faro_point>();
            lp = new List<Faro_point>();
            try
            {
                Faro_point[] a_p = new Faro_point[numRow * 2];
               
                double[] pos = (double[])pos1; int[] clr = (int[])color1;
                int num = -1;
                double r = (ellipse.A + ellipse.B) / 2;
                //每圈第一列
                for (int j = numRow - 1; j > -1; j--)
                {
                    num = numRow - 1 - j;
                    Faro_point p = new Faro_point();
                    //获取每点xyz坐标及灰度color
                    p.X = pos[3 * j];
                    p.Y =/* pos[3 * j + 1] +*/ Mileage_s + num * ddm;
                    p.Z = pos[3 * j + 2];
                    p.Color = clr[j];
                    double Error = p.X * p.X + p.Z * p.Z;
                    if (Error > 900 || Error < 0.04)
                    { p.tag = 1; a_p[num] = p; continue; }
                    double Xr, Yr;//每点x，z映射到以椭圆中心坐标为中心的坐标系下圆心为（0，0），半径为r的圆上对应某点的坐标
                    if (p.X == 0)
                    {
                        Xr = 0;
                        Yr = ellipse.Y0 + Math.Sqrt(r * r - ellipse.X0 * ellipse.X0);
                    }
                    else
                    {
                        //联立Yr=ta*Xr和（Xr-ellipse.X0)^2+(Yr-ellipse.Y0)^2=r^2可求解Xr，Yr
                        double ta = p.Z / p.X;
                        double yX_1 = ellipse.Y0 * ta + ellipse.X0;
                        double yX_2 = 1 + ta * ta;
                        if (p.X >= 0)
                        {
                            Xr = (yX_1 + Math.Sqrt(yX_1 * yX_1 + yX_2 * (r * r - ellipse.X0 * ellipse.X0 - ellipse.Y0 * ellipse.Y0))) / yX_2;
                        }
                        else
                        {
                            Xr = (yX_1 - Math.Sqrt(yX_1 * yX_1 + yX_2 * (r * r - ellipse.X0 * ellipse.X0 - ellipse.Y0 * ellipse.Y0))) / yX_2;
                        }
                        Yr = Xr * ta;
                    }
                    p.xe = (Math.PI - Math.Atan2(Xr - ellipse.X0, Yr - ellipse.Y0)) * r;//每点基于椭圆展开
                    double ae = Math.Atan2(p.Z - ellipse.Y0, p.X - ellipse.X0) - ellipse.Angle;
                    double re = ellipse.A * ellipse.B / Math.Sqrt(Math.Pow(ellipse.A * Math.Sin(ae), 2) + Math.Pow(Math.Cos(ae) * ellipse.B, 2));//点与圆心连线方向上椭圆对应点到圆心的距离
                    p.H = Math.Sqrt(Math.Pow(p.Z - ellipse.Y0, 2) + Math.Pow(p.X - ellipse.X0, 2)) - re;//每点基于圆心方向到椭圆的距离
                    a_p[num] = p;
                    lp.Add(p);
                }
                //每圈第二列
                pos = (double[])pos2; clr = (int[])color2;
                for (int j = 0; j < numRow; j++)
                {
                    num = numRow + j;
                    Faro_point p = new Faro_point();
                    p.X = pos[3 * j];
                    p.Y = /*pos[3 * j + 1] +*/ Mileage_s + num * ddm;
                    p.Z = pos[3 * j + 2];
                    p.Color = clr[j];

                    double Error = p.X * p.X + p.Z * p.Z;
                    if (Error > 900 || Error < 0.04)
                    { p.tag = 1; a_p[num] = p; continue; }
                    double Xr, Yr;
                    if (p.X == 0)
                    {
                        Xr = 0;
                        Yr = ellipse.Y0 + Math.Sqrt(r * r - ellipse.X0 * ellipse.X0);
                    }
                    else
                    {
                        //联立Yr=ta*Xr和（Xr-ellipse.X0)^2+(Yr-ellipse.Y0)^2=r^2可求解Xr，Yr
                        double ta = p.Z / p.X;
                        double yX_1 = ellipse.Y0 * ta + ellipse.X0, yX_2 = 1 + ta * ta;
                        if (p.X < 0)
                        {
                            Xr = (yX_1 - Math.Sqrt(yX_1 * yX_1 + yX_2 * (r * r - ellipse.X0 * ellipse.X0 - ellipse.Y0 * ellipse.Y0))) / yX_2;
                        }
                        else
                        {
                            Xr = (yX_1 + Math.Sqrt(yX_1 * yX_1 + yX_2 * (r * r - ellipse.X0 * ellipse.X0 - ellipse.Y0 * ellipse.Y0))) / yX_2;
                        }
                        Yr = Xr * ta;
                    }
                    p.xe = (Math.PI - Math.Atan2(Xr - ellipse.X0, Yr - ellipse.Y0)) * r;//每点基于椭圆展开
                    double ae = Math.Atan2(p.Z - ellipse.Y0, p.X - ellipse.X0) - ellipse.Angle;
                    double re = ellipse.A * ellipse.B / Math.Sqrt(Math.Pow(ellipse.A * Math.Sin(ae), 2) + Math.Pow(Math.Cos(ae) * ellipse.B, 2));//点与圆心连线方向上椭圆对应点到圆心的距离
                    p.H = Math.Sqrt(Math.Pow(p.Z - ellipse.Y0, 2) + Math.Pow(p.X - ellipse.X0, 2)) - re;//每点基于圆心方向到椭圆的距离
                    a_p[num] = p;
                    lp.Add(p);
                }

                //Get_Limit(GLimitData, lp, "D://Limit//" + round + ".jpg", out List<Faro_point> lfp);

                //if ((round + 1) % 500 == 0)
                //{
                   
                //}
                 LimitCalculation(lp, out AllMark, out LimitData);

                return a_p;
            }
            catch(Exception  e) { return new Faro_point[0]; }
        }
#endregion

#region 里程加载&获取
        /// <summary>
        /// 逐个文件加载里程数据
        /// </summary>
        /// <param name="mileageFilePath">里程文件路径</param>
        /// <param name="mileage_all">里程数据</param>
        public void LoadMileage(string mileageFilePath, ref List<Mileage> mileage_all)
        {
            string[] lines;
            char[] split = new char[] { ' ', ';', '\t' };
            while (true)
            {
                try
                {
                    using (System.IO.StreamReader sr = new System.IO.StreamReader(mileageFilePath, Encoding.Default))
                    {
                        lines = sr.ReadToEnd().Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    }
                    break;//读完跳出循环
                }
                catch
                {
                    //使用TRYCATCH 避免因文件锁问题发生读写错误，发生时重新读写，直到成功读出里程新信息
                }
            }
            if (lines.Length == 0)
            {
                //记录文件里程无数据！
                return;
            }
            string[] item0 = lines[0].Split(split);
            Mileage pre = new Mileage()
            {
                M = Convert.ToDouble(item0[1]),
                Time = Convert.ToInt64(item0[0])
            };//首里程
            for (int i = 1; i < lines.Length; i++)
            {
                var line = lines[i];
                string[] items = line.Split(split);
                Mileage m = new Mileage()
                {
                    M = Convert.ToDouble(items[1]),
                    Time = Convert.ToInt64(items[0])
                };
                if (m.Time == pre.Time)
                {
                    continue;//去除相同时间里程（旧版小车会出现，新版暂未发现，如测试无问题可去除）
                }
                else mileage_all.Add(pre);
                pre = m;
            }

            mileage_all.Add(pre);
        }
        /// <summary>
        /// 里程全部加载
        /// </summary>
        /// <param name="mileageFilePath">里程文件路径</param>
        /// <param name="mileage_all">里程数据</param>
        public void LoadAllMileage(string mileageFilePath, out List<Mileage> mileage_all)
        {
            mileage_all = new List<Mileage>();
            ///首先尝试使用json格式解析里程数据（原始采集数据优先以json格式存储）
            ///如前一步失败，则采用设定好的格式逐行解析
            try
            {
                Process_Solve.GetMileage(mileageFilePath, out mileage_all);
            }
            catch
            {
                try
                {
                    string[] lines;
                    char[] split = new char[] { ' ', ';', '\t' };
                    using (System.IO.StreamReader sr = new System.IO.StreamReader(mileageFilePath, Encoding.Default))
                    {
                        lines = sr.ReadToEnd().Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    }
                    string[] item0 = lines[0].Split(split);
                    int item_N = item0.Length;
                    for (int i = 0; i < lines.Length; i++)
                    {
                        Mileage mileage = new Mileage();
                        string[] item = lines[i].Split(split);
                        ///一般里程至少包含两个信息，里程和对应时间
                        switch (item_N)
                        {
                            case 2://小里程
                                mileage.Time = Convert.ToInt64(item[0]);
                                mileage.M = Convert.ToDouble(item[1]);
                                break;
                            case 3://从数据库中导出的格式
                                mileage.ID = Convert.ToInt32(item[0]);
                                mileage.M = Convert.ToDouble(item[1]);
                                mileage.Time = Convert.ToInt64(item[2]);
                                break;
                            default://里程数据格式需遵循设定规则，出现以下这种情况数据一般不可使用
                                mileage.ID = Convert.ToInt32(item[0]);
                                mileage.Time = Convert.ToInt64(item[1]);
                                mileage.M = Convert.ToDouble(item[2]);
                                break;
                        }
                        mileage_all.Add(mileage);
                    }
                }
                catch
                {
                    mileage_all = new List<Mileage>();//解析失败里程为空
                }
            }

            ///判断加载的里程顺序，如里程为倒序则修正为正序
            ///在设定的扫描流程中不会出现里程顺序正反交替的情况
            if (mileage_all.Count > 2)
            {
                if (mileage_all[0].M > mileage_all[mileage_all.Count - 1].M && mileage_all[0].M > mileage_all[mileage_all.Count / 2].M)
                {
                    foreach (var v in mileage_all)
                    {
                        v.M = -v.M;
                    }
                }
            }
        }
        /// <summary>
        /// 获取单点对应里程
        /// </summary>
        /// <param name="mileage">总里程数据</param>
        /// <param name="startTime">扫描点起始时间</param>
        /// <param name="autotime">当前扫描点时间</param>
        /// <param name="m">对应里程</param>
        /// <returns></returns>
        public static bool Get_Mileage(List<Mileage> mileage, long startTime, ulong autotime, out double m)
        {
            ///如果扫描点时间小于当前里程起始时间，对应里程为起始里程
            ///如果超出里程范围将当前数据最后一段里程延伸
            ///中间区间里程采用线性计算
            m = mileage[0].M;
            long time = startTime + (long)(autotime * Autotime_pra);//扫描单点自动化时间转化成正常UTC时间，与里程的时间匹配
            if (time > mileage[0].Time)
            {
                for (int n = 1; n < mileage.Count - 1; n++)
                {
                    if (mileage[n].Time >= time && mileage[n - 1].Time <= time)
                    {
                        m = mileage[n - 1].M + (mileage[n].M - mileage[n - 1].M) * (time - mileage[n - 1].Time) / (mileage[n].Time - mileage[n - 1].Time);
                        return true;
                    }
                }
                //时间time大于里程最大时间时，基于里程最后变化率计算对应里程
                m = mileage[mileage.Count - 1 - 1].M + (mileage[mileage.Count - 1].M - mileage[mileage.Count - 1 - 1].M) * (time - mileage[mileage.Count - 1 - 1].Time) / (mileage[mileage.Count - 1].Time - mileage[mileage.Count - 1 - 1].Time);

            }
            return false;
        }
#endregion

#region 索引
        /// <summary>
        /// 获取影像上点对应点云中的行列数
        /// </summary>
        /// <param name="w">像素横坐标</param>
        /// <param name="h">像素纵坐标</param>
        /// <param name="hmin">最小纵坐标</param>
        /// <param name="wmin">最小横坐标</param>
        /// <param name="expand">影像分辨率（像素/米）</param>
        /// <param name="intR">行列信息</param>
        public void GetRank(int w, int h, int hmin, int wmin, int expand, out int[] intR)
        {
            double[] P = new double[2];
            P[0] = (1.0 * w + wmin) / expand;
            P[1] = (1.0 * h + hmin) / expand;
            if (P[0] >= m1) intR = FindRankNumber(ap, lme, ellipse, P, 0);
            else intR = FindRankNumber(ap_pre, lm_pre, ellipse, P, numCol / 2 - ap_pre.Length);
        }
        /// <summary>
        /// 单点坐标输入获取对应点
        /// </summary>
        /// <param name="ap">点云，二维数组，第一维对应里程y，第二维对应xe</param>
        /// <param name="lme">点云对应的里程链表，保存扫描数据一定圈数下对应里程</param>
        /// <param name="ellipse">椭圆信息</param>
        /// <param name="p">{y,xe}输入的坐标</param>
        /// <param name="realLength">lme对应原始一整块文件，输入的ap可能只是部分，realLength表示ap起始圈在对应原始数据中的圈数</param>
        /// <returns></returns>
        static int[] FindRankNumber(Faro_point[][] ap, List<MileageEllipse> lme, Ellipse ellipse, double[] p, int realLength)
        {
            double y = p[0], xe = p[1];
            double ex = ellipse.X0, ey = ellipse.Y0, r = (ellipse.A + ellipse.B) / 2;
#region 求解对应点第一维（y）下标
            int ry = 0;
            //if (y < lme[0].mileage || y > lme[lme.Count - 1].mileage) { return new int[] { 0, 0 }; }//小于起始里程或大于终止里程
            if (y < lme[0].mileage) { ry = 0; }
            else if (y > lme[lme.Count - 1].mileage) { ry = ap.Length - 1; }
            else
            {
                double dy1 = (lme[lme.Count - 1].mileage - lme[0].mileage) / (lme.Count - 1);//里程链表数据大致线性
                                                                                             //先确定y在里程链表lme下的下标
                ry = (int)Math.Floor((y - lme[0].mileage) / dy1);
                while (y >= lme[ry].mileage)
                {
                    if (ry > lme.Count - 3) { ry = lme.Count - 2; break; }
                    else if (y < lme[ry + 1].mileage) { break; }
                    else { ry++; }
                }
                while (y < lme[ry].mileage)
                {
                    //if (ry - 1 < 0) { break; }//此情况不会出现因为p[0]>=lm[0][0]
                    if (y >= lme[ry - 1].mileage) { ry--; break; }
                    else { ry--; }
                }
                //里程链表lme相邻里程为实际一定圈数扫描数据的前后里程，每圈间里程间隔为dm，以此确定y对应的具体环数，即最终第一维下标
                ry = (int)(Math.Floor((y - lme[ry].mileage) / lme[ry].dm) + lme[ry].col) - realLength;
                if (ry < 0) { ry = 0; }//考虑后续可能会出现ap首尾里程与lme未对应
                else if (ry > ap.Length - 1) { ry = ap.Length - 1; }
                else
                {
                    while (y >= ap[ry][0].Y)
                    {
                        if (ry == ap.Length - 1) { break; }
                        else if (y < ap[ry + 1][0].Y) { break; }
                        else { ry++; }
                    }
                    while (y < ap[ry][0].Y)
                    {
                        if (ry == 0) { break; }//特殊情况，可能出现
                        else if (y >= ap[ry - 1][0].Y) { ry--; break; }
                        else { ry--; }
                    }
                }
            }
#endregion

#region 求解对应点第二维（xe）下标
            int numRow_1 = ap[ry].Length - 1;
            if (xe < ap[ry][0].xe) { return new int[] { ry, 0 }; }
            if (xe > ap[ry][numRow_1].xe) { return new int[] { ry, numRow_1 }; }
            double angle1 = xe / r - Math.PI / 2;
            double angle2 = Math.Atan2(ex + Math.Cos(angle1), ey + Math.Sin(angle1));
            int rxe = (int)((0.5 - angle2 / (10.0 / 6 * Math.PI)) * numRow_1);
            if (rxe < 0) { rxe = 0; }
            else if (rxe > numRow_1) { rxe = numRow_1; }
            else
            {
                while (xe >= ap[ry][rxe].xe)
                {
                    if (rxe == numRow_1) { break; }
                    else if (xe < ap[ry][rxe + 1].xe) { break; }
                    else { rxe++; }
                }
                while (xe < ap[ry][rxe].xe)
                {
                    if (rxe == 0) { break; }
                    else if (xe >= ap[ry][rxe - 1].xe) { rxe--; break; }
                    else { rxe--; }
                }
            }
#endregion

            return new int[] { ry, rxe };
        }
#endregion

#region 错台计算
        static int idA = 0;//标记的错台序号
        /// <summary>
        /// 查找环间缝，根据输入的坐标在同一行中提取其左右两侧数据进行查找
        /// </summary>
        /// <param name="space">点云数据</param>
        /// <param name="c">输入坐标</param>
        /// <param name="message">查找过程信息</param>
        /// <param name="seamHeight">错台两侧高度</param>
        /// <param name="seamPoint">错台两端点</param>
        /// <param name="oneSideLength">往两侧延伸的长度</param>
        /// <returns></returns>
        static bool FindSeamBetweenRings(Space_Analysis space, double[] c, out string message, out double[] seamHeight, out Faro_point[] seamPoint, out List<Faro_point> llfp, double oneSideLength = 0.1)
        {
            llfp = new List<Faro_point>();
            seamHeight = new double[6]; seamPoint = new Faro_point[3];
            Faro_point[][] ap = space.ap; message = "";
            Faro_point[][] ap_pre = space.ap_pre; int col_pre = ap_pre.Length;
            int[] rank = FindRankNumber(space.ap, space.lme, space.ellipse, c, 0);
            List<Faro_point> lp = new List<Faro_point>(); double Xmid = 0;
            if (rank[0] < space.lme[0].mileage && col_pre > 0)
            {
                rank = FindRankNumber(ap_pre, space.lm_pre, space.ellipse, c, space.numCol / 2 - col_pre);
                double dr = 0; int col = rank[0] - 1;
                int numLeft = 0;
                while (dr < oneSideLength)
                {
                    if (col < 0)
                    {
                        break;
                    }
                    if (ap_pre[col][rank[1]].H > -0.01) { lp.Insert(0, ap_pre[col][rank[1]]); numLeft++; }
                    dr = ap_pre[rank[0]][rank[1]].Y - ap_pre[col][rank[1]].Y;
                    col--;
                }
                if (numLeft < 5) { message = "左侧点过少"; return false; }
                if (ap_pre[rank[0]][rank[1]].H > -0.01) { lp.Add(ap_pre[rank[0]][rank[1]]); }
                dr = 0; col = rank[0] + 1;
                int numRight = 0;
                while (dr < oneSideLength)
                {
                    if (col > ap_pre.Length - 1)
                    {
                        if (col - col_pre >= ap.Length) break;
                        else
                        {
                            if (ap[col - col_pre][rank[1]].H > -0.01) { lp.Add(ap[col - col_pre][rank[1]]); numLeft++; }
                            dr = ap[col - col_pre][rank[1]].Y - ap_pre[rank[0]][rank[1]].Y;
                            col++;
                            continue;
                        }
                    }
                    if (ap_pre[col][rank[1]].H > -0.01) { lp.Add(ap_pre[col][rank[1]]); numRight++; }
                    dr = ap_pre[col][rank[1]].Y - ap_pre[rank[0]][rank[1]].Y;
                    col++;
                }
                if (numRight < 5) { message = "右侧点过少"; return false; }
                Xmid = ap_pre[rank[0]][rank[1]].Y;
            }//中点在左侧文件
            else
            {
                double dr = 0; int col = rank[0] - 1;
                int numLeft = 0;
                //获取最近点左侧一段点集
                while (dr < oneSideLength)
                {
                    ///col小于零即点处于上一个文件
                    if (col < 0)
                    {
                        if (-col > col_pre) break;
                        else
                        {
                            //H超过一定范围可能是干扰点，不参与后续计算，下同
                            if (ap_pre[col + col_pre][rank[1]].H > -0.01) { lp.Insert(0, ap_pre[col + col_pre][rank[1]]); numLeft++; }
                            dr = ap[rank[0]][rank[1]].Y - ap_pre[col + col_pre][rank[1]].Y;
                            col--;
                            continue;
                        }
                    }
                    //H超过一定范围可能是干扰点，不参与后续计算，下同
                    if (ap[col][rank[1]].H > -0.01) { lp.Insert(0, ap[col][rank[1]]); numLeft++; }
                    dr = ap[rank[0]][rank[1]].Y - ap[col][rank[1]].Y;
                    col--;
                }
                if (numLeft < 5) { message = "左侧点过少"; return false; }
                if (ap[rank[0]][rank[1]].H > -0.01) { lp.Add(ap[rank[0]][rank[1]]); }
                dr = 0; col = rank[0] + 1;
                int numRight = 0;
                //获取最近点右侧一段点集
                while (dr < oneSideLength)
                {
                    if (col > ap.Length - 1) { break; }
                    if (ap[col][rank[1]].H > -0.01) { lp.Add(ap[col][rank[1]]); numRight++; }
                    dr = ap[col][rank[1]].Y - ap[rank[0]][rank[1]].Y;
                    col++;
                }
                if (numRight < 5) { message = "右侧点过少"; return false; }
                Xmid = ap[rank[0]][rank[1]].Y;
            }//中点在右侧文件
            double[] h = new double[lp.Count];
            double[] x = new double[lp.Count];
            //获取点集后，环间缝提取隧道纵向数据，此方向选取单点(x^2+z^2)^0.5和y进行判断
            for (int i = 0; i < lp.Count; i++)
            {
                h[i] = Math.Sqrt(lp[i].X * lp[i].X + lp[i].Z * lp[i].Z);
                x[i] = lp[i].Y;
            }

            idA++;
            //对输入数据查找缝
            if (SplitLine(h, x, Xmid, out seamHeight, out int[] seamIndex, out message))
            {
                //seamIndex为缝两段在lp中的序号
                seamPoint = new Faro_point[] { lp[seamIndex[0]], lp[seamIndex[1]], lp[seamIndex[2]] };
                for (int i = 0; i < lp.Count; i++)
                {
                    if (i == (int)seamIndex[0])
                    {
                        llfp.Add(new Faro_point() { X = lp[i].Y, Y = lp[i].xe, Z = h[i], tag = 1, Color = idA });
                    }
                    else if (i == (int)seamIndex[1])
                    {
                        llfp.Add(new Faro_point() { X = lp[i].Y, Y = lp[i].xe, Z = h[i], tag = 1, Color = idA });
                    }
                    else if (lp[i].Y == ap[rank[0]][rank[1]].Y && lp[i].xe == ap[rank[0]][rank[1]].xe)
                    {
                        llfp.Add(new Faro_point() { X = lp[i].Y, Y = lp[i].xe, Z = h[i], tag = 2, Color = idA });
                    }
                    else
                    {
                        llfp.Add(new Faro_point() { X = lp[i].Y, Y = lp[i].xe, Z = h[i], tag = 0, Color = idA });
                    }
                }
                return true;
            }

            return false;
        }
        /// <summary>
        /// 查找环内缝，根据输入的坐标在同一列中提取其上下两侧数据进行查找
        /// </summary>
        /// <param name="space">点云数据</param>
        /// <param name="c">输入坐标</param>
        /// <param name="message">查找过程信息</param>
        /// <param name="seamHeight">错台两侧高度</param>
        /// <param name="seamPoint">错台两端点</param>
        /// <param name="oneSideLength">往两侧延伸的长度</param>
        /// <returns></returns>
        static bool FindSeamInsideRing(Space_Analysis space, double[] c, out string message, out double[] seamHeight, out Faro_point[] seamPoint, double oneSideLength = 0.1)
        {
            seamHeight = new double[2]; seamPoint = new Faro_point[2];
            Faro_point[][] ap = space.ap; message = "出错";
            Faro_point[][] ap_pre = space.ap_pre; int col_pre = ap_pre.Length;
            try
            {
                ///判断环内缝所处文件
                List<Faro_point> lp = new List<Faro_point>();
                if (c[0] < space.m1 && col_pre > 0)
                {
                    int[] rank = FindRankNumber(ap_pre, space.lm_pre, space.ellipse, c, space.numCol / 2 - col_pre);
                    double dr = 0; int row = rank[1] - 1;
                    int numLeft = 0;
                    while (dr < oneSideLength)
                    {
                        if (row < 0) { break; }
                        if (ap_pre[rank[0]][row].H > -0.01 && ap_pre[rank[0]][row].H < 0.3) { lp.Insert(0, ap_pre[rank[0]][row]); numLeft++; }
                        dr = ap_pre[rank[0]][rank[1]].xe - ap_pre[rank[0]][row].xe;
                        row--;
                    }
                    if (numLeft < 5) { message = "左侧点过少"; return false; }
                    if (ap_pre[rank[0]][rank[1]].H > -0.01) { lp.Add(ap_pre[rank[0]][rank[1]]); }
                    dr = 0; row = rank[1] + 1;
                    int numRight = 0;
                    while (dr < oneSideLength)
                    {
                        if (row > ap_pre[0].Length - 1) { break; }
                        if (ap_pre[rank[0]][row].H > -0.01 && ap_pre[rank[0]][row].H < 0.3) { lp.Add(ap_pre[rank[0]][row]); numRight++; }
                        dr = ap_pre[rank[0]][row].xe - ap_pre[rank[0]][rank[1]].xe;
                        row++;
                    }
                    if (numRight < 5) { message = "右侧点过少"; return false; }
                    double Xmid = ap_pre[rank[0]][rank[1]].xe;
                    double[] h = new double[lp.Count];
                    double[] x = new double[lp.Count];
                    for (int i = 0; i < lp.Count; i++)
                    {
                        h[i] = lp[i].H + lp[i].Hs;
                        x[i] = lp[i].xe;
                    }

                    List<string> ls1_2 = new List<string>();
                    //idB++;
                    //Faro_point FP1 = new Faro_point(); Faro_point FP2 = new Faro_point();
                    if (SplitLine(h, x, Xmid, out seamHeight, out int[] seamIndex, out message))
                    {
                        seamPoint = new Faro_point[] { lp[seamIndex[0]], lp[seamIndex[1]] };
                        return true;
                    }
                }
                else
                {
                    int[] rank = FindRankNumber(space.ap, space.lme, space.ellipse, c, 0);
                    double dr = 0; int col = rank[1] - 1;
                    //获取最近点左侧一段点集
                    int numLeft = 0;
                    while (dr < oneSideLength)
                    {
                        if (col < 0) { break; }
                        if (ap[rank[0]][col].H > -0.01 && ap[rank[0]][col].H < 0.3) { lp.Insert(0, ap[rank[0]][col]); numLeft++; }
                        dr = ap[rank[0]][rank[1]].xe - ap[rank[0]][col].xe;
                        col--;
                    }
                    if (numLeft < 5) { message = "左侧点过少"; return false; }
                    if (ap[rank[0]][rank[1]].H > -0.01 && ap[rank[0]][rank[1]].H < 0.3) { lp.Add(ap[rank[0]][rank[1]]); }
                    dr = 0; col = rank[1] + 1;
                    //获取最近点右侧一段点集
                    int numRight = 0;
                    while (dr < oneSideLength)
                    {
                        if (col > ap[0].Length - 1) { break; }
                        if (ap[rank[0]][col].H > -0.01 && ap[rank[0]][col].H < 0.3) { lp.Add(ap[rank[0]][col]); numRight++; }
                        dr = ap[rank[0]][col].xe - ap[rank[0]][rank[1]].xe;
                        col++;
                    }
                    if (numRight < 5) { message = "右侧点过少"; return false; }
                    double Xmid = ap[rank[0]][rank[1]].xe;
                    double[] h = new double[lp.Count];
                    double[] x = new double[lp.Count];
                    //获取点集后，环内缝提取隧道横向数据，此方向选取单点基于圆心方向到拟合椭圆的距离和xe进行判断
                    int tme = 1;
                    for (; tme < space.lme.Count; tme++)//搜索对应椭圆
                    {
                        if (rank[0] < space.lme[tme].col)
                        {
                            tme--;
                            break;
                        }
                    }
                    Ellipse ep = space.lme[tme].ep;
                    for (int i = 0; i < lp.Count; i++)
                    {
                        //单点与拟合椭圆的误差和xe
                        double ae = Math.Atan2(lp[i].Z - ep.Y0, lp[i].X - ep.X0) - ep.Angle;
                        double re = ep.A * ep.B / Math.Sqrt(Math.Pow(ep.A * Math.Sin(ae), 2) + Math.Pow(Math.Cos(ae) * ep.B, 2));
                        h[i] = Math.Sqrt(Math.Pow(lp[i].Z - ep.Y0, 2) + Math.Pow(lp[i].X - ep.X0, 2)) - re;
                        x[i] = lp[i].xe;
                    }
                    //对输入数据查找缝
                    if (SplitLine(h, x, Xmid, out seamHeight, out int[] seamIndex, out message))
                    {
                        //seamIndex为缝两段在lp中的序号
                        seamPoint = new Faro_point[] { lp[seamIndex[0]], lp[seamIndex[1]] };
                        return true;
                    }
                }
                return false;
            }
            catch (Exception e) { return false; }
        }

        /// <summary>
        /// 查找环间缝，根据输入的坐标在同一行中提取其左右两侧数据进行查找
        /// </summary>
        /// <param name="space">点云数据</param>
        /// <param name="c">输入坐标</param>
        /// <param name="message">查找过程信息</param>
        /// <param name="seamHeight">错台两侧高度</param>
        /// <param name="seamPoint">错台两端点</param>
        /// <param name="oneSideLength">往两侧延伸的长度</param>
        /// <returns></returns>
        static bool FindSeamBetweenRings_0(Space_Analysis space, double[] c, out string message, out double[] seamHeight, out Faro_point[] seamPoint, double oneSideLength = 0.1)
        {
            seamHeight = new double[2]; seamPoint = new Faro_point[2];
            Faro_point[][] ap = space.ap;
            //先根据输入的坐标找到最近点
            List<Faro_point> lp = new List<Faro_point>();
            int[] rank = FindRankNumber(space.ap, space.lme, space.ellipse, c, 0);
            double dr = 0; int row = rank[0] - 1;
            //获取最近点左侧一段点集
            int numLeft = 0;
            while (dr < oneSideLength)
            {
                if (row < 0) { break; }
                //H超过一定范围可能是干扰点，不参与后续计算，下同
                if (ap[row][rank[1]].H > -0.01 && ap[row][rank[1]].H < 0.3) { lp.Insert(0, ap[row][rank[1]]); numLeft++; }
                dr = ap[rank[0]][rank[1]].Y - ap[row][rank[1]].Y;
                row--;
            }
            if (numLeft < 5) { message = "左侧点过少"; return false; }
            if (ap[rank[0]][rank[1]].H > -0.01 && ap[rank[0]][rank[1]].H < 0.3) { lp.Add(ap[rank[0]][rank[1]]); }
            dr = 0; row = rank[0] + 1;
            //获取最近点右侧一段点集
            int numRight = 0;
            while (dr < oneSideLength)
            {
                if (row > ap.Length - 1) { break; }
                if (ap[row][rank[1]].H > -0.01 && ap[row][rank[1]].H < 0.3) { lp.Add(ap[row][rank[1]]); numRight++; }
                dr = ap[row][rank[1]].Y - ap[rank[0]][rank[1]].Y;
                row++;
            }
            if (numRight < 5) { message = "右侧点过少"; return false; }
            double Xmid = ap[rank[0]][rank[1]].Y;
            double[] h = new double[lp.Count];
            double[] x = new double[lp.Count];
            //获取点集后，环间缝提取隧道纵向数据，此方向选取单点(x^2+z^2)^0.5和y进行判断
            for (int i = 0; i < lp.Count; i++)
            {
                h[i] = Math.Sqrt(lp[i].X * lp[i].X + lp[i].Z * lp[i].Z);
                x[i] = lp[i].Y;
            }
            //对输入数据查找缝
            if (SplitLine(h, x, Xmid, out seamHeight, out int[] seamIndex, out message))
            {
                //seamIndex为缝两段在lp中的序号
                seamPoint = new Faro_point[] { lp[seamIndex[0]], lp[seamIndex[1]] };
                return true;
            }
            return false;
        }

        /// <summary>
        /// 查找环内缝，根据输入的坐标在同一列中提取其上下两侧数据进行查找
        /// </summary>
        /// <param name="space">点云数据</param>
        /// <param name="c">输入坐标</param>
        /// <param name="message">查找过程信息</param>
        /// <param name="seamHeight">错台两侧高度</param>
        /// <param name="seamPoint">错台两端点</param>
        /// <param name="oneSideLength">往两侧延伸的长度</param>
        /// <returns></returns>
        static bool FindSeamInsideRing_0(Space_Analysis space, double[] c, out string message, out double[] seamHeight, out Faro_point[] seamPoint, double oneSideLength = 0.1)
        {
            seamHeight = new double[2]; seamPoint = new Faro_point[2];
            Faro_point[][] ap = space.ap;
            //先根据输入的坐标找到最近点
            List<Faro_point> lp = new List<Faro_point>();
            int[] rank = FindRankNumber(space.ap, space.lme, space.ellipse, c, 0);
            double dr = 0; int col = rank[1] - 1;
            //获取最近点左侧一段点集
            int numLeft = 0;
            while (dr < oneSideLength)
            {
                if (col < 0) { break; }
                if (ap[rank[0]][col].H > -0.01 && ap[rank[0]][col].H < 0.3) { lp.Insert(0, ap[rank[0]][col]); numLeft++; }
                dr = ap[rank[0]][rank[1]].xe - ap[rank[0]][col].xe;
                col--;
            }
            if (numLeft < 5) { message = "左侧点过少"; return false; }
            if (ap[rank[0]][rank[1]].H > -0.01 && ap[rank[0]][rank[1]].H < 0.3) { lp.Add(ap[rank[0]][rank[1]]); }
            dr = 0; col = rank[1] + 1;
            //获取最近点右侧一段点集
            int numRight = 0;
            while (dr < oneSideLength)
            {
                if (col > ap[0].Length - 1) { break; }
                if (ap[rank[0]][col].H > -0.01 && ap[rank[0]][col].H < 0.3) { lp.Add(ap[rank[0]][col]); numRight++; }
                dr = ap[rank[0]][col].xe - ap[rank[0]][rank[1]].xe;
                col++;
            }
            if (numRight < 5) { message = "右侧点过少"; return false; }
            double Xmid = ap[rank[0]][rank[1]].xe;
            double[] h = new double[lp.Count];
            double[] x = new double[lp.Count];
            //获取点集后，环内缝提取隧道横向数据，此方向选取单点基于圆心方向到拟合椭圆的距离和xe进行判断
            int tme = 1;
            for (; tme < space.lme.Count; tme++)//搜索对应椭圆
            {
                if (rank[0] < space.lme[tme].col)
                {
                    tme--;
                    break;
                }
            }
            Ellipse ep = space.lme[tme].ep;
            for (int i = 0; i < lp.Count; i++)
            {
                //单点与拟合椭圆的误差和xe
                double ae = Math.Atan2(lp[i].Z - ep.Y0, lp[i].X - ep.X0) - ep.Angle;
                double re = ep.A * ep.B / Math.Sqrt(Math.Pow(ep.A * Math.Sin(ae), 2) + Math.Pow(Math.Cos(ae) * ep.B, 2));
                h[i] = Math.Sqrt(Math.Pow(lp[i].Z - ep.Y0, 2) + Math.Pow(lp[i].X - ep.X0, 2)) - re;
                x[i] = lp[i].xe;
            }
            //对输入数据查找缝
            if (SplitLine(h, x, Xmid, out seamHeight, out int[] seamIndex, out message))
            {
                //seamIndex为缝两段在lp中的序号
                seamPoint = new Faro_point[] { lp[seamIndex[0]], lp[seamIndex[1]] };
                return true;
            }
            return false;

        }
#endregion

#region 相应计算函数
        /// <summary>
        /// 输入单环断面基于初步椭圆拟合的深度数据，提取管壁点（旧版）
        /// 可用于曲线的平滑处理
        /// </summary>
        /// <param name="lp">二维点集</param>
        /// <returns></returns>
        static double[] TunnelClassify_old(List<Faro_point> lp)
        {
            int n = lp.Count;
            double[] dh = new double[n];
            for (int i = 1; i < n; i++)
            { dh[i] = lp[i].H - lp[i - 1].H; }
            double[] dh1 = MovingSmooth(dh, 5);
            double mean = 0, var = 0;
            double[] gs11 = dh1.Where(d => Math.Abs(d) < 0.005).ToArray();
            mean = gs11.Average();
            for (int i = 0; i < gs11.Length; i++)
            { var += gs11[i] * gs11[i]; }
            var = var / gs11.Length - mean * mean;//s^2=E(x^2)-E(x)^2
            var = Math.Max(Math.Sqrt(var), 0.0005);
            double[] s = new double[n];
            int st = 0; double sd = 0, lt = 0.02, ht = 0.02;
            for (int i = 1; i < n; i++)
            {
                double hd = Math.Min(Math.Abs(dh[i]), Math.Abs(dh1[i]));
                if (hd < mean + var && hd > mean - var)
                {
                    st += 1;
                    sd += lp[i].xe - lp[i - 1].xe;
                    if (sd > lt && (Math.Abs(lp[i].H) < ht || Math.Abs(lp[i].H - s[i - 1]) < ht))
                    {
                        s[i] = (lp[i].H * 4 + s[i - 1] * 3) / 7;
                        for (int j = st - 1; j > 0; j--)
                        {
                            s[i - j] = (lp[i - j].H * 3 + s[i - j - 1] * 4) / 7;
                        }
                    }
                    else { s[i] = s[i - 1]; }
                }
                else { st = 0; sd = 0; s[i] = s[i - 1]; }
            }
            return s;
        }
        /// <summary>
        /// 移动平均
        /// 步长输入后经计算后保证为奇数，若步长为n，取对应位加前后各(n-1)/2位取平均
        /// 即c[n]=mean(c[n-(n-1)/2]+..+c[n]+..c[n+(n-1)/2])
        /// 前后半步步长内，c[0]=x[0],c[1]=mean(x[0]+x[1]+x[2]),c[n]=mean(x[0]+..+x[2n])
        /// </summary>
        /// <param name="x">输入数组 </param>
        /// <param name="span">步长</param>
        /// <returns></returns>
        public static double[] MovingSmooth(double[] x, int span)
        {
            if (span < 2) { return x; }//步长小于3的直接返回原数组
            int n = x.Length;
            double[] c = new double[n];
            span = Math.Min(span, n);
            int width = span - 1 + span % 2;//保证步长为奇数
            if (width == 1) { return x; }//点数小于3或者步长小于3（步长为奇数）时返回有数组
            int hw = (width - 1) / 2;
            double[] cbegin = new double[(width - 1) / 2];
            double sum = 0, sum1 = 0;
            int len = width - 2;
            for (int i = 0; i < len; i++)//前后半步步长内
            {
                sum1 += x[n - 1 - i];
                sum += x[i];
                if (i % 2 == 0)
                {
                    c[n - 1 - i / 2] = sum1 / (i + 1);
                    c[i / 2] = sum / (i + 1);
                }
            }
            c[hw] = (sum + x[width - 2] + x[width - 1]) / width;
            len = n - hw;
            for (int i = hw + 1; i < len; i++)
            {
                c[i] = c[i - 1] + (x[i + hw] - x[i - 1 - hw]) / width;
            }
            return c;
        }
        /// <summary>
        /// 输入单环断面基于初步椭圆拟合的深度数据，提取管壁点
        /// 输入所需参数为xe（横坐标），h（纵坐标）
        /// 返回的数组s代表对应xe的管壁高度的趋势
        /// </summary>
        /// <param name="lp">二维点集</param>
        /// <returns></returns>
        public static double[] TunnelClassify(List<Faro_point> lp)
        {
#region 按一定间距抽稀，减少梯度计算时的波动
            double d_xe = 0.004;//抽稀间距
            double xe_now = lp[0].xe;
            List<Faro_point> lp_downsample = new List<Faro_point>();
            List<int> li = new List<int>();
            lp_downsample.Add(lp[0]);
            li.Add(0);
            for (int i = 1; i < lp.Count; i++)
            {
                double t = Math.Abs((lp[i].H - lp[i - 1].H) / (lp[i].xe - lp[i - 1].xe));
                if (t > 3 || (lp[i].xe - xe_now) > d_xe)//保留梯度较大点
                {
                    lp_downsample.Add(lp[i]);
                    li.Add(i);
                    xe_now = lp[i].xe;
                }
            }
            int n = lp_downsample.Count;
            if (n < 2) { return new double[n]; }
#endregion
            //---------------------------------------------------------------------
#region 计算梯度及其均值方差
            double[] H = lp_downsample.Select(p => p.H).ToArray();
            double[] xe = lp_downsample.Select(p => p.xe).ToArray();
            double[] H1 = H;
            H = MovingSmooth(H, 5);//移动平均滤波
            for (int i = 0; i < H.Length; i++)
            {
                if (Math.Abs(H1[i] - H[i]) > 0.003)
                {
                    H[i] = H1[i];//减少均值滤波在变化率较大处带来的失真
                }
            }
            double[] grad = new double[n];//梯度
            for (int i = 1; i < n; i++)
            { grad[i] = (H[i] - H[i - 1]) / (xe[i] - xe[i - 1]); }
            double mean = 0, var1 = 0;
            //0.15固定阈值的选择会决定对梯度变化的敏感度
            double[] gs11 = grad.Where(d => Math.Abs(d) < 0.15).ToArray();
            mean = gs11.Average();
            for (int i = 0; i < gs11.Length; i++)
            { var1 += gs11[i] * gs11[i]; }
            var1 = var1 / gs11.Length - mean * mean;//s^2=E(x^2)-E(x)^2
            var1 = Math.Sqrt(var1) * 2;
            double var2 = var1 * 2;
#endregion
            //---------------------------------------------------------------------
#region 对于每个点，通过梯度的方差及均值判断是处于平稳，上升还是下降，对应数值0，1，-1，保存在asn中
            int[] asn = new int[n];
            double[] hd = new double[n];
            hd[1] = grad[1] - mean;
            for (int i = 2; i < n; i++)
            {
                hd[i] = grad[i] - mean;
                if (hd[i - 1] > var1)
                {
                    //大于var2取1，介于var2，var1之间，若其前后存在大于var1的情况，值取1
                    if (hd[i - 1] > var2) { asn[i - 1] = 1; }
                    else if (Math.Abs(hd[i - 2]) > var1 || Math.Abs(hd[i]) > var1) { asn[i - 1] = 1; }
                }
                else if (hd[i - 1] < -var1)
                {
                    //小于-var2取-1，介于var2，var1之间，若其前后存在小于-var1的情况，值取-1
                    if (hd[i - 1] < -var2) { asn[i - 1] = -1; }
                    else if (Math.Abs(hd[i - 2]) > var1 || Math.Abs(hd[i]) > var1) { asn[i - 1] = -1; }
                }
            }
            if (hd[n - 1] > var1) { asn[n - 1] = 1; }
            else if (hd[n - 1] < -var1) { asn[n - 1] = -1; }

            for (int i = 1; i < n - 1; i++)
            {
                //存在单点稳定时将该点状态改为其领域状态
                if (asn[i] != asn[i - 1] && asn[i + 1] == asn[i - 1] && Math.Abs(hd[i]) < var2)
                {
                    asn[i] = asn[i - 1];
                    i++;
                }
            }
#endregion
            //---------------------------------------------------------------------
#region 主体计算部分
            //以下会遍历数据，根据每个点0，1，-1三种状态，将相邻同种状态点当成一个集合，再对集合判断其是否属于管壁点
            //状态为0，集合长度及相应偏离高度在一定合理范围则会判定为管壁点，参与管壁趋势的计算
            //判断时需要结合前后情况，以下三个变量存储前后及当下三个集合对应的首尾下标
            double lt = 0.01, lt1 = 0.1, ht = 0.01, ht1 = 0.02;//距离（xe）差及高度（h）差的相关阈值
            double[] s = new double[lp.Count];//代表管壁趋势
            int si = 0;//s最新更新到的位置
            int[] stateBefore = { 0, 1 }, stateNow, stateAfter;
            int sn = asn[1];//当前状态
            int k = 2;//通过k值进行遍历
            while (k < n && asn[k] == sn)//获取第一个集合的前后下标
            {
                stateBefore[1] = k;
                k++;
            }
            //sn=0是判定为管壁点的前提
            if (sn == 0 && (xe[k - 1] - xe[0]) > lt && Math.Abs(H[k - 1]) < ht)
            {
                for (int j = 1; j <= li[k - 1]; j++)
                {
                    s[j] = (lp[j].H * 6 + s[j - 1]) / 7;//判定为管壁点，参与s的更新计算
                }
                si = li[k - 1];
            }
            if (k < n)//首个集合未包含所有数据的情况（大概率情况）
            {
                int sb = sn, sa;//sb为上一状态，sa为下一状态
                sn = asn[k];
                stateNow = new int[] { k - 1, k };//起始下标与前一状态的结尾下标相同
                k++;
                while (k < n && asn[k] == sn)
                {
                    stateNow[1] = k;
                    k++;
                }
                if (k == n)
                {
                    //由于为每个集合的判断需要依靠前后，当stateNow遍历到最后，k回退一步，构造stateAter为{n-2，n-1}
                    k = n - 1;
                }
                bool end = false;
                while (end == false)
                {
                    if (k == n - 1)
                    {
                        //此处k=n-1说明已遍历最后并已构造最后的stateAfter
                        end = true;
                    }
                    stateAfter = new int[] { k - 1, k };
                    sa = asn[k];
                    k++;
                    while (k < n && asn[k] == sa)
                    {
                        stateAfter[1] = k;
                        k++;
                    }
                    int type = 0;//type=1时表示判断为管壁
                    if (sn == 0)
                    {
                        double sd = xe[stateNow[1]] - xe[stateNow[0]];//当前stateNow长度
                        double drg0 = Math.Abs(H[stateNow[0] + 1]);//起点与0的差值
                        double drg1 = Math.Abs(H[stateNow[0] + 1] - s[si]);//起点与趋势s的最新值的差值
                        if (drg0 < 0.05 || drg1 < 0.05)//差值过大跳过
                        {
                            if (sd > lt1)
                            {
                                //长度超过lt1（0.1）时，sd从0.1到0.5，高度阈值ht1从0.02到0.03，sd大于0.5，ht1=0.03
                                ht1 = 0.02 + 0.025 * (sd - lt1);//0.02+(0.03-0.02)*(sd-lt1)/(0.5-lt1);
                                ht1 = Math.Min(0.03, ht1);
                                if (drg0 < ht1 || drg1 < ht1) { type = 1; }
                                else
                                {
                                    //依据起点判断不符合后遍历stateNow之后的点进行判断
                                    for (int j = stateNow[0] + 2; j <= stateNow[1]; j++)
                                    {
                                        if (Math.Abs(H[j]) < ht1 || Math.Abs(H[j] - s[si]) < ht1)
                                        {
                                            type = 1; break;
                                        }
                                    }
                                }
                            }
                            else if (sd > lt)
                            {
                                //if (drg0 < ht || drg1 < ht)
                                //长度超过lt时，sd从lt到0.1，高度阈值ht从0.01到0.015
                                ht = 0.01 + 0.005 * (sd - lt) / 0.09;//0.01+(0.015-0.01)*(sd-lt)/(0.1-lt);
                                if (drg1 < ht)
                                {
                                    double gradB = (H[stateBefore[1]] - H[stateBefore[0]]) / (xe[stateBefore[1]] - xe[stateBefore[0]]);
                                    double gradA = (H[stateAfter[1]] - H[stateAfter[0]]) / (xe[stateAfter[1]] - xe[stateAfter[0]]);
                                    if (Math.Abs(gradB) > 0.3 || Math.Abs(gradA) > 0.3)
                                    {
                                        type = 1;
                                    }
                                }
                            }
                        }
                    }
                    //
                    if (type == 1)
                    {
                        //当前判断为管壁的集合与上一个判断为管壁的集合之间，线性更新s
                        double ds = (lp[li[stateNow[0]]].H - s[si]) / (li[stateNow[0]] - si);
                        for (int j = si + 1; j <= li[stateNow[0]]; j++)
                        {
                            s[j] = s[j - 1] + ds;
                        }
                        //当前集合参与s的更新计算
                        for (int j = li[stateNow[0]] + 1; j <= li[stateNow[1]]; j++)
                        {
                            s[j] = (lp[j].H * 6 + s[j - 1]) / 7;
                        }
                        si = li[stateNow[1]];
                    }
                    sb = sn; stateBefore = stateNow;
                    sn = sa; stateNow = stateAfter;
                    if (k == n)
                    {
                        //此处k=n，说明stateNow遍历到最后，k回退一步，构造stateAter为{n-2，n-1}
                        k = n - 1;
                    }
                }
            }
            //前面s的计算在lp_downsample.Count之内，由于lp_downsample是从lp中抽稀得到，会存在最后一段点未参与计算
            //对最后一段数据从s[si]到0线性更新

            if (si < lp.Count - 1)
            {
                double ds = (0 - s[si]) / (lp.Count - 1 - si);
                for (int j = si + 1; j < lp.Count; j++)
                {
                    s[j] = s[j - 1] + ds;
                }
            }
#endregion
            //---------------------------------------------------------------------
#region 减少深度图中对破损识别的细小干扰
            int len = 0;
            for (int i = 1; i < s.Length; i++)
            {
                double dh = lp[i].H - s[i];
                if (dh > 0.001 && dh < 0.005)
                {
                    len++;
                }
                else
                {
                    if (len > 0)
                    {
                        if ((lp[i - 1].xe - lp[i - len - 1].xe) < 0.01)
                        {
                            for (int j = 1; j <= len; j++)
                            {
                                s[i - j] = lp[i - j].H;
                            }
                        }
                    }
                    len = 0;
                }
            }
#endregion

            return s;
        }

        /// <summary>
        /// 对一段线状点集找到接缝处分为两段并计算两段高度差用于计算错台
        /// x需保证升序输入
        /// </summary>
        /// <param name="h">高度，纵坐标</param>
        /// <param name="x">横坐标</param>
        /// <param name="Xmid">给定的x中点值</param>
        /// <param name="seamHeight">两侧高度</param>
        /// <param name="seamIndex">缝首尾下标</param>
        /// <param name="message">反馈计算是否成功</param>
        /// <param name="seamLength">默认缝中心的位置与Xmid的距离在seamLength以内</param>
        /// <returns></returns>
        public static bool SplitLine(double[] h, double[] x, double Xmid, out double[] seamHeight, out int[] seamIndex, out string message, double seamLength = 0.03)
        {
            seamHeight = new double[6];
            seamIndex = new int[3];
            //seamHeight = new double[2];
            //seamIndex = new int[2];
            //根据梯度搜索缝
            double[] dh = new double[h.Length];
            for (int i = 0; i < h.Length - 1; i++)
            { dh[i] = h[i] - h[i + 1]; }
            dh[h.Length - 1] = 0;
            double mean, std;
            GetMeanAndStd(dh, out mean, out std);
            double th1 = mean + std;// th2 = mean - std;
            int on = 0;//0代表平稳状态，1代表不平稳状态。缝明显的时候附近是不平稳的
            List<int[]> out_1 = new List<int[]>();//保存不平稳区间对应首尾下标，可能有多个
            for (int k = 0; k < dh.Length - 1; k++)
            {
                //默认缝中心的位置与Xmid的距离在seamLength以内
                if ((x[k] - Xmid) < -seamLength) { continue; }
                if ((x[k] - Xmid) > seamLength) { break; }
                if (Math.Abs(dh[k]) > th1)
                {
                    if (on == 0)
                    { out_1.Add(new int[] { k, k + 1 }); on = 1; }//新增不平稳区间
                }
                else if (on == 1)
                { out_1[out_1.Count - 1][1] = k; on = 0; }//当前不平稳区间终止
            }
            List<double> hd1 = new List<double>();//缝前（左侧）的一段数据，用于计算一端高度
            List<double> hd2 = new List<double>();//缝后（右侧）的一段数据，用于计算另一端的高度
            int get1 = 0, get2 = 0;//用于保存缝的首尾下标
            if (out_1.Count > 0)
            {
#region 整合out_1的区间，合并相近的不平稳区间，保存在out_2中
                List<int[]> out_2 = new List<int[]>();
                out_2.Add(new int[] { out_1[0][0], out_1[0][1] });
                int k2 = 0;//记录out_2下标的变量
                for (int k = 1; k < out_1.Count; k++)
                {
                    double kt = Math.Abs(x[out_2[k2][1]] - x[out_1[k][0]]);//out_2第k2个区间与out_1第k个区间的距离
                    double kh1 = Math.Abs(h[out_2[k2][0]] - h[out_2[k2][1]]);//out_2第k2个区间的首尾高度差
                    double kh2 = Math.Abs(h[out_1[k][0]] - h[out_1[k][1]]);//out_1第k个区间的首尾高度差
                    //
                    if (kh1 > std && kh2 > std)//两个区间都不平稳时
                    {
                        if (kt < 0.005) { out_2[k2][1] = out_1[k][1]; }//距离小于0.005两个区间合并
                        else if (kt < 0.01)
                        {
                            if (kh1 > 3 * std || kh2 > 3 * std) { out_2[k2][1] = out_1[k][1]; }//距离小于0.01且两个区间存在一个高度差大于3*std，两个区间合并
                            else
                            {
                                out_2.Add(new int[] { out_1[k][0], out_1[k][1] });//out_2添加out_1第k个区间
                                k2++;
                            }
                        }
                        else if (kt < 0.015)
                        {
                            if (kh1 > 3 * std && kh2 > 3 * std) { out_2[k2][1] = out_1[k][1]; }//距离小于0.015且两个区间高度差都大于3*std，两个区间合并
                            else
                            {
                                out_2.Add(new int[] { out_1[k][0], out_1[k][1] });//out_2添加out_1第k个区间
                                k2++;
                            }
                        }
                        else
                        {
                            out_2.Add(new int[] { out_1[k][0], out_1[k][1] });//out_2添加out_1第k个区间
                            k2++;
                        }
                    }
                    else
                    {
                        out_2.Add(new int[] { out_1[k][0], out_1[k][1] });//out_2添加out_1第k个区间
                        k2++;
                    }
                }
#endregion

                double Min = 100;
                int get = 0;
                for (int k = 0; k < out_2.Count; k++)
                {
                    //计算out_2中区间中点与Xmid的距离，dc
                    double dc = Math.Abs(Xmid - (x[out_2[k][0]] + x[out_2[k][1]]) / 2);
                    if (dc > 0.03) { continue; }
                    else if (dc < 0.01) { dc = 0.01; }
                    double lk = 0;
                    for (int i = out_2[k][0]; i < out_2[k][1]; i++)
                    { lk += Math.Abs(dh[i]); }
                    dc = dc / lk;//此时dc变为区间内的平均梯度
                    //dc = dc / (x[out_2[k][1]] - x[out_2[k][0]]);
                    if (Min > dc) { Min = dc; get = k; }
                }
                get1 = out_2[get][0];//缝起始下标
                get2 = out_2[get][1];//缝终止下标
                //获取缝左侧数据
                for (int i = 0; i < out_2[0][0]; i++) { hd1.Add(h[i]); }
                for (int i = 0; i < get; i++)
                {
                    for (int j = out_2[i][1] + 1; j < out_2[i + 1][0]; j++)
                    { hd1.Add(h[j]); }
                }
                //获取缝右侧数据
                for (int i = get; i < out_2.Count - 1; i++)
                {
                    for (int j = out_2[i][1] + 1; j < out_2[i + 1][0]; j++)
                    { hd2.Add(h[j]); }
                }
                for (int i = out_2[out_2.Count - 1][1] + 1; i < h.Length; i++) { hd2.Add(h[i]); }
            }
            else//不存在不平稳区间时，将Xmid作为缝的位置
            {
                //获取缝左侧数据
                for (int i = 0; i < h.Length; i++)
                {
                    if (x[i] < Xmid) { hd1.Add(h[i]); }
                    else { get1 = i; break; }
                }
                get2 = get1;
                //获取缝右侧数据
                for (int i = get2 + 1; i < h.Length; i++) { hd2.Add(h[i]); }
            }
            double sh1, sh2;
            double std1, std2;
            if (GetHeightOfSide(hd1, 1, out sh1, out std1))//计算左侧高度
            {
                if (GetHeightOfSide(hd2, 1, out sh2, out std2))//计算右侧高度
                {
                    seamHeight[0] = sh1; seamHeight[1] = sh2;
                    seamIndex[0] = get1; seamIndex[1] = get2;
                    message = "成功获取";
                    ///start-----x补充
                    ///给找到的错台位置添加信息，更精准筛选出可用错台
                    double hmax = Math.Min(sh1, sh2);
                    double fh = 0;
                    int Di = get1;
                    for (int i = get1; i <= get2; i++)
                    {
                        if (Math.Abs(hmax - h[i]) > fh)
                        {
                            fh = Math.Abs(hmax - h[i]);
                            seamHeight[2] = h[i] - hmax;
                            seamHeight[3] = std1;
                            seamHeight[4] = std2;
                            Di = i;
                        }
                    }
                    if (Math.Abs(h[Di] - h[get1]) < 0.003 || Math.Abs(h[Di] - h[get2]) < 0.003)
                    {
                        Di = (get1 + get2) / 2;
                        seamHeight[2] = h[Di] - hmax;
                        seamHeight[5] = h[Di] - Math.Min(sh1, sh2);
                    }
                    else seamHeight[5] = h[Di] - (sh1 * (Di - get1) + sh2 * (get2 - Di)) / (get2 - get1 + 0.00000000000000001);
                    seamIndex[2] = Di;
                    ///end
                    return true;
                }
                else { message = "右侧获取失败"; }
            }
            else { message = "左侧获取失败"; }

            return false;
        }
        /// <summary>
        /// 对一段线状点集找到接缝处分为两段并计算两段高度差用于计算错台
        /// </summary>
        /// <param name="h"></param>
        /// <param name="x">x升序输入</param>
        /// <param name="Xmid">中点x值</param>
        /// <param name="seamHeight"></param>
        /// <param name="seamIndex"></param>
        /// <param name="message"></param>
        /// <returns></returns>

        /// <summary>
        /// 获取单侧高度（用于计算缝两边的错台量）
        /// </summary>
        /// <param name="h">输入的数据</param>
        /// <param name="t"></param>
        /// <param name="hs">返回的高度</param>
        /// <param name="std"></param>
        /// <returns></returns>
        static bool GetHeightOfSide(List<double> h, int t, out double hs, out double std)
        {
            hs = 0;
            double mean;
            GetMeanAndStd(h, out mean, out std);
            //方差较小时说明数据较平稳，选取跳动小的数据，取均值作为hs
            if (std < 0.002)
            {
                double th1 = mean + std * t, th2 = mean - std * t;
                int num = 0;
                foreach (var i in h)
                {
                    if (i < th1 && i > th2)
                    {
                        hs += i; num++;
                    }
                }
                if (num < 2) { return false; }
                hs = hs / num;

            }
            //方差大时可能数据干扰大，按step为步长，逐步查找可能存在的稳定区域并以该区域均值作为hs
            else
            {
                int step = 5; double th = 0.001;
                if (h.Count < step) { return false; }
                double m = 0; int max_num = 0;
                for (int i = 0; i < step; i++) { m += h[i]; }
                m = m / step;
                int num = h.Count(j => Math.Abs(j - m) < th);
                if (max_num < num) { max_num = num; hs = m; }
                for (int i = step; i < h.Count; i++)
                {
                    m = m + (h[i] - h[i - step]) / step;
                    num = h.Count(j => Math.Abs(j - m) < th);
                    if (max_num < num) { max_num = num; hs = m; }
                }
                if (max_num < 2) { return false; }
            }
            return true;
        }

        /// <summary>
        /// 获取均值方差
        /// </summary>
        /// <param name="x">输入数据集</param>
        /// <param name="mean">均值</param>
        /// <param name="std">方差</param>
        public static void GetMeanAndStd(double[] x, out double mean, out double std)
        {
            mean = 0; std = 0;
            for (int i = 0; i < x.Length; i++)
            { mean += x[i]; std += x[i] * x[i]; }
            mean = mean / x.Length;
            std = Math.Sqrt(std / x.Length - mean * mean);//s^2=E(x^2)-E(x)^2
        }

        /// <summary>
        /// 获取均值方差
        /// </summary>
        /// <param name="x">输入数据集</param>
        /// <param name="mean">均值</param>
        /// <param name="std">方差</param>
        public static void GetMeanAndStd(List<double> x, out double mean, out double std)
        {
            mean = 0; std = 0;
            for (int i = 0; i < x.Count; i++)
            { mean += x[i]; std += x[i] * x[i]; }
            mean = mean / x.Count;
            std = Math.Sqrt(std / x.Count - mean * mean);//s^2=E(x^2)-E(x)^2
        }
#endregion

#region 限界计算

        /// <summary>
        /// 生成断面限界图
        /// </summary>
        /// <param name="limitdata">限界点集</param>
        /// <param name="sec">断面点集</param>
        /// <param name="path">保存图片路径</param>
        /// <param name="LFP">选取的限界位置信息</param>
        public static void Get_Limit(List<Faro_point> limitdata, List<Faro_point> sec, string path, out List<Faro_point> LFP)
        {
            LFP = new List<Faro_point>();//选取的限界代表位置信息
            List<Faro_point> LP = new List<Faro_point>();//存储限界文件
            //double hmid = (limitdata.Max(l => l.Y) + limitdata.Min(l => l.Y)) / 2;
            //double xmid = (limitdata.Max(l => l.X) + limitdata.Min(l => l.X)) / 2;
            double hmid, xmid;//断面中心
            Get_limit_Circle(sec, out xmid, out hmid);

            ///以拟合椭圆圆心为中心，计算限界文件各个节点到中心的角度（底部开始逆时针方向）
            foreach (var v in limitdata)
            {
                v.dh = Math.PI - Math.Atan2(v.X - xmid, v.Y - hmid);
            }
            LP = limitdata;
            LP = LP.OrderBy(l => l.dh).ToList();//限界文件各个节点按角度排序
            ///以拟合椭圆圆心为中心，计算断面文件各节点到中心的角度（底部开始逆时针方向）
            foreach (var v in sec)
            {
                v.dh = Math.PI - Math.Atan2(v.X - xmid, v.Z - hmid);
            }
            sec = sec.OrderBy(l => l.dh).ToList();//断面点集按角度排序
            int mark = 1;//统计符合的线的次数
            ///计算断面每个点到限界轮廓的对应点及距离
            for (int i = 0; i < sec.Count; i++)
            {
                sec[i].Color = i;
                mark = 0;
                List<Faro_point> lp = new List<Faro_point>();
                for (int j = 0; j < LP.Count; j++)
                {
                    if (Math.Abs(sec[i].dh - LP[j].dh) < 0.8)
                    {
                        mark++;
                        Faro_point fp = new Faro_point() { X = LP[j].X, Y = LP[j].Y, };
                        lp.Add(fp);
                    }
                    else if (mark >= 2) break;
                }
                if (lp.Count < 2) continue;
                Get_D(lp, sec[i].X, sec[i].Z, out double x, out double y, out double d);
                sec[i].Y = x;
                sec[i].H = y;
                sec[i].R = d;
            }
            mark = 1;
            double dmark = 0;
            double D = 999;
            ///根据前面计算的断面每个点到限界轮廓的对应点及距离，按角度抽取部分代表点另存在LFP中
            for (int i = 0; i < sec.Count; i++)
            {
                if (sec[i].R == 0) continue;
                if (sec[i].dh - dmark > Math.PI / 8)
                {
                    if (D == 999)
                    {
                        mark = i;
                    }
                }
                else if (sec[i].dh - dmark > Math.PI / 72)
                {
                    if (sec[i].R < D)
                    {
                        mark = i;
                        D = sec[i].R;
                    }
                    continue;
                }
                else
                {
                    if (D == 999)
                    {
                        mark = i;
                    }
                    continue;
                }
                Faro_point faro_Point = new Faro_point()
                {
                    X = sec[mark].X,
                    Y = sec[mark].Z,
                    R = sec[mark].R,
                    Z = sec[mark].Y,
                    H = sec[mark].H,
                };
                LFP.Add(faro_Point);
                D = 999;
                dmark = sec[mark].dh;
            }
            ///校验LFP，修正有问题的对应点
            foreach (var v in LFP)
            {
                foreach (var vv in sec)
                {
                    double d = Math.Sqrt((v.Z - vv.X) * (v.Z - vv.X) + (v.H - vv.Z) * (v.H - vv.Z));
                    if (d < v.R)
                    {
                        double sita = Math.PI - Math.Atan2(vv.X - v.Z, vv.Z - v.H);
                        if (Math.Abs(sita - vv.dh) > Math.PI / 10) continue;
                        v.R = d; v.X = vv.X; v.Y = vv.Z; v.Color = vv.Color;
                    }
                }
                if (v.Color != 0)
                {
                    v.Z = sec[v.Color].Y;
                    v.H = sec[v.Color].H;
                    v.R = sec[v.Color].R;
                }
            }
            ///成限界断面图
            //GuiTouShiBie.DrawSection(limitdata, sec, LFP, path);
            SectionAnalysis.DrawSection(limitdata, sec, LFP, path, Path.GetFileNameWithoutExtension(path));

        }
        public static void Get_limit_Circle(List<Faro_point> sec, out double x0, out double y0)
        {
            ///step1 防止改变原有变量，另外声明一份数据l
            List<BaseXY> l = new List<BaseXY>();
            foreach (var v in sec)
            {
                BaseXY baseXY = new BaseXY() { X = v.X, Y = v.Z };
                l.Add(baseXY);
            }
            ///step2 设置拟合椭圆半径为默认半径
            ///step3 拟合计算得到圆心横纵坐标
            //Ellipse_Fitting.R_Tunnel = ScanPublic.GlobalResource.SuiDaoBanJing;
            Ellipse_Fitting.Robust_fitting(l, out Ellipse ep1);
            x0 = ep1.X0; y0 = ep1.Y0;
        }
        /// <summary>
        /// 获取最近点坐标和距离
        /// 方法：遍历两两点相连的线段，得到基准点到线段的最短距离和对应点
        /// </summary>
        /// <param name="lp">相连线段的点集</param>
        /// <param name="x0">基准点X坐标</param>
        /// <param name="y0">基准点Y坐标</param>
        /// <param name="x">最近点X坐标</param>
        /// <param name="y">最近点Y坐标</param>
        /// <param name="d">最近点和基准点距离</param>
        public static void Get_D(List<Faro_point> lp, double x0, double y0, out double x, out double y, out double d)
        {
            x = lp[0].X;
            y = lp[0].Y;
            d = 999;
            int F = 1;
            int E = 101;
            for (int i = 1; i < lp.Count; i++)
            {

                for (int j = F; j < E; j++)
                {
                    double x1 = (lp[i - 1].X * (100 - j) + lp[i].X * j) / 100;
                    double y1 = (lp[i - 1].Y * (100 - j) + lp[i].Y * j) / 100;
                    double d1 = Math.Sqrt(Math.Pow(x1 - x0, 2) + Math.Pow(y1 - y0, 2));
                    if (d1 < d)
                    {
                        d = d1;
                        x = x1;
                        y = y1;
                    }
                }
            }
        }


        public List<Faro_point> GLimitData0 = new List<Faro_point>();
        public static List<Faro_point> GLimitData = new List<Faro_point>();

        /// <summary>
        /// 限界计算函数
        /// </summary>
        /// <param name="SecData"></param>
        /// <param name="AllMark"></param>
        /// <param name="LimitData"></param>
        /// <returns></returns>
        public static bool LimitCalculation( List<Faro_point> SecData,  out List<Faro_point> AllMark, out List<Faro_point> LimitData)
        {
            AllMark = new List<Faro_point>();
            LimitData = new List<Faro_point>();
            try {

                ///step1 筛选得到轨道部分，用于拟合计算得到轨面所在直线和轨面中线
                ///step2 通过轨面所在直线和轨面中线将车辆限界轮廓旋转+平移到断面上的轨道
                List<Faro_point> lfp1 = SecData.FindAll(l => l.Z < 0 && (l.X < -0.5 && l.X > -0.9)).ToList();
                List<Faro_point> lfp2 = SecData.FindAll(l => l.Z < 0 && (l.X > 0.5 && l.X < 0.9)).ToList();
                if (lfp1.Count < 100 || lfp2.Count < 100) return false;
                lfp1 = lfp1.OrderByDescending(l => l.Z).ToList();
                lfp2 = lfp2.OrderByDescending(l => l.Z).ToList();
                List<Faro_point> lfp3 = new List<Faro_point>();
                List<Faro_point> lfp4 = new List<Faro_point>();
                double max1 = lfp1[0].Z; double max2 = lfp2[0].Z;
                for (int i = 0; i < lfp1.Count; i++)
                {
                    if (lfp1[i].Z > max1 - 0.006) lfp3.Add(lfp1[i]);
                    else if (lfp3.Count < 50) { lfp3.Add(lfp1[i]); }
                }
                for (int i = 0; i < lfp2.Count; i++)
                {
                    if (lfp2[i].Z > max2 - 0.006) lfp4.Add(lfp2[i]);
                    else if (lfp4.Count < 50) { lfp4.Add(lfp2[i]); }
                }
                List<Faro_point> lfp5 = lfp3.Concat(lfp4).ToList();
                double mean1 = lfp3.Average(l => l.X);
                double mean2 = lfp4.Average(l => l.X);
                double[,] X = new double[lfp5.Count, 1];
                double[] Y = new double[lfp5.Count];
                for (int i = 0; i < X.Length; i++)
                {
                    X[i, 0] = lfp5[i].X; Y[i] = lfp5[i].Z;
                }
                ///直线拟合
                MathMethor.Polyfit(X, Y, 1, out double[,] P);
                if (P.Length < 2 || Math.Abs(P[0, 0]) > 0.1)
                {
                    return false;
                }

                lfp3 = lfp1.FindAll(l => l.X > mean1 && l.Z > max1 - 0.05).ToList();
                lfp4 = lfp2.FindAll(l => l.X < mean2 && l.Z > max2 - 0.05).ToList();
                mean1 = lfp3.Max(l => l.X); mean2 = lfp4.Min(l => l.X);
                ///获取旋转中心坐标
                double centerOrignal_x = (mean1 + mean2) / 2;
                double centerOrignal_y = centerOrignal_x * P[0, 0] + P[1, 0];



                if (GLimitData.Count < 3) return false;

                ///限界文件统一修改为逆时针方向
                double cx = (GLimitData.Min(l => l.X) + GLimitData.Max(l => l.X)) / 2;
                double cy = (GLimitData.Min(l => l.Y) + GLimitData.Max(l => l.Y)) / 2;
                double sita0 = Math.Atan2(GLimitData[0].Y - cy, GLimitData[0].X - cx);
                double sita1 = Math.Atan2(GLimitData[1].Y - cy, GLimitData[1].X - cx);

                if ((sita0 - sita1 < Math.PI && sita0 > sita1) || sita0 - sita1 < -Math.PI)
                {
                    for (int i = GLimitData.Count - 1; i >= 0; i--)
                    {
                        LimitData.Add(new Faro_point() { X = GLimitData[i].X, Y = GLimitData[i].Y, Color = GLimitData.Count - 1 - i });
                    }
                }
                else
                {
                    for (int i = 0; i < GLimitData.Count; i++)
                    {
                        LimitData.Add(new Faro_point() { X = GLimitData[i].X, Y = GLimitData[i].Y, Color = i });
                    }
                }
                if (LimitData[0].X != LimitData[LimitData.Count - 1].X || LimitData[0].Y != LimitData[LimitData.Count - 1].Y)
                {
                    LimitData.Add(new Faro_point() { X = LimitData[0].X, Y = LimitData[0].Y, Color = LimitData.Count + 1 });
                }

                ///通过轨面所在直线和轨面中心将车辆限界轮廓旋转+平移到断面上的轨道
                ExchangeXianjie2(LimitData, centerOrignal_y, centerOrignal_x, P[0, 0]);


                ///step3 初步筛选
                double minx = LimitData.Min(l => l.X);
                double maxx = LimitData.Max(l => l.X);
                double miny = LimitData.Min(l => l.Y);
                double maxy = LimitData.Max(l => l.Y);
                //筛选出需要参与计算的点
                List<Faro_point> slp_in = new List<Faro_point>();//限界轮廓内部的点
                List<Faro_point> slp_out = new List<Faro_point>();//限界轮廓外部的点
                List<Faro_point> NeedMark_in = new List<Faro_point>();//限界内部需要标出的侵界信息
                List<Faro_point> NeedMark_out = new List<Faro_point>();//限界外部需要标出的限界信息
                List<Faro_point> UNeedMark_out = new List<Faro_point>();//限界外部不需要标出的限界信息
                foreach (var v in SecData)
                {
                    if (v.X < 0.5 && v.X > -0.5 && v.Z < 0 && v.Z > -0.5) continue;//过滤MS100小车车体上的点
                    if (v.X < minx - 1 || v.X > maxx + 1 || v.Z < miny || v.Z > maxy + 1) continue;//过滤较远点
                    if (v.X < maxx && v.X > minx && v.Z < miny + 0.5) continue;//过滤轨道上的点（0.5m阈值可调）
                    ///先行判断筛选掉外围点，减少判断点在内部还是外部的计算量
                    if (v.X < minx || v.X > maxx || v.Z > maxy)
                    {
                        slp_out.Add(v);
                    }
                    else if (IsPointInside(LimitData, v))
                    {
                        slp_in.Add(v);
                    }
                    else slp_out.Add(v);
                }

                ///计算限界轮廓每个线段的线段方程
                double[][] LimitLine = new double[LimitData.Count - 1][];
                for (int i = 0; i < LimitData.Count - 1; i++)
                {
                    double[] abc = GetABC(LimitData[i].X, LimitData[i].Y, LimitData[i + 1].X, LimitData[i + 1].Y);
                    LimitLine[i] = new double[7] { abc[0], abc[1], abc[2], LimitData[i].X, LimitData[i].Y, LimitData[i + 1].X, LimitData[i + 1].Y };
                }
                ///先算内部点的侵界信息
                if (slp_in.Count > 0)
                {
                    foreach (var v in slp_in)
                    {
                        Faro_point faro_Point = GetMinDPoint(LimitLine, v.X, v.Z);
                        v.Color = faro_Point.Color;
                        v.R = faro_Point.R;
                        v.xe = faro_Point.xe;
                        v.H = faro_Point.H;
                        v.Y = faro_Point.Y;
                        v.tag = faro_Point.tag;
                    }

                    ///简单筛选
                    slp_in = slp_in.OrderByDescending(l => l.R).ToList();

                    Faro_point faro_Point0 = slp_in[0];
                    NeedMark_in.Add(faro_Point0);
                    for (int i = 1; i < slp_in.Count; i++)
                    {
                        bool isContinue = false;
                        foreach (var v in NeedMark_in)
                        {
                            ///两点距离过近
                            if (Math.Abs(v.X - slp_in[i].X) + Math.Abs(v.Y - slp_in[i].Y) < 0.1)
                            {
                                isContinue = true;
                                break;
                            }
                            ///两个连接线段相交
                            if ((v.xe == slp_in[i].xe) && (v.H == slp_in[i].H))
                            {
                                isContinue = true;
                                break;
                            }
                            ///两个线段点接近且该点侵界更小
                            if (Math.Abs(v.X - slp_in[i].X) + Math.Abs(v.Y - slp_in[i].Y) < 0.3 && v.R > 2 * slp_in[i].R)
                            {
                                isContinue = true;
                                break;
                            }
                        }
                        if (isContinue) continue;
                        NeedMark_in.Add(slp_in[i]);
                    }

                    UNeedMark_out = NeedMark_in.ToList();
                    ///聚类筛选(待完善)
                    //List<PartMeans> LpartMeans = new List<PartMeans>();
                    //LpartMeans.Add(new PartMeans() {lf=new List<Faro_point>(),cx=slp_in [0].X,cy= slp_in[0].Y,});
                    //LpartMeans[0].lf.Add(slp_in[0]);
                    //LpartMeans[0].xmin = LpartMeans[0].cx;
                    //LpartMeans[0].xmax = LpartMeans[0].cx;
                    //LpartMeans[0].ymin = LpartMeans[0].cy;
                    //LpartMeans[0].ymax = LpartMeans[0].cy;
                    //for (int i = 1; i < slp_in.Count; i++) {
                    //    for (int j = 0; j < LpartMeans.Count; j++) {

                    //    }
                    //}
                }
                //再算外部点的侵界信息
                foreach (var v in slp_out)
                {
                    Faro_point faro_Point = GetMinDPoint(LimitLine, v.X, v.Z);
                    v.Color = faro_Point.Color;
                    v.R = faro_Point.R;
                    v.xe = faro_Point.xe;
                    v.H = faro_Point.H;
                    v.Y = faro_Point.Y;
                    v.tag = faro_Point.tag;
                }
                slp_out = slp_out.OrderBy(l => l.R).ToList();
                for (int i = 0; i < slp_out.Count; i++)
                {
                    bool isContinue = false;
                    foreach (var v in UNeedMark_out)
                    {
                        ///两点距离过近
                        if (Math.Abs(v.xe - slp_out[i].X) + Math.Abs(v.H - slp_out[i].Y) < 0.1)
                        {
                            isContinue = true;
                            break;
                        }
                        ///两个线段点接近且该点侵界更大
                        if (Math.Abs(v.xe - slp_out[i].X) + Math.Abs(v.H - slp_out[i].Y) < 0.3 && 2 * v.R + 0.01 < slp_out[i].R)
                        {
                            isContinue = true;
                            UNeedMark_out.Add(slp_out[i]);
                            break;
                        }
                    }
                    if (isContinue) continue;
                    foreach (var v in NeedMark_out)
                    {
                        /////两点距离过近
                        //if (Math.Abs(v.X - slp_out[i].X) + Math.Abs(v.Y - slp_out[i].Y) < 0.05)
                        //{
                        //    isContinue = true;
                        //    break;
                        //}
                        ///两个线段相交
                        if ((v.xe == slp_out[i].xe) && (v.H == slp_out[i].H))
                        {
                            isContinue = true;
                            break;
                        }
                        ///两个线段点接近且该点更接近
                        if (Math.Abs(v.xe - slp_out[i].xe) + Math.Abs(v.H - slp_out[i].H) < 0.5)
                        {
                            isContinue = true;
                            break;
                        }
                    }
                    if (isContinue) continue;
                    NeedMark_out.Add(slp_out[i]);
                }

                for (int i = 0; i < NeedMark_in.Count; i++)
                {
                    NeedMark_in[i].tag = 0;
                    AllMark.Add(NeedMark_in[i]);
                }
                foreach (var v in NeedMark_out)
                {
                    v.tag = 1;
                    AllMark.Add(v);
                }
                //SectionAnalysis.DrawSection(LimitData,SecData,AllMark, savepath,Path.GetFileNameWithoutExtension(savepath));
                return true;
            }
            catch (Exception e)
            {
                return false;
            }
         
        }
        /// <summary>
        /// 判断点是在多边形内部还是外部
        /// </summary>
        /// <param name="lfp">多边形点集</param>
        /// <param name="p">需要判断点</param>
        /// <returns></returns>
        public static bool IsPointInside(List<Faro_point> lfp, Faro_point p)
        {
            ///判断点在多边形内部还是外部
            ///（1）当前点向右延伸，和多边形相交的次数（端点算两次或0次，这里为方便判断算0次）
            ///（2）如果相交次数是偶数，则点在多边形外部，如相交次数是奇数则点在多边形内部
            int flag = 0;
            for (int i = 0; i < lfp.Count - 1; i++)
            {
                if (lfp[i].X < p.X && lfp[i + 1].X < p.X) continue;
                else if (lfp[i].Y > p.Z && lfp[i + 1].Y > p.Z) continue;
                else if (lfp[i].Y < p.Z && lfp[i + 1].Y < p.Z) continue;
                else if (lfp[i].Y == p.Z || lfp[i].Y == p.Z) continue;

                double x0 = lfp[i].X + (lfp[i + 1].X - lfp[i].X) * (p.Z - lfp[i].Y) / (lfp[i + 1].Y - lfp[i].Y);
                if (x0 >= p.X) flag++;
            }
            if (flag % 2 == 1) return true;
            return false;
        }
        /// <summary>
        /// 求取两点直线方程
        /// </summary>
        /// <param name="x1">点1x</param>
        /// <param name="y1">点1y</param>
        /// <param name="x2">点2x</param>
        /// <param name="y2">点2y</param>
        /// <returns></returns>
        public static double[] GetABC(double x1, double y1, double x2, double y2)
        {
            double[] ABC = new double[3];

            if (x1 == x2)
            {
                if (y1 == y2) return ABC;
                ABC[0] = 1;
                ABC[1] = 0;
                ABC[2] = -x1;
            }
            else if (y1 == y2)
            {
                ABC[0] = 0;
                ABC[1] = 1;
                ABC[2] = -y1;
            }
            else
            {
                ABC[0] = y2 - y1;
                ABC[1] = x1 - x2;
                ABC[2] = -ABC[0] * x1 - ABC[1] * y1;
            }
            return ABC;
        }

        /// <summary>
        /// 获取点到限界上每个线段中的最小和特殊距离及对应点集
        /// xe 对应线段上点X
        /// H 对应线段上点Y
        /// X 计算点X
        /// Y 计算点Y
        /// R 距离
        /// Color 对应线段序号（从0计）
        /// tag 0是线段上点，1是端点
        /// </summary>
        /// <param name="LimitLine">限界上每个线段</param>
        /// <param name="m">计算点X</param>
        /// <param name="n">计算点Y</param>
        public static List<Faro_point> GetMinDSPoint(double[][] LimitLine, double m, double n)
        {
            ///step1 逐个线段计算，筛选得到内凹端点和最近点
            ///step2 判断内凹端点和最近点是否重复，如重复则不添加最近点，最后以距离近到远排序输出
            List<Faro_point> lfp = new List<Faro_point>();
            double[] D0 = GetMinDxy(LimitLine[LimitLine.Length - 1], m, n);
            double d = D0[0]; double d0 = D0[0];
            int flag = LimitLine.Length - 1;
            for (int i = 0; i < LimitLine.Length; i++)
            {
                double[] D = GetMinDxy(LimitLine[i], m, n);
                if (d0 == D[0] && d0 == Math.Sqrt((LimitLine[i][3] - m) * (LimitLine[i][3] - m) + (LimitLine[i][4] - n) * (LimitLine[i][4] - n)))
                {
                    lfp.Add(new Faro_point()
                    {
                        X = m,
                        Y = n,
                        R = d,
                        Color = i,
                        tag = 1,
                        xe = LimitLine[i][3],
                        H = LimitLine[i][4],
                    });
                }
                if (d >= D[0])
                {
                    d = D[0];
                    flag = i;
                    D0 = D;
                }
                d0 = D[0];
            }

            if (lfp.Count > 0)
            {
                lfp = lfp.OrderBy(l => l.R).ToList();
                if (D0[0] < lfp[0].R)
                {
                    lfp.Insert(0, new Faro_point()
                    {
                        xe = D0[1],
                        H = D0[2],
                        R = D0[0],
                        Color = flag,
                        tag = 0,
                        X = m,
                        Y = n
                    });
                }
            }
            else
            {
                lfp.Add(new Faro_point()
                {
                    xe = D0[1],
                    H = D0[2],
                    R = D0[0],
                    Color = flag,
                    tag = 0,
                    X = m,
                    Y = n
                });
            }

            return lfp;
        }
        public static Faro_point GetMinDPoint(double[][] LimitLine, double m, double n)
        {
            Faro_point fPoint = new Faro_point() { X = m, Y = n };
            double d = 99999;
            for (int i = 0; i < LimitLine.Length; i++)
            {
                double[] D = GetMinDxy(LimitLine[i], m, n);
                if (d >= D[0])
                {
                    d = D[0];
                    fPoint.Color = i;
                    fPoint.R = D[0];
                    fPoint.xe = D[1];
                    fPoint.H = D[2];
                }
            }
            return fPoint;
        }
        /// <summary>
        /// 计算点到线段的最小距离及对应坐标
        /// </summary>
        /// <param name="A">直线系数A</param>
        /// <param name="B">直线系数B</param>
        /// <param name="C">直线系数C</param>
        /// <param name="x1">线段1端点X</param>
        /// <param name="y1">线段1端点Y</param>
        /// <param name="x2">线段2端点X</param>
        /// <param name="y2">线段2端点Y</param>
        /// <param name="m">计算点X</param>
        /// <param name="n">计算点Y</param>
        /// <returns></returns>
        public static double[] GetMinDxy(double A, double B, double C, double x1, double y1, double x2, double y2, double m, double n)
        {
            double[] Dxy;
            double[] DropFoor = GetDropFoot(A, B, C, m, n);
            ///优先判断垂足在线段上还是线段之外
            ///再进行相应的距离计算
            double d12 = Math.Sqrt((x1 - x2) * (x1 - x2) + (y1 - y2) * (y1 - y2));
            double df1 = Math.Sqrt((x1 - DropFoor[0]) * (x1 - DropFoor[0]) + (y1 - DropFoor[1]) * (y1 - DropFoor[1]));
            double df2 = Math.Sqrt((DropFoor[0] - x2) * (DropFoor[0] - x2) + (DropFoor[1] - y2) * (DropFoor[1] - y2));
            if (d12 < df1 || d12 < df2)
            {
                if (df1 < df2)
                {
                    double d1 = Math.Sqrt((x1 - m) * (x1 - m) + (y1 - n) * (y1 - n));
                    Dxy = new double[3] { d1, x1, y1 };
                }
                else
                {
                    double d2 = Math.Sqrt((x2 - m) * (x2 - m) + (y2 - n) * (y2 - n));
                    Dxy = new double[3] { d2, x2, y2 };
                }
            }
            else
            {
                double d0 = Math.Sqrt((DropFoor[0] - m) * (DropFoor[0] - m) + (DropFoor[1] - n) * (DropFoor[1] - n));
                Dxy = new double[3] { d0, DropFoor[0], DropFoor[1] };
            }
            return Dxy;
        }
        public static double[] GetMinDxy(double[] ABCXY12, double m, double n)
        {
            if (ABCXY12.Length < 7) return new double[3] { 99999, 0, 0 };
            return GetMinDxy(ABCXY12[0], ABCXY12[1], ABCXY12[2], ABCXY12[3], ABCXY12[4], ABCXY12[5], ABCXY12[6], m, n);
        }
        /// <summary>
        /// 得到点到直线垂足坐标
        /// </summary>
        /// <param name="A">直线系数A</param>
        /// <param name="B">直线系数B</param>
        /// <param name="C">直线系数C</param>
        /// <param name="m">计算点X</param>
        /// <param name="n">计算点Y</param>
        /// <returns></returns>
        public static double[] GetDropFoot(double A, double B, double C, double m, double n)
        {
            double[] xy0 = new double[2];
            xy0[0] = (B * B * m - A * B * n - A * C) / (A * A + B * B);
            xy0[1] = (A * A * n - A * B * m - B * C) / (A * A + B * B);
            return xy0;
        }

        /// <summary>
        /// 限界文件调整
        /// </summary>
        /// <param name="pList">限界文件</param>
        /// <param name="centerOrignal_x">旋转中心X</param>
        /// <param name="centerOrignal_y">旋转中心Y</param>
        /// <param name="k">斜率</param>
        public static void ExchangeXianjie2(List<Faro_point> pList, double centerOrignal_x, double centerOrignal_y, double k)
        {
            double temp = Math.Sqrt(k * k + 1);
            double pHNorm1 = k / temp;//横轴的基向量
            double pHNorm2 = 1 / temp;
            double pVNorm1 = 1 / temp;
            double pVNorm2 = -k / temp;
            foreach (var v in pList)
            {
                v.R = centerOrignal_x + v.X * pHNorm1 + v.Y * pVNorm1;
                v.H = centerOrignal_y + v.X * pHNorm2 + v.Y * pVNorm2;
            }
            foreach (var v in pList)
            {
                v.Y = v.R; v.X = v.H;
            }
        }
        /// <summary>
        /// 读取断面文件
        /// </summary>
        /// <param name="path">断面文件路径</param>
        /// <param name="lp">断面点集</param>
        /// <returns></returns>
        public static bool ReadTxt1(string path, out List<Faro_point> lp)
        {
            lp = new List<Faro_point>();
            try
            {
                string[] lines;
                using (StreamReader sr = new StreamReader(path, Encoding.Default))
                {
                    lines = sr.ReadToEnd().Split(new char[] { ';', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                }
                int N = 0;
                foreach (var line in lines)
                {
                    string[] items = line.Split(new char[] { ',', '\t', ' ' });
                    {
                        Faro_point b = new Faro_point();
                        b.X = Convert.ToDouble(items[0]);
                        b.Z = Convert.ToDouble(items[1]);
                        lp.Add(b);
                    }
                }
                return true;
            }
            catch
            {
                return false;
            }
        }
        /// <summary>
        /// 读取限界文件
        /// </summary>
        /// <param name="path">限界文件路径</param>
        /// <param name="lp">限界信息</param>
        /// <returns></returns>
        public bool ReadLimitFile(string path, out List<Faro_point> lp)
        {
            lp = new List<Faro_point>();
            try
            {
                string[] lines;
                ///一次性读取文件到字符串数组lines
                using (StreamReader sr = new StreamReader(path, Encoding.Default))
                {
                    lines = sr.ReadToEnd().Split(new char[] { ';', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                }
                ///逐行解析
                foreach (var line in lines)
                {
                    string[] items = line.Split(new char[] { ',', '\t', ' ' });
                    {
                        Faro_point b = new Faro_point();
                        b.X = Convert.ToDouble(items[1]);
                        b.Y = Convert.ToDouble(items[2]);
                        b.Color = Convert.ToInt16(items[0]);
                        lp.Add(b);
                    }
                }
                return true;
            }
            catch
            {
                return false;
            }
        }
#endregion

#region 处理部分-分环
        /// <summary>
        /// 破损用深度图
        /// </summary>
        /// <param name="savePath">保存路径</param>
        /// <param name="Lb">展开点云数据</param>
        /// <param name="expand">分辨率（像素/米）</param>
        /// <param name="height">影像高度</param>
        /// <param name="llfp">环缝信息</param>
        public void ET_ShenDuTu0125(string savePath, List<BaseXY> Lb, int expand, int height, List<List<Faro_point>> llfp)
        {
            try
            {
                if (Lb.Count == 0) return;
                if (!Directory.Exists(Path.GetDirectoryName(savePath))) Directory.CreateDirectory(Path.GetDirectoryName(savePath));
                Lb = Lb.FindAll(l => l.Y > m1).ToList();
                double deta = 0.09;

                height = (int)(1.0 * height * 400 / expand);
                expand = 400;
                int width = (int)(Math.Floor(expand * (m2 - m1))) + 1;
                byte[,] lp_arr = new byte[height, width];
                double rmin = Lb.Min(l => l.angle);
                foreach (var v in Lb)
                {
                    int h1 = (int)Math.Floor(expand * v.X);
                    int w1 = (int)(Math.Floor(expand * (v.Y - m1)));
                    if (w1 < 0) {/* min = Math.Min(min, w1);*/ w1 = 0; }
                    else if (w1 >= width) { /*max = Math.Max(max, w1);*/ w1 = width - 1; }
                    if (h1 >= height) h1 = height - 1;
                    else if (h1 < 0) h1 = 0;
                    lp_arr[h1, w1] = GetGrayShenDuTu(v.angle, rmin, deta);
                }
                GetPhoto getPhoto = new GetPhoto();
                getPhoto.BornPic_shendu(lp_arr, height, width, savePath);
                if (llfp.Count > 0)
                {
                    List<List<Faro_point>> llf = new List<List<Faro_point>>();
                    foreach (var v in llfp)
                    {
                        if (v != null && v.Count > 0)
                        {
                            if (v[0].tag == 0) llf.Add(v);
                        }
                    }
                    if (llf.Count > 0)
                    {
                        string path = savePath.Replace(".tiff", ".txt");
                        HuanFengBuild(llf, path);
                    }
                }
            }
            catch { }
        }
        /// <summary>
        /// 输出环缝信息
        /// </summary>
        /// <param name="llf">环缝信息</param>
        /// <param name="savepath">保存路径</param>
        public void HuanFengBuild(List<List<Faro_point>> llf, string savepath)
        {
            int N = 0;
            List<string> ls = new List<string>();
            for (int i = 0; i < llf.Count; i++)
            {
                N++;
                for (int j = 0; j < llf[i].Count; j++)
                {
                    string s = N + " " + llf[i][j].X + " " + llf[i][j].Y;
                    ls.Add(s);
                }
            }
            Text_IO.WriteLineToTxt(savepath, ls, true);
        }
        /// <summary>
        /// 计算灰度值
        /// </summary>
        /// <param name="value">输入值</param>
        /// <param name="rmin">最小值</param>
        /// <param name="deta">线性取值范围</param>
        /// <returns>输出灰度值</returns>
        public byte GetGrayShenDuTu(double value, double rmin, double deta)
        {
            if (value < rmin) return 0;
            else if (value > rmin + deta) return 255;
            else
            {
                return (byte)((value - rmin) / deta * 255);
            }
        }
        /// <summary>
        /// 分环处理
        /// </summary>
        /// <param name="photopath">保存图片路径</param>
        /// <param name="expand0">影像分辨率</param>
        /// <param name="height">影像高度</param>
        /// <param name="isFirst">是否是第一个文件</param>
        /// <param name="ldivide">识别到的环缝信息</param>
        /// <param name="HuanWidth">环宽</param>
        /// <returns></returns>
        public bool ET_Divide0125(string photopath, int expand0, int height, bool isFirst, out List<DivideLine> ldivide, double HuanWidth = 1.5)
        {
            ldivide = new List<DivideLine>();
            try
            {
                List<BaseXY> Lb = new List<BaseXY>();
                List<BaseXY> Lb2 = new List<BaseXY>();

                ///得到当前文件数据的平面坐标
                double startH = 0;
                for (int i = 0; i < numCol / 2; i++)
                {
                    foreach (var v in ap[i])
                    {
                        if (v.H > startH && v.tag != 1)
                        {
                            Lb.Add(new BaseXY()
                            {
                                X = v.xe,
                                Y = v.Y,
                                Color = v.Color,
                                angle = v.H,
                                Error = v.Hs,
                            });
                        }
                    }
                }
                ///非首个文件则加载上个文件的部分平面坐标
                if (!isFirst)
                {
                    foreach (var vv in ap_pre)
                    {
                        foreach (var v in vv)
                        {
                            if (v.H > startH && v.tag != 1)
                            {
                                Lb.Add(new BaseXY() { X = v.xe, Y = v.Y, Color = v.Color, angle = v.H, Error = v.Hs, });
                            }
                        }
                    }
                }
                ///无点集跳出
                if (Lb.Count == 0) return true;

                int expand = 100;//环缝识别的深度图分辨率固定为100，即10mm分辨率的图片
                int hmin = (int)(Lb.Min(l => l.X) * expand);//纵坐标起算值
                int wmin = (int)(Lb.Min(l => l.Y) * expand);//横坐标起算值
                ///生成灰度影像图，用于环缝管片缝预识别
                getWH(Lb, hmin, wmin, expand, out int[] hIntList, out int[] wIntList);
                GetPhoto.getP(photopath, Lb, hIntList, wIntList, 0.09, startH);
                //识别缝
                Process_Solve.LS_main(photopath, expand, HuanWidth, out List<DivideLine> getldivide);
                Thread.Sleep(5);
                ldivide = getldivide.ToList();
                Thread.Sleep(5);
                if (ldivide.Count == 0)
                {
                    Process_Solve.LS_main(photopath, expand, HuanWidth, out getldivide);
                    Thread.Sleep(5);
                    ldivide = getldivide.ToList();
                    Thread.Sleep(5);
                }

                foreach (var V in ldivide)
                {
                    foreach (var vv in V.point_data)
                    {
                        vv.x = (1.0 * vv.w + wmin) / expand;
                        vv.y = (1.0 * vv.h + hmin) / expand;
                    }
                }

                List<List<Faro_point>> llf3ci = new List<List<Faro_point>>();
                if (ldivide.Count >= 1)
                {
                    List<BaseXY> lb = new List<BaseXY>();
                    List<List<Faro_point>> llf_luosuankong = new List<List<Faro_point>>();
                    List<double> d1 = new List<double>();
                    List<double[]> d2 = new List<double[]>();
                    List<double> d1_1 = new List<double>();
                    for (int i = ldivide.Count - 1; i >= 0; i--)
                    {
                        if (ldivide[i].point_data.Count < 2) ldivide.Remove(ldivide[i]);
                    }
                    if (ldivide.Count <= 0)
                    {
                        List<point_data> lpoint_Data = new List<point_data>();
                        lpoint_Data.Add(new point_data() { x = m1 });
                        ldivide.Add(new DivideLine() { attribute = "EndCol", ID = 0, point_data = lpoint_Data });
                    }
                    for (int i = 0; i < ldivide.Count; i++)
                    {
                        if (ldivide[i].attribute == "Ring") d1.Add(ldivide[i].point_data.Average(l => l.x));
                        else if (ldivide[i].attribute == "platform") d2.Add(new double[3] { i, ldivide[i].point_data[0].x, ldivide[i].point_data[1].x });
                    }
                    if (d1.Count > 2)
                    {
                        d1 = d1.OrderBy(l => l).ToList();
                        int d1_N = d1.Count;
                        double[] dd1 = new double[d1_N - 1];
                        for (int i = 0; i < d1_N - 1; i++)
                        {
                            dd1[i] = d1[i + 1] - d1[i];
                        }
                        double maxD = dd1.Max();
                        double minD = dd1.Min();
                        if (maxD - 2 * minD < 1 && maxD - 2 * minD > -0.9)
                        {
                            List<double> d12 = new List<double>();
                            for (int i = 0; i < d1_N - 2; i++)
                            {
                                if (dd1[i] > (maxD + minD) / 2 + 0.1)
                                {
                                    d12.Add(d1[i]);
                                    d12.Add(d1[i + 1]);
                                    i += 1;
                                }
                                else if (dd1[i + 1] + dd1[i] < (maxD + minD) / 2 + 0.1)
                                {
                                    d12.Add(d1[i]);
                                    d12.Add(d1[i + 2]);
                                    i += 2;
                                }
                            }
                            for (int i = d1_N - 1; i >= 0; i--)
                            {
                                bool isfalse = false;
                                foreach (var v in d12)
                                {
                                    if (d1[i] == v)
                                    {
                                        isfalse = true;
                                        break;
                                    }
                                }
                                if (isfalse) d1.Remove(d1[i]);
                            }
                        }
                        if (d2.Count > 0)
                        {
                            for (int i = d2.Count - 1; i >= 0; i--)
                            {
                                bool isfalse = false;
                                foreach (var v in d1)
                                {
                                    if (d2[i][1] < v && d2[i][2] > v)
                                    {
                                        isfalse = true;
                                        break;
                                    }
                                }
                                if (isfalse) d2.Remove(d2[i]);
                            }
                        }
                    }


                    for (int i = ldivide.Count - 1; i >= 0; i--)
                    {
                        bool isfalse = false;
                        if (ldivide[i].attribute == "Ring")
                        {
                            foreach (var v in d1)
                            {
                                if (v == ldivide[i].point_data.Average(l => l.x)) { isfalse = true; break; }
                            }
                            // if (!isfalse) ldivide.Remove(ldivide[i]);
                            double xsum = 0; double ysum = 0;
                            double wsum = 0; double hsum = 0;
                            if (ldivide[i].point_data.Count == 0) continue;
                            foreach (var v in ldivide[i].point_data)
                            {
                                xsum += v.x; ysum += v.y;
                                wsum += v.w; hsum += v.h;
                            }
                            lb.Add(new BaseXY()
                            {
                                Color = ldivide[i].ID,
                                X = xsum / ldivide[i].point_data.Count,
                                Y = ysum / ldivide[i].point_data.Count,
                                angle = wsum / ldivide[i].point_data.Count,
                                Error = hsum / ldivide[i].point_data.Count,
                            });
                        }
                        else if (ldivide[i].attribute == "luosuankong")
                        {
                            List<Faro_point> lsk = new List<Faro_point>();
                            foreach (var v in ldivide[i].point_data)
                            {
                                Faro_point faro_Point = new Faro_point();
                                faro_Point.X = (1.0 * v.w + wmin) / expand;
                                faro_Point.Y = (1.0 * v.h + hmin) / expand;
                                lsk.Add(faro_Point);
                            }
                            llf_luosuankong.Add(lsk);
                        }
                        else
                        {
                            foreach (var v in d2)
                            {
                                if (v[1] == ldivide[i].point_data[0].x && v[2] == ldivide[i].point_data[1].x) { isfalse = true; break; }
                            }
                        }
                    }
                    lb = lb.OrderByDescending(l => l.Color).ToList();
                    List<Faro_point> Rank_test_ALL = new List<Faro_point>();

                    List<List<Faro_point>> llf = new List<List<Faro_point>>();
                    List<List<Faro_point>> llf2 = new List<List<Faro_point>>();
                    List<List<Faro_point>> llf3 = new List<List<Faro_point>>();
                    List<int[][]> Li = new List<int[][]>();
                    List<List<Faro_point>> llfp = new List<List<Faro_point>>();
                    try
                    {

                        if (ldivide.Count <= 0)
                        {
                            List<point_data> lpoint_Data = new List<point_data>();
                            lpoint_Data.Add(new point_data() { x = m1 });
                            ldivide.Add(new DivideLine() { attribute = "EndCol", ID = 0, point_data = lpoint_Data });
                        }

                        for (int i = 0; i < ldivide.Count; i++)
                        {
                            if (ldivide[i].attribute == "luosuankong") continue;
                            DivideLine div = ldivide[i];
                            Seam_Test1(div, hmin, wmin, expand, out List<Faro_point> Rank_test, out List<Faro_point> lfp, out List<List<Faro_point>> lfpoint/*, out List<Faro_point> lfp_3*/);
                            llfp = llfp.Concat(lfpoint).ToList();
                            Rank_test_ALL = Rank_test_ALL.Concat(Rank_test).ToList();
                            llf.Add(lfp);
                            llf3.Add(Rank_test);
                            llf3ci.Add(lfp);
                            if (div.attribute == "Ring") llf2.Add(lfp);
                        }
                        if (Rank_test_ALL.Count > 0)
                        {
                            ThreadPool.QueueUserWorkItem((state) =>
                            {
                                GetPhoto.GetP_cuotai_4(photopath.Replace(".tiff", "mark.tiff"), Lb, Rank_test_ALL, llf, expand0, llf_luosuankong);
                                Text_IO.WriteSlabMessage2(photopath.Replace("_huan.tiff", ".bin"), Rank_test_ALL, llf, true);
                            });
                        }
                    }
                    catch (Exception e) { }
                    if (lb.Count > 1)
                    {
                        point_data point_Data = new point_data()
                        {
                            x = (lb[0].X + lb[1].X) / 2,
                            y = (lb[0].Y + lb[1].Y) / 2,
                            w = (int)(lb[0].angle + lb[1].angle) / 2,
                            h = (int)(lb[0].Error + lb[1].Error) / 2
                        };
                        GetRank(point_Data.w, point_Data.h, hmin, wmin, expand, out int[] intR);
                        List<point_data> lpoint_Data = new List<point_data>();
                        lpoint_Data.Add(point_Data);
                        ldivide.Add(new DivideLine() { attribute = "EndCol", ID = intR[0], point_data = lpoint_Data });
                    }
                }
                else
                {
                    List<point_data> lpoint_Data = new List<point_data>();
                    lpoint_Data.Add(new point_data() { x = m1 });
                    ldivide.Add(new DivideLine() { attribute = "EndCol", ID = 0, point_data = lpoint_Data });
                }//新增EndCol于分环信息中，保存需要切除的最后两环的中心列
                string saveShenduPath = Path.GetDirectoryName(photopath) + "//ShenDu//" + Path.GetFileName(photopath);
                ThreadPool.QueueUserWorkItem((state) =>
                {
                    ET_ShenDuTu0125(saveShenduPath, Lb, expand0, height, llf3ci);
                });
                return true;
            }
            catch (Exception e)
            {
                string log = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + " 分环过程发生错误于" + e.Message + e.StackTrace;
                Text_IO.WriteLineToTxt(WriteLogPath, log);
                return false;
            }
        }//自动分环-正常
        /// <summary>
        /// 真实坐标==>像素坐标转换
        /// </summary>
        /// <param name="Lb">平面点击</param>
        /// <param name="hmin">纵坐标起算值</param>
        /// <param name="wmin">横坐标起算值</param>
        /// <param name="expand">影像分辨率（像素/米）</param>
        /// <param name="hIntList">纵坐标集</param>
        /// <param name="wIntList">横坐标集</param>
        static void getWH(List<BaseXY> Lb, int hmin, int wmin, int expand, out int[] hIntList, out int[] wIntList)
        {
            int len = Lb.Count;
            hIntList = new int[len];
            wIntList = new int[len];
            for (int i = 0; i < len; i++)
            {
                hIntList[i] = (int)((Lb[i].X * expand)) - hmin;
                wIntList[i] = (int)((Lb[i].Y * expand)) - wmin;
            }
        }
        /// <summary>
        /// 错台计算
        /// </summary>
        /// <param name="divide">环缝信息</param>
        /// <param name="hmin">纵坐标起算值</param>
        /// <param name="wmin">横坐标起算值</param>
        /// <param name="expand">影像分辨率（像素/米）</param>
        /// <param name="GetRank">错台信息</param>
        /// <param name="lfp">环缝点信息</param>
        /// <param name="lfpoint">错台信息</param>
        public void Seam_Test1(DivideLine divide, int hmin, int wmin, int expand, out List<Faro_point> GetRank, out List<Faro_point> lfp, out List<List<Faro_point>> lfpoint)
        {
            GetRank = new List<Faro_point>();
            lfpoint = new List<List<Faro_point>>();
            lfp = new List<Faro_point>();
            double detaH = 0.15;//单次迭代纵坐标值
            double ddh = 0.004;//
            ///判断是环间缝还是环内缝
            if (divide.attribute == "Ring")
            {
                List<Faro_point> lc = new List<Faro_point>();

                divide.point_data = divide.point_data.OrderBy(l => l.h).ToList();
                double Hmin = 0;
                double dFirstW = 0;

                double Hmax = (1.0 * divide.point_data[0].h + hmin) / expand;
                if (divide.point_data.Count >= 2)
                {
                    Hmax = ((1.0 * divide.point_data[0].h + hmin) / expand + (1.0 * divide.point_data[1].h + hmin) / expand) / 2;
                }
                for (int di = 0; di < divide.point_data.Count; di++)
                {
                    double h = (1.0 * divide.point_data[di].h + hmin) / expand - detaH;
                    double firstW = (1.0 * divide.point_data[di].w + wmin) / expand;
                    dFirstW += firstW;
                    while (h > Hmin)
                    {
                        List<Faro_point> LFp = new List<Faro_point>();
                        bool isBuild = false;
                        for (int i = -5; i < 5; i++)
                        {
                            double[] p01 = new double[2];
                            p01[0] = firstW;
                            p01[1] = h + ddh * i;
                            if (FindSeamBetweenRings(this, p01, out string message, out double[] seamHeight, out Faro_point[] seamPoint, out List<Faro_point> lllfp))
                            {
                                Faro_point faro_Point = new Faro_point()
                                {
                                    tag = 0,
                                    X = seamPoint[0].Y,
                                    Y = seamPoint[0].xe,
                                    H = seamPoint[1].Y,
                                    Hs = seamPoint[1].xe,
                                    Z = seamHeight[0] - seamHeight[1],
                                    R = (seamPoint[0].Y + seamPoint[1].Y) / 2,
                                };
                                LFp.Add(faro_Point);
                                if (!isBuild)
                                {
                                    lfpoint.Add(lllfp);
                                    isBuild = true;
                                }
                                else if (i <= 0) lfpoint[lfpoint.Count - 1] = lllfp;
                            }
                        }
                        if (LFp.Count <= 3/*&& LFp.Count>0*/)
                        {
                            //int n = 0;
                            //Faro_point faro_Point = LFp[n];
                            //GetRank.Add(faro_Point);
                            //lfp.Add(new Faro_point() { X = (faro_Point.X + faro_Point.H) / 2, Y = (faro_Point.Y + faro_Point.Hs) / 2 });
                        }
                        else
                        {
                            List<double> ld1 = new List<double>();
                            List<double> ld2 = new List<double>();
                            foreach (var v in LFp)
                            {
                                ld1.Add(v.R);
                                ld2.Add(v.Z);
                            }
                            GetMeanAndStd(ld1, out double mean1, out double std1);
                            GetMeanAndStd(ld2, out double mean2, out double std2);
                            if (std1 > 0.05 || std2 > 0.003)
                            {
                                for (int i = 0; i < LFp.Count; i++)
                                {
                                    LFp[i].xe = Math.Abs(LFp[i].R - mean1);
                                }
                                LFp = LFp.OrderBy(l => l.xe).ToList();
                                Faro_point faro_Point = LFp[0];
                                if (Math.Abs(faro_Point.X - firstW) < 0.02)
                                {
                                    firstW = LFp[0].R;
                                    GetRank.Add(faro_Point);
                                    lfp.Add(new Faro_point() { X = (faro_Point.X + faro_Point.H) / 2, Y = (faro_Point.Y + faro_Point.Hs) / 2 });
                                }
                            }
                            else
                            {
                                int n = LFp.Count / 2;
                                Faro_point faro_Point = LFp[n];

                                if (Math.Abs(faro_Point.X - firstW) < 0.02)
                                {
                                    firstW = mean1;
                                    GetRank.Add(faro_Point);
                                    lfp.Add(new Faro_point() { X = (faro_Point.X + faro_Point.H) / 2, Y = (faro_Point.Y + faro_Point.Hs) / 2 });
                                }
                            }
                        }
                        h -= detaH;
                    }
                    h = (1.0 * divide.point_data[di].h + hmin) / expand + detaH;
                    firstW = (1.0 * divide.point_data[di].w + wmin) / expand;
                    if (di == divide.point_data.Count - 1) Hmax = 2.7 * Math.PI * 2;
                    else Hmax = ((1.0 * divide.point_data[di + 1].h + hmin) / expand + (1.0 * divide.point_data[di].h + hmin) / expand) / 2;
                    while (h < Hmax)
                    {
                        List<Faro_point> LFp = new List<Faro_point>();
                        bool isBuild = false;
                        for (int i = -5; i < 5; i++)
                        {
                            double[] p01 = new double[2];
                            p01[0] = firstW;
                            p01[1] = h + ddh * i;
                            if (FindSeamBetweenRings(this, p01, out string message, out double[] seamHeight, out Faro_point[] seamPoint, out List<Faro_point> lllfp))
                            {
                                Faro_point faro_Point = new Faro_point()
                                {
                                    tag = 0,
                                    X = seamPoint[0].Y,
                                    Y = seamPoint[0].xe,
                                    H = seamPoint[1].Y,
                                    Hs = seamPoint[1].xe,
                                    Z = seamHeight[0] - seamHeight[1],
                                    R = (seamPoint[0].Y + seamPoint[1].Y) / 2,
                                };
                                LFp.Add(faro_Point);
                                if (!isBuild)
                                {
                                    lfpoint.Add(lllfp);
                                    isBuild = true;
                                }
                                else if (i <= 0) lfpoint[lfpoint.Count - 1] = lllfp;
                            }
                        }
                        if (LFp.Count <= 3 /*&& LFp.Count > 0*/)
                        {
                            //int n = 0;
                            //Faro_point faro_Point = LFp[n];
                            //GetRank.Add(faro_Point);
                            //lfp.Add(new Faro_point() { X = (faro_Point.X + faro_Point.H) / 2, Y = (faro_Point.Y + faro_Point.Hs) / 2 });
                        }
                        else
                        {
                            List<double> ld1 = new List<double>();
                            List<double> ld2 = new List<double>();
                            foreach (var v in LFp)
                            {
                                ld1.Add(v.R);
                                ld2.Add(v.Z);
                            }
                            GetMeanAndStd(ld1, out double mean1, out double std1);
                            GetMeanAndStd(ld2, out double mean2, out double std2);
                            if (std1 > 0.04 || std2 > 0.0025)
                            {
                                for (int i = 0; i < LFp.Count; i++)
                                {
                                    LFp[i].xe = Math.Abs(LFp[i].R - mean1);
                                }
                                LFp = LFp.OrderBy(l => l.xe).ToList();

                                Faro_point faro_Point = LFp[0];
                                if (Math.Abs(faro_Point.X - firstW) < 0.02)
                                {
                                    firstW = LFp[0].R;
                                    GetRank.Add(faro_Point);
                                    lfp.Add(new Faro_point() { X = (faro_Point.X + faro_Point.H) / 2, Y = (faro_Point.Y + faro_Point.Hs) / 2 });
                                }
                            }
                            else
                            {
                                int n = LFp.Count / 2;
                                Faro_point faro_Point = LFp[n];
                                if (Math.Abs(faro_Point.X - firstW) < 0.02)
                                {
                                    firstW = mean1;
                                    GetRank.Add(faro_Point);
                                    lfp.Add(new Faro_point() { X = (faro_Point.X + faro_Point.H) / 2, Y = (faro_Point.Y + faro_Point.Hs) / 2 });
                                }
                            }
                        }
                        h += detaH;
                    }
                    Hmin = Hmax;
                }
                if (lfp.Count <= 0) return;
                lfp = lfp.OrderBy(l => l.Y).ToList();
                dFirstW = dFirstW / divide.point_data.Count;
                double[] dX = new double[lfp.Count];
                double[] dY = new double[lfp.Count];
                double[] dYY = new double[lfp.Count];

                double[] tX = new double[lfp.Count];
                double[] tY = new double[lfp.Count];
                for (int i = 0; i < lfp.Count; i++)
                {
                    tX[i] = lfp[i].Y;
                    tY[i] = lfp[i].X;
                }
                MathMethor.Polyfit(tX, tY, 3, out double[,] P);
                for (int i = lfp.Count - 1; i >= 0; i--)
                {
                    dX[i] = P[0, 0] * tX[i] * tX[i] * tX[i] + P[1, 0] * tX[i] * tX[i] + P[2, 0] * tX[i] + P[3, 0] - tY[i];
                }
                double xmean = dX.Average();
                for (int i = lfp.Count - 1; i >= 0; i--)
                {
                    double dxx = Math.Abs(dX[i] - xmean);
                    if (dxx > 0.016) lfp.Remove(lfp[i]);
                }

            }
            else if (divide.attribute == "platform")
            {
                GetRank = new List<Faro_point>();
                try
                {
                    if (divide.point_data.Count > 1)
                    {
                        double[] p1 = new double[2];
                        p1[1] = (1.0 * divide.point_data[1].h + hmin) / expand;
                        p1[0] = (1.0 * divide.point_data[1].w + wmin) / expand;
                        double[] p0 = new double[2];
                        p0[1] = (1.0 * divide.point_data[0].h + hmin) / expand;
                        p0[0] = (1.0 * divide.point_data[0].w + wmin) / expand;
                        for (int j = -5; j < 25; j++)
                        {
                            double[] p01 = new double[2];
                            p01[0] = (p1[0] * j + p0[0] * (20 - j)) / 20;
                            p01[1] = (p1[1] * j + p0[1] * (20 - j)) / 20;
                            if (FindSeamInsideRing(this, p01, out string message, out double[] seamHeight, out Faro_point[] seamPoint))
                            {
                                Faro_point faro_Point = new Faro_point()
                                {
                                    tag = 1,
                                    X = seamPoint[0].Y,
                                    Y = seamPoint[0].xe,
                                    H = seamPoint[1].Y,
                                    Hs = seamPoint[1].xe,
                                    Z = seamHeight[0] - seamHeight[1],
                                    R = (seamPoint[0].Y + seamPoint[1].Y) / 2,
                                    xe = (seamPoint[0].xe + seamPoint[1].xe) / 2,
                                };
                                GetRank.Add(faro_Point);
                            }
                        }

                        List<Faro_point> lp1 = new List<Faro_point>();
                        if (GetRank.Count <= 0) return;
                        double xe0 = GetRank[0].xe;
                        for (int i = 0; i < GetRank.Count; i++)
                        {
                            Faro_point faro_Point = new Faro_point()
                            {
                                xe = GetRank[i].R,
                                H = GetRank[i].xe - xe0,
                            };
                            lp1.Add(faro_Point);
                        }
                        double[] s = TunnelClassify_old(lp1);
                        for (int i = 0; i < GetRank.Count; i++)
                        {
                            GetRank[i].xe = GetRank[i].xe - s[i];
                        }

                        double p010 = (p1[0] * (-5) + p0[0] * (20 + 5)) / 20;
                        double p020 = (p1[0] * 25 + p0[0] * (-5)) / 20;
                        int N = GetRank.Count;
                        if (N <= 1) return;
                        double[,] X = new double[N, 1];
                        double[] Y = new double[N];
                        for (int i = 0; i < N; i++)
                        {
                            X[i, 0] = GetRank[i].R;
                            Y[i] = GetRank[i].xe;
                        }
                        MathMethor.Polyfit(X, Y, 1, out double[,] P2);
                        double SumI = 0;
                        for (int i = 0; i < N; i++)
                        {
                            GetRank[i].Color = (int)(1000 * Math.Abs(Y[i] - (X[i, 0] * P2[0, 0] + P2[1, 0])));
                            SumI += GetRank[i].Color;
                        }
                        SumI /= N;
                        GetRank = GetRank.FindAll(l => l.Color < SumI / 1.1).OrderBy(l => l.R).ToList();
                        N = GetRank.Count;
                        X = new double[N, 1];
                        Y = new double[N];
                        for (int i = 0; i < N; i++)
                        {
                            X[i, 0] = GetRank[i].R;
                            Y[i] = GetRank[i].xe;
                        }
                        MathMethor.Polyfit(X, Y, 1, out P2);
                        Faro_point faro_Point1 = new Faro_point()
                        {
                            X = p010,
                            Y = p010 * P2[0, 0] + P2[1, 0] + 0.02,
                            H = p010,
                            Hs = p010 * P2[0, 0] + P2[1, 0] - 0.02,
                            Z = GetRank[0].Z,
                            tag = 1,
                        };
                        Faro_point faro_Point2 = new Faro_point()
                        {
                            X = p020,
                            Y = p020 * P2[0, 0] + P2[1, 0] + 0.02,
                            H = p020,
                            Hs = p020 * P2[0, 0] + P2[1, 0] - 0.02,
                            Z = GetRank[N - 1].Z,
                            tag = 1,
                        };
                        Faro_point faro_Point12 = new Faro_point()
                        {
                            X = (p010 + p020) / 2,
                            Y = (p010 + p020) / 2 * P2[0, 0] + P2[1, 0] + 0.02,
                            H = (p010 + p020) / 2,
                            Hs = (p010 + p020) / 2 * P2[0, 0] + P2[1, 0] - 0.02,
                            Z = GetRank[N / 2].Z,
                            tag = 1,
                        };
                        lfp.Add(new Faro_point() { X = (faro_Point1.X + faro_Point1.H) / 2, Y = (faro_Point1.Y + faro_Point1.Hs) / 2 });
                        lfp.Add(new Faro_point() { X = (faro_Point12.X + faro_Point12.H) / 2, Y = (faro_Point12.Y + faro_Point12.Hs) / 2 });
                        lfp.Add(new Faro_point() { X = (faro_Point2.X + faro_Point2.H) / 2, Y = (faro_Point2.Y + faro_Point2.Hs) / 2 });

                        GetRank = new List<Faro_point>();
                        GetRank.Add(faro_Point1);
                        GetRank.Add(faro_Point12);
                        GetRank.Add(faro_Point2);
                    }
                }
                catch (Exception exp) { }

            }
        }

        public int IDCloud = 0;
#endregion

#region 影像图生成
        /// <summary>
        /// 生成高清影像图
        /// </summary>
        /// <param name="savepath">影像保存路径</param>
        /// <param name="messagePath">影像信息保存路径</param>
        /// <param name="expand">影像分辨率（像素/米）</param>
        /// <param name="height">影像高度</param>
        public void ET_GetPhoto_later(string savepath, string messagePath, int expand, int height)
        {
            try
            {
                int width = (int)(Math.Floor(expand * (m2 - m1))) + 1;
                byte[,] lp_arr = new byte[height, width];
                //给数据赋值
                for (int i = 0; i < ap.Length; i++)
                {
                    if (ap[i].Length <= 0) continue;
                    foreach (var v in ap[i])
                    {
                        if (v.tag == 1) continue;
                        //lp.Add(v);
                        int h1 = (int)Math.Floor(expand * v.xe);
                        int w1 = (int)(Math.Floor(expand * (v.Y - m1)));
                        if (w1 < 0) {/* min = Math.Min(min, w1);*/ w1 = 0; }
                        else if (w1 >= width) { /*max = Math.Max(max, w1);*/ w1 = width - 1; }
                        if (h1 >= height) h1 = height - 1;
                        else if (h1 < 0) h1 = 0;
                        lp_arr[h1, w1] = (byte)v.Color;
                    }
                }
                GetPhoto GetPic = new GetPhoto();
                lp_arr = GetPic.FillBlack(lp_arr, width, height);
                GetPic.BornPic_later(lp_arr, height, width, savepath);
                string str = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + " " + savepath + " " + expand + " " + m1 + " " + m2;
                Text_IO.WriteLineToTxt(messagePath, str);
            }
            catch (Exception e)
            {
                string log = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + " 成图过程发生错误：" + e.Message;
                Text_IO.WriteLineToTxt(WriteLogPath, log);
            }
        }
#endregion

#region 读取&保存文件
        /// <summary>
        /// 保存三维数据和数据里程信息
        /// </summary>
        /// <param name="savepath"></param>
        /// <param name="messagePath"></param>
        public void ET_Save3DFile(string savepath, string messagePath)
        {
            Text_IO.WriteS3D_bit2(ap, savepath);
            string str1 = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + " " + savepath + " " + m1 + " " + m2;
            Text_IO.WriteLineToTxt(messagePath, str1);
        }
        /// <summary>
        /// 读取三维数据和里程信息
        /// </summary>
        /// <param name="readpath">三维数据文件路径</param>
        /// <param name="readpath2">里程数据文件路径</param>
        /// <param name="col">上一文件截取列数</param>
        /// <param name="precol">上一文件总列数</param>
        public void ET_Read3DFileAndLm(string readpath, string readpath2, int col, int precol)
        {
            if (!File.Exists(readpath)) { ET_INI(); return; }
            if (!Text_IO.ReadS3DFile(readpath, col, precol / 2, numRow * 2, out ap_pre))
            {
                if (ap_pre.Length == 0 || ap_pre[0] == null || ap_pre[0].Length == 0) { ET_INI(); return; }

                ET_ReadLmFile(readpath2);

                string path2 = readpath.Replace(".tsd", ".tsds");
                if (File.Exists(path2))
                {

                    if (Text_IO.ReadS3D_xeH(path2, col, precol / 2, numRow * 2, ref ap_pre))
                    {
                        File.Delete(path2);
                        // File.Delete(readpath);
                        return;
                    }
                }
                int cols = ap_pre.Length;
                for (int i = 0; i < cols; i++)
                {
                    List<Faro_point> lfp = new List<Faro_point>();
                    Ellipse ellipse0 = ellipse;
                    double r = ellipse.A;
                    Ellipse ep = new Ellipse()
                    {
                        A = lm_pre[0].ep.A,
                        B = lm_pre[0].ep.B,
                        X0 = lm_pre[0].ep.X0,
                        Y0 = lm_pre[0].ep.Y0,
                        Angle = lm_pre[0].ep.Angle,
                    };
                    for (int j = 1; j < lm_pre.Count; j++)
                    {
                        if (lm_pre[j].col > i + col)
                        {
                            break;
                        }
                        ep.A = lm_pre[j].ep.A;
                        ep.B = lm_pre[j].ep.B;
                        ep.X0 = lm_pre[j].ep.X0;
                        ep.Y0 = lm_pre[j].ep.Y0;
                        ep.Angle = lm_pre[j].ep.Angle;
                    }
                    for (int j = 0; j < ap_pre[i].Length; j++)
                    {
                        if (ap_pre[i][j].tag == 1) continue;
                        double Xr, Yr;
                        if (ap_pre[i][j].X == 0)
                        {
                            Xr = 0;
                            Yr = ellipse.Y0 + Math.Sqrt(r * r - ellipse.X0 * ellipse.X0);
                        }
                        else
                        {
                            double ta = ap_pre[i][j].Z / ap_pre[i][j].X;
                            double yX_1 = ellipse.Y0 * ta + ellipse.X0;
                            double yX_2 = 1 + ta * ta;
                            if (ap_pre[i][j].X >= 0)
                            {
                                Xr = (yX_1 + Math.Sqrt(yX_1 * yX_1 + yX_2 * (r * r - ellipse.X0 * ellipse.X0 - ellipse.Y0 * ellipse.Y0))) / yX_2;
                            }
                            else
                            {
                                Xr = (yX_1 - Math.Sqrt(yX_1 * yX_1 + yX_2 * (r * r - ellipse.X0 * ellipse.X0 - ellipse.Y0 * ellipse.Y0))) / yX_2;
                            }
                            Yr = Xr * ta;
                        }
                        double ae = Math.Atan2(ap_pre[i][j].Z - ep.Y0, ap_pre[i][j].X - ep.X0) - ep.Angle;
                        double re = ep.A * ep.B / Math.Sqrt(Math.Pow(ep.A * Math.Sin(ae), 2) + Math.Pow(Math.Cos(ae) * ep.B, 2));
                        ap_pre[i][j].H = Math.Sqrt(Math.Pow(ap_pre[i][j].Z - ep.Y0, 2) + Math.Pow(ap_pre[i][j].X - ep.X0, 2)) - re;
                        //p.H = (p.Color) * 0.01 / 255;
                        ap_pre[i][j].xe =/* 2*Math.PI*r-*/(Math.PI - Math.Atan2(Xr - ellipse.X0, Yr - ellipse.Y0)) * r;
                        lfp.Add(ap_pre[i][j]);
                    }
                    double[] s = TunnelClassify(lfp);
                    for (int k = 0, j = 0; k < ap_pre[i].Length; k++)
                    {
                        if (ap_pre[i][k].tag != 1)
                        {
                            ap_pre[i][k].Hs = ap_pre[i][k].H;
                            ap_pre[i][k].H -= s[j];
                            j++;
                        }
                    }
                }
                //File.Delete(readpath);
            }
            else ET_ReadLmFile(readpath2);
        }
        /// <summary>
        /// 读取三维数据（无使用）
        /// </summary>
        /// <param name="readpath">三维数据文件路径</param>
        /// <param name="col">上一文件截取列数</param>
        /// <param name="precol">上一文件总列数</param>
        public void ET_Read3DFile(string readpath, int col, int precol)
        {
            if (!File.Exists(readpath)) return;
            Text_IO.ReadS3DFile(readpath, col, precol / 2, numRow * 2, out ap_pre);
        }
        /// <summary>
        /// 读取里程信息
        /// </summary>
        /// <param name="readpath">里程数据文件路径</param>
        public void ET_ReadLmFile(string readpath)
        {
            if (!File.Exists(readpath)) return;
            Text_IO.ReadLmFile(readpath, out lm_pre);
        }
        /// <summary>
        /// 初始化lp_pre和lm_pre
        /// </summary>
        public void ET_INI()
        {
            lm_pre = new List<MileageEllipse>();
            ap_pre = new Faro_point[0][];
        }
        /// <summary>
        /// 保存LM里程数据
        /// </summary>
        /// <param name="savepath">保存路径</param>
        public void ET_SaveLM(string savepath)
        {
            List<string> ls = new List<string>();
            foreach (var v in lme)
            {
                string l = v.mileage + " " + v.dm + " " + v.col + " " + v.ep.A + " " + v.ep.B + " " + v.ep.X0 + " " + v.ep.Y0 + " " + v.ep.Angle;
                ls.Add(l);
            }
            Text_IO.WriteLineToTxt(savepath, ls, true);
        }
#endregion

#region 断面处理
        /// <summary>
        /// 得到每环断面文件
        /// </summary>
        /// <param name="first">上一文件最后一个环缝里程</param>
        /// <param name="ldivide">环缝信息</param>
        /// <param name="PathHead">保存文件路径</param>
        public void ET_GetSectionFile(double first, List<DivideLine> ldivide, string PathHead)
        {
            List<double> lm_cut = new List<double>();
            if (first != 0) lm_cut.Add(first);
            for (int i = 0; i < ldivide.Count; i++)
            {
                if (ldivide[i].attribute == "Ring")
                {
                    double wsum = 0; double hsum = 0;
                    foreach (var v in ldivide[i].point_data)
                    {
                        wsum += v.x; hsum += v.y;
                    }
                    //lb.Add(new BaseXY() { Color = ldivide[i].ID, X = wsum / ldivide[i].point_data.Count, Y = hsum / ldivide[i].point_data.Count });
                    lm_cut.Add(wsum / ldivide[i].point_data.Count);
                }
            }
            if (lm_cut.Count <= 1) return;
            lm_cut = lm_cut.OrderBy(l => l).ToList();
            ET_GetSectionFile(lm_cut, PathHead);
        }
        /// <summary>
        /// 筛选并得到每环断面文件
        /// </summary>
        /// <param name="first"></param>
        /// <param name="lm_cut">分环里程信息</param>
        /// <param name="PathHead">保存文件路径</param>
        public void ET_GetSectionFile_test(List<double> lm_cut, string PathHead)
        {
            if (m1 > 0 && m2 > 0)
            {
                if (lm_cut.Count == 0)
                {
                    lm_cut = new List<double>();
                    for (double k = m1 + 0.3; k < m2; k++)
                    {
                        lm_cut.Add(k);
                    }
                }
                ET_GetSectionFile(lm_cut, PathHead);
            }
        }
        /// <summary>
        /// 得到每环断面文件
        /// </summary>
        /// <param name="lm_cut">分环里程信息</param>
        /// <param name="PathHead">保存文件路径</param>
        public void ET_GetSectionFile(List<double> lm_cut, string PathHead)
        {
            if (lm_cut.Count <= 0) return;
            List<List<Faro_point>> llf = new List<List<Faro_point>>();
            lm_cut = lm_cut.OrderBy(l => l).ToList();
            for (int i = 0; i < lm_cut.Count - 1; i++)
            {
                llf.Add(new List<Faro_point>());
            }
            foreach (var bp in ap)
            {
                foreach (var v in bp)
                {
                    if (v.tag == 1) continue;
                    if (v.Y < lm_cut[0] || v.Y > lm_cut[lm_cut.Count - 1]) continue;
                    int i = 1;
                    for (; i < lm_cut.Count; i++)
                    {
                        if (v.Y <= lm_cut[i]) break;
                    }
                    llf[i - 1].Add(v);
                }
            }
            if (ap_pre != null)
            {
                foreach (var bps in ap_pre)
                {
                    foreach (var v in bps)
                    {
                        if (v.tag == 1) continue;
                        if (v.Y < lm_cut[0] || v.Y > lm_cut[lm_cut.Count - 1]) continue;
                        int i = 1;
                        for (; i < lm_cut.Count; i++)
                        {
                            if (v.Y <= lm_cut[i]) break;
                        }
                        llf[i - 1].Add(v);
                    }
                }
            }
            double deta_row = 0.0015;//
            for (int i = 0; i < lm_cut.Count - 1; i++)
            {
                string writeSecPath = PathHead + (int)((lm_cut[i] + lm_cut[i + 1]) / 2 * 1000) + "mm.txt";
                SectionAnalysis.Section_Get_0201(llf[i], writeSecPath, deta_row);
            }
        }
#endregion

#region 其他辅助函数
        /// <summary>
        /// 无计算椭圆参数时根据输入参数得到椭圆参数
        /// </summary>
        /// <param name="GetEllipse">输出椭圆信息</param>
        /// <param name="Radius">拟合椭圆半径</param>
        /// <param name="H_Scanner">扫描仪距离轨面高度</param>
        public void GetEllipse(out Ellipse GetEllipse, double Radius, double H_Scanner)
        {
            GetEllipse = new Ellipse();
            if (Radius == 2.7)
            {
                GetEllipse.X0 = 0;
                GetEllipse.Y0 = Radius - 0.915 - H_Scanner;
                GetEllipse.A = Radius + 0.001;
                GetEllipse.B = Radius - 0.001;
                GetEllipse.Angle = 0;
            }
            else if (Radius == 2.75)
            {
                GetEllipse.X0 = 0;
                GetEllipse.Y0 = Radius - 0.850 - H_Scanner;
                GetEllipse.A = Radius + 0.001;
                GetEllipse.B = Radius - 0.001;
                GetEllipse.Angle = 0;
            }
            else if (Radius == 3.85)
            {
                GetEllipse.X0 = 0;
                GetEllipse.Y0 = Radius - 1.250 - H_Scanner;
                GetEllipse.A = Radius + 0.001;
                GetEllipse.B = Radius - 0.001;
                GetEllipse.Angle = 0;
            }
            else
            {
                GetEllipse.X0 = 0;
                GetEllipse.Y0 = Radius - 0.850 - H_Scanner;
                GetEllipse.A = Radius + 0.001;
                GetEllipse.B = Radius - 0.001;
                GetEllipse.Angle = 0;

            }

        }
        /// <summary>
        /// 获取扫描文件起止时间用于校验扫描时间单位
        /// </summary>
        public void CheckAuto_pra()
        {
            ulong t1, t2;
            OpenFls.libRef.getAutomationTimeOfScanPoint(0, (numRow - 1), 0, out t1);
            OpenFls.libRef.getAutomationTimeOfScanPoint(0, (numRow - 1), (numCol - 1), out t2);
            CheckAuto_pra(t1, t2);
        }
        /// <summary>
        /// 校验扫描时间单位（默认扫描采用测量速率最高（8），如果不是该参数需进行相应调整）
        /// </summary>
        /// <param name="t1"></param>
        /// <param name="t2"></param>
        public void CheckAuto_pra(ulong t1, ulong t2)
        {
            double t12 = (double)(t2 - t1) / numCol / numRow;
            if (Math.Abs(t12 - 2.457) < 0.1) Autotime_pra = 5;//该情况出现较少

        }
        /// <summary>
        /// 简单检测扫描轨距
        /// </summary>
        /// <param name="SecData">断面数据</param>
        /// <param name="LimitData">轨距信息，属性Z代表轨距</param>
        /// <returns></returns>
        public static bool GetPoint_cloud(List<BaseXY> SecData, out Faro_point LimitData)
        {
            LimitData = new Faro_point();
            List<BaseXY> lfp1 = SecData.FindAll(l => l.Y < -0.4 && (l.X < -0.5 && l.X > -0.9)).ToList();
            List<BaseXY> lfp2 = SecData.FindAll(l => l.Y < -0.4 && (l.X > 0.5 && l.X < 0.9)).ToList();
            if (lfp1.Count < 100 || lfp2.Count < 100) return false;
            lfp1 = lfp1.OrderByDescending(l => l.Y).ToList();
            lfp2 = lfp2.OrderByDescending(l => l.Y).ToList();
            List<BaseXY> lfp3 = new List<BaseXY>();
            List<BaseXY> lfp4 = new List<BaseXY>();
            double max1 = lfp1[0].Y; double max2 = lfp2[0].Y;
            for (int i = 0; i < lfp1.Count; i++)
            {
                if (lfp1[i].Y > max1 - 0.006) lfp3.Add(lfp1[i]);
                else if (lfp3.Count < 50) { lfp3.Add(lfp1[i]); }
            }
            for (int i = 0; i < lfp2.Count; i++)
            {
                if (lfp2[i].Y > max2 - 0.006) lfp4.Add(lfp2[i]);
                else if (lfp4.Count < 50) { lfp4.Add(lfp2[i]); }
            }
            List<BaseXY> lfp5 = lfp3.Concat(lfp4).ToList();
            double mean1 = lfp3.Average(l => l.X);
            double mean2 = lfp4.Average(l => l.X);
            double[,] X = new double[lfp5.Count, 1];
            double[] Y = new double[lfp5.Count];
            for (int i = 0; i < X.Length; i++)
            {
                X[i, 0] = lfp5[i].X; Y[i] = lfp5[i].Y;
            }
            MathMethor.Polyfit(X, Y, 1, out double[,] P);
            if (Math.Abs(P[0, 0]) > 0.1)
            {
                return false;
            }
            lfp3 = lfp1.FindAll(l => l.X > mean1 && l.Y > max1 - 0.05).ToList();
            lfp4 = lfp2.FindAll(l => l.X < mean2 && l.Y > max2 - 0.05).ToList();
            mean1 = lfp3.Max(l => l.X); mean2 = lfp4.Min(l => l.X);

            LimitData = new Faro_point() { X = mean1, Y = mean2, Z = mean2 - mean1 };
            double centerOrignal_x = (mean1 + mean2) / 2;
            double centerOrignal_y = centerOrignal_x * P[0, 0] + P[1, 0];
            return true;
        }
#endregion

    }
    /// <summary>
    /// 解析使用类
    /// </summary>
    class ThreadInfo
    {
        /// <summary>
        /// 当前圈数
        /// </summary>
        public int round { get; set; }
        /// <summary>
        /// 前面一列点云数组
        /// </summary>
        public Array p1 { get; set; }
        /// <summary>
        /// 后面一列点云数组
        /// </summary>
        public Array p2 { get; set; }
        /// <summary>
        /// 前面一列灰度数组
        /// </summary>
        public Array c1 { get; set; }
        /// <summary>
        /// 后面一列灰度数组
        /// </summary>
        public Array c2 { get; set; }
        /// <summary>
        /// 总行数
        /// </summary>
        public int numRow { get; set; }
        /// <summary>
        /// 总列数
        /// </summary>
        public int numCol { get; set; }
        /// <summary>
        /// 起始里程
        /// </summary>
        public double mileage_s { get; set; }
        /// <summary>
        /// 扫描点里程间隔
        /// </summary>
        public double ddm { get; set; }
        /// <summary>
        /// 拟合椭圆
        /// </summary>
        public Ellipse ep1 { get; set; }
        /// <summary>
        /// 拟合椭圆
        /// </summary>
        public Ellipse ep2 { get; set; }
    }

}
