using Emgu.CV;
using Emgu.CV.CvEnum;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace EmguCVStarter
{
    public partial class Form1 : Form
    {
        Mat originalImage = new Mat();
        Mat grayscaleImage = new Mat();
        Mat processedImage1 = new Mat();
        Mat processedImage2 = new Mat();
        Mat processedImage3 = new Mat();

        private Capture capture = null;
        int FPS = 25;
        Timer myTimer = new Timer();
        bool isPlaying = false;

        public Form1()
        {
            InitializeComponent();
        }

        /// <summary>
        /// a function to process video frame
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ProcessVideoFrame(object sender, EventArgs e)
        {
            //stream video & convert
            originalImage = capture.QueryFrame();
            try
            {
                CvInvoke.CvtColor(originalImage, grayscaleImage, ColorConversion.Bgr2Gray);
                Process();
            }
            catch (NullReferenceException ex)
            {
                isPlaying = false;
                myTimer.Stop();
                capture.Stop();
                MessageBox.Show("End of video file or " + ex.Message);
            }
        }

        //this function is called when "Load" button is clicked
        private void buttonLoad_Click(object sender, EventArgs e)
        {
            OpenFileDialog Openfile = new OpenFileDialog();
            if (Openfile.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    if (radioButtonImage.Checked)
                    {
                        grayscaleImage = CvInvoke.Imread(Openfile.FileName, LoadImageType.Grayscale);
                        buttonProcess.Enabled = true;
                        Process();
                    }
                    else if (radioButtonVideo.Checked)
                    {
                        capture = new Capture(Openfile.FileName);
                        myTimer.Interval = 1000 / FPS;
                        myTimer.Tick += new EventHandler(ProcessVideoFrame);
                        buttonProcess.Enabled = true;
                        imageBox1.Image = null;
                        imageBox2.Image = null;
                        imageBox3.Image = null;
                        imageBox4.Image = null;
                    }
                }
                catch (NullReferenceException excpt)
                {
                    MessageBox.Show(excpt.Message);
                }
                catch (ArgumentException ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        //this function is called when "Process" button is clicked
        private void buttonProcess_Click(object sender, EventArgs e)
        {
            if (radioButtonVideo.Checked)
            {
                if (!isPlaying)
                {
                    isPlaying = !isPlaying;
                    myTimer.Start();
                    buttonProcess.Text = "Stop";
                }
                else
                {
                    isPlaying = !isPlaying;
                    buttonProcess.Text = "Start";
                    myTimer.Stop();
                    capture.Stop();
                }
            }
            else if (radioButtonImage.Checked)
            {
                Process();
            }
        }

        //main process of image processing 
        private void Process()
        {
            imageBox1.Image = grayscaleImage;

            CvInvoke.Threshold(grayscaleImage, processedImage1, 40, 255, ThresholdType.Binary);
            imageBox2.Image = processedImage1;

            CvInvoke.Canny(grayscaleImage, processedImage2, 30, 70);
            imageBox3.Image = processedImage2;

            CvInvoke.CLAHE(grayscaleImage, 40, new Size(8, 8), processedImage3);
            imageBox4.Image = processedImage3;
        }
    }
}
