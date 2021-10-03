using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;

namespace RebarSketch
{
    public partial class Form1 : Form
    {
        public string executionFolder;
        public string templateImagePath;
        public string curTempImage = "";

        private GlobalSettings sets;

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
            this.LoadAndActivatePicture();

            if (dataGridView1.Rows.Count == 0)
            {
                dataGridView1.Rows.Add("Арм_А", "000", "100", "200", "0");
            }
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

            templateImagePath = openDialog.FileName;
            executionFolder = System.IO.Path.GetDirectoryName(templateImagePath);

            this.ActivateControls();
            dataGridView1.Rows.Clear();

            string xmlConfigFilePath = System.IO.Path.Combine(executionFolder, "config.xml");
            XmlSketchItem xsi = null;
            if (System.IO.File.Exists(xmlConfigFilePath))
                xsi = XmlSketchItem.LoadFromXml(xmlConfigFilePath);
            else
                xsi = XmlSketchItem.LoadFromTxt(executionFolder);

            for (int i = 0; i < xsi.parameters.Count; i++)
            {
                ScetchParameter sp = xsi.parameters[i];
                dataGridView1.Rows.Add(sp.Name, i, sp.FontSize,
                    sp.PositionX, sp.PositionY, sp.Rotation, sp.IsNarrow, sp.LengthAccuracy, sp.MinValueForRound);
            }

            richTextBoxFamilies.Lines = xsi.families.ToArray();

            //this.RefreshImage();
        }

        private bool LoadAndActivatePicture()
        {


            //pictureBox1.Load(templateImagePath);

            return true;
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            XmlSketchItem xsi = CreateSketchItemByGrid();

            XmlSketchItem.Save(executionFolder, xsi);

            pictureBox1.Dispose();
            this.Close();
        }

        private void btnAddRow_Click(object sender, EventArgs e)
        {
            dataGridView1.Rows.Add("Арм_А", "000", "50", "100", "200", "0", false, 5, 20);
            btnDeleteRow.Enabled = true;
        }

        private void btnDeleteRow_Click(object sender, EventArgs e)
        {
            DataGridViewRow row = dataGridView1.Rows[dataGridView1.Rows.Count - 1];
            dataGridView1.Rows.Remove(row);

            if (dataGridView1.Rows.Count == 1)
            {
                btnDeleteRow.Enabled = false;
            }

        }


        private void btnRefresh_Click(object sender, EventArgs e)
        {
            this.RefreshImage();
        }

        private XmlSketchItem CreateSketchItemByGrid()
        {
            XmlSketchItem xsi = new XmlSketchItem();
            xsi.parameters = new List<ScetchParameter>();

            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                ScetchParameter sparam = new ScetchParameter();

                var cells = row.Cells;

                sparam.Name = row.Cells[0].Value.ToString();
                sparam.FontSize = float.Parse(cells[2].Value.ToString());
                sparam.PositionX = (float)cells[3].Value;
                sparam.PositionY = (float)cells[4].Value;
                sparam.Rotation = (float)cells[5].Value;
                sparam.IsNarrow = (bool)cells[6].Value;
                sparam.LengthAccuracy = (double)cells[7].Value;
                sparam.MinValueForRound = (double)cells[8].Value;

                xsi.parameters.Add(sparam);

                //writer.WriteLine(line);
            }

            xsi.families = new List<string>();

            foreach (string fam in richTextBoxFamilies.Lines)
            {
                xsi.families.Add(fam);
            }
            return xsi;
        }


        private void RefreshImage()
        {
            XmlSketchItem xsi = CreateSketchItemByGrid();

            //взять картинку
            //нанести на неё размеры
            //временно сохранить картинку
            string newTempImage = ScetchImage.GenerateTemporary(sets, templateImagePath, xsi.parameters);

            //вывести в форму
            pictureBox1.Load(newTempImage);

            if (curTempImage != "")
            {
                System.IO.File.Delete(curTempImage);
            }

            curTempImage = newTempImage;
        }

        private void ActivateControls()
        {
            btnAddRow.Enabled = true;
            btnDeleteRow.Enabled = true;
            btnRefresh.Enabled = true;
            dataGridView1.Enabled = true;
            richTextBoxFamilies.Enabled = true;
            btnSave.Enabled = true;
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            pictureBox1.Dispose();
            if (curTempImage != "")
            {
                System.IO.File.Delete(curTempImage);
            }
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
            this.RefreshImage();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("http://bim-starter.com/plugins/rebarsketch/");
        }
    }
}
