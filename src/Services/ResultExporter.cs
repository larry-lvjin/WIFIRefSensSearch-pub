using System;
using System.Collections.Generic;
using System.Globalization;
using ClosedXML.Excel;
using WIFISensitivitySearch.Models;

namespace WIFISensitivitySearch.Services;

public static class ResultExporter
{
    public static void ExportXlsx(string filePath, IEnumerable<TestRow> rows)
    {
        // 实现细节未公开（算法核心）
        throw new NotImplementedException("该方法的实现不在开源范围内");
    }

    /// <summary>
    /// 通道结果 "36 : -74.1 dBm" → 数值 -74.1 (Excel 数字单元格); 空/"NOT ASSOC"/"FAIL"/"RAMP FAIL"
    /// 等非数值状态 → 保留原文本。目标表格 Channel A/B/C 列只显示纯数值灵敏度。
    /// </summary>
    private static XLCellValue ChannelValue(string result)
    {
        // 实现细节未公开（算法核心）
        throw new NotImplementedException("该方法的实现不在开源范围内");
    }
}
