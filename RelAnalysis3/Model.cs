using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace RelAnalysis3
{
    /// <summary>
    /// 扫描任务参数
    /// </summary>
    public class ScanTask
    {
        public int StartNum = 0;
        public int LineNum = 1;
        public double Slide_meter = 1.5;
        public string TaskRoad { get; set; }
        public string ProjectName { get; set; }//项目文件名
        public string ScanBaseName { get; set; }//扫描文件名
        public double ScannerHeight { get; set; }//扫描仪高度
        public double SegmentRadius = 2.7;//管片半径
        public double SegmentWidth = 1.5;//管片宽度
        public double ScanAccuary = 2;//扫描精度
        public bool IsNeedShowReal = true;//是否实时显示
        public bool IsStartZeroMileage = false;//里程是否从零开始
        public bool IsBuildPhoto = true;//是否生成影像图
        public bool IsHighDefinition = true;//是否高清显示
        public bool IsBuildSection = false;//是否生成断面
        public bool IsDoLimit = false;//是否做限界分析
        public bool IsAutoShutdown = false;//是否自动关机
        public long StartTime = 0;//开始扫描时间
        public double Speed = 800;//速度 单位m/h
        public string Mode = "Mode0";//采集模式
        public InitializeParam initializeParam { get; set; }//扫描参数
    }
    /// <summary>
    /// 扫描仪初始化参数
    /// </summary>
    public class InitializeParam
    {
        /// <summary>
        /// 扫描文件序号
        /// </summary>
        private int scanFileNumber = 1;
        public int ScanFileNumber
        {
            get { return scanFileNumber; }
            set { scanFileNumber = value; }
        }

        /// <summary>
        /// 扫描基本名称
        /// </summary>
        private string scanBaseName;
        public string ScanBaseName
        {
            get { return scanBaseName; }
            set { scanBaseName = value; }
        }

        /// <summary>
        /// 远程存储路径
        /// </summary>
        private string remoteScanStoragePath;
        public string RemoteScanStoragePath
        {
            get { return remoteScanStoragePath; }
            set { remoteScanStoragePath = value; }
        }

        /// <summary>
        /// 分辨率
        /// </summary>
        private int resolution = 1;
        public int Resolution
        {
            get { return resolution; }
            set { resolution = value; }
        }
        /// <summary>
        /// 质量
        /// </summary>
        private int measurementRate = 8;
        public int MeasurementRate
        {
            get { return measurementRate; }
            set { measurementRate = value; }
        }
        /// <summary>
        /// 噪声压缩
        /// </summary>
        private int noiseCompression = 1;
        public int NoiseCompression
        {
            get { return noiseCompression; }
            set { noiseCompression = value; }
        }

        /// <summary>
        /// 竖直角最小值
        /// </summary>
        private double verticalAngleMin = -60;
        public double VerticalAngleMin
        {
            get { return verticalAngleMin; }
            set { verticalAngleMin = value; }
        }
        /// <summary>
        /// 竖直角最大值
        /// </summary>
        private double verticalAngleMax = 90;
        public double VerticalAngleMax
        {
            get { return verticalAngleMax; }
            set { verticalAngleMax = value; }
        }
        /// <summary>
        /// 扫描线数
        /// </summary>
        private int numCols = 2000000;
        public int NumCols
        {
            get { return numCols; }
            set { numCols = value; }
        }
        /// <summary>
        /// 分块线数
        /// </summary>
        private int splitAfterLines = 5000;
        public int SplitAfterLines
        {
            get { return splitAfterLines; }
            set { splitAfterLines = value; }
        }
    }
    /// <summary>
    /// 点云点类
    /// </summary>
    public class Faro_point
    {
        public void getValue(Faro_point p)
        {
            X = p.X;
            Y = p.Y;
            Z = p.Z;
            Color = p.Color;
            xe = p.xe;
            H = p.H;
            Hs = p.Hs;
            //tag = p.tag; 
        }
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
        public double R { get; set; }
        public int Color { get; set; }
        public double xe { get; set; }
        public double H { get; set; }
        public double Hs { get; set; }
        public byte tag { get; set; }
        public double dh { get; set; }
    }
    /// <summary>
    /// 椭圆参数类
    /// </summary>
    public class Ellipse
    {
        public Ellipse(Ellipse ellipse) {
            A = ellipse.A;
            B = ellipse.B;
            X0 = ellipse.X0;
            Y0 = ellipse.Y0;
            Angle = ellipse.Angle;
        }
        public Ellipse() {

        }
        public double A { get; set; }
        public double B { get; set; }
        public double X0 { get; set; }
        public double Y0 { get; set; }
        public double Angle { get; set; }
    }
    /// <summary>
    /// 里程椭圆参数类（包括椭圆参数）
    /// </summary>
    public class MileageEllipse {
        public double mileage { get; set; }
        public double dm { get; set; }
        public int col { get; set; }
        public Ellipse ep { get; set; }
    }
    /// <summary>
    /// 二维点辅助计算类
    /// </summary>
    public class BaseXY
    {
        public double X { get; set; }
        public double Y { get; set; }
        public int Color { get; set; }
        public double Error { get; set; }
        public double angle { get; set; }
    }
    /// <summary>
    /// 里程类
    /// </summary>
    public class Mileage
    {
        public double ID { get; set; }
        public double M { get; set; }
        public long Time { get; set; }
    }
    /// <summary>
    /// 识别环缝类
    /// </summary>
    public class DivideLine
    {
        /// <summary>
        /// 类别
        /// </summary>
        public string attribute { get; set; }
        /// <summary>
        /// ID/标识（标明第几条环间缝）
        /// </summary>
        public int ID { get; set; }
        /// <summary>
        /// 左标识/ID（标明环内缝左侧环缝ID）
        /// </summary>
        public int left_ID { get; set; }
        /// <summary>
        /// 右标识/ID（标明环内缝右侧环缝ID）
        /// </summary>
        public int right_ID { get; set; }
        /// <summary>
        /// 环缝点集/螺栓孔点集
        /// </summary>
        public List<point_data> point_data { get; set; }
    }
    /// <summary>
    /// 处理信息类
    /// </summary>
    public class Do_Message
    {
        /// <summary>
        /// 文件名
        /// </summary>
        public string NameOrPoint { get; set; }
        /// <summary>
        /// 分辨率，单位像素/米
        /// </summary>
        public int expand { get; set; }
        /// <summary>
        /// 左里程
        /// </summary>
        public double y1 { get; set; }
        /// <summary>
        /// 右里程
        /// </summary>
        public double y2 { get; set; }
        /// <summary>
        /// 中里程
        /// </summary>
        public double y0 { get; set; }
    }
    /// <summary>
    /// 环缝/螺栓孔点集类
    /// </summary>
    public class point_data
    {
        /// <summary>
        /// 像素横坐标
        /// </summary>
        public int w { get; set; }
        /// <summary>
        /// 像素纵坐标
        /// </summary>
        public int h { get; set; }
        /// <summary>
        /// 实际横坐标
        /// </summary>
        public double x { get; set; }
        /// <summary>
        /// 实际纵坐标
        /// </summary>
        public double y { get; set; }
    }
    /// <summary>
    /// 病害类型类
    /// </summary>
    public class Contour
    {
        /// <summary>
        /// 点集
        /// </summary>
        public List<Point> point;
        /// <summary>
        /// 面积
        /// </summary>
        public double S;
        /// <summary>
        /// 长度
        /// </summary>
        public double Length;
        /// <summary>
        /// 宽度
        /// </summary>
        public double Wide;
    }
}
