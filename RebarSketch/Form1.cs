using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;

namespace RebarSketch
{
    public partial class Form1 : Form
    {
        public string executionFolder;
        //public string templateImagePath;
        public string curTempImagePath = "";

        private GlobalSettings sets;
        private XmlSketchItem template;

        public Form1(GlobalSettings gsets)
        {
            InitializeComponent();
            executionFolder = App.libraryPath;
            sets = gsets;
            this.Text = "Редактор форм арматуры. Версия " +
                System.IO.File.GetLastWriteTime(System.Reflection.Assembly.GetExecutingAssembly().Location).ToString();
        }

        private void btnNewForm_Click(object sender, EventArgs e)
        {
            //this.LoadAndActivatePicture();

            if (dataGridView1.Rows.Count == 0)
            {
                dataGridView1.Rows.Add("Арм_А", "000", "100", "200", "0");
            }
            RefreshImage();
        }

        private void btnLoadTemplate_Click(object sender, EventArgs e)
        {
            OpenFileDialog openDialog = new OpenFileDialog();
            openDialog.InitialDirectory = executionFolder;
            openDialog.Title = "Выберите картинку";
            openDialog.Multiselect = false;
            openDialog.Filter = "PNG images(*.png)| *.png";

            if (openDialog.ShowDialog() != DialogResult.OK)
                return;

            string templateImagePath = openDialog.FileName;
            executionFolder = System.IO.Path.GetDirectoryName(templateImagePath);

            this.ActivateControls();
            dataGridView1.Rows.Clear();

            template = XmlSketchItem.Load(executionFolder);

            for (int i = 0; i < template.parameters.Count; i++)
            {
                ScetchParameter sp = template.parameters[i];
                sp.value = sp.Name;
                dataGridView1.Rows.Add(sp.Name, sp.Name, sp.FontSize,
                    sp.PositionX, sp.PositionY, sp.Rotation, sp.IsNarrow, sp.LengthAccuracy);
            }

            richTextBoxFamilies.Lines = template.families.ToArray();

            this.RefreshImage();
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            UpdateTemplateByGrid();

            template.Save();

            pictureBox1.Dispose();
            this.Close();
        }

        private void UpdateTemplateByGrid()
        {
            if (template == null) return;
            template.parameters = new List<ScetchParameter>();

            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                if (row.IsNewRow) continue;
                var cells = row.Cells;

                if (cells[0].Value == null) cells[0].Value = "Арм_А";
                if (cells[1].Value == null) cells[1].Value = "Арм_А";
                if (cells[2].Value == null) cells[2].Value = sets.defaultFontSize;
                if (cells[3].Value == null) cells[3].Value = 100;
                if (cells[4].Value == null) cells[4].Value = 200;
                if (cells[5].Value == null) cells[5].Value = 0;
                if (cells[6].Value == null) cells[6].Value = false;
                if (cells[7].Value == null) cells[7].Value = sets.defautLengthAccuracy;

                ScetchParameter sparam = new ScetchParameter();

                sparam.Name = row.Cells[0].Value.ToString();
                sparam.value = row.Cells[1].Value.ToString();
                sparam.FontSize = float.Parse(cells[2].Value.ToString());
                sparam.PositionX = float.Parse(cells[3].Value.ToString());
                sparam.PositionY = float.Parse(cells[4].Value.ToString());
                sparam.Rotation = float.Parse(cells[5].Value.ToString());
                sparam.IsNarrow = (bool)cells[6].Value;
                sparam.LengthAccuracy = double.Parse(cells[7].Value.ToString());

                template.parameters.Add(sparam);
            }

            template.families = new List<string>();

            foreach (string fam in richTextBoxFamilies.Lines)
            {
                template.families.Add(fam);
            }
        }


        private void RefreshImage()
        {
            if (template == null) return;
            
            //взять картинку
            //нанести на неё размеры
            //временно сохранить картинку
            string newTempImage = ScetchImage.GenerateTemporary(sets, template.templateImagePath, template.parameters);

            //вывести в форму
            pictureBox1.Load(newTempImage);

            if (!string.IsNullOrEmpty(curTempImagePath))
            {
                System.IO.File.Delete(curTempImagePath);
            }

            curTempImagePath = newTempImage;
        }

        private void ActivateControls()
        {
            dataGridView1.Enabled = true;
            richTextBoxFamilies.Enabled = true;
            btnSave.Enabled = true;
        }

        private void CloseAndDispose()
        {
            pictureBox1.Dispose();
            if (!string.IsNullOrEmpty(curTempImagePath))
            {
                System.IO.File.Delete(curTempImagePath);
            }
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            CloseAndDispose();
        }

        private void buttonOpenLibraryFolder_Click(object sender, EventArgs e)
        {
            if (System.IO.Directory.Exists(App.libraryPath))
            {
                System.Diagnostics.Process.Start(App.libraryPath);
            }
            else
            {
                MessageBox.Show("Не найдена папка " + App.libraryPath);
            }
        }

        private void buttonOpenConfigFile_Click(object sender, EventArgs e)
        {
            if (System.IO.File.Exists(App.configFilePath))
            {
                System.Diagnostics.Process.Start(App.configFilePath);
            }
            else
            {
                MessageBox.Show("Не найден файл " + App.configFilePath);
            }
        }

        private void buttonResetLibrary_Click(object sender, EventArgs e)
        {
            if (System.IO.File.Exists(App.configFilePath))
            {
                System.IO.File.Delete(App.configFilePath);
                MessageBox.Show("Настройки сброшены. При запуске Ведомости деталей будет повторен запрос пути к библиотеке.");
                this.Close();
            }
            else
            {
                MessageBox.Show("Не найден файл " + App.configFilePath);
            }
        }

        private void dataGridView1_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            UpdateTemplateByGrid();
            this.RefreshImage();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("http://bim-starter.com/plugins/rebarsketch/");
        }

        private void buttonSettings_Click(object sender, EventArgs e)
        {
            FormGlobalSettings formSettings = new FormGlobalSettings(sets);
            if (formSettings.ShowDialog() != DialogResult.OK)
                return;

            sets = formSettings.newSettings;
            GlobalSettings.Save(sets);
            if (template != null)
            {
                UpdateTemplateByGrid();
                RefreshImage();
            }
        }


        private void dataGridView1_UserAddedRow(object sender, DataGridViewRowEventArgs e)
        {
            UpdateTemplateByGrid();
            RefreshImage();
        }

        private void dataGridView1_UserDeletedRow(object sender, DataGridViewRowEventArgs e)
        {
            UpdateTemplateByGrid();
            RefreshImage();
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            CloseAndDispose();
        }
    }
}
