using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace _3D_SAP
{
    public class PreviewForm : Form
    {
        private readonly Form _owner;     // Form1 فعليًا
        private PictureBox _pb;
        private Button _btnRefresh;

        public PreviewForm(Form owner)
        {
            _owner = owner;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            Text = "Quick Preview";
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(560, 440);

            // إظهار بدون أيقونة وعلى القمة
            ShowIcon = false;
            TopMost = true;

            _pb = new PictureBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                SizeMode = PictureBoxSizeMode.CenterImage
            };

            _btnRefresh = new Button
            {
                Dock = DockStyle.Bottom,
                Height = 36,
                Text = "Refresh"
            };
            _btnRefresh.Click += (s, e) => Draw();

            Controls.Add(_pb);
            Controls.Add(_btnRefresh);

            Shown += (s, e) => Draw();
            Resize += (s, e) => Draw();
        }

        private void Draw()
        {
            var m = GetModelFromOwner();

            // دايمًا خلفية فاتحة
            bool dark = false;

            // امسح الصورة القديمة
            if (_pb.Image != null)
            {
                var old = _pb.Image;
                _pb.Image = null;
                old.Dispose();
            }

            switch (m.Kind)
            {
                case PreviewKind.ISection:
                    SectionPreview.DrawI(_pb, dark, m.H, m.BTop, m.TfTop, m.Tw, m.BBot, m.TfBot);
                    break;

                case PreviewKind.RHS:
                    SectionPreview.DrawRHS(_pb, dark, m.Depth, m.Width, m.T);
                    break;

                default:
                    Bitmap bmp = new Bitmap(_pb.Width, _pb.Height);
                    using (Graphics g = Graphics.FromImage(bmp))
                    {
                        g.Clear(Color.White);

                        using (SolidBrush br = new SolidBrush(Color.Black))
                        using (StringFormat sf = new StringFormat
                        {
                            Alignment = StringAlignment.Center,
                            LineAlignment = StringAlignment.Center
                        })
                        {
                            g.DrawString("No preview data", Font, br,
                                         new Rectangle(0, 0, bmp.Width, bmp.Height), sf);
                        }
                    }
                    _pb.Image = bmp;
                    break;

            }
        }

        private PreviewModel GetModelFromOwner()
        {
            try
            {
                var mi = _owner.GetType().GetMethod("GetPreviewModel",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                if (mi != null && mi.Invoke(_owner, null) is PreviewModel pm)
                    return pm;
            }
            catch { }
            return new PreviewModel { Kind = PreviewKind.None };
        }
    }
}
