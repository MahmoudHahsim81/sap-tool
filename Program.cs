// ========= Program.cs =========
using System;
using System.Net;
using System.Windows.Forms;

namespace _3D_SAP
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            try { ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12; } catch { }

            // 1) لو مفيش توكن: افتح شاشة التفعيل
            if (!LicenseManager.HasLocalToken())
            {
                using (var dlg = new ActivationForm())
                {
                    var r = dlg.ShowDialog();
                    if (r != DialogResult.OK || !dlg.ActivatedOk)
                    {
                        MessageBox.Show("Activation required. Exiting.",
                            "Hashim Tool — License", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                        return;
                    }
                }
            }

            // 2) لازم يتحقق أونلاين عند كل تشغيل
            var res = LicenseManager.RequireOnlineValidTokenAsync().GetAwaiter().GetResult();
            if (!res.ok)
            {
                // امسح التوكن عشان حتى لو النت فصل بعد كده مايفتحش
                LicenseManager.ClearToken();

                MessageBox.Show(
                    "Online license validation failed:\n" + res.reason +
                    "\n\nPlease connect to the internet and try again.",
                    "Hashim Tool — License",
                    MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            Application.Run(new Form1());
        }
    }
}
