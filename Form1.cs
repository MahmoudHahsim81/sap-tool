using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using SAP2000v1;

namespace _3D_SAP
{
    public partial class Form1  Form
    {
        public Form1()
    {
        InitializeComponent();
        this.TopMost = true; دايمًا فوق

             ربط الأحداث(بدون RHS)
            this.button1.Click += button1_Click; IPE
            this.button3.Click += button3_Click; HEA
            this.button4.Click += button4_Click; HEB
        }

         ================== SAP2000 Integration ==================
        private cHelper _helper;
    private cOAPI _oapi;
    private cSapModel _model;

    اتشبك على SAP شغال أو افتح واحد جديد
        private void EnsureSapAttached()
    {
        if (_model != null) return;

        _helper = new Helper();

        try
        {
            لو SAP شغال، اتشبك عليه
                _oapi = _helper.GetObject(CSI.SAP2000.API.SapObject);
        }
        catch
        {
            لو مش شغال، افتحه
           _oapi = _helper.CreateObjectProgID(CSI.SAP2000.API.SapObject);
            _oapi.ApplicationStart();
        }

        _model = _oapi.SapModel  throw new Exception(تعذّر الوصول إلى SapModel.);
    }

    يتحقق أن القطاع مُعرّف في المشروع
        private bool SectionExists(string sectionName)
    {
        int n = 0;
        string[] names = { };
        int ret = _model.PropFrame.GetNameList(ref n, ref names);
        if (ret != 0) return false;

        return names != null &&
               names.Any(s = string.Equals(s, sectionName, StringComparison.OrdinalIgnoreCase));
    }

    يحاول تعديل مادة القطاع(مش كل الأنواع تسمح)
        private void TrySetSectionMaterial(string sectionName, string materialName)
    {
        if (string.IsNullOrWhiteSpace(materialName)) return;
        _model.PropFrame.SetMaterial(sectionName, materialName);
    }
         =========================================================

         ===== أحداث غير مستخدمة حالياً(سايبها زي ما هي) =====
        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e) { }
    private void label1_Click(object sender, EventArgs e) { }
    private void label2_Click(object sender, EventArgs e) { }
    private void label5_Click(object sender, EventArgs e) { }
    private void label4_Click(object sender, EventArgs e) { }
    private void label3_Click(object sender, EventArgs e) { }
    private void label6_Click(object sender, EventArgs e) { }
    private void label4_Click_1(object sender, EventArgs e) { }
    private void listBox1_SelectedIndexChanged(object sender, EventArgs e) { }
    private void textBox6_TextChanged(object sender, EventArgs e) { }
    private void label8_Click(object sender, EventArgs e) { }
    private void label14_Click(object sender, EventArgs e) { }
    private void label9_Click(object sender, EventArgs e) { }
    private void label10_Click(object sender, EventArgs e) { }
    private void label11_Click(object sender, EventArgs e) { }
    private void label12_Click(object sender, EventArgs e) { }
    private void label13_Click(object sender, EventArgs e) { }
    private void label8_Click_1(object sender, EventArgs e) { }
    private void folderBrowserDialog1_HelpRequest(object sender, EventArgs e) { }
    private void splitContainer1_Panel1_Paint(object sender, PaintEventArgs e) { }
    private void fileSystemWatcher1_Changed(object sender, System.IO.FileSystemEventArgs e) { }
    private void label3_Click_1(object sender, EventArgs e) { }
    private void label8_Click_2(object sender, EventArgs e) { }
    private void label9_Click_1(object sender, EventArgs e) { }
    private void textBox5_TextChanged(object sender, EventArgs e) { }
    private void textBox8_TextChanged(object sender, EventArgs e) { }
    private void comboBox7_SelectedIndexChanged(object sender, EventArgs e) { }
    private void textBox6_TextChanged_1(object sender, EventArgs e) { }
    private void comboBox6_SelectedIndexChanged(object sender, EventArgs e) { }
    private void comboBox10_SelectedIndexChanged(object sender, EventArgs e) { }
    private void textBox14_TextChanged(object sender, EventArgs e) { }
    private void comboBox2_SelectedIndexChanged(object sender, EventArgs e) { }
    private void textBox15_TextChanged(object sender, EventArgs e) { }
    private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e) { }
    private void tabPage1_Click(object sender, EventArgs e) { }
    private void timer1_Tick(object sender, EventArgs e) { }
    private void timer2_Tick(object sender, EventArgs e) { }
    private void bindingNavigatorAddNewItem_Click(object sender, EventArgs e) { }
    private void toolStripContainer1_ContentPanel_Load(object sender, EventArgs e) { }
    private void statusStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e) { }
    private void textBox1_TextChanged(object sender, EventArgs e) { }
    private void comboBox9_SelectedIndexChanged(object sender, EventArgs e) { }
    private void comboBox3_SelectedIndexChanged(object sender, EventArgs e) { }

         ===== زر Assign(IPE) =====
        private void button1_Click(object sender, EventArgs e)
    {
        try
        {
            EnsureSapAttached();

            string sectionName = comboBox7.Text.Trim(); اسم القطاع
                string materialName = comboBox1.Text.Trim(); الماتريال

                if (string.IsNullOrWhiteSpace(sectionName))
                throw new Exception(اختار اسم القطاع من comboBox7.);

            if (!SectionExists(sectionName))
                throw new Exception($القطاع '{sectionName}' غير موجود بالمشروع.);

            if (!string.IsNullOrWhiteSpace(materialName))
                TrySetSectionMaterial(sectionName, materialName);

            int ret = _model.FrameObj.SetSection(ALL, sectionName, eItemType.SelectedObjects);
            if (ret != 0) throw new Exception($SAP رجّع كود خطأ أثناء الإسناد { ret });

            MessageBox.Show(تم الإسناد بنجاح للعناصر المحددة., تم,
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, خطأ, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

         ===== زر Assign HEA =====
        private void button3_Click(object sender, EventArgs e)
    {
        try
        {
            EnsureSapAttached();

            string sectionName = comboBox9.Text.Trim();
            string materialName = comboBox3.Text.Trim();

            if (string.IsNullOrWhiteSpace(sectionName))
                throw new Exception(اختار اسم القطاع من comboBox9.);

            if (!SectionExists(sectionName))
                throw new Exception($القطاع '{sectionName}' غير موجود بالمشروع.);

            if (!string.IsNullOrWhiteSpace(materialName))
                TrySetSectionMaterial(sectionName, materialName);

            int ret = _model.FrameObj.SetSection(ALL, sectionName, eItemType.SelectedObjects);
            if (ret != 0) throw new Exception($SAP رجّع كود خطأ أثناء الإسناد { ret });

            MessageBox.Show(تم الإسناد بنجاح للعناصر المحددة., تم,
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, خطأ, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

         ===== زر Assign HEB =====
        private void button4_Click(object sender, EventArgs e)
    {
        try
        {
            EnsureSapAttached();

            string sectionName = comboBox8.Text.Trim();
            string materialName = comboBox4.Text.Trim();

            if (string.IsNullOrWhiteSpace(sectionName))
                throw new Exception(اختار اسم القطاع من comboBox8.);

            if (!SectionExists(sectionName))
                throw new Exception($القطاع '{sectionName}' غير موجود بالمشروع.);

            if (!string.IsNullOrWhiteSpace(materialName))
                TrySetSectionMaterial(sectionName, materialName);

            int ret = _model.FrameObj.SetSection(ALL, sectionName, eItemType.SelectedObjects);
            if (ret != 0) throw new Exception($SAP رجّع كود خطأ أثناء الإسناد { ret });

            MessageBox.Show(تم الإسناد بنجاح للعناصر المحددة., تم,
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, خطأ, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void button5_Click(object sender, EventArgs e)
    {
        (SHSRHS غير مفعّل هنا بناءً على طلبك)
        }
}
}
