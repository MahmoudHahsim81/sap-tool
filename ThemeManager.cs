using System.Drawing;
using System.Windows.Forms;

namespace _3D_SAP
{
    // ================== Theme Manager (Light/Dark) ==================
    public enum AppTheme { Light, Dark }

    public static class ThemeManager
    {
        public static AppTheme Current { get; private set; } = AppTheme.Light;

        private struct Palette
        {
            public Color Back, Fore, CtrlBack, Accent, Border, Grid;
        }

        private static Palette Light => new Palette
        {
            Back = Color.White,
            Fore = Color.Black,
            CtrlBack = SystemColors.Control,
            Accent = Color.FromArgb(0, 120, 215),
            Border = Color.Silver,
            Grid = Color.Gainsboro
        };

        private static Palette Dark => new Palette
        {
            Back = Color.FromArgb(45, 45, 48),
            Fore = Color.White,
            CtrlBack = Color.FromArgb(28, 28, 28),
            Accent = Color.FromArgb(0, 122, 204),
            Border = Color.FromArgb(70, 70, 70),
            Grid = Color.FromArgb(64, 64, 64)
        };

        public static void SetTheme(AppTheme theme) => Current = theme;

        // طبّق على فورم كامل
        public static void Apply(Form form)
        {
            var p = (Current == AppTheme.Dark) ? Dark : Light;

            form.BackColor = p.Back;
            form.ForeColor = p.Fore;

            foreach (Control c in form.Controls)
                ApplyToControl(c, p);

            if (form.MainMenuStrip != null)
                ApplyToolStrip(form.MainMenuStrip, p);
        }

        // طبّق على كل الفورمات المفتوحة
        public static void ApplyAllOpenForms()
        {
            foreach (Form f in Application.OpenForms) Apply(f);
        }

        // ---------- Core ----------
        private static void ApplyToControl(Control c, Palette p)
        {
            switch (c)
            {
                // ToolStrips (الأكثر تحديدًا أولًا)
                case StatusStrip ss:
                    ApplyToolStrip(ss, p);
                    break;

                case MenuStrip ms:
                    ApplyToolStrip(ms, p);
                    break;

                case ToolStrip ts:
                    ApplyToolStrip(ts, p);
                    break;

                // حاويات مشتقة من Panel أولًا
                case FlowLayoutPanel _:
                case TableLayoutPanel _:
                    c.BackColor = p.Back;
                    c.ForeColor = p.Fore;
                    break;

                case SplitContainer sc:
                    sc.BackColor = p.Back;
                    sc.ForeColor = p.Fore;
                    sc.Panel1.BackColor = p.Back;
                    sc.Panel2.BackColor = p.Back;
                    break;

                // ثم الحاويات العامة (رتّب المشتقات قبل Panel)
                case TabPage _:
                case GroupBox _:
                case Panel _:
                    c.BackColor = p.Back;
                    c.ForeColor = p.Fore;
                    break;

                case TabControl tc:
                    tc.BackColor = p.Back;
                    tc.ForeColor = p.Fore;
                    foreach (TabPage tp in tc.TabPages) ApplyToControl(tp, p);
                    break;

                // Buttons
                case Button btn:
                    // سيب الأزرار المخصصة لو Tag="keep"
                    if ((btn.Tag as string) == "keep") break;

                    // لو زرار افتراضي فقط يتلوّن
                    if (btn.BackColor == SystemColors.Control || btn.UseVisualStyleBackColor)
                    {
                        btn.BackColor = p.CtrlBack;
                        btn.ForeColor = p.Fore;
                    }
                    btn.FlatStyle = FlatStyle.Flat;
                    btn.FlatAppearance.BorderColor = p.Border;
                    break;

                // Text inputs
                case TextBox txt:
                    txt.BackColor = (Current == AppTheme.Dark) ? Color.FromArgb(30, 30, 30) : Color.White;
                    txt.ForeColor = p.Fore;
                    txt.BorderStyle = BorderStyle.FixedSingle;
                    break;

                case MaskedTextBox mtxt:
                    mtxt.BackColor = (Current == AppTheme.Dark) ? Color.FromArgb(30, 30, 30) : Color.White;
                    mtxt.ForeColor = p.Fore;
                    break;

                case RichTextBox rtb:
                    rtb.BackColor = (Current == AppTheme.Dark) ? Color.FromArgb(30, 30, 30) : Color.White;
                    rtb.ForeColor = p.Fore;
                    break;

                // Selectors
                case ComboBox cb:
                    cb.BackColor = (Current == AppTheme.Dark) ? Color.FromArgb(30, 30, 30) : Color.White;
                    cb.ForeColor = p.Fore;
                    break;

                case NumericUpDown nud:
                    nud.BackColor = (Current == AppTheme.Dark) ? Color.FromArgb(30, 30, 30) : Color.White;
                    nud.ForeColor = p.Fore;
                    break;

                case DateTimePicker dtp:
                    dtp.CalendarMonthBackground = (Current == AppTheme.Dark) ? Color.FromArgb(30, 30, 30) : Color.White;
                    dtp.CalendarForeColor = p.Fore;
                    dtp.BackColor = p.Back;
                    dtp.ForeColor = p.Fore;
                    break;

                // Labels & checks — رتب LinkLabel قبل Label
                case CheckBox _:
                case RadioButton _:
                case LinkLabel _:
                case Label _:
                case ListBox _:
                    c.ForeColor = p.Fore;
                    c.BackColor = p.Back;
                    break;

                // Lists/Trees
                case ListView lv:
                    lv.BackColor = (Current == AppTheme.Dark) ? Color.FromArgb(30, 30, 30) : Color.White;
                    lv.ForeColor = p.Fore;
                    lv.BorderStyle = BorderStyle.FixedSingle;
                    break;

                case TreeView tv:
                    tv.BackColor = (Current == AppTheme.Dark) ? Color.FromArgb(30, 30, 30) : Color.White;
                    tv.ForeColor = p.Fore;
                    break;

                // Grid
                case DataGridView dgv:
                    ApplyGrid(dgv, p);
                    break;
            }

            // Recursive
            foreach (Control child in c.Controls)
                ApplyToControl(child, p);
        }

        private static void ApplyToolStrip(ToolStrip ts, Palette p)
        {
            ts.RenderMode = ToolStripRenderMode.System;
            ts.BackColor = p.Back;
            ts.ForeColor = p.Fore;

            foreach (ToolStripItem it in ts.Items)
            {
                it.ForeColor = p.Fore;

                if (it is ToolStripDropDownItem ddi && ddi.HasDropDownItems)
                {
                    ddi.DropDown.BackColor = p.Back;
                    ddi.DropDown.ForeColor = p.Fore;
                    foreach (ToolStripItem sub in ddi.DropDown.Items)
                        sub.ForeColor = p.Fore;
                }
            }
        }

        private static void ApplyGrid(DataGridView g, Palette p)
        {
            g.BackgroundColor = p.Back;
            g.GridColor = p.Grid;
            g.EnableHeadersVisualStyles = false;

            g.ColumnHeadersDefaultCellStyle.BackColor = p.CtrlBack;
            g.ColumnHeadersDefaultCellStyle.ForeColor = p.Fore;
            g.RowHeadersDefaultCellStyle.BackColor = p.CtrlBack;
            g.RowHeadersDefaultCellStyle.ForeColor = p.Fore;

            g.DefaultCellStyle.BackColor = (Current == AppTheme.Dark) ? Color.FromArgb(30, 30, 30) : Color.White;
            g.DefaultCellStyle.ForeColor = p.Fore;
            g.DefaultCellStyle.SelectionBackColor = p.Accent;
            g.DefaultCellStyle.SelectionForeColor = Color.White;
        }
    }
}
