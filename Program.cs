using libzkfpcsharp;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FPReader_ZK_Base
{
    class Program
    {
        private const string Filename = "fingerprint.jpeg";
        private static int defaultIndex = 0;
        static IntPtr mDevHandle = IntPtr.Zero;
        static IntPtr mDBHandle = IntPtr.Zero;
        static byte[] FPBuffer;
        static byte[] CapTmp = new byte[2048];
        static int cbCapTmp = 2048;
        private static int mfpWidth = 0;
        private static int mfpHeight = 0;
        private static int mfpDpi = 0;
        private static bool bIsTimeToDie = true;
        const int MESSAGE_CAPTURED_OK = 0x0400 + 6;

        [DllImport("user32.dll", EntryPoint = "SendMessageA")]
        public static extern int SendMessage(IntPtr hwnd, int wMsg, IntPtr wParam, IntPtr lParam);
        private static readonly HttpClient client = new HttpClient();
        static ConsoleEventDelegate handler;   // Keeps it from getting garbage collected
                                               // Pinvoke
        private delegate bool ConsoleEventDelegate(int eventType);
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetConsoleCtrlHandler(ConsoleEventDelegate callback, bool add);

        static bool ConsoleEventCallback(int eventType)
        {
            if (eventType == 2)
            {
                OnProcessExit();
            }
            return false;
        }

        private static void OnProcessExit()
        {
            zkfp2.CloseDevice(mDevHandle);
        }

        static void Main(string[] args)
        {
            handler = new ConsoleEventDelegate(ConsoleEventCallback);
            SetConsoleCtrlHandler(handler, true);

            Console.WriteLine("Check for finger print scanner!");
            int ret = zkfperrdef.ZKFP_ERR_OK;
            if ((ret = zkfp2.Init()) == zkfperrdef.ZKFP_ERR_OK)
            {
                int nCount = zkfp2.GetDeviceCount();
                if (zkfp2.GetDeviceCount() > 0)
                {
                    Console.WriteLine("Found " + nCount + " Device");
                }
                if (IntPtr.Zero == (mDevHandle = zkfp2.OpenDevice(defaultIndex)))
                {
                    Console.WriteLine("Failed to connect");
                }
                else
                {
                    Console.WriteLine("Connection Successful");
                    if (IntPtr.Zero == (mDBHandle = zkfp2.DBInit()))
                    {
                        Console.WriteLine("Init DB fail");
                        zkfp2.CloseDevice(mDevHandle);
                        mDevHandle = IntPtr.Zero;
                        return;
                    }
                    Console.WriteLine("Connected to DB Successfully");
                    byte[] paramValue = new byte[4];
                    int size = 4;
                    zkfp2.GetParameters(mDevHandle, 1, paramValue, ref size);
                    zkfp2.ByteArray2Int(paramValue, ref mfpWidth);

                    size = 4;
                    zkfp2.GetParameters(mDevHandle, 2, paramValue, ref size);
                    zkfp2.ByteArray2Int(paramValue, ref mfpHeight);

                    FPBuffer = new byte[mfpWidth * mfpHeight];

                    size = 4;
                    zkfp2.GetParameters(mDevHandle, 3, paramValue, ref size);
                    zkfp2.ByteArray2Int(paramValue, ref mfpDpi);
                    Console.WriteLine("reader parameter, image width:" + mfpWidth + ", height:" + mfpHeight + ", dpi:" + mfpDpi + "\n");

                    Console.WriteLine("Place your finger on the sensor and press any key");
                    Thread thread = new Thread(new ThreadStart(DoCaptureAsync));
                    thread.IsBackground = true;
                    thread.Start();
                    bIsTimeToDie = false;
                    Console.Read();

                }

            }
        }
        static async void DoCaptureAsync()
        {
            while (!bIsTimeToDie)
            {
                cbCapTmp = 2048;
                int ret = zkfp2.AcquireFingerprint(mDevHandle, FPBuffer, CapTmp, ref cbCapTmp);
                if (ret == zkfp.ZKFP_ERR_OK)
                {
                    Console.WriteLine("Fingerprint captured successfully");
                    MemoryStream ms = new MemoryStream();
                    BitmapFormat.GetBitmap(FPBuffer, mfpWidth, mfpHeight, ref ms);
                    Bitmap bmp = new Bitmap(ms);
/*                    bmp.Save("Fingerprint.jpeg");*/
                    byte[] byteArray = ms.ToArray();
/*                    var values = new Dictionary<string, string>
                      {
                          { "Title", "hello" },
                          { "Data", Convert.ToBase64String(byteArray) }

                      };*/
                    Payload p = new Payload();
                    p.Title = "2022-463";
                    p.Data = Convert.ToBase64String(byteArray);
                    var json = JsonConvert.SerializeObject(p);
                    /*                    var content = new FormUrlEncodedContent(values);*/
                    var content = new StringContent(json.ToString(), Encoding.UTF8, "application/json");

                    var response = await client.PostAsync("https://prod-229.westeurope.logic.azure.com:443/workflows/2d51ba759ba74a81bad3954b20b3affe/triggers/manual/paths/invoke?api-version=2016-06-01&sp=%2Ftriggers%2Fmanual%2Frun&sv=1.0&sig=nnbBmJzSTWtWbRWjg0RSkknBRb1xGG5mmhqHwNvqExc", content);

                    var responseString = await response.Content.ReadAsStringAsync();



                }
            }
        }
    }
}
class Payload
{
    public String Title { get; set; }
    public String Data { get; set; }
}
