using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace _3D_SAP
{
    public class NP3PreviewForm : Form
    {
        private Func<NP3Spec> _getSpec;   // دالة تجيب القيم الحالية من الشاشة
        private NP3Spec _spec;            // آخر نسخة

        private readonly Button _btnRefresh = new Button();

        public NP3PreviewForm(Func<NP3Spec> getSpec)
        {
            if (getSpec == null) throw new ArgumentNullException("getSpec");
            _getSpec = getSpec;
            _spec = _getSpec();

            Text = "Preview – " + _spec.Name;
            MinimumSize = new Size(720, 360);
            BackColor = Color.White;
            DoubleBuffered = true;

            // زر ريفريش
            _btnRefresh.Text = "Refresh";
            _btnRefresh.Dock = DockStyle.Bottom;
            _btnRefresh.Height = 36;
            _btnRefresh.Click += delegate
            {
                var s = _getSpec();
                if (s != null)
                {
                    _spec = s;
                    Text = "Preview – " + _spec.Name;
                    Invalidate();
                }
            };
            Controls.Add(_btnRefresh);

            Resize += (o, e) => Invalidate();
            Paint += OnPaintPreview;
        }

        // يُستدعى من الفورم الأساسي لتغيير دالة القراءة أثناء الحياة
        public void RefreshWith(Func<NP3Spec> getSpec)
        {
            if (getSpec != null) _getSpec = getSpec;
            var s = _getSpec();
            if (s != null) { _spec = s; Text = "Preview – " + _spec.Name; Invalidate(); }
        }

        private void OnPaintPreview(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;

            // خلفية خفيفة (لو الفورم مش متغيّر خلفيته في الـctor)
            this.BackColor = Color.FromArgb(244, 245, 247); // #F4F5F7

            // مساحة الرسم (اترك مكان لزر Refresh إن وجد)
            Rectangle rc = this.ClientRectangle;
            int refreshH = 36; // اضبطها حسب ارتفاع الزر عندك، أو 0 لو مفيش زر
            rc = new Rectangle(rc.Left, rc.Top, rc.Width, rc.Height - refreshH);

            const int pad = 24;
            RectangleF box = RectangleF.Inflate(rc, -pad, -pad - 6);

            // ===== Level 0 (الخط العلوي) =====
            float topY = box.Top + 6;

            // أكبر ارتفاع للمقياس الرأسي
            double maxH = Math.Max(_spec.H1, Math.Max(_spec.H2, _spec.H3));
            if (maxH <= 0) maxH = 1;

            // المسافة المتاحة أسفل Level 0
            float vSpace = box.Bottom - topY - 6;

            // خرائط X,Y
            Func<double, float> X = t => (float)(box.Left + t * box.Width);
            Func<double, float> Y = h => topY + (float)((h / maxH) * vSpace); // قيم موجبة ترسم لأسفل

            // نسب تراكمية
            double x0 = 0.0, x1 = _spec.R1, x2 = _spec.R1 + _spec.R2, x3 = 1.0;

            // القمة (أفقي عند Level 0)
            PointF p0 = new PointF(X(x0), topY);
            PointF p1 = new PointF(X(x1), topY);
            PointF p2 = new PointF(X(x2), topY);
            PointF p3 = new PointF(X(x3), topY);

            // القاع: ميل (H1→H2) ثم أفقي على H2 ثم ميل (H2→H3)
            PointF b0 = new PointF(X(x0), Y(_spec.H1));
            PointF b1 = new PointF(X(x1), Y(_spec.H2));
            PointF b2 = new PointF(X(x2), Y(_spec.H2));
            PointF b3 = new PointF(X(x3), Y(_spec.H3));

            // تعبئة الجسم (أزرق فاتح) – ممكن تشيلها لو عايز كونتور فقط
            var gp = new System.Drawing.Drawing2D.GraphicsPath();
            gp.AddPolygon(new[] { p0, p3, b3, b2, b1, b0 });
            var fill = new SolidBrush(Color.FromArgb(169, 210, 242)); // #A9D2F2
            g.FillPath(fill, gp);
            fill.Dispose();
            gp.Dispose();

            // كونتور أزرق غامق
            var pen = new Pen(Color.FromArgb(11, 98, 161), 3f); // #0B62A1
            g.DrawLine(pen, p0, p3);                   // أعلى
            g.DrawLine(pen, p0, b0);                   // يسار
            g.DrawLines(pen, new[] { b0, b1, b2, b3 }); // قاع: ميل-أفقي-ميل
            g.DrawLine(pen, b3, p3);                   // يمين
            pen.Dispose();

            // (اختياري) خطوط فواصل عند R1 و R1+R2
            var dash = new Pen(Color.FromArgb(154, 164, 178), 1.5f); // رمادي #9AA4B2
            dash.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
            g.DrawLine(dash, X(x1), box.Top, X(x1), box.Bottom);
            g.DrawLine(dash, X(x2), box.Top, X(x2), box.Bottom);
            dash.Dispose();
        }

    }
}