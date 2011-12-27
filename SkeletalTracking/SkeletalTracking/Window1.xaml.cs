using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Research.Kinect.Nui;
using Coding4Fun.Kinect.Wpf;
using Kinect.Extensions;

namespace SkeletalTracking
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    
    


    
    public partial class Window1 : Window
    {

        // Developer Options
        const double dev_ColorWindowHeight = 1050;
        const double dev_ColorWindowWidth = 800;
        const int dev_scaledKinectWidth = 640;
        const int dev_scaledKinectHeight = 480;
        
        // public static MainWindow colorWindow = new MainWindow();
        public static MainWindow colorWindow;
        GridSettings gridSettings;
        ToleranceSettings toleranceSettings;
        SmoothSkeleton smoothSkeleton; // what should this be called???
        SkeletonData skeleton;          // the current skeleton
        CalibrationSettings calibrationSettings;
        Runtime nui;
        
        public Window1()
        {
            
            
            colorWindow = new MainWindow();
            
            InitializeComponent();
            

            // initialize grid
            gridSettings = new GridSettings(colorWindow.Width, colorWindow.Height);
            toleranceSettings = new ToleranceSettings();
            smoothSkeleton = new SmoothSkeleton();
            calibrationSettings = new CalibrationSettings();
            this.updateGrid();



            colorWindow.Show();
            
            

        }

        public void updateGrid() {
            colorWindow.updateGrid(gridSettings.lines);
            colorWindow.lineGrid.SetValue(WidthProperty, gridSettings.gridWidth);
            colorWindow.lineGrid.SetValue(HeightProperty, gridSettings.gridHeight);
        }

        

        System.Windows.Threading.DispatcherTimer myDispatcherTimer;

        // tilt
        int tiltValue;

        
        
        // depth + vga viewer
        SkeletalViewer.MainWindow viewer = new SkeletalViewer.MainWindow();
        bool viewerOpen = false;






        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

            //Initialize to do skeletal tracking
            nui.Initialize(RuntimeOptions.UseSkeletalTracking);

            #region TransformSmooth
            //Must set to true and set after call to Initialize
            nui.SkeletonEngine.TransformSmooth = false;

            //Use to transform and reduce jitter
            var parameters = new TransformSmoothParameters
            {
                Smoothing = .5f, // default .5f
                Correction = 0.0f,
                Prediction = 0.0f,
                JitterRadius = 0.05f, // default .005f
                MaxDeviationRadius = 0.05f
            };

            nui.SkeletonEngine.SmoothParameters = parameters;

            #endregion

            //add event to receive skeleton data
            nui.SkeletonFrameReady += new EventHandler<SkeletonFrameReadyEventArgs>(nui_SkeletonFrameReady);

        }

        private void Window_Closed(object sender, EventArgs e)
        {
            //Cleanup
            nui.Uninitialize();
        }

        
        void nui_SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {

            SkeletonFrame allSkeletons = e.SkeletonFrame;
            // allSkeletons is not null iff allSkeletons has a skeleton?
            skeleton = (from s in allSkeletons.Skeletons
                        where s.TrackingState == SkeletonTrackingState.Tracked
                        select s).FirstOrDefault();

            if (skeleton != null)
            {
                var leftHandY = skeleton.Joints[JointID.HandLeft].ScaleTo(dev_scaledKinectWidth, dev_scaledKinectHeight, .5f, .5f).Position.Y;
                var rightHandY = skeleton.Joints[JointID.HandRight].ScaleTo(dev_scaledKinectWidth, dev_scaledKinectHeight, .5f, .5f).Position.Y;


                if (allSkeletons.FrameNumber % 1 == 0)              // how many skeletons/second should be looked at and smoothed?
                {
                    // initial skeleton
                    if (smoothSkeleton == null)
                    {
                        smoothSkeleton = new SmoothSkeleton(leftHandY, rightHandY);
                    }
                    // process every new skeleton
                    else
                    {
                        smoothSkeleton.update(leftHandY, rightHandY);
                    }

                    // 
                    // Console.Write(allSkeletons.FrameNumber + " " + allSkeletons.TimeStamp + " ");

                    // set background color
                    SetColor(proportion, smoothSkeleton);
                }
            }
            //}
        }


        private void slider1_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            colorWindow.changeLeftHandValue(leftHandSlider.Value);
        }

        private void slider2_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            colorWindow.changeRightHandValue(rightHandSlider.Value);
        }

        //private void rightOverLeft_Checked(object sender, RoutedEventArgs e)
        //{
        //    colorWindow.changeHandOrientation(0);
        //}

        //private void leftOverRight_Checked(object sender, RoutedEventArgs e)
        //{
        //    colorWindow.changeHandOrientation(1);
        //}


        private void calibrateButton_Click(object sender, RoutedEventArgs e)
        {
                myDispatcherTimer = new System.Windows.Threading.DispatcherTimer();
                myDispatcherTimer.Interval = new TimeSpan(0, 0, 1); // 1 second
                myDispatcherTimer.Tick += new EventHandler(Each_Tick);
                myDispatcherTimer.Start();
        }
        

        int countDown = 5;
        public void Each_Tick(object o, EventArgs sender)
        {
            if (countDown == 0)
            {
                myDispatcherTimer.Stop();
                calibrateButton.SetValue(IsEnabledProperty, false);
                calibrationSettings.setCalibrationMode(true);
                
                calibrationSettings.callCalibrate();
                
                calibrateButton.SetValue(IsEnabledProperty, true);
                calibrateButton.Content = "Calibrate";
                
                // restart countdown
                countDown = 5;

            }
            else {
               sendMessage("Calibrating in... " + countDown--.ToString());
            }
        }

        private void launchViewer_Click(object sender, RoutedEventArgs e)
        {
            if (viewerOpen == false)
            {
                
                viewer.Show();
                //String launchViewerButtonContent = "Launch Depth Viewer";
                launchViewer.SetValue(ContentProperty, "Close Depth Viewer");
                viewerOpen = true;
            }
            else {
                viewer.Hide();
                launchViewer.SetValue(ContentProperty, "Launch Depth Viewer");
                viewerOpen = false;
            }
        }

        private void enableCrosshairs_Checked(object sender, RoutedEventArgs e)
        {
            colorWindow.setCrosshairs();
        }

        private void enableCrosshairs_Unchecked(object sender, RoutedEventArgs e)
        {
            colorWindow.setCrosshairs();
        }

        //private void toleranceSlider_ValueChanged_1(object sender, RoutedPropertyChangedEventArgs<double> e)
        //{
        //    double value = toleranceSlider.Value;
        //    colorWindow.setTolerance(toleranceSlider.Value);
        //}

        private void textBox1_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (textBox1.Text != "")
            {
                int value = Int32.Parse(textBox1.Text);
                toleranceSettings.setTolerance(value);
            }
        }

   

        private void tiltSlider_ValueChanged_1(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            tiltValue = Convert.ToInt32(tiltSlider.Value);
        }

        private void setTiltButton_Click(object sender, RoutedEventArgs e)
        {
            this.setTilt(tiltValue);
        }

        private void toleranceOption1_Checked(object sender, RoutedEventArgs e)
        {
            toleranceSettings.setToleranceMode(false);
        }

        private void toleranceOption2_Checked(object sender, RoutedEventArgs e)
        {
            toleranceSettings.setToleranceMode(true);
        }

        private void setToleranceRange_Click(object sender, RoutedEventArgs e)
        {
            toleranceSettings.setMinTolerance(Convert.ToInt32(minToleranceBox.Text));
            toleranceSettings.setMaxTolerance(Convert.ToInt32(maxToleranceBox.Text));
            toleranceOption2.SetValue(IsEnabledProperty, true);
        }

        private void HideRatioValueCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            colorWindow.textBlock1.SetValue(VisibilityProperty,Visibility.Hidden);
        }

        private void HideRatioValueCheckbox_Unchecked(object sender, RoutedEventArgs e)
        {
            colorWindow.textBlock1.SetValue(VisibilityProperty, Visibility.Visible);
            
        }

        private void numberOfGridUnits_TextChanged(object sender, TextChangedEventArgs e)
        {
            
            try 
            {
                int newGridUnits = Convert.ToInt32(numberOfGridUnits.Text);
                if (newGridUnits < 1)
                {
                    //message.Text = "GRID UNITS: please enter a number greater than 1.";
                    this.sendMessage("GRID UNITS: please enter a number greater than 1.");
                    numberOfGridUnits.Background = new SolidColorBrush(Color.FromArgb(255,255,95,95));
                    
                }
                else
                {
                    message.Text = "";
                    numberOfGridUnits.Background = new SolidColorBrush(Colors.White);
                    gridSettings.gridUnits = newGridUnits;
                }

            }
            catch (Exception ex) {}

        }

        private void sendMessage(String msg)
        {
            message.Text = msg;
        }

        private void setGridUnits_Click(object sender, RoutedEventArgs e)
        {
            gridSettings.setGridUnits(gridSettings.gridUnits, gridSettings.rotation);
            updateGrid();
        }

        private void GridUnitsCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            colorWindow.showGridUnits();
        }

        private void GridUnitsCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            colorWindow.hideGridUnits();
        }

        

        private void mSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            
        }

        private void trendRateSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            
        }

        private void dataRateSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            
        }

        private void mBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                this.SmoothSkeleton.setM(Convert.ToDouble(mBox.Text));
            }
            catch (FormatException ex) { }

        }

        private void trendRateBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                MainWindow.SmoothSkeleton.setTrendSmoothingRate(Convert.ToDouble(trendRateBox.Text));
            }
            catch (FormatException ex) { }
        }

        private void dataRateBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                MainWindow.SmoothSkeleton.setDataSmoothingRate(Convert.ToDouble(dataRateBox.Text));
            }
            catch (FormatException ex) { }
        }

        

        

        private void CWRotation_Checked(object sender, RoutedEventArgs e)
        {
            // change color window size
            colorWindow.SetValue(WidthProperty, SystemParameters.VirtualScreenWidth);
            colorWindow.SetValue(HeightProperty, SystemParameters.VirtualScreenHeight);
            // change grid size
            this.gridSettings.resize(SystemParameters.VirtualScreenWidth, SystemParameters.VirtualScreenHeight, 1);
            updateGrid();
        }

        private void devRotation_Checked(object sender, RoutedEventArgs e)
        {
            colorWindow.SetValue(WidthProperty, dev_ColorWindowWidth );
            colorWindow.SetValue(HeightProperty, dev_ColorWindowHeight);
            if (gridSettings != null)
            {
                this.gridSettings.resize(dev_ColorWindowWidth, dev_ColorWindowHeight, 0);
                updateGrid();
            }
        }

        private void CCWRotation_Checked(object sender, RoutedEventArgs e)
        {
            colorWindow.SetValue(WidthProperty, SystemParameters.VirtualScreenWidth);
            colorWindow.SetValue(HeightProperty, SystemParameters.VirtualScreenHeight);
            this.gridSettings.resize(SystemParameters.VirtualScreenWidth, SystemParameters.VirtualScreenHeight, -1);
            updateGrid();
        }

        public void setTilt(int angle)
        {
            if (nui.NuiCamera.TrySetAngle(angle))
                sendMessage("Moving the sensor...");
            else
                sendMessage("Error occurred moving the sensor.");


        }
       
    }

    public class GridSettings
    {
        
        int gapSize;                 // crosshair height: 100
        DoubleCollection lineStyle;
        public int gridUnits;
        public int rotation;
        public double gridWidth;
        public double gridHeight;
        public System.Collections.ArrayList lines;


        public GridSettings(double windowWidth, double windowHeight)
        {
            
            
            gridHeight = windowHeight;
            gridWidth = windowWidth;
            lineStyle = new DoubleCollection();
            gridUnits = 10;
            gapSize = 50;
            rotation = 0;
            lines = new System.Collections.ArrayList();


            setGridUnits(gridUnits, rotation);


        }

        public void resize(double windowWidth, double windowHeight, int rotation)
        {
            gridHeight = windowHeight;
            gridWidth = windowWidth;
            setGridUnits(gridUnits, rotation);
        }
        

        /// <summary>
        /// puts new grid lines into (Arraylist lines) according to grid unit and rotation (parameters), gap size, and the size of the color window (specified in gridSetup). a call to setGridUnits is usually followed by a call to updateGrid().
        /// </summary>
        /// <param name="gridUnits">number of units between top and bottom lines (what the crosshairs move through)</param>
        /// <param name="rotation">0 = developer; 1 = 90deg clockwise, -1 = 90deg counterclockwise</param>
        public void setGridUnits(int gridUnits, int rotation)
        {
            lines.Clear();

            this.gridUnits = gridUnits;
            this.rotation = rotation;

            // gridsize: pixel length of a side of one square
            int gridSize = 0;
            if (rotation == 0)
            {
                if (gridUnits > 0)
                {
                    gridSize = Convert.ToInt32(gridHeight - gapSize * 2) / gridUnits;
                }
                else
                {
                    gridSize = Convert.ToInt32(gridHeight - gapSize * 2);
                }
            }
            if (rotation == 1 | rotation == -1)
            {
                if (gridUnits > 0)
                {
                    gridSize = Convert.ToInt32(gridWidth - gapSize * 2) / gridUnits;
                }
                else
                {
                    gridSize = Convert.ToInt32(gridWidth - gapSize * 2);
                }
            }

            // rows
            Line topLine = new Line();
            topLine.Stroke = System.Windows.Media.Brushes.Black;
            if (rotation == 0)
            {
                topLine.X1 = 0;
                topLine.X2 = gridWidth;
                topLine.Y1 = gapSize;
                topLine.Y2 = gapSize;
            }
            if (rotation == 1)
            {
                topLine.X1 = gapSize;
                topLine.X2 = gapSize;
                topLine.Y1 = 0;
                topLine.Y2 = gridHeight;
            }
            if (rotation == -1)
            {
                topLine.X1 = gridWidth - gapSize;
                topLine.X2 = gridWidth - gapSize;
                topLine.Y1 = 0;
                topLine.Y2 = gridHeight;
            }

            topLine.StrokeThickness = 3;
            topLine.StrokeDashArray = lineStyle;
            // lineGrid.Children.Add(topLine);
            lines.Add(topLine);
            

            Line bottomLine = new Line();
            bottomLine.Stroke = System.Windows.Media.Brushes.Black;

            if (rotation == 0)
            {
                bottomLine.X1 = 0;
                bottomLine.X2 = gridWidth;
                bottomLine.Y1 = gridHeight - gapSize;
                bottomLine.Y2 = gridHeight - gapSize;
            }
            if (rotation == 1)
            {
                bottomLine.X1 = gridWidth - gapSize;
                bottomLine.X2 = gridWidth - gapSize;
                bottomLine.Y1 = 0;
                bottomLine.Y2 = gridHeight;
            }
            if (rotation == -1)
            {
                bottomLine.X1 = gapSize;
                bottomLine.X2 = gapSize;
                bottomLine.Y1 = 0;
                bottomLine.Y2 = gridHeight;
            }

            bottomLine.StrokeThickness = 3;
            bottomLine.StrokeDashArray = lineStyle;
            lines.Add(bottomLine);


            // rows
            for (int i = 1; i < gridUnits; i++)
            {
                Line midLine = new Line();
                midLine.Stroke = System.Windows.Media.Brushes.Black;
                if (rotation == 0)
                {
                    midLine.X1 = 0;
                    midLine.X2 = gridWidth;
                    midLine.Y1 = i * gridSize + gapSize;
                    midLine.Y2 = i * gridSize + gapSize;
                }
                if (rotation == 1 | rotation == -1)
                {
                    midLine.X1 = i * gridSize + gapSize;
                    midLine.X2 = i * gridSize + gapSize;
                    midLine.Y1 = 0;
                    midLine.Y2 = gridHeight;
                }
                midLine.StrokeThickness = 1;
                midLine.StrokeDashArray = lineStyle;
                lines.Add(midLine);
            }

            // columns
            for (int j = 1; j < gridWidth / gridUnits; j++)
            {
                Line midLine = new Line();
                midLine.Stroke = System.Windows.Media.Brushes.Black;
                if (rotation == 0)
                {
                    midLine.Y1 = 0;
                    midLine.Y2 = gridHeight;
                    midLine.X1 = j * gridSize;
                    midLine.X2 = j * gridSize;
                }
                if (rotation == 1 | rotation == -1)
                {
                    midLine.Y1 = j * gridSize;
                    midLine.Y2 = j * gridSize;
                    midLine.X1 = 0;
                    midLine.X2 = gridWidth;
                }
                midLine.StrokeThickness = 1;
                midLine.StrokeDashArray = lineStyle;
                lines.Add(midLine);
            }



        }
    }

    public class ToleranceSettings
    {
        // tolerance:
        // true = relative tolerance
        // false = absolute tolerance
        bool toleranceMode;
        double maxHandPosition;
        double maxTolerance;
        double minTolerance;
        double toleranceLeftHand;
        double toleranceRightHand;

        //Kinect Runtime
        double leftHandRatio;
        double rightHandRatio;

        double proportion;
        double tolerance;
        double greenYellowFade;
        double yellowRedFade;

        public ToleranceSettings()
        {
            // relative or absolute tolerance?
            toleranceMode = false;
            maxTolerance = 0;
            minTolerance = 0;
            
            // these are constrained
            leftHandRatio = 1;
            rightHandRatio = 1;
            proportion = 1;
            
            tolerance = .1;
            greenYellowFade = .01;
            yellowRedFade = .01;
        }

        public void setToleranceMode(bool mode)
        {
            toleranceMode = mode;
        }
        public void setMinTolerance(int minT)
        {
            minTolerance = minT;
        }
        public void setMaxTolerance(int maxT)
        {
            maxTolerance = maxT;
        }
        public void setTolerance(int toleranceValue)
        {
            if (toleranceValue <= 50 && toleranceValue >= 0)
            {

                tolerance = (double)toleranceValue / 100;
            }
        }
        public void changeLeftHandValue(double leftValue)
        {
            leftHandRatio = leftValue;
            recalculateProportion();
        }

        public void changeRightHandValue(double rightValue)
        {
            rightHandRatio = rightValue;
            recalculateProportion();
        }

        private void recalculateProportion()
        {
            proportion = leftHandRatio / rightHandRatio;
        }



        class ColorInterpolator
        {
            delegate byte ComponentSelector(System.Windows.Media.Color color);
            static ComponentSelector _redSelector = color => color.R;
            static ComponentSelector _greenSelector = color => color.G;
            static ComponentSelector _blueSelector = color => color.B;

            public static System.Windows.Media.Color InterpolateBetween(
                System.Windows.Media.Color endPoint1,
                System.Windows.Media.Color endPoint2,
                double lambda)
            {
                if (lambda < 0 || lambda > 1)
                {
                    throw new ArgumentOutOfRangeException("lambda");
                }
                System.Windows.Media.Color color = System.Windows.Media.Color.FromArgb(
                    255,
                    InterpolateComponent(endPoint1, endPoint2, lambda, _redSelector),
                    InterpolateComponent(endPoint1, endPoint2, lambda, _greenSelector),
                    InterpolateComponent(endPoint1, endPoint2, lambda, _blueSelector)
                );

                return color;
            }

            static byte InterpolateComponent(
                System.Windows.Media.Color endPoint1,
                System.Windows.Media.Color endPoint2,
                double lambda,
                ComponentSelector selector)
            {
                // redComponent(endPoint1) + lambda*difference between endPoints
                return (byte)(selector(endPoint1)
                    + (selector(endPoint2) - selector(endPoint1)) 
                    * lambda);
            }
        }

    }

    public class SmoothSkeleton
    {

        //double jitterRegion = 5;
        static bool smoothingEnabled = true;
        static double trendSmoothingRate = 0.1;
        static double dataSmoothingRate = 0.2;
        static double m = 1;                          // play with this value 
        // (it's like prediction)
        double leftCurrentPos;
        double leftPrevPos;
        double leftCurrentSmooth;
        double leftPrevSmooth;
        double leftCurrentTrendSmooth;
        double leftPrevTrendSmooth;

        double rightCurrentPos;
        double rightPrevPos;
        double rightCurrentSmooth;
        double rightPrevSmooth;
        double rightCurrentTrendSmooth;
        double rightPrevTrendSmooth;

        public double leftOutput;
        public double rightOutput;


        public static void setTrendSmoothingRate(double newRate)
        {
            trendSmoothingRate = newRate;
        }
        public static void setDataSmoothingRate(double newRate)
        {
            dataSmoothingRate = newRate;
        }
        public static void setM(double newM)
        {
            m = newM;
        }

        public SmoothSkeleton()
        {
        }

        public SmoothSkeleton(double leftHandY, double rightHandY)
        {
            leftCurrentPos = leftHandY;
            leftCurrentSmooth = leftHandY;
            leftCurrentTrendSmooth = 0;         // ?????

            rightCurrentSmooth = rightHandY;

            leftOutput = leftHandY;
            rightOutput = rightHandY;

        }

        public void update(double newLeft, double newRight)
        {
            // LEFT HAND
            if (smoothingEnabled == true)
            {
                // jitterReduce section
                //if (Math.Abs(leftCurrentPos - leftPrevPos) > jitterRegion) {
                //    leftCurrentPos = leftPrevPos;
                //}
                //if (Math.Abs(rightCurrentPos - rightPrevPos) > jitterRegion) {
                //    rightCurrentPos = rightPrevPos;
                //}

                //

                leftPrevPos = leftCurrentPos;
                leftPrevSmooth = leftCurrentSmooth;
                leftPrevTrendSmooth = leftCurrentTrendSmooth;

                leftCurrentPos = newLeft;

                leftCurrentSmooth =
                    (dataSmoothingRate * leftCurrentPos)
                    + (1 - dataSmoothingRate) * (leftPrevSmooth + leftPrevTrendSmooth);

                // is this happening AFTER the previous line? possible race condition.
                // change right hand value too.
                leftCurrentTrendSmooth =
                    trendSmoothingRate * (leftCurrentSmooth - leftPrevSmooth)
                    + (1 - trendSmoothingRate) * leftPrevTrendSmooth;

                leftOutput = leftCurrentSmooth + (m * leftCurrentTrendSmooth);


                // RIGHT HAND
                rightPrevPos = rightCurrentPos;
                rightPrevSmooth = rightCurrentSmooth;
                rightPrevTrendSmooth = rightCurrentTrendSmooth;

                rightCurrentPos = newRight;

                rightCurrentSmooth =
                    (dataSmoothingRate * rightCurrentPos)
                    + (1 - dataSmoothingRate) * (rightPrevSmooth + rightPrevTrendSmooth);

                rightCurrentTrendSmooth =
                    trendSmoothingRate * (rightCurrentSmooth - rightPrevSmooth)
                    + (1 - trendSmoothingRate) * rightPrevTrendSmooth;

                rightOutput = rightCurrentSmooth + (m * rightCurrentTrendSmooth);
            }
            else
            {
                leftOutput = newLeft;
                rightOutput = newRight;
            }
        }
    }

    public class CalibrationSettings
    {
        bool inCalibrationMode;
        double crosshairRate;
        double calibrationBaseline;
        bool isCalibrated;

        public CalibrationSettings()
        {
            inCalibrationMode = false;
            calibrationBaseline = 0;
            crosshairRate = 1;  // should start as 0?
            isCalibrated = false;
        }

        public void setCalibrationMode(bool value)
        {
            if (!inCalibrationMode)
            {
                inCalibrationMode = value;
            }
        }
        /*
        public void callCalibrate()
        {
            calibrate(skeleton);
        }
        */
        public bool checkForSkeleton(SkeletonData skeleton)
        {
            if (skeleton == null)
            {

                return false;
            }
            else
            {
                return true;
            }
        }

        // sets a calibration baseline
        public string calibrate(SkeletonData skeleton)
        {
            // reset isCalibrated
            if (isCalibrated == true)
            {
                isCalibrated = false;
            }

            // isCalibrated is false at this point
            // only attempt to calibrate if skeleton is not null
            if (checkForSkeleton(skeleton) == false)
            {
                return "person not recognized";
            }
            else
            {
                while (!isCalibrated)
                {
                    var scaledLeftHand = skeleton.Joints[JointID.HandLeft].ScaleTo(640, 480, .5f, .5f);
                    var scaledRightHand = skeleton.Joints[JointID.HandRight].ScaleTo(640, 480, .5f, .5f);
                    double leftHandCalibrationValue = scaledLeftHand.Position.Y;
                    double rightHandCalibrationValue = scaledRightHand.Position.Y;



                    if (leftHandCalibrationValue < rightHandCalibrationValue)
                    {
                        calibrationBaseline = leftHandCalibrationValue;
                    }
                    else
                    {
                        calibrationBaseline = rightHandCalibrationValue;
                    }
                    // correct???????? should this be in colorWindow?
                    crosshairRate = (SystemParameters.VirtualScreenHeight / -calibrationBaseline) * 0.9;

                    if (calibrationBaseline > 0)
                    {
                        isCalibrated = true;
                    }
                }

                return "calibrated!";
            }
        }
    }
        
    
}

namespace Kinect.Extensions
{
    public static class CameraExtensions
    {
        public static bool TrySetAngle(this Camera camera, int angle)
        {
            try
            {
                camera.ElevationAngle = angle;
                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }
    }
}

