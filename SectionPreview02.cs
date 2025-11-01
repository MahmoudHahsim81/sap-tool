using System;
using System.Drawing;
using System.Windows.Forms;

namespace _3D_SAP
{
    /// <summary>
    /// رسومات المعاينة + كل الدوال المساعدة (متوافق C# 7.3)
    /// </summary>
    public static class SectionPreview02
    {
        // ===== Common style =====
        private static readonly Font DimFont = new Font("Segoe UI", 9f, FontStyle.Regular, GraphicsUnit.Point);
        private const int Margin = 22;   // هامش عام
        private const int Tick = 6;      // طول المساكة
        private const int Gap = 8;       // مسافة النص عن خط البعد

        private static Color Bg(bool dark) { return dark ? Color.FromArgb(32, 32, 34) : Color.White; }
        private static Color Edge(bool dark) { return dark ? Color.FromArgb(210, 225, 235) : Color.FromArgb(0, 90, 160); }
        private static Color FillC(bool dark) { return dark ? Color.FromArgb(60, 140, 220, 255) : Color.FromArgb(120, 30, 144, 255); }
        private static Color DimC(bool dark) { return dark ? Color.FromArgb(220, 220, 220) : Color.FromArgb(50, 50, 50); }

        private static string RoundMM(double v) { return string.Format("{0:0} mm", Math.Round(v)); }

        // يبدأ لوحة جديدة
        private static Graphics Begin(PictureBox pb, bool dark, out Bitmap bmp)
        {
            bmp = new Bitmap(Math.Max(2, pb.Width), Math.Max(2, pb.Height));
            Graphics g = Graphics.FromImage(bmp);
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.Clear(Bg(dark));
            return g;
        }

        // يثبت الصورة في الـ PictureBox
        private static void Commit(PictureBox pb, Bitmap bmp)
        {
            if (pb.Image != null)
            {
                var old = pb.Image;
                pb.Image = null;
                try { old.Dispose(); } catch { }
            }
            pb.Image = bmp;
        }

        // ====== أبعاد أفقية ======
        private static void DrawDimH(Graphics g, Pen p, SolidBrush br, float x1, float y, float x2, string text, bool above)
        {
            if (x2 < x1) { float t = x1; x1 = x2; x2 = t; }
            float yLine = y + (above ? -Tick : Tick);

            g.DrawLine(p, x1, yLine, x2, yLine);
            g.DrawLine(p, x1, yLine - Tick, x1, yLine + Tick);
            g.DrawLine(p, x2, yLine - Tick, x2, yLine + Tick);

            string s = text ?? "";
            SizeF sz = g.MeasureString(s, DimFont);
            float cx = (x1 + x2 - sz.Width) / 2f;
            float ty = yLine + (above ? -Gap - sz.Height : Gap);
            g.DrawString(s, DimFont, br, cx, ty);
        }

        // ====== أبعاد رأسية ======
        // textOnRight = true يطبع النص يمين خط البعد، false يطبع شماله
        private static void DrawDimV(Graphics g, Pen p, SolidBrush br, float x, float y1, float y2, string text, bool textOnRight)
        {
            if (y2 < y1) { float t = y1; y1 = y2; y2 = t; }

            g.DrawLine(p, x, y1, x, y2);
            g.DrawLine(p, x - Tick, y1, x + Tick, y1);
            g.DrawLine(p, x - Tick, y2, x + Tick, y2);

            string s = text ?? "";
            SizeF sz = g.MeasureString(s, DimFont);
            float tx = textOnRight ? (x + Gap) : (x - sz.Width - Gap);
            float ty = (y1 + y2 - sz.Height) / 2f;
            g.DrawString(s, DimFont, br, tx, ty);
        }

        // ===== I-Section =====
        public static void DrawI(PictureBox pb, bool dark,
                                 double h, double bTop, double tfTop,
                                 double tw, double bBot, double tfBot,
                                 bool showDims)
        {
            if (pb == null || pb.Width <= 2 || pb.Height <= 2) return;

            Color edge = Edge(dark);
            Color fillL = dark ? Color.FromArgb(120, 180, 220) : Color.FromArgb(150, 180, 255);
            Color dimCol = DimC(dark);

            Bitmap bmp = null;
            Graphics g = null;
            Pen pEdge = null, pDim = null;
            SolidBrush brFill = null, brDim = null;

            try
            {
                g = Begin(pb, dark, out bmp);
                pEdge = new Pen(edge, 2f);
                pDim = new Pen(dimCol, 1.4f);
                brFill = new SolidBrush(fillL);
                brDim = new SolidBrush(dimCol);

                double maxW = Math.Max(bTop, bBot);
                double sx = (pb.Width - 2 * Margin) / Math.Max(1.0, maxW);
                double sy = (pb.Height - 2 * Margin) / Math.Max(1.0, h);
                double s = Math.Min(sx, sy);

                float H = (float)(h * s);
                float BT = (float)(bTop * s);
                float TT = (float)(tfTop * s);
                float TW = (float)(tw * s);
                float BB = (float)(bBot * s);
                float TB = (float)(tfBot * s);

                float cx = pb.Width / 2f;
                float topY = (pb.Height - H) / 2f;

                RectangleF topFlange = new RectangleF(cx - BT / 2, topY, BT, Math.Max(2f, TT));
                RectangleF web = new RectangleF(cx - TW / 2, topY + TT, Math.Max(2f, TW), Math.Max(2f, H - TT - TB));
                RectangleF botFlange = new RectangleF(cx - BB / 2, topY + H - TB, BB, Math.Max(2f, TB));

                g.FillRectangle(brFill, topFlange);
                g.FillRectangle(brFill, web);
                g.FillRectangle(brFill, botFlange);
                g.DrawRectangle(pEdge, topFlange.X, topFlange.Y, topFlange.Width, topFlange.Height);
                g.DrawRectangle(pEdge, web.X, web.Y, web.Width, web.Height);
                g.DrawRectangle(pEdge, botFlange.X, botFlange.Y, botFlange.Width, botFlange.Height);

                if (showDims)
                {
                    // البعد الرأسي الكُلّي (H) على الشمال
                    DrawDimV(g, pDim, brDim, topFlange.Left - 28, topFlange.Top, botFlange.Bottom, RoundMM(h), false);

                    // ======= أبعاد BT/BB أفقية بدون قص =======
                    const float pad = 0f;
                    float xLTop = topFlange.Left - pad;
                    float xRTop = topFlange.Right + pad;
                    float xLBot = botFlange.Left - pad;
                    float xRBot = botFlange.Right + pad;

                    SizeF szTopText = g.MeasureString("BT=" + Math.Round(bTop), DimFont);
                    SizeF szBotText = g.MeasureString("BB=" + Math.Round(bBot), DimFont);

                    float minMargin = 2f;

                    // أعلى (BT) – النص فوق الخط
                    float lineYTopDesired = topFlange.Top - 6f;
                    float lineYTopMinForTxt = 5 + szTopText.Height + minMargin;
                    float lineYTop = Math.Max(lineYTopDesired, lineYTopMinForTxt);
                    float yBaseTopForFunc = lineYTop + Tick; // لأن DrawDimH ترفع الخط Tick عند above=true
                    DrawDimH(g, pDim, brDim, xLTop, yBaseTopForFunc, xRTop, "BT=" + Math.Round(bTop), false);

                    // أسفل (BB) – النص تحت الخط
                    float lineYBotDesired = botFlange.Bottom - 6f;
                    float lineYBotMaxForTxt = pb.Height - (5 + szBotText.Height + Tick + minMargin);
                    float lineYBot = Math.Min(lineYBotDesired, lineYBotMaxForTxt);
                    float yBaseBotForFunc = lineYBot - Tick; // لأن DrawDimH تنزل الخط Tick عند above=false
                    DrawDimH(g, pDim, brDim, xLBot, yBaseBotForFunc, xRBot, "BB=" + Math.Round(bBot), true);
                    // ======================================

                    // ملاحظات السماكات على يمين القطاع
                    float xNote = Math.Max(topFlange.Right, botFlange.Right) + 40;

                    // TfT
                    g.DrawLine(pDim, topFlange.Right + 15, topFlange.Top + TT / 2, xNote - 8, topFlange.Top + TT / 2);
                    g.DrawString("TfT=" + Math.Round(tfTop), DimFont, brDim,
                                 xNote, topFlange.Top + TT / 2 - g.MeasureString("X", DimFont).Height / 2);

                    // Tw
                    float webMidY = web.Top + web.Height / 2;
                    g.DrawLine(pDim, web.Right + 15, webMidY, xNote - 8, webMidY);
                    g.DrawString("Tw=" + Math.Round(tw), DimFont, brDim,
                                 xNote, webMidY - g.MeasureString("X", DimFont).Height / 2);

                    // TfB
                    g.DrawLine(pDim, botFlange.Right + 15, botFlange.Top + TB / 2, xNote - 8, botFlange.Top + TB / 2);
                    g.DrawString("TfB=" + Math.Round(tfBot), DimFont, brDim,
                                 xNote, botFlange.Top + TB / 2 - g.MeasureString("X", DimFont).Height / 2);
                }
            }
            finally
            {
                if (g != null) g.Dispose();
                if (pEdge != null) pEdge.Dispose();
                if (pDim != null) pDim.Dispose();
                if (brFill != null) brFill.Dispose();
                if (brDim != null) brDim.Dispose();
                if (bmp != null) Commit(pb, bmp);
            }
        }

        // ===== RHS =====
        public static void DrawRHS(PictureBox pb, bool dark, double depth, double width, double t)
        {
            if (pb == null || pb.Width <= 2 || pb.Height <= 2) return;

            Color edge = Edge(dark);
            Color fill = FillC(dark);

            Bitmap bmp = null;
            Graphics g = null;
            Pen pEdge = null;
            SolidBrush brFill = null, brBg = null;

            try
            {
                g = Begin(pb, dark, out bmp);
                pEdge = new Pen(edge, 2f);
                brFill = new SolidBrush(fill);
                brBg = new SolidBrush(Bg(dark));

                double sx = (pb.Width - 2 * Margin) / Math.Max(1.0, width);
                double sy = (pb.Height - 2 * Margin) / Math.Max(1.0, depth);
                double s = Math.Min(sx, sy);

                float W = (float)(width * s);
                float D = (float)(depth * s);
                float T = (float)Math.Max(2.0, t * s);

                float x = (pb.Width - W) / 2f;
                float y = (pb.Height - D) / 2f;

                RectangleF outer = new RectangleF(x, y, W, D);
                RectangleF inner = new RectangleF(x + T, y + T,
                    Math.Max(1, W - 2 * T),
                    Math.Max(1, D - 2 * T));

                g.FillRectangle(brFill, outer);
                g.DrawRectangle(pEdge, outer.X, outer.Y, outer.Width, outer.Height);
                g.FillRectangle(brBg, inner);
                g.DrawRectangle(pEdge, inner.X, inner.Y, inner.Width, inner.Height);
            }
            finally
            {
                if (g != null) g.Dispose();
                if (pEdge != null) pEdge.Dispose();
                if (brFill != null) brFill.Dispose();
                if (brBg != null) brBg.Dispose();
                if (bmp != null) Commit(pb, bmp);
            }
        }

        // ===== Non-Prismatic (Taper Panel) =====
        public static void DrawTaper(PictureBox pb, bool dark, double startDepth, double endDepth)
        {
            if (pb == null || pb.Width <= 2 || pb.Height <= 2) return;

            Color edge = Edge(dark);
            Color fill = FillC(dark);
            Color dimCol = DimC(dark);

            Bitmap bmp = null;
            Graphics g = null;
            Pen pEdge = null, pDim = null;
            SolidBrush brFill = null, brDim = null;

            try
            {
                g = Begin(pb, dark, out bmp);
                pEdge = new Pen(edge, 3f);
                pDim = new Pen(dimCol, 1.5f);
                brFill = new SolidBrush(fill);
                brDim = new SolidBrush(dimCol);

                // Scaling للعمق الرأسي
                double maxD = Math.Max(1.0, Math.Max(startDepth, endDepth));
                double s = (pb.Height - 2.0 * Margin) / maxD;

                float left = Margin + 14;
                float right = pb.Width - Margin - 14;
                float top = Margin;

                float dStart = (float)(startDepth * s);
                float dEnd = (float)(endDepth * s);

                PointF A = new PointF(left, top);
                PointF B = new PointF(right, top);
                PointF C = new PointF(right, top + dEnd);
                PointF D = new PointF(left, top + dStart);

                // Fill + Edge
                g.FillPolygon(brFill, new[] { A, B, C, D });
                g.DrawPolygon(pEdge, new[] { A, B, C, D });

                // على اليسار: الرقم على يمين الخط
                float xLeftDim = left - 5;
                DrawDimV(g, pDim, brDim, xLeftDim, A.Y, D.Y, RoundMM(startDepth), true);

                // على اليمين: الرقم على يسار الخط
                float xRightDim = right + 5;
                DrawDimV(g, pDim, brDim, xRightDim, B.Y, C.Y, RoundMM(endDepth), false);
            }
            finally
            {
                if (g != null) g.Dispose();
                if (pEdge != null) pEdge.Dispose();
                if (pDim != null) pDim.Dispose();
                if (brFill != null) brFill.Dispose();
                if (brDim != null) brDim.Dispose();
                if (bmp != null) Commit(pb, bmp);
            }
        }
    }
}
