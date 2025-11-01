using System;
using System.Drawing;
using System.Windows.Forms;

namespace _3D_SAP
{
    public sealed class NonPrsmPreviewForm : Form
    {
        private readonly PictureBox _pbTaper;   // الجزء المائل (Start/End depth)
        private readonly PictureBox _pbI;       // قطاع I مع الأبعاد
        private readonly Button _btnRefresh;
        private readonly Panel _bottomBar;
        private readonly Form _owner;

        public NonPrsmPreviewForm(Form owner)
        {
            _owner = owner;

            Text = "Non-Prismatic Preview";
            StartPosition = FormStartPosition.CenterParent;
            TopMost = true;
            ShowInTaskbar = false;
            ShowIcon = false;
            FormBorderStyle = FormBorderStyle.Sizable;
            MinimumSize = new Size(800, 520);
            DoubleBuffered = true;
            KeyPreview = true;

            _pbTaper = new PictureBox
            {
                BackColor = Color.Transparent,
                SizeMode = PictureBoxSizeMode.Normal,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Height = 180,
                Left = 24,
                Top = 20
            };

            _pbI = new PictureBox
            {
                BackColor = Color.Transparent,
                SizeMode = PictureBoxSizeMode.Normal,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
            };

            _bottomBar = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 44,
                BackColor = SystemColors.Control
            };

            _btnRefresh = new Button
            {
                Text = "Refresh",
                Dock = DockStyle.Fill,
                Height = 32,
                Margin = new Padding(120, 6, 120, 6)
            };

            _bottomBar.Controls.Add(_btnRefresh);

            Controls.Add(_pbTaper);
            Controls.Add(_pbI);
            Controls.Add(_bottomBar);

            Load += OnLoad;
            Resize += OnResizeRedraw;
            _btnRefresh.Click += (s, e) => Redraw();
            KeyDown += NonPrsmPreviewForm_KeyDown;

            AcceptButton = _btnRefresh;

            LayoutNow();
        }

        private void NonPrsmPreviewForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape) Close();
            else if (e.KeyCode == Keys.F5 || (e.KeyCode == Keys.Enter && !e.Shift && !e.Control && !e.Alt))
                Redraw();
        }

        private void OnLoad(object sender, EventArgs e)
        {
            LayoutNow();
            Redraw();
        }

        private void OnResizeRedraw(object sender, EventArgs e)
        {
            LayoutNow();
            Redraw();
        }

        private void LayoutNow()
        {
            int margin = 24;
            _bottomBar.Height = 44;

            _pbTaper.Left = margin;
            _pbTaper.Width = ClientSize.Width - 2 * margin;

            _pbI.Left = margin;
            _pbI.Top = _pbTaper.Bottom + 24;
            _pbI.Width = ClientSize.Width - 2 * margin;
            _pbI.Height = ClientSize.Height - _pbI.Top - _bottomBar.Height - 16;
        }

        private void Redraw()
        {
            string T(string name)
            {
                if (_owner == null) return "";
                var ctrl = FindIn(_owner, name) as TextBox;
                return ctrl?.Text?.Trim() ?? "";
            }

            Control FindIn(Control parent, string name)
            {
                foreach (Control c in parent.Controls)
                {
                    if (string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase))
                        return c;
                    var sub = FindIn(c, name);
                    if (sub != null) return sub;
                }
                return null;
            }

            double.TryParse(T("textBox19"), out double startDepth);
            double.TryParse(T("textBox18"), out double endDepth);

            int.TryParse(T("textBox4"), out int bTop);
            int.TryParse(T("textBox16"), out int tfTop);
            int.TryParse(T("textBox2"), out int tw);
            int.TryParse(T("textBox3"), out int bBot);
            int.TryParse(T("textBox17"), out int tfBot);

            bool dark = false; // دايمًا فاتح

            SectionPreview02.DrawTaper(_pbTaper, dark, startDepth, endDepth);
            SectionPreview02.DrawI(_pbI, dark,
                Math.Max(startDepth, endDepth),
                bTop, tfTop, tw, bBot, tfBot,
                showDims: true);
        }
    }
}
