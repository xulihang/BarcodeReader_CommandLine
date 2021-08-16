using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json;
using Scandit.Recognition;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

namespace ScanditCommandLine
{
    class Program
    {
        static string ScanditBarcodeScannerAppKey = "AfUgBWaWRhz5FdlPVQHOBu4V3j5LFKttb3KHUHtNOXm7bT0KgjG40+YaHy+wRsrT4jHrctRaw62zYk7TvwgYoMpUQXonEGyp6EVftLFZv2aqWg8eVm3KL6MWUwfWAcb9AgLjc7NFU0ieZpXSiE4nNy4JCe9xKtVCV1T7R33VDkqhvR5kgCsm8Cq2G7n0SFutXtW5ly7EckhaUdxmXqVrtTvKb7pquNebRqNjljgShJoLgX0tDm7ADcKHs4Ft5YUrngWh/A0rHRYI3JZl538r71XE+euXfz1KnwK8oDLoSUfOgBVUG5i8NrYb4K7Bq9uEnQTVviq6AQT39PexXwOQ7Bhvd7Pgubsy8smOq1obrAS7KT1B8ze1i7eDrva8RD6IWRb8+ABbaJ6OsxpSVMjFhOADp6ZHIKNmjR4w+lhEGJl6fcYm+41Jg1V6ISsypM0Cuhb7T/rKphwI5Cw/98ya+6Vs4+tdpa8yESNOHxaZDXFWBicRdq1TtH0PlZtdPiZikLR+5LsqQ+QfBxSdWPBr30PxKNsV3ty0zQTA9RenA9rJxyGZGiyEuHgO4sy3Q2Jw15BFXAiSwmv9SLUW5Nw7WlTZRjcZym3nrAW+G+zbgeQNwn49qkERXudCzCPRCMGsDNEhTRr9sU9OHqo3cyRdHGOyt7j8kCjONBTqoAuC6s5miLGA6mFfpaIlBmA1kODldZ/tK7By9bXjgVOqqB03p+5r5V2tyHAevd9wKu0kKMFqEOXxBCMjGZR4wnAkwftJiphknvMr+uyWzuF+V3T6uVKApB/yxa94kgJ1K85MMBhSLlZLLS+zgw==";
        static void Main(string[] args)
        {
            string path = "";
            Boolean serverMode = true;
            if (args.Length == 1)
            {                
                path = args[0];
                serverMode = false;
            }
            else
            {
                Console.WriteLine("Running under server mode.");
            }
            //Console.WriteLine(ScanditBarcodeScannerAppKey);
            // the barcode scanner requires a writeable folder for caching purposes. 
            var tempPath = System.IO.Path.GetTempPath();
            // the recognition context handles and schedules recognition tasks.
            var context = new RecognitionContext(ScanditBarcodeScannerAppKey, tempPath);
            var settings = new BarcodeScannerSettings();
            // enable scanning of all codes.
            //settings.Symbologies[BarcodeSymbology.Ean8].Enabled = true;
            settings.Symbologies[BarcodeSymbology.Ean13].Enabled = true;
            settings.Symbologies[BarcodeSymbology.Upca].Enabled = true;
            //settings.Symbologies[BarcodeSymbology.Upce].Enabled = true;
            //settings.Symbologies[BarcodeSymbology.Qr].Enabled = true;
            //settings.Symbologies[BarcodeSymbology.Aztec].Enabled = true;
            //settings.Symbologies[BarcodeSymbology.Codabar].Enabled = true;
            //settings.Symbologies[BarcodeSymbology.Code11].Enabled = true;
            //settings.Symbologies[BarcodeSymbology.Code25].Enabled = true;
            //settings.Symbologies[BarcodeSymbology.Code32].Enabled = true;
            //settings.Symbologies[BarcodeSymbology.Code39].Enabled = true;
            //settings.Symbologies[BarcodeSymbology.Code93].Enabled = true;
            //settings.Symbologies[BarcodeSymbology.DataMatrix].Enabled = true;
            //settings.Symbologies[BarcodeSymbology.DotCode].Enabled = true;
            //settings.Symbologies[BarcodeSymbology.FiveDigitAddOn].Enabled = true;
            //settings.Symbologies[BarcodeSymbology.Gs1Databar].Enabled = true;
            //settings.Symbologies[BarcodeSymbology.Gs1DatabarExpanded].Enabled = true;
            //settings.Symbologies[BarcodeSymbology.Gs1DatabarLimited].Enabled = true;
            //settings.Symbologies[BarcodeSymbology.Interleaved2Of5].Enabled = true;
            //settings.Symbologies[BarcodeSymbology.Kix].Enabled = true;
            //settings.Symbologies[BarcodeSymbology.Lapa4sc].Enabled = true;
            //settings.Symbologies[BarcodeSymbology.MaxiCode].Enabled = true;
            //settings.Symbologies[BarcodeSymbology.MicroPdf417].Enabled = true;
            //settings.Symbologies[BarcodeSymbology.MicroQr].Enabled = true;
            //settings.Symbologies[BarcodeSymbology.MsiPlessey].Enabled = true;
            //settings.Symbologies[BarcodeSymbology.Pdf417].Enabled = true;
            //settings.Symbologies[BarcodeSymbology.Rm4scc].Enabled = true;
            //settings.Symbologies[BarcodeSymbology.TwoDigitAddOn].Enabled = true;
            settings.MaxNumberOfCodesPerFrame = 999;
            var scanner = new BarcodeScanner(context, settings);
            if (serverMode)
            {
                using (var server = new ResponseSocket())
                {
                    server.Bind("tcp://*:5556");
                    while (true)
                    {
                        string msg = server.ReceiveFrameString();
                        Console.WriteLine("From Client: {0}", msg);

                        if (File.Exists(msg))
                        {
                            server.SendFrame(decode(scanner, context, msg));
                        }
                        else
                        {
                            server.SendFrame("Received");
                        }

                        if (msg == "q")
                        {
                            System.Environment.Exit(0);
                        }

                    }
                    
                }
            }
            else
            {
                Console.WriteLine(decode(scanner, context, path));
            }
            
        }

        private static string decode(BarcodeScanner scanner, RecognitionContext context, string path)
        {
            string filename = Path.GetFileName(path);
            string folderPath = Path.GetDirectoryName(path);
            var inputImage = new Bitmap(path);

            var fmt = inputImage.PixelFormat;
            var bits = inputImage.LockBits(new Rectangle(0, 0, inputImage.Width, inputImage.Height),
                                           System.Drawing.Imaging.ImageLockMode.ReadOnly,
                                           System.Drawing.Imaging.PixelFormat.Format24bppRgb);

            if (bits.Stride < 0)
            {
                Console.WriteLine("Bottom-up images are not supported at that moment.");
                return "";
            }

            var imageDescription = new ImageDescription();
            imageDescription.Layout = ImageLayout.Rgb8U;
            imageDescription.Width = (uint)inputImage.Width;
            imageDescription.Height = (uint)inputImage.Height;
            imageDescription.FirstPlaneRowBytes = (uint)bits.Stride;
            imageDescription.MemorySize = (uint)(inputImage.Height * bits.Stride);

            using (var frameSeq = context.StartFrameSequence())
            {
                // recognize bar codes in the image, the result of the barcode recognition will become 
                // available in scanner.Session
                DateTime startTime = DateTime.Now;
                frameSeq.ProcessFrame(imageDescription, bits.Scan0);
                inputImage.UnlockBits(bits);
                var recognizedCodes = scanner.Session.GetNewlyRecognizedCodes().ToArray();
                DateTime endTime = DateTime.Now;
                long elapsed = (long)(endTime - startTime).TotalMilliseconds;
                //Console.WriteLine("Elased: " + elapsed);
                Dictionary<string, object> jsonObject = new Dictionary<string, Object>();
                ArrayList results = new ArrayList();
                if (recognizedCodes.Length > 0)
                {

                    //Console.WriteLine("recognized codes");
                    foreach (var code in recognizedCodes)
                    {
                        //Console.WriteLine("{0}: {1}", code.SymbologyString.ToUpper(), code.Data);
                        Dictionary<string, Object> result = new Dictionary<string, Object>();
                        result["barcodeText"] = code.Data;
                        result["barcodeFormat"] = code.SymbologyString;
                        result["x1"] = code.Location.TopLeft.X;
                        result["y1"] = code.Location.TopLeft.Y;
                        result["x2"] = code.Location.TopRight.X;
                        result["y2"] = code.Location.TopRight.Y;
                        result["x3"] = code.Location.BottomRight.X;
                        result["y3"] = code.Location.BottomRight.Y;
                        result["x4"] = code.Location.BottomLeft.X;
                        result["y4"] = code.Location.BottomLeft.Y;
                        results.Add(result);
                    }

                }
                jsonObject["results"] = results;
                jsonObject["elapsedTime"] = elapsed;
                string jsonStr = JsonConvert.SerializeObject(jsonObject);
                string outputPath = Path.Combine(folderPath, filename + "-scandit.json");
                //File.WriteAllText(outputPath, jsonStr);
                //Console.WriteLine(jsonStr);
                inputImage.Dispose();
                return jsonStr;
            }
        }
    }
}
