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
        MainWindow colorWindow = new MainWindow();
        System.Windows.Threading.DispatcherTimer myDispatcherTimer;

        // tilt
        int tiltValue;

        
        
        // depth + vga viewer
        SkeletalViewer.MainWindow viewer = new SkeletalViewer.MainWindow();
        bool viewerOpen = false;

        public Window1()
        {

            InitializeComponent();
            colorWindow.Show();
            
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
            colorWindow.gridUnits = Convert.ToInt32(numberOfGridUnits.Text);

        }

        private void setGridUnits_Click(object sender, RoutedEventArgs e)
        {
            
            colorWindow.setGridUnits(colorWindow.gridUnits);
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
    }
}
