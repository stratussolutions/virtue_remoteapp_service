using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web.Profile;

namespace TestKeyCreation
{
    public class Program
    {
        public static readonly Guid Desktop = new Guid("B4BFCC3A-DB2C-424C-B029-7FE99A87C641");
        public static readonly Guid PublicDesktop = new Guid("C4AA340D-F20F-4863-AFEF-F87EF2E6BA25");
        [DllImport("wtsapi32.dll")]
        static extern Int32 WTSEnumerateSessions(
        IntPtr hServer,
        [MarshalAs(UnmanagedType.U4)] Int32 Reserved,
        [MarshalAs(UnmanagedType.U4)] Int32 Version,
        ref IntPtr ppSessionInfo,
        [MarshalAs(UnmanagedType.U4)] ref Int32 pCount);

        public static void Main(string[] args)
        {

            if (Directory.Exists("C:\\Users\\kamoser\\Desktop"))
            {
                Console.WriteLine("C");
            }
            if (Directory.Exists("D:\\Users\\kamoser\\Desktop"))
            {
                Console.WriteLine("D");
            }


            Console.ReadLine();
            //GenerateKey();
        }

        public static void GenerateKey()
        {
            CngKeyCreationParameters ckcParams = new CngKeyCreationParameters()
            {
                ExportPolicy = CngExportPolicies.AllowPlaintextExport,
                KeyCreationOptions = CngKeyCreationOptions.None,
                KeyUsage = CngKeyUsages.AllUsages,
                
            };
            ckcParams.Parameters.Add(new CngProperty("Length", BitConverter.GetBytes(2048), CngPropertyOptions.None));

            CngKey key = CngKey.Create(CngAlgorithm.Rsa, null, ckcParams);
            Console.WriteLine(key.Provider);
            //byte[] exportedPrivateBytes = key.Export(CngKeyBlobFormat.GenericPrivateBlob);
            //string exportedPrivateString = Encoding.UTF8.GetString(exportedPrivateBytes);
            //byte[] pkcs8PrivateKey = Encoding.UTF8.GetBytes(exportedPrivateString);

            byte[] privatePlainTextBlob = key.Export(CngKeyBlobFormat.Pkcs8PrivateBlob);
            byte[] publicPlainTextBlob = key.Export(CngKeyBlobFormat.GenericPublicBlob);
            Console.WriteLine("Private key: " + Convert.ToBase64String(privatePlainTextBlob));
            Console.WriteLine("Public key: " + Convert.ToBase64String(publicPlainTextBlob));
            Console.ReadLine();
        }
    }
}
