using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RelAnalysis3
{
    /// <summary>
    /// 调用进程类
    /// </summary>
    public class Process_Solve
    {
        /// <summary>
        /// 是否是错缝隧道
        /// </summary>
        public static bool IsCuoFeng = true;
        /// <summary>
        /// EXE所在路径
        /// </summary>
        public static string EXEPath = "D://TSD";
        /// <summary>
        /// 运行信息
        /// </summary>
        private static string python_message;
        /// <summary>
        /// 输出点集（用于判断EXE运行是否结束）
        /// 可用于洇湿、破损等病害识别过程，为之后的通用性此处暂不做修改
        /// </summary>
        public static List<Contour> contours1 = new List<Contour>();
        /// <summary>
        /// 是否调用EXE（新版（2020之后）都默认调用）
        /// </summary>
        public static bool isUseEXE = true;
        /// <summary>
        /// 输出环缝等信息
        /// </summary>
        public static List<DivideLine> divideLines = new List<DivideLine>();
        /// <summary>
        /// 用于判断是否接收到消息
        /// </summary>
        static bool check = false;
        /// <summary>
        /// 调用环缝识别程序
        /// </summary>
        /// <param name="photopath">深度图路径</param>
        /// <param name="expand">图片分辨率，单位像素/米</param>
        /// <param name="Wide">环宽</param>
        /// <param name="ldivideline">输出环缝等信息</param>
        /// <returns></returns>
        static public string LS_main(string photopath, int expand, double Wide, out List<DivideLine> ldivideline)
        {
            string[] strArr = new string[5];
            divideLines = new List<DivideLine>();
            ///输入格式：图片分辨率 环宽 图片路径 EXE路径
            ///
            strArr[0] = expand.ToString();
            strArr[1] = Wide.ToString();
            strArr[2] = photopath;
            strArr[3] = EXEPath + "\\PY_EXE";
            ///初始化ldivideline、contours1
            ldivideline = new List<DivideLine>();
            contours1 = new List<Contour>();
            check = false;
            //strArr[3]=path_save;
            if (!File.Exists(photopath))
            {
                python_message = "路径为空";
                return python_message;
            }
            string sArguments = "识别环缝.py";
            python_message = "";
            if (isUseEXE)
            {
                //RunPythonScript_test(9, "LS_main202017.exe", "-u", strArr);
                if (IsCuoFeng) RunPythonScript_test(9, "LS_main0803.exe", "-u", strArr);
                else RunPythonScript_test(9, "LSK_TongFeng_FenHuan.exe", "-u", strArr);
            }
            else
            {
                RunPythonScript(9, sArguments, "-u", strArr);
            }
            while (true)
            {
                if (contours1.Count> 0)
                {
                    ldivideline = divideLines;
                    break;
                }
                //System.Threading.Thread.Sleep(100);
                //N++;
                //if (N > 300) break;
            }
            return python_message;
        }
        /// <summary>
        /// 解析环缝信息
        /// </summary>
        /// <param name="jsonText">环缝信息文本（json格式）</param>
        /// <returns></returns>
        static string DeJson_Divide(string jsonText)
        {
            JObject jo = (JObject)JsonConvert.DeserializeObject(jsonText);
            string str = "";
            contours1 = new List<Contour>();
            ///判断是否识别到了环缝
            if (jo["isReady"].ToString() == "True")
            {
                divideLines = new List<DivideLine>();
                ///环间缝和环内缝获取
                foreach (var cnt in jo["data"])
                {
                    DivideLine divideLine = new DivideLine();
                    divideLine.attribute = cnt["attribute"].ToString();
                    switch (divideLine.attribute)
                    {
                        case "Ring":
                            divideLine.ID = Convert.ToInt32(cnt["ID"].ToString());
                            break;
                        case "platform":
                            divideLine.left_ID = Convert.ToInt32(cnt["left_ID"].ToString());
                            divideLine.right_ID = Convert.ToInt32(cnt["right_ID"].ToString());
                            break;
                    }
                    divideLine.point_data = new List<point_data>();
                    bool isValue = false;
                    foreach (var v in cnt["point_data"])
                    {
                        isValue = true;
                        point_data point_Data = new point_data();
                        point_Data.w = Convert.ToInt32(v["w"].ToString());
                        point_Data.h = Convert.ToInt32(v["h"].ToString());
                        divideLine.point_data.Add(point_Data);
                    }
                    if (!isValue) continue;
                    if (!IsCuoFeng && divideLine.attribute == "Ring" && divideLine.point_data.Count == 2) continue;
                    divideLines.Add(divideLine);
                    contours1.Add(new Contour());
                }
                ///螺栓孔轮廓获取
                try {
                    foreach (var cn2 in jo["luosuankong"])
                    {
                        DivideLine divideLine = new DivideLine();
                        divideLine.attribute = "luosuankong";
                        divideLine.point_data = new List<point_data>();
                        foreach (var v in cn2)
                        {
                            point_data point_Data = new point_data();
                            point_Data.w = Convert.ToInt32(v[0].ToString());
                            point_Data.h = Convert.ToInt32(v[1].ToString());
                            divideLine.point_data.Add(point_Data);
                        }
                        divideLines.Add(divideLine);
                        contours1.Add(new Contour());
                    }
                } catch { }
                str = "完成";
            }
            else if (jo["isReady"].ToString() == "False")
            {
                str = "未识别到环缝";
            }
            else str = "返回信息错误";
            return str;
        }
        /// <summary>
        /// 调用python程序
        /// </summary>
        /// <param name="mode">调用方法（此处暂时只有一种解析）</param>
        /// <param name="sArgName">调用程序</param>
        /// <param name="args">调用参数</param>
        /// <param name="teps">传入参数</param>
        public static void RunPythonScript(int mode, string sArgName, string args = "", params string[] teps)
        {
            try
            {
                Process p = new Process();
                //string path = @"G:\work\py\suidaozhankai\隧道展开与越洋合并代码\" + sArgName;
                string path = EXEPath + @"\" + sArgName;//将PY文件放在程序启动路径里即可
                if (!File.Exists(path))
                {
                    python_message = "识别程序不存在";
                    if (contours1.Count == 0) contours1.Add(new Contour { S = -99 });
                    return;
                }                                                                             //p.StartInfo.FileName = @"C:\Users\admin\Anaconda3\python.exe";
                string filename = @"C:\Program Files\Anaconda3\python.exe";
                if (!File.Exists(filename)) filename = @"C:\Users\admin\Anaconda3\python.exe";
                if (!File.Exists(filename)) filename = @"C:\Users\Administrator\Anaconda3\python.exe";
                if (!File.Exists(filename))
                {
                    python_message = "识别程序不存在";
                    if (contours1.Count == 0) contours1.Add(new Contour { S = -99 });
                    return;
                }
                p.StartInfo.FileName = filename;
                string sArguments = path;
                foreach (string sigstr in teps)
                {
                    sArguments += " " + sigstr;
                }
                sArguments += " " + args;
                p.StartInfo.Arguments = sArguments;
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.RedirectStandardInput = true;
                p.StartInfo.RedirectStandardError = true;
                p.StartInfo.CreateNoWindow = true;
                p.Start();
                p.BeginOutputReadLine();
                p.OutputDataReceived += new DataReceivedEventHandler(p_OutputDataReceived);//注册接收函数
            }
            catch
            {
                python_message = "识别过程出错";
                if (contours1.Count == 0) contours1.Add(new Contour { S = -99 });
            }

        }
        /// <summary>
        /// 调用打包后的EXE文件
        /// </summary>
        /// <param name="mode">调用方法（此处暂时只有一种解析）</param>
        /// <param name="sArgName">调用程序</param>
        /// <param name="args">调用参数</param>
        /// <param name="teps">传入参数</param>
        public static void RunPythonScript_test(int mode, string sArgName, string args = "", params string[] teps)
        {
            try
            {
                Process p = new Process();
                string path = EXEPath + @"\PY_EXE\" + sArgName;//将文件放在程序启动路径里即可
                                                                                                    //p.StartInfo.FileName = @"C:\Users\admin\Anaconda3\python.exe";
                string filename = EXEPath + @"\PY_EXE\" + sArgName;
                if (!File.Exists(filename))
                {
                    python_message = "识别程序不存在";
                    if (contours1.Count == 0) contours1.Add(new Contour { S = -99 });
                    return;
                }
                p.StartInfo.FileName = filename;
                string sArguments = "";
                foreach (string sigstr in teps)
                {
                    sArguments += " " + sigstr;
                }
                sArguments += " " + args;
                p.StartInfo.Arguments = sArguments;
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.RedirectStandardInput = true;
                p.StartInfo.RedirectStandardError = true;
                p.StartInfo.CreateNoWindow = true;
                p.Start();
                p.BeginOutputReadLine();
                check = false;
                p.OutputDataReceived += new DataReceivedEventHandler(p_OutputDataReceived);//注册接收函数
            }
            catch
            {
                python_message = "识别过程出错";
                if (contours1.Count == 0) contours1.Add(new Contour { S = -99 });
            }
        }
        /// <summary>
        /// 信息接收函数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        static void p_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            ///首次接收到信息如果为null则说明程序已崩
            ///程序结束时将发送null，故一般程序会有两个输出
            if (!string.IsNullOrEmpty(e.Data))
            {
                check = true;
                python_message = DeJson_Divide(e.Data);
                if (contours1.Count == 0) contours1.Add(new Contour { S = -99 });
            }
            else
            {
                if (contours1.Count == 0&&(!check)) contours1.Add(new Contour { S = -99 });
            }
        }

        /// <summary>
        /// 解析里程文件，获取里程
        /// </summary>
        /// <param name="Path">里程文件路径</param>
        /// <param name="LM">里程</param>
        public static void GetMileage(string Path,out List<Mileage>LM) {
            ///一次性将里程文件读出成文本
            Encoding encode = System.Text.Encoding.Default;
            string jsonText = System.IO.File.ReadAllText(Path, encode);
            ///左右加上中括号后，里程文件可作为json文件解析
            jsonText = "[" + jsonText + "]";
            jsonText = jsonText.Replace(";", ",");
            LM = new List<Mileage>();
            ///通过json本身分块，逐个解析里程，得到里程ID、Time、M
            ///旧版小车（2020年及之前制作）存在重复时间里程的BUG，不同里程时间相同
            ///对于重复时间的里程，选取最后一个里程作为可信数据，其他里程舍去
            JArray jo = (JArray)JsonConvert.DeserializeObject(jsonText);
            double M= -99;long T = 0;
            Mileage mileage1 = new Mileage();
            for (int i = 0; i < jo.Count; i++) {
                var v = jo[i];
                Mileage mileage = new Mileage();
                mileage.ID = Convert.ToInt32(v["ID".ToString()]);
                mileage.Time = Convert.ToInt64(v["Time".ToString()]);
                mileage.M = Convert.ToDouble(v["Mileage".ToString()]);
                ///里程ID不为负数，此处选取-99作为同时间不同里程的标记；同时时间Time一般远大于0
                ///step1 M初始值为-99
                ///故i=0时将给T、M、mileage1初始化
                ///step2 通过T判断时间是否相同，如相同则执行step3；如不同执行step4
                ///step3 重新给mileage1赋值，并将mileage1。ID置为-99循环直到再次判断T与mileage.Time不同时，将mileage值赋给mileage1，同时T、M为mileage的值，即T>0,M!=-99
                ///step4 再次进入时保存mileage1，并将新的mileage值赋给mileage1，同时T、M为mileage的值，即T>0,M!=-99
                ///step5 跳到step2知道达到最大次数
                if (M != -99)//此处判断只第一次生效，可调整结构使时间更快（这里考虑到结构完整性没有修改）
                {
                    if (T == mileage.Time)
                    {
                        M = mileage.M;
                        mileage1.M = M;
                        mileage1.ID = -99;
                        continue;
                    }
                    if (mileage1.ID == -99)
                    {
                        T = mileage.Time;
                        M = mileage.M;
                        mileage1 = mileage;
                    }
                    else {
                        Mileage mileage2 = new Mileage() {
                            Time = mileage1.Time,
                            M=mileage1.M,
                            ID=mileage.ID,
                        };
                        LM.Add(mileage2);
                        T = mileage.Time;
                        M = mileage.M;
                        mileage1 = mileage;
                    }
                }
                else {
                    T = mileage.Time;
                    M = mileage.M;
                    mileage1 = mileage;
                    continue;
                }
            }
            ///添加最后一个里程
            LM.Add(mileage1);
            return ;
        }
    }
}
