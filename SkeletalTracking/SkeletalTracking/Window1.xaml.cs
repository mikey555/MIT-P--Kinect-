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
using System.Threading;


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
        const int dev_scaledDepthCamWidth = 640;
        const int dev_scaledDepthCamHeight = 480;
        
        // public static MainWindow colorWindow = new MainWindow();
        public static MainWindow colorWindow;
        GridSettings gridSettings;
        ToleranceSettings toleranceSettings;
        CalibrationSettings calibrationSettings;

        SmoothSkeleton smoothSkeleton; // what should this be called???
        SkeletonData skeleton;         // the current skeleton
        
        Runtime nui;
        
        public Window1()
        {
            

            colorWindow = new MainWindow();
            nui = Runtime.Kinects[0];
            nui.Initialize(RuntimeOptions.UseColor);
            nui.NuiCamera.ElevationAngle = 0;

            

            // initialize grid
            gridSettings = new GridSettings(colorWindow.Width, colorWindow.Height);
            toleranceSettings = new ToleranceSettings();
            calibrationSettings = new CalibrationSettings();

            smoothSkeleton = new SmoothSkeleton();
       
            this.updateGrid();

            // this will set default crosshair heights
            colorWindow.changeRotationMode(gridSettings.rotation);

            InitializeComponent();
            colorWindow.Show();

            toleranceOption1.IsChecked = true;
            
        }


        

        public void updateGrid() {
            gridSettings.callSetGridUnits();
            colorWindow.updateGrid(gridSettings.lines, gridSettings.numbers);
            colorWindow.lineGrid.SetValue(WidthProperty, gridSettings.gridWidth);
            colorWindow.lineGrid.SetValue(HeightProperty, gridSettings.gridHeight);
        }

        

        System.Windows.Threading.DispatcherTimer myDispatcherTimer;

        // tilt
        int tiltValue;

        
        
        // depth + vga viewer
        SkeletalViewer.MainWindow viewer;
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
            if (allSkeletons != null)
            {
                skeleton = (from s in allSkeletons.Skeletons
                            where s.TrackingState == SkeletonTrackingState.Tracked
                            select s).FirstOrDefault();

                if (skeleton != null)
                {
                    // unsmoothed y-values
                    var leftHandY = skeleton.Joints[JointID.HandLeft].ScaleTo(dev_scaledDepthCamWidth, dev_scaledDepthCamHeight, .5f, .5f).Position.Y;
                    var rightHandY = skeleton.Joints[JointID.HandRight].ScaleTo(dev_scaledDepthCamWidth, dev_scaledDepthCamHeight, .5f, .5f).Position.Y;


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
                        //SetColor(proportion, smoothSkeleton);
                        colorWindow.changeColor(toleranceSettings.getCurrentColor(smoothSkeleton));
                        if (CalibrationSettings.isCalibrated) 
                            colorWindow.setCrosshairs(toleranceSettings.calibratedLeftHandPos, toleranceSettings.calibratedRightHandPos);
                    }
                }
            }
        }


        private void slider1_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            try
            {
                toleranceSettings.changeLeftHandValue(leftHandSlider.Value);
                leftProportionText.Text = leftHandSlider.Value.ToString();
            }
            catch (NullReferenceException ex) 
            {
            }
        }

        private void slider2_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            try
            {
                toleranceSettings.changeRightHandValue(rightHandSlider.Value);
                rightProportionText.Text = rightHandSlider.Value.ToString();
            }
            catch (NullReferenceException ex)
            {
            }
        }

        //private void rightOverLeft_Checked(object sender, RoutedEventArgs e)
        //{
        //    colorWindow.changeHandOrientation(0);
        //}

        //private void leftOverRight_Checked(object sender, RoutedEventArgs e)
        //{
        //    colorWindow.changeHandOrientation(1);
        //}

        public void enableCalibrateButton()
        {
            calibrateButton.IsEnabled = true;
        }
        public void disableCalibrateButton()
        {
            calibrateButton.IsEnabled = false;
        }

        private void calibrateButton_Click(object sender, RoutedEventArgs e)
        {
            
            if (!CalibrationSettings.inCalibrationMode)
            {
                disableCalibrateButton();
                myDispatcherTimer = new System.Windows.Threading.DispatcherTimer();
                myDispatcherTimer.Interval = new TimeSpan(0, 0, 1); // 1 second
                myDispatcherTimer.Tick += new EventHandler(Each_Tick);
                myDispatcherTimer.Start();
            }
        }

        

        int countDown = 5;
        public void Each_Tick(object o, EventArgs sender)
        {
            if (countDown == 0)
            {
                myDispatcherTimer.Stop();
                //calibrateButton.SetValue(IsEnabledProperty, false);
                calibrationSettings.setCalibrationMode(true);
                
                // calibrate returns a string indicating success or failure.
                // if success, it sets the calibration baseline.
                sendMessage(calibrationSettings.calibrate(skeleton));
                
                // changeCrosshairRate depends on the new calibration baseline
                gridSettings.changeCrosshairRate();

                enableCalibrateButton();
                // restart countdown
                countDown = 5;

            }
            else {
               sendMessage("Calibrating in... " + countDown--.ToString());
            }
        }

        private void launchViewerThread_Click(object sender, RoutedEventArgs e)
        {
            //ThreadStart viewerDelegate = new ThreadStart(toggleViewer);
            //Thread viewerThread = new Thread(viewerDelegate);
            Thread viewerThread = new Thread(delegate()
            {
                viewer = new SkeletalViewer.MainWindow();
                
                viewer.Dispatcher.Invoke(new Action(delegate() 
                    {
                        
                        viewer.Show();
                        System.Windows.Threading.Dispatcher.Run();

                    }));
                SolidColorBrush green = new SolidColorBrush(Colors.Green);
                
                
            });


            viewerThread.SetApartmentState(ApartmentState.STA); // needs to be STA or throws exception
            viewerThread.Start();
            
        }

        private void toggleViewer()
        {
            
            viewer = new SkeletalViewer.MainWindow();

            

            //viewer.Show();
        }


        private void enableCrosshairs_Checked(object sender, RoutedEventArgs e)
        {
            colorWindow.toggleCrosshairs();
        }

        private void enableCrosshairs_Unchecked(object sender, RoutedEventArgs e)
        {
            colorWindow.toggleCrosshairs();
        }

        //private void toleranceSlider_ValueChanged_1(object sender, RoutedPropertyChangedEventArgs<double> e)
        //{
        //    double value = toleranceSlider.Value;
        //    colorWindow.setTolerance(toleranceSlider.Value);
        //}

        

        private void toleranceOption1_Checked(object sender, RoutedEventArgs e)
        {
            toleranceSettings.setToleranceMode(true);

            // enable absolute tolerance settings
            absoluteGreenYellowFadeTextbox.IsEnabled = true;
            absoluteYellowRedFadeTextbox.IsEnabled = true;
            absoluteToleranceTextbox.IsEnabled = true;
            
            // disable proportional tolerance settings
            proportionalGreenYellowFadeTextbox.IsEnabled = false;
            proportionalYellowRedFadeTextbox.IsEnabled = false;
            proportionalToleranceTextbox.IsEnabled = false;
        }

        private void toleranceOption2_Checked(object sender, RoutedEventArgs e)
        {
            toleranceSettings.setToleranceMode(false);

            // enable proportional tolerance settings
            proportionalGreenYellowFadeTextbox.IsEnabled = true;
            proportionalYellowRedFadeTextbox.IsEnabled = true;
            proportionalToleranceTextbox.IsEnabled = true;

            // disable absolute tolerance settings
            absoluteGreenYellowFadeTextbox.IsEnabled = false;
            absoluteYellowRedFadeTextbox.IsEnabled = false;
            absoluteToleranceTextbox.IsEnabled = false;
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
                    this.sendMessage("GRID UNITS: please enter a number greater than 0.");
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
                SmoothSkeleton.setM(Convert.ToDouble(mBox.Text));
            }
            catch (FormatException ex) { }

        }

        private void trendRateBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                SmoothSkeleton.setTrendSmoothingRate(Convert.ToDouble(trendRateBox.Text));
            }
            catch (FormatException ex) { }
        }

        private void dataRateBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                SmoothSkeleton.setDataSmoothingRate(Convert.ToDouble(dataRateBox.Text));
            }
            catch (FormatException ex) { }
        }

        private void CWRotation_Checked(object sender, RoutedEventArgs e)
        {
            // change color window size
            colorWindow.SetValue(WidthProperty, SystemParameters.VirtualScreenWidth);
            colorWindow.SetValue(HeightProperty, SystemParameters.VirtualScreenHeight);
            // change grid size
            colorWindow.changeRotationMode(1);
            this.gridSettings.resize(SystemParameters.VirtualScreenWidth, SystemParameters.VirtualScreenHeight, 1);
            updateGrid();
            
        }

        private void devRotation_Checked(object sender, RoutedEventArgs e)
        {
            colorWindow.SetValue(WidthProperty, dev_ColorWindowWidth );
            colorWindow.SetValue(HeightProperty, dev_ColorWindowHeight);
            if (gridSettings != null)
            {
                colorWindow.changeRotationMode(0);
                this.gridSettings.resize(dev_ColorWindowWidth, dev_ColorWindowHeight, 0);
                updateGrid();
            }
            
            
        }

        

        public void errorBox(TextBox box, string boxName, double lowerBound, double upperBound)
        {
            box.Background = new SolidColorBrush(Color.FromArgb(255,255,95,95));
            sendMessage(boxName + ": enter a number between " + lowerBound + " and " + upperBound + ".");
        }

        public void errorBox(TextBox box, string boxName, double lowerBound)
        {
            box.Background = new SolidColorBrush(Color.FromArgb(255,255,95,95));
            sendMessage(boxName + ": enter a number greater than " + lowerBound + ".");
        }

        public void unerrorBox(TextBox box)
        {
            box.Background = new SolidColorBrush(Colors.White);
            message.Text = "";
        }

        // tilt
        public void setTilt(int angle)
        {
            if (nui.NuiCamera.TrySetAngle(angle))
                sendMessage("Moving the sensor...");
            else
                sendMessage("Error occurred moving the sensor.");


        }

        private void tiltSlider_ValueChanged_1(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            tiltValue = Convert.ToInt32(tiltSlider.Value);
            tiltText.Text = Math.Round (tiltSlider.Value,1).ToString();
        }

        private void setTiltButton_Click(object sender, RoutedEventArgs e)
        {
            this.setTilt(tiltValue);
        }

        private void crosshairsStopAtBottom_Checked(object sender, RoutedEventArgs e)
        {
            GridSettings.crosshairsStopAtBottom = true;
        }
        private void crosshairsStopAtBottom_Unchecked(object sender, RoutedEventArgs e)
        {
            GridSettings.crosshairsStopAtBottom = false;
        }

        private void opacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            GridSettings.opacity = opacitySlider.Value;
        }

        private void tabControl1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void checkRange(TextBox box, string boxName, double value, double lowerBound, double upperBound)
        {
            if (value > lowerBound && value < upperBound)
                this.unerrorBox(box);
            else
                this.errorBox(box, boxName, lowerBound, upperBound);
        }

        private void checkRange(TextBox box, string boxName, double value, double lowerBound)
        {
            if (value > lowerBound)
                this.unerrorBox(box);
            else
                this.errorBox(box, boxName, lowerBound);
        }

        private void proportionalToleranceTextbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                toleranceSettings.boxChanged("proportionalTolerance", proportionalToleranceTextbox.Text);
                this.checkRange(proportionalToleranceTextbox, "PROP. TOLERANCE", Convert.ToDouble(proportionalToleranceTextbox.Text), 0, 1);
            }
            catch (Exception ex)
            {
                this.errorBox(proportionalToleranceTextbox, "PROP. TOLERANCE", 0, 1);
            }
        }

        private void proportionalGreenYellowFadeTextbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                toleranceSettings.boxChanged("proportionalGreenYellowFade", proportionalGreenYellowFadeTextbox.Text);
                this.checkRange(proportionalGreenYellowFadeTextbox, "PROP. GREEN-YELLOW FADE", Convert.ToDouble(proportionalGreenYellowFadeTextbox.Text), 0, 1);
            }
            catch (Exception ex)
            {
                this.errorBox(proportionalGreenYellowFadeTextbox, "PROP. GREEN-YELLOW FADE", 0, 1);
            }
        }

        private void proportionalYellowRedFadeTextbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                toleranceSettings.boxChanged("proportionalYellowRedFade", proportionalYellowRedFadeTextbox.Text);
                this.checkRange(proportionalYellowRedFadeTextbox, "PROP. YELLOW-RED FADE", Convert.ToDouble(proportionalYellowRedFadeTextbox.Text), 0, 1);
            }
            catch (Exception ex)
            {
                this.errorBox(proportionalYellowRedFadeTextbox, "PROP. YELLOW-RED FADE", 0, 1);
            }
        }

        private void absoluteToleranceTextbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                toleranceSettings.boxChanged("absoluteTolerance", absoluteToleranceTextbox.Text);
                this.checkRange(absoluteToleranceTextbox, "ABS. TOLERANCE", Convert.ToDouble(absoluteToleranceTextbox.Text), 0);
            }
            catch (Exception ex)
            {
                this.errorBox(absoluteToleranceTextbox, "ABS. TOLERANCE", 0);
            }
        }

        private void absoluteGreenYellowFadeTextbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                toleranceSettings.boxChanged("absoluteGreenYellowFade", absoluteGreenYellowFadeTextbox.Text);
                this.checkRange(absoluteGreenYellowFadeTextbox, "ABS. GREEN-YELLOW FADE", Convert.ToDouble(absoluteGreenYellowFadeTextbox.Text), 0);
            }
            catch (Exception ex)
            {
                this.errorBox(absoluteToleranceTextbox, "ABS. GREEN-YELLOW FADE", 0);
            }
        }

        private void absoluteYellowRedFadeTextbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                toleranceSettings.boxChanged("absoluteYellowRedFade", absoluteYellowRedFadeTextbox.Text);
                this.checkRange(absoluteYellowRedFadeTextbox, "ABS. YELLOW-RED FADE", Convert.ToDouble(absoluteYellowRedFadeTextbox.Text), 0);
            }
            catch (Exception ex)
            {
                this.errorBox(absoluteYellowRedFadeTextbox, "ABS. YELLOW-RED FADE", 0);
            }
        }

        

        private void tiltText_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            absoluteToleranceTextbox.Text = "50";
            absoluteGreenYellowFadeTextbox.Text = "5";
            absoluteYellowRedFadeTextbox.Text = "5";

            proportionalToleranceTextbox.Text = ".10";
            proportionalGreenYellowFadeTextbox.Text = ".01";
            proportionalYellowRedFadeTextbox.Text = ".01";

            message.Text = "";
        }

        private void checkBox2_Checked(object sender, RoutedEventArgs e)
        {

        }


        private void slider5_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            colorWindow.setCrosshairSize(slider5.Value);
        }

        private void slider6_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            colorWindow.setCrosshairThickness(slider6.Value);
        }

        private void gridUnitsSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            gridSettings.setGridUnits(Convert.ToInt32(gridUnitsSlider.Value), gridSettings.rotation);
            updateGrid();
        }  

        
       
    }

    public class GridSettings
    {

        public static int verticalGapSize = 50;                 // crosshair height: 100
        int horizontalGapSize = 50;
        DoubleCollection lineStyle = new DoubleCollection();
        public int gridUnits = 10;
        public int rotation = 0;
        public double gridWidth;
        public double gridHeight;
        public System.Collections.ArrayList lines = new System.Collections.ArrayList();
        public System.Collections.ArrayList numbers = new System.Collections.ArrayList();
        public static double crosshairRate = 0;
        public static bool crosshairsStopAtBottom;
        public static double opacity = 1;
        bool leftSideNumbersEnabled = false;
        bool rightSideNumbersEnabled = false;
        double sideNumberSize = 1;


        public GridSettings(double windowWidth, double windowHeight)
        {
            gridHeight = windowHeight;
            gridWidth = windowWidth;
            setGridUnits(gridUnits, rotation);
        }

        public void resize(double windowWidth, double windowHeight, int rotation)
        {
            gridHeight = windowHeight;
            gridWidth = windowWidth;
            setGridUnits(gridUnits, rotation);
            changeCrosshairRate();
        }

        public void setSideNumberSize(double value)
        {
            sideNumberSize = value;
        }

        public void toggleBool(bool var)
        {
            if (var == true){
                var = false;
            }
            else 
                var = true;
        }
        
        /// <summary>
        /// crosshair rate is given by:
        /// ( (pixel height of app window - pixel height of gap) / baseline )
        /// where baseline is a pixel distance from the top of the Kinect's range of vision.
        /// 
        /// In other words:
        /// ( pixels traversed by crosshairs / distance, measured in pixels by depth cam, traversed by hands ).
        /// 
        /// This is a rate of how fast the crosshairs move up the screen. If the rate is any less, the crosshairs
        /// will move too slowly and at their highest point will still be partially visible on the grid.
        /// </summary>
        public void changeCrosshairRate()
        {
            if(CalibrationSettings.isCalibrated) {
                if (rotation == 0)
                {
                    
                    crosshairRate = (gridHeight / CalibrationSettings.calibrationBaseline);
                    
                }
                if (rotation == 1)
                {
                    
                    crosshairRate = (gridWidth / CalibrationSettings.calibrationBaseline);
                }
            }

        }
        

        /// <summary>
        /// puts new grid lines into (Arraylist lines) according to grid unit and rotation (parameters), gap size, and the size of the color window (specified in gridSetup). a call to setGridUnits is usually followed by a call to updateGrid().
        /// </summary>
        /// <param name="gridUnits">number of units between the baseline and the top of the screen</param>
        /// <param name="rotation">0 = developer; 1 = 90deg clockwise, -1 = 90deg counterclockwise</param>
        public void callSetGridUnits()
        {
            setGridUnits(gridUnits, rotation);
        }
        
        // gridUnits = number of units (squares) from bottom line to top of screen)
        public void setGridUnits(int gridUnits, int rotation)
        {
            lines.Clear();
            numbers.Clear();

            this.gridUnits = gridUnits;
            this.rotation = rotation;

            // gridsize: pixel length of a side of one square
            int unitSize = 0;
            if (rotation == 0)
            {
                if (gridUnits > 0)
                {
                    unitSize = Convert.ToInt32(gridHeight - verticalGapSize) / gridUnits;
                }
                else
                {
                    unitSize = Convert.ToInt32(gridHeight - verticalGapSize);
                }
            }
            if (rotation == 1)
            {
                if (gridUnits > 0)
                {
                    unitSize = Convert.ToInt32(gridWidth - verticalGapSize) / gridUnits;
                }
                else
                {
                    unitSize = Convert.ToInt32(gridWidth - verticalGapSize);
                }
            }

            // rows
            //top thick line
            /*
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
            

            topLine.StrokeThickness = 3;
            topLine.StrokeDashArray = lineStyle;
            // lineGrid.Children.Add(topLine);
            lines.Add(topLine);
            */

            Line bottomLine = new Line();
            bottomLine.Stroke = System.Windows.Media.Brushes.Black;

            if (rotation == 0)
            {
                bottomLine.X1 = 0;
                bottomLine.X2 = gridWidth;
                bottomLine.Y1 = gridHeight - verticalGapSize;
                bottomLine.Y2 = gridHeight - verticalGapSize;
            }
            if (rotation == 1)
            {
                bottomLine.X1 = gridWidth - horizontalGapSize;
                bottomLine.X2 = gridWidth - horizontalGapSize;
                bottomLine.Y1 = 0;
                bottomLine.Y2 = gridHeight;
            }
            

            bottomLine.StrokeThickness = 3;
            bottomLine.StrokeDashArray = lineStyle;
            lines.Add(bottomLine);
            

            // rows
            for (int i = 0; i < gridUnits + 1; i++)
            {
               
                Line midLine = new Line();
                midLine.Stroke = System.Windows.Media.Brushes.Black;
                
                TextBlock leftNumber;
                TextBlock rightNumber;
                
                leftNumber = new TextBlock();
                leftNumber.Text = i.ToString();
                
                
                rightNumber = new TextBlock();
                rightNumber.Text = i.ToString();

                TransformGroup tg = new TransformGroup();
                ScaleTransform st = new ScaleTransform(sideNumberSize, sideNumberSize, 5, 5);

                if (rotation == 0)
                {
                    midLine.X1 = 0;
                    midLine.X2 = gridWidth;
                    midLine.Y1 = gridHeight - verticalGapSize - (i * unitSize);
                    midLine.Y2 = gridHeight - verticalGapSize - (i * unitSize);

                    tg.Children.Add(st);
                    leftNumber.RenderTransform = tg;
                    rightNumber.RenderTransform = tg;
                    
                    leftNumber.Margin = new Thickness(
                        horizontalGapSize - 10, 
                        gridHeight - verticalGapSize - (i * unitSize) - 5, 
                        0, 
                        0);

                    rightNumber.Margin = new Thickness(
                        //Math.Round((gridWidth / gridUnits), 1) * gridUnits + 5,
                        Math.Floor(gridWidth / gridUnits) * gridUnits - 20,
                        gridHeight - verticalGapSize - (i * unitSize) - 5,
                        0,
                        0);

                    
                    

                    
                }
                if (rotation == 1)
                {
                    midLine.X1 = gridWidth - verticalGapSize - (i * unitSize);
                    midLine.X2 = gridWidth - verticalGapSize - (i * unitSize);
                    midLine.Y1 = 0;
                    midLine.Y2 = gridHeight;


                    RotateTransform rt = new RotateTransform(-90);
                    
                    tg.Children.Add(rt);
                    tg.Children.Add(st);
                    leftNumber.RenderTransform = tg;
                    rightNumber.RenderTransform = tg;
                    
                    leftNumber.Margin = new Thickness(
                        gridWidth - verticalGapSize - (i * unitSize) - 5,
                        gridHeight - verticalGapSize + 20,
                        0,
                        0);

                    rightNumber.Margin = new Thickness(
                        //Math.Round((gridWidth / gridUnits), 1) * gridUnits + 5,
                        gridWidth - verticalGapSize - (i * unitSize) - 5,
                        50,
                        0,
                        0);

                }
                midLine.StrokeThickness = 1;
                midLine.StrokeDashArray = lineStyle;
                lines.Add(midLine);
                numbers.Add(leftNumber);
                numbers.Add(rightNumber);
                
            }

            // columns
            for (int j = 0; j < gridWidth / gridUnits; j++)
            {
                Line midLine = new Line();
                midLine.Stroke = System.Windows.Media.Brushes.Black;
                if (rotation == 0)
                {
                    midLine.Y1 = 0;
                    midLine.Y2 = gridHeight;
                    midLine.X1 = horizontalGapSize + (j * unitSize);
                    midLine.X2 = horizontalGapSize + (j * unitSize);
                }
                /*
                if (rotation == 1)
                {
                    midLine.Y1 = j * unitSize;
                    midLine.Y2 = j * unitSize;
                    midLine.X1 = 0;
                    midLine.X2 = gridWidth;
                }
                */
                if (rotation == 1)
                {
                    midLine.Y1 = j * unitSize;
                    midLine.Y2 = j * unitSize;
                    midLine.X1 = 0;
                    midLine.X2 = gridWidth;
                }

                midLine.StrokeThickness = 1;
                midLine.StrokeDashArray = lineStyle;
                lines.Add(midLine);
            }

            // fill in the gaps -- rows
            if (verticalGapSize > unitSize)
            {
                for (int i = 0; i < (verticalGapSize / unitSize); i++) 
                {
                    Line gapLine = new Line();
                    gapLine.Stroke = System.Windows.Media.Brushes.Black;
                    if (rotation == 0)
                    {
                        gapLine.X1 = 0;
                        gapLine.X2 = gridWidth;
                        gapLine.Y1 = gridHeight - verticalGapSize + unitSize + (i * unitSize);
                        gapLine.Y2 = gridHeight - verticalGapSize + unitSize + (i * unitSize);
                    }
                    if (rotation == 1)
                    {
                        gapLine.X1 = gridWidth - unitSize - (i * unitSize);
                        gapLine.X2 = gridWidth - unitSize - (i * unitSize);
                        gapLine.Y1 = 0;
                        gapLine.Y2 = gridHeight;
                    }
                    gapLine.StrokeThickness = 1;
                    gapLine.StrokeDashArray = lineStyle;
                    lines.Add(gapLine);
                }
            }

            // fill in the gaps -- columns
            if (horizontalGapSize > unitSize)
            {
                for (int j = 0; j < horizontalGapSize / unitSize; j++)
                {
                    Line gapLine = new Line();
                    gapLine.Stroke = System.Windows.Media.Brushes.Black;
                    if (rotation == 0)
                    {
                        gapLine.Y1 = 0;
                        gapLine.Y2 = gridHeight;
                        gapLine.X1 = horizontalGapSize + (j * unitSize);
                        gapLine.X2 = horizontalGapSize + (j * unitSize);
                    }
                    if (rotation == 1)
                    {
                        gapLine.Y1 = j * unitSize;
                        gapLine.Y2 = j * unitSize;
                        gapLine.X1 = 0;
                        gapLine.X2 = gridWidth;
                    }
                    gapLine.StrokeThickness = 1;
                    gapLine.StrokeDashArray = lineStyle;
                    lines.Add(gapLine);
                }
            }


            // add numbers 
            // left and right sides



        }
    }

    public class ToleranceSettings
    {
        // toleranceMode:
        // true = absolute tolerance (tolerance is bounded by actual hand heights -- 
        //          number of inches above/below hand)
        // false = proportional tolerance (tolerance is bounded by proportion values --
        //          percentage above/below hand)
        bool toleranceMode = false;
        double maxHandPosition;
        double toleranceLeftHand;
        double toleranceRightHand;
        public double calibratedLeftHandPos;
        public double calibratedRightHandPos;

        //Kinect Runtime
        double leftHandRatio;
        double rightHandRatio;

        double proportion;

        double proportionalTolerance;   // a range of proportions
        double proportionalGreenYellowFade;
        double proportionalYellowRedFade;

        double absoluteTolerance;       // a pixel distance
        double absoluteGreenYellowFade;
        double absoluteYellowRedFade;

        // proportion gradient points
        double bottomTolerance;
        double topTolerance;
        double topYellow;
        double bottomYellow;
        double topRed;
        double bottomRed;

        SolidColorBrush[] brushArray;
        SolidColorBrush greenBrush = new SolidColorBrush(Colors.Green);
        SolidColorBrush redBrush = new SolidColorBrush(Colors.Red);

        public ToleranceSettings()
        {            

            // these vars are constrained
            leftHandRatio = 1;
            rightHandRatio = 1;
            proportion = 1; // can only have 2 decimal digits!!
            
            
            absoluteTolerance = 10;
            absoluteGreenYellowFade = 5;
            absoluteYellowRedFade = 5;
            
            proportionalTolerance = .1;
            proportionalGreenYellowFade = .01;
            proportionalYellowRedFade = .01;

            recalculateGradientPoints();
            
        }

        public void boxChanged(string doubleName, string text)
        {
            try
            {
                switch (doubleName)
                {
                    case "absoluteTolerance":
                        setDouble(ref absoluteTolerance, text);
                        break;
                    case "absoluteGreenYellowFade":
                        setDouble(ref absoluteGreenYellowFade, text);
                        break;
                    case "absoluteYellowRedFade":
                        setDouble(ref absoluteYellowRedFade, text);
                        break;
                    case "proportionalTolerance":
                        setDouble(ref proportionalTolerance, text);
                        break;
                    case "proportionalYellowRedFade":
                        setDouble(ref proportionalYellowRedFade, text);
                        break;
                    case "proportionalGreenYellowFade":
                        setDouble(ref proportionalGreenYellowFade, text);
                        break;
                        
                }
            }
            catch (FormatException ex)
            {
                throw ex;
            }
        }

        private void setDouble(ref Double d, string text)
        {
            if (text.Trim() != "")
            {
                Double oldValue = d;
                try
                {
                    Double value = Double.Parse(text);
                    d = value;
                }
                catch (FormatException ex)
                {
                    d = oldValue;
                    throw ex;
                }

            }
        }

        private void updateToleranceSettings()
        {
            recalculateProportion();
            recalculateGradientPoints();
        }

        /// <summary>
        /// sets interpolation boundaries. e.g., bottomTolerance and topTolerance are 1.9 and 2.1 when proportion = 2 and tolerance = 0.1.
        /// bottomRed, bottomYellow, bottomTolerance (green) are points on the proportion spectrum BELOW the secret proportion where solid colors will appear.
        /// Not used in absolute tolerance mode, where tolerance limits are calculated in real time.
        /// </summary>
        private void recalculateGradientPoints()
        {
            // double arithmetic precision sucks, so round numbers to two decimal places
            // (this is the precision we are working with, after all)
            
            // proportional tolerance
            if (toleranceMode == false)
            {
                bottomTolerance = proportion - (proportionalTolerance / 2);
                bottomTolerance = Math.Round(bottomTolerance, 3);
                topTolerance = proportion + (proportionalTolerance / 2);
                topTolerance = Math.Round(topTolerance, 3);
                topYellow = proportion + (proportionalTolerance / 2) + proportionalGreenYellowFade;
                topYellow = Math.Round(topYellow, 3);
                bottomYellow = proportion - (proportionalTolerance / 2) - proportionalGreenYellowFade;
                bottomYellow = Math.Round(bottomYellow, 3);
                topRed = proportion + proportionalGreenYellowFade + proportionalYellowRedFade + (proportionalTolerance / 2);
                topRed = Math.Round(topRed, 3);
                bottomRed = proportion - proportionalGreenYellowFade - proportionalYellowRedFade - (proportionalTolerance / 2);
                bottomRed = Math.Round(bottomRed, 3);
            }
            

        }

        public void setToleranceMode(bool mode)
        {
            toleranceMode = mode;
            if (mode == false)
            {
                recalculateGradientPoints();
            }
        }

        //public void setTolerance(int toleranceValue)
        //{
        //    if (toleranceValue <= 50 && toleranceValue >= 0)
        //    {

        //        proportionalTolerance = (double)toleranceValue / 100;
        //    }
        //}


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

        /*
        /// <summary>
        /// deprecated. application doesn't use this method anymore.
        /// </summary>
        private void createColorArray()
        {
            int arraySize = Convert.ToInt32((topRed - bottomRed) / arrayPrecision);
            brushArray = new SolidColorBrush[arraySize];

            int arrayIndex = 0;
            int yellowRedFadeColors = Convert.ToInt32(yellowRedFade / arrayPrecision);
            int greenYellowFadeColors = Convert.ToInt32(greenYellowFade / arrayPrecision);
            int greenColors = Convert.ToInt32(tolerance / arrayPrecision);
            
            // red to yellow
            for (double map = 0, index = arrayIndex; index < yellowRedFadeColors; map += (arrayPrecision/yellowRedFade), index++)
            {
                map = Math.Round(map, 3);
                Color color = ColorInterpolator.InterpolateBetween(Colors.Red, Colors.Yellow, map);
                arrayIndex = Convert.ToInt32(index);
                SolidColorBrush newBrush = new SolidColorBrush(color);
                brushArray[arrayIndex] = newBrush;
                brushArray[arraySize - arrayIndex - 1] = newBrush;
            }
            // at this point, arrayIndex = yellowRedFadeColors
           
            // yellow to green
            for (double map = 0, index = arrayIndex; index < yellowRedFadeColors + greenYellowFadeColors; map += (arrayPrecision / greenYellowFade), index++)
            {
                map = Math.Round(map, 3);
                Color color = ColorInterpolator.InterpolateBetween(Colors.Yellow, Colors.Green, map);
                arrayIndex = Convert.ToInt32(index);
                SolidColorBrush newBrush = new SolidColorBrush(color);
                brushArray[arrayIndex] = newBrush;
                brushArray[arraySize - arrayIndex - 1] = newBrush;
            }
            // at this point, arrayIndex = yellowRedFadeColors + greenYellowFadeColors
            
            // half of green
            
            for (double map = 0, index = arrayIndex; index < yellowRedFadeColors + greenYellowFadeColors + greenColors; map += (arrayPrecision / greenYellowFade), index++)
            {
                arrayIndex = Convert.ToInt32(index);
                brushArray[arrayIndex] = greenBrush;
            }
        }
         * */

        
        
        public SolidColorBrush getCurrentColor(SmoothSkeleton skeleton)
        {
            // top of screen = 0
            // bottom of screen = 480
            double leftHandPosition = skeleton.leftOutput;
            double rightHandPosition = skeleton.rightOutput;

            calibratedLeftHandPos = -(leftHandPosition - CalibrationSettings.calibrationBaseline);
            if (calibratedLeftHandPos == 0)
            {
                calibratedLeftHandPos += .0001;
            }

            calibratedRightHandPos = -(rightHandPosition - CalibrationSettings.calibrationBaseline);
            if (calibratedRightHandPos == 0)
            {
                calibratedRightHandPos += .0001;
            }

            // SET THE CROSSHAIRS
            

            double currentProportion = Math.Round(calibratedLeftHandPos / calibratedRightHandPos, 3);
            

            // implement relative tolerance

            // display current proportion

            // proportional tolerance
            if (toleranceMode == false)
            {

                if (currentProportion < topTolerance && currentProportion > bottomTolerance)
                {
                    return greenBrush;
                }

                // bottom red -> yellow
                else if (currentProportion > bottomRed && currentProportion < bottomYellow)
                {
                    //int index = Convert.ToInt32((currentProportion - bottomRed) * (1 / arrayPrecision)) - 1;
                    //return brushArray[index];
                    double map = (1 / proportionalYellowRedFade) * (currentProportion - bottomRed);
                    return new SolidColorBrush(ColorInterpolator.InterpolateBetween(Colors.Red, Colors.Yellow, map));

                }
                // bottom yellow -> green
                else if (currentProportion > bottomYellow && currentProportion < bottomTolerance)
                {
                    double map = (1 / proportionalGreenYellowFade) * (currentProportion - bottomYellow);
                    return new SolidColorBrush(ColorInterpolator.InterpolateBetween(Colors.Yellow, Colors.Green, map));
                }
                // top green -> yellow
                else if (currentProportion > topTolerance && currentProportion < topYellow)
                {
                    double map = (1 / proportionalGreenYellowFade) * (currentProportion - topTolerance);
                    return new SolidColorBrush(ColorInterpolator.InterpolateBetween(Colors.Green, Colors.Yellow, map));
                }
                // top yellow -> red
                else if (currentProportion > topYellow && currentProportion < topRed)
                {
                    double map = (1 / proportionalYellowRedFade) * (currentProportion - topYellow);
                    return new SolidColorBrush(ColorInterpolator.InterpolateBetween(Colors.Yellow, Colors.Red, map));
                }
                else return redBrush;
            }


            // absolute tolerance:
            // height region that the lower hand moves through where screen is green. e.g., if absolute tolerance = 10,
            // and the proportion is 1:2 (left:right), when the right is at 200, the left hand can be anywhere between 
            // (100 - 10) and (100 + 10).
            else
            {
                
                
                // figure out the height of the highest hand
                double higherHandHeight = Math.Max(calibratedLeftHandPos, calibratedRightHandPos);
                double lowerHandHeight = Math.Min(calibratedLeftHandPos, calibratedRightHandPos);

                //
                double heightThatLowerHandShouldBeAt = proportion * higherHandHeight;
                double upperAbsoluteToleranceBound = heightThatLowerHandShouldBeAt + absoluteTolerance;
                double lowerAbsoluteToleranceBound = heightThatLowerHandShouldBeAt - absoluteTolerance;

                topYellow = upperAbsoluteToleranceBound + absoluteGreenYellowFade;
                topRed = upperAbsoluteToleranceBound + absoluteGreenYellowFade + absoluteYellowRedFade;

                bottomYellow = lowerAbsoluteToleranceBound - absoluteGreenYellowFade;
                bottomRed = lowerAbsoluteToleranceBound - absoluteGreenYellowFade - absoluteYellowRedFade;


                if (lowerHandHeight < upperAbsoluteToleranceBound 
                    && lowerHandHeight > lowerAbsoluteToleranceBound)
                {
                    return greenBrush;
                }

                // bottom red -> yellow
                else if (lowerHandHeight > bottomRed
                    && lowerHandHeight < bottomYellow)
                {
                    double map = (lowerHandHeight - bottomRed) / absoluteYellowRedFade;
                    return new SolidColorBrush(ColorInterpolator.InterpolateBetween(Colors.Red, Colors.Yellow, map));
                }

                // bottom yellow -> green
                else if (lowerHandHeight > bottomYellow 
                    && lowerHandHeight < lowerAbsoluteToleranceBound)
                {
                    double map = (lowerHandHeight - bottomYellow) / absoluteGreenYellowFade;
                    return new SolidColorBrush(ColorInterpolator.InterpolateBetween(Colors.Yellow, Colors.Green, map));
                }

                // top green -> yellow
                else if (lowerHandHeight > upperAbsoluteToleranceBound 
                    && lowerHandHeight < topYellow)
                {
                    double map = (lowerHandHeight - upperAbsoluteToleranceBound) / absoluteGreenYellowFade;
                    return new SolidColorBrush(ColorInterpolator.InterpolateBetween(Colors.Green, Colors.Yellow, map));
                }
                // top yellow -> red
                else if (lowerHandHeight > topYellow 
                    && lowerHandHeight < topRed)
                {
                    double map = (lowerHandHeight - topYellow) / absoluteYellowRedFade;
                    return new SolidColorBrush(ColorInterpolator.InterpolateBetween(Colors.Yellow, Colors.Red, map));
                }
                else return redBrush;
            }
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
        public static bool inCalibrationMode;
        public static double calibrationBaseline;
        public static bool isCalibrated;

        public CalibrationSettings()
        {
            inCalibrationMode = false;
            calibrationBaseline = 0;
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
        // TODO: parameter should be smoothSkeleton
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
                inCalibrationMode = false;
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

                    if (calibrationBaseline > 0)
                    {
                        isCalibrated = true;
                        
                    }
                }
                inCalibrationMode = false;
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

