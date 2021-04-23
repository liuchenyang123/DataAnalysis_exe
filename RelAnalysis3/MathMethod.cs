using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RelAnalysis3
{
    /// <summary>
    /// 椭圆拟合类
    /// </summary>
    public class Ellipse_Fitting
    {
        /// <summary>
        /// 根据输入数据进行一次性椭圆拟合
        /// </summary>
        /// <param name="coords"></param>
        /// <param name="a">椭圆长轴</param>
        /// <param name="b">椭圆短轴</param>
        /// <param name="xc">在输入坐标体系下，椭圆中心坐标X</param>
        /// <param name="yc">在输入坐标体系下，椭圆中心坐标Y</param>
        /// <param name="Angle">椭圆横轴与X轴逆时针夹角</param>
        public static void One_off_fitting(List<BaseXY> coords, out double a, out double b, out double xc, out double yc, out double Angle)
        {
            double[,] Xi = new double[5, 5];
            double[,] L = new double[5, 1];
            foreach (var m in coords)
            {
                double m11 = m.X * m.Y, m20 = m.X * m.X, m02 = m.Y * m.Y,
                    m30 = m20 * m.X, m21 = m20 * m.Y, m12 = m.X * m02, m03 = m02 * m.Y,
                    m31 = m30 * m.Y, m22 = m20 * m02, m13 = m.X * m03, m04 = m03 * m.Y;

                Xi[0, 0] += m22;
                Xi[0, 1] += m13;
                Xi[0, 2] += m21;
                Xi[0, 3] += m12;
                Xi[0, 4] += m11;
                //
                Xi[1, 0] += m13;
                Xi[1, 1] += m04;
                Xi[1, 2] += m12;
                Xi[1, 3] += m03;
                Xi[1, 4] += m02;
                //
                Xi[2, 0] += m21;
                Xi[2, 1] += m12;
                Xi[2, 2] += m20;
                Xi[2, 3] += m11;
                Xi[2, 4] += m.X;
                //
                Xi[3, 0] += m12;
                Xi[3, 1] += m03;
                Xi[3, 2] += m11;
                Xi[3, 3] += m02;
                Xi[3, 4] += m.Y;
                //
                Xi[4, 0] += m11;
                Xi[4, 1] += m02;
                Xi[4, 2] += m.X;
                Xi[4, 3] += m.Y;

                //
                L[0, 0] -= m31;
                L[1, 0] -= m22;
                L[2, 0] -= m30;
                L[3, 0] -= m21;
                L[4, 0] -= m20;
            }
            Xi[4, 4] = coords.Count;
            //
            var X = new Matrix(Xi).Inv() * new Matrix(L);
            //
            double A = X[0, 0], B = X[1, 0], C = X[2, 0], D = X[3, 0], E = X[4, 0];
            //
            xc = (2 * B * C - A * D) / (A * A - 4 * B);
            //
            yc = (2 * D - A * C) / (A * A - 4 * B);
            //
            double t = 2 * (A * C * D - B * C * C - D * D + 4 * B * E - A * A * E);
            double t1 = A * A - 4 * B;
            double t2 = Math.Sqrt(A * A + (1 - B) * (1 - B));
            //
            a = Math.Sqrt(t / (t1 * (B - t2 + 1)));
            //
            b = Math.Sqrt(t / (t1 * (B + t2 + 1)));
            //
            Angle = Math.Atan(Math.Sqrt((a * a - b * b * B) / (a * a * B - b * b)));
            if (A > 0) { Angle = -Angle; }
        }
        /// <summary>
        /// 根据输入数据进行椭圆拟合,自动排除噪点
        /// </summary>
        /// <param name="coords">输入坐标集合</param>
        /// <param name="eillpse">输出椭圆</param>
        public static void Robust_fitting(List<BaseXY> coords, out Ellipse ellipse)
        {
            try
            {
                double a, b, x0, y0, angle;
                //coords = coords.FindAll(l => l.Y > 0).ToList();
                Robust_fitting(coords, out a, out b, out x0, out y0, out angle);
                ellipse = new Ellipse();
                ellipse.A = a;
                ellipse.B = b;
                ellipse.X0 = x0;
                ellipse.Y0 = y0;
                ellipse.Angle = angle;
                //Robust_fitting(coords, out a, out b, out x0, out y0, out angle);
            }
            catch
            {
                ellipse = new Ellipse() { };
                ellipse.A = 2.711012; ellipse.B = 2.69057;
                ellipse.X0 = 0; ellipse.Y0 = 1.1720362129;
            }

        }
        /// <summary>
        /// 根据输入数据进行椭圆拟合,自动排除噪点
        /// </summary>
        /// <param name="coords">输入坐标集合</param>
        /// <param name="a">椭圆长轴</param>
        /// <param name="b">椭圆短轴</param>
        /// <param name="xc">在输入坐标体系下，椭圆中心坐标X</param>
        /// <param name="yc">在输入坐标体系下，椭圆中心坐标Y</param>
        /// <param name="Angle">椭圆横轴与X轴逆时针夹角</param>
        public static List<BaseXY> Robust_fitting(List<BaseXY> coords, out double a, out double b, out double xc, out double yc, out double Angle)
        {
            List<BaseXY> coords1 = coords;
            List<BaseXY> selectCoords = new List<BaseXY>();
            One_off_fitting(coords1, out a, out b, out xc, out yc, out Angle);
            double mean_error = 0;
            int pre_count = coords1.Count;
            int count = 0;
            double pre_xc = xc + 1;
            double pre_yc = yc + 1;
            while (Math.Max(Math.Abs(pre_xc - xc), Math.Abs(pre_yc - yc)) > 0.001)
            {
                mean_error = 0;
                foreach (var c in coords1)
                {
                    c.Error = Math.Abs(Math.Sqrt((c.X - xc) * (c.X - xc) + (c.Y - yc) * (c.Y - yc)) - R_Tunnel);
                    mean_error += c.Error;
                }
                mean_error = mean_error / coords1.Count();
                pre_count = coords1.Count();
                selectCoords = new List<BaseXY>();

                foreach (var c in coords1)
                {
                    if (c.Error < 2 * mean_error)
                    {
                        selectCoords.Add(c);
                    }
                }
                pre_xc = xc;
                pre_yc = yc;
                One_off_fitting(selectCoords, out a, out b, out xc, out yc, out Angle);
                count = selectCoords.Count;
                coords1 = selectCoords;
            }

            double g = 0.05;
            coords1 = coords;
            while (g > 0.002)
            {
                selectCoords = new List<BaseXY>();
                foreach (var c in coords1)
                {
                    double dy = c.Y - yc, dx = c.X - xc;
                    double aj = Math.Atan2(dy, dx);
                    aj -= Angle;
                    c.angle = aj;
                    double r = a * b / Math.Sqrt(Math.Pow(a * Math.Sin(aj), 2) + Math.Pow(Math.Cos(aj) * b, 2));
                    c.Error = Math.Abs(Math.Sqrt(dx * dx + dy * dy) - r);
                    if (c.Error < g)
                    {
                        selectCoords.Add(c);
                    }
                }
                selectCoords = selectCoords.OrderBy(s => s.angle).ToList();
                double angleMin = selectCoords[0].angle;
                double angleMax = selectCoords[selectCoords.Count - 1].angle;
                double da = (angleMax - angleMin) / 1800;
                int index = 0;
                List<BaseXY> selectCoords1 = new List<BaseXY>();//按角度重新选取
                for (int i = 1; i <= 1800; i++)
                {
                    double ag = (i - 1) * da + angleMin;
                    double bg = ag + da;

                    for (int j = index; j < selectCoords.Count - 1; j++)
                    {
                        if (j == selectCoords.Count - 2)
                        {
                            j = j + 1 - 1;
                        }
                        if (selectCoords[j].angle >= ag && selectCoords[j].angle <= bg)
                        {
                            selectCoords1.Add(selectCoords[j]);
                            index = j;

                            break;
                        }
                    }

                }
                One_off_fitting(selectCoords, out a, out b, out xc, out yc, out Angle);
                coords1 = selectCoords;
                g = g / 2;
            }
            return coords1;
        }
        /// <summary>
        /// 隧道半径（默认为2.7）
        /// </summary>
        public static double R_Tunnel = 2.7;
        public static void Robust_fitting1(List<BaseXY> coords, ref Ellipse ellipse)
        {
            try
            {
                double a = ellipse.A, b = ellipse.B, x0 = ellipse.X0, y0 = ellipse.Y0, angle = ellipse.Angle;
                //coords = coords.FindAll(l => l.Y > 0).ToList();
                Robust_fitting1(coords, ref a, ref b, ref x0, ref y0, ref angle);
                //ellipse = new Ellipse();
                ellipse.A = a;
                ellipse.B = b;
                ellipse.X0 = x0;
                ellipse.Y0 = y0;
                ellipse.Angle = angle;
            }
            catch
            {
                ellipse.A = 2.711012; ellipse.B = 2.69057;
                ellipse.X0 = 0; ellipse.Y0 = 1.1720362129;
            }

        }
        public static void Robust_fitting1(List<BaseXY> coords, ref double a, ref double b, ref double xc, ref double yc, ref double Angle)
        {


            double g = 0.05;
            var coords1 = coords;
            while (g > 0.002)
            {

                List<BaseXY> selectCoords = new List<BaseXY>();
                foreach (var c in coords1)
                {
                    double dy = c.Y - yc, dx = c.X - xc;
                    double aj = Math.Atan2(dy, dx);
                    aj -= Angle;
                    c.angle = aj;
                    double r = a * b / Math.Sqrt(Math.Pow(a * Math.Sin(aj), 2) + Math.Pow(Math.Cos(aj) * b, 2));
                    c.Error = Math.Abs(Math.Sqrt(dx * dx + dy * dy) - r);
                    if (c.Error < g)
                    {
                        selectCoords.Add(c);
                    }
                }

                One_off_fitting(selectCoords, out a, out b, out xc, out yc, out Angle);
                coords1 = selectCoords;
                g = g / 2;
            }
            //return coords1;

        }

    }
    /// <summary>
    /// 矩阵计算类
    /// </summary>
    public static partial class Matrix_Method
    {
        // 矩阵求逆方法2
        public static double[,] juzhenqiuni_2(double[,] M)
        {
            int m = M.GetLength(0);
            int n = M.GetLength(1);
            if (m != n)
            {
                Exception myException = new Exception("求逆的矩阵不是方阵");
                throw myException;
            }
            double[,] ret = new double[m, n];
            double[,] a0 = M;
            double[,] a = (double[,])a0.Clone();
            double[,] b = ret;
            int i, j, row, k;
            double max, temp;
            //单位矩阵         
            for (i = 0; i < n; i++)
            {
                b[i, i] = 1;
            }
            //遍历行数
            for (k = 0; k < n; k++)
            {
                max = 0;
                row = k;
                //遍历列，找最大元，其所在行为row       
                for (i = k; i < n; i++)
                {
                    temp = Math.Abs(a[i, k]);
                    if (max < temp)
                    {
                        max = temp;
                        row = i;
                    }
                }
                if (max == 0)
                {
                    Exception myException = new Exception("该矩阵无逆矩阵");
                    //throw myException;
                }
                //交换k与row行      
                if (row != k)
                {
                    for (j = 0; j < n; j++)
                    {
                        temp = a[row, j];
                        a[row, j] = a[k, j];
                        a[k, j] = temp;
                        temp = b[row, j];
                        b[row, j] = b[k, j];
                        b[k, j] = temp;
                    }
                }
                //首元化为1         
                for (j = k + 1; j < n; j++)
                {
                    a[k, j] /= a[k, k];
                }
                for (j = 0; j < n; j++)
                {
                    b[k, j] /= a[k, k];
                }
                a[k, k] = 1;
                //k列化为0
                //对a    
                for (j = k + 1; j < n; j++)
                {
                    for (i = 0; i < k; i++)
                    {
                        a[i, j] -= a[i, k] * a[k, j];
                    }
                    for (i = k + 1; i < n; i++)
                    {
                        a[i, j] -= a[i, k] * a[k, j];
                    }
                }
                //对b             
                for (j = 0; j < n; j++)
                {
                    for (i = 0; i < k; i++)
                    {
                        b[i, j] -= a[i, k] * b[k, j];
                    }
                    for (i = k + 1; i < n; i++)
                    {
                        b[i, j] -= a[i, k] * b[k, j];
                    }
                }
                for (i = 0; i < n; i++)
                {
                    a[i, k] = 0;
                }
                a[k, k] = 1;
            }
            return ret;
        }
        //矩阵转置
        public static double[,] juzhengzhuanzhi(double[,] B)
        {
            double[,] Bt = new double[B.GetLength(1), B.GetLength(0)];
            for (int i = 0; i < Bt.GetLength(0); i++)
            {
                for (int j = 0; j < Bt.GetLength(1); j++)
                {
                    Bt[i, j] = B[j, i];
                }
            }
            return Bt;
        }
        // 两矩阵相乘 
        public static double[,] juzhengxiangcheng(double[,] a, double[,] b)
        {
            double[,] result = new double[a.GetLength(0), b.GetLength(1)];
            if (a.GetLength(1) != b.GetLength(0))
            {
                return null;
            }
            else
            {
                for (int c = 0; c < result.GetLength(0); c++)//行
                {
                    for (int d = 0; d < result.GetLength(1); d++) //列
                    {
                        for (int j = 0; j < b.GetLength(0); j++)
                        {
                            result[c, d] = result[c, d] + a[c, j] * b[j, d];

                        }
                    }
                }
            }
            return result;
        }
        // 矩阵求逆
        public static double[,] juzhenqiuni(double[,] x)
        {
            //实现矩阵的求逆运算，并输出结果         
            int a = x.GetLength(0);
            int b = x.GetLength(1);
            double[,] result1 = new double[a, b]; //用来存放代数余子式矩阵 
            double[,] result2 = new double[a, b];  //用来存放求逆后的矩阵   
            if (a != b)
            {
                return null;
            }
            else
            {
                double m = QiuZhi(x, x.GetLength(0));    //求出行列式的模长，进行下一步的判断 
                if (m == 0)
                {                                 //判断是否有逆矩阵 
                    return null;
                }
                else
                {
                    result1 = QiuYuZiShi(x, a); //代数余子式                    
                    double n = 1 / m;
                    for (int i = 0; i < a; i++)     //一一求出逆矩阵的每一项 
                        for (int j = 0; j < a; j++)
                            result2[i, j] = n * result1[i, j];
                }
            }
            return result2;

        }
        //矩阵求秩
        public static double QiuZhi(double[,] x, int a) //行列式的值函数 
        {

            double[,] temp; //声明临时矩阵数组 
            double s = 1.0; //用他来控制余子式的符号   
            double result = 0.0;//声明临时存储矩阵行列式变量和符号变量 
            if (a == 1)
            {
                return x[0, 0] * s;
            }
            for (int i = 0; i < a; i++)
            {
                temp = new double[a - 1, a - 1];//给余子式数组分配空间 
                for (int j = 0; j < a - 1; j++)//j为余子式列，i为行                     
                    for (int k = 0; k < a - 1; k++)
                    {
                        if (j < i)//判断构造的元素在去掉的列前面还是后面 
                            temp[k, j] = x[k + 1, j];
                        else
                            temp[k, j] = x[k + 1, j + 1];
                    }
                s = Math.Pow(-1, i); //计算余子式的符号                
                result += x[0, i] * QiuZhi(temp, a - 1) * s;//用递归算法计算行列式的值     
            }
            return result;
        }
        //求余子式
        public static double[,] QiuYuZiShi(double[,] x, int a)  //求行列式的代数余子式矩阵，
        {
            double[,] temp;
            double[,] result = new double[a, a];
            for (int i = 0; i < a; i++) //i,m两个for循环对x矩阵遍历，求代数余子式 
            {
                for (int m = 0; m < a; m++)
                {
                    temp = new double[a - 1, a - 1]; //生成余子式数组 
                    for (int j = 0; j < a - 1; j++) //j为余子式列，i为行 
                        for (int k = 0; k < a - 1; k++)
                        {
                            if (j < i && k < m)  //判断构造的元素在去掉的列前面还是后面  行的上面还是下面                                 
                                temp[k, j] = x[k, j];
                            if (j < i && k >= m)
                                temp[k, j] = x[k + 1, j];
                            if (j >= i && k < m)
                                temp[k, j] = x[k, j + 1];
                            if (j >= i && k >= m)
                                temp[k, j] = x[k + 1, j + 1];
                        }
                    double s = Math.Pow(-1, i + m); //计算余子式的符号 
                    result[i, m] = s * QiuZhi(temp, a - 1);//得代数余子式的一项 
                }
            }
            return result;
        }
        // 矩阵相减
        public static double[,] juzhenxiangjian(double[,] a, double[,] b)
        {
            double[,] result = new double[a.GetLength(0), a.GetLength(1)];
            for (int i = 0; i < a.GetLength(0); i++)
            {
                for (int j = 0; j < a.GetLength(1); j++)
                {
                    result[i, j] = a[i, j] - b[i, j];
                }
            }
            return result;
        }
        // 矩阵相加
        public static double[,] juzhenxiangjia(double[,] a, double[,] b)
        {
            double[,] result = new double[a.GetLength(0), a.GetLength(1)];
            for (int i = 0; i < a.GetLength(0); i++)
            {
                for (int j = 0; j < a.GetLength(1); j++)
                {
                    result[i, j] = a[i, j] + b[i, j];
                }
            }
            return result;
        }
        #region 最小范数逆
        //public static double[,] ZuiXiaoFanShuNi(double[,] A)
        //{
        //    //N
        //   double [,] N = Matrix_Method.juzhengxiangcheng( Matrix_Method.juzhengzhuanzhi(A),A);
        //    Log.AppendLine_2("N=A(T)A:\r\n " + stringHelper2.ShuZuToStr(N));
        //    //NN
        //    double[,] NN = Matrix_Method.juzhengxiangcheng(Matrix_Method.juzhengzhuanzhi(N), N);
        //    Log.AppendLine_2("NN:\r\n " + stringHelper2.ShuZuToStr(NN));
        //    //N的秩
        //    int RN = FMOS_DAL.QiuZhi.Rank(N);
        //    Log.AppendLine_2("N的秩序R(N): " + RN);
        //    double[,] M = new double[RN, RN];//中间矩阵
        //    for (int h = 0; h < RN; h++)
        //    {
        //        for (int Lie = 0; Lie < RN; Lie++)
        //        {
        //            M[h, Lie] = NN[h, Lie];
        //        }
        //    }
        //    Log.AppendLine_2("M: \r\n" + stringHelper2.ShuZuToStr(M));
        //    double[,] M_ = juzhenqiuni_2(M);
        //    Log.AppendLine_2("M凯利逆: \r\n" + stringHelper2.ShuZuToStr(M_));
        //    double[,] NN_ = new double[N.GetLength(0), N.GetLength(1)];//中间矩阵
        //    for (int i = 0; i < N.GetLength(0); i++)
        //    {
        //        for (int j = 0; j < N.GetLength(0); j++) 
        //        {
        //            NN_[i, j] = M_[i, j];
        //        }
        //    }
        //    Log.AppendLine_2("NN~:\r\n" + stringHelper2.ShuZuToStr(NN_));
        //    double[,] Nm_ = juzhengxiangcheng(N, NN_);
        //    Log.AppendLine_2("最小范数逆Nm:\r\n" + stringHelper2.ShuZuToStr(Nm_));
        //    return Nm_;
        //}
        #endregion
        //求参数改正数，观测值改正数
        public static void GetXV(double[,] B, double[,] P, double[,] l, out double[,] x, out double[,] V, out double[,] Nbbni)
        {
            double[,] Bt = Matrix_Method.juzhengzhuanzhi(B);
            double[,] BtP = Matrix_Method.juzhengxiangcheng(Bt, P);
            double[,] Nbb = Matrix_Method.juzhengxiangcheng(BtP, B);
            double[,] W = Matrix_Method.juzhengxiangcheng(Matrix_Method.juzhengxiangcheng(Bt, P), l);
            Nbbni = Matrix_Method.juzhenqiuni_2(Nbb);
            x = Matrix_Method.juzhengxiangcheng(Nbbni, W);
            V = Matrix_Method.juzhenxiangjian(Matrix_Method.juzhengxiangcheng(B, x), l);
        }
        //求单位权方差,V观测值改正数,观测值权矩阵，t必要观测数，n观测值个数
        public static double o_2(double[,] V, double[,] P, int t, int n)
        {
            double[,] VtPV = Matrix_Method.juzhengxiangcheng(Matrix_Method.juzhengxiangcheng(Matrix_Method.juzhengzhuanzhi(V), P), V);
            double VPV = VtPV[0, 0];
            int r = n - t;
            double Do = VPV / r;
            return Do;
        }
        //用于量化表示公式
        public static string[,] juzhengxiangcheng(string[,] a, string[,] b)
        {
            string[,] result = new string[a.GetLength(0), b.GetLength(1)];
            if (a.GetLength(1) != b.GetLength(0))
            {
                return null;
            }
            else
            {
                for (int c = 0; c < result.GetLength(0); c++)//行
                {
                    for (int d = 0; d < result.GetLength(1); d++) //列
                    {
                        for (int j = 0; j < b.GetLength(0); j++)
                        {
                            //object mathRound = Math.Round(result[c, d] + a[c, j] * b[j, d], 5);;
                            if (a[c, j] == "0" || b[j, d] == "0")
                            {
                                if (result[c, d] == null || result[c, d] == "0")
                                {
                                    result[c, d] = "0";
                                }
                            }
                            else
                            {
                                if (result[c, d] == null || result[c, d] == "0")
                                {
                                    if (a[c, j] == "1")
                                    {
                                        result[c, d] = b[j, d];
                                        continue;
                                    }
                                    if (b[j, d] == "1")
                                    {
                                        result[c, d] = a[c, j];
                                        continue;
                                    }
                                    result[c, d] = string.Format("{0}*{1}", a[c, j], b[j, d]);
                                }
                                else
                                {
                                    if (a[c, j] == "1")
                                    {
                                        result[c, d] = string.Format("{0}+{1}", result[c, d], b[j, d]);
                                        continue;
                                    }
                                    if (b[j, d] == "1")
                                    {
                                        result[c, d] = string.Format("{0}+{1}", result[c, d], a[c, j]);
                                        continue;
                                    }
                                    result[c, d] = string.Format("{0}+{1}*{2}", result[c, d], a[c, j], b[j, d]);
                                }
                            }
                        }
                    }
                }
            }
            return result;
        }
        //方差
        public static double MeanError(double[] M)
        {
            double avg = M.Average();
            double nnCound = 0;
            double count = M.Length;
            foreach (var m in M)
            {
                nnCound += (m - avg) * (m - avg);
            }
            double 方差 = nnCound / count;
            return Math.Sqrt(方差);
        }
        //输入为度
        //public static double AngMeanError(double[] M)
        //{
        //    double avg = AngCoordCaculater.AvgHar(M);
        //    double nnCound = 0;
        //    double count = M.Length;
        //    foreach (var m in M)
        //    {
        //        double d = m - avg;
        //        //
        //        if (m - avg > 180)
        //        {
        //            d = d - 360;
        //        }
        //        if (m - avg < -1 * 180)
        //        {
        //            d = d + 360;
        //        }
        //        d = d * 3600;
        //        nnCound += d * d;
        //    }
        //    double 方差 = nnCound / count;
        //    return Math.Sqrt(方差);
        //}
        //
        public static Matrix T(this Matrix M)
        {
            return new Matrix(Matrix_Method.juzhengzhuanzhi(M.M));
        }
        //
        public static Matrix Inv(this Matrix M)
        {
            return new Matrix(Matrix_Method.juzhenqiuni_2(M.M));
            //var ran = Matrix_Method.InverseMatrix(M.M.TodoubleZ());
            // return new Matrix(ran.TodoubleZ());
        }
        //
    }
    /// <summary>
    /// 矩阵类
    /// </summary>
    public class Matrix
    {
        public double[,] M { get; set; }
        // 
        public double this[int row, int cln]
        {
            get
            {
                return M[row, cln];
            }
            set
            {
                M[row, cln] = value;
            }
        }
        //
        public Matrix(double[,] m)
        {
            this.M = m;
        }
        //

        public static Matrix operator *(Matrix B, Matrix P)
        {
            return new Matrix(Matrix_Method.juzhengxiangcheng(B.M, P.M));
        }
        //
        public static Matrix operator -(Matrix B, Matrix P)
        {
            return new Matrix(Matrix_Method.juzhenxiangjian(B.M, P.M));
        }
        //
        public static Matrix operator *(double B, Matrix P)
        {
            Matrix p = new Matrix(P.M.Clone() as double[,]);

            for (int i = 0; i < P.M.GetLength(0); i++)
            {
                for (int j = 0; j < P.M.GetLength(1); j++)
                {
                    p.M[i, j] = B * P.M[i, j];
                }
            }
            return p;
        }
        //
        public static Matrix operator +(Matrix B, Matrix P)
        {
            return new Matrix(Matrix_Method.juzhenxiangjia(B.M, P.M));
        }
        //
    }
    /// <summary>
    /// 矩阵计算类（旧版）
    /// </summary>
    public static partial class Matrix_Method
    {
        public static double[][] TodoubleZ(this double[,] d)
        {
            double[][] z = new double[d.GetLength(0)][];
            for (int i = 0; i < d.GetLength(0); i++)
            {
                z[i] = new double[d.GetLength(1)];
                //
                for (int j = 0; j < d.GetLength(1); j++)
                {
                    z[i][j] = d[i, j];
                }
            }
            return z;
        }
        //
        public static double[,] TodoubleZ(this double[][] d)
        {
            var r = d.GetLength(0);
            var c = d[0].GetLength(0);
            var z = new double[r, c];
            for (int i = 0; i < d.GetLength(0); i++)
            {
                for (int j = 0; j < d[i].GetLength(0); j++)
                {
                    z[i, j] = d[i][j];
                }
            }
            return z;
        }
        //
        /// <summary>
        /// 计算矩阵的秩
        /// </summary>
        /// <param name="matrix">矩阵</param>
        /// <returns></returns>
        public static int Rank(double[][] matrix)
        {
            //matrix为空则直接默认已经是最简形式
            if (matrix == null || matrix.Length == 0) return 0;

            //复制一个matrix到copy，之后因计算需要改动矩阵时并不改动matrix本身
            double[][] copy = new double[matrix.Length][];
            for (int i = 0; i < copy.Length; i++)
            {
                copy[i] = new double[matrix[i].Length];
            }
            for (int i = 0; i < matrix.Length; i++)
            {
                for (int j = 0; j < matrix[0].Length; j++)
                {
                    copy[i][j] = matrix[i][j];
                }
            }

            //先以最左侧非零项的位置进行行排序
            Operation1(copy);

            //循环化简矩阵
            while (!isFinished(copy))
            {
                Operation2(copy);
                Operation1(copy);
            }

            //过于趋近0的项，视作0，减小误差
            Operation3(copy);

            //行最简矩阵的秩即为所求
            return Operation4(matrix);
        }

        /// <summary>
        /// 判断矩阵是否变换到最简形式（非零行数达到最少）
        /// </summary>
        /// <param name="matrix"></param>
        /// <returns>true:</returns>
        private static bool isFinished(double[][] matrix)
        {
            //统计每行第一个非零元素的出现位置
            int[] counter = new int[matrix.Length];
            for (int i = 0; i < matrix.Length; i++)
            {
                for (int j = 0; j < matrix[i].Length; j++)
                {
                    if (matrix[i][j] == 0)
                    {
                        counter[i]++;
                    }
                    else break;
                }
            }

            //后面行的非零元素出现位置必须在前面行的后面，全零行除外
            for (int i = 1; i < counter.Length; i++)
            {
                if (counter[i] <= counter[i - 1] && counter[i] != matrix[0].Length)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 排序（按左侧最前非零位位置自上而下升序排列）
        /// </summary>
        /// <param name="matrix">矩阵</param>
        private static void Operation1(double[][] matrix)
        {
            //统计每行第一个非零元素的出现位置
            int[] counter = new int[matrix.Length];
            for (int i = 0; i < matrix.Length; i++)
            {
                for (int j = 0; j < matrix[i].Length; j++)
                {
                    if (matrix[i][j] == 0)
                    {
                        counter[i]++;
                    }
                    else break;
                }
            }

            //按每行非零元素的出现位置升序排列
            for (int i = 0; i < counter.Length; i++)
            {
                for (int j = i; j < counter.Length; j++)
                {
                    if (counter[i] > counter[j])
                    {
                        double[] dTemp = matrix[i];
                        matrix[i] = matrix[j];
                        matrix[j] = dTemp;
                    }
                }
            }
        }

        /// <summary>
        /// 行初等变换（左侧最前非零位位置最靠前的行，只保留一个）
        /// </summary>
        /// <param name="matrix">矩阵</param>
        private static void Operation2(double[][] matrix)
        {
            //统计每行第一个非零元素的出现位置
            int[] counter = new int[matrix.Length];
            for (int i = 0; i < matrix.Length; i++)
            {
                for (int j = 0; j < matrix[i].Length; j++)
                {
                    if (matrix[i][j] == 0)
                    {
                        counter[i]++;
                    }
                    else break;
                }
            }

            for (int i = 1; i < counter.Length; i++)
            {
                if (counter[i] == counter[i - 1] && counter[i] != matrix[0].Length)
                {
                    double a = matrix[i - 1][counter[i - 1]];
                    double b = matrix[i][counter[i]]; //counter[i]==counter[i-1]

                    matrix[i][counter[i]] = 0;
                    for (int j = counter[i] + 1; j < matrix[i].Length; j++)
                    {
                        double c = matrix[i - 1][j];
                        matrix[i][j] -= (c * b / a);
                    }

                    break;
                }
            }
        }

        /// <summary>
        /// 将和0非常接近的数字视为0
        /// </summary>
        /// <param name="matrix"></param>
        private static void Operation3(double[][] matrix)
        {
            for (int i = 0; i < matrix.Length; i++)
            {
                for (int j = 0; j < matrix[0].Length; j++)
                {
                    if (Math.Abs(matrix[i][j]) <= 0.00001)
                    {
                        matrix[i][j] = 0;
                    }
                }
            }
        }

        /// <summary>
        /// 计算行最简矩阵的秩
        /// </summary>
        /// <param name="matrix"></param>
        /// <returns></returns>
        private static int Operation4(double[][] matrix)
        {
            int rank = -1;
            bool isAllZero = true;
            for (int i = 0; i < matrix.Length; i++)
            {
                isAllZero = true;

                //查看当前行有没有0
                for (int j = 0; j < matrix[0].Length; j++)
                {
                    if (matrix[i][j] != 0)
                    {
                        isAllZero = false;
                        break;
                    }
                }

                //若第i行全为0，则矩阵的秩为i
                if (isAllZero)
                {
                    rank = i;
                    break;
                }
            }
            //满秩矩阵的情况
            if (rank == -1)
            {
                rank = matrix.Length;
            }

            return rank;
        }
    }

    /// <summary>
    /// 直线类：采用y=a*x+b或者x=c的形式表示直线。
    /// </summary>
    public class Line
    {
        /// <summary>
        /// y=ax+b中的a
        /// </summary>
        public double A { get; set; }
        /// <summary>
        /// y=ax+b中的b
        /// </summary>
        public double B { get; set; }
        /// <summary>
        /// x=c中的c
        /// </summary>
        public double C { get; set; }

        /// <summary>
        /// 构造函数（如果直线为y=ax+b形式，则C为Nan；如果直线为x=c形式，则A和B为Nan）
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="c"></param>
        public Line(double a, double b, double c)
        {
            if (!((double.IsNaN(a) && double.IsNaN(b) && !double.IsNaN(c)) || (!double.IsNaN(a) && !double.IsNaN(b) && double.IsNaN(c))))
                throw new ArgumentException("参数错误，无效的直线参数。");
            A = a;
            B = b;
            C = c;
        }

        /// <summary>
        /// 构造函数，由两个点确定直线
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        public Line(BaseXY p1, BaseXY p2)
        {
            if (p1.X == p2.X)
            {
                A = double.NaN;
                B = double.NaN;
                C = p1.X;
            }
            else
            {
                A = 1d * (p1.Y - p2.Y) / (p1.X - p2.X);
                B = p1.Y - A * p1.X;
                C = double.NaN;
            }
        }
        /// <summary>
        /// 生成一条随机的直线
        /// </summary>
        /// <returns></returns>
        public static Line GetRandomLine()
        {
            Random random = new Random();
            int a = random.Next(-10, 10);
            int b = random.Next(-10, 10);
            return new Line(a, b, double.NaN);
        }
        /// <summary>
        /// 获取点到直线的距离
        /// </summary>
        /// <param name="p">点</param>
        /// <returns>返回点到直线的距离；如果直线通过点，返回0。</returns>
        public double GetDistance(BaseXY p)
        {
            double d = 0d;
            if (double.IsNaN(C))
            {
                //y=ax+b相当于ax-y+b=0
                d = Math.Abs(1d * (A * p.X - p.Y + B) / Math.Sqrt(A * A + 1));
            }
            else
            {
                d = Math.Abs(C - p.X);
            }
            return d;
        }
        /// <summary>
        /// 根据x坐标，得到直线上点的y坐标 
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public double GetY(double x)
        {
            double y;
            if (double.IsNaN(C))
                y = A * x + B;
            else
                y = double.NaN;
            return y;
        }
        /// <summary>
        /// 返回直线方程
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            string formula = "";
            if (double.IsNaN(C))
                formula = string.Format("y={0}{1}", A != 0 ? string.Format("{0:F02}x", A) : "", B != 0 ? (B > 0 ? string.Format("+{0:F02}", B) : string.Format("{0:F02}", B)) : "");
            else
                formula = string.Format("x={0:F02}", C);
            return formula;
        }
        //
        /// <summary>
        /// 尝试获取直线
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static Line ransacLine0(List<BaseXY> l)
        {
            //用RANSAC方法获取最佳直线
            Line bestLine = null;           //最佳直线
            double bestD = double.MaxValue;
            double nowD = 0;
            for (int i = 0; i < l.Count - 1; i++)
            {
                for (int j = 0; j < l.Count; j++)
                {
                    if (j == i)
                    {
                        continue;
                    }
                    Line line = new Line(l[i], l[j]);
                    if (Math.Abs(line.A) > 10)
                    {
                        continue;
                    }
                    for (int k = 0; k < l.Count; k++)
                    {
                        if (k != i && k != j)
                        {
                            double dk = line.GetDistance(l[k]);
                            if (dk < 0.1)
                            {
                                nowD += dk;
                            }
                        }
                    }

                    if (nowD < bestD)
                    {
                        bestLine = line;
                        bestD = nowD;

                    }
                    nowD = 0;
                }
            }
            return bestLine;

        }
        //
        public static Line leastSquareFitting(List<BaseXY> l)
        {
            Line line = null;
            double sum_x = 0;
            double sum_y = 0;
            double sum_xx = 0;
            double sum_xy = 0;
            foreach (var b in l)
            {
                sum_x += b.X;
                sum_y += b.Y;
                sum_xx += b.X * b.X;
                sum_xy += b.X * b.X;
            }
            int n = l.Count;
            double divisor = n * sum_xx - sum_x * sum_x;
            if (divisor < Math.Pow(10, -6))
            {
                line.C = l[0].X;

            }
            line.A = (n * sum_xy - sum_x * sum_y) / divisor;
            line.B = (sum_xx * sum_y - sum_xy * sum_x) / divisor;
            return line;
        }
        //
        //public static Line leastSquareFitting1(List<BaseXY> l)
        //{
        //    Line line0 = leastSquareFitting(l);
        //    double meanError = 0;
        //    foreach (var b in l)
        //    {
        //        b.Error = line0.GetDistance(b);
        //        meanError += b.Error;
        //    }
        //    meanError = meanError / l.Count;
        //}
        //        
    }
    /// <summary>
    /// 矩阵计算类（新）
    /// </summary>
    public class MathMethor
    {
        public static void Polyval(double[,] x, double[] p, out double[] y)
        {
            int rows = x.GetLength(0);
            int cols = x.GetLength(1);
            y = new double[rows];
            if (cols != p.Length - 1) return;
            if (p.Length == 2)
            {
                for (int i = 0; i < rows; i++)
                {
                    y[i] = x[i, 0] * p[0] + p[1];
                }
            }
        }
        public static void Polyval(double[] x, double[,] p, out double[] y)
        {
            int rows = x.GetLength(0);
            y = new double[rows];
            if (p.Length > 2) return;
            if (p.Length == 2)
            {
                for (int i = 0; i < rows; i++)
                {
                    y[i] = x[i] * p[0, 0] + p[1, 0];
                }
            }

        }
        public static void Polyfit(double[] x, double[] y, int n, out double[,] P)
        {
            MathMatrix V = new MathMatrix()
            {
                Rows = x.Length,
                Cols = n + 1,
                A = new double[x.Length, n + 1],
            };
            for (int i = 0; i < V.Rows; i++)
            {
                for (int j = V.Cols - 1; j >= 0; j--)
                {
                    if (j == V.Cols - 1) V.A[i, j] = 1;
                    else
                    {
                        V.A[i, j] = x[i] * V.A[i, j + 1];
                    }
                }
            }
            bool isSuccess = HouseQR(V, out MathMatrix Q, out MathMatrix R);
            if (!isSuccess) { P = new double[1, 1]; return; }
            //p = R\(Q'*y); 
            MatrixInv(Q.A, out double[,] QN);
            double[,] Y = new double[y.Length, 1];
            for (int i = 0; i < y.Length; i++)
            {
                Y[i, 0] = y[i];
            }
            MatrixMulti(QN, Y, out double[,] QY);
            MatrixMidivide(R.A, QY, out P);
        }
        public static void PolyCut(double[] x, double[] y, double[] y1, out double[] X, out double[] Y)
        {
            double[] pdeta = new double[y1.Length];
            for (int k = 0; k < y1.Length; k++)
            {
                pdeta[k] = Math.Abs(y[k] - y1[k]);
            }
            GetMean(pdeta, out double mean);
            int[] Num = new int[y1.Length];
            int num = 0;
            for (int i = 0; i < y1.Length; i++)
            {
                if (pdeta[i] < 0.005 || pdeta[i] < mean * 1.8)
                {
                    Num[num] = i;
                    num++;
                }
            }
            X = new double[num];
            Y = new double[num];
            for (int i = 0; i < num; i++)
            {
                X[i] = x[Num[i]];
                Y[i] = y[Num[i]];
            }
        }
        public static void GetMean(double[] x, out double result)
        {
            result = 0;
            for (int i = 0; i < x.Length; i++)
            {
                result += x[i];
            }
            result /= x.Length;
        }
        public static void GetMean(double[,] x, out double[] result)
        {
            int row = x.GetLength(0);
            int col = x.GetLength(1);
            result = new double[col];
            for (int i = 0; i < col; i++)
            {
                for (int j = 0; j < row; j++)
                {
                    result[i] += x[j, i];
                }
                result[i] /= row;
            }
        }
        public static void Polyfit(double[,] x, double[] y, int n, out double[,] P)
        {
            MathMatrix V = new MathMatrix()
            {
                Rows = x.Length,
                Cols = n + 1,
                A = new double[x.Length, n + 1],
            };
            for (int i = 0; i < V.Rows; i++)
            {
                for (int j = V.Cols - 1; j >= 0; j--)
                {
                    if (j == V.Cols - 1) V.A[i, j] = 1;
                    else
                    {
                        V.A[i, j] = x[i, 0] * V.A[i, j + 1];
                    }
                }
            }
            bool isSuccess = HouseQR(V, out MathMatrix Q, out MathMatrix R);
            //p = R\(Q'*y); 
            if (!isSuccess) { P = new double[1, 1]; return; }
            MatrixInv(Q.A, out double[,] QN);
            double[,] Y = new double[y.Length, 1];
            for (int i = 0; i < y.Length; i++)
            {
                Y[i, 0] = y[i];
            }
            MatrixMulti(QN, Y, out double[,] QY);
            MatrixMidivide(R.A, QY, out P);
        }
        //矩阵QR简单分解
        public static bool HouseQR(MathMatrix mathMatrix, out MathMatrix Q, out MathMatrix R)
        {
            Q = new MathMatrix();
            R = new MathMatrix();
            if (mathMatrix.Rows == 0 || mathMatrix.Cols == 0) return false;
            double[,] A1 = mathMatrix.A;
            double[,] H1 = new double[mathMatrix.Rows, mathMatrix.Rows];
            H1.Initialize();
            for (int i = 0; i < mathMatrix.Rows; i++)
            {
                H1[i, i] = 1;
            }
            for (int i = 0; i < mathMatrix.Cols; i++)
            {
                double[,] H0 = new double[mathMatrix.Rows, mathMatrix.Rows];
                H0.Initialize();
                for (int j = 0; j < mathMatrix.Rows; j++)
                {
                    H0[j, j] = 1;
                }
                double s = 0;
                for (int k = i; k < mathMatrix.Rows; k++)
                {
                    s = s + A1[k, i] * A1[k, i];
                }
                s = Math.Sqrt(s);
                double[,] u = new double[mathMatrix.Rows, 1];//cols
                u.Initialize();
                if (A1[i, i] >= 0) { u[i, 0] = A1[i, i] + s; }
                else { u[i, 0] = A1[i, i] - s; }
                for (int k = i + 1; k < mathMatrix.Rows; k++)
                {
                    u[k, 0] = A1[k, i];
                }
                double du = 0;
                for (int k = i; k < mathMatrix.Rows; k++)
                {
                    du = du + u[k, 0] * u[k, 0];
                }
                for (int j = i; j < mathMatrix.Rows; j++)
                {
                    for (int k = i; k < mathMatrix.Rows; k++)
                    {
                        H0[j, k] = -2 * u[j, 0] * u[k, 0] / du;
                        if (j == k) { H0[j, k] = H0[j, k] + 1; }
                    }
                }
                MatrixMulti(H0, A1, out double[,] A2);
                if (A2.Length == 1) return false;
                A1 = A2;
                MatrixMulti(H1, H0, out H1);
                if (H1.Length == 1) return false;
            }
            if (mathMatrix.Rows <= mathMatrix.Cols)
            {
                Q.Rows = mathMatrix.Rows;
                Q.Cols = mathMatrix.Rows;
                R.Rows = mathMatrix.Rows;
                R.Cols = mathMatrix.Cols;
                Q.A = new double[Q.Rows, Q.Cols];
                R.A = new double[R.Rows, R.Cols];
                Q.A = H1;
                R.A = A1;
            }
            else
            {
                Q.Rows = mathMatrix.Rows;
                Q.Cols = mathMatrix.Cols;
                R.Rows = mathMatrix.Cols;
                R.Cols = mathMatrix.Cols;
                Q.A = new double[Q.Rows, Q.Cols];
                R.A = new double[R.Rows, R.Cols];
                for (int i = 0; i < Q.Rows; i++)
                {
                    for (int j = 0; j < Q.Cols; j++)
                    {
                        Q.A[i, j] = H1[i, j];
                        if (i < R.Rows) R.A[i, j] = A1[i, j];
                    }
                }
            }
            return true;
        }//QR分解
        public static MathMatrix Transposition(MathMatrix A)
        {
            MathMatrix mathMatrix = new MathMatrix();
            mathMatrix.Rows = A.Cols;
            mathMatrix.Cols = A.Rows;
            mathMatrix.A = new double[mathMatrix.Rows, mathMatrix.Cols];
            for (int i = 0; i < mathMatrix.Rows; i++)
            {
                for (int j = 0; j < mathMatrix.Cols; j++)
                {
                    mathMatrix.A[i, j] = A.A[j, i];
                }
            }
            return mathMatrix;
        }//矩阵转置
        public static double[,] Transposition(double[,] A)
        {
            int rows = A.GetLength(0);
            int cols = A.GetLength(1);
            double[,] B = new double[cols, rows];
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    B[j, i] = A[i, j];
                }
            }
            return B;
        }//矩阵转置
        public static MathMatrix MatrixMulti(MathMatrix A, MathMatrix B)
        {
            MathMatrix C = new MathMatrix();
            C.Rows = 0;
            if (A.Cols != B.Rows) return C;
            C.Rows = A.Rows;
            C.Cols = B.Cols;
            C.A = new double[C.Rows, C.Cols];
            for (int i = 0; i < C.Rows; i++)
            {
                for (int j = 0; j < C.Cols; j++)
                {
                    C.A[i, j] = 0;
                    for (int k = 0; k < A.Cols; k++)
                    {
                        C.A[i, j] = C.A[i, j] + A.A[i, k] * B.A[k, j];
                    }
                }
            }
            return C;
        }//矩阵相乘
        public static void MatrixMulti(double[,] A, double[,] B, out double[,] C)
        {
            //int a=A.GetUpperBound(1);
            int rows = A.GetLength(0);
            int rowcol = A.GetLength(1);
            int m = B.GetLength(0);
            int cols = B.GetLength(1);
            if (rowcol != m)
            {
                C = new double[1, 1];
                return;
            }
            C = new double[rows, cols];
            C.Initialize();
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    for (int k = 0; k < rowcol; k++)
                    {
                        C[i, j] = C[i, j] + A[i, k] * B[k, j];
                    }
                }
            }
        }//矩阵相乘
        public static void MatrixPlus(double[,] A, double[,] B, out double[,] C)
        {
            int rows = A.GetLength(0);
            int cols = A.GetLength(1);
            int m = B.GetLength(0);
            int n = B.GetLength(1);
            if (m != rows || n != cols)
            {
                C = new double[1, 1];
                return;
            }
            C = new double[rows, cols];
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    C[i, j] = A[i, j] + B[i, j];
                }
            }
        }//矩阵相加
        public static void MatrixMimus(double[,] A, double[,] B, out double[,] C)
        {
            int rows = A.GetLength(0);
            int cols = A.GetLength(1);
            int m = B.GetLength(0);
            int n = B.GetLength(1);
            if (m != rows || n != cols)
            {
                C = new double[1, 1];
                return;
            }
            C = new double[rows, cols];
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    C[i, j] = A[i, j] - B[i, j];
                }
            }
        }//矩阵相减
        public static void MatrixDeterminant(double[,] A, out double result)
        {

            int rows = A.GetLength(0);
            int deta = 1;
            double[,] Ax = new double[rows, rows];
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < rows; j++)
                {
                    Ax[i, j] = A[i, j];
                }
            }
            result = 0;
            if (rows == 1) { result = Ax[0, 0]; return; }
            for (int i = 0; i < rows - 1; i++)
            {
                if (Ax[i, i] == 0)
                {
                    for (int j = i + 1; j < rows; j++)
                    {
                        if (Ax[j, i] != 0)
                        {
                            for (int k = i; k < rows; k++)
                            {
                                Ax[i, k] = Ax[i, k] + Ax[j, k];
                            }
                            break;
                        }
                    }
                }
                if (Ax[i, i] == 0) return;
                for (int j = i + 1; j < rows; j++)
                {
                    double dd = Ax[j, i] / Ax[i, i];
                    for (int k = i; k < rows; k++)
                    {
                        Ax[j, k] = Ax[j, k] - Ax[i, k] * dd;
                    }
                }

            }
            result = deta;
            for (int i = 0; i < rows; i++)
            {
                result *= Ax[i, i];
            }
        }//行列式值
        public static void MatrixMimor(double[,] A, int m, int n, out double result)
        {
            int rows = A.GetLength(0);
            result = 0;
            if (m > rows || n > rows || rows == 1) return;
            double[,] B = new double[rows - 1, rows - 1];
            for (int i = 0; i < rows; i++)
            {
                if (i == m) continue;
                for (int j = 0; j < rows; j++)
                {
                    if (j == n) continue;
                    int i1 = i;
                    int j1 = j;
                    if (i > m) i1 = i1 - 1;
                    if (j > n) j1 = j1 - 1;
                    B[i1, j1] = A[i, j];
                }
            }
            MatrixDeterminant(B, out result);
        }
        public static void MatrixMidivide(double[,] A, double[,] B, out double[,] M)
        {
            int rows1 = A.GetLength(0);
            int cols1 = A.GetLength(1);
            int rows2 = B.GetLength(0);
            int cols2 = B.GetLength(1);
            M = new double[cols1, cols2];
            if (rows1 != rows2) return;
            for (int i = 0; i < cols2; i++)
            {
                double[] C = new double[rows1];
                for (int j = 0; j < rows1; j++)
                {
                    C[j] = B[j, i];
                }
                MatrixRCtran(A, C, out double[] D);
                for (int j = 0; j < cols1; j++)
                {
                    M[j, i] = D[j];
                }
            }

        }//A\B==>矩阵左除
        public static void MatrixInv(double[,] A, out double[,] B)
        {
            int rows = A.GetLength(0);
            int cols = A.GetLength(1);
            if (rows == cols) { MatrixSquareInv(A, out B); return; }
            double[,] AT = Transposition(A);

            if (rows > cols)
            {
                MatrixMulti(AT, A, out double[,] ATA);
                MatrixSquareInv(ATA, out double[,] ATAN);
                if (ATA.Length != ATAN.Length) { B = new double[1, 1]; B[0, 0] = 0; return; }
                B = new double[cols, rows];
                MatrixMulti(ATAN, AT, out B);
            }
            else
            {
                MatrixMulti(A, AT, out double[,] AAT);
                MatrixSquareInv(AAT, out double[,] AATN);
                if (AAT.Length != AATN.Length) { B = new double[1, 1]; B[0, 0] = 0; return; }
                MatrixMulti(AT, AATN, out B);
            }

        }//求逆(包括广义逆)
        public static void MatrixSquareInv(double[,] A, out double[,] B)
        {
            int rows = A.GetLength(0);
            B = new double[1, 1];
            B[0, 0] = 0;
            if (rows == 1) { B[0, 0] = 1 / A[0, 0]; return; }
            MatrixDeterminant(A, out double result);
            if (result == 0) return;
            B = new double[rows, rows];
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < rows; j++)
                {

                    MatrixMimor(A, i, j, out double a);
                    int deta = 1;
                    if ((i + j) % 2 == 1) deta = -deta;
                    B[i, j] = deta * a / result;
                }
            }
            B = Transposition(B);
        }//求方阵的逆
        public static void MatrixRCtran(double[,] A, double[] B, out double[] X)
        {
            int rows = A.GetLength(0);
            int cols = A.GetLength(1);
            X = new double[cols];
            if (rows < cols) return;
            double[,] AB = new double[rows, cols + 1];//增广矩阵[A,B]
            int[] Num = new int[cols];
            int record = 0;
            int num = 0;
            for (int i = 0; i < rows; i++)
            {
                Num[i] = i;
                for (int j = 0; j < cols; j++)
                {
                    AB[i, j] = A[i, j];
                }
                AB[i, cols] = B[i];
            }
            for (int i = 0; i < cols; i++)
            {
                for (int j = num; j < rows; j++)
                {
                    if (AB[Num[j], i] != 0)
                    {
                        record = Num[i];
                        Num[i] = Num[j];
                        Num[j] = record;
                        for (int ik = 0; ik < rows; ik++)
                        {
                            double ABIKI = AB[ik, num];
                            double ABIKJ = AB[Num[i], num];
                            if (ABIKI == 0) continue;
                            for (int jk = num; jk < cols + 1; jk++)
                            {
                                if (ik == Num[i])
                                {
                                    AB[ik, jk] = AB[ik, jk] / ABIKJ;
                                }
                                else
                                {
                                    AB[ik, jk] = AB[ik, jk] - ABIKI / ABIKJ * AB[Num[i], jk];
                                }
                            }
                        }
                        num++;
                        break;
                    }
                }
            }

            for (int i = 0; i < cols; i++)
            {
                for (int j = 0; j < rows; j++)
                {
                    if (Num[j] == i) { X[i] = AB[j, cols]; break; }
                }
            }

        }//解方程----->A m*n B m*1 增广矩阵[A,B]
    }
    /// <summary>
    /// 矩阵类（新）
    /// </summary>
    public class MathMatrix
    {
        public int Rows;
        public int Cols;
        public double[,] A;
        public bool isFool;//满秩与否
        public bool isSparse;//稀疏矩阵与否
        public bool isSingular;//奇异与否
    }
}
