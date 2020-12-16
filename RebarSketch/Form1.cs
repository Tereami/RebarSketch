using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RebarSketch
{
    public partial class Form1 : Form
    {
        public string executionFolder;
        public string templateImagePath;
        public string curTempImage = "";

        public Form1()
        {
            InitializeComponent();
            executionFolder = SupportSettings.libraryPath;
            this.Text = "Редактор форм арматуры. Версия " + System.IO.File.GetLastWriteTime(System.Reflection.Assembly.GetExecutingAssembly().Location).ToString();
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
            bool checkDialog = this.LoadAndActivatePicture();
            if (!checkDialog) return;

            dataGridView1.Rows.Clear();

            string paramsFile = System.IO.Path.Combine(executionFolder, "parameters.txt");
            bool check = System.IO.File.Exists(paramsFile);
            if (!check) throw new Exception("Файл параметров не найден в папке с картинкой");


            ScetchTemplate st = new ScetchTemplate();


            string[] paramsArray = FileSupport.ReadFileWithAnyDecoding(paramsFile);


            st.parameters = new List<ScetchParameter>();
            foreach (string p in paramsArray)
            {
                if (p.StartsWith("#")) continue;
                string[] paramInfo = p.Split(',');
                string paramName = paramInfo[0];
                string posX = paramInfo[1];
                string posY = paramInfo[2];
                string r = paramInfo[3];

                bool needsWrap = false;
                if(paramInfo.Length >4)
                {
                    if(paramInfo[4] == "1")
                    {
                        needsWrap = true;
                    }
                }

                dataGridView1.Rows.Add(paramName, paramName, posX, posY, r, needsWrap);
            }

            this.RefreshImage();
            btnDeleteRow.Enabled = true;
        }

        private bool LoadAndActivatePicture()
        {
            OpenFileDialog openDialog = new OpenFileDialog();
            openDialog.InitialDirectory = executionFolder;
            openDialog.Title = "Выберите картинку";
            openDialog.Multiselect = false;
            openDialog.Filter = "PNG images(*.png)| *.png";

            if (openDialog.ShowDialog() != DialogResult.OK) return false;

            templateImagePath = openDialog.FileName;
            executionFolder = System.IO.Path.GetDirectoryName(templateImagePath);

            pictureBox1.Load(templateImagePath);


            this.ActivateControls();
            return true;
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            string paramsFileName = System.IO.Path.Combine(executionFolder, "parameters.txt");
            System.IO.File.Delete(paramsFileName);

            //SaveFileDialog saveDialog = new SaveFileDialog();
            //saveDialog.AddExtension = true;
            //saveDialog.DefaultExt = "txt";
            //saveDialog.Filter = "TXT file(*.txt)| *.txt";
            //if (saveDialog.ShowDialog() != DialogResult.OK) return;

            //string txtFile = saveDialog.FileName;
            System.IO.StreamWriter writer = System.IO.File.CreateText(paramsFileName);

            writer.WriteLine("#Имя параметра,отступ слева,отступ сверху,угол поворота");

            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                string line = "";
                line += row.Cells[0].Value.ToString() + ",";
                line += row.Cells[2].Value.ToString() + ",";
                line += row.Cells[3].Value.ToString() + ",";
                line += row.Cells[4].Value.ToString();

                bool needsWrap = (bool)row.Cells[5].Value;
                if(needsWrap) line += "," + "1";
                else line += "," + "0";


                writer.WriteLine(line);
            }

            writer.Close();

            pictureBox1.Dispose();
            dataGridView1.Rows.Clear();
            btnAddRow.Enabled = false;
            btnRefresh.Enabled = false;
            dataGridView1.Enabled = false;
            btnSave.Enabled = false;
        }

        private void btnAddRow_Click(object sender, EventArgs e)
        {
            dataGridView1.Rows.Add("Арм_А", "000", "100", "200", "0", false);
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


        private void RefreshImage()
        {
            //считать параметры из таблицы
            List<ScetchParameter> parameters = new List<ScetchParameter>();
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                ScetchParameter param = new ScetchParameter();
                param.Name = row.Cells[0].Value.ToString();
                param.value = row.Cells[1].Value.ToString();

                string posXstring = row.Cells[2].Value.ToString();
                param.PositionX = float.Parse(posXstring);

                string posYstring = row.Cells[3].Value.ToString();
                param.PositionY = float.Parse(posYstring);

                string rotateString = row.Cells[4].Value.ToString();
                param.Rotation = float.Parse(rotateString);

                param.NeedsWrap = (bool)row.Cells[5].Value;

                parameters.Add(param);
            }


            //взять картинку
            //нанести на неё размеры
            //временно сохранить картинку
            string newTempImage = ScetchImage.GenerateTemporary(templateImagePath, parameters);

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
            btnRefresh.Enabled = true;
            dataGridView1.Enabled = true;
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

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void buttonOpenLibraryFolder_Click(object sender, EventArgs e)
        {
            if (System.IO.Directory.Exists(SupportSettings.libraryPath))
            {
                System.Diagnostics.Process.Start(SupportSettings.libraryPath);
            }
            else
            {
                MessageBox.Show("Не найдена папка " + SupportSettings.libraryPath);
            }
        }

        private void buttonOpenConfigFile_Click(object sender, EventArgs e)
        {
            if(System.IO.File.Exists(SupportSettings.configFilePath))
            {
                System.Diagnostics.Process.Start(SupportSettings.configFilePath);
            }
            else
            {
                MessageBox.Show("Не найден файл " + SupportSettings.configFilePath);
            }
        }

        //private void InitializeComponent()
        //{
        //    this.SuspendLayout();
        //    // 
        //    // Form1
        //    // 
        //    this.ClientSize = new System.Drawing.Size(292, 273);
        //    this.Name = "Form1";
        //    this.Load += new System.EventHandler(this.Form1_Load);
        //    this.ResumeLayout(false);

        //}


    }
}
