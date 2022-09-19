using libzkfpcsharp;
using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

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
        static void Main(string[] args)
        {


            Console.WriteLine("Check for finger print scanner!");
            int ret = zkfperrdef.ZKFP_ERR_OK;
            if ((ret = zkfp2.Init()) == zkfperrdef.ZKFP_ERR_OK)
            {
                int nCount = zkfp2.GetDeviceCount();
                if (zkfp2.GetDeviceCount()>0)
                {
                    Console.WriteLine("Found "+nCount+" Device");
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
                    Thread thread = new Thread(new ThreadStart(DoCapture));
                    thread.IsBackground = true;
                    thread.Start();
                    bIsTimeToDie = false;
                    Console.Read();

                }
                zkfp2.CloseDevice(mDevHandle);
            }
        }
        static void DoCapture()
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
                    bmp.Save("Fingerprint.jpeg");
                }
            }
        }
    }
}
