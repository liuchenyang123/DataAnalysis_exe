using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IQOPENLib;

/// <summary>
/// 由于faro的sdk只支持.net3.5以下的版本，所以将一些用到的方法封装成dll方便在高版本.net中调用
/// </summary>
namespace DLLForFls
{
    public class point
    {
        public double x { get; set; }
        public double y { get; set; }
        public double z { get; set; }
        public int color { get; set; }
    }
    public static class OpenFls
    {
        public static bool IsSuccess = false;
        public static IiQLibIf libRef;
        private static IiQLicensedInterfaceIf licLibIf;
        /// <summary>
        /// 初始化
        /// </summary>
        public static void Authorize()
        {
            string licenseCode = /* FARO LS license code */
             "FARO Open Runtime License\n" +
             "Key:J3CW4PNRTCTXFJ7T6KZUARUPL\n" + // replace ? with key!此处固定了秘钥，正常解析应该都能用，后续如果需要可以更改秘钥
             "\n" +
             "The software is the registered property of " +
             "FARO Scanner Production GmbH, Stuttgart, Germany.\n" +
             "All rights reserved.\n" +
             "This software may only be used with written permission " +
             "of FARO Scanner Production GmbH, Stuttgart, Germany.";
            licLibIf = new iQLibIfClass();
            libRef = (IiQLibIf)licLibIf;
            licLibIf.License = licenseCode;
            IsSuccess = true;
            
        }
        /// <summary>
        /// 解析反射强度的模式，详情参考Faro的sdk文档
        /// </summary>
        /// <param name="mode">0，1，2可选</param>
        public static void ScanReflectionMode(int mode)
        {
            libRef.scanReflectionMode = mode;
        }
        /// <summary>
        /// fls文件的加载
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <returns></returns>
        public static int load(string fileName)
        {
            return libRef.load(fileName);
        }
        
        /// <summary>
        /// 获取扫描的行数
        /// </summary>
        /// <param name="scan">扫描的序号，一般选0</param>
        /// <returns></returns>
        public static int getScanNumRows(int scan)
        {
            return libRef.getScanNumRows(scan);
        }

        /// <summary>
        /// 获取扫描的列数
        /// </summary>
        /// <param name="scan">扫描的序号，一般选0</param>
        /// <returns></returns>
        public static int getScanNumCols(int scan)
        {
            return libRef.getScanNumCols(scan);
        }

        /// <summary>
        /// 获取单点的时间戳
        /// </summary>
        /// <param name="scan">扫描的序号，一般选0</param>
        /// <param name="row">行</param>
        /// <param name="col">列</param>
        /// <param name="atime">时间戳</param>
        /// <returns></returns>
        public static int getAutomationTimeOfScanPoint(int scan, int row, int col, out ulong atime)
        {
            return libRef.getAutomationTimeOfScanPoint(scan, row, col, out atime);
        }

        /// <summary>
        /// 获取某列中第row行开始一共numRows行的扫描数据（笛卡尔坐标系下）
        /// </summary>
        /// <param name="scan">扫描的序号，一般选0</param>
        /// <param name="row">起始行</param>
        /// <param name="col">列</param>
        /// <param name="numRows">总行数</param>
        /// <param name="pos">xyz坐标数据</param>
        /// <param name="reflections">反射强度数据</param>
        /// <returns></returns>
        public static int getXYZScanPoints2(int scan, int row, int col, int numRows, out Array pos, out Array reflections)
        {

            return libRef.getXYZScanPoints2(scan, row, col, numRows, out pos, out reflections);

        }

        /// <summary>
        /// 获取某列中第row行开始一共numRows行的扫描数据（极坐标系下）
        /// </summary>
        /// <param name="scan">扫描的序号，一般选0</param>
        /// <param name="row">起始行</param>
        /// <param name="col">列</param>
        /// <param name="numRows">总行数</param>
        /// <param name="pos">极长，水平角，竖直角坐标数据</param>
        /// <param name="reflections">反射强度数据</param>
        /// <returns></returns>
        public static int getPolarScanPoints2(int scan, int row, int col, int numRows, out Array pos, out Array reflections)
        {
            return libRef.getPolarScanPoints2(scan, row, col, numRows, out pos, out reflections);
        }
    }
}
