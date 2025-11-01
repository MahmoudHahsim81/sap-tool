using System;
using System.Drawing;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;

public class ActivationForm : Form
{
    private Label lblHeader;
    private Label lblSubHeader;

    private TextBox txtMachine;
    private Button btnCopy;
    private Label lblMachine;

    private TextBox txtKey;
    private Button btnPaste;
    private Label lblKey;

    private Button btnActivate;
    private Label lblStatus;

    public bool ActivatedOk { get; private set; }

    public ActivationForm()
    {
        InitializeComponent();
        // قيم البداية
        txtMachine.Text = LicenseManager.MachineId();
        lblStatus.Text = "أدخل مفتاح الترخيص لتفعيل الأداة.";
    }

    private void InitializeComponent()
    {
        // Form
        Text = "Activate License";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(560, 300);
        Font = new Font("Segoe UI", 10);
        BackColor = Color.White;
        AcceptButton = btnActivate; // هيتحدد بعد إنشاء الزر

        // Header panel
        var header = new Panel
        {
            Dock = DockStyle.Top,
            Height = 70,
            BackColor = Color.FromArgb(0x1E, 0x88, 0xE5) // أزرق جميل
        };
        Controls.Add(header);

        lblHeader = new Label
        {
            AutoSize = true,
            ForeColor = Color.White,
            Font = new Font("Segoe UI Semibold", 16, FontStyle.Bold),
            Text = "Hashim Tool — Activation",
            Location = new Point(16, 10)
        };
        header.Controls.Add(lblHeader);

        lblSubHeader = new Label
        {
            AutoSize = true,
            ForeColor = Color.FromArgb(230, 245, 255),
            Font = new Font("Segoe UI", 9),
            Text = "Please activate to continue. Internet connection is required.",
            Location = new Point(20, 42)
        };
        header.Controls.Add(lblSubHeader);

        // محتوى
        var pad = 18;
        var top = header.Bottom + 14;

        // Machine ID
        lblMachine = new Label
        {
            AutoSize = true,
            Text = "Machine ID:",
            Location = new Point(pad, top)
        };
        Controls.Add(lblMachine);

        txtMachine = new TextBox
        {
            Location = new Point(pad, top + 22),
            Width = 410,
            ReadOnly = true,
            BorderStyle = BorderStyle.FixedSingle
        };
        Controls.Add(txtMachine);

        btnCopy = MakeSmallButton("Copy", new Point(txtMachine.Right + 8, txtMachine.Top - 1));
        btnCopy.Click += (s, e) =>
        {
            try
            {
                Clipboard.SetText(txtMachine.Text ?? "");
                ShowInfo("Machine ID copied to clipboard.");
            }
            catch { ShowError("Couldn't copy Machine ID."); }
        };
        Controls.Add(btnCopy);

        // License Key
        top = txtMachine.Bottom + 16;
        lblKey = new Label
        {
            AutoSize = true,
            Text = "License Key:",
            Location = new Point(pad, top)
        };
        Controls.Add(lblKey);

        txtKey = new TextBox
        {
            Location = new Point(pad, top + 22),
            Width = 410,
            BorderStyle = BorderStyle.FixedSingle,
        };
        Controls.Add(txtKey);

        btnPaste = MakeSmallButton("Paste", new Point(txtKey.Right + 8, txtKey.Top - 1));
        btnPaste.Click += (s, e) =>
        {
            try
            {
                if (Clipboard.ContainsText())
                    txtKey.Text = Clipboard.GetText().Trim();
            }
            catch { }
        };
        Controls.Add(btnPaste);

        // Activate
        btnActivate = new Button
        {
            Text = "Activate",
            Location = new Point(pad, txtKey.Bottom + 18),
            Width = 140,
            Height = 36,
            BackColor = Color.FromArgb(0x1E, 0x88, 0xE5),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        btnActivate.FlatAppearance.BorderSize = 0;
        btnActivate.Cursor = Cursors.Hand;
        btnActivate.Click += async (s, e) => await DoActivateAsync();
        Controls.Add(btnActivate);
        AcceptButton = btnActivate;

        // Status line
        lblStatus = new Label
        {
            AutoSize = false,
            Height = 24,
            Width = ClientSize.Width - 2 * pad,
            Location = new Point(pad, btnActivate.Bottom + 12),
            ForeColor = Color.DimGray
        };
        Controls.Add(lblStatus);
    }

    private Button MakeSmallButton(string text, Point location)
    {
        return new Button
        {
            Text = text,
            Location = location,
            Width = 90,
            Height = 28,
            BackColor = Color.White,
            FlatStyle = FlatStyle.Standard
        };
    }

    private async Task DoActivateAsync()
    {
        try
        {
            var key = (txtKey.Text ?? "").Trim();
            if (string.IsNullOrWhiteSpace(key))
            {
                ShowError("Please enter the license key.");
                txtKey.Focus();
                return;
            }

            ToggleUi(false);
            ShowInfo("Activating... please wait.");

            var (ok, msg) = await LicenseManager.ActivateOnlineAsync(key);
            if (!ok)
            {
                ShowError(msg);
                return;
            }

            ActivatedOk = true;
            ShowSuccess("Activated successfully. Thank you!");
            await Task.Delay(600);
            Close();
        }
        catch (HttpRequestException ex)
        {
            var msg = "Network error: " + ex.Message;
            if (ex.InnerException != null) msg += "\nInner: " + ex.InnerException.Message;
            MessageBox.Show(msg, "Activation", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }
        catch (Exception ex)
        {
            MessageBox.Show("Unexpected error: " + ex.Message, "Activation",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }
    }

    private void ToggleUi(bool on)
    {
        txtKey.Enabled = on;
        btnPaste.Enabled = on;
        btnActivate.Enabled = on;
        Cursor = on ? Cursors.Default : Cursors.WaitCursor;
    }

    private void ShowInfo(string text)
    {
        lblStatus.Text = text;
        lblStatus.ForeColor = Color.DimGray;
    }

    private void ShowError(string text)
    {
        lblStatus.Text = text;
        lblStatus.ForeColor = Color.FromArgb(200, 40, 40);
        System.Media.SystemSounds.Exclamation.Play();
    }

    private void ShowSuccess(string text)
    {
        lblStatus.Text = text;
        lblStatus.ForeColor = Color.FromArgb(40, 140, 60);
        System.Media.SystemSounds.Asterisk.Play();
    }
}
