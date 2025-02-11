using AutomataVideoGenerator.Automatons.GPUCompute;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;

namespace AutomatonGen_UI
{
    public partial class MainForm : Form
    {
        GenericBSN automaton;
        Bitmap image;
        System.Windows.Forms.Timer timer;

        public MainForm()
        {
            automaton = new GenericBSN(40, 40, 10, GenericBSN.defaults.GameOfLife);
            timer = new System.Windows.Forms.Timer();
            timer.Tick += new EventHandler(update);

            InitializeComponent();

            ModelListBox.DataSource = Enum.GetNames(typeof(GenericBSN.defaults)).ToList();
            reDrawMainImage();
        }

        private void update(object sender, EventArgs e)
        {
            automaton.update();
            reDrawMainImage();
        }

        private void reDrawMainImage()
        {
            if (image != null)
            {
                image.Dispose();
            }

            image = automaton.getImage();
            DisplayImageBox.Image = image;
        }

        private void resetAutomaton()
        {
            timer.Stop();

            automaton.Dispose();
            automaton = new GenericBSN(int.Parse(WidthControl.Value.ToString()), int.Parse(HeightControl.Value.ToString()), int.Parse(ScaleControl.Value.ToString()), (GenericBSN.defaults)ModelListBox.SelectedIndex); //Cast enmum from int

            reDrawMainImage();
        }

        private void StartStopButton_Click(object sender, EventArgs e)
        {
            if (!timer.Enabled)
            {
                timer.Interval = 100;
                timer.Start();
            }
            else
            {
                timer.Stop();
            }
        }

        private void ModelListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            resetAutomaton();
        }

        private void WidthControl_ValueChanged(object sender, EventArgs e)
        {
            resetAutomaton();
        }

        private void HeightControl_ValueChanged(object sender, EventArgs e)
        {
            resetAutomaton();
        }

        private void ScaleControl_ValueChanged(object sender, EventArgs e)
        {
            resetAutomaton();
        }
    }
}