using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RelAnalysis3
{
    /// <summary>
    /// 断面分析类
    /// </summary>
    class SectionAnalysis
    {
        //获取断面+断面图
        public static void Section_Get_0201(List<Faro_point> lp, string WritePath, double deta_row)
        {
            if (deta_row > 0.01) deta_row = 0.005;
            double cut_deta = Math.Max(deta_row, 0.0006);

            if (cut_deta <= 0) cut_deta = 0.005;
            List<BaseXY> original_2D = new List<BaseXY>();
            Ellipse ep1 = new Ellipse();
            if (lp.Count < 10) return;
            lp = lp.OrderBy(l => l.Y).ToList();
            double mid = (lp[0].Y + lp[lp.Count - 1].Y) / 2;
            List<BaseXY> original_2d = new List<BaseXY>();
            for (int i = 0; i < lp.Count; i++)
            {
                if (lp[i].Y < mid - cut_deta) continue;
                else if (lp[i].Y > mid + cut_deta) break;
                BaseXY baseXy = new BaseXY()
                {
                    X = lp[i].X,
                    Y = lp[i].Z,
                    Error = lp[i].H,
                };
                original_2D.Add(baseXy);
                original_2d.Add(baseXy);
            }
            Text_IO.WriteToTxt(original_2d, WritePath);
        }

        public static void DrawSection(List<Faro_point> limitdata, List<Faro_point> secdata, List<Faro_point> LFP, string path, string mark_str = "")
        {
            if (secdata.Count == 0) return;
            double xmin = Math.Min(limitdata.Min(l => l.X) - 0.7, secdata.Min(l => l.X) - 0.3);
            double ymin = Math.Min(limitdata.Min(l => l.Y) - 0.7, secdata.Min(l => l.Z) - 0.3);
            double xmax = Math.Max(limitdata.Max(l => l.X) + 0.7, secdata.Max(l => l.X) + 0.3);
            double ymax = Math.Max(limitdata.Max(l => l.Y) + 0.7, secdata.Max(l => l.Z) + 0.3);
            double xmid = (xmin + xmax) / 2;
            double ymid = (ymin + ymax) / 2;
            List<PointDraw> lp = new List<PointDraw>();
            List<PointDraw> lp2 = new List<PointDraw>();
            PointDraw minLimit = new PointDraw() { x = xmin, y = ymin, };
            PointDraw maxLimit = new PointDraw() { x = xmax, y = ymax, };
            DrawUnint drawUnint = new DrawUnint();
            Font font = new Font("宋体", 60);
            Font font2 = new Font("宋体", 30);
            drawUnint.InitiImage(minLimit, maxLimit);
            foreach (var t in secdata)
            {
                PointDraw p2 = new PointDraw() { x = t.X, y = t.Z, color = Brushes.Black, };
                lp.Add(p2);
            }
            drawUnint.DrawPoints(lp, 3);
            limitdata = limitdata.OrderBy(l => l.Color).ToList();

            //----mark
            double h_down = limitdata[0].Y - 0.025;
            double set1_x = limitdata[1].X;
            double set1_y = limitdata[1].Y;

            //----mark
            for (int i = 1; i < limitdata.Count; i++)
            {
                PointDraw p1 = new PointDraw() { x = limitdata[i].X, y = limitdata[i].Y, };
                PointDraw p2 = new PointDraw() { x = limitdata[i - 1].X, y = limitdata[i - 1].Y, };
                drawUnint.DrawPath(p1, p2, Color.Blue, "实线");
            }
            foreach (var v in LFP)
            {
                PointDraw p1 = new PointDraw() { x = v.X, y = v.Y, };
                PointDraw p2 = new PointDraw() { x = v.xe, y = v.H, };

                if (Math.Abs(v.R) > 0.8) continue;

                if (v.tag == 0)
                {
                    drawUnint.DrawPath(p1, p2, Color.Red, "虚线");
                    drawUnint.DrawString(((int)Math.Floor(v.R * 1000)).ToString() + "mm", font2, Brushes.Green, (v.X + v.xe) / 2, (v.Y + v.H) / 2, 0);
                }
                else
                {
                    if (Math.Abs(v.R) > 0.02)
                    {
                        drawUnint.DrawPath(p1, p2, Color.Green, "虚线");
                        drawUnint.DrawString(((int)Math.Floor(v.R * 1000)).ToString() + "mm", font2, Brushes.Green, (v.X + v.xe) / 2, (v.Y + v.H) / 2, 0);
                    }
                    else
                    {
                        drawUnint.DrawPath(p1, p2, Color.Orange, "虚线");
                        drawUnint.DrawString(((int)Math.Floor(v.R * 1000)).ToString() + "mm", font2, Brushes.Red, (v.X + v.xe) / 2, (v.Y + v.H) / 2, 0);
                    }
                }
            }
            drawUnint.DrawString(mark_str, font, Brushes.Red, xmid - 0.1, ymid, 0);
            drawUnint.Save_photo(path);
        }


        /// <summary>
        /// 生成限界图函数
        /// </summary>
        /// <param name="limitdata"></param>
        /// <param name="secdata"></param>
        /// <param name="LFP"></param>
        /// <param name="path"></param>
        /// <param name="mark_str"></param>
        public static void DrawSection_limit(List<Faro_point> limitdata, Faro_point[] secdata, List<Faro_point> LFP, string path, string mark_str = "")
        {
            if (secdata.Length == 0 ) return;
            double xmin = Math.Min(limitdata.Min(l => l.X) - 0.7, secdata.Min(l => l.X) - 0.3);
            double ymin = Math.Min(limitdata.Min(l => l.Y) - 0.7, secdata.Min(l => l.Z) - 0.3);
            double xmax = Math.Max(limitdata.Max(l => l.X) + 0.7, secdata.Max(l => l.X) + 0.3);
            double ymax = Math.Max(limitdata.Max(l => l.Y) + 0.7, secdata.Max(l => l.Z) + 0.3);
            double xmid = (xmin + xmax) / 2;
            double ymid = (ymin + ymax) / 2;
            List<PointDraw> lp = new List<PointDraw>();
            List<PointDraw> lp2 = new List<PointDraw>();
            PointDraw minLimit = new PointDraw() { x = xmin, y = ymin, };
            PointDraw maxLimit = new PointDraw() { x = xmax, y = ymax, };
            DrawUnint drawUnint = new DrawUnint();
            Font font = new Font("宋体", 60);
            Font font2 = new Font("宋体", 30);
            drawUnint.InitiImage(minLimit, maxLimit);
            foreach (var t in secdata)
            {
                PointDraw p2 = new PointDraw() { x = t.X, y = t.Z, color = Brushes.Black, };
                lp.Add(p2);
            }
            drawUnint.DrawPoints(lp, 3);
            limitdata = limitdata.OrderBy(l => l.Color).ToList();

            //----mark
            double h_down = limitdata[0].Y - 0.025;
            double set1_x = limitdata[1].X;
            double set1_y = limitdata[1].Y;

            //----mark
            for (int i = 1; i < limitdata.Count; i++)
            {
                PointDraw p1 = new PointDraw() { x = limitdata[i].X, y = limitdata[i].Y, };
                PointDraw p2 = new PointDraw() { x = limitdata[i - 1].X, y = limitdata[i - 1].Y, };
                drawUnint.DrawPath(p1, p2, Color.Blue, "实线");
            }
            foreach (var v in LFP)
            {
                PointDraw p1 = new PointDraw() { x = v.X, y = v.Y, };
                PointDraw p2 = new PointDraw() { x = v.xe, y = v.H, };

                if (Math.Abs(v.R) > 0.8) continue;

                if (v.tag == 0)
                {
                    drawUnint.DrawPath(p1, p2, Color.Red, "虚线");
                    drawUnint.DrawString(((int)Math.Floor(v.R * 1000)).ToString() + "mm", font2, Brushes.Green, (v.X + v.xe) / 2, (v.Y + v.H) / 2, 0);
                }
                else
                {
                    if (Math.Abs(v.R) > 0.02)
                    {
                        drawUnint.DrawPath(p1, p2, Color.Green, "虚线");
                        drawUnint.DrawString(((int)Math.Floor(v.R * 1000)).ToString() + "mm", font2, Brushes.Green, (v.X + v.xe) / 2, (v.Y + v.H) / 2, 0);
                    }
                    else
                    {
                        drawUnint.DrawPath(p1, p2, Color.Orange, "虚线");
                        drawUnint.DrawString(((int)Math.Floor(v.R * 1000)).ToString() + "mm", font2, Brushes.Red, (v.X + v.xe) / 2, (v.Y + v.H) / 2, 0);
                    }
                }
            }
            drawUnint.DrawString(mark_str, font, Brushes.Red, xmid - 0.1, ymid, 0);
            drawUnint.Save_photo(path);
        }
    }
    /// <summary>
    /// 画图函数
    /// </summary>
    public class DrawUnint
    {
        Bitmap bitmap;
        Graphics g;
        double dCenterX;
        double dCenterY;
        double scale;
        public void Save_photo(string path)
        {
            bitmap.Save(path);
        }
        public void InitiImage(List<double> lx, List<double> ly, double eCenterX, double eCenterY, double a, double b, double angle)
        {
            double Xmax = lx.Max();
            double Xmin = lx.Min();
            double Ymax = ly.Max();
            double Ymin = ly.Min();
            dCenterX = (Xmax + Xmin) / 2;
            dCenterY = (Ymax + Ymin) / 2;
            double X_deta = Xmax - Xmin;
            double Y_deta = Ymax - Ymin;
            if (X_deta == 0) X_deta = 100;//X方向只有一个值
            if (Y_deta == 0) Y_deta = 100;//Y方向只有一个值

            int bWidth = 3000;//默认3000
            scale = bWidth / X_deta * 0.8;
            int bHeight = (int)(Y_deta * scale / 0.8);
            bitmap = new Bitmap(bWidth, bHeight);
            g = Graphics.FromImage(bitmap);
            g.FillRectangle(Brushes.White, new Rectangle(0, 0, bWidth, bHeight));
            g.TranslateTransform(bWidth / 2, bHeight / 2);
            //画断面，黑色
            for (int i = 0; i < lx.Count; i++)
            {
                g.FillEllipse(Brushes.Black, (float)((lx[i] - dCenterX) * scale), (float)((dCenterY - ly[i]) * scale), 5, 5);
            }
            //画椭圆，绿色
            eCenterX = (eCenterX - dCenterX) * scale;
            eCenterY = (dCenterY - eCenterY) * scale;
            double angle1 = angle + Math.Atan(b / a) * 180 / Math.PI;

            b = b * scale;
            a = a * scale;
            float dx = (float)(eCenterX - Math.Cos(angle * Math.PI / 180) * a - Math.Sin(angle * Math.PI / 180) * b);
            //float dy = (float)(eCenterY - Math.Sin(angle * Math.PI / 180) * a + Math.Cos(angle * Math.PI / 180) * b);
            float dy = (float)(eCenterY + Math.Sin(angle * Math.PI / 180) * a - Math.Cos(angle * Math.PI / 180) * b);

            //double c = Math.Sqrt(a * a + b * b);
            //float dx = (float)((eCenterX - Math.Cos(angle1) * c) * scale);
            //float dy = (float)((eCenterY - Math.Sin(angle1) * c) * scale);
            //double c = Math.Sqrt(a * a + b * b) ;
            //float dx = (float)(eCenterX - Math.Cos(angle1*Math.PI/180) * c);
            //float dy = (float)(eCenterY - Math.Sin(angle1 * Math.PI / 180) * c);


            g.TranslateTransform(dx, dy);
            g.RotateTransform((float)-angle);
            using (Pen pen = new Pen(Color.Green, 3))
            {
                pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;//虚线
                g.DrawEllipse(pen, 0, 0, (float)(a * 2), (float)(b * 2));
            }
            g.RotateTransform((float)angle);
            g.TranslateTransform(-dx, -dy);
        }
        public void InitiImage(PointDraw minLimit, PointDraw maxLimit)
        {
            double Xmin = Math.Min(minLimit.x, maxLimit.x);
            double Xmax = Math.Max(minLimit.x, maxLimit.x);
            double Ymin = Math.Min(minLimit.y, maxLimit.y);
            double Ymax = Math.Max(minLimit.y, maxLimit.y);
            dCenterX = (Xmax + Xmin) / 2;
            dCenterY = (Ymax + Ymin) / 2;
            double X_deta = Xmax - Xmin;
            double Y_deta = Ymax - Ymin;
            if (X_deta == 0) X_deta = 100;//X方向只有一个值
            if (Y_deta == 0) Y_deta = 100;//Y方向只有一个值
            int bWidth = 5000;
            int bHeight = 4000;
            if (X_deta * 2 < Y_deta * 3)
            {
                scale = bHeight / Y_deta;
            }
            else
            {
                scale = bWidth / X_deta;
            }

            //scale = bWidth / X_deta * 0.8;
            //int bHeight = (int)(Y_deta * scale / 0.8);
            bitmap = new Bitmap(bWidth, bHeight);
            g = Graphics.FromImage(bitmap);
            g.FillRectangle(Brushes.White, new Rectangle(0, 0, bWidth, bHeight));
            g.TranslateTransform(bWidth / 2, bHeight / 2);

        }
        public void DrawString(string s, Font font, Brush brush, double x, double y, float angle)
        {
            //font = new Font("宋体",20);
            SizeF sizef = g.MeasureString(s, font);
            float dx = (float)((x - dCenterX) * scale);
            float dy = (float)((dCenterY - y) * scale);
            g.TranslateTransform(dx, dy);
            g.RotateTransform(angle);
            g.DrawString(s, font, brush, 0, 0);
            g.RotateTransform(-angle);
            g.TranslateTransform(-dx, -dy);
        }
        public void DrawPath(double x1, double y1, double x2, double y2, Color color, System.Drawing.Drawing2D.DashStyle linestye = System.Drawing.Drawing2D.DashStyle.Solid)
        {
            using (Pen pen = new Pen(color, 4))
            {
                //solid 实线
                //Dash 虚线
                pen.DashStyle = linestye;//虚线
                g.DrawLine(pen, (float)((x1 - dCenterX) * scale), (float)((dCenterY - y1) * scale), (float)((x2 - dCenterX) * scale), (float)((dCenterY - y2) * scale));
            }
        }

        public void DrawPoint(PointDraw p, float width = 5)
        {
            g.FillEllipse(p.color, (float)((p.x - dCenterX) * scale), (float)((dCenterY - p.y) * scale), width, width);
        }
        public void DrawPoints(List<PointDraw> lp, float width = 20)
        {
            foreach (var p in lp)
            {
                g.FillEllipse(p.color, (float)((p.x - dCenterX) * scale), (float)((dCenterY - p.y) * scale), width, width);
            }
        }
        public void DrawPoints(List<PointDraw> lp)
        {
            foreach (var p in lp)
            {
                g.FillEllipse(p.color, (float)((p.x - dCenterX) * scale), (float)((dCenterY - p.y) * scale), 5, 5);
            }
        }
        public void DrawPath(PointDraw p1, PointDraw p2, Color color, string pentype = "虚线", float width = 6)
        {
            using (Pen pen = new Pen(color, width))
            {
                //solid 实线
                //Dash 虚线
                if (pentype == "虚线") { pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash; }//虚线 
                else { pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Solid; }
                pen.Color = color;
                g.DrawLine(pen, (float)((p1.x - dCenterX) * scale), (float)((dCenterY - p1.y) * scale), (float)((p2.x - dCenterX) * scale), (float)((dCenterY - p2.y) * scale));
            }
        }
        public void DrawCircle(PointDraw circleCenter, double R, double angle_QD, double angle_ZD, Color color, float width = 10)
        {
            Pen pe = new Pen(color, width);
            g.DrawArc(pe, (float)((circleCenter.x - dCenterX) * scale), (float)((dCenterY - circleCenter.y) * scale), 2 * (float)(R * scale), 2 * (float)(R * scale), (float)angle_QD, (float)angle_ZD);

        }

    }
    public class PointDraw
    {
        public double x;
        public double y;
        public double z;
        public Brush color;
    }
    public class LineDraw
    {
        public double x1;
        public double y1;
        public double x2;
        public double y2;
        public Brush color;
    }
    public class StrDraw
    {
        public double x;
        public double y;
        public Brush color;
        public string str;
    }
    public class SectionDraw
    {
        Bitmap bitmap;
        Graphics g;
        double dCenterX;
        double dCenterY;
        double scale;
        public void Save_photo(string path)
        {
            bitmap.Save(path);
        }

        public void InitiImage(List<double> lx, List<double> ly, double eCenterX, double eCenterY, double a, double b, double angle)
        {
            double Xmax = lx.Max();
            double Xmin = lx.Min();
            double Ymax = ly.Max();
            double Ymin = ly.Min();
            dCenterX = (Xmax + Xmin) / 2;
            dCenterY = (Ymax + Ymin) / 2;
            double X_deta = Xmax - Xmin;
            double Y_deta = Ymax - Ymin;
            if (X_deta == 0) X_deta = 100;//X方向只有一个值
            if (Y_deta == 0) Y_deta = 100;//Y方向只有一个值

            int bWidth = 3000;//默认3000
            scale = bWidth / X_deta * 0.8;
            int bHeight = (int)(Y_deta * scale / 0.8);
            bitmap = new Bitmap(bWidth, bHeight);
            g = Graphics.FromImage(bitmap);
            g.FillRectangle(Brushes.White, new Rectangle(0, 0, bWidth, bHeight));
            g.TranslateTransform(bWidth / 2, bHeight / 2);
            //画断面，黑色
            for (int i = 0; i < lx.Count; i++)
            {
                g.FillEllipse(Brushes.Black, (float)((lx[i] - dCenterX) * scale), (float)((dCenterY - ly[i]) * scale), 5, 5);
            }
            //画椭圆，绿色
            eCenterX = (eCenterX - dCenterX) * scale;
            eCenterY = (dCenterY - eCenterY) * scale;
            double angle1 = angle + Math.Atan(b / a) * 180 / Math.PI;

            b = b * scale;
            a = a * scale;
            float dx = (float)(eCenterX - Math.Cos(angle * Math.PI / 180) * a - Math.Sin(angle * Math.PI / 180) * b);
            //float dy = (float)(eCenterY - Math.Sin(angle * Math.PI / 180) * a + Math.Cos(angle * Math.PI / 180) * b);
            float dy = (float)(eCenterY + Math.Sin(angle * Math.PI / 180) * a - Math.Cos(angle * Math.PI / 180) * b);

            //double c = Math.Sqrt(a * a + b * b);
            //float dx = (float)((eCenterX - Math.Cos(angle1) * c) * scale);
            //float dy = (float)((eCenterY - Math.Sin(angle1) * c) * scale);
            //double c = Math.Sqrt(a * a + b * b) ;
            //float dx = (float)(eCenterX - Math.Cos(angle1*Math.PI/180) * c);
            //float dy = (float)(eCenterY - Math.Sin(angle1 * Math.PI / 180) * c);


            g.TranslateTransform(dx, dy);
            g.RotateTransform((float)-angle);
            using (Pen pen = new Pen(Color.Green, 3))
            {
                pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;//虚线
                g.DrawEllipse(pen, 0, 0, (float)(a * 2), (float)(b * 2));
            }
            g.RotateTransform((float)angle);
            g.TranslateTransform(-dx, -dy);
        }
        public void InitiImage2(List<double> lx, List<double> ly, double eCenterX, double eCenterY, double a, double b, double angle)
        {
            if (Math.Abs(eCenterX) < 0.5) dCenterX = eCenterX;
            else dCenterX = 0;
            if (Math.Abs(eCenterY) < 0.5) dCenterY = eCenterY - 0.2;
            else dCenterY = 1.4;
            double X_deta = 6.366;
            double Y_deta = 5.15;
            int bWidth = 3000;//默认3000
            scale = bWidth / X_deta * 0.8;
            int bHeight = (int)(Y_deta * scale / 0.8);
            bitmap = new Bitmap(bWidth, bHeight);
            g = Graphics.FromImage(bitmap);
            g.FillRectangle(Brushes.White, new Rectangle(0, 0, bWidth, bHeight));
            g.TranslateTransform(bWidth / 2, bHeight / 2);
            //画断面，黑色
            for (int i = 0; i < lx.Count; i++)
            {
                g.FillEllipse(Brushes.Black, (float)((lx[i] - dCenterX) * scale), (float)((dCenterY - ly[i]) * scale), 5, 5);
            }
            //画椭圆，绿色
            eCenterX = (eCenterX - dCenterX) * scale;
            eCenterY = (dCenterY - eCenterY) * scale;
            double angle1 = angle + Math.Atan(b / a) * 180 / Math.PI;

            b = b * scale;
            a = a * scale;
            float dx = (float)(eCenterX - Math.Cos(angle * Math.PI / 180) * a - Math.Sin(angle * Math.PI / 180) * b);
            //float dy = (float)(eCenterY - Math.Sin(angle * Math.PI / 180) * a + Math.Cos(angle * Math.PI / 180) * b);
            float dy = (float)(eCenterY + Math.Sin(angle * Math.PI / 180) * a - Math.Cos(angle * Math.PI / 180) * b);

            //double c = Math.Sqrt(a * a + b * b);
            //float dx = (float)((eCenterX - Math.Cos(angle1) * c) * scale);
            //float dy = (float)((eCenterY - Math.Sin(angle1) * c) * scale);
            //double c = Math.Sqrt(a * a + b * b) ;
            //float dx = (float)(eCenterX - Math.Cos(angle1*Math.PI/180) * c);
            //float dy = (float)(eCenterY - Math.Sin(angle1 * Math.PI / 180) * c);

            g.TranslateTransform(dx, dy);
            g.RotateTransform((float)-angle);
            using (Pen pen = new Pen(Color.Green, 3))
            {
                pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;//虚线
                g.DrawEllipse(pen, 0, 0, (float)(a * 2), (float)(b * 2));
            }
            g.RotateTransform((float)angle);
            g.TranslateTransform(-dx, -dy);
        }
        public void InitiImage22(List<double> lx, List<double> ly, double eCenterX, double eCenterY)
        {
            double Xmax = lx.Max();
            double Xmin = lx.Min();
            double Ymax = ly.Max();
            double Ymin = ly.Min();
            dCenterX = (Xmax + Xmin) / 2;
            dCenterY = (Ymax + Ymin) / 2;
            double X_deta = Xmax - Xmin;
            double Y_deta = Ymax - Ymin;
            if (X_deta == 0) X_deta = 100;//X方向只有一个值
            if (Y_deta == 0) Y_deta = 100;//Y方向只有一个值

            int bWidth = 3000;//默认3000
            scale = bWidth / X_deta * 0.7;
            int bHeight = (int)(Y_deta * scale / 0.7);
            bitmap = new Bitmap(bWidth, bHeight);
            g = Graphics.FromImage(bitmap);
            g.FillRectangle(Brushes.White, new Rectangle(0, 0, bWidth, bHeight));
            g.TranslateTransform(bWidth / 2, bHeight / 2);

            //画断面，黑色
            for (int i = 0; i < lx.Count; i++)
            {
                g.FillEllipse(Brushes.Black, (float)((lx[i] - dCenterX) * scale), (float)((dCenterY - ly[i]) * scale), 5, 5);
            }

        }
        public void DrawString(string s, Font font, Brush brush, double x, double y, float angle)
        {
            //font = new Font("宋体",20);
            SizeF sizef = g.MeasureString(s, font);
            float dx = (float)((x - dCenterX) * scale);
            float dy = (float)((dCenterY - y) * scale);
            g.TranslateTransform(dx, dy);
            g.RotateTransform(angle);
            g.DrawString(s, font, brush, 0, 0);
            g.RotateTransform(-angle);
            g.TranslateTransform(-dx, -dy);
        }
        public void DrawPath(double x1, double y1, double x2, double y2, Color color, System.Drawing.Drawing2D.DashStyle linestye = System.Drawing.Drawing2D.DashStyle.Solid)
        {
            using (Pen pen = new Pen(color, 4))
            {
                //solid 实线
                //Dash 虚线
                pen.DashStyle = linestye;//虚线
                g.DrawLine(pen, (float)((x1 - dCenterX) * scale), (float)((dCenterY - y1) * scale), (float)((x2 - dCenterX) * scale), (float)((dCenterY - y2) * scale));
            }
        }
    }
}
