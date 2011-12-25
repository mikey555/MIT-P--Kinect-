/////////////////////////////////////////////////////////////////////////
//
// This module contains code to do a basic green screen.
//
// Copyright © Microsoft Corporation.  All rights reserved.  
// This code is licensed under the terms of the 
// Microsoft Kinect for Windows SDK (Beta) from Microsoft Research 
// License Agreement: http://research.microsoft.com/KinectSDK-ToU
//
/////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Research.Kinect.Nui;
using Coding4Fun.Kinect.Wpf;
using Kinect.Extensions;

namespace SkeletalTracking
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Runtime nui;

        public MainWindow()
        {
            InitializeComponent();
            nui = Runtime.Kinects[0];
            
         }

        // Grid setup




        int gapSize = 30;
        DoubleCollection lineStyle = new DoubleCollection();
        public void setGridUnits(int gridUnits) 
        {
            lineGrid.Children.Clear();
            
            int gridSize = Convert.ToInt32(lineGrid.Height - gapSize*2) / gridUnits;
            
            // rows
            Line topLine = new Line();
            topLine.Stroke = System.Windows.Media.Brushes.Black;
            topLine.X1 = 0;
            topLine.X2 = 900;
            topLine.Y1 = gapSize;
            topLine.Y2 = gapSize;
            topLine.StrokeThickness = 1;
            topLine.StrokeDashArray = lineStyle;
            lineGrid.Children.Add(topLine);

            Line bottomLine = new Line();
            bottomLine.Stroke = System.Windows.Media.Brushes.Black;
            bottomLine.X1 = 0;
            bottomLine.X2 = 900;
            bottomLine.Y1 = lineGrid.Height - gapSize;
            bottomLine.Y2 = lineGrid.Height - gapSize;
            bottomLine.StrokeThickness = 1;
            bottomLine.StrokeDashArray = lineStyle;
            lineGrid.Children.Add(bottomLine);

            for (int i = 1; i < gridUnits ; i++)
            {
                Line midLine = new Line();
                midLine.Stroke = System.Windows.Media.Brushes.Black;
                midLine.X1 = 0;
                midLine.X2 = 900;
                midLine.Y1 = i * gridSize + gapSize;
                midLine.Y2 = i * gridSize + gapSize;
                midLine.StrokeThickness = 1;
                midLine.StrokeDashArray = lineStyle;
                lineGrid.Children.Add(midLine);
            }

            // columns
            for (int j = 1; j < lineGrid.Width / gridUnits; j++)
            {
                Line midLine = new Line();
                midLine.Stroke = System.Windows.Media.Brushes.Black;
                midLine.Y1 = 0;
                midLine.Y2 = lineGrid.Height;
                midLine.X1 = j * gridSize;
                midLine.X2 = j * gridSize;
                midLine.StrokeThickness = 1;
                midLine.StrokeDashArray = lineStyle;
                lineGrid.Children.Add(midLine);
            }


            // <Grid Height="561" Canvas.Left="0" Canvas.Top="0" Width="778" >
            

            /*
            grid1.RowDefinitions.Clear();
            

            RowDefinition topRow = new RowDefinition();
            topRow.MaxHeight = 40;
            topRow.MinHeight = 40;
            grid1.RowDefinitions.Add(topRow);

            for (int i = 0; i < gridUnits; i++)
            {
                grid1.RowDefinitions.Add(new RowDefinition());
            }

            RowDefinition bottomRow = new RowDefinition();
            bottomRow.MaxHeight = 40;
            bottomRow.MinHeight = 40;
            grid1.RowDefinitions.Add(bottomRow);
            */
        }

        public void showGridUnits()
        {
            
            lineGrid.SetValue(VisibilityProperty, Visibility.Visible);
        }

        public void hideGridUnits()
        {
             lineGrid.SetValue(VisibilityProperty, Visibility.Hidden);
        }

















        // tolerance:
        // true = relative tolerance
        // false = absolute tolerance
        bool toleranceMode = false;
        double maxHandPosition;
        double maxTolerance = 0;
        double minTolerance = 0;
        double toleranceLeftHand;
        double toleranceRightHand;

        //Kinect Runtime
        double leftHandRatio = 1;
        double rightHandRatio = 1;

        // right over left: 0
        // left over right: 1
        // int handOrientation = 0;
        
        double proportion = 1.33;     // [0, 1]
        double tolerance = .2;      // [0, .5]
        double greenYellowFade = .01;
        double yellowRedFade = .01;


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
                
                tolerance = (double) toleranceValue / 100;
            }
        }


        SkeletonData skeleton;
        bool inCalibrationMode = false;
        bool crosshairsEnabled = true;
        double crosshairRate;
        public void setCrosshairs()
        {
            if (crosshairsEnabled == true)
            {
                crosshair1.SetValue(VisibilityProperty, Visibility.Hidden);
                crosshair2.SetValue(VisibilityProperty, Visibility.Hidden);
                crosshairsEnabled = false;
            }
            else
            {
                crosshair1.SetValue(VisibilityProperty, Visibility.Visible);
                crosshair2.SetValue(VisibilityProperty, Visibility.Visible);
                crosshairsEnabled = true;
            }
        }

        public void setCalibrationMode(bool value)
        {
            if (!inCalibrationMode)
            {
                inCalibrationMode = value;
            }
        }

        
        
        public void sendMessage(string message)
        {
            messageBlock.Text = message;
        }
        


        double calibrationBaseline = 0;

        public void callCalibrate() {
            calibrate(skeleton);
        }

        public void calibrate(SkeletonData skeleton)
        {
            bool isCalibrated = false;
            
            while (!isCalibrated) {
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
                crosshairRate = (this.Height / -calibrationBaseline) * 1.1;

                if (calibrationBaseline > 0)
                {
                    isCalibrated = true;            
                }
            }

        sendMessage("calibrated!");
        }

        public void setTilt(int angle)
        {
            if (nui.NuiCamera.TrySetAngle(angle))
                sendMessage("Moving the sensor..");
            else
                sendMessage("Error occured moving the sensor..");


        }


    


        //public void changeHandOrientation(int handOrientationValue)
        //{
        //    if (handOrientation != handOrientationValue)
        //    {
        //        handOrientation = handOrientationValue;
        //        if (handOrientation == 1)
        //        {
        //            proportion = leftHandRatio / rightHandRatio;
        //        }
        //        if (handOrientation == 0)
        //        {
        //            proportion = rightHandRatio / leftHandRatio;
        //        }
        //    }
        //}


        public void changeLeftHandValue(double leftValue)
        {
            leftHandRatio = leftValue;
            changeProportion(leftHandRatio, rightHandRatio);
        }

        public void changeRightHandValue(double rightValue)
        {
            rightHandRatio = rightValue;
            changeProportion(leftHandRatio, rightHandRatio);
        }

        private void changeProportion (double leftValue, double rightValue) 
        {
            proportion = rightHandRatio / leftHandRatio;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {            

            //Initialize to do skeletal tracking
            nui.Initialize(RuntimeOptions.UseSkeletalTracking);

            #region TransformSmooth
            //Must set to true and set after call to Initialize
            nui.SkeletonEngine.TransformSmooth = true;

            //Use to transform and reduce jitter
            var parameters = new TransformSmoothParameters
            {
                Smoothing = .1f, // default .5f
                Correction = 0.1f,
                Prediction = 0.0f,
                JitterRadius = 0.2f, // default .005f
                MaxDeviationRadius = 0.8f
            };

            nui.SkeletonEngine.SmoothParameters = parameters; 

            #endregion

            //add event to receive skeleton data
            nui.SkeletonFrameReady += new EventHandler<SkeletonFrameReadyEventArgs>(nui_SkeletonFrameReady);

          
            

        }

        void nui_SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            
            SkeletonFrame allSkeletons = e.SkeletonFrame;

            //get the first tracked skeleton
            //SkeletonData skeleton = (from s in allSkeletons.Skeletons
            // skeleton is of type SkeletonData, reference is global
            
            skeleton = (from s in allSkeletons.Skeletons
                                     where s.TrackingState == SkeletonTrackingState.Tracked
                                     select s).FirstOrDefault();
            
            // set background color
            if (skeleton != null)
            {
                SetColor(proportion, skeleton);
            }

        }


        private void SetColor(double setProportion, SkeletonData skeleton)
        {
            var scaledLeftHand = skeleton.Joints[JointID.HandLeft].ScaleTo(640, 480, .5f, .5f);
            var scaledRightHand = skeleton.Joints[JointID.HandRight].ScaleTo(640, 480, .5f, .5f);

            
            // top of screen = 0
            // bottom of screen = 480
            double leftHandPosition = scaledLeftHand.Position.Y;
            double rightHandPosition = scaledRightHand.Position.Y;

           

            crosshair1.SetValue(Canvas.BottomProperty, (leftHandPosition - calibrationBaseline) * crosshairRate);
            crosshair2.SetValue(Canvas.BottomProperty, (rightHandPosition - calibrationBaseline) * crosshairRate);
            

            double calibratedLeftHandPosition = scaledLeftHand.Position.Y - calibrationBaseline;
            double calibratedRightHandPosition = scaledRightHand.Position.Y - calibrationBaseline;
            if (calibratedRightHandPosition == 0)
            {
                calibratedRightHandPosition += .0001;
            }

            // TODO Don't divide by zero
            double currentProportion = calibratedLeftHandPosition / calibratedRightHandPosition;
            
            
            // tolerance settings
            if (toleranceMode == true)            // relative tolerance is enabled
            {
                toleranceLeftHand = Math.Abs(leftHandPosition - 480);
                toleranceRightHand = Math.Abs(rightHandPosition - 480);

                if (toleranceLeftHand > toleranceRightHand) {
                    maxHandPosition = toleranceLeftHand;
                } else {
                    maxHandPosition = toleranceRightHand;
                }

               tolerance = (maxHandPosition * ((maxTolerance - minTolerance) / 480) + minTolerance) / 100;       // dimensions
               
                sendMessage("tolerance: " + tolerance.ToString());
            }
            

            // display heights
            textBlock1.Text = currentProportion.ToString();

            // determine gradient points
            double bottomTolerance = setProportion - tolerance;
            double topTolerance = setProportion + tolerance;
            double topYellow = setProportion + tolerance + greenYellowFade;
            double bottomYellow = setProportion - tolerance - greenYellowFade;
            double topRed = setProportion + tolerance + greenYellowFade + yellowRedFade;
            double bottomRed = setProportion - tolerance - greenYellowFade - yellowRedFade;

            // determine gradient mappings
            // slope*currentProportion - y-intercept
            double topToleranceYellowMap = ((1 / (greenYellowFade)) * currentProportion) - ((1 / (greenYellowFade)) * topTolerance);
            double topYellowRedMap = ((1 / (yellowRedFade)) * currentProportion) - ((1 / (greenYellowFade)) * topYellow);
            double bottomYellowToleranceMap = ((1 / (greenYellowFade)) * currentProportion) - ((1 / (greenYellowFade)) * bottomYellow);
            double bottomRedYellowMap = ((1 / (yellowRedFade)) * currentProportion) - ((1 / (greenYellowFade)) * bottomRed);

            
            
            System.Windows.Media.Color interpolatedColor = Colors.Red;
            if (currentProportion >= bottomTolerance &&
                currentProportion <= topTolerance)   // green
            {
                interpolatedColor = Colors.Green;
            }


            // green to yellow (top)
            else if (currentProportion > topTolerance &&
                currentProportion <= topYellow)
            {
                interpolatedColor = ColorInterpolator.InterpolateBetween(Colors.Green, Colors.Yellow, topToleranceYellowMap);
            }
            // green to yellow (bottom)
            else if (currentProportion < bottomTolerance &&
                currentProportion >= bottomYellow)
            {
                interpolatedColor = ColorInterpolator.InterpolateBetween(Colors.Yellow, Colors.Green, bottomYellowToleranceMap);
            }


            // yellow to red (top) 
            // (.57, .59]
            else if (currentProportion > topYellow &&
                currentProportion <= topRed)
            {
                interpolatedColor = ColorInterpolator.InterpolateBetween(Colors.Yellow, Colors.Red, topYellowRedMap);
            }
            // yellow to red (bottom)
            else if (currentProportion < bottomYellow &&
                currentProportion >= bottomRed)
            {
                interpolatedColor = ColorInterpolator.InterpolateBetween(Colors.Red, Colors.Yellow, bottomRedYellowMap);
            }
            else
                interpolatedColor = Colors.Red;

            //interpolatedColor = ColorInterpolator.InterpolateBetween(Colors.Yellow, Colors.Green, currentProportion);
            SolidColorBrush interpolatedColorBrush = new SolidColorBrush(interpolatedColor);

            MainCanvas.Background = interpolatedColorBrush;      // set Background to a colorString, e.g. "#113355FF"
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            //Cleanup
            nui.Uninitialize();
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
            return (byte)(selector(endPoint1)
                + (selector(endPoint2) - selector(endPoint1)) * lambda);
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