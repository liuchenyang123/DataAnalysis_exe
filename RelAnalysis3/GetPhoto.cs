using OpenCvSharp;
using OpenCvSharp.CPlusPlus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RelAnalysis3
{
    /// <summary>
    /// 成图类
    /// </summary>
    public class GetPhoto
    {
        /// <summary>
        /// 补黑线
        /// </summary>
        /// <param name="A"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public byte[,] FillBlack(byte[,] A, int width, int height)
        {
            if (width == 1 || height == 1) return A;
            int fillDistance = 3;
            byte[,] G1 = new byte[height, width];
            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {

                    //G1[i, j] = A[i, j];
                    //continue;

                    if (A[i, j] == 0)
                    {

                        int i1 = i - fillDistance;
                        int i2 = i + fillDistance;
                        int j1 = j - fillDistance;
                        int j2 = j + fillDistance;
                        int s = 0;
                        int n = 0;
                        if (i1 < 0) i1 = 0;
                        if (i2 >= height) i2 = height - 1;
                        if (j1 < 0) j1 = 0;
                        if (j2 >= width) j2 = width - 1;
                        for (int k = i1; k < i2; k++)
                        {
                            for (int l = j1; l < j2; l++)
                            {
                                if (A[k, l] > 0)
                                {
                                    s = s + A[k, l];
                                    n++;
                                }
                            }
                        }
                        if (n == 0) n = 1;
                        byte b = (byte)(s / n);
                        G1[i, j] = b;
                    }
                    else
                    {
                        G1[i, j] = A[i, j];
                    }
                }
            }

            return G1;
        }
        /// <summary>
        /// 数组成图
        /// </summary>
        /// <param name="A"></param>
        /// <param name="height"></param>
        /// <param name="width"></param>
        /// <param name="path"></param>
        public void BornPic_later(byte[,] A, int height, int width, string path)
        {
            Mat Pic = GetMat(A, width, height);
            Cv2.ImWrite(path, Pic);
        }
        /// <summary>
        /// 数组成图
        /// </summary>
        /// <param name="A"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public Mat GetMat(byte[,] A, int width, int height)
        {
            int Height = (int)(height * 1.06);
            //int Height = (int)(height );
            byte[,] B = new byte[Height, width];
            for (int i = 0; i < Height; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    if (i < height) B[i, j] = A[i, j];
                    else B[i, j] = 255;
                }
            }
            Mat Pic = new Mat(new OpenCvSharp.CPlusPlus.Size(width, Height), MatType.CV_8UC1);
            Pic.SetArray(0, 0, B);
            Cv2.EqualizeHist(Pic, Pic);//均衡化处理
            return Pic;
        }

        /// <summary>
        /// 生成深度图
        /// </summary>
        /// <param name="savePath"></param>
        /// <param name="Lb"></param>
        /// <param name="hIntList"></param>
        /// <param name="wIntList"></param>
        /// <param name="maxD"></param>
        /// <param name="rmin"></param>
        static public void getP(string savePath, List<BaseXY> Lb, int[] hIntList, int[] wIntList,double maxD=0.09,double rmin=0)
        {
            int height = hIntList.Max() + 1;
            int width = wIntList.Max() + 1;
            if (height < 0 || width < 0) return;
            Mat grayImage = new Mat(new Size(width, height), MatType.CV_8UC1);
            //Mat grayImage2 = new Mat(new Size(width, height), MatType.CV_8UC1);
            //double rmin = Lb.Min(l => l.angle);

            for (int i = 0; i < Lb.Count; i++)
            {
                byte gray = 0;
                if (Lb[i].angle < rmin) gray = 0;
                else if (Lb[i].angle < rmin + maxD) gray = (byte)(255 * (Lb[i].angle - rmin) / maxD);
                else gray = 255;
                grayImage.Set<byte>(hIntList[i], wIntList[i], gray);//(height,width,rgb)
                //grayImage2.Set<byte>(hIntList[i], wIntList[i], (byte)Lb[i].Color);
            }
            Cv2.ImWrite(savePath, grayImage);
            grayImage.Dispose();
            //Cv2.ImWrite(savePath.Replace(".tiff", "_normal.tiff"), grayImage2);
        }//成识别环缝图
        
        /// <summary>
        /// 直接数组成图
        /// </summary>
        /// <param name="A">数组</param>
        /// <param name="height">影像图高度</param>
        /// <param name="width">影像图宽度</param>
        /// <param name="path">保存路径</param>
        public void BornPic_shendu(byte[,] A, int height, int width, string path)
        {
            Mat Pic = new Mat(new OpenCvSharp.CPlusPlus.Size(width, height), MatType.CV_8UC1);
            Pic.SetArray(0, 0, A);
            Cv2.ImWrite(path, Pic);
        }
        /// <summary>
        /// 错台成果图片输出
        /// </summary>
        /// <param name="savePath">保存路径</param>
        /// <param name="Lb"></param>
        /// <param name="Lf"></param>
        /// <param name="llf"></param>
        /// <param name="multi_n"></param>
        /// <param name="llf_luosuankong"></param>
        static public void GetP_cuotai_4(string savePath, List<BaseXY> Lb, List<Faro_point> Lf, List<List<Faro_point>> llf, int multi_n, List<List<Faro_point>>llf_luosuankong)
        {
            double hmax = Lb.Max(l => l.X);
            double hmin = Lb.Min(l => l.X);
            double wmax = Lb.Max(l => l.Y);
            double wmin = Lb.Min(l => l.Y);
            int height = (int)((hmax - hmin) * multi_n) + 1;
            int width = (int)((wmax - wmin) * multi_n) + 1;

            Mat grayImage = new Mat(new Size(width, height), MatType.CV_8UC3, Scalar.Black);

            double rmin = Lb.Min(l => l.angle);
            byte[,] A = new byte[height, width];
            for (int i = 0; i < Lb.Count; i++)
            {
                byte gray = (byte)Lb[i].Color;
                A[(int)((Lb[i].X - hmin) * multi_n), (int)((Lb[i].Y - wmin) * multi_n)] = gray;
            }
            GetPhoto getPhoto = new GetPhoto();
            A = getPhoto.FillBlack(A, width, height);
            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    Vec3b vec3B = new Vec3b() { Item0 = A[i, j], Item1 = A[i, j], Item2 = A[i, j] };
                    grayImage.Set<Vec3b>(i, j, vec3B);
                }
            }
            for (int i = 0; i < llf.Count; i++)
            {
                for (int j = 1; j < llf[i].Count; j++)
                {
                    int w1 = (int)((llf[i][j - 1].Y - hmin) * multi_n);
                    int h1 = (int)((llf[i][j - 1].X - wmin) * multi_n);
                    int w2 = (int)((llf[i][j].Y - hmin) * multi_n);
                    int h2 = (int)((llf[i][j].X - wmin) * multi_n);
                    if (w1 < 0) w1 = 0;
                    else if (w1 >= height) w1 = height - 1;
                    if (h1 < 0) h1 = 0;
                    else if (h1 >= width) h1 = width - 1;
                    if (w2 < 0) w2 = 0;
                    else if (w2 >= height) w2 = height - 1;
                    if (h2 < 0) h2 = 0;
                    else if (h2 >= width) h2 = width - 1;
                    grayImage.Line(h1, w1, h2, w2, Scalar.Green, 4);
                }

            }
            Mat grayImage2 = new Mat(new Size(height, width), MatType.CV_8UC1, Scalar.Black);

            Vec3b vec3B1 = new Vec3b() { Item0 = 0, Item1 = 0, Item2 = 255 };
            CvFont cvFont = new CvFont(FontFace.HersheyPlain, 1, 1, 1);
            foreach (var v in Lf)
            {
                int w1 = (int)((v.Y - hmin) * multi_n);
                int h1 = (int)((v.X - wmin) * multi_n);
                int w2 = (int)((v.Hs - hmin) * multi_n);
                int h2 = (int)((v.H - wmin) * multi_n);
                if (w1 < 0) w1 = 0;
                else if (w1 >= height) w1 = height - 1;
                if (h1 < 0) h1 = 0;
                else if (h1 >= width) h1 = width - 1;
                if (w2 < 0) w2 = 0;
                else if (w2 >= height) w2 = height - 1;
                if (h2 < 0) h2 = 0;
                else if (h2 >= width) h2 = width - 1;

                grayImage.Set<Vec3b>(w1, h1, vec3B1);
                grayImage.Set<Vec3b>(w2, h2, vec3B1);

                grayImage.Line(h1, w1, h2, w2, Scalar.Yellow);
                if (v.tag == 0)
                {
                    int xh12 = (h1 + h2) / 2 - multi_n / 5;
                    if (xh12 < 0) xh12 = 0;
                    if (Math.Abs(v.Z) > 0.006)
                    {

                        Cv2.PutText(grayImage, string.Format("{0:N2}", (v.Z * 1000)) + "mm",
           new Point() { Y = (w1 + w2) / 2, X = xh12 },
           OpenCvSharp.FontFace.HersheyPlain, 3, Scalar.Red, 2, OpenCvSharp.LineType.Link4);
                    }
                    else
                    {
                        Cv2.PutText(grayImage, string.Format("{0:N2}", (v.Z * 1000)) + "mm",
           new Point() { Y = (w1 + w2) / 2, X = xh12 },
           OpenCvSharp.FontFace.HersheyPlain, 3, Scalar.Blue, 2, OpenCvSharp.LineType.Link4);
                    }

                }
                else
                {
                    int xw12 = (w1 + w2) / 2 - multi_n / 5;
                    if (Math.Abs(v.Z) > 0.005)
                    {
                        Cv2.PutText(grayImage2, string.Format("{0:N2}", (v.Z * 1000)) + "mm",
           new Point() { X = xw12, Y = width - 1 - Math.Min(h1, h2) },
           cvFont.FontFace, 3, new Scalar(2), 2, OpenCvSharp.LineType.Link4);
                        //Cv2.PutText(grayImage, string.Format("{0:N2}", (v.Z * 1000)) + "mm",
                        //new Point() { Y = (w1 + w2) / 2, X = (h1 + h2) / 2 },
                        //cvFont.FontFace, 1, Scalar.Red, 1, OpenCvSharp.LineType.Link8);
                    }
                    else
                    {
                        Cv2.PutText(grayImage2, string.Format("{0:N2}", (v.Z * 1000)) + "mm",
           new Point() { X = xw12, Y = width - 1 - Math.Min(h1, h2) },
           cvFont.FontFace, 3, new Scalar(1), 2, OpenCvSharp.LineType.Link4);
                        //             Cv2.PutText(grayImage, string.Format("{0:N2}", (v.Z * 1000)) + "mm",
                        //new Point() { Y = (w1 + w2) / 2, X = (h1 + h2) / 2 },
                        //cvFont.FontFace, 1, Scalar.Green, 1, OpenCvSharp.LineType.Link8);
                    }
                }
            }
            Vec3b vec3B2 = new Vec3b() { Item0 = 0, Item1 = 0, Item2 = 255 };
            Vec3b vec3B3 = new Vec3b() { Item0 = 255, Item1 = 0, Item2 = 0 };
            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    byte g0 = grayImage2.Get<byte>(width - 1 - j, i);
                    if (g0 == 2)
                    {
                        grayImage.Set<Vec3b>(i, j, vec3B2);
                    }
                    else if (g0 == 1)
                    {
                        grayImage.Set<Vec3b>(i, j, vec3B3);
                    }
                }
            }
            for (int i = 0; i < llf_luosuankong.Count; i++)
            {
                for (int j = 1; j < llf_luosuankong[i].Count; j++)
                {
                    int w1 = (int)((llf_luosuankong[i][j - 1].Y - hmin) * multi_n);
                    int h1 = (int)((llf_luosuankong[i][j - 1].X - wmin) * multi_n);
                    int w2 = (int)((llf_luosuankong[i][j].Y - hmin) * multi_n);
                    int h2 = (int)((llf_luosuankong[i][j].X - wmin) * multi_n);
                    if (w1 < 0) w1 = 0;
                    else if (w1 >= height) w1 = height - 1;
                    if (h1 < 0) h1 = 0;
                    else if (h1 >= width) h1 = width - 1;
                    if (w2 < 0) w2 = 0;
                    else if (w2 >= height) w2 = height - 1;
                    if (h2 < 0) h2 = 0;
                    else if (h2 >= width) h2 = width - 1;
                    grayImage.Line(h1, w1, h2, w2, Scalar.Blue, 1);
                    if (j == llf_luosuankong[i].Count - 1) {
                        w1 = (int)((llf_luosuankong[i][0].Y - hmin) * multi_n);
                        h1 = (int)((llf_luosuankong[i][0].X - wmin) * multi_n);
                        if (w1 < 0) w1 = 0;
                        else if (w1 >= height) w1 = height - 1;
                        if (h1 < 0) h1 = 0;
                        else if (h1 >= width) h1 = width - 1;
                        grayImage.Line(h1, w1, h2, w2, Scalar.Blue, 1);
                    }
                }

            }

            Cv2.ImWrite(savePath, grayImage);
        }
    }

}
