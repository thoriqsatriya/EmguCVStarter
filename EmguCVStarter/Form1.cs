using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
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
        private void Process(string path=null)
        {

            if (path != null) grayscaleImage = CvInvoke.Imread(path, LoadImageType.Grayscale);


            //masking
            Mat maskingImage = new Mat();
            Mat tempImage = grayscaleImage.Clone();

            maskingImage = grayscaleImage.Clone();
            maskingImage.SetTo(new MCvScalar(0));
            CvInvoke.Circle(maskingImage, new Point(grayscaleImage.Width / 2, grayscaleImage.Height / 2), (int)(0.75 * grayscaleImage.Height / 2), new MCvScalar(255), -1);

            grayscaleImage.SetTo(new MCvScalar(255));
            tempImage.CopyTo(grayscaleImage, maskingImage);

            //original image
            imageBox1.Image = grayscaleImage;

            //thresholding
            CvInvoke.Threshold(grayscaleImage, processedImage1, 40, 255, ThresholdType.BinaryInv);
            imageBox2.Image = processedImage1;

            //create black image
            processedImage2 = grayscaleImage.Clone();
            processedImage2.SetTo(new MCvScalar(0));

            //contour extractioin process
            VectorOfPoint largestContour;
            int largestContourIndex = 0;
            double largestAreaTemp = 0;

            using (Mat hierachy = new Mat())
            using (VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint())
            {
                //main process
                CvInvoke.FindContours(processedImage1, contours, hierachy, RetrType.Tree, ChainApproxMethod.ChainApproxNone);

                //find the area of contour
                for (int i = 0; i < contours.Size; i++)
                {
                    double area = CvInvoke.ContourArea(contours[i], false);
                    if (area > largestAreaTemp)
                    {
                        largestAreaTemp = area;
                        largestContourIndex = i;
                    }
                    //CvInvoke.DrawContours(processedImage2, contours, i, new MCvScalar(255));
                }
                CvInvoke.DrawContours(processedImage2, contours, largestContourIndex, new MCvScalar(255));
                largestContour = new VectorOfPoint(contours[largestContourIndex].ToArray());
            }

            imageBox3.Image = processedImage2;




            //center of gravity
            //MCvMoments largestContourMoment = CvInvoke.Moments(largestContour);
            //Point centerMoment = new Point((int)(largestContourMoment.M10 / largestContourMoment.M00), (int)(largestContourMoment.M01 / largestContourMoment.M00));

            //processedImage3 = grayscaleImage.Clone();
            //CvInvoke.CvtColor(processedImage3, processedImage3, ColorConversion.Gray2Bgr);
            //CvInvoke.Circle(processedImage3, centerMoment, 3, new MCvScalar(0, 0, 255), -1);

            ////draw ellipse around pupil
            //Rectangle rectBound = CvInvoke.BoundingRectangle(largestContour);
            //CvInvoke.Ellipse(processedImage3, new RotatedRect(centerMoment, rectBound.Size, 0), new MCvScalar(0, 0, 255), 2);

            //imageBox4.Image = processedImage3;




            //ellipse fitting

            RotatedRect ellipseFitted = CvInvoke.FitEllipse(largestContour);

            processedImage3 = grayscaleImage.Clone();
            CvInvoke.CvtColor(processedImage3, processedImage3, ColorConversion.Gray2Bgr);
            CvInvoke.Ellipse(processedImage3, ellipseFitted, new MCvScalar(0,0,255), 2);
            imageBox4.Image = processedImage3;


            using (StreamWriter sw = File.AppendText("log.csv"))
            {
                //cog
                //sw.WriteLine(centerMoment.X.ToString() + "," + centerMoment.Y.ToString() + "," + (rectBound.Height/2).ToString());

                //ellipse fitting
                sw.WriteLine(ellipseFitted.Center.X.ToString() + "," + ellipseFitted.Center.Y.ToString() + "," + (ellipseFitted.Size.Height / 2).ToString());
            }

        }

        private void button1_Click(object sender, EventArgs e)
        {
            string path = "E:\\DOCUMENTS\\_research\\iris_data\\CASIA-Iris-Syn\\random\\original";
            string[] fileEntries = Directory.GetFiles(path);

            for(int i=0; i<fileEntries.Length; i++)
            {
                Process(fileEntries[i]);
            }
        }
    }
}
