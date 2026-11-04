using System;
namespace QksExcelAddIn.Modeling.Printing
{
    public static class PrintPreviewScale
    {
        // Page dimensions are PDF points; this factor maps the vector page to WPF DIPs.
        public static double Fit(double pageWidth,double pageHeight,double viewportWidth,double viewportHeight)
        {
            if(pageWidth<=0||pageHeight<=0||viewportWidth<=0||viewportHeight<=0)return 1;
            return Math.Max(.01,Math.Min(4,Math.Min(viewportWidth/pageWidth,viewportHeight/pageHeight)));
        }
    }
}
