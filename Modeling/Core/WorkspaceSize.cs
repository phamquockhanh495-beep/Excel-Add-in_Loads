using System;

namespace QksExcelAddIn.Modeling.Core
{
    public sealed class WorkspaceSize
    {
        public double Width { get; }
        public double Height { get; }
        public double MinWidth { get; }
        public double MinHeight { get; }
        private WorkspaceSize(double width, double height)
        { Width = width; Height = height; MinWidth = Math.Min(960, width); MinHeight = Math.Min(540, height); }
        // Work area is physical pixels; WPF dimensions are DIP. Keep taskbar and a small outside margin clear.
        public static WorkspaceSize Fit(double workPixelsWidth, double workPixelsHeight, double scaleX, double scaleY,
            double preferredWidth = 1180, double preferredHeight = 740)
        {
            foreach (double value in new[] {workPixelsWidth,workPixelsHeight,scaleX,scaleY,preferredWidth,preferredHeight})
                if (!Number.Finite(value) || value <= 0) throw new ArgumentException("Work area, DPI và kích thước phải hữu hạn, lớn hơn 0.");
            double workWidth = workPixelsWidth / scaleX, workHeight = workPixelsHeight / scaleY;
            double marginX = Math.Min(12,workWidth/8), marginY = Math.Min(12,workHeight/8);
            return new WorkspaceSize(Math.Min(preferredWidth,workWidth-2*marginX),Math.Min(preferredHeight,workHeight-2*marginY));
        }
        public static int PanelMode(double width) => width >= 1060 ? 2 : width >= 820 ? 1 : 0;
    }
}
