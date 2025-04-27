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
using System.Net.Http;
using System.Threading.Tasks;
using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Diagnostics;
using System.Net.Sockets;

namespace SoundReferbApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private HttpClient _httpClient;
        private string filePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ESP32Profile.xml");

        private bool _soundDetected = false;


        public MainWindow()
        {
            InitializeComponent();
            _httpClient = new HttpClient();
            //CheckConnection();
            ListenForSoundDetection();
        }

        private async void CheckConnection()
        {
            try
            {
                var response = await _httpClient.GetAsync("http://192.168.4.1");

                if (response.IsSuccessStatusCode)
                {
                    StatusText.Text = "Connected to ESP Network!";
                }
                else
                {
                    StatusText.Text = "Not Connected to ESP Network!";
                }
            }//END OF TRY
            catch (Exception)//this is catching before connected to esp
            {
                StatusText.Text = "Error, Not connected to ESP Network!";
            }
        }//end of CheckConnection

        private async void SendMessage_Click(object sender, RoutedEventArgs e)
        {
            string ssid = SSIDInput.Text; //variable to link to ssidbox
            string password = PasswordInput.Password; //variable to link to password input box.
            string ipaddress = GetLocalIP();

            if (string.IsNullOrWhiteSpace(ssid) || string.IsNullOrWhiteSpace(password))//check to make sure box boxes are filled out
            {
                MessageBox.Show("Missing SSID or Password...please fill out and click again.");
                return;//stop function here 
            }


            try//first try is for passing the ID to the ESP
            {
                var ssidContent = new StringContent(ssid, Encoding.UTF8, "text/plain");
                var ssidResponse = await _httpClient.PostAsync("http://192.168.4.1/sendSSID", ssidContent);//change send to a /sendSSID
                if (ssidResponse.IsSuccessStatusCode)
                {
                    StatusText.Text = "SSID has been sent successfully. ";
                }
                else
                {
                    StatusText.Text = "Failed to send SSID.";
                }
            }//END TRY
            catch
            {
                StatusText.Text = "Error sending SSID.";
            }

            try//2nd try is for sending the password.
            {
                var passwordContent = new StringContent(password, Encoding.UTF8, "text/plain");
                var passwordResponse = await _httpClient.PostAsync("http://192.168.4.1/sendpassword", passwordContent);//change send to a "/sendpassword
                if (passwordResponse.IsSuccessStatusCode)
                {
                    StatusText.Text += "\nPassword sent successfully!";
                }
                else
                {
                    StatusText.Text += "\nFailed sending Password.";
                }

            }
            catch
            {
                StatusText.Text = "\nError sending Password to ESP";
            }
            try
            {
                Debug.WriteLine(ipaddress);
                var ipContent = new StringContent(ipaddress, Encoding.UTF8, "text/plain");
                var ipResponse = await _httpClient.PostAsync("http://192.168.4.1/sendlocalIP", ipContent);
                if (ipResponse.IsSuccessStatusCode)
                {
                    StatusText.Text += "\nLocal IP sent successfully!";
                }
                else
                {
                    StatusText.Text += "\nFailed sending Local IP.";
                }
            }
            catch
            {
                StatusText.Text = "\nError sending local IP to ESP";
            }
            await Task.Delay(3000);

            ReconnectOriginalNetwork(ssid);
        }//end of click function

        private async void ConnectToESPButton_Click(object sender, RoutedEventArgs e) //method used for connecting to the ESP32 
        {
            try
            {
                // Path to the profile file included in your build output
                string profilePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ESP32Profile.xml");

                //Add the WiFi profile
                var addProfileProcess = new System.Diagnostics.ProcessStartInfo("netsh", $"wlan add profile filename=\"{profilePath}\"");
                addProfileProcess.CreateNoWindow = true;
                addProfileProcess.UseShellExecute = false;
                System.Diagnostics.Process.Start(addProfileProcess)?.WaitForExit();

                //Connect using the profile name
                var connectProcess = new System.Diagnostics.ProcessStartInfo("netsh", $"wlan connect name=\"ESP32WIFI\"");
                connectProcess.CreateNoWindow = true;
                connectProcess.UseShellExecute = false;
                System.Diagnostics.Process.Start(connectProcess)?.WaitForExit();

                // Wait and retry GET requests to ESP32
                bool connectedToESP = false;
                using (var httpClient = new HttpClient())
                {
                    for (int attempt = 0; attempt < 5; attempt++) // 5 tries
                    {
                        try
                        {
                            await Task.Delay(2000); // wait 2 seconds between tries

                            var response = await httpClient.GetAsync("http://192.168.4.1");
                            if (response.IsSuccessStatusCode)
                            {
                                connectedToESP = true;
                                break;
                            }
                        }
                        catch
                        {
                            // ignore and retry
                        }
                    }
                }

                //Ping the ESP to confirm connection
                using (var httpClient = new HttpClient())
                {
                    try
                    {
                        var response = await httpClient.GetAsync("http://192.168.4.1");
                        if (response.IsSuccessStatusCode)
                        {
                            StatusText.Text = "Connected to ESP Network!";
                        }
                        else
                        {
                            StatusText.Text = "Could not reach ESP after connecting.";
                        }
                    }
                    catch
                    {
                        StatusText.Text = "Failed to reach ESP. Not connected.";
                    }
                }
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Error connecting: {ex.Message}";
            }

        }

        private void ReconnectOriginalNetwork(string originalSSID){
            // Connect back to the original network using netsh
            if (!string.IsNullOrEmpty(originalSSID))
            {
                string command = $"netsh wlan connect name={originalSSID}";
                var processStartInfo = new System.Diagnostics.ProcessStartInfo("cmd.exe", $"/C {command}")
                {
                    WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden
                };
                System.Diagnostics.Process.Start(processStartInfo);
            }

            //NEED TO ADD CHECKS TO SEE IF YOU ARE RECONNECTED...SOME SORT OF UI TEXT ON APP. BUT AS OF NOW IT WORKS.

        }

        private string GetLocalIP()
        {
            string localIP = "";
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    localIP = ip.ToString();
                    break;
                }
            }
            return localIP;
        }

        private async void ListenForSoundDetection()
        {
            HttpListener listener = new HttpListener();
            listener.Prefixes.Add("http://+:8080/detection/");
            listener.Start();
            Debug.WriteLine("Starting Listener");
            while (true)
            {
                Debug.WriteLine("Pre get context");
                var context = await listener.GetContextAsync();
                Debug.WriteLine("Post get context");
                var request = context.Request;
                Debug.WriteLine(request);
                if (request.HttpMethod == "GET")
                {
                    Debug.WriteLine("Received GET request! Inside IF statement");
                    _soundDetected = true;
                    Dispatcher.Invoke(() => UpdateSoundDetectionText(true));

                    //respond to ESP32 with a 200 OK
                    context.Response.StatusCode = 200;
                    byte[] buffer = Encoding.UTF8.GetBytes("Sound received");
                    context.Response.OutputStream.Write(buffer, 0, buffer.Length);
                    context.Response.Close();

                    //Wait 5 seconds and reset cool
                    await Task.Delay(5000);
                    _soundDetected = false;
                    Dispatcher.Invoke(() => UpdateSoundDetectionText(false));
                }
            }
        }

        private void UpdateSoundDetectionText(bool detected)
        {
            if (detected)
            {
                SoundDetection.Text = "Sound Detected!";
                SoundDetection.Foreground = new SolidColorBrush(Colors.Green);
            }
            else
            {
                SoundDetection.Text = "No Sound Detected...";
                SoundDetection.Foreground = new SolidColorBrush(Colors.Red);
            }
        }

    }//end of MainWindow
}