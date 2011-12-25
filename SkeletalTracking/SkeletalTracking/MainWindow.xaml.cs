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




        int gapSize = 50;
        DoubleCollection lineStyle = new DoubleCollection();
        public int gridUnits;
        public void setGridUnits(int gridUnits)
        {

            lineGrid.Children.Clear();
            lineGrid.SetValue(WidthProperty, ActualWidth);
            lineGrid.SetValue(HeightProperty, ActualHeight);

            int gridSize = Convert.ToInt32(lineGrid.Height - gapSize * 2) / gridUnits;
            if (gridUnits == 0)
            {
                gridSize = Convert.ToInt32(lineGrid.Height - gapSize * 2);
            }

            // rows
            Line topLine = new Line();
            topLine.Stroke = System.Windows.Media.Brushes.Black;
            topLine.X1 = 0;
            topLine.X2 = this.Width;
            topLine.Y1 = gapSize;
            topLine.Y2 = gapSize;
            topLine.StrokeThickness = 3;
            topLine.StrokeDashArray = lineStyle;
            lineGrid.Children.Add(topLine);

            Line bottomLine = new Line();
            bottomLine.Stroke = System.Windows.Media.Brushes.Black;
            bottomLine.X1 = 0;
            bottomLine.X2 = this.Width;
            bottomLine.Y1 = lineGrid.Height - gapSize;
            bottomLine.Y2 = lineGrid.Height - gapSize;
            bottomLine.StrokeThickness = 3;
            bottomLine.StrokeDashArray = lineStyle;
            lineGrid.Children.Add(bottomLine);

            for (int i = 1; i < gridUnits; i++)
            {
                Line midLine = new Line();
                midLine.Stroke = System.Windows.Media.Brushes.Black;
                midLine.X1 = 0;
                midLine.X2 = this.Width;
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

                tolerance = (double)toleranceValue / 100;
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

        public void callCalibrate()
        {
            calibrate(skeleton);
        }


        public bool checkForSkeleton()
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





        bool isCalibrated = false;
        public void calibrate(SkeletonData skeleton)
        {
            // reset isCalibrated
            if (isCalibrated == true)
            {
                isCalibrated = false;
            }

            // isCalibrated is false at this point
            // only attempt to calibrate if skeleton is not null
            if (checkForSkeleton() == false)
            {
                sendMessage("person not recognized");
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
                    crosshairRate = (this.Height / -calibrationBaseline) * 0.9;

                    if (calibrationBaseline > 0)
                    {
                        isCalibrated = true;
                    }
                }

                sendMessage("calibrated!");
            }
        }




        public void setTilt(int angle)
        {
            if (nui.NuiCamera.TrySetAngle(angle))
                sendMessage("Moving the sensor..");
            else
                sendMessage("Error occured moving the sensor..");


        }







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

        private void changeProportion(double leftValue, double rightValue)
        {
            proportion = leftHandRatio / rightHandRatio;
        }

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
        

        static bool smoothingEnabled = true;
        SmoothSkeleton smoothSkeleton;
        void nui_SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {

            SkeletonFrame allSkeletons = e.SkeletonFrame;
            //if (allSkeletons != null)
            //{
                // allSkeletons is not null iff allSkeletons has a skeleton?
                skeleton = (from s in allSkeletons.Skeletons                
                            where s.TrackingState == SkeletonTrackingState.Tracked
                            select s).FirstOrDefault();

                if (skeleton != null)
                {
                    var leftHandY = skeleton.Joints[JointID.HandLeft].ScaleTo(640, 480, .5f, .5f).Position.Y;
                    var rightHandY = skeleton.Joints[JointID.HandRight].ScaleTo(640, 480, .5f, .5f).Position.Y;


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





        //private void SetColor(double setProportion, SkeletonData skeleton)
        private void SetColor(double setProportion, SmoothSkeleton skeleton)
        {


            // top of screen = 0
            // bottom of screen = 480
            double leftHandPosition = skeleton.leftOutput;
            double rightHandPosition = skeleton.rightOutput;



            crosshair1.SetValue(Canvas.BottomProperty, Math.Min(0, leftHandPosition - calibrationBaseline) * crosshairRate);
            crosshair2.SetValue(Canvas.BottomProperty, Math.Min(0, rightHandPosition - calibrationBaseline) * crosshairRate);


            double calibratedLeftHandPosition = leftHandPosition - calibrationBaseline;
            double calibratedRightHandPosition = rightHandPosition - calibrationBaseline;
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

                if (toleranceLeftHand > toleranceRightHand)
                {
                    maxHandPosition = toleranceLeftHand;
                }
                else
                {
                    maxHandPosition = toleranceRightHand;
                }

                tolerance = (maxHandPosition * ((maxTolerance - minTolerance) / 480) + minTolerance) / 100;       // dimensions

                sendMessage("tolerance: " + tolerance.ToString());
            }


            // display heights
            textBlock1.Text = currentProportion.ToString();

            // determine gradient points
            double bottomTolerance = setProportion - (tolerance * 1/2);
            double topTolerance = setProportion + (tolerance * 1/2);
            double topYellow = setProportion + (tolerance * 1 / 2) + greenYellowFade;
            double bottomYellow = setProportion - (tolerance * 1 / 2) - greenYellowFade;
            double topRed = setProportion + (tolerance * 1 / 2) + greenYellowFade + yellowRedFade;
            double bottomRed = setProportion - (tolerance * 1 / 2) - greenYellowFade - yellowRedFade;

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

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {

            setGridUnits(gridUnits);

        }



        public class SmoothSkeleton
        {

            //double jitterRegion = 5;
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


            public static void setTrendSmoothingRate (double newRate)
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