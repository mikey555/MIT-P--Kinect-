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

namespace SkeletalTracking
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    
    


    
    public partial class Window1 : Window
    {
        // public static MainWindow colorWindow = new MainWindow();
        public static MainWindow colorWindow;
        GridSetup gridSetup;
        
        public Window1()
        {
            colorWindow = new MainWindow();
            InitializeComponent();

            // initialize grid
            gridSetup = new GridSetup(colorWindow.Height, colorWindow.Width);
            this.updateGrid();



            colorWindow.Show();
            
            

        }

        public void updateGrid() {
            colorWindow.updateGrid(gridSetup.lines);
        }

        System.Windows.Threading.DispatcherTimer myDispatcherTimer;

        // tilt
        int tiltValue;

        
        
        // depth + vga viewer
        SkeletalViewer.MainWindow viewer = new SkeletalViewer.MainWindow();
        bool viewerOpen = false;

        

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
                colorWindow.setCalibrationMode(true);
                
                colorWindow.callCalibrate();
                
                calibrateButton.SetValue(IsEnabledProperty, true);
                calibrateButton.Content = "Calibrate";
                countDown = 5;

            }
            else {
                colorWindow.sendMessage("Calibrating in... " + countDown--.ToString());
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
                colorWindow.setTolerance(value);
            }
        }

   

        private void tiltSlider_ValueChanged_1(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            tiltValue = Convert.ToInt32(tiltSlider.Value);
        }

        private void setTiltButton_Click(object sender, RoutedEventArgs e)
        {
            colorWindow.setTilt(tiltValue);
        }

        private void toleranceOption1_Checked(object sender, RoutedEventArgs e)
        {
            colorWindow.setToleranceMode(false);
        }

        private void toleranceOption2_Checked(object sender, RoutedEventArgs e)
        {
            colorWindow.setToleranceMode(true);
        }

        private void setToleranceRange_Click(object sender, RoutedEventArgs e)
        {
            colorWindow.setMinTolerance(Convert.ToInt32(minToleranceBox.Text));
            colorWindow.setMaxTolerance(Convert.ToInt32(maxToleranceBox.Text));
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
            //colorWindow.gridUnits = Convert.ToInt32(numberOfGridUnits.Text);
            try {
            gridSetup.gridUnits = Convert.ToInt32(numberOfGridUnits.Text);
            }
            catch (Exception ex) {}

        }

        private void setGridUnits_Click(object sender, RoutedEventArgs e)
        {
            if (CCWRotation.IsChecked == true)
            {
                gridSetup.setGridUnits(gridSetup.gridUnits, -1);
            }
            else if (CWRotation.IsChecked == true)
            {
                gridSetup.setGridUnits(gridSetup.gridUnits, 1);
            }
            else
            {
                gridSetup.setGridUnits(gridSetup.gridUnits, 0);
            }
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
                MainWindow.SmoothSkeleton.setM(Convert.ToDouble(mBox.Text));
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

        private void rotate_Checked(object sender, RoutedEventArgs e)
        {
            
            colorWindow.SetValue(WidthProperty, SystemParameters.VirtualScreenWidth);
        }

        private void rotate_Unchecked(object sender, RoutedEventArgs e)
        {
            double testWidth = 800;
            colorWindow.SetValue(WidthProperty, testWidth);
            
            
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            colorWindow.updateGrid(this.gridSetup.lines);
        }
       
    }

    public class GridSetup
    {
        
        int gapSize;                 // crosshair height: 100
        DoubleCollection lineStyle;
        public int gridUnits;
        double gridWidth;
        double gridHeight;
        int rotation;
        public System.Collections.ArrayList lines;


        public GridSetup(double windowHeight, double windowWidth)
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

        public void resize(int windowHeight, int windowWidth, int rotation)
        {
            gridHeight = windowHeight;
            gridWidth = windowWidth;
            setGridUnits(gridUnits, rotation);

        }

        // 90deg counter-clockwise, rotation = -1
        // 0deg, developer screen, rotation = 0
        // 90deg, clockwise, rotation = 1
        // gridUnits: number of vertical squares that the crosshairs will move through
        public void setGridUnits(int gridUnits, int rotation)
        {
            lines.Clear();

            this.gridUnits = gridUnits;
            this.rotation = rotation;

            // gridsize: pixel height of one box
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
        
    
}

