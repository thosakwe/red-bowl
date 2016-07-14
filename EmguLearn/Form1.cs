using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;

namespace EmguLearn
{
    public partial class Form1 : Form
    {
        CircleF cursor;

        public Form1()
        {
            InitializeComponent();
        }

        Bitmap processImage(Image<Bgr, byte> image)
        {
            var img = image.Resize(400, 400, Inter.Linear, true);

            var uimg = new UMat();
            CvInvoke.CvtColor(img, uimg, ColorConversion.Bgr2Gray);

            var pyrDown = new UMat();
            CvInvoke.PyrDown(uimg, pyrDown);
            CvInvoke.PyrUp(pyrDown, uimg);

            var circles = CvInvoke.HoughCircles(uimg, HoughType.Gradient, 2, 20, 180, 120, 5);
            foreach (var circle in circles)
            {
                img.Draw(circle, new Bgr(Color.Pink), 3);
            }

            // Canny and edge detection
            var cannyEdges = new UMat();
            CvInvoke.Canny(uimg, cannyEdges, 180, 120);

            var lines = CvInvoke.HoughLinesP(cannyEdges, 1, Math.PI / 45.0, 20, 30, 10);

            using (var contours = new VectorOfVectorOfPoint())
            {
                CvInvoke.FindContours(cannyEdges, contours, null, RetrType.List, ChainApproxMethod.ChainApproxSimple);
                int count = contours.Size;

                for (int i = 0; i < count; i++)
                {
                    using (var contour = contours[i])
                    {
                        using (var approxContour = new VectorOfPoint())
                        {
                            CvInvoke.ApproxPolyDP(contour, approxContour, CvInvoke.ArcLength(contour, true) * .05, true);
                            // Only consider contours greater than 250 area
                            if (CvInvoke.ContourArea(approxContour, false) > 250)
                            {
                                var points = approxContour.ToArray();

                                if (approxContour.Size == 3)
                                {
                                    // Size tells how many vertices...
                                    var triangle = new Triangle2DF(points[0], points[1], points[2]);
                                    img.Draw(triangle, new Bgr(Color.Orange), 3);
                                }
                            }
                        }
                    }
                }
            }

            return img.ToBitmap();
        }

        Bitmap processImage(string fileName)
        {
            return processImage(new Image<Bgr, byte>(fileName));
        }

        float makeCoord(float coord, int width)
        {
            var factor = coord / (width * 1.0F);
            return factor * Width;
        }

        void runMouse(Mat mat)
        {
            var img = mat.ToImage<Bgr, byte>().Flip(FlipType.Horizontal).Resize(400, 400, Inter.Linear, true);

            var uimg = new UMat();
            CvInvoke.CvtColor(img, uimg, ColorConversion.Bgr2Gray);

            var pyrDown = new UMat();
            CvInvoke.PyrDown(uimg, pyrDown);
            CvInvoke.PyrUp(pyrDown, uimg);

            var circles = CvInvoke.HoughCircles(uimg, HoughType.Gradient, 2, 20, 180, 120, 5);

            //var cursorImg = mat.ToImage<Bgr, byte>().CopyBlank();
            //cursorImg.Mat.SetTo(new Bgr(Color.White).MCvScalar);

            if (circles.Length > 0)
            {
                cursor = circles[0];
                img.Draw(cursor, new Bgr(Color.Pink), 2);
            }

            /*if (cursor.Area > 0)
            {
                cursorImg.Draw(cursor, new Bgr(Color.Red), 2);
            }*/

            var x = makeCoord(cursor.Center.X - cursor.Radius, img.Width);
            var y = makeCoord(cursor.Center.Y - cursor.Radius, img.Width);

            lblCursor.Location = new Point(Convert.ToInt32(x), Convert.ToInt32(y));
            //pictureBox1.BackgroundImage = cursorImg.ToBitmap();
            preview.BackgroundImage = img.ToBitmap();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            var capture = new Capture();
            //var haar = new CascadeClassifier("..\\hand.xml");

            Application.Idle += (_sender, _e) =>
            {
                //pictureBox1.BackgroundImage = processImage(capture.QueryFrame().ToImage<Bgr, byte>());
                runMouse(capture.QueryFrame());

                if (MakeRect(lblCursor).IntersectsWith(MakeRect(button1)))
                {
                    button1.Text = "ACTIVE";
                }
                else
                {
                    button1.Text = "INACTIVE";
                }
            };
        }

        private void button1_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Clicked!");
        }

        Rectangle MakeRect(Control control)
        {
            return new Rectangle(control.Location, control.Size);
        }

        void PaintControl(Pen pen, Control control, Graphics graphics)
        {
            graphics.DrawRectangle(pen, MakeRect(control));
        }

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            Invalidate();
        }
    }
}
