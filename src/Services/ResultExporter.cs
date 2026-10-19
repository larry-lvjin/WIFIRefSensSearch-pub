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
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Sensitivity");

        string[] headers = { "Type", "Threshold", "Start ()", "Channel A", "Channel B", "Channel C" };
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontColor = XLColor.FromHtml("#F2C840");
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#0C1A38");
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cell.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
        }

        int r = 2;
        foreach (var row in rows)
        {
            ws.Cell(r, 1).Value = row.Type;
            ws.Cell(r, 1).Style.Font.Bold = true;
            ws.Cell(r, 1).Style.Font.FontColor = XLColor.FromHtml("#1F6EA0");

            ws.Cell(r, 2).Value = row.Threshold;
            ws.Cell(r, 2).Style.Font.FontColor = XLColor.FromHtml("#666666");
            ws.Cell(r, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Cell(r, 3).Value = row.Start.ToString("0.#", CultureInfo.InvariantCulture);
            ws.Cell(r, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Cell(r, 4).Value = ChannelValue(row.Channel1Result);
            ws.Cell(r, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Cell(r, 5).Value = ChannelValue(row.Channel2Result);
            ws.Cell(r, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Cell(r, 6).Value = ChannelValue(row.Channel3Result);
            ws.Cell(r, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            // Alternating row banding to mirror the UI grid.
            var rowRange = ws.Range(r, 1, r, headers.Length);
            rowRange.Style.Border.BottomBorder = XLBorderStyleValues.Hair;
            rowRange.Style.Border.BottomBorderColor = XLColor.FromHtml("#1B2A55");
            r++;
        }

        ws.Columns().AdjustToContents();
        ws.Column(1).Width = System.Math.Max(ws.Column(1).Width, 28);
        ws.Column(4).Width = System.Math.Max(ws.Column(4).Width, 18);
        ws.Column(5).Width = System.Math.Max(ws.Column(5).Width, 18);
        ws.Column(6).Width = System.Math.Max(ws.Column(6).Width, 18);
        ws.SheetView.FreezeRows(1);

        wb.SaveAs(filePath);
    }

    /// <summary>
    /// 通道结果 "36 : -74.1 dBm" → 数值 -74.1 (Excel 数字单元格); 空/"NOT ASSOC"/"FAIL"/"RAMP FAIL"
    /// 等非数值状态 → 保留原文本。目标表格 Channel A/B/C 列只显示纯数值灵敏度。
    /// </summary>
    private static XLCellValue ChannelValue(string result)
    {
        if (string.IsNullOrWhiteSpace(result)) return string.Empty;
        var s = result;
        int colon = s.LastIndexOf(':');
        if (colon >= 0) s = s.Substring(colon + 1);
        s = s.Replace("dBm", "", StringComparison.OrdinalIgnoreCase).Trim();
        if (double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var v))
            return v;
        return result;
    }
}
