using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using Renci.SshNet;
using System.Net;
using System.Threading;
using System.Diagnostics;

using Microsoft.Kinect;

namespace Kinect_Robot
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string hostname = "raspberrypi";
        string host;
        string user = "pi";
        string pass = "ray";
        string command;
        SshClient client;
        SshClient start;
        Boolean connected = false;
        Boolean keepstart = false;
        Thread StartingThread;

        DateTime Last_Date;

        KinectSensor sensor;//This initialise a variable of type KinectSensor which we will use downbelow 
        const double thickness_of_Joint = 4;//This is used to set the thickness of the dots reprsenting joints when we make the skeleton model
        Brush tracked_Joint_Point = Brushes.Green;//This is the colour of the dots reprsenting joints E.G. The neck, wrist ect
        Brush inferred_Joint_Point = Brushes.Yellow;//This is the colour of the dots reprsenting inferred joints E.G. joints it cant see but guess its there.
        Pen tracked_joint_bone = new Pen(Brushes.Red, 5);//This is the colour and line thickness of the lines that will connect the joints and make the bones.
        Pen inferredBonePen = new Pen(Brushes.Gray, 2);//This is the colour and line thickness of the lines that will connect the joints and make the bones which is guessed.
        /// <summary>
        /// Inferred vs Tracked. Tracked is when the Kinect can activly sees the joints, Infferred is when It guesses where a joint will be E.G. If the Kinect can see
        /// your wrist and sholder but not the rest of the arm it will try to guess where it is and draw it.
        /// </summary>

        DrawingGroup Skeleton_Draw_Group; //This  DrawingGroup allows me to bundle all the drawings into one group
        DrawingImage Skeleton_Image; //A DrawingImage type allows use to use a drawing in a image E.G. Skeleton_Draw_Group

        WriteableBitmap camera_Bitmap; //This creates a bitmap which we can keep writing pixels to, we will use this bitmap to display the camera output.
        byte[] colorPixels;


        public MainWindow()
        {
            InitializeComponent();
        }

        public Advance_Settings Advance_Settings
        {
            get
            {
                throw new System.NotImplementedException();
            }
            set
            {
            }
        }


        private void Window_Closed(object sender, EventArgs e)
        {
            if (connected)
            {
                Disconnect(); // If there is a connection the robot, disconnect upon exit
            }

            if (null != this.sensor)
            {
                this.sensor.Stop();// If there is a Kinect connected, disconnect upon exit
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            robot_connect_label.Content = "Not Connected";
            robot_connect_label.Background = Brushes.LightPink;
            robot_found_label.Content = "Results";
            current_host.Content = "Current Hostname: " + hostname;
            current_user.Content = "Current User: " + user;
            Last_Date = DateTime.Now;

            this.Skeleton_Draw_Group = new DrawingGroup();//This will create a group for our drawing.

            this.Skeleton_Image = new DrawingImage(this.Skeleton_Draw_Group);//This sets up the DrawingImage to use our drawing Skeleton_Image; 

            // Display the drawing using our image control
            Image_skeleton.Source = this.Skeleton_Image; // This sets our skeleton image, to the image on the Windows form

            foreach (var potentialSensor in KinectSensor.KinectSensors)
            {
                //At the beginning of the application, the application is going to look for the Kinect sensor. If it finds it it's going to set it to the variable sensor.
                if (potentialSensor.Status == KinectStatus.Connected)
                {
                    this.sensor = potentialSensor;
                    break;
                }
            }

            if (null != this.sensor)
            {
                /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                // Turn on the color stream to receive color frames
                this.sensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30); // This enables the colour stream, so we can get video output

                // Allocate space to put the pixels we'll receive
                this.colorPixels = new byte[this.sensor.ColorStream.FramePixelDataLength];

                this.camera_Bitmap = new WriteableBitmap(this.sensor.ColorStream.FrameWidth, this.sensor.ColorStream.FrameHeight, 96.0, 96.0, PixelFormats.Bgr32, null);
                //This is the bitmap that we are going to display on the screen. 

                this.Image_cam.Source = this.camera_Bitmap; //This sets the bitmap to the image on the Windows form.


                this.sensor.ColorFrameReady += this.SensorColorFrameReady;//This is an event handler which is activated every time there is a new frame

                /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

                // Turn on the skeleton stream to receive skeleton frames
                this.sensor.SkeletonStream.Enable();//This turns on the skeleton stream.

                this.sensor.SkeletonFrameReady += this.SensorSkeletonFrameReady;
                //This is an event handler which is activated every time it detects there is a new skeleton in the frame.

                // Start the sensor!
                try
                {
                    this.sensor.Start();
                }
                catch (IOException)
                {
                    this.sensor = null;
                }
            }

            if (null == this.sensor)
            {

            }
        }
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////// 
        private void SensorColorFrameReady(object sender, ColorImageFrameReadyEventArgs e)
        {
            //This is the event handler, for the video output of the connect. 
            //Every time there is a new frame this method is called, and the new frame is written into the bitmap. Which then appears on the screen
            using (ColorImageFrame colorFrame = e.OpenColorImageFrame())
            {
                if (colorFrame != null)
                {
                    // Copy the pixel data from the image to a temporary array
                    colorFrame.CopyPixelDataTo(this.colorPixels);

                    //Writes the image data into the bitmap
                    this.camera_Bitmap.WritePixels(
                        new Int32Rect(0, 0, this.camera_Bitmap.PixelWidth, this.camera_Bitmap.PixelHeight),
                        this.colorPixels,
                        this.camera_Bitmap.PixelWidth * sizeof(int),
                        0);
                }
            }
        }


        //This is the event handler for the skeleton tracking. It is called every time there's a new skeleton in the frame.
        void SensorSkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {

            Skeleton[] skeletons = new Skeleton[0];

            using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame())
            {
                if (skeletonFrame != null)
                {
                    skeletons = new Skeleton[skeletonFrame.SkeletonArrayLength];
                    skeletonFrame.CopySkeletonDataTo(skeletons);
                }
            }

            using (DrawingContext dc = this.Skeleton_Draw_Group.Open())
            {
                //This section is responsible for drawing the skeleton outline.
                // We create a transparent rectangle set to the same size as the image on the main form 640x480.
                //The rectangles is made transparent, so the skeleton overlays the video output
                dc.DrawRectangle(Brushes.Transparent, null, new Rect(0.0, 0.0, 640.0f, 480.0f));

                if (skeletons.Length != 0)
                {
                    foreach (Skeleton skel in skeletons)
                    {

                        if (skel.TrackingState == SkeletonTrackingState.Tracked)
                        {
                            KinectCommand(skel);//Every time there is new skeletal data, this sends the
                            //data to a method called KinectCommand , which searches for gestures.
                            this.DrawBonesAndJoints(skel, dc); //This method sets what part of the skeleton is to be drawn
                        }
                    }
                }

                // This prevents the drawings from being done outside of our specified area 640x480
                this.Skeleton_Draw_Group.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, 640.0f, 480.0f));
            }
        }


        // This method defines what section of the skeleton is going to be drawn, for our purposes, we only need to waist up
        void DrawBonesAndJoints(Skeleton skeleton, DrawingContext drawingContext)
        {
            // This will draw out, the torso of the body - this is required because our hand gestures will 
            //be compared against the hip. And so the user can see what section of his upper body is being tracked
            this.draw_The_Bone(skeleton, drawingContext, JointType.Head, JointType.ShoulderCenter);
            this.draw_The_Bone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.ShoulderLeft);
            this.draw_The_Bone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.ShoulderRight);
            this.draw_The_Bone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.Spine);
            this.draw_The_Bone(skeleton, drawingContext, JointType.Spine, JointType.HipCenter);
            this.draw_The_Bone(skeleton, drawingContext, JointType.HipCenter, JointType.HipLeft);
            this.draw_The_Bone(skeleton, drawingContext, JointType.HipCenter, JointType.HipRight);

            // This will draw out the left arm
            this.draw_The_Bone(skeleton, drawingContext, JointType.ShoulderLeft, JointType.ElbowLeft);
            this.draw_The_Bone(skeleton, drawingContext, JointType.ElbowLeft, JointType.WristLeft);
            this.draw_The_Bone(skeleton, drawingContext, JointType.WristLeft, JointType.HandLeft);

            // This will draw out the Right arm
            this.draw_The_Bone(skeleton, drawingContext, JointType.ShoulderRight, JointType.ElbowRight);
            this.draw_The_Bone(skeleton, drawingContext, JointType.ElbowRight, JointType.WristRight);
            this.draw_The_Bone(skeleton, drawingContext, JointType.WristRight, JointType.HandRight);

            //The defined skeleton, calls the method draws_the_bone, passing through the two points, 
            //which the bone we drawn from e.g. the left shoulder to the left elbow 

            //  This method creates the dots that will represent joints on the overlay. It will allow the user to see exactly how many joins of being tracked
            foreach (Joint joint in skeleton.Joints)
            {
                Brush drawBrush = null;

                if (joint.TrackingState == JointTrackingState.Tracked)
                {
                    drawBrush = this.tracked_Joint_Point;//This sets the attract joints
                }
                else if (joint.TrackingState == JointTrackingState.Inferred)
                {
                    drawBrush = this.inferred_Joint_Point;//And this sets the inferred joints
                }

                if (drawBrush != null)
                {
                    drawingContext.DrawEllipse(drawBrush, null, this.SkeletonPointToScreen(joint.Position), thickness_of_Joint, thickness_of_Joint);
                }
            }
        }


        //Because the skeleton is being mapped on a Live, the user may be moving back
        //and forward as well as left and right, this means that the distance from the
        //camera(depth) is changing. This method makes sure that our skeleton render is
        //within our output resolution of 640x 480 and is not distorted.
        private Point SkeletonPointToScreen(SkeletonPoint skelpoint)
        {
            // We are converting the point in which we want to display the joint, to take into the count of the depth of the user. 
            DepthImagePoint depthPoint = this.sensor.CoordinateMapper.MapSkeletonPointToDepthPoint(skelpoint, DepthImageFormat.Resolution640x480Fps30);
            return new Point(depthPoint.X, depthPoint.Y);
        }

        
        //This is the method used to draw a line between two points, the last two arguments. 
        //The constructor accepts- are the points from which lines will be drawn e.g., right elbow to right shoulder
        private void draw_The_Bone(Skeleton skeleton, DrawingContext drawingContext, JointType jointType0, JointType jointType1)
        {
            //The argument of JointType can either be tracked or inferred. 
            Joint joint0 = skeleton.Joints[jointType0];
            Joint joint1 = skeleton.Joints[jointType1];

            // If the joints that are sent cannot be found, then the method quits, exit
            if (joint0.TrackingState == JointTrackingState.NotTracked ||
                joint1.TrackingState == JointTrackingState.NotTracked)
            {
                return;
            }

            // If both the joints that are sent in our inferred, then we do not draw this
            if (joint0.TrackingState == JointTrackingState.Inferred &&
                joint1.TrackingState == JointTrackingState.Inferred)
            {
                return;
            }

            // Whilst drawing the bones, every bone is considered inferred unless both the joints are tracked
            Pen drawPen = this.inferredBonePen;
            if (joint0.TrackingState == JointTrackingState.Tracked && joint1.TrackingState == JointTrackingState.Tracked)
            {
                drawPen = this.tracked_joint_bone;
            }

            drawingContext.DrawLine(drawPen, this.SkeletonPointToScreen(joint0.Position), this.SkeletonPointToScreen(joint1.Position));
        }

        //This is the method used to, to see if any gestures are being performed.
        public void KinectCommand(Skeleton model)
        {
            if (connected) //Whilst the application is connected to the robot
            {
                var Current_Date = DateTime.Now;
                if (Current_Date.Subtract(Last_Date).TotalMilliseconds < 500) //We set up a timer so we are not constantly sending command's, a gap of half a second is in place
                    return;
                Last_Date = DateTime.Now;

                //if (StartingThread.IsAlive) { Debug.WriteLine("Its alive"); } else { Debug.WriteLine("Its dead"); }

                Joint Right_hand = model.Joints[JointType.HandRight];
                Joint Left_hand = model.Joints[JointType.HandLeft];
                Joint Right_shoulder = model.Joints[JointType.ShoulderRight];
                Joint Left_shoulder = model.Joints[JointType.ShoulderLeft];
                Joint Hip_center = model.Joints[JointType.HipCenter]; 


                if (Right_hand.Position.Y > Right_shoulder.Position.Y && Left_hand.Position.Y > Left_shoulder.Position.Y)
                {
                    var distance1 = (Right_hand.Position.Y - Right_shoulder.Position.Y) * 100;
                    var distance2 = (Left_hand.Position.Y - Left_shoulder.Position.Y) * 100;
                    var speedF = distance1 + distance2 + 15;
                    int speed = Convert.ToInt32(speedF);
                    Debug.WriteLine("Forward" + speed);
                    if (speed > 100)
                    {
                        speed = 100;
                    }
                    SendCommands("01", speed);
                }

                if (Right_shoulder.Position.Y > Right_hand.Position.Y && Right_hand.Position.Y > Hip_center.Position.Y &&
                    Left_shoulder.Position.Y > Left_hand.Position.Y && Left_hand.Position.Y > Hip_center.Position.Y)
                {
                    var speedF = ((Right_hand.Position.X - Left_hand.Position.X) * 100);
                    int speed = Convert.ToInt32(speedF);
                    if (speed > 100)
                    {
                        speed = 100;
                    }
                    SendCommands("02", speed);
                    Debug.WriteLine("Back" + speed);
                }

                if (Left_hand.Position.Y > Left_shoulder.Position.Y && Right_hand.Position.Y < Hip_center.Position.Y)
                {
                    Debug.WriteLine("Turn Left");
                    SendCommands("03", 00);
                }
                if (Left_hand.Position.Y < Hip_center.Position.Y && Right_hand.Position.Y > Right_shoulder.Position.Y)
                {
                    Debug.WriteLine("Turn Right");
                    SendCommands("04", 00);
                }
                if (Right_hand.Position.Y < Hip_center.Position.Y && Left_hand.Position.Y < Hip_center.Position.Y)
                {
                    Debug.WriteLine("Stop");
                    SendCommands("05", 00);
                }

            }
        }

        /// <summary>
        /// ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            DoGetHostEntry(hostname);

        }

        private void connect_robot_button_Click(object sender, RoutedEventArgs e)
        {
            StartingThread = new Thread(() => Startup());
            keepstart = true;
            StartingThread.Start();
            Connect();
        }

        private void advance_button_Click(object sender, RoutedEventArgs e)
        {
            Advance_Settings page = new Advance_Settings();
            page.ShowDialog();
            if (page.want_save())
            {
                hostname = page.hostname();
                user = page.user();
                pass = page.password();
                setcurrent();
            }
        }
        private void disconnect_button_Click(object sender, RoutedEventArgs e)
        {
            Disconnect();
        }
        private void forward_button_Click(object sender, RoutedEventArgs e)
        {
            SendCommands("01", 80);
        }

        private void back_button_Click(object sender, RoutedEventArgs e)
        {
            SendCommands("02", 80);
        }

        private void turn_left_button_Click(object sender, RoutedEventArgs e)
        {
            SendCommands("03", 85);
        }

        private void turn_right_button_Click(object sender, RoutedEventArgs e)
        {
            SendCommands("04", 85);
        }

        private void stop_button_Click(object sender, RoutedEventArgs e)
        {
            SendCommands("05", 00);
        }

        private void exit_button_Click(object sender, RoutedEventArgs e)
        {
            if (connected)
            {
                Disconnect();
            }
            Application.Current.Shutdown();
        }
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //Classes for ssh connection

        public void SendCommands(string direction, int speed)
        {
            int dir_number = Int32.Parse(direction);
            switch (dir_number)
            {
                case 1:
                    command = "echo '01-" + speed + "' | cat > control/Commands.txt";
                    SshStream();
                    break;
                case 2:
                    command = "echo '02-" + speed + "' | cat > control/Commands.txt";
                    SshStream();
                    break;
                case 3:
                    command = "echo '03-100' | cat > control/Commands.txt";
                    SshStream();
                    break;
                case 4:
                    command = "echo '04-100' | cat > control/Commands.txt";
                    SshStream();
                    break;
                case 5:
                    command = "echo '00-00' | cat > control/Commands.txt";
                    SshStream();
                    break;
            }
        }

        public void Startup()
        {            
            while (keepstart)
            {
                start = new SshClient(host, user, pass);
                try
                {
                    start.Connect();
                    start.RunCommand("sudo python3 control/CommandReader.py"); 
                }
                catch
                {
                    Console.Write("not wokring");
                }
            } start.Disconnect();
            

        }

        public void Connect()
        {
            robot_connect_label.Content = "Connected";
            client = new SshClient(host, user, pass);
            try
            {
                client.Connect();
                disconnect_button.IsEnabled = true;
                advance_button.IsEnabled = false;
                search_robot_button.IsEnabled = false;
                connect_robot_button.IsEnabled = false;
                back_button.IsEnabled = true;
                forward_button.IsEnabled = true;
                turn_left_button.IsEnabled = true;
                turn_right_button.IsEnabled = true;
                stop_button.IsEnabled = true;
                robot_connect_label.Background = Brushes.LightGreen;
                connected = true;
            }
            catch
            {
                connect_robot_button.IsEnabled = false;
                robot_connect_label.Background = Brushes.LightPink;
                robot_connect_label.Content = "Could not connect to Robot";
                robot_found_label.Background = Brushes.LightPink;
                robot_found_label.Content = "Try resetting the robot";
            }
        }

        public void Disconnect()
        {
            keepstart = true;
            connected = false;
            client.RunCommand("echo '05-00' | cat > control/Commands.txt");
            client.RunCommand("echo '00-00' | cat > control/Commands.txt");
            client.Disconnect();
            start.Disconnect();
            disconnect_button.IsEnabled = false;
            advance_button.IsEnabled = true;
            search_robot_button.IsEnabled = true;
            connect_robot_button.IsEnabled = false;
            back_button.IsEnabled = false;
            forward_button.IsEnabled = false;
            turn_right_button.IsEnabled = false;
            turn_left_button.IsEnabled = false;
            stop_button.IsEnabled = false;
            robot_connect_label.Content = "Not Connected";
            robot_connect_label.Background = Brushes.LightPink;
            robot_found_label.Content = "Search again...";
            robot_found_label.Background = Brushes.LightYellow;
            StartingThread.Abort();

        }

        public void DoGetHostEntry(string hostname)
        {
            Thread.Sleep(1000);
            try
            {
                IPHostEntry hostdata = Dns.GetHostEntry(hostname);
                IPAddress ip = hostdata.AddressList[0];
                robot_found_label.Background = Brushes.LightGreen;
                robot_found_label.Content = "Record Found at : " + ip;
                host = ip.ToString();
                allowConnect();

            }
            catch (Exception)
            {
                robot_found_label.Background = Brushes.LightPink;
                robot_found_label.Content = "Robot not Found on Network";
            }
        }

        public void allowConnect()
        {
            connect_robot_button.IsEnabled = true;
        }

        public void setcurrent()
        {
            current_host.Content = "Current Hostname: " + hostname;
            current_user.Content = "Current User: " + user;
        }

        public void SshStream()
        {
            try
            {
                var Command = client.CreateCommand(command);
                var asynch = Command.BeginExecute();
                var output = new StreamReader(Command.OutputStream);
                var error_message = new StreamReader(Command.ExtendedOutputStream);

                while (!asynch.IsCompleted)
                {
                    var result = output.ReadToEnd();
                    var error = error_message.ReadToEnd();
                    if (string.IsNullOrEmpty(result) && string.IsNullOrEmpty(error))
                        continue;
                    Console.WriteLine("StdErr: " + error);
                    Console.WriteLine("StdOut: " + result);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////// 
    }
}
