using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using ExcelDna.Integration;
using QksExcelAddIn.Modeling.Core;

namespace QksExcelAddIn.Services
{
    internal static class WindExcelSelection
    {
        internal static (IReadOnlyList<StoryWindInput> Rows,string Basis) Read(string defaultPattern)
        {
            dynamic app=ExcelDnaUtil.Application;dynamic range=app.Selection;
            if((int)range.Areas.Count!=1)throw new ArgumentException("Chọn một vùng Excel liên tục, có hàng heading.");
            int count=(int)range.Rows.Count,columns=(int)range.Columns.Count;
            if(count>10020||columns>64)throw new ArgumentException("Vùng chọn vượt 10.000 dòng hoặc 64 cột.");
            object raw=range.Value2;var values=raw as object[,];
            if(values==null)throw new ArgumentException("Vùng chọn cần heading và dữ liệu, không chỉ một ô.");
            string Format(object value)=>value==null?"":value is ErrorWrapper?"#EXCEL_ERROR":value is IFormattable f?f.ToString(null,CultureInfo.InvariantCulture):value.ToString()??"";
            var table=new List<string[]>();
            for(int r=values.GetLowerBound(0);r<=values.GetUpperBound(0);r++) { var line=new string[columns];for(int c=0;c<columns;c++)line[c]=Format(values[r,c+values.GetLowerBound(1)]);table.Add(line); }
            string basis="Excel: "+(string)app.ActiveWorkbook.Name+" · "+(string)range.Address;
            return (WindInputTable.Parse(table,defaultPattern),basis);
        }
    }
}
