using System;
using System.Drawing;
using System.Windows.Forms;

namespace _3D_SAP
{
    /// <summary>
    /// رسومات المعاينة لقطاعات I و RHS على PictureBox مع أبعاد.
    /// موضع الأبعاد مُحسّن: العلوي ظاهر، السفلي منخفض، والسماكات على الجنب.
    /// </summary>
    public static class SectionPreview
    {
        private const int Margin = 24;        // هامش داخلي
        private const int DimOffset = 10;     // بُعد خط البُعد عن العنصر
        private const int Arrow = 6;          // طول رأس السهم
        private static readonly Font DimFont = new Font("Segoe UI", 9f, FontStyle.Regular);

        public static void DrawI(PictureBox pb, bool dark,
                                 double h, double bTop, double tfTop,
                                 double tw, double bBot, double tfBot,
                                 bool showDims = true)
        {
            if (pb == null || pb.Width <= 2 || pb.Height <= 2) return;

            Color bg = dark ? Color.FromArgb(32, 32, 34) : Color.White;
            Color edge = dark ? Color.White : Color.Black;
            Color fill = dark ? Color.FromArgb(60, 140, 220, 255) : Color.FromArgb(120, 30, 144, 255);
            Color dimC = dark ? Color.FromArgb(220, 220, 220) : Color.FromArgb(40, 40, 40);

            Bitmap bmp = new Bitmap(pb.Width, pb.Height);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(bg);
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                using (Pen p = new Pen(edge, 2f))
                using (SolidBrush b = new SolidBrush(fill))
                using (Pen pDim = new Pen(dimC, 1.5f))
                using (SolidBrush brDim = new SolidBrush(dimC))
                {
                    // Scale
                    double maxW = Math.Max(bTop, bBot);
                    double sx = (pb.Width - (Margin * 2)) / (maxW <= 0 ? 1.0 : maxW);
                    double sy = (pb.Height - (Margin * 2)) / (h <= 0 ? 1.0 : h);
                    double s = Math.Min(sx, sy);

                    float cx = pb.Width / 2f;
                    float topY = (float)((pb.Height - h * s) / 2.0);

                    float H = (float)(h * s);
                    float BT = (float)(bTop * s);
                    float TT = (float)(tfTop * s);
                    float TW = (float)(tw * s);
                    float BB = (float)(bBot * s);
                    float TB = (float)(tfBot * s);

                    RectangleF topFlange = new RectangleF(cx - BT / 2, topY, BT, TT);
                    RectangleF web = new RectangleF(cx - TW / 2, topY + TT, TW, H - TT - TB);
                    RectangleF botFlange = new RectangleF(cx - BB / 2, topY + H - TB, BB, TB);

                    // رسم القطاع
                    g.FillRectangle(b, topFlange);
                    g.FillRectangle(b, web);
                    g.FillRectangle(b, botFlange);
                    g.DrawRectangle(p, topFlange.X, topFlange.Y, topFlange.Width, topFlange.Height);
                    g.DrawRectangle(p, web.X, web.Y, web.Width, web.Height);
                    g.DrawRectangle(p, botFlange.X, botFlange.Y, botFlange.Width, botFlange.Height);

                    if (showDims)
                    {
                        // ===== الأبعاد الرئيسية =====
                        // 1) الارتفاع الكلي H (يسار العنصر)
                        float xLeft = (float)Math.Min(topFlange.Left, Math.Min(web.Left, botFlange.Left));
                        float dimX = xLeft - DimOffset - 14;
                        DrawDimV(g, pDim, brDim, dimX, topY, topY + H, $"{RoundMM(h)} mm");

                        // 2) عرض الـTop Flange (ارفعناه لفوق أكثر ليظهر)
                        float yTopDim = topFlange.Top - (DimOffset - 2);
                        DrawDimH(g, pDim, brDim, topFlange.Left, topFlange.Right, yTopDim, $"BT={RoundMM(bTop)}");

                        // 3) عرض الـBottom Flange (نزلناه لتحت أكثر)
                        float yBotDim = botFlange.Bottom + (DimOffset + 12);
                        DrawDimH(g, pDim, brDim, botFlange.Left, botFlange.Right, yBotDim, $"BB={RoundMM(bBot)}");

                        // ===== السماكات على الجانب الأيمن =====
                        float rightMost = Math.Max(topFlange.Right, Math.Max(web.Right, botFlange.Right));

                        // 4) TfTop على اليمين
                        DrawLeaderTextH(g, pDim, brDim,
                            rightMost + 10, topFlange.Top + TT / 2f,
                            $"TfT={RoundMM(tfTop)}", toRight: true);

                        // 5) TfBot على اليمين
                        DrawLeaderTextH(g, pDim, brDim,
                            rightMost + 10, botFlange.Top + TB / 2f,
                            $"TfB={RoundMM(tfBot)}", toRight: true);

                        // 6) Tw على اليمين في منتصف الـWeb
                        DrawLeaderTextH(g, pDim, brDim,
                            rightMost + 10, web.Top + web.Height / 2f,
                            $"Tw={RoundMM(tw)}", toRight: true);
                    }
                }
            }

            SetPbImage(pb, bmp);
        }

        public static void DrawRHS(PictureBox pb, bool dark,
                                   double depth, double width, double t,
                                   bool showDims = true)
        {
            if (pb == null || pb.Width <= 2 || pb.Height <= 2) return;

            Color bg = dark ? Color.FromArgb(32, 32, 34) : Color.White;
            Color edge = dark ? Color.White : Color.Black;
            Color fill = dark ? Color.FromArgb(60, 140, 220, 255) : Color.FromArgb(120, 30, 144, 255);
            Color dimC = dark ? Color.FromArgb(220, 220, 220) : Color.FromArgb(40, 40, 40);

            Bitmap bmp = new Bitmap(pb.Width, pb.Height);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(bg);
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                using (Pen p = new Pen(edge, 2f))
                using (SolidBrush b = new SolidBrush(fill))
                using (SolidBrush bgB = new SolidBrush(bg))
                using (Pen pDim = new Pen(dimC, 1.5f))
                using (SolidBrush brDim = new SolidBrush(dimC))
                {
                    double sx = (pb.Width - (Margin * 2)) / (width <= 0 ? 1.0 : width);
                    double sy = (pb.Height - (Margin * 2)) / (depth <= 0 ? 1.0 : depth);
                    double s = Math.Min(sx, sy);

                    float W = (float)(width * s);
                    float D = (float)(depth * s);
                    float T = (float)(t * s);
                    if (T < 2f) T = 2f;

                    float x = (pb.Width - W) / 2f;
                    float y = (pb.Height - D) / 2f;

                    RectangleF outer = new RectangleF(x, y, W, D);
                    RectangleF inner = new RectangleF(x + T, y + T,
                        (W - 2 * T) < 1 ? 1 : W - 2 * T,
                        (D - 2 * T) < 1 ? 1 : D - 2 * T);

                    // رسم
                    g.DrawRectangle(p, outer.X, outer.Y, outer.Width, outer.Height);
                    g.FillRectangle(b, outer);
                    g.FillRectangle(bgB, inner);
                    g.DrawRectangle(p, inner.X, inner.Y, inner.Width, inner.Height);

                    if (showDims)
                    {
                        // عرض W أعلى
                        DrawDimH(g, pDim, brDim, outer.Left, outer.Right, outer.Top - (DimOffset + 12), $"W={RoundMM(width)}");
                        // عمق D يسار
                        DrawDimV(g, pDim, brDim, outer.Left - (DimOffset + 14), outer.Top, outer.Bottom, $"D={RoundMM(depth)}");
                        // السماكة t على اليمين كـ leader
                        DrawLeaderTextH(g, pDim, brDim, outer.Right + 10, (outer.Top + outer.Bottom) / 2f, $"t={RoundMM(t)}", toRight: true);
                    }
                }
            }

            SetPbImage(pb, bmp);
        }

        // ====================== Helpers ======================
        private static void SetPbImage(PictureBox pb, Bitmap bmp)
        {
            if (pb.Image != null)
            {
                var old = pb.Image;
                pb.Image = null;
                old.Dispose();
            }
            pb.Image = bmp;
        }

        private static string RoundMM(double v)
        {
            if (double.IsNaN(v) || double.IsInfinity(v)) return "-";
            double r = Math.Round(v, v >= 10 ? 0 : 1);
            return r.ToString();
        }

        // بُعد أفقي
        private static void DrawDimH(Graphics g, Pen p, Brush br, float x1, float x2, float y, string text, bool flipArrows = false, bool centerOnLine = true)
        {
            if (x2 < x1) { float t = x1; x1 = x2; x2 = t; }
            g.DrawLine(p, x1, y, x2, y);

            if (!flipArrows)
            {
                DrawArrow(g, p, new PointF(x1, y), new PointF(x1 + Arrow, y));
                DrawArrow(g, p, new PointF(x2, y), new PointF(x2 - Arrow, y));
            }
            else
            {
                DrawArrow(g, p, new PointF(x1, y), new PointF(x1 - Arrow, y));
                DrawArrow(g, p, new PointF(x2, y), new PointF(x2 + Arrow, y));
            }

            SizeF sz = g.MeasureString(text, DimFont);
            float tx = centerOnLine ? (x1 + x2 - sz.Width) / 2f : x1;
            float ty = y - sz.Height - 2;
            g.DrawString(text, DimFont, br, tx, ty);
        }

        // بُعد رأسي
        private static void DrawDimV(Graphics g, Pen p, Brush br, float x, float y1, float y2, string text)
        {
            if (y2 < y1) { float t = y1; y1 = y2; y2 = t; }
            g.DrawLine(p, x, y1, x, y2);
            DrawArrow(g, p, new PointF(x, y1), new PointF(x, y1 + Arrow));
            DrawArrow(g, p, new PointF(x, y2), new PointF(x, y2 - Arrow));

            SizeF sz = g.MeasureString(text, DimFont);
            float tx = x - sz.Width - 4;
            float ty = (y1 + y2 - sz.Height) / 2f;
            g.DrawString(text, DimFont, br, tx, ty);
        }

        // خط قائد أفقي + نص (للسماكات على الجنب)
        private static void DrawLeaderTextH(Graphics g, Pen p, Brush br, float xStart, float y, string text, bool toRight)
        {
            float L = 18f;
            float xEnd = toRight ? xStart + L : xStart - L;
            g.DrawLine(p, xStart, y, xEnd, y);

            SizeF sz = g.MeasureString(text, DimFont);
            float tx = toRight ? (xEnd + 4) : (xEnd - sz.Width - 4);
            float ty = y - sz.Height / 2f;
            g.DrawString(text, DimFont, br, tx, ty);
        }

        private static void DrawArrow(Graphics g, Pen p, PointF at, PointF towards)
        {
            float dx = towards.X - at.X;
            float dy = towards.Y - at.Y;
            float len = (float)Math.Sqrt(dx * dx + dy * dy);
            if (len < 0.001f) return;

            dx /= len; dy /= len;

            float ax = -dy; // تدوير 90°
            float ay = dx;

            PointF p1 = new PointF(at.X + ax * Arrow, at.Y + ay * Arrow);
            PointF p2 = new PointF(at.X, at.Y);
            PointF p3 = new PointF(at.X - ax * Arrow, at.Y - ay * Arrow);

            g.DrawLine(p, p1, p2);
            g.DrawLine(p, p3, p2);
        }
    }
}
